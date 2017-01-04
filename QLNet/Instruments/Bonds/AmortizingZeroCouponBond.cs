/*
 Copyright (C) 2016 Jean-Camille Tournier (jean-camille.tournier@avivainvestors.com)
  
 This file is part of QLNet Project https://github.com/amaggiulli/qlnet

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
using System.Linq;
using System.Collections.Generic;

namespace QLNet
{
   //! amortizing zero coupon bond
   public class AmortizingZeroCouponBond : Bond
   {
        public AmortizingZeroCouponBond( int settlementDays, 
                                        Calendar calendar, 
                                        double faceAmount, 
                                        Date maturityDate,
                                        BusinessDayConvention paymentConvention, 
                                        List<Date> amortizingDates,
                                        List<double> notionals, 
                                        Date issueDate)
            :base(settlementDays, calendar, issueDate)
        {
            Utils.QL_REQUIRE(!amortizingDates.empty(), () => "bond with no amortizing dates!");
            Utils.QL_REQUIRE(!notionals.empty(), () => "bond with no amortizing notionals!");

            if (amortizingDates.Count != notionals.Count)
                throw new Exception("different number of amortizing dates and notionals!");

            maturityDate_ = maturityDate;
            setCashFlows(faceAmount, amortizingDates, notionals, paymentConvention);
        }


        protected void setCashFlows(double faceAmount, List<Date> amortizingDates, List<double> notionals, BusinessDayConvention paymentConvention)
        {
            notionals_.Clear();
            notionalSchedule_.Clear();
            redemptions_.Clear();

            notionalSchedule_.Add(new Date());
            notionals_.Add(faceAmount);

            for (int i = 0; i < amortizingDates.Count; i++)
            {
                double amount = faceAmount / 100.0 * ((i == 0 ? 100.0 : notionals[i - 1]) - notionals[i]);
                CashFlow payment;
                payment = new AmortizingPayment(amount, amortizingDates[i]);

                notionalSchedule_.Add(amortizingDates[i]);
                notionals_.Add(amount);
                cashflows_.Add(payment);
            }

            Date redemptionDate = calendar_.adjust(maturityDate_, paymentConvention);
            CashFlow redemption = new Redemption(2.0 * faceAmount - notionals_.Sum(), redemptionDate);
            notionalSchedule_.Add(redemptionDate);
            notionals_.Add(redemption.amount());
            cashflows_.Add(redemption);
        }
   }
}
