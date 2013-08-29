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
   public class AmortizingFixedRateBond : Bond
   {
      protected Frequency frequency_;
      protected DayCounter dayCounter_;
      protected Schedule schedule_;

      public AmortizingFixedRateBond(
                          int settlementDays,
                          List<double> notionals,
                          Schedule schedule,
                          List<double> coupons,
                          DayCounter accrualDayCounter,
                          BusinessDayConvention paymentConvention = BusinessDayConvention.Following,
                          Date issueDate = null)
         :base(settlementDays, schedule.calendar(), issueDate)
      {
         frequency_ = schedule.tenor().frequency();
         dayCounter_ = accrualDayCounter;
         schedule_ = schedule;

         maturityDate_ = schedule.endDate();

         cashflows_ = new FixedRateLeg(schedule)
             .withCouponRates(coupons, accrualDayCounter)
             .withNotionals(notionals)
             .withPaymentAdjustment(paymentConvention).value();
             

         addRedemptionsToCashflows();

         if ( cashflows().empty())
            throw new ApplicationException("bond with no cashflows!");
      }

      public AmortizingFixedRateBond(
                          int settlementDays,
                          Calendar calendar,
                          double faceAmount,
                          Date startDate,
                          Period bondTenor,
                          Frequency sinkingFrequency,
                          double coupon,
                          DayCounter accrualDayCounter,
                          BusinessDayConvention paymentConvention = BusinessDayConvention.Following,
                          Date issueDate = null)
         :base(settlementDays, calendar, issueDate)
      {
         frequency_ = sinkingFrequency;
         dayCounter_ = accrualDayCounter;

         maturityDate_ = startDate + bondTenor;
         schedule_ = sinkingSchedule(startDate, bondTenor, sinkingFrequency, calendar);
         cashflows_ = new FixedRateLeg(schedule_)
                        .withCouponRates(coupon, accrualDayCounter)
                        .withNotionals(sinkingNotionals(bondTenor, sinkingFrequency, coupon, faceAmount))        
                        .withPaymentAdjustment(paymentConvention).value();

         addRedemptionsToCashflows();

      }

      Frequency frequency() { return frequency_; }
      DayCounter dayCounter() { return dayCounter_; }

      protected Schedule sinkingSchedule( Date startDate,
                                Period maturityTenor,
                                Frequency sinkingFrequency,
                                Calendar paymentCalendar) 
      {
            Period freqPeriod = new Period(sinkingFrequency);
            Date maturityDate = new Date(startDate + maturityTenor);
            Schedule retVal = new Schedule(startDate, maturityDate, freqPeriod,
                            paymentCalendar,BusinessDayConvention.Unadjusted,BusinessDayConvention.Unadjusted,
                            DateGeneration.Rule.Backward, false);
            return retVal;
      }

      protected List<double> sinkingNotionals(Period maturityTenor,
                                    Frequency sinkingFrequency,
                                    double couponRate,
                                    double initialNotional) 
      {
            Period freqPeriod = new Period(sinkingFrequency);
            int nPeriods = 0;
            if(!isSubPeriod(freqPeriod, maturityTenor, out nPeriods))
               throw new  ApplicationException("Bond frequency is incompatible with the maturity tenor");

            List<double> notionals = new InitializedList<double>(nPeriods+1);
            notionals[0] = initialNotional;
            double coupon = couponRate / (double)sinkingFrequency;
            double compoundedInterest = 1.0;
            double totalValue = Math.Pow(1.0+coupon, nPeriods);
            for(int i = 0; i < (int)nPeriods-1; ++i) 
            {
                compoundedInterest *= (1.0 + coupon);
                double currentNotional =
                    initialNotional*(compoundedInterest - (compoundedInterest-1.0)/(1.0 - 1.0/totalValue));
                notionals[i+1] = currentNotional;
            }
            notionals[notionals.Count-1] = 0.0;
            return notionals;
        }

      protected bool isSubPeriod(Period subPeriod,Period superPeriod,out int numSubPeriods)
      {
            numSubPeriods = 0;

            KeyValuePair<int, int> superDays = daysMinMax(superPeriod);
            KeyValuePair<int, int>  subDays = daysMinMax(subPeriod);

            //obtain the approximate time ratio
            double minPeriodRatio =
                ((double)superDays.Key)/((double)subDays.Value);
            double maxPeriodRatio =
                ((double)superDays.Value)/((double)subDays.Key);
            int lowRatio = (int)(Math.Floor(minPeriodRatio));
            int highRatio = (int)(Math.Ceiling(maxPeriodRatio));

            try 
            {
                for(int i=lowRatio; i <= highRatio; ++i) 
                {
                    Period testPeriod = subPeriod * i;
                    if(testPeriod == superPeriod) 
                    {
                        numSubPeriods = i;
                        return true;
                    }
                }
            } 
            catch(Exception) 
            {
                return false;
            }

            return false;
        }

      KeyValuePair<int, int> daysMinMax(Period p) 
      {
            switch (p.units()) {
              case TimeUnit.Days:
                return new KeyValuePair<int, int>(p.length(), p.length());
              case TimeUnit.Weeks:
                return new KeyValuePair<int, int>(7*p.length(), 7*p.length());
              case TimeUnit.Months:
                return new KeyValuePair<int, int>(28*p.length(), 31*p.length());
              case TimeUnit.Years:
                return new KeyValuePair<int, int>(365 * p.length(), 366 * p.length());
              default:
                throw new ApplicationException("unknown time unit (" + p.units() + ")");
            }
        }
   }
}
