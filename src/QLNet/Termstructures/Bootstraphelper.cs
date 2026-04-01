/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2016  Andrea Maggiulli (a.maggiulli@gmail.com)

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
   public struct Pillar
   {
       /// <summary>
       /// Specifies the alternatives for determining the pillar date.
       /// </summary>
       public enum Choice
       {
          /// <summary>
          /// Uses the instrument maturity date.
          /// </summary>
          MaturityDate,

          /// <summary>
          /// Uses the last date relevant for instrument pricing.
          /// </summary>
          LastRelevantDate,

          /// <summary>
          /// Uses a custom date.
          /// </summary>
          CustomDate
       }
   }
   // Base helper class for bootstrapping
   /* This class provides an abstraction for the instruments used to bootstrap a term structure.
      It is advised that a bootstrap helper for an instrument contains an instance of the actual instrument
    * class to ensure consistancy between the algorithms used during bootstrapping
      and later instrument pricing. This is not yet fully enforced in the available rate helpers. */
   public class BootstrapHelper<TS> : IObservable, IObserver
      where TS : class
   {
      protected Handle<Quote> quote_;
      protected TS termStructure_;
      protected Date earliestDate_, latestDate_;
      protected Date maturityDate_, latestRelevantDate_, pillarDate_;

      public BootstrapHelper() { } // required for generics

      public BootstrapHelper(Handle<Quote> quote)
      {
         quote_ = quote;
         quote_.registerWith(update);
      }
      public BootstrapHelper(double quote)
      {
         quote_ = new Handle<Quote>(new SimpleQuote(quote));
      }


      /// <summary>
      /// Returns the quote handle associated with the bootstrap helper.
      /// </summary>
      public Handle<Quote> quote() { return quote_; }
      public double quoteError() { return quote_.link.value() - impliedQuote(); }
      public double quoteValue() { return quote_.link.value(); }
      public bool quoteIsValid() { return quote_.link.isValid(); }
      public virtual double impliedQuote() { throw new NotSupportedException(); }


      /// <summary>
      /// Sets the term structure to be used for pricing.
      /// </summary>
      /// <remarks>
      /// The caller is responsible for keeping the term structure alive for the whole life of the helper. This method is normally called from inside the term structure being bootstrapped, passing <c>this</c>.
      /// </remarks>
      public virtual void setTermStructure(TS ts)
      {
         if (ts == null)
            throw new ArgumentException("null term structure given");
         termStructure_ = ts;
      }

      // earliest relevant date
      // The earliest date at which discounts are needed by the helper in order to provide a quote.
      public virtual Date earliestDate() { return earliestDate_; }

      /// <summary>
      /// Returns the instrument maturity date.
      /// </summary>
      public virtual Date maturityDate()
      {
         if (maturityDate_ == null)
            return latestRelevantDate();
         return maturityDate_;
      }

      /// <summary>
      /// Returns the latest relevant date.
      /// </summary>
      /// <remarks>
      /// This is the latest date for which data are needed by the helper to provide a quote, and it does not necessarily match the maturity of the underlying instrument.
      /// </remarks>
      public virtual Date latestRelevantDate()
      {
         if (latestRelevantDate_ == null)
            return latestDate();
         return latestRelevantDate_;
      }

      /// <summary>
      /// Returns the pillar date.
      /// </summary>
      public virtual Date pillarDate()
      {
         if (pillarDate_ == null)
            return latestDate();
         return pillarDate_;
      }

      // latest relevant date
      /* The latest date at which discounts are needed by the helper in order to provide a quote.
       * It does not necessarily equal the maturity of the underlying instrument. */
      public virtual Date latestDate()
      {
         if (latestDate_ == null)
            return pillarDate_;
         return latestDate_;
      }


      #region observer interface
      private readonly WeakEventSource eventSource = new WeakEventSource();
      public event Callback notifyObserversEvent
      {
         add
         {
            eventSource.Subscribe(value);
         }
         remove
         {
            eventSource.Unsubscribe(value);
         }
      }

      public void registerWith(Callback handler) { notifyObserversEvent += handler; }
      public void unregisterWith(Callback handler) { notifyObserversEvent -= handler; }
      protected void notifyObservers()
      {
         eventSource.Raise();
      }

      public virtual void update() { notifyObservers(); }
      #endregion
   }

   public class RateHelper : BootstrapHelper<YieldTermStructure>
   {
      public RateHelper() : base() { } // required for generics
      public RateHelper(Handle<Quote> quote) : base(quote) {}
      public RateHelper(double quote) : base(quote) {}
   }
}
