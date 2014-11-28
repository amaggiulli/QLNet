/*
 Copyright (C) 2008, 2009 , 2010, 2011, 2012  Andrea Maggiulli (a.maggiulli@gmail.com) 
  
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
   //! amortizing CMS-rate bond
   public class AmortizingCmsRateBond : Bond
   {
      public AmortizingCmsRateBond( int settlementDays,
                               List<double> notionals,
                               Schedule schedule,
                               SwapIndex index,
                               DayCounter paymentDayCounter,
                               BusinessDayConvention paymentConvention = BusinessDayConvention.Following,
                               int fixingDays = 0,
                               List<double> gearings = null,
                               List<double> spreads = null,
                               List<double> caps = null,
                               List<double> floors = null,
                               bool inArrears = false,
                               Date issueDate = null)
         :base(settlementDays, schedule.calendar(), issueDate)
      {
         // Optional value check
         if ( gearings == null ) gearings = new List<double>(){1.0};
         if ( spreads == null ) spreads = new List<double>(){0};
         if (caps == null) caps = new List<double>();
         if (floors == null) floors = new List<double>();

         maturityDate_ = schedule.endDate();

         cashflows_ = new CmsLeg(schedule, index)
             .withPaymentDayCounter(paymentDayCounter)
             .withFixingDays(fixingDays)
             .withGearings(gearings)
             .withSpreads(spreads)
             .withCaps(caps)
             .withFloors(floors)
             .inArrears(inArrears)
             .withNotionals(notionals)
             .withPaymentAdjustment(paymentConvention);

         addRedemptionsToCashflows();

         Utils.QL_REQUIRE( !cashflows().empty(), () => "bond with no cashflows!" );

         index.registerWith(update);
        
      }
    
   }
}
