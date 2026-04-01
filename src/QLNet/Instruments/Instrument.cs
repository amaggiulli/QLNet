/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2013 Andrea Maggiulli (a.maggiulli@gmail.com)

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
using System.Collections.Generic;

namespace QLNet
{
   // Abstract instrument class. It defines the interface of concrete instruments
   public class Instrument : LazyObject
   {
      // The value of these attributes and any other that derived classes might declare must be set during calculation.
      protected double? NPV_, errorEstimate_, CASH_;
      protected Dictionary<string, object> additionalResults_ = new Dictionary<string, object>();
      protected IPricingEngine engine_;
      protected Date valuationDate_ = null;

      /// <summary>
      /// Sets the pricing engine to be used.
      /// </summary>
      /// <remarks>
      /// Calling this method has no effect if <c>performCalculations</c> is overridden in a derived class.
      /// </remarks>
      public void setPricingEngine(IPricingEngine e)
      {
         if (engine_ != null)
            engine_.unregisterWith(update);
         engine_ = e;
         if (engine_ != null)
            engine_.registerWith(update);

         update();       // trigger (lazy) recalculation and notify observers
      }


      /// <summary>
      /// Populates the pricing-engine arguments for the instrument.
      /// </summary>
      /// <remarks>
      /// Override this when a derived argument structure is defined. This is mandatory when a pricing engine is used.
      /// </remarks>
      public virtual void setupArguments(IPricingEngineArguments a) { throw new NotImplementedException(); }


      #region Lazy object interface
      protected override void calculate()
      {
         if (isExpired())
         {
            setupExpired();
            calculated_ = true;
         }
         else
         {
            base.calculate();
         }
      }

      /* In case a pricing engine is not used, this method must be overridden to perform the actual
         calculations and set any needed results.
       * In case a pricing engine is used, the default implementation can be used. */
      protected override void performCalculations()
      {
         if (engine_ == null)
            throw new ArgumentException("null pricing engine");
         engine_.reset();
         setupArguments(engine_.getArguments());
         engine_.getArguments().validate();
         engine_.calculate();
         fetchResults(engine_.getResults());
      }
      #endregion

      #region Results
      /// <summary>
      /// Reads the pricing-engine results for the instrument.
      /// </summary>
      /// <remarks>
      /// Override this when a derived result structure is defined. This is mandatory when a pricing engine is used.
      /// </remarks>
      public virtual void fetchResults(IPricingEngineResults r)
      {
         Instrument.Results results = r as Instrument.Results;
         if (results == null)
            throw new ArgumentException("no results returned from pricing engine");
         NPV_ = results.value;
         CASH_ = results.cash;
         errorEstimate_ = results.errorEstimate;
         valuationDate_ = results.valuationDate;
         additionalResults_ = new Dictionary<string, object>(results.additionalResults);
      }

      public double NPV()
      {
         calculate();
         if (NPV_ == null)
            throw new ArgumentException("NPV not provided");
         return NPV_.GetValueOrDefault();
      }

      public double CASH()
      {
         calculate();
         if (CASH_ == null)
            throw new ArgumentException("CASH not provided");
         return CASH_.GetValueOrDefault();
      }

      public double errorEstimate()
      {
         calculate();
         if (errorEstimate_ == null)
            throw new ArgumentException("error estimate not provided");
         return errorEstimate_.GetValueOrDefault();
      }
      /// <summary>
      /// Returns the date to which the net present value refers.
      /// </summary>
      public Date valuationDate()
      {
         calculate();
         Utils.QL_REQUIRE(valuationDate_ != null, () => "valuation date not provided");
         return valuationDate_;
      }

      // returns any additional result returned by the pricing engine.
      public object result(string tag)
      {
         calculate();
         try
         {
            return additionalResults_[tag];
         }
         catch (KeyNotFoundException)
         {
            throw new ArgumentException(tag + " not provided");
         }
      }

      // returns all additional result returned by the pricing engine.
      public Dictionary<string, object> additionalResults() { return additionalResults_; }
      #endregion

      // This method must leave the instrument in a consistent state when the expiration condition is met.
      protected virtual void setupExpired()
      {
         NPV_ = errorEstimate_ = CASH_ = 0.0;
         valuationDate_ = null;
         additionalResults_.Clear();
      }

      /// <summary>
      /// Returns whether the instrument is still tradable.
      /// </summary>
      public virtual bool isExpired() { throw new NotSupportedException(); }


      public class Results : IPricingEngineResults
      {
         public Results()
         {
            additionalResults = new Dictionary<string, object>();
         }
         public double? value { get; set; }
         public double? errorEstimate { get; set; }
         public double? cash { get; set; }
         public Date valuationDate { get; set; }

         public Dictionary<string, object> additionalResults { get; set; }

         public virtual void reset()
         {
            value = errorEstimate = cash = null;
            additionalResults.Clear();
            valuationDate = null;
         }

      }

   }
}
