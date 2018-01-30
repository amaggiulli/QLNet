﻿/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)

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

namespace QLNet
{
   //! Local volatility surface derived from a Black vol surface
   /*! For details about this implementation refer to
       "Stochastic Volatility and Local Volatility," in
       "Case Studies and Financial Modelling Course Notes," by
       Jim Gatheral, Fall Term, 2003

       see www.math.nyu.edu/fellows_fin_math/gatheral/Lecture1_Fall02.pdf

       \bug this class is untested, probably unreliable.
   */

   public class LocalVolSurface : LocalVolTermStructure
   {
      private Handle<BlackVolTermStructure> blackTS_;
      private Handle<YieldTermStructure> riskFreeTS_, dividendTS_;
      private Handle<Quote> underlying_;

      public LocalVolSurface(Handle<BlackVolTermStructure> blackTS, Handle<YieldTermStructure> riskFreeTS,
                             Handle<YieldTermStructure> dividendTS, Handle<Quote> underlying)
          : base(blackTS.link.businessDayConvention(), blackTS.link.dayCounter())
      {
         blackTS_ = blackTS;
         riskFreeTS_ = riskFreeTS;
         dividendTS_ = dividendTS;
         underlying_ = underlying;

         blackTS_.registerWith(update);
         riskFreeTS_.registerWith(update);
         dividendTS_.registerWith(update);
         underlying_.registerWith(update);
      }

      public LocalVolSurface(Handle<BlackVolTermStructure> blackTS, Handle<YieldTermStructure> riskFreeTS,
                             Handle<YieldTermStructure> dividendTS, double underlying)
           : base(blackTS.link.businessDayConvention(), blackTS.link.dayCounter())
      {
         blackTS_ = blackTS;
         riskFreeTS_ = riskFreeTS;
         dividendTS_ = dividendTS;
         underlying_ = new Handle<Quote>(new SimpleQuote(underlying));

         blackTS_.registerWith(update);
         riskFreeTS_.registerWith(update);
         dividendTS_.registerWith(update);
         underlying_.registerWith(update);
      }

      // TermStructure interface
      public override Date referenceDate() { return blackTS_.link.referenceDate(); }

      public override DayCounter dayCounter() { return blackTS_.link.dayCounter(); }

      public override Date maxDate() { return blackTS_.link.maxDate(); }

      // VolatilityTermStructure interface
      public override double minStrike() { return blackTS_.link.minStrike(); }

      public override double maxStrike() { return blackTS_.link.maxStrike(); }

      protected override double localVolImpl(double t, double underlyingLevel)
      {
         double dr = riskFreeTS_.currentLink().discount(t, true);
         double dq = dividendTS_.currentLink().discount(t, true);
         double forwardValue = underlying_.currentLink().value() * dq / dr;

         // strike derivatives
         double strike, y, dy, strikep, strikem;
         double w, wp, wm, dwdy, d2wdy2;
         strike = underlyingLevel;
         y = Math.Log(strike / forwardValue);
         dy = ((Math.Abs(y) > 0.001) ? y * 0.0001 : 0.000001);
         strikep = strike * Math.Exp(dy);
         strikem = strike / Math.Exp(dy);
         w = blackTS_.link.blackVariance(t, strike, true);
         wp = blackTS_.link.blackVariance(t, strikep, true);
         wm = blackTS_.link.blackVariance(t, strikem, true);
         dwdy = (wp - wm) / (2.0 * dy);
         d2wdy2 = (wp - 2.0 * w + wm) / (dy * dy);

         // time derivative
         double dt, wpt, wmt, dwdt;
         if (t.IsEqual(0.0))
         {
            dt = 0.0001;
            double drpt = riskFreeTS_.currentLink().discount(t + dt, true);
            double dqpt = dividendTS_.currentLink().discount(t + dt, true);
            double strikept = strike * dr * dqpt / (drpt * dq);

            wpt = blackTS_.link.blackVariance(t + dt, strikept, true);

            Utils.QL_REQUIRE(wpt >= w, () =>
                 "decreasing variance at strike " + strike + " between time " + t + " and time " + (t + dt));
            dwdt = (wpt - w) / dt;
         }
         else
         {
            dt = Math.Min(0.0001, t / 2.0);
            double drpt = riskFreeTS_.currentLink().discount(t + dt, true);
            double drmt = riskFreeTS_.currentLink().discount(t - dt, true);
            double dqpt = dividendTS_.currentLink().discount(t + dt, true);
            double dqmt = dividendTS_.currentLink().discount(t - dt, true);

            double strikept = strike * dr * dqpt / (drpt * dq);
            double strikemt = strike * dr * dqmt / (drmt * dq);

            wpt = blackTS_.link.blackVariance(t + dt, strikept, true);
            wmt = blackTS_.link.blackVariance(t - dt, strikemt, true);
            Utils.QL_REQUIRE(wpt >= w, () =>
                 "decreasing variance at strike " + strike + " between time " + t + " and time " + (t + dt));
            Utils.QL_REQUIRE(w >= wmt, () =>
                 "decreasing variance at strike " + strike + " between time " + (t - dt) + " and time " + t);
            dwdt = (wpt - wmt) / (2.0 * dt);
         }

         if (dwdy.IsEqual(0.0) && d2wdy2.IsEqual(0.0))
         { // avoid /w where w might be 0.0
            return Math.Sqrt(dwdt);
         }
         else
         {
            double den1 = 1.0 - y / w * dwdy;
            double den2 = 0.25 * (-0.25 - 1.0 / w + y * y / w / w) * dwdy * dwdy;
            double den3 = 0.5 * d2wdy2;
            double den = den1 + den2 + den3;
            double result = dwdt / den;
            Utils.QL_REQUIRE(result >= 0.0, () =>
                 "negative local vol^2 at strike " + strike + " and time " + t + "; the black vol surface is not smooth enough");
            return Math.Sqrt(result);
         }
      }
   }
}
