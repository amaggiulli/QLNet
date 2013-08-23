/*
 Copyright (C) 2010 Philippe Real (ph_real@hotmail.com)
  
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
    //! Jamshidian swaption engine
    /*! \ingroup swaptionengines

        \warning The engine assumes that the exercise date equals the
                 start date of the passed swap.
    */

    public class JamshidianSwaptionEngine : GenericModelEngine<OneFactorAffineModel,
                                                                Swaption.Arguments,
                                                                Swaption.Results>
    {

        /*! \note the term structure is only needed when the short-rate
                 model cannot provide one itself.
        */
        public JamshidianSwaptionEngine(OneFactorAffineModel model,
                                Handle<YieldTermStructure> termStructure)
            : base(model)
        {
            termStructure_ = termStructure;
            termStructure_.registerWith(update);
        }

        public JamshidianSwaptionEngine(OneFactorAffineModel model)
            : this(model, new Handle<YieldTermStructure>())
        { }

        private Handle<YieldTermStructure> termStructure_;

        public class rStarFinder : ISolver1d
        {

            public rStarFinder(OneFactorAffineModel model,
                        double nominal,
                        double maturity,
                        List<double> fixedPayTimes,
                        List<double> amounts)
            {
                strike_ = nominal;
                maturity_ = maturity;
                times_ = fixedPayTimes;
                amounts_ = amounts;
                model_ = model;
            }

            public override double value(double x)
            {
                double value = strike_;
                int size = times_.Count;
                for (int i = 0; i < size; i++)
                {
                    double dbValue =
                        model_.discountBond(maturity_, times_[i], x);
                    value -= amounts_[i] * dbValue;
                }
                return value;
            }

            private double strike_;
            private double maturity_;
            private List<double> times_;
            private List<double> amounts_;
            private OneFactorAffineModel model_;
        }

        public override void calculate()
        {
            if (!(arguments_.settlementType == Settlement.Type.Physical))
                throw new ArgumentException("cash-settled swaptions not priced by Jamshidian engine");

            if (!(arguments_.exercise.type() == Exercise.Type.European))
                throw new ArgumentException("cannot use the Jamshidian decomposition "
                       + "on exotic swaptions");

            Date referenceDate;
            DayCounter dayCounter;

            ITermStructureConsistentModel tsmodel =
                (ITermStructureConsistentModel)base.model_;
            try
            {
                if (tsmodel != null)
                {
                    referenceDate = tsmodel.termStructure().link.referenceDate();
                    dayCounter = tsmodel.termStructure().link.dayCounter();
                }
                else
                {
                    referenceDate = termStructure_.link.referenceDate();
                    dayCounter = termStructure_.link.dayCounter();
                }
            }
            catch
            {
                referenceDate = termStructure_.link.referenceDate();
                dayCounter = termStructure_.link.dayCounter();
            }

            List<double> amounts = new InitializedList<double>(arguments_.fixedCoupons.Count);
            for (int i = 0; i < amounts.Count; i++)
                amounts[i] = arguments_.fixedCoupons[i];
            amounts[amounts.Count-1] = amounts.Last() + arguments_.nominal;
            //amounts.Last()+=arguments_.nominal;

            double maturity = dayCounter.yearFraction(referenceDate,
                                                    arguments_.exercise.date(0));

            List<double> fixedPayTimes = new InitializedList<double>(arguments_.fixedPayDates.Count);
            for (int i = 0; i < fixedPayTimes.Count; i++)
                fixedPayTimes[i] =
                    dayCounter.yearFraction(referenceDate,
                                            arguments_.fixedPayDates[i]);

            rStarFinder finder = new rStarFinder(model_, arguments_.nominal, maturity,
                                                fixedPayTimes, amounts);
            Brent s1d = new Brent();
            double minStrike = -10.0;
            double maxStrike = 10.0;
            s1d.setMaxEvaluations(10000);
            s1d.setLowerBound(minStrike);
            s1d.setUpperBound(maxStrike);
            double rStar = s1d.solve(finder, 1e-8, 0.05, minStrike, maxStrike);

            Option.Type w = arguments_.type == VanillaSwap.Type.Payer ?
                                                    Option.Type.Put : Option.Type.Call;
            int size = arguments_.fixedCoupons.Count;

            double value = 0.0;
            for (int i = 0; i < size; i++)
            {
                double fixedPayTime =
                    dayCounter.yearFraction(referenceDate,
                                            arguments_.fixedPayDates[i]);
                double strike = model_.discountBond(maturity,
                                                   fixedPayTime,
                                                   rStar);
                double dboValue = model_.discountBondOption(
                                                   w, strike, maturity,
                                                   fixedPayTime);
                value += amounts[i] * dboValue;
            }
            results_.value = value;
        }
    }
}