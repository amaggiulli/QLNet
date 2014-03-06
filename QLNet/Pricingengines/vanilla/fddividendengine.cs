/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2013  Andrea Maggiulli (a.maggiulli@gmail.com)
  
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

namespace QLNet {
    //! Abstract base class for dividend engines
    /*! \todo The dividend class really needs to be made more
              sophisticated to distinguish between fixed dividends and fractional dividends
    */
    public abstract class FDDividendEngineBase : FDMultiPeriodEngine {
		 // required for generics
		 public FDDividendEngineBase() { }

        //public FDDividendEngineBase(GeneralizedBlackScholesProcess process,
        //    Size timeSteps = 100, Size gridPoints = 100, bool timeDependent = false)
        public FDDividendEngineBase(GeneralizedBlackScholesProcess process, int timeSteps, int gridPoints, bool timeDependent)
            : base(process, timeSteps, gridPoints, timeDependent) {}

		  public override FDVanillaEngine factory(GeneralizedBlackScholesProcess process,
													 int timeSteps, int gridPoints, bool timeDependent)
		  {
			  return factory2(process, timeSteps, gridPoints, timeDependent);
		  }

		  public abstract FDVanillaEngine factory2(GeneralizedBlackScholesProcess process,
													 int timeSteps, int gridPoints, bool timeDependent);

        public override void setupArguments(IPricingEngineArguments a) {
            DividendVanillaOption.Arguments args = a as DividendVanillaOption.Arguments;
            if (args == null) throw new ApplicationException("incorrect argument type");
            List<Event> events = new List<Event>();
            foreach (Event e in args.cashFlow)
                events.Add(e);
            base.setupArguments(a, events);
        }

        protected double getDividendAmount(int i) {
            Dividend dividend = events_[i] as Dividend;
            if (dividend != null) {
                return dividend.amount();
            } else {
                return 0.0;
            }
        }

        protected double getDiscountedDividend(int i) {
            double dividend = getDividendAmount(i);
            double discount = process_.riskFreeRate().link.discount(events_[i].date()) /
                              process_.dividendYield().link.discount(events_[i].date());
            return dividend * discount;
        }
    }


    //! Finite-differences pricing engine for dividend options using
    // escowed dividend model
    /*! \ingroup vanillaengines */
    /* The merton 73 engine is the classic engine described in most
       derivatives texts.  However, Haug, Haug, and Lewis in
       "Back to Basics: a new approach to the discrete dividend
       problem" argues that this scheme underprices call options.
       This is set as the default engine, because it is consistent
       with the analytic version.
    */
    public class FDDividendEngineMerton73 : FDDividendEngineBase {
		 // required for generics
		 public FDDividendEngineMerton73() { }

        public FDDividendEngineMerton73(GeneralizedBlackScholesProcess process, int timeSteps, int gridPoints, bool timeDependent)
            : base(process, timeSteps, gridPoints, timeDependent) {}

		  public override FDVanillaEngine factory2(GeneralizedBlackScholesProcess process, int timeSteps, int gridPoints, bool timeDependent)
		  {
			  throw new NotImplementedException();
		  }
        // The value of the x axis is the NPV of the underlying minus the
        // value of the paid dividends.

        // Note that to get the PDE to work, I have to scale the values
        // and not shift them.  This means that the price curve assumes
        // that the dividends are scaled with the value of the underlying.
        //
        protected override void setGridLimits() {
            double paidDividends = 0.0;
            for (int i=0; i<events_.Count; i++) {
                if (getDividendTime(i) >= 0.0)
                    paidDividends += getDiscountedDividend(i);
            }

            base.setGridLimits(process_.stateVariable().link.value()-paidDividends, getResidualTime());
            ensureStrikeInGrid();
        }

        // TODO:  Make this work for both fixed and scaled dividends
        protected override void executeIntermediateStep(int step) {
            double scaleFactor = getDiscountedDividend(step) / center_ + 1.0;
            sMin_ *= scaleFactor;
            sMax_ *= scaleFactor;
            center_ *= scaleFactor;

            intrinsicValues_.scaleGrid(scaleFactor);
            intrinsicValues_.sample(payoff_.value);
            prices_.scaleGrid(scaleFactor);
            initializeOperator();
            initializeModel();

            initializeStepCondition();
            stepCondition_.applyTo(prices_.values(), getDividendTime(step));
        }

		 
	 }


    //! Finite-differences engine for dividend options using shifted dividends
    /*! \ingroup vanillaengines */
    /* This engine uses the same algorithm that was used in quantlib
       in versions 0.3.11 and earlier.  It produces results that
       are different from the Merton 73 engine.

       \todo Review literature to see whether this is described
    */
    public class FDDividendEngineShiftScale : FDDividendEngineBase {
        public FDDividendEngineShiftScale(GeneralizedBlackScholesProcess process, int timeSteps, int gridPoints, bool timeDependent)
            : base(process, timeSteps, gridPoints, timeDependent) {}

		  public override FDVanillaEngine factory2(GeneralizedBlackScholesProcess process, int timeSteps, int gridPoints, bool timeDependent)
		  {
			  throw new NotImplementedException();
		  }

        protected override void setGridLimits() {
            double underlying = process_.stateVariable().link.value();
            for (int i=0; i<events_.Count; i++) {
                Dividend dividend = events_[i] as Dividend;
                if (dividend == null) continue;
                if (getDividendTime(i) < 0.0) continue;
                underlying -= dividend.amount(underlying);
            }

            base.setGridLimits(underlying, getResidualTime());
            ensureStrikeInGrid();
        }

        protected override void executeIntermediateStep(int step) {
            Dividend dividend = events_[step] as Dividend;
            if (dividend == null) return;
            DividendAdder adder = new DividendAdder(dividend);
            sMin_ = adder.value(sMin_);
            sMax_ = adder.value(sMax_);
            center_ = adder.value(center_);
            intrinsicValues_.transformGrid(adder.value);

            intrinsicValues_.sample(payoff_.value);
            prices_.transformGrid(adder.value);

            initializeOperator();
            initializeModel();

            initializeStepCondition();
            stepCondition_.applyTo(prices_.values(), getDividendTime(step));
        }

        class DividendAdder {
            private Dividend dividend;
            
            public DividendAdder(Dividend d) {
                dividend = d;
            }
            public double value(double x) {
                return x + dividend.amount(x);
            }
        }
    }

	// Use Merton73 engine as default.
   public class FDDividendEngine : FDDividendEngineMerton73 
	{

		public FDDividendEngine()
		{}

      public FDDividendEngine( GeneralizedBlackScholesProcess process, int timeSteps = 100,int gridPoints = 100,
                               bool timeDependent = false) : base(process, timeSteps,gridPoints, timeDependent) 
		{}

		public override FDVanillaEngine factory2(GeneralizedBlackScholesProcess process, int timeSteps, int gridPoints, bool timeDependent)
		{
			return new FDDividendEngine(process, timeSteps, gridPoints, timeDependent);
		}
    }
}
