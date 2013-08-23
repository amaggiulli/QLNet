/*
 Copyright (C) 2009 Philippe Real (ph_real@hotmail.com)
  
 This file is part of QLNet Project http://qlnet.sourceforge.net/

 QLNet is free software: you can redistribute it and/or modify it
 under the terms of the QLNet license.  You should have received a
 copy of the license along with this program; if not, license is  
 available online at <http://qlnet.sourceforge.net/License.html>.
  
 QLNet is a based on QuantLib, a free-software/open-source library
 for financial quantitative analysts and developers - http://quantlib.org/
 The QuantLib license is available online at http://quantlib.org/license.shtml.
 
 This program is distributed in the hope that it will be useful, but WITHOUT
 ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
 FOR A PARTICULAR PURPOSE.  See the license for more details.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QLNet
{
    public class LiborForwardModel : CalibratedModel, IAffineModel
    {
        List<double> f_;
        List<double> accrualPeriod_;

        LfmCovarianceProxy covarProxy_;
        LiborForwardModelProcess process_;
        SwaptionVolatilityMatrix swaptionVola;

        public LiborForwardModel(LiborForwardModelProcess process,
                          LmVolatilityModel volaModel,
                          LmCorrelationModel corrModel)
            : base(volaModel.parameters().Count() + corrModel.parameters().Count()) {

            f_ = new InitializedList<double>(process.size());
            accrualPeriod_ = new InitializedList<double>(process.size());
            covarProxy_=new LfmCovarianceProxy(volaModel, corrModel);
            process_=process;
            
            /*copy(volaModel.parameters().begin(), volaModel.parameters().end(),
            arguments_.begin());
            copy(corrModel.parameters().begin(), corrModel.parameters().end(),
            arguments_.begin()+k);*/
            
            int k=volaModel.parameters().Count;
            for (int j = 0; j < k; j++)
                arguments_[j] = volaModel.parameters()[j];
            for (int j = 0; j < corrModel.parameters().Count; j++)
                arguments_[j+k] = corrModel.parameters()[j];

            for (int i=0; i < process.size(); ++i) {
                accrualPeriod_[i] =  process.accrualEndTimes()[i]
                                - process.accrualStartTimes()[i];
                f_[i] = 1.0/(1.0+accrualPeriod_[i]*process_.initialValues()[i]);
            }
        }



        public override void setParams( Vector parameters) {
        base.setParams(parameters);

        int k=covarProxy_.volatilityModel().parameters().Count;

        covarProxy_.volatilityModel().setParams(new List<Parameter>(arguments_.GetRange(0, k)));
        covarProxy_.correlationModel().setParams(new List<Parameter>(arguments_.GetRange(k, arguments_.Count-k)));

        swaptionVola = null;
    }


        public double discountBondOption(Option.Type type,
                                               double strike, double maturity,
                                               double bondMaturity)  {

            List<double>  accrualStartTimes
                = process_.accrualStartTimes();
            List<double>   accrualEndTimes
                = process_.accrualEndTimes();

            if (!(accrualStartTimes.First() <= maturity && accrualStartTimes.Last() >= maturity))
                throw new ApplicationException("capet maturity does not fit to the process"); 
            
            int i = accrualStartTimes.BinarySearch(maturity);
            if (i < 0)
                // The lower_bound() algorithm finds the first position in a sequence that value can occupy 
                // without violating the sequence's ordering
                // if BinarySearch does not find value the value, the index of the prev minor item is returned
                i = ~i + 1;

            // impose limits. we need the one before last at max or the first at min
            i = Math.Max(Math.Min(i, accrualStartTimes.Count - 1), 0);
            
            if  (!(i<process_.size()
                && Math.Abs(maturity - accrualStartTimes[i]) < 100 * Const.QL_Epsilon
                && Math.Abs(bondMaturity - accrualEndTimes[i]) < 100 * Const.QL_Epsilon))
                throw new ApplicationException("irregular fixings are not (yet) supported"); 

            double tenor     = accrualEndTimes[i] - accrualStartTimes[i];
            double forward   = process_.initialValues()[i];
            double capRate   = (1.0/strike - 1.0)/tenor;
            double var = covarProxy_.integratedCovariance(i, i, process_.fixingTimes()[i] );
            double dis = process_.index().forwardingTermStructure().link.discount(bondMaturity);

            double black = Utils.blackFormula(
                (type == Option.Type.Put ? Option.Type.Call : Option.Type.Put),
                capRate, forward, Math.Sqrt(var));

            double   npv = dis * tenor * black;

            return npv / (1.0 + capRate*tenor);
        }

        public double discount(double t) {
           return process_.index().forwardingTermStructure().link.discount(t);
        }

        public double discountBond(double t,double maturity,Vector v) {
            return discount(maturity);
        }

        public Vector w_0(int alpha, int beta)  
{
            Vector omega = new Vector(beta + 1, 0.0);
            if(!(alpha<beta))
                throw new ApplicationException("alpha needs to be smaller than beta");

            double s=0.0;
            for (int k=alpha+1; k<=beta; ++k) {
                double b = accrualPeriod_[k];
                for (int j=alpha+1; j<=k; ++j) {
                    b*=f_[j];
                }
                s+=b;
            }

            for (int i = alpha + 1; i <= beta; ++i){
                double a = accrualPeriod_[i];
                for (int j = alpha + 1; j <= i; ++j){
                    a*=f_[j];
                }
                omega[i] = a/s;
            }
            return omega;
        }
    
        public double S_0(int alpha, int beta)  {
            Vector w = w_0(alpha, beta);
            Vector f = process_.initialValues();

            double fwdRate=0.0;
            for (int i=alpha+1; i <=beta; ++i) {
                fwdRate+=w[i]*f[i];
            }
            return fwdRate;
        }
    

        // calculating swaption volatility matrix using
        // Rebonatos approx. formula. Be aware that this
        // matrix is valid only for regular fixings and
        // assumes that the fix and floating leg have the
        // same frequency
        public SwaptionVolatilityMatrix getSwaptionVolatilityMatrix()  
        {
            if (swaptionVola!=null) {
                return swaptionVola;
            }

            IborIndex index = process_.index();
            Date today = process_.fixingDates()[0];

            int size=process_.size()/2;
            Matrix volatilities=new Matrix(size, size);

            List<Date> exercises = new InitializedList<Date>(size);
            for (int i = 0; i < size; ++i){
                exercises[i]=process_.fixingDates()[i+1];
            }

            List<Period> lengths = new InitializedList<Period>(size);
            for (int i=0; i < size; ++i) {
                lengths[i] = (i+1)*index.tenor();
            }

            Vector f = process_.initialValues();
            for (int k=0; k < size; ++k) {
                int alpha  =k;
                double t_alpha=process_.fixingTimes()[alpha+1];

                Matrix var=new Matrix(size, size);
                for (int i=alpha+1; i <= k+size; ++i) {
                    for (int j=i; j <= k+size; ++j) {
                        var[i-alpha-1,j-alpha-1] = var[j-alpha-1,i-alpha-1] =
                            covarProxy_.integratedCovariance(i, j, t_alpha,null);
                    }
                }

                for (int l=1; l <= size; ++l) {
                    int beta =l + k;
                    Vector w = w_0(alpha, beta);

                    double sum=0.0;
                    for (int i=alpha+1; i <= beta; ++i) {
                        for (int j=alpha+1; j <= beta; ++j) {
                            sum+=w[i]*w[j]*f[i]*f[j]*var[i-alpha-1,j-alpha-1];
                        }
                    }
                    volatilities[k,l-1] =
                        Math.Sqrt(sum/t_alpha)/S_0(alpha, beta);
                }
            }

            return swaptionVola = new SwaptionVolatilityMatrix( today, exercises, lengths,
                                                                volatilities,index.dayCounter());
        }
    }
}
