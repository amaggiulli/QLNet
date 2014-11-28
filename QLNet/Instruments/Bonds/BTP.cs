/*
 Copyright (C) 2008-2013  Andrea Maggiulli (a.maggiulli@gmail.com)

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

namespace QLNet
{
   /*! Italian CCTEU (Certificato di credito del tesoro)
        Euribor6M indexed floating rate bond
    
        \ingroup instruments

   */
   public class CCTEU : FloatingRateBond 
   {
      public CCTEU(Date maturityDate,double spread,Handle<YieldTermStructure> fwdCurve = null,
                   Date startDate = null,Date issueDate = null)
         :base(3, 100.0,
                       new Schedule(startDate,
                                maturityDate, new Period(6,TimeUnit.Months),
                                new NullCalendar(), BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                DateGeneration.Rule.Backward, true),
                       new Euribor6M(fwdCurve != null ? fwdCurve : new Handle<YieldTermStructure>() ),
                       new Actual360(),
                       BusinessDayConvention.Following,
                       new Euribor6M().fixingDays(),
                       new List<double>{1.0}, // gearing
                       new List<double>{spread},
                       new List<double>(), // caps
                       new List<double>(), // floors
                       false, // in arrears
                       100.0, // redemption
                       issueDate) {}

      #region Bond interface

      //! accrued amount at a given date
      /*! The default bond settlement is used if no date is given. */
      public override double accruedAmount(Date d = null)
      {
         double result = base.accruedAmount(d);
         return new ClosestRounding(5).Round(result);
      }

      #endregion

   }

   //! Italian BTP (Buono Poliennali del Tesoro) fixed rate bond
   /*! \ingroup instruments

   */
   public class BTP : FixedRateBond 
   {
      public BTP(Date maturityDate,double fixedRate,Date startDate = null,Date issueDate = null)
         :base(3, 100.0,new Schedule(startDate,
                             maturityDate, new Period(6,TimeUnit.Months),
                             new NullCalendar(), BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                             DateGeneration.Rule.Backward, true),
                    new List<double>{fixedRate},
                    new ActualActual(ActualActual.Convention.ISMA),
                    BusinessDayConvention.ModifiedFollowing, 100.0, issueDate, new TARGET()) { }

      /*! constructor needed for legacy non-par redemption BTPs.
          As of today the only remaining one is IT123456789012
          that will redeem 99.999 on xx-may-2037 */
      public BTP(Date maturityDate, double fixedRate, double redemption,Date startDate = null,Date issueDate = null)
      :base(3, 100.0, new Schedule(startDate,
                             maturityDate, new Period(6,TimeUnit.Months),
                             new NullCalendar(), BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                             DateGeneration.Rule.Backward, true),
                    new List<double>{fixedRate},
                    new ActualActual(ActualActual.Convention.ISMA),
                    BusinessDayConvention.ModifiedFollowing, redemption, issueDate, new TARGET()) { }
      #region Bond interface
 
      //! accrued amount at a given date
      /*! The default bond settlement is used if no date is given. */
      public override double accruedAmount(Date d = null)
      {
         double result = base.accruedAmount(d);
         return new ClosestRounding(5).Round(result);
      }
      
      #endregion

      //! BTP yield given a (clean) price and settlement date
      /*! The default BTP conventions are used: Actual/Actual (ISMA),
          Compounded, Annual.
          The default bond settlement is used if no date is given. */
      public double yield(double cleanPrice, Date settlementDate = null, double accuracy = 1.0e-8, int maxEvaluations = 100)
      {
         return base.yield(cleanPrice, new ActualActual(ActualActual.Convention.ISMA),
                           Compounding.Compounded, Frequency.Annual,settlementDate, accuracy, maxEvaluations);
      }
   }

   public class RendistatoBasket :  IObserver,IObservable 
   {

      public RendistatoBasket(List<BTP> btps, List<double> outstandings, List<Handle<Quote>> cleanPriceQuotes)
      {
         btps_ = btps;
         outstandings_ = outstandings;
         quotes_ = cleanPriceQuotes;

         Utils.QL_REQUIRE( !btps_.empty(), () => "empty RendistatoCalculator Basket" );
         int k = btps_.Count;

         Utils.QL_REQUIRE( outstandings_.Count == k, () =>
                   "mismatch between number of BTPs (" + k +
                   ") and number of outstandings (" +
                   outstandings_.Count + ")");
         Utils.QL_REQUIRE( quotes_.Count == k, () =>
                   "mismatch between number of BTPs (" + k +
                   ") and number of clean prices quotes (" +
                   quotes_.Count + ")");

         // require non-negative outstanding
         for (int i=0; i<k; ++i) 
         {
            Utils.QL_REQUIRE( outstandings[i] >= 0, () =>
                       "negative outstanding for " + i +
                       " bond, maturity " + btps[i].maturityDate());
            // add check for prices ??
         }

         // TODO: filter out expired bonds, zero outstanding bond, etc

         Utils.QL_REQUIRE( !btps_.empty(), () => "invalid bonds only in RendistatoCalculator Basket" );
         n_ = btps_.Count;

         outstanding_ = 0.0;
         for (int i=0; i<n_; ++i)
            outstanding_ += outstandings[i];

         weights_ = new List<double>(n_);
         for (int i=0; i<n_; ++i) 
         {
            weights_.Add(outstandings[i]/outstanding_);
            quotes_[i].registerWith(update);
        }

      }
      #region Inspectors
      
      public int size() { return n_;}
      public List<BTP> btps() {return btps_;}
      public List<Handle<Quote>> cleanPriceQuotes() { return quotes_;}
      public List<double> outstandings() { return outstandings_;}
      public List<double> weights()  { return weights_;}
      public double outstanding()  { return outstanding_;}

      #endregion

      #region Observer & observable
      public event Callback notifyObserversEvent;
      public void registerWith(Callback handler) { notifyObserversEvent += handler; }
      public void unregisterWith(Callback handler) { notifyObserversEvent -= handler; }
      protected void notifyObservers()
      {
         Callback handler = notifyObserversEvent;
         if (handler != null)
         {
            handler();
         }
      }

      // observer interface
      public void update() { notifyObservers(); }
      #endregion

      
      private   List<BTP> btps_;
      private   List<double> outstandings_;
      private   List<Handle<Quote> > quotes_;
      private   double outstanding_;
      private   int n_;
      private   List<double> weights_;
    }

   public class RendistatoCalculator : LazyObject 
   {
      public RendistatoCalculator(RendistatoBasket basket, Euribor euriborIndex, Handle<YieldTermStructure> discountCurve)
      {
         basket_ = basket;
         euriborIndex_ = euriborIndex;
         discountCurve_ = discountCurve;
         yields_ = new InitializedList<double>(basket_.size(), 0.05); 
         durations_ = new List<double>(basket_.size());
         nSwaps_ = 15;  // TODO: generalize number of swaps and their lenghts
         swaps_ = new List<VanillaSwap>(nSwaps_);
         swapLenghts_ = new List<double>(nSwaps_);
         swapBondDurations_ = new InitializedList<double?>(nSwaps_, null);
         swapBondYields_ = new InitializedList<double?>(nSwaps_, 0.05);
         swapRates_ = new InitializedList<double?>(nSwaps_, null);

         basket_.registerWith(update);
         euriborIndex_.registerWith(update);
         discountCurve_.registerWith(update);

         double dummyRate = 0.05;
         for (int i=0; i<nSwaps_; ++i) 
         {
            swapLenghts_[i] = (i+1);
            swaps_[i] = new MakeVanillaSwap( new Period((int)swapLenghts_[i],TimeUnit.Years), 
                                             euriborIndex_, dummyRate, new Period(1,TimeUnit.Days))
                                             .withDiscountingTermStructure(discountCurve_);
         }
      }
      
      #region Calculations
      
      public double yield()
      {
         double inner_product = 0;
         basket_.weights().ForEach((ii, vv) => inner_product += vv * yields()[ii]);
         return inner_product;
      }
      public double duration()
      {
         calculate();
         return duration_;
      }
      // bonds
      public List<double> yields()
      {
         calculate();
         return yields_;
      }
      public List<double> durations()
      {
         calculate();
         return durations_;
      }
      // swaps
      public List<double> swapLengths() { return swapLenghts_; }
      public InitializedList<double?> swapRates()
      {
         calculate();
         return swapRates_;
      }
      public InitializedList<double?> swapYields()
      {
         calculate();
         return swapBondYields_;
      }
      public InitializedList<double?> swapDurations()
      {
         calculate();
         return swapBondDurations_;
      }
      #endregion

      #region Equivalent Swap proxy
      
      public VanillaSwap equivalentSwap()
      {
         calculate();
         return swaps_[equivalentSwapIndex_];
      }
      public double equivalentSwapRate()
      {
         calculate();
         return swapRates_[equivalentSwapIndex_].Value;
      }
      public double equivalentSwapYield()
      {
         calculate();
         return swapBondYields_[equivalentSwapIndex_].Value;
      }
      public double equivalentSwapDuration()
      {
         calculate();
         return swapBondDurations_[equivalentSwapIndex_].Value;
      }
      public double equivalentSwapLength()
      {
         calculate();
         return swapLenghts_[equivalentSwapIndex_];
      }
      public double equivalentSwapSpread()
      {
         return yield() - equivalentSwapRate();
      }
      
      #endregion

      #region LazyObject interface
      
      protected override void performCalculations()
      {
         List<BTP> btps = basket_.btps();
         List<Handle<Quote> > quotes = basket_.cleanPriceQuotes();
         Date bondSettlementDate = btps[0].settlementDate();
         for (int i=0; i<basket_.size(); ++i) 
         {
            yields_[i] = BondFunctions.yield(btps[i], quotes[i].link.value(),
                                             new ActualActual(ActualActual.Convention.ISMA), 
                                             Compounding.Compounded, Frequency.Annual,
                                             bondSettlementDate,
                                             // accuracy, maxIterations, guess
                                             1.0e-10, 100, yields_[i]);

            durations_[i] = BondFunctions.duration(btps[i], yields_[i],new ActualActual(ActualActual.Convention.ISMA), 
                                                   Compounding.Compounded, Frequency.Annual,Duration.Type.Modified, 
                                                   bondSettlementDate);
         }

         duration_ = 0;
         basket_.weights().ForEach((ii, vv) => duration_ += vv * yields()[ii]);


         //duration_ = std::inner_product(basket_->weights().begin(),
         //                              basket_->weights().end(),
         //                              durations_.begin(), 0.0);

         int settlDays = 3;
         DayCounter fixedDayCount = swaps_[0].fixedDayCount();
         equivalentSwapIndex_ = nSwaps_-1;
         swapRates_[0]= swaps_[0].fairRate();
         FixedRateBond swapBond = new FixedRateBond(settlDays,
                                                    100.0,      // faceAmount
                                                    swaps_[0].fixedSchedule(),
                                                    new List<double>() { swapRates_[0].Value },
                                                    fixedDayCount,
                                                    BusinessDayConvention.Following, // paymentConvention
                                                    100.0);    // redemption
         swapBondYields_[0] = BondFunctions.yield(swapBond,
                                                  100.0, // floating leg NPV including end payment
                                                  new ActualActual(ActualActual.Convention.ISMA), 
                                                  Compounding.Compounded, Frequency.Annual,
                                                  bondSettlementDate,
                                                  // accuracy, maxIterations, guess
                                                  1.0e-10, 100, swapBondYields_[0].Value);

         swapBondDurations_[0] = BondFunctions.duration(swapBond, swapBondYields_[0].Value,
                                                        new ActualActual(ActualActual.Convention.ISMA), 
                                                        Compounding.Compounded, Frequency.Annual,
                                                        Duration.Type.Modified, bondSettlementDate);
         for (int i=1; i<nSwaps_; ++i) 
         {
            swapRates_[i]= swaps_[i].fairRate();
            FixedRateBond swapBond2 = new FixedRateBond(settlDays,
                                                       100.0,      // faceAmount
                                                       swaps_[i].fixedSchedule(),
                                                       new List<double>(){swapRates_[i].Value},
                                                       fixedDayCount,
                                                       BusinessDayConvention.Following, // paymentConvention
                                                       100.0);    // redemption

            swapBondYields_[i] = BondFunctions.yield(swapBond2, 100.0, // floating leg NPV including end payment
                                                     new ActualActual(ActualActual.Convention.ISMA), 
                                                     Compounding.Compounded, Frequency.Annual,
                                                     bondSettlementDate,
                                                     // accuracy, maxIterations, guess
                                                     1.0e-10, 100, swapBondYields_[i].Value);
            
            swapBondDurations_[i] = BondFunctions.duration(swapBond2, swapBondYields_[i].Value,
                                                           new ActualActual(ActualActual.Convention.ISMA), 
                                                           Compounding.Compounded, Frequency.Annual,
                                                           Duration.Type.Modified, bondSettlementDate);
            if (swapBondDurations_[i] > duration_) 
            {
                equivalentSwapIndex_ = i-1;
                break; // exit the loop
            }
        }

        return;
      }
      
      #endregion

      private RendistatoBasket basket_;
      private Euribor euriborIndex_;
      private Handle<YieldTermStructure> discountCurve_;

      private InitializedList<double> yields_;
      private List<double> durations_;
      private double duration_;
      private int equivalentSwapIndex_;

      private int nSwaps_;
      private List<VanillaSwap> swaps_;
      private List<double> swapLenghts_;
      private InitializedList<double?> swapBondDurations_;
      private InitializedList<double?> swapBondYields_, swapRates_;
    }

   //! RendistatoCalculator equivalent swap lenth Quote adapter
   public class RendistatoEquivalentSwapLengthQuote : Quote 
   {
      public RendistatoEquivalentSwapLengthQuote(RendistatoCalculator r) { r_ = r; }
      public override double value() { return r_.equivalentSwapLength(); }
      public override bool isValid()
      {
         try 
         {
            value();
            return true;
         } 
         catch (Exception) 
         {
            return false;
         }
      }
   
      private RendistatoCalculator r_;
   }

   //! RendistatoCalculator equivalent swap spread Quote adapter
   public class RendistatoEquivalentSwapSpreadQuote : Quote 
   {
      public RendistatoEquivalentSwapSpreadQuote(RendistatoCalculator r) { r_ = r; }
      public override double value() { return r_.equivalentSwapSpread(); }
      public override bool isValid()
      {
         try
         {
            value();
            return true;
         }
         catch (Exception)
         {
            return false;
         }
      }

      private RendistatoCalculator r_;
   }

}
