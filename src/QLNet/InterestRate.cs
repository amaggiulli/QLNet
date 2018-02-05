/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)

 This file is part of QLNet Project https://github.com/amaggiulli/qlnet

 QLNet is free software: you can redistribute it and/or modify it
 under the terms of the QLNet license.  You should have received a
 copy of the license along with this program; if not, license is
 available at <https://github.com/amaggiulli/QLNet/blob/develop/LICENSE>.

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
   //! Concrete interest rate class
   /*! This class encapsulate the interest rate compounding algebra.
       It manages day-counting conventions, compounding conventions,
       conversion between different conventions, discount/compound factor
       calculations, and implied/equivalent rate calculations.

       \test Converted rates are checked against known good results
   */
   public class InterestRate
   {
      #region Constructors

      //! Default constructor returning a null interest rate.
      public InterestRate()
      {
         r_ = null;
      }

      //! Standard constructor
      public InterestRate(double r, DayCounter dc, Compounding comp, Frequency freq)
      {
         r_ = r;
         dc_ = dc;
         comp_ = comp;
         freqMakesSense_ = false;

         if (comp_ == Compounding.Compounded || comp_ == Compounding.SimpleThenCompounded)
         {
            freqMakesSense_ = true;
            Utils.QL_REQUIRE(freq != Frequency.Once && freq != Frequency.NoFrequency, () => "frequency not allowed for this interest rate");
            freq_ = (double)freq;

         }
      }

      #endregion

      #region Conversions

      public double value() { return rate(); }        // operator redefinition

      #endregion

      #region Inspectors

      public double rate() { return r_.Value; }
      public DayCounter dayCounter() { return dc_; }
      public Compounding compounding() { return comp_; }
      public Frequency frequency() { return freqMakesSense_ ? (Frequency)freq_ : Frequency.NoFrequency; }

      #endregion

      #region discount/compound factor calculations

      //! discount factor implied by the rate compounded at time t.
      /*! \warning Time must be measured using InterestRate's own
                   day counter.
      */
      public double discountFactor(double t) { return 1.0 / compoundFactor(t); }

      //! discount factor implied by the rate compounded between two dates
      public double discountFactor(Date d1, Date d2, Date refStart = null, Date refEnd = null)
      {
         Utils.QL_REQUIRE(d2 >= d1, () => "d1 (" + d1 + ") later than d2 (" + d2 + ")");
         double t = dc_.yearFraction(d1, d2, refStart, refEnd);
         return discountFactor(t);
      }

      //! compound factor implied by the rate compounded at time t.
      /*! returns the compound (a.k.a capitalization) factor
          implied by the rate compounded at time t.

          \warning Time must be measured using InterestRate's own
                   day counter.
      */
      public double compoundFactor(double t)
      {
         Utils.QL_REQUIRE(t >= 0.0, () => "negative time not allowed");
         Utils.QL_REQUIRE(r_ != null, () => "null interest rate");
         switch (comp_)
         {
            case Compounding.Simple:
               return 1.0 + r_.Value * t;
            case Compounding.Compounded:
               return Math.Pow(1.0 + r_.Value / freq_, freq_ * t);
            case Compounding.Continuous:
               return Math.Exp(r_.Value * t);
            case Compounding.SimpleThenCompounded:
               if (t <= 1.0 / freq_)
                  return 1.0 + r_.Value * t;
               else
                  return Math.Pow(1.0 + r_.Value / freq_, freq_ * t);
            default:
               Utils.QL_FAIL("unknown compounding convention");
               return 0;
         }
      }

      //! compound factor implied by the rate compounded between two dates
      /*! returns the compound (a.k.a capitalization) factor
          implied by the rate compounded between two dates.
      */
      public double compoundFactor(Date d1, Date d2, Date refStart = null, Date refEnd = null)
      {
         Utils.QL_REQUIRE(d2 >= d1, () => "d1 (" + d1 + ") later than d2 (" + d2 + ")");
         double t = dc_.yearFraction(d1, d2, refStart, refEnd);
         return compoundFactor(t);
      }

      #endregion

      #region implied rate calculations


      //! implied interest rate for a given compound factor at a given time.
      /*! The resulting InterestRate has the day-counter provided as input.

          \warning Time must be measured using the day-counter provided
                   as input.
      */
      public static InterestRate impliedRate(double compound, DayCounter resultDC, Compounding comp, Frequency freq, double t)
      {
         Utils.QL_REQUIRE(compound > 0.0, () => "positive compound factor required");

         double r = 0;
         if (compound.IsEqual(1.0))
         {
            Utils.QL_REQUIRE(t >= 0.0, () => "non negative time (" + t + ") required");
            r = 0.0;
         }
         else
         {
            Utils.QL_REQUIRE(t > 0.0, () => "positive time (" + t + ") required");
            switch (comp)
            {
               case Compounding.Simple:
                  r = (compound - 1.0) / t;
                  break;
               case Compounding.Compounded:
                  r = (Math.Pow(compound, 1.0 / (((double)freq) * t)) - 1.0) * ((double)freq);
                  break;
               case Compounding.Continuous:
                  r = Math.Log(compound) / t;
                  break;
               case Compounding.SimpleThenCompounded:
                  if (t <= 1.0 / ((double)freq))
                     r = (compound - 1.0) / t;
                  else
                     r = (Math.Pow(compound, 1.0 / (((double)freq) * t)) - 1.0) * ((double)freq);
                  break;
               default:
                  Utils.QL_FAIL("unknown compounding convention (" + comp + ")");
                  break;
            }
         }
         return new InterestRate(r, resultDC, comp, freq);
      }

      //! implied rate for a given compound factor between two dates.
      /*! The resulting rate is calculated taking the required
          day-counting rule into account.
      */
      public static InterestRate impliedRate(double compound, DayCounter resultDC, Compounding comp, Frequency freq, Date d1, Date d2,
                                             Date refStart = null, Date refEnd = null)
      {
         Utils.QL_REQUIRE(d2 >= d1, () => "d1 (" + d1 + ") later than d2 (" + d2 + ")");
         double t = resultDC.yearFraction(d1, d2, refStart, refEnd);
         return impliedRate(compound, resultDC, comp, freq, t);
      }

      #endregion

      #region equivalent rate calculations


      //! equivalent interest rate for a compounding period t.
      /*! The resulting InterestRate shares the same implicit
          day-counting rule of the original InterestRate instance.

          \warning Time must be measured using the InterestRate's
                   own day counter.
      */
      public InterestRate equivalentRate(Compounding comp, Frequency freq, double t)
      {
         return impliedRate(compoundFactor(t), dc_, comp, freq, t);
      }

      //! equivalent rate for a compounding period between two dates
      /*! The resulting rate is calculated taking the required
          day-counting rule into account.
      */
      public InterestRate equivalentRate(DayCounter resultDC, Compounding comp, Frequency freq, Date d1, Date d2,
                                         Date refStart = null, Date refEnd = null)
      {
         Utils.QL_REQUIRE(d2 >= d1, () => "d1 (" + d1 + ") later than d2 (" + d2 + ")");
         double t1 = dc_.yearFraction(d1, d2, refStart, refEnd);
         double t2 = resultDC.yearFraction(d1, d2, refStart, refEnd);
         return impliedRate(compoundFactor(t1), resultDC, comp, freq, t2);
      }

      public override string ToString()
      {
         string result = "";
         if (r_ == null)
            return "null interest rate";

         result += string.Format("{0:0.00%}", rate()) + " " + dayCounter().name() + " ";
         switch (compounding())
         {
            case Compounding.Simple:
               result += "simple compounding";
               break;
            case Compounding.Compounded:
               switch (frequency())
               {
                  case Frequency.NoFrequency:
                  case Frequency.Once:
                     Utils.QL_FAIL(frequency() + " frequency not allowed for this interest rate");
                     break;
                  default:
                     result += frequency() + " compounding";
                     break;
               }
               break;
            case Compounding.Continuous:
               result += "continuous compounding";
               break;
            case Compounding.SimpleThenCompounded:
               switch (frequency())
               {
                  case Frequency.NoFrequency:
                  case Frequency.Once:
                     Utils.QL_FAIL(frequency() + " frequency not allowed for this interest rate");
                     break;
                  default:
                     result += "simple compounding up to "
                               + 12 / (int)frequency() + " months, then "
                               + frequency() + " compounding";
                     break;
               }
               break;
            default:
               Utils.QL_FAIL("unknown compounding convention (" + compounding() + ")");
               break;
         }
         return result;
      }

      #endregion


      private double? r_;
      private DayCounter dc_;
      private Compounding comp_;
      private bool freqMakesSense_;
      private double freq_;
   }

}
