/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008, 2009 , 2010 Andrea Maggiulli (a.maggiulli@gmail.com) 
  
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
    //! Bond Market Association index
    /*! The BMA index is the short-term tax-exempt reference index of
        the Bond Market Association.  It has tenor one week, is fixed
        weekly on Wednesdays and is applied with a one-day's fixing
        gap from Thursdays on for one week.  It is the tax-exempt
        correspondent of the 1M USD-Libor.
    */
    public class BMAIndex : InterestRateIndex {
        protected Handle<YieldTermStructure> termStructure_;
        public Handle<YieldTermStructure> forwardingTermStructure() { return termStructure_; }

        public BMAIndex() : this(new Handle<YieldTermStructure>()) { }
        public BMAIndex(Handle<YieldTermStructure> h)
            : base("BMA", new Period(1, TimeUnit.Weeks), 1, new USDCurrency(),
                   new UnitedStates(UnitedStates.Market.NYSE), new ActualActual(ActualActual.Convention.ISDA)) {
            termStructure_ = h;
            h.registerWith(update);
        }

        //! \name Index interface
        public override string name() { return "BMA"; }

        public override bool isValidFixingDate(Date fixingDate) {
            // either the fixing date is last Wednesday, or all days
            // between last Wednesday included and the fixing date are
            // holidays
            for (Date d = Utils.previousWednesday(fixingDate); d < fixingDate; ++d) {
                if (fixingCalendar_.isBusinessDay(d))
                    return false;
            }
            // also, the fixing date itself must be a business day
            return fixingCalendar_.isBusinessDay(fixingDate);
        }

        //! \name InterestRateIndex interface
        public override Date maturityDate(Date valueDate) {
            Date fixingDate = fixingCalendar_.advance(valueDate, -1, TimeUnit.Days);
            Date nextWednesday = Utils.previousWednesday(fixingDate + 7);
            return fixingCalendar_.advance(nextWednesday,1,TimeUnit.Days);
        }

        //! \name Date calculations
        /*! This method returns a schedule of fixing dates between start and end. */
        public Schedule fixingSchedule(Date start, Date end) {
           return new MakeSchedule().from(Utils.previousWednesday(start))
                             .to(Utils.nextWednesday(end))
                             .withFrequency(Frequency.Weekly)
                             .withCalendar(fixingCalendar_)
                             .withConvention(BusinessDayConvention.Following)
                             .forwards()
                             .value();
        }

        protected override double forecastFixing(Date fixingDate) {
            if (termStructure_.empty())
               throw new ApplicationException("null term structure set to this instance of " + name());
            Date start = fixingCalendar_.advance(fixingDate,1,TimeUnit.Days);
            Date end = maturityDate(start);
            return termStructure_.link.forwardRate(start, end, dayCounter_, Compounding.Simple).rate();
        }
    }

    public partial class Utils {
        public static Date previousWednesday(Date date) {
            int w = date.weekday();
            if (w >= 4) // roll back w-4 days
                return date - new Period((w - 4), TimeUnit.Days);
            else // roll forward 4-w days and back one week
                return date + new Period((4 - w - 7), TimeUnit.Days);
        }

        public static Date nextWednesday(Date date) {
            return previousWednesday(date+7);
        }
    }
}
