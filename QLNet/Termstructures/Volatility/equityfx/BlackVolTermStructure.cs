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
using System.Linq;
using System.Text;

namespace QLNet {
    //! Black-volatility term structure
    /*! This abstract class defines the interface of concrete
        Black-volatility term structures which will be derived from
        this one.

        Volatilities are assumed to be expressed on an annual basis.
    */
    public class BlackVolTermStructure : VolatilityTermStructure {
        private const double dT = 1.0/365.0;

        //! default constructor
        /*! \warning term structures initialized by means of this
                     constructor must manage their own reference date
                     by overriding the referenceDate() method.
        */
        // public BlackVolTermStructure(Calendar cal = Calendar(), BusinessDayConvention bdc = Following, DayCounter dc = DayCounter())
        public BlackVolTermStructure() : this(new Calendar(), BusinessDayConvention.Following, new DayCounter()) { }
        public BlackVolTermStructure(Calendar cal, BusinessDayConvention bdc, DayCounter dc)
            : base(cal, bdc, dc) {}

        //! initialize with a fixed reference date
        //public BlackVolTermStructure(Date referenceDate, Calendar cal = Calendar(),
        //                             BusinessDayConvention bdc = Following, DayCounter dc = DayCounter());
        public BlackVolTermStructure(Date referenceDate, Calendar cal, BusinessDayConvention bdc, DayCounter dc)
            : base(referenceDate, cal, bdc, dc) { }

        //! calculate the reference date based on the global evaluation date
        //public BlackVolTermStructure(int settlementDays, Calendar cal, BusinessDayConvention bdc = Following, DayCounter dc = DayCounter());
        public BlackVolTermStructure(int settlementDays, Calendar cal, BusinessDayConvention bdc, DayCounter dc)
            : base(settlementDays, cal, bdc, dc) { }

        //! \name Black Volatility
        //@{
        //! spot volatility
        public double blackVol(Date maturity, double strike) { return blackVol(maturity, strike, false); }
        public double blackVol(Date d, double strike, bool extrapolate) {
            checkRange(d, extrapolate);
            checkStrike(strike, extrapolate);
            double t = timeFromReference(d);
            return blackVolImpl(t, strike);
        }
        public double blackVol(double t, double strike) { return blackVol(t, strike, false); }
        public double blackVol(double t, double strike, bool extrapolate) {
            checkRange(t, extrapolate);
            checkStrike(strike, extrapolate);
            return blackVolImpl(t, strike);
        }

        //! spot variance
        public double blackVariance(Date maturity, double strike) { return blackVariance(maturity, strike, false); }
        public double blackVariance(Date d, double strike, bool extrapolate) {
            checkRange(d, extrapolate);
            checkStrike(strike, extrapolate);
            double t = timeFromReference(d);
            return blackVarianceImpl(t, strike);
        }
        public double blackVariance(double maturity, double strike) { return blackVariance(maturity, strike, false); }
        public double blackVariance(double t, double strike, bool extrapolate) {
            checkRange(t, extrapolate);
            checkStrike(strike, extrapolate);
            return blackVarianceImpl(t, strike);
        }


        //! forward (at-the-money) volatility
        //public double blackForwardVol(Date date1, Date date2, double strike, bool extrapolate = false) {
        public double blackForwardVol(Date date1, Date date2, double strike, bool extrapolate) {
            // (redundant) date-based checks
            if (!(date1 <= date2)) throw new ApplicationException(date1 + " later than " + date2);
            
            checkRange(date2, extrapolate);

            // using the time implementation
            double time1 = timeFromReference(date1);
            double time2 = timeFromReference(date2);
            return blackForwardVol(time1, time2, strike, extrapolate);
        }

        // public double blackForwardVol(Time time1, Time time2, Real strike, bool extrapolate = false) const;
        public double blackForwardVol(double time1, double time2, double strike, bool extrapolate) {
            if (!(time1 <= time2)) throw new ApplicationException(time1 + " later than " + time2);

            checkRange(time2, extrapolate);
            checkStrike(strike, extrapolate);
            if (time2 == time1) {
                if (time1 == 0.0) {
                    double epsilon = 1.0e-5;
                    double var = blackVarianceImpl(epsilon, strike);
                    return Math.Sqrt(var / epsilon);
                } else {
                    double epsilon = Math.Min(1.0e-5, time1);
                    double var1 = blackVarianceImpl(time1 - epsilon, strike);
                    double var2 = blackVarianceImpl(time1 + epsilon, strike);
                    if (!(var2 >= var1)) throw new ApplicationException("variances must be non-decreasing");
                    return Math.Sqrt((var2 - var1) / (2 * epsilon));
                }
            } else {
                double var1 = blackVarianceImpl(time1, strike);
                double var2 = blackVarianceImpl(time2, strike);
                if (!(var2 >= var1)) throw new ApplicationException("variances must be non-decreasing");
                return Math.Sqrt((var2 - var1) / (time2 - time1));
            }
        }

        //! forward (at-the-money) variance
        // public double blackForwardVariance(Date date1, Date date2, double strike, bool extrapolate = false) {
        public double blackForwardVariance(Date date1, Date date2, double strike, bool extrapolate) {
            // (redundant) date-based checks
            if (!(date1 <= date2)) throw new ApplicationException(date1 + " later than " + date2);

            checkRange(date2, extrapolate);

            // using the time implementation
            double time1 = timeFromReference(date1);
            double time2 = timeFromReference(date2);
            return blackForwardVariance(time1, time2, strike, extrapolate);
        }

        //public double blackForwardVariance(double time1, double time2, double strike, bool extrapolate = false) {
        public double blackForwardVariance(double time1, double time2, double strike, bool extrapolate) {
            if (!(time1 <= time2)) throw new ApplicationException(time1 + " later than " + time2);

            checkRange(time2, extrapolate);
            checkStrike(strike, extrapolate);

            double var1 = blackVarianceImpl(time1, strike);
            double var2 = blackVarianceImpl(time2, strike);
            if (!(var2 >= var1)) throw new ApplicationException("variances must be non-decreasing");

            return var2 - var1;
        }

        /*! \name Calculations

            These methods must be implemented in derived classes to perform
            the actual volatility calculations. When they are called,
            range check has already been performed; therefore, they must
            assume that extrapolation is required.
        */
        //@{
        //! Black variance calculation
        protected virtual double blackVarianceImpl(double t, double strike) { throw new NotSupportedException(); }
        //! Black volatility calculation
        protected virtual double blackVolImpl(double t, double strike) { throw new NotSupportedException(); }
    }


    //! Black-volatility term structure
    /*! This abstract class acts as an adapter to BlackVolTermStructure allowing the programmer to implement only the
        <tt>blackVolImpl(Time, Real, bool)</tt> method in derived classes.

        Volatility are assumed to be expressed on an annual basis.
    */
    public class BlackVolatilityTermStructure : BlackVolTermStructure {
        //! default constructor
        /*! \warning term structures initialized by means of this
                     constructor must manage their own reference date
                     by overriding the referenceDate() method.
        */
        //public BlackVolatilityTermStructure(Calendar cal = Calendar(), BusinessDayConvention bdc = Following,
        //                                    DayCounter dc = DayCounter())
        public BlackVolatilityTermStructure(Calendar cal, BusinessDayConvention bdc, DayCounter dc)
            : base(cal, bdc, dc) { }

        //! initialize with a fixed reference date
        //public BlackVolatilityTermStructure(Date referenceDate, Calendar cal = Calendar(),
        //                                    BusinessDayConvention bdc = Following, DayCounter dc = DayCounter());
        public BlackVolatilityTermStructure(Date referenceDate, Calendar cal, BusinessDayConvention bdc, DayCounter dc)
            : base(referenceDate, cal, bdc, dc) { }

        //! calculate the reference date based on the global evaluation date
        //public BlackVolatilityTermStructure(int settlementDays, Calendar cal,
        //                                    BusinessDayConvention bdc = Following, DayCounter dc = DayCounter());
        public BlackVolatilityTermStructure(int settlementDays, Calendar cal, BusinessDayConvention bdc, DayCounter dc)
            : base(settlementDays, cal, bdc, dc) { }

        /*! Returns the variance for the given strike and date calculating it from the volatility. */
        protected override double blackVarianceImpl(double t, double strike) {
            double vol = blackVolImpl(t, strike);
            return vol * vol * t;
        }
    }


    //! Black variance term structure
    /*! This abstract class acts as an adapter to VolTermStructure allowing the programmer to implement only the
        <tt>blackVarianceImpl(Time, Real, bool)</tt> method in derived classes.

        Volatility are assumed to be expressed on an annual basis.
    */
    public class BlackVarianceTermStructure : BlackVolTermStructure {
        //! default constructor
        /*! \warning term structures initialized by means of this
                     constructor must manage their own reference date
                     by overriding the referenceDate() method.
        */
        //public BlackVarianceTermStructure(Calendar cal = Calendar(), BusinessDayConvention bdc = Following,
        //                                    DayCounter dc = DayCounter())
        public BlackVarianceTermStructure()
            : this(new Calendar(), BusinessDayConvention.Following, new DayCounter()) { }
        public BlackVarianceTermStructure(Calendar cal, BusinessDayConvention bdc, DayCounter dc)
            : base(cal, bdc, dc) { }

        //! initialize with a fixed reference date
        //public BlackVarianceTermStructure(Date referenceDate, Calendar cal = Calendar(),
        //                                    BusinessDayConvention bdc = Following, DayCounter dc = DayCounter());
        public BlackVarianceTermStructure(Date referenceDate)
            : this(referenceDate, new Calendar(), BusinessDayConvention.Following, new DayCounter()) { }
        public BlackVarianceTermStructure(Date referenceDate, Calendar cal, BusinessDayConvention bdc, DayCounter dc)
            : base(referenceDate, cal, bdc, dc) { }

        //! calculate the reference date based on the global evaluation date
        //public BlackVarianceTermStructure(int settlementDays, Calendar cal,
        //                                    BusinessDayConvention bdc = Following, DayCounter dc = DayCounter());
        public BlackVarianceTermStructure(int settlementDays, Calendar cal, BusinessDayConvention bdc, DayCounter dc)
            : base(settlementDays, cal, bdc, dc) { }

        /*! Returns the volatility for the given strike and date calculating it from the variance. */
        protected override double blackVarianceImpl(double t, double strike) {
            double nonZeroMaturity = (t==0.0 ? 0.00001 : t);
            double var = blackVarianceImpl(nonZeroMaturity, strike);
            return Math.Sqrt(var/nonZeroMaturity);
        }

        protected override double blackVolImpl(double t, double strike)
        {
            double nonZeroMaturity = (t == 0.0 ? 0.00001 : t);
            double var = blackVarianceImpl(nonZeroMaturity, strike);
            return Math.Sqrt(var / nonZeroMaturity);
        }

    }
}
