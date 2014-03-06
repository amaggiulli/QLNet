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
    //! Single-asset vanilla option (no barriers) with discrete dividends
    /*! \ingroup instruments */
    public class DividendVanillaOption : OneAssetOption {
        private List<Dividend> cashFlow_;

        public DividendVanillaOption(StrikedTypePayoff payoff, Exercise exercise,
                                     List<Date> dividendDates, List<double> dividends)   
            : base(payoff, exercise) {
            cashFlow_ = Utils.DividendVector(dividendDates, dividends);
        }
        
        
        /*! \warning see VanillaOption for notes on implied-volatility
                     calculation.
        */
        public double impliedVolatility(double targetValue, GeneralizedBlackScholesProcess process,
				 double accuracy = 1.0e-4, int maxEvaluations = 100, double minVol = 1.0e-7, double maxVol = 4.0)
		  {

					 if (isExpired()) throw new ApplicationException("option expired");

					 SimpleQuote volQuote = new SimpleQuote();

					 GeneralizedBlackScholesProcess newProcess = ImpliedVolatilityHelper.clone(process, volQuote);

					 // engines are built-in for the time being
					 IPricingEngine engine;
					 switch (exercise_.type())
					 {
						 case Exercise.Type.European:
							engine = new AnalyticDividendEuropeanEngine(newProcess);
							break;
						 case Exercise.Type.American:
							engine = new FDDividendAmericanEngine(newProcess);
							break;
						 case Exercise.Type.Bermudan:
							 throw new ApplicationException("engine not available for Bermudan option with dividends");
						 default:
							 throw new ArgumentException("unknown exercise type");
					 }

					 return ImpliedVolatilityHelper.calculate(this, engine, volQuote, targetValue, accuracy,
																			maxEvaluations, minVol, maxVol);
        }


        public override void setupArguments(IPricingEngineArguments args)  {
            base.setupArguments(args);

            Arguments arguments = args as Arguments;
            if (arguments == null) throw new ApplicationException("wrong engine type");

            arguments.cashFlow = cashFlow_;
        }


        //! %Arguments for dividend vanilla option calculation
        new public class Arguments : OneAssetOption.Arguments {
            public List<Dividend> cashFlow;
            
            public Arguments() {}
            
            public override void validate() {
                base.validate();

                Date exerciseDate = exercise.lastDate();

                for (int i = 0; i < cashFlow.Count; i++) {
                    if (!(cashFlow[i].date() <= exerciseDate))
                        throw new ApplicationException((i+1) + " dividend date (" + cashFlow[i].date()
                               + ") is later than the exercise date (" + exerciseDate + ")");
                }
            }
        }

        //! %Dividend-vanilla-option %engine base class
        new public class Engine : GenericEngine<Arguments, Results> { }
    }
}
