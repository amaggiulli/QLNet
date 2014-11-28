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
   //! Zero-coupon inflation-swap bootstrap helper
   public class ZeroCouponInflationSwapHelper : BootstrapHelper<ZeroInflationTermStructure> 
   {
      public ZeroCouponInflationSwapHelper(
         Handle<Quote> quote,
         Period swapObsLag,   // lag on swap observation of index
         Date maturity,
         Calendar calendar,   // index may have null calendar as valid on every day
         BusinessDayConvention paymentConvention,
         DayCounter dayCounter,
         ZeroInflationIndex zii)
         : base(quote)
      {
         swapObsLag_ = swapObsLag;
         maturity_ = maturity;
         calendar_ = calendar;
         paymentConvention_ = paymentConvention;
         dayCounter_ = dayCounter;
         zii_ = zii;

         if (zii_.interpolated()) 
         {
            // if interpolated then simple
            earliestDate_ = maturity_ - swapObsLag_;
            latestDate_ = maturity_ - swapObsLag_;
         } 
         else 
         {
            // but if NOT interpolated then the value is valid
            // for every day in an inflation period so you actually
            // get an extended validity, however for curve building
            // just put the first date because using that convention
            // for the base date throughout
         KeyValuePair<Date,Date> limStart = Utils.inflationPeriod(maturity_ - swapObsLag_,
                                                         zii_.frequency());
         earliestDate_ = limStart.Key;
         latestDate_ = limStart.Key;
         }

         // check that the observation lag of the swap
         // is compatible with the availability lag of the index AND
         // it's interpolation (assuming the start day is spot)
         if (zii_.interpolated()) 
         {
            Period pShift = new Period(zii_.frequency());
            if ( (swapObsLag_ - pShift) <= zii_.availabilityLag())
               throw new ApplicationException(
                     "inconsistency between swap observation of index "
                     + swapObsLag_ +
                     " index availability " + zii_.availabilityLag() +
                     " index period " + pShift +
                     " and index availability " + zii_.availabilityLag() +
                     " need (obsLag-index period) > availLag");

         }
         Settings.registerWith(update);
      }


      public override void setTermStructure(ZeroInflationTermStructure z)
      {

			base.setTermStructure( z );

			// set up a new ZCIIS
			// but this one does NOT own its inflation term structure
			bool own = false;
			double K = quote().link.value();

			// The effect of the new inflation term structure is
			// felt via the effect on the inflation index
			Handle<ZeroInflationTermStructure> zits = new Handle<ZeroInflationTermStructure>( z, own );

			ZeroInflationIndex new_zii = zii_.clone( zits );

			double nominal = 1000000.0;   // has to be something but doesn't matter what
			Date start = z.nominalTermStructure().link.referenceDate();
			zciis_ = new ZeroCouponInflationSwap(
										ZeroCouponInflationSwap.Type.Payer,
										nominal, start, maturity_,
										calendar_, paymentConvention_, dayCounter_, K, // fixed side & fixed rate
										new_zii, swapObsLag_ );
			// Because very simple instrument only takes
			// standard discounting swap engine.
			zciis_.setPricingEngine( new DiscountingSwapEngine( z.nominalTermStructure() ) );
      }

      public override double impliedQuote() 
      {
         // what does the term structure imply?
         // in this case just the same value ... trivial case
         // (would not be so for an inflation-linked bond)
         zciis_.recalculate();
         return zciis_.fairRate();
      }
    
       
      protected Period swapObsLag_;
      protected Date maturity_;
      protected Calendar calendar_;
      protected BusinessDayConvention paymentConvention_;
      protected DayCounter dayCounter_;
      protected ZeroInflationIndex zii_;
      protected ZeroCouponInflationSwap zciis_;
   }

	//! Year-on-year inflation-swap bootstrap helper
   public class YearOnYearInflationSwapHelper  : BootstrapHelper<YoYInflationTermStructure> 
	{
		public YearOnYearInflationSwapHelper( Handle<Quote> quote,
												  Period swapObsLag,
												  Date maturity,
												  Calendar calendar,
												  BusinessDayConvention paymentConvention,
												  DayCounter dayCounter,
												  YoYInflationIndex yii )
			: base( quote )
		{
			swapObsLag_= swapObsLag; 
			maturity_ = maturity;
			calendar_= calendar;
			paymentConvention_= paymentConvention;
			dayCounter_= dayCounter;
			yii_ = yii;

			if (yii_.interpolated()) 
			{
            // if interpolated then simple
            earliestDate_ = maturity_ - swapObsLag_;
            latestDate_ = maturity_ - swapObsLag_;
			} 
			else 
			{
            // but if NOT interpolated then the value is valid
            // for every day in an inflation period so you actually
            // get an extended validity, however for curve building
            // just put the first date because using that convention
            // for the base date throughout
            KeyValuePair<Date,Date> limStart = Utils.inflationPeriod(maturity_ - swapObsLag_,
                                                            yii_.frequency());
            earliestDate_ = limStart.Key;
            latestDate_ = limStart.Key;
        }

        // check that the observation lag of the swap
        // is compatible with the availability lag of the index AND
        // it's interpolation (assuming the start day is spot)
        if (yii_.interpolated()) 
		  {
            Period pShift = new Period(yii_.frequency());
            Utils.QL_REQUIRE( swapObsLag_ - pShift > yii_.availabilityLag(), () =>
                       "inconsistency between swap observation of index "
                       + swapObsLag_ +
                       " index availability " + yii_.availabilityLag() +
                       " index period " + pShift +
                       " and index availability " + yii_.availabilityLag() +
                       " need (obsLag-index period) > availLag");
        }

        Settings.registerWith(update);
		}

		public override void setTermStructure( YoYInflationTermStructure y )
		{
			base.setTermStructure(y);

        // set up a new YYIIS
        // but this one does NOT own its inflation term structure
        const bool own = false;

        // The effect of the new inflation term structure is
        // felt via the effect on the inflation index
        Handle<YoYInflationTermStructure> yyts = new Handle<YoYInflationTermStructure>(y, own);

        YoYInflationIndex new_yii = yii_.clone(yyts);

        // always works because tenor is always 1 year so
        // no problem with different days-in-month
        Date from = Settings.evaluationDate();
        Date to = maturity_;
        Schedule fixedSchedule = new MakeSchedule().from(from).to(to)
                                    .withTenor(new Period(1,TimeUnit.Years))
                                    .withConvention(BusinessDayConvention.Unadjusted)
                                    .withCalendar(calendar_)// fixed leg gets cal from sched
                                    .value();
        Schedule yoySchedule = fixedSchedule;
        double spread = 0.0;
        double fixedRate = quote().link.value();

        double nominal = 1000000.0;   // has to be something but doesn't matter what
        yyiis_ = new YearOnYearInflationSwap(YearOnYearInflationSwap.Type.Payer,
				nominal,
				fixedSchedule,
				fixedRate,
				dayCounter_,
				yoySchedule,
				new_yii,
				swapObsLag_,
				spread,
				dayCounter_,
				calendar_,  // inflation index does not have a calendar
				paymentConvention_);


        // Because very simple instrument only takes
        // standard discounting swap engine.
        yyiis_.setPricingEngine(new DiscountingSwapEngine(y.nominalTermStructure()));
		}

		public override double impliedQuote()
		{
			// what does the term structure imply?
			// in this case just the same value ... trivial case
			// (would not be so for an inflation-linked bond)
			yyiis_.recalculate();
			return yyiis_.fairRate();
		}

		protected Period swapObsLag_;
		protected Date maturity_;
		protected Calendar calendar_;
		protected BusinessDayConvention paymentConvention_;
		protected DayCounter dayCounter_;
		protected YoYInflationIndex yii_;
		protected YearOnYearInflationSwap yyiis_;
    }

}
