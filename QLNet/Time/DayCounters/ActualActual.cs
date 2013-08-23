/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
  
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

namespace QLNet
{
    //! Actual/Actual day count
    /*! The day count can be calculated according to:
	        - the ISDA convention, also known as "Actual/Actual (Historical)", "Actual/Actual", "Act/Act", 
              and according to ISDA also "Actual/365", "Act/365", and "A/365";
	        - the ISMA and US Treasury convention, also known as "Actual/Actual (Bond)";
	        - the AFB convention, also known as "Actual/Actual (Euro)".
	For more details, refer to http://www.isda.org/publications/pdf/Day-Count-Fracation1999.pdf  */
    public class ActualActual : DayCounter
    {
        public enum Convention { ISMA, Bond, ISDA, Historical, Actual365, AFB, Euro };

        public ActualActual() : base(ISDA_Impl.Singleton) { }
        public ActualActual(Convention c) : base(conventions(c)) { }

        private static DayCounter conventions(Convention c)
        {
            switch (c)
            {
                case Convention.ISMA:
                case Convention.Bond:
                    return ISMA_Impl.Singleton;
                case Convention.ISDA:
                case Convention.Historical:
                case Convention.Actual365:
                    return ISDA_Impl.Singleton;
                case Convention.AFB:
                case Convention.Euro:
                    return AFB_Impl.Singleton;
                default:
                    throw new ArgumentException("Unknown day count convention: " + c);
            }
        }

        private class ISMA_Impl : DayCounter
        {
            public static readonly ISMA_Impl Singleton = new ISMA_Impl();
            private ISMA_Impl() { }

            public override string name() { return "Actual/Actual (ISMA)"; }

            public override int dayCount(Date d1, Date d2) { return (d2 - d1); }

            public override double yearFraction(Date d1, Date d2, Date d3, Date d4)
            {
                if (d1 == d2) return 0;
                if (d1 > d2) return -yearFraction(d2, d1, d3, d4);

                // when the reference period is not specified, try taking it equal to (d1,d2)
                Date refPeriodStart = (d3 != null ? d3 : d1);
                Date refPeriodEnd = (d4 != null ? d4 : d2);

                if (!(refPeriodEnd > refPeriodStart && refPeriodEnd > d1))
                    throw new ArgumentException("Invalid reference period: date 1: " + d1 + ", date 2: " + d2 +
                          ", reference period start: " + refPeriodStart + ", reference period end: " + refPeriodEnd);

                // estimate roughly the length in months of a period
                int months = (int)(0.5 + 12 * (refPeriodEnd - refPeriodStart) / 365.0);

                // for short periods...
                if (months == 0)
                {
                    // ...take the reference period as 1 year from d1
                    refPeriodStart = d1;
                    refPeriodEnd = d1 + TimeUnit.Years;
                    months = 12;
                }

                double period = months / 12.0;

                if (d2 <= refPeriodEnd)
                {
                    // here refPeriodEnd is a future (notional?) payment date
                    if (d1 >= refPeriodStart)
                    {
                        // here refPeriodStart is the last (maybe notional) payment date.
                        // refPeriodStart <= d1 <= d2 <= refPeriodEnd
                        // [maybe the equality should be enforced, since	refPeriodStart < d1 <= d2 < refPeriodEnd	could give wrong results] ???
                        return period * dayCount(d1, d2) / dayCount(refPeriodStart, refPeriodEnd);
                    }
                    else
                    {
                        // here refPeriodStart is the next (maybe notional) payment date and refPeriodEnd is the second next (maybe notional) payment date.
                        // d1 < refPeriodStart < refPeriodEnd
                        // AND d2 <= refPeriodEnd
                        // this case is long first coupon

                        // the last notional payment date
                        Date previousRef = refPeriodStart - new Period(months, TimeUnit.Months);
                        if (d2 > refPeriodStart)
                            return yearFraction(d1, refPeriodStart, previousRef, refPeriodStart) +
                                   yearFraction(refPeriodStart, d2, refPeriodStart, refPeriodEnd);
                        else
                            return yearFraction(d1, d2, previousRef, refPeriodStart);
                    }
                }
                else
                {
                    // here refPeriodEnd is the last (notional?) payment date
                    // d1 < refPeriodEnd < d2 AND refPeriodStart < refPeriodEnd
                    if (!(refPeriodStart <= d1)) throw new ArgumentException("invalid dates: d1 < refPeriodStart < refPeriodEnd < d2");

                    // now it is: refPeriodStart <= d1 < refPeriodEnd < d2

                    // the part from d1 to refPeriodEnd
                    double sum = yearFraction(d1, refPeriodEnd, refPeriodStart, refPeriodEnd);

                    // the part from refPeriodEnd to d2
                    // count how many regular periods are in [refPeriodEnd, d2], then add the remaining time
                    int i = 0;
                    Date newRefStart, newRefEnd;
                    for(;;)
                    {
                        newRefStart = refPeriodEnd + new Period(months * i, TimeUnit.Months);
                        newRefEnd = refPeriodEnd + new Period(months * (i + 1), TimeUnit.Months);
                        if (d2 < newRefEnd)
                        {
                            break;
                        }
                        else
                        {
                            sum += period;
                            i++;
                        }
                    } 
                    sum += yearFraction(newRefStart, d2, newRefStart, newRefEnd);
                    return sum;
                }
            }
        };

        private class ISDA_Impl : DayCounter
        {
            public static readonly ISDA_Impl Singleton = new ISDA_Impl();
            private ISDA_Impl() { }

            public override string name() { return "Actual/Actual (ISDA)"; }

            public override int dayCount(Date d1, Date d2) { return (d2 - d1); }

            public override double yearFraction(Date d1, Date d2, Date refPeriodStart, Date refPeriodEnd)
            {
                if (d1 == d2) return 0;
                if (d1 > d2) return -yearFraction(d2, d1, null, null);

                int y1 = d1.Year, y2 = d2.Year;
                double dib1 = (Date.IsLeapYear(y1) ? 366 : 365),
                       dib2 = (Date.IsLeapYear(y2) ? 366 : 365);

                double sum = y2 - y1 - 1;
                sum += dayCount(d1, new Date(1, Month.January, y1 + 1)) / dib1;
                sum += dayCount(new Date(1, Month.January, y2), d2) / dib2;
                return sum;
            }
        };

        private class AFB_Impl : DayCounter
        {
            public static readonly AFB_Impl Singleton = new AFB_Impl();
            private AFB_Impl() { }

            public override string name() { return "Actual/Actual (AFB)"; }

            public override int dayCount(Date d1, Date d2) { return (d2 - d1); }

            public override double yearFraction(Date d1, Date d2, Date refPeriodStart, Date refPeriodEnd)
            {
                if (d1 == d2) return 0;
                if (d1 > d2) return -yearFraction(d2, d1, null, null);

                Date newD2 = d2, temp = d2;
                double sum = 0;
                while (temp > d1)
                {
                    temp = newD2 - TimeUnit.Years;
                    if (temp.Day == 28 && temp.Month == 2 && Date.IsLeapYear(temp.Year))
                        temp += 1;
                    if (temp >= d1)
                    {
                        sum += 1;
                        newD2 = temp;
                    }
                }

                double den = 365;

                if (Date.IsLeapYear(newD2.Year))
                {
                    temp = new Date(29, Month.February, newD2.Year);
                    if (newD2 > temp && d1 <= temp)
                        den += 1;
                }
                else if (Date.IsLeapYear(d1.Year))
                {
                    temp = new Date(29, Month.February, d1.Year);
                    if (newD2 > temp && d1 <= temp)
                        den += 1;
                }

                return sum + dayCount(d1, newD2) / den;
            }
        };

    }
}