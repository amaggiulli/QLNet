/*
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
   public class OISRateHelper : RelativeDateRateHelper
   {
      public OISRateHelper(int settlementDays,
                           Period tenor, // swap maturity
                           Handle<Quote> fixedRate,
                           OvernightIndex overnightIndex)
         : base(fixedRate)
      {
         settlementDays_ = settlementDays;
         tenor_ = tenor;
         overnightIndex_ = overnightIndex;
         overnightIndex_.registerWith(update);
         initializeDates();
      }

      public OvernightIndexedSwap swap() { return swap_; }

      protected override void initializeDates() 
      {

         // dummy OvernightIndex with curve/swap arguments
         // review here
         IborIndex clonedIborIndex = overnightIndex_.clone(termStructureHandle_);
         OvernightIndex clonedOvernightIndex = clonedIborIndex as OvernightIndex;

         swap_ = new MakeOIS(tenor_, clonedOvernightIndex, 0.0)
                     .withSettlementDays(settlementDays_)
                     .withDiscountingTermStructure(termStructureHandle_);
         
         earliestDate_ = swap_.startDate();
         latestDate_ = swap_.maturityDate();
      }

      public override void setTermStructure(YieldTermStructure t) 
      {
         // no need to register---the index is not lazy
         termStructureHandle_.linkTo(t, false);
         base.setTermStructure(t);
      }

      public override double impliedQuote() 
      {
         if (termStructure_ == null) throw new ArgumentException("term structure not set");

         // we didn't register as observers - force calculation
         swap_.recalculate();
         return swap_.fairRate().Value;
      }

      protected int settlementDays_;
      protected Period tenor_;
      protected OvernightIndex overnightIndex_;
      protected OvernightIndexedSwap swap_;
      protected RelinkableHandle<YieldTermStructure> termStructureHandle_ = new RelinkableHandle<YieldTermStructure>();
   }


   //! Rate helper for bootstrapping over Overnight Indexed Swap rates
   public class DatedOISRateHelper : RateHelper
   {
      
      public DatedOISRateHelper(Date startDate,
                                Date endDate,
                                Handle<Quote> fixedRate,
                                OvernightIndex overnightIndex)
    
         : base(fixedRate) 
      {

        overnightIndex.registerWith(update);

        // dummy OvernightIndex with curve/swap arguments
        // review here
        IborIndex clonedIborIndex = overnightIndex.clone(termStructureHandle_);
        OvernightIndex clonedOvernightIndex = clonedIborIndex as OvernightIndex;

         swap_ = new MakeOIS(new Period(), clonedOvernightIndex, 0.0)
                              .withEffectiveDate(startDate)
                              .withTerminationDate(endDate)
                              .withDiscountingTermStructure(termStructureHandle_);

         earliestDate_ = swap_.startDate();
         latestDate_ = swap_.maturityDate();
    
      }


      public override void setTermStructure(YieldTermStructure t) 
      {
         // no need to register---the index is not lazy
         termStructureHandle_.linkTo(t, false);
         base.setTermStructure(t);
    
      }

      public override double impliedQuote()
      {
         if (termStructure_ == null) throw new ArgumentException("term structure not set");
         
         // we didn't register as observers - force calculation
         swap_.recalculate();
         return swap_.fairRate().Value;
      }    

      protected OvernightIndexedSwap swap_;
      protected RelinkableHandle<YieldTermStructure> termStructureHandle_ = new RelinkableHandle<YieldTermStructure>();
   }
}
