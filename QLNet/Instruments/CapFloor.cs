/*
 Copyright (C) 2008-2013 Andrea Maggiulli (a.maggiulli@gmail.com)
  
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
   /// <summary>
   /// Base class for cap-like instruments
   /// \ingroup instruments
   /// \test
   /// - the correctness of the returned value is tested by checking
   ///   that the price of a cap (resp. floor) decreases
   ///   (resp. increases) with the strike rate.
   /// - the relationship between the values of caps, floors and the
   ///   resulting collars is checked.
   /// - the put-call parity between the values of caps, floors and
   ///   swaps is checked.
   /// - the correctness of the returned implied volatility is tested
   ///   by using it for reproducing the target value.
   /// - the correctness of the returned value is tested by checking
   ///   it against a known good value.
   /// </summary>
   public class CapFloor : Instrument
   {
      #region Private Attributes
      
      private CapFloorType type_;
      private List<CashFlow> floatingLeg_;
      private List<double> capRates_;
      private List<double> floorRates_;

      #endregion

      #region Constructors

      public CapFloor(CapFloorType type, List<CashFlow> floatingLeg, List<double> capRates, List<double> floorRates)
      {

         type_ = type;
         floatingLeg_ = new List<CashFlow>(floatingLeg);
         capRates_ = new List<double>(capRates);
         floorRates_ = new List<double>(floorRates); 

         if (type_ == CapFloorType.Cap || type_ == CapFloorType.Collar) 
         {
            if (capRates_.Count == 0 )
               throw new ArgumentException("no cap rates given");

            while (capRates_.Count < floatingLeg_.Count)
                capRates_.Add(capRates_.Last());
         }
         if (type_ == CapFloorType.Floor || type_ == CapFloorType.Collar) 
         {
            if (floorRates_.Count == 0)
               throw new ArgumentException("no floor rates given");

            while (floorRates_.Count < floatingLeg_.Count)
               floorRates_.Add(floorRates_.Last());
         }

         for (int i = 0; i < floatingLeg_.Count; i++)
            floatingLeg_[i].registerWith(update);

         Settings.registerWith(update);

      }
      public CapFloor(CapFloorType type,List<CashFlow> floatingLeg,List<double> strikes)
      {

         type_ = type;
         floatingLeg_ = new List<CashFlow>(floatingLeg);
 
         if ( strikes.Count == 0 )
            throw new ArgumentException("no strikes given");

         if (type_ == CapFloorType.Cap) 
         {
            capRates_ = new List<double>(strikes);

            while (capRates_.Count < floatingLeg_.Count)
               capRates_.Add(capRates_.Last());

         } 
         else if (type_ == CapFloorType.Floor) 
         {
            floorRates_ = new List<double>(strikes);

            while (floorRates_.Count < floorRates_.Count)
               floorRates_.Add(floorRates_.Last());
         } 
         else
            throw new ArgumentException("only Cap/Floor types allowed in this constructor");


         for (int i = 0; i < floatingLeg_.Count; i++)
            floatingLeg_[i].registerWith(update);

         Settings.registerWith(update);
      }

      #endregion

      #region Instrument interface

      public override bool isExpired() 
      {
         Date today = Settings.evaluationDate();
         foreach (var cf in floatingLeg_)
            if (!cf.hasOccurred(today)) return false;

         return true;
      }
      public override void setupArguments(IPricingEngineArguments args) 
      {
         CapFloor.Arguments arguments = args as CapFloor.Arguments;

         if (arguments == null) throw new ArgumentException("wrong argument type");


         int n = floatingLeg_.Count;

         arguments.startDates = new InitializedList<Date>(n) ;
         arguments.fixingDates = new InitializedList<Date>(n);
         arguments.endDates = new InitializedList<Date>(n);
         arguments.accrualTimes = new InitializedList<double>(n);
         arguments.forwards = new InitializedList<double?>(n);
         arguments.nominals = new InitializedList<double>(n);
         arguments.gearings = new InitializedList<double>(n);
         arguments.capRates = new InitializedList<double?>(n);
         arguments.floorRates = new InitializedList<double?>(n);
         arguments.spreads = new InitializedList<double>(n);

         arguments.type = type_;

         Date today = Settings.evaluationDate();

         for (int i=0; i<n; ++i) 
         {
            FloatingRateCoupon coupon = floatingLeg_[i] as FloatingRateCoupon;

            if ( coupon == null ) 
               throw new ArgumentException("non-FloatingRateCoupon given");

            arguments.startDates[i] = coupon.accrualStartDate();
            arguments.fixingDates[i] = coupon.fixingDate();
            arguments.endDates[i] = coupon.date();

            // this is passed explicitly for precision
            arguments.accrualTimes[i] = coupon.accrualPeriod();

            // this is passed explicitly for precision...
            if (arguments.endDates[i] >= today) 
            { 
               // ...but only if needed
               arguments.forwards[i] = coupon.adjustedFixing;
            } 
            else 
            {
               arguments.forwards[i] = null;
            }

            arguments.nominals[i] = coupon.nominal();
            double spread = coupon.spread();
            double gearing = coupon.gearing();
            arguments.gearings[i] = gearing;
            arguments.spreads[i] = spread;

            if (type_ == CapFloorType.Cap || type_ == CapFloorType.Collar)
                arguments.capRates[i] = (capRates_[i]-spread)/gearing;
            else
                arguments.capRates[i] = null;

            if (type_ == CapFloorType.Floor || type_ == CapFloorType.Collar)
                arguments.floorRates[i] = (floorRates_[i]-spread)/gearing;
            else
                arguments.floorRates[i] = null;
         }
      }

      #endregion

      #region Inspectors

      public CapFloorType getType() { return type_; }
      public List<double> capRates() { return capRates_; }
      public List<double> floorRates() { return floorRates_; }
      public List<CashFlow> floatingLeg() { return floatingLeg_; }

      public Date startDate() {return CashFlows.startDate(floatingLeg_);}
      public Date maturityDate() {return CashFlows.maturityDate(floatingLeg_);}
      
      public FloatingRateCoupon lastFloatingRateCoupon() 
      {
         CashFlow lastCF = floatingLeg_.Last();
         FloatingRateCoupon lastFloatingCoupon = lastCF as FloatingRateCoupon;
         return lastFloatingCoupon;
      }

      public CapFloor optionlet(int i) 
      {
         if ( i >= floatingLeg().Count )
            throw new ArgumentException( i + " optionlet does not exist, only " +
                                         floatingLeg().Count);

        List<CashFlow> cf = new List<CashFlow>();
        cf.Add(floatingLeg()[i]);

        List<double> cap = new List<double>() ;
        List<double> floor = new List<double>() ;

        if (getType() == CapFloorType.Cap || getType() == CapFloorType.Collar)
            cap.Add(capRates()[i]);
        if (getType() == CapFloorType.Floor || getType() == CapFloorType.Collar)
            floor.Add(floorRates()[i]);

        return new CapFloor(getType(), cf, cap, floor);
      }

      public double atmRate(YieldTermStructure discountCurve) 
      {
         bool includeSettlementDateFlows = false;
         Date settlementDate = discountCurve.referenceDate();
         return CashFlows.atmRate(floatingLeg_, discountCurve, includeSettlementDateFlows, settlementDate);
      }

      public double impliedVolatility(
                              double targetValue,
                              Handle<YieldTermStructure> discountCurve,
                              double guess,
                              double accuracy,
                              int maxEvaluations)
      {
         return impliedVolatility(targetValue, discountCurve, guess, accuracy, maxEvaluations,
                                  1.0e-7, 4.0);
      }


      public double impliedVolatility(
                              double targetValue,
                              Handle<YieldTermStructure> discountCurve,
                              double guess,
                              double accuracy,
                              int maxEvaluations,
                              double minVol,
                              double maxVol) 
      {
         calculate();
         if (isExpired()) 
            throw new ArgumentException("instrument expired");

         ImpliedVolHelper f = new ImpliedVolHelper(this, discountCurve, targetValue);
         //Brent solver;
         NewtonSafe solver = new NewtonSafe();
         solver.setMaxEvaluations(maxEvaluations);
         return solver.solve(f, accuracy, guess, minVol, maxVol);
         //return 0;
      }

      #endregion

      #region Pricing

      public class Arguments : IPricingEngineArguments
      {
         public Arguments() 
         {
           //type = -1;
         }
         public CapFloorType type;
         public List<Date> startDates;
         public List<Date> fixingDates;
         public List<Date> endDates;
         public List<double> accrualTimes;
         public List<double?> capRates;
         public List<double?> floorRates;
         public List<double?> forwards;
         public List<double> gearings;
         public List<double> spreads;
         public List<double> nominals;
         public void validate() 
         {
            if (endDates.Count != startDates.Count)
               throw new ArgumentException( "number of start dates (" + startDates.Count
                                            + ") different from that of end dates ("
                                            + endDates.Count + ")");

            if (accrualTimes.Count != startDates.Count)
               throw new ArgumentException( "number of start dates (" + startDates.Count
                                            + ") different from that of  accrual times  ("
                                            + accrualTimes.Count + ")");

            if (capRates.Count != startDates.Count && type!= CapFloorType.Floor)
               throw new ArgumentException( "number of start dates (" + startDates.Count
                                            + ") different from that of  of cap rates  ("
                                            + capRates.Count + ")");

            if (floorRates.Count != startDates.Count && type!= CapFloorType.Cap)
               throw new ArgumentException( "number of start dates (" + startDates.Count
                                            + ") different from that of  of floor rates  ("
                                            + floorRates.Count + ")");

            if (gearings.Count != startDates.Count)
               throw new ArgumentException("number of start dates (" + startDates.Count
                                            + ") different from that of  gearings ("
                                            + gearings.Count + ")");

            if (spreads.Count != startDates.Count)
               throw new ArgumentException("number of start dates (" + startDates.Count
                                            + ") different from that of spreads ("
                                            + spreads.Count + ")");

            if (nominals.Count != startDates.Count)
               throw new ArgumentException("number of start dates (" + startDates.Count
                                            + ") different from that of nominals ("
                                            + nominals.Count + ")");

            if (forwards.Count != startDates.Count)
               throw new ArgumentException("number of start dates (" + startDates.Count
                                            + ") different from that of forwards ("
                                            + forwards.Count + ")");

        }
      }

      #endregion
   }

   /// <summary>
   /// Concrete cap class
   /// \ingroup instruments
   /// </summary>
   public class Cap : CapFloor 
   {
      public Cap(List<CashFlow> floatingLeg,List<double> exerciseRates)
         : base(CapFloorType.Cap, floatingLeg, exerciseRates, new List<double>()) {}
   };

   /// <summary>
   /// Concrete floor class
   /// \ingroup instruments 
   /// </summary>
   public class Floor : CapFloor 
    {
      public Floor(List<CashFlow> floatingLeg,List<double> exerciseRates)
        : base(CapFloorType.Floor, floatingLeg,new List<double>(), exerciseRates) {}
    };

   /// <summary>
   /// Concrete collar class
   /// \ingroup instruments
   /// </summary>
   public class Collar : CapFloor 
    {
      public Collar(List<CashFlow> floatingLeg,List<double> capRates, List<double> floorRates)
          : base(CapFloorType.Collar, floatingLeg, capRates, floorRates) { }
    };

   //! base class for cap/floor engines
   public abstract class CapFloorEngine 
        : GenericEngine<CapFloor.Arguments, CapFloor.Results> {};

   public class ImpliedVolHelper : ISolver1d
   {
      private IPricingEngine engine_;
      private Handle<YieldTermStructure> discountCurve_;
      private double targetValue_;
      private SimpleQuote vol_;
      private Instrument.Results results_;

      public ImpliedVolHelper(CapFloor cap,Handle<YieldTermStructure> discountCurve,
                              double targetValue)
      {
         discountCurve_ = discountCurve;
         targetValue_ = targetValue;

         vol_ = new SimpleQuote(-1.0);
         Handle<Quote> h = new Handle<Quote>(vol_);
         engine_ = (IPricingEngine)new BlackCapFloorEngine(discountCurve_, h);
         cap.setupArguments(engine_.getArguments());
         results_ = engine_.getResults() as Instrument.Results;

      }

      //Real operator()(Volatility x) const;
      //Real derivative(Volatility x) const;

      public override double value(double x)
      {
         if (x!=vol_.value()) 
         {
            vol_.setValue(x);
            engine_.calculate();
         }

         return results_.value.Value -targetValue_;
      }

      public override double derivative(double x) 
      {
         if (x!=vol_.value()) 
         {
            vol_.setValue(x);
            engine_.calculate();
         }
         if ( ! results_.additionalResults.Keys.Contains("vega") )
            throw new Exception("vega not provided");

         return (double) results_.additionalResults["vega"];
      }
   }
}
