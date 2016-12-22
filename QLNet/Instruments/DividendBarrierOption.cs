//  Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)
//  
//  This file is part of QLNet Project https://github.com/amaggiulli/qlnet
//  QLNet is free software: you can redistribute it and/or modify it
//  under the terms of the QLNet license.  You should have received a
//  copy of the license along with this program; if not, license is  
//  available online at <http://qlnet.sourceforge.net/License.html>.
//   
//  QLNet is a based on QuantLib, a free-software/open-source library
//  for financial quantitative analysts and developers - http://quantlib.org/
//  The QuantLib license is available online at http://quantlib.org/license.shtml.
//  
//  This program is distributed in the hope that it will be useful, but WITHOUT
//  ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
//  FOR A PARTICULAR PURPOSE.  See the license for more details.

using System.Collections.Generic;

namespace QLNet
{
   //! Single-asset barrier option with discrete dividends
   /*! \ingroup instruments */
   public class DividendBarrierOption : BarrierOption
   {
      public DividendBarrierOption( Barrier.Type barrierType,
                                    double barrier,
                                    double rebate,
                                    StrikedTypePayoff payoff,
                                    Exercise exercise,
                                    List<Date> dividendDates,
                                    List<double> dividends)
         :base(barrierType, barrier, rebate, payoff, exercise)
      {
         cashFlow_ = Utils.DividendVector( dividendDates, dividends );
      }

      public override void setupArguments( IPricingEngineArguments args )
      {
         base.setupArguments(args);

         DividendBarrierOption.Arguments arguments = args as DividendBarrierOption.Arguments;
         Utils.QL_REQUIRE(arguments != null,()=> "wrong engine type");

         arguments.cashFlow = cashFlow_;
      }

      private List<Dividend> cashFlow_;


       //! %Arguments for dividend barrier option calculation
      public new class Arguments : BarrierOption.Arguments 
      {
         public List<Dividend> cashFlow = new List<Dividend>();
         public Arguments() {}
         public override void validate()
         {
            base.validate();

            Date exerciseDate = exercise.lastDate();

            for (int i = 0; i < cashFlow.Count; i++) 
            {
               Utils.QL_REQUIRE(cashFlow[i].date() <= exerciseDate,()=>
                  "the " + (i+1) + " dividend date ("+ cashFlow[i].date()+ ") is later than the exercise date ("
                  + exerciseDate + ")");
            }
         }
      }
      //! %Dividend-barrier-option %engine base class
      public new class Engine :  GenericEngine<DividendBarrierOption.Arguments,DividendBarrierOption.Results> {}

   }
   
}

