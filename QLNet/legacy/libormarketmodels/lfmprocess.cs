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
    public class LiborForwardModelProcess : StochasticProcess
    {
        public int size_;
        public IborIndex index_;
        public LfmCovarianceParameterization lfmParam_;
        public List<double> initialValues_; 
        public List<double> fixingTimes_;
        public List<Date> fixingDates_;
        public List<double> accrualStartTimes_;
        List<double> accrualEndTimes_;
        public List<double> accrualPeriod_;
        Vector m1;
        Vector m2;

        public LiborForwardModelProcess(int size, IborIndex index, IDiscretization disc)
            : base(disc )
        {
            size_ = size;
            index_ = index;
            initialValues_ = new InitializedList<double>(size_);
            fixingTimes_ = new InitializedList<double>(size);
            fixingDates_ = new InitializedList<Date>(size_);
            accrualStartTimes_ = new InitializedList<double>(size);
            accrualEndTimes_ = new InitializedList<double>(size);
            accrualPeriod_ = new InitializedList<double>(size_);
            m1 = new Vector(size_);
            m2 = new Vector(size_);
            DayCounter dayCounter = index.dayCounter();
            IList<CashFlow> flows = cashFlows(1);

            if(!(size_ == flows.Count))
                    throw new ArgumentException( "wrong number of cashflows");

            Date settlement = index_.forwardingTermStructure().link.referenceDate();
            Date startDate;
            IborCoupon iborcoupon = (IborCoupon)flows[0];
            startDate = iborcoupon.fixingDate();

            for (int i = 0; i < size_; ++i)
            {
                IborCoupon coupon = (IborCoupon)flows[i];

                if(!(coupon.date() == coupon.accrualEndDate()))
                    throw new ArgumentException("irregular coupon types are not suppported");

                initialValues_[i]=coupon.rate();
                accrualPeriod_[i]=coupon.accrualPeriod();

                fixingDates_[i]=coupon.fixingDate();
                fixingTimes_[i]=dayCounter.yearFraction(startDate, coupon.fixingDate());
                accrualStartTimes_[i]=dayCounter.yearFraction(settlement, coupon.accrualStartDate());
                accrualEndTimes_[i]=dayCounter.yearFraction(settlement, coupon.accrualEndDate());
            }
        }

        public LiborForwardModelProcess(int size, IborIndex index)
            : this( size, index,new EulerDiscretization()){}

        public override Vector drift(double t, Vector x) {
            Vector f = new Vector(size_, 0.0);
            Matrix covariance = lfmParam_.covariance(t, x);
            int m = nextIndexReset(t);

            for (int k = m; k < size_; ++k)
            {
                m1[k] = accrualPeriod_[k] * x[k] / (1 + accrualPeriod_[k] * x[k]);
                double inner_product = 0;
                m1.GetRange(m, k + 1 - m).ForEach(
                    (ii, vv) => inner_product += vv *
                        covariance.column(k).GetRange(m, covariance.rows() - m)[ii]);

                f[k] = inner_product - 0.5 * covariance[k, k];
            }
            return f;
        }

        public override Matrix diffusion(double t, Vector x) {
            return lfmParam_.diffusion(t, x);
        }

        public override Matrix covariance(double t, Vector x, double dt) {
            return lfmParam_.covariance(t, x) * dt;
        }

        public override Vector apply(Vector x0, Vector dx) {
            Vector tmp = new Vector(size_);
            for (int k = 0; k < size_; ++k) {
                tmp[k] = x0[k] * Math.Exp(dx[k]);
            }
            return tmp;
        }

        public override Vector evolve(double t0, Vector x0, double dt, Vector dw) {
            /* predictor-corrector step to reduce discretization errors.

               Short - but slow - solution would be

               Array rnd_0     = stdDeviation(t0, x0, dt)*dw;
               Array drift_0   = discretization_->drift(*this, t0, x0, dt);

               return apply(x0, ( drift_0 + discretization_
                    ->drift(*this,t0,apply(x0, drift_0 + rnd_0),dt) )*0.5 + rnd_0);

               The following implementation does the same but is faster.
            */

            int m = nextIndexReset(t0);
            double sdt = Math.Sqrt(dt);

            Vector f = new Vector(x0);
            Matrix diff = lfmParam_.diffusion(t0, x0);
            Matrix covariance = lfmParam_.covariance(t0, x0);

            for (int k = m; k < size_; ++k)
            {
                double y = accrualPeriod_[k] * x0[k];
                m1[k] = y / (1 + y);

                double d = 0;
                m1.GetRange(m, k + 1 - m).ForEach(
                    (ii, vv) => d += vv * 
                        covariance.column(k).GetRange(m, covariance.rows()-m)[ii]);
                d=(d -0.5 * covariance[k, k]) * dt;

                double r = 0;
                diff.row(k).ForEach((kk, vv) => r += vv * dw[kk]);
                r *= sdt;
                
                double x = y * Math.Exp(d + r);
                m2[k] = x / (1 + x);

                double inner_product = 0;
                m2.GetRange(m, k + 1 - m).ForEach(
                    (ii, vv) => inner_product += vv * 
                        covariance.column(k).GetRange(m, covariance.rows() - m)[ii]);
                f[k] = x0[k] * Math.Exp(0.5 * (d + (inner_product-0.5 * covariance[k, k]) * dt) + r);
            }

            return f;
        }

        public override Vector initialValues() {
            Vector tmp = new Vector(size());
            for (int i = 0; i < size(); ++i)
                tmp[i] = initialValues_[i];
            return tmp;
        }

        public void setCovarParam(LfmCovarianceParameterization param) {
            lfmParam_ = param;
        }

        public LfmCovarianceParameterization covarParam()  {
            return lfmParam_;
        }

        public IborIndex index() {
            return index_;
        }

        public List<CashFlow> cashFlows(){
            return cashFlows(1);
        }

        public List<CashFlow> cashFlows(double amount) 
        {
           Date refDate = index_.forwardingTermStructure().link.referenceDate();
            
            Schedule schedule = new Schedule(refDate,
                              refDate + new Period(index_.tenor().length() * size_,
                                               index_.tenor().units()),
                              index_.tenor(), index_.fixingCalendar(),
                              index_.businessDayConvention(),
                              index_.businessDayConvention(),
                              DateGeneration.Rule.Forward, false);

            IborLeg cashflows = (IborLeg) new IborLeg(schedule, index_)
                                              .withFixingDays(index_.fixingDays())
                                              .withPaymentDayCounter(index_.dayCounter())
                                              .withNotionals(amount)
                                              .withPaymentAdjustment(index_.businessDayConvention());
            return cashflows.value();
        }

        public override int size() { return size_; }

        public override int factors() {
            return lfmParam_.factors();
        }

        public List<double> fixingTimes() {
            return fixingTimes_;
        }

        public List<Date> fixingDates() {
            return fixingDates_;
        }

        public List<double> accrualStartTimes(){
            return accrualStartTimes_;
        }

        public List<double> accrualEndTimes(){
            return accrualEndTimes_;
        }

        public int nextIndexReset(double t) {
            //return upper_bound(fixingTimes_.begin(), fixingTimes_.end(), t)
            //        - fixingTimes_.begin();
            int result = fixingTimes_.FindIndex(x => x>t);
            if (result < 0)
                result = ~result - 1;
            // impose limits. we need the one before last at max or the first at min
            result = Math.Max(Math.Min(result, fixingTimes_.Count - 1),0);
            return result;
        }

        public List<double> discountBond(List<double> rates) {

            List<double> discountFactors = new InitializedList<double>(size_);
            discountFactors[0] = 1.0 / (1.0 + rates[0] * accrualPeriod_[0]);

            for (int i = 1; i < size_; ++i) {
                discountFactors[i] =
                    discountFactors[i - 1] / (1.0 + rates[i] * accrualPeriod_[i]);
            }
            return discountFactors;
        }
    }
}
