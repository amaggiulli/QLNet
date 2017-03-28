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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QLNet
{
   //! %Forward performance engine for vanilla options
   /*! \ingroup forwardengines

       \test
       - the correctness of the returned value is tested by
         reproducing results available in literature.
       - the correctness of the returned greeks is tested by
         reproducing numerical derivatives.
   */
   public class ForwardPerformanceVanillaEngine : ForwardVanillaEngine
   {
      public ForwardPerformanceVanillaEngine( GeneralizedBlackScholesProcess process, GetOriginalEngine getEngine )
         : base( process, getEngine ) { } 
      public override void calculate()
      {
         this.setup();
         this.originalEngine_.calculate();
         getOriginalResults();
      }
      protected override void getOriginalResults()
      {
         DayCounter rfdc = this.process_.riskFreeRate().link.dayCounter();
         double resetTime = rfdc.yearFraction(this.process_.riskFreeRate().link.referenceDate(),
             this.arguments_.resetDate );
         double discR = this.process_.riskFreeRate().link.discount(this.arguments_.resetDate );
         // it's a performance option
         discR /= this.process_.stateVariable().link.value();

         double? temp = this.originalResults_.value;
         this.results_.value = discR * temp;
         this.results_.delta = 0.0;
         this.results_.gamma = 0.0;
         this.results_.theta = this.process_.riskFreeRate().link.
             zeroRate( this.arguments_.resetDate, rfdc, Compounding.Continuous, Frequency.NoFrequency ).value()
             * this.results_.value;
         this.results_.vega = discR * this.originalResults_.vega;
         this.results_.rho = -resetTime * this.results_.value + discR * this.originalResults_.rho;
         this.results_.dividendRho = discR * this.originalResults_.dividendRho;

      }
   }
}
