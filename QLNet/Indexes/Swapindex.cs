/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
 * 
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

   public class SwapIndex : InterestRateIndex 
   {
      // need by CashFlowVectors
      public SwapIndex() { }


      public SwapIndex(string familyName, Period tenor, int settlementDays, Currency currency,
                        Calendar calendar, Period fixedLegTenor, BusinessDayConvention fixedLegConvention,
                        DayCounter fixedLegDayCounter, IborIndex iborIndex) :
         base(familyName, tenor, settlementDays, currency, calendar, fixedLegDayCounter)
      {
         tenor_ = tenor;
         iborIndex_ = iborIndex;
         fixedLegTenor_ = fixedLegTenor;
         fixedLegConvention_ = fixedLegConvention;
         exogenousDiscount_ = false;
         discount_ = new Handle<YieldTermStructure>();

         iborIndex_.registerWith(update);
      }

      public SwapIndex(string familyName,
                       Period tenor,
                       int settlementDays,
                       Currency currency,
                       Calendar calendar,
                       Period fixedLegTenor,
                       BusinessDayConvention fixedLegConvention,
                       DayCounter fixedLegDayCounter,
                       IborIndex iborIndex,
                       Handle<YieldTermStructure> discountingTermStructure)
      {
         tenor_ = tenor;
         iborIndex_ = iborIndex;
         fixedLegTenor_ = fixedLegTenor;
         fixedLegConvention_ = fixedLegConvention;
         exogenousDiscount_ = true;
         discount_ = discountingTermStructure;

         iborIndex_.registerWith(update);
      }

      //! \name InterestRateIndex interface
      //@{
      public override Date maturityDate(Date valueDate)
      {
         Date fixDate = fixingDate(valueDate);
         return underlyingSwap(fixDate).maturityDate();
      }
      //@}
      //! \name Inspectors
      //@{
      public Period fixedLegTenor() { return fixedLegTenor_; }
      public BusinessDayConvention fixedLegConvention() { return fixedLegConvention_; }
      public IborIndex iborIndex() { return iborIndex_; }
      public Handle<YieldTermStructure> forwardingTermStructure() { return iborIndex_.forwardingTermStructure();}
		public Handle<YieldTermStructure> discountingTermStructure() { return discount_; }
      public bool exogenousDiscount() { return exogenousDiscount_; }
      // \warning Relinking the term structure underlying the index will not have effect on the returned swap.
      // recheck
      public VanillaSwap underlyingSwap(Date fixingDate)
      {
         double fixedRate = 0.0;
         if (exogenousDiscount_)
            return new MakeVanillaSwap(tenor_, iborIndex_, fixedRate)
                 .withEffectiveDate(valueDate(fixingDate))
                 .withFixedLegCalendar(fixingCalendar())
                 .withFixedLegDayCount(dayCounter_)
                 .withFixedLegTenor(fixedLegTenor_)
                 .withFixedLegConvention(fixedLegConvention_)
                 .withFixedLegTerminationDateConvention(fixedLegConvention_)
                 .withDiscountingTermStructure(discount_)
                 .value();
         else
            return new MakeVanillaSwap(tenor_, iborIndex_, fixedRate)
                 .withEffectiveDate(valueDate(fixingDate))
                 .withFixedLegCalendar(fixingCalendar())
                 .withFixedLegDayCount(dayCounter_)
                 .withFixedLegTenor(fixedLegTenor_)
                 .withFixedLegConvention(fixedLegConvention_)
                 .withFixedLegTerminationDateConvention(fixedLegConvention_)
                 .value();

      }
      //@}
      //! \name Other methods
      //@{
      //! returns a copy of itself linked to a different forwarding curve
      public virtual SwapIndex clone(Handle<YieldTermStructure> forwarding) 
      {
         if (exogenousDiscount_)
            return new SwapIndex(familyName(),
                       tenor(),
                       fixingDays(),
                       currency(),
                       fixingCalendar(),
                       fixedLegTenor(),
                       fixedLegConvention(),
                       dayCounter(),
                       iborIndex_.clone(forwarding),
                       discount_);
         else
            return new SwapIndex(familyName(),
                       tenor(),
                       fixingDays(),
                       currency(),
                       fixingCalendar(),
                       fixedLegTenor(),
                       fixedLegConvention(),
                       dayCounter(),
                       iborIndex_.clone(forwarding));
      }
		//! returns a copy of itself linked to a different curves
		public virtual SwapIndex clone( Handle<YieldTermStructure> forwarding, Handle<YieldTermStructure> discounting )
		{
				return new SwapIndex( familyName(),
							  tenor(),
							  fixingDays(),
							  currency(),
							  fixingCalendar(),
							  fixedLegTenor(),
							  fixedLegConvention(),
							  dayCounter(),
							  iborIndex_.clone( forwarding ),
							  discounting );
		}
		//! returns a copy of itself linked to a different tenor
		public virtual SwapIndex clone( Period tenor )
		{
			if ( exogenousDiscount_ )
				return new SwapIndex( familyName(),
							  tenor,
							  fixingDays(),
							  currency(),
							  fixingCalendar(),
							  fixedLegTenor(),
							  fixedLegConvention(),
							  dayCounter(),
							  iborIndex(),
							  discountingTermStructure() );
			else
				return new SwapIndex( familyName(),
							  tenor,
							  fixingDays(),
							  currency(),
							  fixingCalendar(),
							  fixedLegTenor(),
							  fixedLegConvention(),
							  dayCounter(),
							  iborIndex() );
		}
      // @}
      protected override double forecastFixing(Date fixingDate)
      {
         return underlyingSwap(fixingDate).fairRate();
      }
      protected new Period tenor_;
      protected IborIndex iborIndex_;
      protected Period fixedLegTenor_;
      BusinessDayConvention fixedLegConvention_;
      bool exogenousDiscount_;
      Handle<YieldTermStructure> discount_;
   }

   //! base class for overnight indexed swap indexes
   public class OvernightIndexedSwapIndex : SwapIndex 
   {
      public OvernightIndexedSwapIndex(
                  string familyName,
                  Period tenor,
                  int settlementDays,
                  Currency currency,
                  OvernightIndex overnightIndex)
         :base(familyName, tenor, settlementDays,
                currency, overnightIndex.fixingCalendar(),
                new Period(1,TimeUnit.Years),BusinessDayConvention.ModifiedFollowing, 
                overnightIndex.dayCounter(),overnightIndex)
      {
         overnightIndex_ = overnightIndex;
      }
      //! \name Inspectors
      //@{
      public OvernightIndex overnightIndex() { return overnightIndex_;}
      /*! \warning Relinking the term structure underlying the index will
                   not have effect on the returned swap.
      */
      public new OvernightIndexedSwap underlyingSwap(Date fixingDate)
      {
         double fixedRate = 0.0;
         return new MakeOIS(tenor_, overnightIndex_, fixedRate)
             .withEffectiveDate(valueDate(fixingDate))
             .withFixedLegDayCount(dayCounter_);
      }
      //@}
      protected OvernightIndex overnightIndex_;
    };


}
