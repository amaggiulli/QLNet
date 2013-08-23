/*
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
 Copyright (C) 2008, 2009 , 2010  Andrea Maggiulli (a.maggiulli@gmail.com)
  
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

   //! Cms-rate coupon with digital digital call/put option
   public class DigitalCmsCoupon : DigitalCoupon
   {
      // need by CashFlowVectors
      public DigitalCmsCoupon() { }

      public DigitalCmsCoupon(CmsCoupon underlying, 
                              double? callStrike = null,
                              Position.Type callPosition = Position.Type.Long, 
                              bool isCallATMIncluded = false, 
                              double? callDigitalPayoff = null, 
                              double? putStrike = null,
                              Position.Type putPosition = Position.Type.Long, 
                              bool isPutATMIncluded = false, 
                              double? putDigitalPayoff = null, 
                              DigitalReplication replication = null )
         : base(underlying, callStrike, callPosition, isCallATMIncluded, callDigitalPayoff, putStrike, putPosition, isPutATMIncluded, putDigitalPayoff, replication)
      {
      }

      // Factory - for Leg generators
      public virtual CashFlow factory(CmsCoupon underlying, double? callStrike, Position.Type callPosition, bool isCallATMIncluded, double? callDigitalPayoff, double? putStrike, Position.Type putPosition, bool isPutATMIncluded, double? putDigitalPayoff, DigitalReplication replication)
      {
         return new DigitalCmsCoupon(underlying, callStrike, callPosition, isCallATMIncluded, callDigitalPayoff, putStrike, putPosition, isPutATMIncluded, putDigitalPayoff, replication);
      }

      //! \name Visitability
      //@{
      //public void accept(ref AcyclicVisitor v)
      //{
      //    Visitor<DigitalCmsCoupon> v1 = v as Visitor<DigitalCmsCoupon>;
      //    if (v1 != 0)
      //        v1.visit( this);
      //    else
      //        base.accept(ref v);
      //}
      //@}
   }


   //! helper class building a sequence of digital ibor-rate coupons
   public class DigitalCmsLeg
   {
      public DigitalCmsLeg(Schedule schedule, SwapIndex index)
      {
         schedule_ = schedule;
         index_ = index;
         paymentAdjustment_ = BusinessDayConvention.Following;
         inArrears_ = false;
         longCallOption_ = Position.Type.Long;
         callATM_ = false;
         longPutOption_ = Position.Type.Long;
         putATM_ = false;
      }
      public DigitalCmsLeg withNotionals(double notional)
      {
         notionals_ = new List<double>(); notionals_.Add(notional);
         return this;
      }
      public DigitalCmsLeg withNotionals(List<double> notionals)
      {
         notionals_ = notionals;
         return this;
      }
      public DigitalCmsLeg withPaymentDayCounter(DayCounter dayCounter)
      {
         paymentDayCounter_ = dayCounter;
         return this;
      }
      public DigitalCmsLeg withPaymentAdjustment(BusinessDayConvention convention)
      {
         paymentAdjustment_ = convention;
         return this;
      }
      public DigitalCmsLeg withFixingDays(int fixingDays)
      {
         fixingDays_ = new List<int>(); fixingDays_.Add(fixingDays);
         return this;
      }
      public DigitalCmsLeg withFixingDays(List<int> fixingDays)
      {
         fixingDays_ = fixingDays;
         return this;
      }
      public DigitalCmsLeg withGearings(double gearing)
      {
         gearings_ = new List<double>(); gearings_.Add(gearing);
         return this;
      }
      public DigitalCmsLeg withGearings(List<double> gearings)
      {
         gearings_ = gearings;
         return this;
      }
      public DigitalCmsLeg withSpreads(double spread)
      {
         spreads_ = new List<double>(); spreads_.Add(spread);
         return this;
      }
      public DigitalCmsLeg withSpreads(List<double> spreads)
      {
         spreads_ = spreads;
         return this;
      }
      public DigitalCmsLeg inArrears()
      {
         return inArrears(true);
      }
      public DigitalCmsLeg inArrears(bool flag)
      {
         inArrears_ = flag;
         return this;
      }
      public DigitalCmsLeg withCallStrikes(double strike)
      {
         callStrikes_ = new List<double>(); callStrikes_.Add(strike);
         return this;
      }
      public DigitalCmsLeg withCallStrikes(List<double> strikes)
      {
         callStrikes_ = strikes;
         return this;
      }
      public DigitalCmsLeg withLongCallOption(Position.Type type)
      {
         longCallOption_ = type;
         return this;
      }
      public DigitalCmsLeg withCallATM()
      {
         return withCallATM(true);
      }
      public DigitalCmsLeg withCallATM(bool flag)
      {
         callATM_ = flag;
         return this;
      }
      public DigitalCmsLeg withCallPayoffs(double payoff)
      {
         callPayoffs_ = new List<double>(); callPayoffs_.Add(payoff);
         return this;
      }
      public DigitalCmsLeg withCallPayoffs(List<double> payoffs)
      {
         callPayoffs_ = payoffs;
         return this;
      }
      public DigitalCmsLeg withPutStrikes(double strike)
      {
         putStrikes_ = new List<double>(); putStrikes_.Add(strike);
         return this;
      }
      public DigitalCmsLeg withPutStrikes(List<double> strikes)
      {
         putStrikes_ = strikes;
         return this;
      }
      public DigitalCmsLeg withLongPutOption(Position.Type type)
      {
         longPutOption_ = type;
         return this;
      }
      public DigitalCmsLeg withPutATM()
      {
         return withPutATM(true);
      }
      public DigitalCmsLeg withPutATM(bool flag)
      {
         putATM_ = flag;
         return this;
      }
      public DigitalCmsLeg withPutPayoffs(double payoff)
      {
         putPayoffs_ = new List<double>(); putPayoffs_.Add(payoff);
         return this;
      }
      public DigitalCmsLeg withPutPayoffs(List<double> payoffs)
      {
         putPayoffs_ = payoffs;
         return this;
      }
      public DigitalCmsLeg withReplication()
      {
         return withReplication(new DigitalReplication());
      }
      public DigitalCmsLeg withReplication(DigitalReplication replication)
      {
         replication_ = replication;
         return this;
      }
      public List<CashFlow> value()
      {
         return CashFlowVectors.FloatingDigitalLeg<SwapIndex, CmsCoupon, DigitalCmsCoupon>(notionals_, schedule_, index_, paymentDayCounter_, paymentAdjustment_, fixingDays_, gearings_, spreads_, inArrears_, callStrikes_, longCallOption_, callATM_, callPayoffs_, putStrikes_, longPutOption_, putATM_, putPayoffs_, replication_);
      }

      private Schedule schedule_;
      private SwapIndex index_;
      private List<double> notionals_;
      private DayCounter paymentDayCounter_;
      private BusinessDayConvention paymentAdjustment_;
      private List<int> fixingDays_;
      private List<double> gearings_;
      private List<double> spreads_;
      private bool inArrears_;
      private List<double> callStrikes_;
      private List<double> callPayoffs_;
      private Position.Type longCallOption_;
      private bool callATM_;
      private List<double> putStrikes_;
      private List<double> putPayoffs_;
      private Position.Type longPutOption_;
      private bool putATM_;
      private DigitalReplication replication_;
   }

}
