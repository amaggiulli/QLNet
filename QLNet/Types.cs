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

namespace QLNet {
    // interface for all value methods
    public interface IValue {
        double value(double v);
    }

    public struct Const {
        public const double QL_Epsilon = 2.2204460492503131e-016;

        public const double M_SQRT_2 = 0.7071067811865475244008443621048490392848359376887;
        public const double M_1_SQRTPI = 0.564189583547756286948;

        public const double M_LN2 = 0.693147180559945309417;
        public const double M_PI = 3.141592653589793238462643383280;
        public const double M_PI_2 = 1.57079632679489661923;
    }

    public class TimeSeries<T> : Dictionary<Date, T> {
		// constructors
		public TimeSeries() : base() {}
        public TimeSeries(int size) : base(size) { }
    }

    public struct Duration {
        public enum Type { Simple, Macaulay, Modified };
    };

	public struct Position {
		public enum Type { Long, Short };
	};

    public enum InterestRateType { Fixed, Floating }
    //! Interest rate coumpounding rule
    public enum Compounding {
        Simple = 0,          //!< \f$ 1+rt \f$
        Compounded = 1,      //!< \f$ (1+r)^t \f$
        Continuous = 2,      //!< \f$ e^{rt} \f$
        SimpleThenCompounded //!< Simple up to the first period then Compounded
    };
	
	public enum Month { January   = 1,
                 February  = 2,
                 March     = 3,
                 April     = 4,
                 May       = 5,
                 June      = 6,
                 July      = 7,
                 August    = 8,
                 September = 9,
                 October   = 10,
                 November  = 11,
                 December  = 12,
                 Jan = 1,
                 Feb = 2,
                 Mar = 3,
                 Apr = 4,
                 Jun = 6,
                 Jul = 7,
                 Aug = 8,
                 Sep = 9,
                 Oct = 10,
                 Nov = 11,
                 Dec = 12
    };

	public enum BusinessDayConvention {
        // ISDA
        Following,          /*!< Choose the first business day after
                                 the given holiday. */
        ModifiedFollowing,  /*!< Choose the first business day after
                                 the given holiday unless it belongs
                                 to a different month, in which case
                                 choose the first business day before
                                 the holiday. */
        Preceding,          /*!< Choose the first business day before
                                 the given holiday. */
        // NON ISDA
        ModifiedPreceding,  /*!< Choose the first business day before
                                 the given holiday unless it belongs
                                 to a different month, in which case
                                 choose the first business day after
                                 the holiday. */
        Unadjusted          /*!< Do not adjust. */
    };

    //! Units used to describe time periods
    public enum TimeUnit {
        Days,
        Weeks,
        Months,
        Years };

    public enum Frequency {
        NoFrequency = -1,     //!< null frequency
        Once = 0,             //!< only once, e.g., a zero-coupon
        Annual = 1,           //!< once a year
        Semiannual = 2,       //!< twice a year
        EveryFourthMonth = 3, //!< every fourth month
        Quarterly = 4,        //!< every third month
        Bimonthly = 6,        //!< every second month
        Monthly = 12,         //!< once a month
        EveryFourthWeek = 13, //!< every fourth week
        Biweekly = 26,        //!< every second week
        Weekly = 52,          //!< once a week
        Daily = 365,          //!< once a day
        OtherFrequency = 999  //!< some other unknown frequency
    };

    // These conventions specify the rule used to generate dates in a Schedule.
    public struct DateGeneration {
        public enum Rule {
            Backward,      /*!< Backward from termination date to effective date. */
            Forward,       /*!< Forward from effective date to termination date. */
            Zero,          /*!< No intermediate dates between effective date and termination date. */
            ThirdWednesday,/*!< All dates but effective date and termination date are taken to be on the third wednesday of their month*/
            Twentieth,     /*!< All dates but the effective date are taken to be the twentieth of their
                                month (used for CDS schedules in emerging markets.)  The termination
                                date is also modified. */
            TwentiethIMM,   /*!< All dates but the effective date are taken to be the twentieth of an IMM
                                month (used for CDS schedules.)  The termination date is also modified. */
            OldCDS,         /*!< Same as TwentiethIMM with unrestricted date ends and log/short stub 
                                 coupon period (old CDS convention). */
            CDS             /*!< Credit derivatives standard rule since 'Big Bang' changes in 2009.  */

        }
    };

    public enum CapFloorType { Cap, Floor, Collar };

}