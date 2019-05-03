/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)

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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
#if NET452
using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
using Xunit;
#endif
using QLNet;

namespace TestSuite
{
   public class Flag : IObserver
   {
      private bool up_;

      public Flag()
      {
         up_ = false;
      }

      public void raise() { up_ = true; }
      public void lower() { up_ = false; }
      public bool isUp() { return up_; }
      public void update() { raise(); }
   }

   public static class Utilities
   {
      public static YieldTermStructure flatRate(Date today, double forward, DayCounter dc)
      {
         return new FlatForward(today, new SimpleQuote(forward), dc);
      }

      public static YieldTermStructure flatRate(Date today, Quote forward, DayCounter dc)
      {
         return new FlatForward(today, forward, dc);
      }

      //philippe2009_17
      public static YieldTermStructure flatRate(double forward, DayCounter dc)
      {
         return flatRate(new SimpleQuote(forward), dc);
      }

      public static YieldTermStructure flatRate(Quote forward, DayCounter dc)
      {
         return new FlatForward(0, new NullCalendar(), forward, dc);
      }

      public static BlackVolTermStructure flatVol(Date today, double vol, DayCounter dc)
      {
         return flatVol(today, new SimpleQuote(vol), dc);
      }

      public static BlackVolTermStructure flatVol(Date today, Quote vol, DayCounter dc)
      {
         return new BlackConstantVol(today, new NullCalendar(), new Handle<Quote>(vol), dc);
      }
      //philippe2009_17
      public static BlackVolTermStructure flatVol(Quote vol, DayCounter dc)
      {
         return new BlackConstantVol(0, new NullCalendar(), new Handle<Quote>(vol), dc);
      }

      public static BlackVolTermStructure flatVol(double vol, DayCounter dc)
      {
         return flatVol(new SimpleQuote(vol), dc);
      }

      public static double norm(Vector v, int size, double h)
      {
         // squared values
         List<double> f2 = new InitializedList<double>(size);

         for (int i = 0; i < v.Count; i++)
            f2[i] = v[i] * v[i];

         // numeric integral of f^2
         double I = h * (f2.Sum() - 0.5 * f2.First() - 0.5 * f2.Last());
         return Math.Sqrt(I);
      }

      public static double relativeError(double x1, double x2, double reference)
      {
         if (reference.IsNotEqual(0.0))
            return Math.Abs(x1 - x2) / reference;
         else
            // fall back to absolute error
            return Math.Abs(x1 - x2);
      }

      public static String exerciseTypeToString(Exercise h)
      {
         object hd = null;

         hd = h as EuropeanExercise;
         if (hd != null)
            return "European";

         hd = h as AmericanExercise;
         if (hd != null)
            return "American";

         hd = h as BermudanExercise;
         if (hd != null)
            return "Bermudan";

         Utils.QL_FAIL("unknown exercise type");
         return String.Empty;
      }

      public static String payoffTypeToString(Payoff h)
      {
         object  hd = null;
         hd = h as PlainVanillaPayoff;
         if (hd != null)
            return "plain-vanilla";
         hd = h as CashOrNothingPayoff;
         if (hd != null)
            return "cash-or-nothing";
         hd = h as AssetOrNothingPayoff;
         if (hd != null)
            return "asset-or-nothing";
         hd = h as SuperSharePayoff;
         if (hd != null)
            return "super-share";
         hd = h as SuperFundPayoff;
         if (hd != null)
            return "super-fund";
         hd = h as PercentageStrikePayoff;
         if (hd != null)
            return"percentage-strike";
         hd = h as GapPayoff;
         if (hd != null)
            return "gap";
         hd = h as FloatingTypePayoff;
         if (hd != null)
            return "floating-type";

         Utils.QL_FAIL("unknown payoff type");
         return String.Empty;
      }
   }

   // this cleans up index-fixing histories when disposed
   public class IndexHistoryCleaner : IDisposable
   {
      public void Dispose() { IndexManager.instance().clearHistories(); }
   }

   public static partial class QAssert
   {
      public static void CollectionAreEqual(ICollection expected, ICollection actual)
      {
#if NET452
         CollectionAssert.AreEqual(expected, actual);
#else
         Assert.AreEqual(expected, actual);
#endif
      }
      public static void CollectionAreNotEqual(ICollection notExpected, ICollection actual)
      {
#if NET452
         CollectionAssert.AreNotEqual(notExpected, actual);
#else
         Assert.AreNotEqual(notExpected, actual);
#endif
      }

      public static void AreNotSame(object notExpected, object actual)
      {
#if NET452
         Assert.AreNotSame(notExpected, actual);
#else
         Assert.NotSame(notExpected, actual);
#endif
      }

      public static void Fail(string message)
      {
#if NET452
         Assert.Fail(message);
#else
         Assert.True(false, message);
#endif
      }

      public static void AreEqual(double expected, double actual, double delta)
      {
#if NET452
         Assert.AreEqual(expected, actual, delta);
#else
         Assert.True(Math.Abs(expected - actual) <= delta);
#endif
      }

      public static void AreEqual(double expected, double actual, double delta, string message)
      {
#if NET452
         Assert.AreEqual(expected, actual, delta, message);
#else
         Assert.True(Math.Abs(expected - actual) <= delta, message);
#endif
      }

      public static void AreEqual<T>(T expected, T actual)
      {

#if NET452
         Assert.AreEqual(expected, actual);
#else
         Assert.Equal(expected, actual);
#endif
      }

      public static void AreEqual<T>(T expected, T actual, string message)
      {
#if NET452
         Assert.AreEqual(expected, actual, message);
#else
         Assert.Equal(expected, actual);
#endif
      }

      public static void AreNotEqual<T>(T expected, T actual)
      {

#if NET452
         Assert.AreNotEqual(expected, actual);
#else
         Assert.NotEqual(expected, actual);
#endif
      }

      public static void IsTrue(bool condition)
      {
#if NET452
         Assert.IsTrue(condition);
#else
         Assert.True(condition);
#endif
      }

      public static void IsTrue(bool condition, string message)
      {
#if NET452
         Assert.IsTrue(condition, message);
#else
         Assert.True(condition, message);
#endif
      }

      public static void IsFalse(bool condition)
      {
#if NET452
         Assert.IsFalse(condition);
#else
         Assert.False(condition);
#endif
      }

      public static void IsFalse(bool condition, string message)
      {
#if NET452
         Assert.IsFalse(condition, message);
#else
         Assert.False(condition, message);
#endif
      }

      /// <summary>
      /// Verifies that an object reference is not null.
      /// </summary>
      /// <param name="obj">The object to be validated</param>
      public static void Require(object obj)
      {
#if NET452
         Assert.IsNotNull(obj);
#else
         Assert.NotNull(obj);
#endif
      }

      /// <summary>
      /// Verifies an Action throw the specified Exception
      /// </summary>
      /// <typeparam name="T">The Exception</typeparam>
      /// <param name="action">The Action</param>
      public static void ThrowsException<T>(Action action) where T: SystemException
      {
#if NET452
         Assert.ThrowsException<T>(action);
#else
         Assert.ThrowsException<T>(action);
#endif
      }

   }

   public struct SwaptionTenors
   {
      public List<Period> options;
      public List<Period> swaps;
   }
   public struct SwaptionMarketConventions
   {
      public Calendar calendar;
      public BusinessDayConvention optionBdc;
      public DayCounter dayCounter;
      public void setConventions()
      {
         calendar = new TARGET();
         optionBdc = BusinessDayConvention.ModifiedFollowing;
         dayCounter = new Actual365Fixed();
      }
   }

   public struct AtmVolatility
   {
      public SwaptionTenors tenors;
      public Matrix vols;
      public List<List<Handle<Quote> > > volsHandle;
      public void setMarketData()
      {
         tenors.options = new InitializedList<Period>(6);
         tenors.options[0] = new Period(1, TimeUnit.Months);
         tenors.options[1] = new Period(6, TimeUnit.Months);
         tenors.options[2] = new Period(1, TimeUnit.Years);
         tenors.options[3] = new Period(5, TimeUnit.Years);
         tenors.options[4] = new Period(10, TimeUnit.Years);
         tenors.options[5] = new Period(30, TimeUnit.Years);
         tenors.swaps = new InitializedList<Period>(4);
         tenors.swaps[0] = new Period(1, TimeUnit.Years);
         tenors.swaps[1] = new Period(5, TimeUnit.Years);
         tenors.swaps[2] = new Period(10, TimeUnit.Years);
         tenors.swaps[3] = new Period(30, TimeUnit.Years);
         vols = new Matrix(tenors.options.Count, tenors.swaps.Count);
         vols[0, 0] = 0.1300; vols[0, 1] = 0.1560; vols[0, 2] = 0.1390; vols[0, 3] = 0.1220;
         vols[1, 0] = 0.1440; vols[1, 1] = 0.1580; vols[1, 2] = 0.1460; vols[1, 3] = 0.1260;
         vols[2, 0] = 0.1600; vols[2, 1] = 0.1590; vols[2, 2] = 0.1470; vols[2, 3] = 0.1290;
         vols[3, 0] = 0.1640; vols[3, 1] = 0.1470; vols[3, 2] = 0.1370; vols[3, 3] = 0.1220;
         vols[4, 0] = 0.1400; vols[4, 1] = 0.1300; vols[4, 2] = 0.1250; vols[4, 3] = 0.1100;
         vols[5, 0] = 0.1130; vols[5, 1] = 0.1090; vols[5, 2] = 0.1070; vols[5, 3] = 0.0930;
         volsHandle = new InitializedList<List<Handle<Quote>>>(tenors.options.Count);
         for (int i = 0; i < tenors.options.Count; i++)
         {
            volsHandle[i] = new InitializedList<Handle<Quote>>(tenors.swaps.Count);
            for (int j = 0; j < tenors.swaps.Count; j++)
               // every handle must be reassigned, as the ones created by
               // default are all linked together.
               volsHandle[i][j] = new Handle<Quote>(new SimpleQuote(vols[i, j]));
         }
      }
   }

   public struct VolatilityCube
   {
      public SwaptionTenors tenors;
      public Matrix volSpreads;
      public List<List<Handle<Quote> > > volSpreadsHandle;
      public List<double> strikeSpreads;
      public void setMarketData()
      {
         tenors.options = new InitializedList<Period>(3);
         tenors.options[0] = new Period(1, TimeUnit.Years);
         tenors.options[1] = new Period(10, TimeUnit.Years);
         tenors.options[2] = new Period(30, TimeUnit.Years);
         tenors.swaps = new InitializedList<Period>(3);
         tenors.swaps[0] = new Period(2, TimeUnit.Years);
         tenors.swaps[1] = new Period(10, TimeUnit.Years);
         tenors.swaps[2] = new Period(30, TimeUnit.Years);
         strikeSpreads = new InitializedList<double>(5);
         strikeSpreads[0] = -0.020;
         strikeSpreads[1] = -0.005;
         strikeSpreads[2] = +0.000;
         strikeSpreads[3] = +0.005;
         strikeSpreads[4] = +0.020;
         volSpreads = new Matrix(tenors.options.Count * tenors.swaps.Count, strikeSpreads.Count);
         volSpreads[0, 0] = 0.0599; volSpreads[0, 1] = 0.0049;
         volSpreads[0, 2] = 0.0000;
         volSpreads[0, 3] = -0.0001; volSpreads[0, 4] = 0.0127;
         volSpreads[1, 0] = 0.0729; volSpreads[1, 1] = 0.0086;
         volSpreads[1, 2] = 0.0000;
         volSpreads[1, 3] = -0.0024; volSpreads[1, 4] = 0.0098;
         volSpreads[2, 0] = 0.0738; volSpreads[2, 1] = 0.0102;
         volSpreads[2, 2] = 0.0000;
         volSpreads[2, 3] = -0.0039; volSpreads[2, 4] = 0.0065;
         volSpreads[3, 0] = 0.0465; volSpreads[3, 1] = 0.0063;
         volSpreads[3, 2] = 0.0000;
         volSpreads[3, 3] = -0.0032; volSpreads[3, 4] = -0.0010;
         volSpreads[4, 0] = 0.0558; volSpreads[4, 1] = 0.0084;
         volSpreads[4, 2] = 0.0000;
         volSpreads[4, 3] = -0.0050; volSpreads[4, 4] = -0.0057;
         volSpreads[5, 0] = 0.0576; volSpreads[5, 1] = 0.0083;
         volSpreads[5, 2] = 0.0000;
         volSpreads[5, 3] = -0.0043; volSpreads[5, 4] = -0.0014;
         volSpreads[6, 0] = 0.0437; volSpreads[6, 1] = 0.0059;
         volSpreads[6, 2] = 0.0000;
         volSpreads[6, 3] = -0.0030; volSpreads[6, 4] = -0.0006;
         volSpreads[7, 0] = 0.0533; volSpreads[7, 1] = 0.0078;
         volSpreads[7, 2] = 0.0000;
         volSpreads[7, 3] = -0.0045; volSpreads[7, 4] = -0.0046;
         volSpreads[8, 0] = 0.0545; volSpreads[8, 1] = 0.0079;
         volSpreads[8, 2] = 0.0000;
         volSpreads[8, 3] = -0.0042; volSpreads[8, 4] = -0.0020;
         volSpreadsHandle = new InitializedList<List<Handle<Quote>>>(tenors.options.Count * tenors.swaps.Count);
         for (int i = 0; i < tenors.options.Count * tenors.swaps.Count; i++)
         {
            volSpreadsHandle[i] = new InitializedList<Handle<Quote>>(strikeSpreads.Count);
            for (int j = 0; j < strikeSpreads.Count; j++)
            {
               // every handle must be reassigned, as the ones created by
               // default are all linked together.
               volSpreadsHandle[i][j] = new Handle<Quote>(new SimpleQuote(volSpreads[i, j]));
            }
         }
      }
   }
}
