//  Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)
//                2017 Jean-Camille Tournier (jean-camille.tournier@avivainvestors.com)
//
//  This file is part of QLNet Project https://github.com/amaggiulli/qlnet
//  QLNet is free software: you can redistribute it and/or modify it
//  under the terms of the QLNet license.  You should have received a
//  copy of the license along with this program; if not, license is
//  available at <https://github.com/amaggiulli/QLNet/blob/develop/LICENSE>.
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

namespace QLNet
{
   public class SviInterpolatedSmileSection : SmileSection
   {
      //! \name Constructors
      //@{
      //! all market data are quotes
      public SviInterpolatedSmileSection(
         Date optionDate,
         Handle<Quote> forward,
         List<double> strikes,
         bool hasFloatingStrikes,
         Handle<Quote> atmVolatility,
         List<Handle<Quote>> volHandles,
         double a, double b, double sigma, double rho, double m,
         bool isAFixed, bool isBFixed, bool isSigmaFixed, bool isRhoFixed, bool isMFixed,
         bool vegaWeighted,
         EndCriteria endCriteria = null,
         OptimizationMethod method = null,
         DayCounter dc = null)
         : base(optionDate, dc)
      {
         forward_ = forward;
         atmVolatility_ = atmVolatility;
         volHandles_ = volHandles;
         strikes_ = strikes;
         actualStrikes_ = strikes;
         hasFloatingStrikes_ = hasFloatingStrikes;
         a_ = a;
         b_ = b;
         sigma_ = sigma;
         rho_ = rho;
         m_ = m;
         isAFixed_ = isAFixed;
         isBFixed_ = isBFixed;
         isSigmaFixed_ = isSigmaFixed;
         isRhoFixed_ = isRhoFixed;
         isMFixed_ = isMFixed;
         vegaWeighted_ = vegaWeighted;
         endCriteria_ = endCriteria;
         method_ = method;

         forward_.registerWith(update);
         atmVolatility_.registerWith(update);

         for (int i = 0; i < volHandles_.Count; ++i)
            volHandles_[i].registerWith(update);
      }

      public SviInterpolatedSmileSection(
         Date optionDate,
         double forward,
         List<double> strikes,
         bool hasFloatingStrikes,
         double atmVolatility,
         List<double> volHandles,
         double a, double b, double sigma, double rho, double m,
         bool isAFixed, bool isBFixed, bool isSigmaFixed, bool isRhoFixed, bool isMFixed,
         bool vegaWeighted,
         EndCriteria endCriteria = null,
         OptimizationMethod method = null,
         DayCounter dc = null)
         : base(optionDate, dc)
      {
         forward_ = new Handle<Quote>(new SimpleQuote(forward));
         atmVolatility_ = new Handle<Quote>(new SimpleQuote(atmVolatility));
         strikes_ = strikes;
         actualStrikes_ = strikes;
         hasFloatingStrikes_ = hasFloatingStrikes;
         vols_ = volHandles;
         a_ = a;
         b_ = b;
         sigma_ = sigma;
         rho_ = rho;
         m_ = m;
         isAFixed_ = isAFixed;
         isBFixed_ = isBFixed;
         isSigmaFixed_ = isSigmaFixed;
         isRhoFixed_ = isRhoFixed;
         isMFixed_ = isMFixed;
         vegaWeighted_ = vegaWeighted;
         endCriteria_ = endCriteria;
         method_ = method;

         for (int i = 0; i < volHandles_.Count; ++i)
            volHandles_[i] = new Handle<Quote>(new SimpleQuote(volHandles[i]));
      }


      protected override void performCalculations()
      {
         forwardValue_ = forward_.link.value();
         vols_.Clear();
         actualStrikes_.Clear();
         // we populate the volatilities, skipping the invalid ones
         for (int i = 0; i < volHandles_.Count; ++i)
         {
            if (volHandles_[i].link.isValid())
            {
               if (hasFloatingStrikes_)
               {
                  actualStrikes_.Add(forwardValue_ + strikes_[i]);
                  vols_.Add(atmVolatility_.link.value() +
                            volHandles_[i].link.value());
               }
               else
               {
                  actualStrikes_.Add(strikes_[i]);
                  vols_.Add(volHandles_[i].link.value());
               }
            }
         }
         // we are recreating the sabrinterpolation object unconditionnaly to
         // avoid iterator invalidation
         createInterpolation();
         sviInterpolation_.update();
      }


      protected void createInterpolation()
      {
         SviInterpolation tmp = new SviInterpolation(actualStrikes_.Where(x => actualStrikes_.First().IsEqual(x)).ToList(),
                                                     actualStrikes_.Count,
                                                     vols_.Where(x => vols_.First().IsEqual(x)).ToList(),
                                                     exerciseTime(), forwardValue_, a_, b_, sigma_, rho_, m_, isAFixed_,
                                                     isBFixed_, isSigmaFixed_, isRhoFixed_, isMFixed_, vegaWeighted_,
                                                     endCriteria_, method_);
         Utils.swap<SviInterpolation>(ref tmp, ref sviInterpolation_);
      }

      protected override double varianceImpl(double strike)
      {
         calculate();
         double v = sviInterpolation_.value(strike, true);
         return v * v * exerciseTime();
      }

      protected override double volatilityImpl(double strike)
      {
         calculate();
         return sviInterpolation_.value(strike, true);
      }
      public override double minStrike() { calculate(); return strikes_.First(); }
      public override double maxStrike() { calculate(); return strikes_.Last(); }
      public override double? atmLevel() { throw new NotImplementedException(); }
      public override void update() { base.update(); }

      private List<double> strikes_;
      private List<double> vols_;

      #region svi
      //! Svi parameters
      private double a_, b_, sigma_, rho_, m_;
      //! Svi interpolation settings
      bool isAFixed_, isBFixed_, isSigmaFixed_, isRhoFixed_, isMFixed_;
      bool vegaWeighted_;
      EndCriteria endCriteria_;
      OptimizationMethod method_;
      SviInterpolation sviInterpolation_;

      public double a() { calculate(); return a_; }
      public double b() { calculate(); return b_; }
      public double sigma() { calculate(); return sigma_; }
      public double rho() { calculate(); return rho_; }
      public double m() { calculate(); return m_; }
      public double rmsError() { calculate(); return sviInterpolation_.rmsError(); }
      public double maxError() { calculate(); return sviInterpolation_.maxError(); }
      public EndCriteria.Type endCriteria() { calculate(); return sviInterpolation_.endCriteria(); }
      #endregion

      #region svi smile section
      protected Handle<Quote> forward_;
      protected Handle<Quote> atmVolatility_;
      protected List<Handle<Quote>> volHandles_;
      protected List<double> actualStrikes_;
      protected bool hasFloatingStrikes_;
      protected double forwardValue_;
      #endregion
   }
}
