/*
 Copyright (C) 2008, 2009 , 2010 Andrea Maggiulli (a.maggiulli@gmail.com)

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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QLNet;

namespace TestSuite
{
   struct Datum
   {
      public Date date;
      public double rate;
      public Datum(Date d , double r )
      {
         date = d;
         rate = r;
      }
   };

   //===========================================================================================
   // zero inflation tests, index, termstructure, and swaps
   //===========================================================================================

   [TestClass()]
   public class T_Inflation
   {

      private YieldTermStructure nominalTermStructure() 
      {
        Date evaluationDate = new Date(13, Month.August, 2007);
        return new FlatForward(evaluationDate, 0.05, new Actual360());
      }

      private List<BootstrapHelper<ZeroInflationTermStructure>> makeHelpers(Datum[] iiData, int N,
                                                ZeroInflationIndex ii, Period observationLag,
                                                Calendar calendar,
                                                BusinessDayConvention bdc,
                                                DayCounter dc)
      {
         List <BootstrapHelper<ZeroInflationTermStructure>> instruments = new  List<BootstrapHelper<ZeroInflationTermStructure>>();
         for (int i = 0; i < N; i++)
         {
            Date maturity = iiData[i].date;
            Handle<Quote> quote = new Handle<Quote>(new SimpleQuote(iiData[i].rate / 100.0));
            BootstrapHelper<ZeroInflationTermStructure> anInstrument = new ZeroCouponInflationSwapHelper(quote, observationLag, maturity,
                      calendar, bdc, dc, ii);
            instruments.Add(anInstrument);
         }
         return instruments;
      }
		private List<BootstrapHelper<YoYInflationTermStructure>> makeHelpers( Datum[] iiData, int N,
														YoYInflationIndex ii, Period observationLag,
														Calendar calendar,
														BusinessDayConvention bdc,
														DayCounter dc )
		{
			List<BootstrapHelper<YoYInflationTermStructure>> instruments = new List<BootstrapHelper<YoYInflationTermStructure>>();
			for ( int i = 0; i < N; i++ )
			{
				Date maturity = iiData[i].date;
				Handle<Quote> quote = new Handle<Quote>( new SimpleQuote( iiData[i].rate / 100.0 ) );
				BootstrapHelper<YoYInflationTermStructure> anInstrument = new YearOnYearInflationSwapHelper( quote, observationLag, maturity,
							 calendar, bdc, dc, ii );
				instruments.Add( anInstrument );
			}
			return instruments;
		}

		[TestMethod()]
      public void testZeroIndex()
      {
         // Testing zero inflation indices...
         EUHICP euhicp = new EUHICP(true);

         if (euhicp.name() != "EU HICP"
             || euhicp.frequency() != Frequency.Monthly
             || euhicp.revised()
             || !euhicp.interpolated()
             || euhicp.availabilityLag() != new Period(1, TimeUnit.Months))
         {
            Assert.Fail("wrong EU HICP data ("
                        + euhicp.name() + ", "
                        + euhicp.frequency() + ", "
                        + euhicp.revised() + ", "
                        + euhicp.interpolated() + ", "
                        + euhicp.availabilityLag() + ")");
         }

         UKRPI ukrpi = new UKRPI(false);
         if (ukrpi.name() != "UK RPI"
             || ukrpi.frequency() != Frequency.Monthly
             || ukrpi.revised()
             || ukrpi.interpolated()
             || ukrpi.availabilityLag() != new Period(1, TimeUnit.Months))
         {
            Assert.Fail("wrong UK RPI data ("
                        + ukrpi.name() + ", "
                        + ukrpi.frequency() + ", "
                        + ukrpi.revised() + ", "
                        + ukrpi.interpolated() + ", "
                        + ukrpi.availabilityLag() + ")");
         }


         // Retrieval test.
         //----------------
         // make sure of the evaluation date
         Date evaluationDate = new Date(13, Month.August, 2007);
         evaluationDate = new UnitedKingdom().adjust(evaluationDate);
         Settings.setEvaluationDate(evaluationDate);

         // fixing data
         Date from = new Date(1, Month.January, 2005);
         Date to = new Date(13, Month.August, 2007);
         Schedule rpiSchedule = new MakeSchedule().from(from).to(to)
                                .withTenor(new Period(1, TimeUnit.Months))
                                .withCalendar(new UnitedKingdom())
                                .withConvention(BusinessDayConvention.ModifiedFollowing)
                                .value();

         double[] fixData = { 189.9, 189.9, 189.6, 190.5, 191.6, 192.0,
                              192.2, 192.2, 192.6, 193.1, 193.3, 193.6,
                              194.1, 193.4, 194.2, 195.0, 196.5, 197.7,
                              198.5, 198.5, 199.2, 200.1, 200.4, 201.1,
                              202.7, 201.6, 203.1, 204.4, 205.4, 206.2,
                              207.3, 206.1, -999.0 };

         bool interp = false;
         UKRPI iir = new UKRPI(interp);

         for (int i = 0; i < rpiSchedule.Count - 1; i++)
         {
            iir.addFixing(rpiSchedule[i], fixData[i]);
         }

         Date todayMinusLag = evaluationDate - iir.availabilityLag();
         KeyValuePair<Date, Date> lim1 = Utils.inflationPeriod(todayMinusLag, iir.frequency());
         todayMinusLag = lim1.Key;

         double eps = 1.0e-8;

         // -1 because last value not yet available,
         // (no TS so can't forecast).
         for (int i = 0; i < rpiSchedule.Count - 1; i++)
         {
            KeyValuePair<Date, Date> lim = Utils.inflationPeriod(rpiSchedule[i],
                                                   iir.frequency());
            for (Date d = lim.Key; d <= lim.Value; d++)
            {
               if (d < Utils.inflationPeriod(todayMinusLag, iir.frequency()).Key)
               {
                  if (Math.Abs(iir.fixing(d) - fixData[i]) > eps)
                     Assert.Fail("Fixings not constant within a period: "
                                 + iir.fixing(d)
                                 + ", should be " + fixData[i]);
               }
            }
         }
      }

		[TestMethod()]
		public void testZeroTermStructure()
		{
			// Testing zero inflation term structure...

			SavedSettings backup = new SavedSettings();

			// try the Zero UK
			Calendar calendar = new UnitedKingdom();
			BusinessDayConvention bdc = BusinessDayConvention.ModifiedFollowing;
			Date evaluationDate = new Date( 13, Month.August, 2007 );
			evaluationDate = calendar.adjust( evaluationDate );
			Settings.setEvaluationDate( evaluationDate );

			// fixing data
			Date from = new Date( 1, Month.January, 2005 );
			Date to = new Date( 13, Month.August, 2007 );
			Schedule rpiSchedule = new MakeSchedule().from( from ).to( to )
										  .withTenor( new Period( 1, TimeUnit.Months ) )
										  .withCalendar( new UnitedKingdom() )
										  .withConvention( BusinessDayConvention.ModifiedFollowing )
										  .value();

			double[] fixData = { 189.9, 189.9, 189.6, 190.5, 191.6, 192.0,
                              192.2, 192.2, 192.6, 193.1, 193.3, 193.6,
                              194.1, 193.4, 194.2, 195.0, 196.5, 197.7,
                              198.5, 198.5, 199.2, 200.1, 200.4, 201.1,
                              202.7, 201.6, 203.1, 204.4, 205.4, 206.2,
                              207.3, 206.1,  -999.0 };

			RelinkableHandle<ZeroInflationTermStructure> hz = new RelinkableHandle<ZeroInflationTermStructure>();
			bool interp = false;
			UKRPI iiUKRPI = new UKRPI( interp, hz );
			for ( int i = 0; i < rpiSchedule.Count; i++ )
			{
				iiUKRPI.addFixing( rpiSchedule[i], fixData[i] );
			}


			ZeroInflationIndex ii = iiUKRPI as ZeroInflationIndex;
			YieldTermStructure nominalTS = nominalTermStructure();


			// now build the zero inflation curve

			Datum[] zcData = {
           new Datum( new Date(13, Month.August, 2008), 2.93 ),
           new Datum( new Date(13, Month.August, 2009), 2.95 ),
           new Datum( new Date(13, Month.August, 2010), 2.965 ),
           new Datum( new Date(15, Month.August, 2011), 2.98 ),
           new Datum( new Date(13, Month.August, 2012), 3.0 ),
           new Datum( new Date(13, Month.August, 2014), 3.06 ),
           new Datum( new Date(13, Month.August, 2017), 3.175 ),
           new Datum( new Date(13, Month.August, 2019), 3.243 ),
           new Datum( new Date(15, Month.August, 2022), 3.293 ),
           new Datum( new Date(14, Month.August, 2027), 3.338 ),
           new Datum( new Date(13, Month.August, 2032), 3.348 ),
           new Datum( new Date(15, Month.August, 2037), 3.348 ),
           new Datum( new Date(13, Month.August, 2047), 3.308 ),
           new Datum( new Date(13, Month.August, 2057), 3.228 )};


			Period observationLag = new Period( 2, TimeUnit.Months );
			DayCounter dc = new Thirty360();
			Frequency frequency = Frequency.Monthly;
			List<BootstrapHelper<ZeroInflationTermStructure>> helpers =
				makeHelpers( zcData, zcData.Length, ii,
												observationLag,
												calendar, bdc, dc );

			double baseZeroRate = zcData[0].rate / 100.0;
			PiecewiseZeroInflationCurve<Linear> pZITS = new PiecewiseZeroInflationCurve<Linear>(
								evaluationDate, calendar, dc, observationLag,
								frequency, ii.interpolated(), baseZeroRate,
								new Handle<YieldTermStructure>( nominalTS ), helpers );
			pZITS.recalculate();

			// first check that the zero rates on the curve match the data
			// and that the helpers give the correct impled rates
			const double eps = 0.00000001;
			bool forceLinearInterpolation = false;
			for ( int i = 0; i < zcData.Length; i++ )
			{
				Assert.IsTrue( Math.Abs( zcData[i].rate / 100.0
				- pZITS.zeroRate( zcData[i].date, observationLag, forceLinearInterpolation ) ) < eps,
				"ZITS zeroRate != instrument "
				+ pZITS.zeroRate( zcData[i].date, observationLag, forceLinearInterpolation )
				+ " vs " + zcData[i].rate / 100.0
				+ " interpolation: " + ii.interpolated()
				+ " forceLinearInterpolation " + forceLinearInterpolation );

				Assert.IsTrue( Math.Abs( helpers[i].impliedQuote()
				- zcData[i].rate / 100.0 ) < eps,
				"ZITS implied quote != instrument "
				+ helpers[i].impliedQuote()
				+ " vs " + zcData[i].rate / 100.0 );
			}

			// now test the forecasting capability of the index.
			hz.linkTo( pZITS );
			from = hz.link.baseDate();
			to = hz.link.maxDate() - new Period( 1, TimeUnit.Months ); // a bit of margin for adjustments
			Schedule testIndex = new MakeSchedule().from( from ).to( to )
											.withTenor( new Period( 1, TimeUnit.Months ) )
											.withCalendar( new UnitedKingdom() )
											.withConvention( BusinessDayConvention.ModifiedFollowing ).value();

			// we are testing UKRPI which is not interpolated
			Date bd = hz.link.baseDate();
			double bf = ii.fixing( bd );
			for ( int i = 0; i < testIndex.Count; i++ )
			{
				Date d = testIndex[i];
				double z = hz.link.zeroRate( d, new Period( 0, TimeUnit.Days ) );
				double t = hz.link.dayCounter().yearFraction( bd, d );
				if ( !ii.interpolated() ) // because fixing constant over period
					t = hz.link.dayCounter().yearFraction( bd,
					 Utils.inflationPeriod( d, ii.frequency() ).Key );
				double calc = bf * Math.Pow( 1 + z, t );
				if ( t <= 0 )
					calc = ii.fixing( d, false ); // still historical
				if ( Math.Abs( calc - ii.fixing( d, true ) ) / 10000.0 > eps )
					Assert.Fail( "ZC index does not forecast correctly for date " + d
								+ " from base date " + bd
								+ " with fixing " + bf
								+ ", correct:  " + calc
								+ ", fix: " + ii.fixing( d, true )
								+ ", t " + t );
			}

			//===========================================================================================
			// Test zero-inflation-indexed (i.e. cpi ratio) cashflow
			// just ordinary indexed cashflow with a zero inflation index

			Date baseDate = new Date( 1, Month.January, 2006 );
			Date fixDate = new Date( 1, Month.August, 2014 );
			Date payDate = new UnitedKingdom().adjust( fixDate + new Period( 3, TimeUnit.Months ), BusinessDayConvention.ModifiedFollowing );
			Index ind = ii as Index;
         Utils.QL_REQUIRE( ind != null, () => "dynamic_pointer_cast to Index from InflationIndex failed" );

			double notional = 1000000.0;//1m
			IndexedCashFlow iicf = new IndexedCashFlow( notional, ind, baseDate, fixDate, payDate );
			double correctIndexed = ii.fixing( iicf.fixingDate() ) / ii.fixing( iicf.baseDate() );
			double calculatedIndexed = iicf.amount() / iicf.notional();
			Assert.IsTrue( Math.Abs( correctIndexed - calculatedIndexed ) < eps,
								  "IndexedCashFlow indexing wrong: " + calculatedIndexed + " vs correct = "
								  + correctIndexed );

			//===========================================================================================
			// Test zero coupon swap

			// first make one ...

			ZeroInflationIndex zii = ii as ZeroInflationIndex;
         Utils.QL_REQUIRE( zii != null, () => "dynamic_pointer_cast to ZeroInflationIndex from UKRPI failed" );
			ZeroCouponInflationSwap nzcis =
				new ZeroCouponInflationSwap( ZeroCouponInflationSwap.Type.Payer,
													 1000000.0,
													 evaluationDate,
													 zcData[6].date,    // end date = maturity
													 calendar, bdc, dc, zcData[6].rate / 100.0, // fixed rate
													 zii, observationLag );

			// N.B. no coupon pricer because it is not a coupon, effect of inflation curve via
			//      inflation curve attached to the inflation index.
			Handle<YieldTermStructure> hTS = new Handle<YieldTermStructure>( nominalTS );
			IPricingEngine sppe = new DiscountingSwapEngine( hTS );
			nzcis.setPricingEngine( sppe );

			// ... and price it, should be zero
			Assert.IsTrue( Math.Abs( nzcis.NPV() ) < 0.00001, "ZCIS does not reprice to zero "
							  + nzcis.NPV()
							  + evaluationDate + " to " + zcData[6].date + " becoming " + nzcis.maturityDate()
							  + " rate " + zcData[6].rate
							  + " fixed leg " + nzcis.legNPV( 0 )
							  + " indexed-predicted inflated leg " + nzcis.legNPV( 1 )
							  + " discount " + nominalTS.discount( nzcis.maturityDate() ) );


			//===========================================================================================
			// Test multiplicative seasonality in price
			//

			//Seasonality factors NOT normalized
			//and UKRPI is not interpolated
			Date trueBaseDate = Utils.inflationPeriod( hz.link.baseDate(), ii.frequency() ).Value;
			Date seasonallityBaseDate = new Date( 31, Month.January, trueBaseDate.year() );
			List<double> seasonalityFactors = new List<double>( 12 );
			seasonalityFactors.Add( 1.003245 );
			seasonalityFactors.Add( 1.000000 );
			seasonalityFactors.Add( 0.999715 );
			seasonalityFactors.Add( 1.000495 );
			seasonalityFactors.Add( 1.000929 );
			seasonalityFactors.Add( 0.998687 );
			seasonalityFactors.Add( 0.995949 );
			seasonalityFactors.Add( 0.994682 );
			seasonalityFactors.Add( 0.995949 );
			seasonalityFactors.Add( 1.000519 );
			seasonalityFactors.Add( 1.003705 );
			seasonalityFactors.Add( 1.004186 );

			//Creating two different seasonality objects
			//
			MultiplicativePriceSeasonality seasonality_1 = new MultiplicativePriceSeasonality();
			InitializedList<double> seasonalityFactors_1 = new InitializedList<double>( 12, 1.0 );
			seasonality_1.set( seasonallityBaseDate, Frequency.Monthly, seasonalityFactors_1 );

			MultiplicativePriceSeasonality seasonality_real =
				new MultiplicativePriceSeasonality( seasonallityBaseDate, Frequency.Monthly, seasonalityFactors );
			//Testing seasonality correction when seasonality factors are = 1
			//
			double[] fixing = {
            ii.fixing(new Date(14,Month.January  ,2013),true),
            ii.fixing(new Date(14,Month.February ,2013),true),
            ii.fixing(new Date(14,Month.March    ,2013),true),
            ii.fixing(new Date(14,Month.April    ,2013),true),
            ii.fixing(new Date(14,Month.May      ,2013),true),
            ii.fixing(new Date(14,Month.June     ,2013),true),
            ii.fixing(new Date(14,Month.July     ,2013),true),
            ii.fixing(new Date(14,Month.August   ,2013),true),
            ii.fixing(new Date(14,Month.September,2013),true),
            ii.fixing(new Date(14,Month.October  ,2013),true),
            ii.fixing(new Date(14,Month.November ,2013),true),
            ii.fixing(new Date(14,Month.December ,2013),true)
         };

			hz.link.setSeasonality( seasonality_1 );
         Utils.QL_REQUIRE( hz.link.hasSeasonality(), () => "[44] incorrectly believes NO seasonality correction" );

			double[] seasonalityFixing_1 = {
            ii.fixing(new Date(14,Month.January  ,2013),true),
            ii.fixing(new Date(14,Month.February ,2013),true),
            ii.fixing(new Date(14,Month.March    ,2013),true),
            ii.fixing(new Date(14,Month.April    ,2013),true),
            ii.fixing(new Date(14,Month.May      ,2013),true),
            ii.fixing(new Date(14,Month.June     ,2013),true),
            ii.fixing(new Date(14,Month.July     ,2013),true),
            ii.fixing(new Date(14,Month.August   ,2013),true),
            ii.fixing(new Date(14,Month.September,2013),true),
            ii.fixing(new Date(14,Month.October  ,2013),true),
            ii.fixing(new Date(14,Month.November ,2013),true),
            ii.fixing(new Date(14,Month.December ,2013),true)
         };

			for ( int i = 0; i < 12; i++ )
			{
				if ( Math.Abs( fixing[i] - seasonalityFixing_1[i] ) > eps )
				{
					Assert.Fail( "Seasonality doesn't work correctly when seasonality factors are set = 1" );
				}
			}

			//Testing seasonality correction when seasonality factors are different from 1
			//
			//0.998687 is the seasonality factor corresponding to June (the base CPI curve month)
			//
			double[] expectedFixing = {
            ii.fixing(new Date(14,Month.January  ,2013),true) * 1.003245/0.998687,
            ii.fixing(new Date(14,Month.February ,2013),true) * 1.000000/0.998687,
            ii.fixing(new Date(14,Month.March    ,2013),true) * 0.999715/0.998687,
            ii.fixing(new Date(14,Month.April    ,2013),true) * 1.000495/0.998687,
            ii.fixing(new Date(14,Month.May      ,2013),true) * 1.000929/0.998687,
            ii.fixing(new Date(14,Month.June     ,2013),true) * 0.998687/0.998687,
            ii.fixing(new Date(14,Month.July     ,2013),true) * 0.995949/0.998687,
            ii.fixing(new Date(14,Month.August   ,2013),true) * 0.994682/0.998687,
            ii.fixing(new Date(14,Month.September,2013),true) * 0.995949/0.998687,
            ii.fixing(new Date(14,Month.October  ,2013),true) * 1.000519/0.998687,
            ii.fixing(new Date(14,Month.November ,2013),true) * 1.003705/0.998687,
            ii.fixing(new Date(14,Month.December ,2013),true) * 1.004186/0.998687
         };

			hz.link.setSeasonality( seasonality_real );

			double[] seasonalityFixing_real = {
            ii.fixing(new Date(14,Month.January  ,2013),true),
            ii.fixing(new Date(14,Month.February ,2013),true),
            ii.fixing(new Date(14,Month.March    ,2013),true),
            ii.fixing(new Date(14,Month.April    ,2013),true),
            ii.fixing(new Date(14,Month.May      ,2013),true),
            ii.fixing(new Date(14,Month.June     ,2013),true),
            ii.fixing(new Date(14,Month.July     ,2013),true),
            ii.fixing(new Date(14,Month.August   ,2013),true),
            ii.fixing(new Date(14,Month.September,2013),true),
            ii.fixing(new Date(14,Month.October  ,2013),true),
            ii.fixing(new Date(14,Month.November ,2013),true),
            ii.fixing(new Date(14,Month.December ,2013),true)
         };

			for ( int i = 0; i < 12; i++ )
			{
				if ( Math.Abs( expectedFixing[i] - seasonalityFixing_real[i] ) > 0.01 )
				{
					Assert.Fail( "Seasonality doesn't work correctly when considering seasonality factors != 1 "
									+ expectedFixing[i] + " vs " + seasonalityFixing_real[i] );
				}
			}


			//Testing Unset function
			//
         Utils.QL_REQUIRE( hz.link.hasSeasonality(), () => "[4] incorrectly believes NO seasonality correction" );
			hz.link.setSeasonality();
         Utils.QL_REQUIRE( !hz.link.hasSeasonality(), () => "[5] incorrectly believes HAS seasonality correction" );

			double[] seasonalityFixing_unset = {
            ii.fixing(new Date(14,Month.January  ,2013),true),
            ii.fixing(new Date(14,Month.February ,2013),true),
            ii.fixing(new Date(14,Month.March    ,2013),true),
            ii.fixing(new Date(14,Month.April    ,2013),true),
            ii.fixing(new Date(14,Month.May      ,2013),true),
            ii.fixing(new Date(14,Month.June     ,2013),true),
            ii.fixing(new Date(14,Month.July     ,2013),true),
            ii.fixing(new Date(14,Month.August   ,2013),true),
            ii.fixing(new Date(14,Month.September,2013),true),
            ii.fixing(new Date(14,Month.October  ,2013),true),
            ii.fixing(new Date(14,Month.November ,2013),true),
            ii.fixing(new Date(14,Month.December ,2013),true)
         };

			for ( int i = 0; i < 12; i++ )
			{
				if ( Math.Abs( seasonalityFixing_unset[i] - seasonalityFixing_1[i] ) > eps )
				{
					Assert.Fail( "UnsetSeasonality doesn't work correctly "
									+ seasonalityFixing_unset[i] + " vs " + seasonalityFixing_1[i] );
				}
			}


			//==============================================================================
			// now do an INTERPOLATED index, i.e. repeat everything on a fake version of
			// UKRPI (to save making another term structure)

			bool interpYES = true;
			UKRPI iiUKRPIyes = new UKRPI( interpYES, hz );
			for ( int i = 0; i < fixData.Length; i++ )
			{
				iiUKRPIyes.addFixing( rpiSchedule[i], fixData[i] );
			}

			ZeroInflationIndex iiyes = iiUKRPIyes as ZeroInflationIndex;

			// now build the zero inflation curve
			// same data, bigger lag or it will be a self-contradiction
			Period observationLagyes = new Period( 3, TimeUnit.Months );
			List<BootstrapHelper<ZeroInflationTermStructure>> helpersyes =
				makeHelpers( zcData, zcData.Length,
				iiyes, observationLagyes, calendar, bdc, dc );

			PiecewiseZeroInflationCurve<Linear> pZITSyes =
					new PiecewiseZeroInflationCurve<Linear>(
					evaluationDate, calendar, dc, observationLagyes,
					frequency, iiyes.interpolated(), baseZeroRate,
					new Handle<YieldTermStructure>( nominalTS ), helpersyes );
			pZITSyes.recalculate();

			// first check that the zero rates on the curve match the data
			// and that the helpers give the correct impled rates
			forceLinearInterpolation = false;   // still
			for ( int i = 0; i < zcData.Length; i++ )
			{
				Assert.IsTrue( Math.Abs( zcData[i].rate / 100.0
								- pZITSyes.zeroRate( zcData[i].date, observationLagyes, forceLinearInterpolation ) ) < eps,
								"ZITS INTERPOLATED zeroRate != instrument "
								+ pZITSyes.zeroRate( zcData[i].date, observationLagyes, forceLinearInterpolation )
								+ " date " + zcData[i].date + " observationLagyes " + observationLagyes
								+ " vs " + zcData[i].rate / 100.0
								+ " interpolation: " + iiyes.interpolated()
								+ " forceLinearInterpolation " + forceLinearInterpolation );
				Assert.IsTrue( Math.Abs( helpersyes[i].impliedQuote()
									- zcData[i].rate / 100.0 ) < eps,
								"ZITS INTERPOLATED implied quote != instrument "
								+ helpersyes[i].impliedQuote()
								+ " vs " + zcData[i].rate / 100.0 );
			}


			//======================================================================================
			// now test the forecasting capability of the index.
			hz.linkTo( pZITSyes );
			from = hz.link.baseDate() + new Period( 1, TimeUnit.Months ); // to avoid historical linear bit for rest of base month
			to = hz.link.maxDate() - new Period( 1, TimeUnit.Months ); // a bit of margin for adjustments
			testIndex = new MakeSchedule().from( from ).to( to )
			.withTenor( new Period( 1, TimeUnit.Months ) )
			.withCalendar( new UnitedKingdom() )
			.withConvention( BusinessDayConvention.ModifiedFollowing ).value();

			// we are testing UKRPI which is FAKE interpolated for testing here
			bd = hz.link.baseDate();
			bf = iiyes.fixing( bd );
			for ( int i = 0; i < testIndex.Count; i++ )
			{
				Date d = testIndex[i];
				double z = hz.link.zeroRate( d, new Period( 0, TimeUnit.Days ) );
				double t = hz.link.dayCounter().yearFraction( bd, d );
				double calc = bf * Math.Pow( 1 + z, t );
				if ( t <= 0 ) calc = iiyes.fixing( d ); // still historical
				if ( Math.Abs( calc - iiyes.fixing( d ) ) > eps )
					Assert.Fail( "ZC INTERPOLATED index does not forecast correctly for date " + d
									+ " from base date " + bd
									+ " with fixing " + bf
									+ ", correct:  " + calc
									+ ", fix: " + iiyes.fixing( d )
									+ ", t " + t
									+ ", zero " + z );
			}


			//===========================================================================================
			// Test zero coupon swap

			ZeroInflationIndex ziiyes = iiyes as ZeroInflationIndex;
         Utils.QL_REQUIRE( ziiyes != null, () => "dynamic_pointer_cast to ZeroInflationIndex from UKRPI-I failed" );
			ZeroCouponInflationSwap nzcisyes = new ZeroCouponInflationSwap( ZeroCouponInflationSwap.Type.Payer,
															  1000000.0,
															  evaluationDate,
															  zcData[6].date,    // end date = maturity
															  calendar, bdc, dc, zcData[6].rate / 100.0, // fixed rate
															  ziiyes, observationLagyes );

			// N.B. no coupon pricer because it is not a coupon, effect of inflation curve via
			//      inflation curve attached to the inflation index.
			nzcisyes.setPricingEngine( sppe );

			// ... and price it, should be zero
			Assert.IsTrue( Math.Abs( nzcisyes.NPV() ) < 0.00001, "ZCIS-I does not reprice to zero "
									+ nzcisyes.NPV()
									+ evaluationDate + " to " + zcData[6].date + " becoming " + nzcisyes.maturityDate()
									+ " rate " + zcData[6].rate
									+ " fixed leg " + nzcisyes.legNPV( 0 )
									+ " indexed-predicted inflated leg " + nzcisyes.legNPV( 1 )
									+ " discount " + nominalTS.discount( nzcisyes.maturityDate() )
									);

			// remove circular refernce
			hz.linkTo( new ZeroInflationTermStructure() );


		}

		//===========================================================================================
		// year on year tests, index, termstructure, and swaps
		//===========================================================================================
		[TestMethod()]
		public void testYYIndex()
		{
			// Testing year-on-year inflation indices

			SavedSettings backup = new SavedSettings();
			//IndexHistoryCleaner cleaner = new IndexHistoryCleaner();

			YYEUHICP yyeuhicp = new YYEUHICP( true );
			if ( yyeuhicp.name() != "EU YY_HICP"
				|| yyeuhicp.frequency() != Frequency.Monthly
				|| yyeuhicp.revised()
				|| !yyeuhicp.interpolated()
				|| yyeuhicp.ratio()
				|| yyeuhicp.availabilityLag() != new Period( 1, TimeUnit.Months ) )
			{
				Assert.Fail( "wrong year-on-year EU HICP data ("
							+ yyeuhicp.name() + ", "
							+ yyeuhicp.frequency() + ", "
							+ yyeuhicp.revised() + ", "
							+ yyeuhicp.interpolated() + ", "
							+ yyeuhicp.ratio() + ", "
							+ yyeuhicp.availabilityLag() + ")" );
			}

			YYEUHICPr yyeuhicpr = new YYEUHICPr( true );
			if ( yyeuhicpr.name() != "EU YYR_HICP"
				|| yyeuhicpr.frequency() != Frequency.Monthly
				|| yyeuhicpr.revised()
				|| !yyeuhicpr.interpolated()
				|| !yyeuhicpr.ratio()
				|| yyeuhicpr.availabilityLag() != new Period( 1, TimeUnit.Months ) )
			{
				Assert.Fail( "wrong year-on-year EU HICPr data ("
								+ yyeuhicpr.name() + ", "
								+ yyeuhicpr.frequency() + ", "
								+ yyeuhicpr.revised() + ", "
								+ yyeuhicpr.interpolated() + ", "
								+ yyeuhicpr.ratio() + ", "
								+ yyeuhicpr.availabilityLag() + ")" );
			}

			YYUKRPI yyukrpi = new YYUKRPI( false );
			if ( yyukrpi.name() != "UK YY_RPI"
				|| yyukrpi.frequency() != Frequency.Monthly
				|| yyukrpi.revised()
				|| yyukrpi.interpolated()
				|| yyukrpi.ratio()
				|| yyukrpi.availabilityLag() != new Period( 1, TimeUnit.Months ) )
			{
				Assert.Fail( "wrong year-on-year UK RPI data ("
								+ yyukrpi.name() + ", "
								+ yyukrpi.frequency() + ", "
								+ yyukrpi.revised() + ", "
								+ yyukrpi.interpolated() + ", "
								+ yyukrpi.ratio() + ", "
								+ yyukrpi.availabilityLag() + ")" );
			}

			YYUKRPIr yyukrpir = new YYUKRPIr( false );
			if ( yyukrpir.name() != "UK YYR_RPI"
				|| yyukrpir.frequency() != Frequency.Monthly
				|| yyukrpir.revised()
				|| yyukrpir.interpolated()
				|| !yyukrpir.ratio()
				|| yyukrpir.availabilityLag() != new Period( 1, TimeUnit.Months ) )
			{
				Assert.Fail( "wrong year-on-year UK RPIr data ("
								+ yyukrpir.name() + ", "
								+ yyukrpir.frequency() + ", "
								+ yyukrpir.revised() + ", "
								+ yyukrpir.interpolated() + ", "
								+ yyukrpir.ratio() + ", "
								+ yyukrpir.availabilityLag() + ")" );
			}


			// Retrieval test.
			//----------------
			// make sure of the evaluation date
			Date evaluationDate = new Date( 13, Month.August, 2007 );
			evaluationDate = new UnitedKingdom().adjust( evaluationDate );
			Settings.setEvaluationDate( evaluationDate );

			// fixing data
			Date from = new Date( 1, Month.January, 2005 );
			Date to = new Date( 13, Month.August, 2007 );
			Schedule rpiSchedule = new MakeSchedule().from( from ).to( to )
			.withTenor( new Period( 1, TimeUnit.Months ) )
			.withCalendar( new UnitedKingdom() )
			.withConvention( BusinessDayConvention.ModifiedFollowing ).value();

			double[] fixData = { 189.9, 189.9, 189.6, 190.5, 191.6, 192.0,
            192.2, 192.2, 192.6, 193.1, 193.3, 193.6,
            194.1, 193.4, 194.2, 195.0, 196.5, 197.7,
            198.5, 198.5, 199.2, 200.1, 200.4, 201.1,
            202.7, 201.6, 203.1, 204.4, 205.4, 206.2,
            207.3 };

			bool interp = false;
			YYUKRPIr iir = new YYUKRPIr( interp );
			YYUKRPIr iirYES = new YYUKRPIr( true );
			for ( int i = 0; i < fixData.Length; i++ )
			{
				iir.addFixing( rpiSchedule[i], fixData[i] );
				iirYES.addFixing( rpiSchedule[i], fixData[i] );
			}

			Date todayMinusLag = evaluationDate - iir.availabilityLag();
			KeyValuePair<Date, Date> lim0 = Utils.inflationPeriod( todayMinusLag, iir.frequency() );
			todayMinusLag = lim0.Value + 1 - 2 * new Period( iir.frequency() );

			double eps = 1.0e-8;

			// Interpolation tests
			//--------------------
			// (no TS so can't forecast).
			for ( int i = 13; i < rpiSchedule.Count; i++ )
			{
				KeyValuePair<Date, Date> lim = Utils.inflationPeriod( rpiSchedule[i], iir.frequency() );
				KeyValuePair<Date, Date> limBef = Utils.inflationPeriod( rpiSchedule[i - 12], iir.frequency() );
				for ( Date d = lim.Key; d <= lim.Value; d++ )
				{
					if ( d < todayMinusLag )
					{
						double expected = fixData[i] / fixData[i - 12] - 1.0;
						double calculated = iir.fixing( d );
						Assert.IsTrue( Math.Abs( calculated - expected ) < eps,
												"Non-interpolated fixings not constant within a period: "
												+ calculated
												+ ", should be "
												+ expected );

						double dp = lim.Value + 1 - lim.Key;
						double dpBef = limBef.Value + 1 - limBef.Key;
						double dl = d - lim.Key;
						// potentially does not work on 29th Feb
						double dlBef = new NullCalendar().advance( d, -new Period( 1, TimeUnit.Years ),
							BusinessDayConvention.ModifiedFollowing ) - limBef.Key;

						double linearNow = fixData[i] + ( fixData[i + 1] - fixData[i] ) * dl / dp;
						double linearBef = fixData[i - 12] + ( fixData[i + 1 - 12] - fixData[i - 12] ) * dlBef / dpBef;
						double expectedYES = linearNow / linearBef - 1.0;
						double calculatedYES = iirYES.fixing( d );
						Assert.IsTrue( Math.Abs( expectedYES - calculatedYES ) < eps,
												"Error in interpolated fixings: expect " + expectedYES
												+ " see " + calculatedYES
												+ " flat " + calculated
												+ ", data: " + fixData[i - 12] + ", " + fixData[i + 1 - 12]
												+ ", " + fixData[i] + ", " + fixData[i + 1]
												+ ", fac: " + dp + ", " + dl
												+ ", " + dpBef + ", " + dlBef
												+ ", to: " + linearNow + ", " + linearBef
												);
					}
				}
			}
		}

		[TestMethod()]
		public void testYYTermStructure() 
		{
			// Testing year-on-year inflation term structure...

			SavedSettings backup = new SavedSettings();
			//IndexHistoryCleaner cleaner;

			// try the YY UK
			Calendar calendar = new UnitedKingdom();
			BusinessDayConvention bdc = BusinessDayConvention.ModifiedFollowing;
			Date evaluationDate = new Date(13, Month.August, 2007);
			evaluationDate = calendar.adjust(evaluationDate);
			Settings.setEvaluationDate(evaluationDate);


			// fixing data
			Date from = new Date(1, Month.January, 2005);
			Date to = new Date(13, Month.August, 2007);
			Schedule rpiSchedule = new MakeSchedule().from(from).to(to)
			.withTenor(new Period(1,TimeUnit.Months))
			.withCalendar(new UnitedKingdom())
			.withConvention(BusinessDayConvention.ModifiedFollowing).value();
			double[] fixData = { 189.9, 189.9, 189.6, 190.5, 191.6, 192.0,
				192.2, 192.2, 192.6, 193.1, 193.3, 193.6,
				194.1, 193.4, 194.2, 195.0, 196.5, 197.7,
				198.5, 198.5, 199.2, 200.1, 200.4, 201.1,
				202.7, 201.6, 203.1, 204.4, 205.4, 206.2,
				207.3 };

			RelinkableHandle<YoYInflationTermStructure> hy = new RelinkableHandle<YoYInflationTermStructure>();
			bool interp = false;
			YYUKRPIr iir = new YYUKRPIr(interp, hy);
			for (int i=0; i<fixData.Length; i++) 
			{
				iir.addFixing(rpiSchedule[i], fixData[i]);
			}

			YieldTermStructure nominalTS = nominalTermStructure();

			// now build the YoY inflation curve
			Datum[] yyData = {
				new Datum( new Date(13, Month.August, 2008), 2.95 ),
				new Datum( new Date(13, Month.August, 2009), 2.95 ),
				new Datum( new Date(13, Month.August, 2010), 2.93 ),
				new Datum( new Date(15, Month.August, 2011), 2.955 ),
				new Datum( new Date(13, Month.August, 2012), 2.945 ),
				new Datum( new Date(13, Month.August, 2013), 2.985 ),
				new Datum( new Date(13, Month.August, 2014), 3.01 ),
				new Datum( new Date(13, Month.August, 2015), 3.035 ),
				new Datum( new Date(13, Month.August, 2016), 3.055 ),  // note that
				new Datum( new Date(13, Month.August, 2017), 3.075 ),  // some dates will be on
				new Datum( new Date(13, Month.August, 2019), 3.105 ),  // holidays but the payment
				new Datum( new Date(15, Month.August, 2022), 3.135 ),  // calendar will roll them
				new Datum( new Date(13, Month.August, 2027), 3.155 ),
				new Datum( new Date(13, Month.August, 2032), 3.145 ),
				new Datum( new Date(13, Month.August, 2037), 3.145 )
			};

			Period observationLag = new Period(2,TimeUnit.Months);
			DayCounter dc = new Thirty360();

			// now build the helpers ...
			List<BootstrapHelper<YoYInflationTermStructure>> helpers =
			makeHelpers (yyData, yyData.Length, iir,observationLag, calendar, bdc, dc);

			double baseYYRate = yyData[0].rate/100.0;
			PiecewiseYoYInflationCurve<Linear> pYYTS = new PiecewiseYoYInflationCurve<Linear>(
							evaluationDate, calendar, dc, observationLag,
							iir.frequency(),iir.interpolated(), baseYYRate,
							new Handle<YieldTermStructure>(nominalTS), helpers);
			pYYTS.recalculate();

			// validation
			// yoy swaps should reprice to zero
			// yy rates should not equal yySwap rates
			double eps = 0.000001;
			// usual swap engine
			Handle<YieldTermStructure> hTS = new Handle<YieldTermStructure>(nominalTS);
			IPricingEngine sppe = new DiscountingSwapEngine(hTS);

			// make sure that the index has the latest yoy term structure
			hy.linkTo(pYYTS);

			for (int j = 1; j < yyData.Length; j++) 
			{

				from = nominalTS.referenceDate();
				to = yyData[j].date;
				Schedule yoySchedule = new MakeSchedule().from(from).to(to)
				.withConvention(BusinessDayConvention.Unadjusted) // fixed leg gets calendar from
				.withCalendar(calendar)     // schedule
				.withTenor(new Period(1,TimeUnit.Years)).value(); // .back

				YearOnYearInflationSwap yyS2 = new YearOnYearInflationSwap(
					YearOnYearInflationSwap.Type.Payer,
					1000000.0,
					yoySchedule,//fixed schedule, but same as yoy
					yyData[j].rate/100.0,
					dc,
					yoySchedule,
					iir,
					observationLag,
					0.0,        //spread on index
					dc,
					new UnitedKingdom());

				yyS2.setPricingEngine(sppe);



				Assert.IsTrue(Math.Abs(yyS2.NPV())<eps,"fresh yoy swap NPV!=0 from TS "
							+"swap quote for pt " + j
							+ ", is " + yyData[j].rate/100.0
							+" vs YoY rate "+ pYYTS.yoyRate(yyData[j].date-observationLag)
							+" at quote date "+(yyData[j].date-observationLag)
							+", NPV of a fresh yoy swap is " + yyS2.NPV()
							+"\n      fair rate " + yyS2.fairRate()
							+" payment "+yyS2.paymentConvention());
			}

			int jj=3;
			for (int k = 0; k < 14; k++) 
			{
				from = nominalTS.referenceDate() - new Period(k,TimeUnit.Months);
				to = yyData[jj].date - new Period(k,TimeUnit.Months);
				Schedule yoySchedule = new MakeSchedule().from(from).to(to)
				.withConvention(BusinessDayConvention.Unadjusted) // fixed leg gets calendar from
				.withCalendar(calendar)     // schedule
				.withTenor(new Period(1,TimeUnit.Years))
				.value(); //backwards()

				YearOnYearInflationSwap yyS3 = new YearOnYearInflationSwap(
					YearOnYearInflationSwap.Type.Payer,
					1000000.0,
					yoySchedule,//fixed schedule, but same as yoy
					yyData[jj].rate/100.0,
					dc,
					yoySchedule,
					iir,
					observationLag,
					0.0,        //spread on index
					dc,
					new UnitedKingdom());

				yyS3.setPricingEngine(sppe);

				Assert.IsTrue(Math.Abs(yyS3.NPV())< 20000.0,
											"unexpected size of aged YoY swap, aged "
											+ k +" months: YY aged NPV = " + yyS3.NPV()
											+", legs "+ yyS3.legNPV(0) + " and " + yyS3.legNPV(1)
											);
			}
			// remove circular refernce
			hy.linkTo( new YoYInflationTermStructure());
	}


   }
}
