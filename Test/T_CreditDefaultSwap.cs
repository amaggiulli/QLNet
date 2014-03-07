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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QLNet;

namespace TestSuite
{
	[TestClass()]
	public class T_CreditDefaultSwap
	{
		[TestMethod()]
		public void testCachedValue() 
		{
			// Testing credit-default swap against cached values...

			SavedSettings backup = new SavedSettings();

			// Initialize curves
			Settings.setEvaluationDate(new Date(9,Month.June,2006));
			Date today = Settings.evaluationDate();
			Calendar calendar = new TARGET();

			Handle<Quote> hazardRate = new Handle<Quote>(new SimpleQuote(0.01234));
			RelinkableHandle<DefaultProbabilityTermStructure> probabilityCurve = new RelinkableHandle<DefaultProbabilityTermStructure>();
			probabilityCurve.linkTo(new FlatHazardRate(0, calendar, hazardRate, new Actual360()));

			RelinkableHandle<YieldTermStructure> discountCurve = new RelinkableHandle<YieldTermStructure>();

			discountCurve.linkTo(new FlatForward(today,0.06,new Actual360()));

			// Build the schedule
			Date issueDate = calendar.advance(today, -1, TimeUnit.Years);
			Date maturity = calendar.advance(issueDate, 10, TimeUnit.Years);
			Frequency frequency = Frequency.Semiannual;
			BusinessDayConvention convention = BusinessDayConvention.ModifiedFollowing;

			Schedule schedule = new Schedule(issueDate, maturity, new Period(frequency), calendar,
									convention, convention, DateGeneration.Rule.Forward, false);

			// Build the CDS
			double fixedRate = 0.0120;
			DayCounter dayCount = new Actual360();
			double notional = 10000.0;
			double recoveryRate = 0.4;

			CreditDefaultSwap cds = new CreditDefaultSwap(Protection.Side.Seller, notional, fixedRate,
										schedule, convention, dayCount, true, true);
			cds.setPricingEngine(new MidPointCdsEngine(probabilityCurve,recoveryRate,discountCurve));

			double npv = 295.0153398;
			double fairRate = 0.007517539081;

			double calculatedNpv = cds.NPV();
			double calculatedFairRate = cds.fairSpread();
			double tolerance = 1.0e-7;

			if (Math.Abs(calculatedNpv - npv) > tolerance)
				Assert.Fail(
					"Failed to reproduce NPV with mid-point engine\n"
					+ "    calculated NPV: " + calculatedNpv + "\n"
					+ "    expected NPV:   " + npv);

			if (Math.Abs(calculatedFairRate - fairRate) > tolerance)
				Assert.Fail(
					"Failed to reproduce fair rate with mid-point engine\n"
					+ "    calculated fair rate: " + calculatedFairRate + "\n"
					+ "    expected fair rate:   " + fairRate);

			cds.setPricingEngine(new IntegralCdsEngine(new Period(1,TimeUnit.Days),probabilityCurve,
																	recoveryRate,discountCurve));

			calculatedNpv = cds.NPV();
			calculatedFairRate = cds.fairSpread();
			tolerance = 1.0e-5;

			if (Math.Abs(calculatedNpv - npv) > notional*tolerance*10)
				Assert.Fail(
					"Failed to reproduce NPV with integral engine "
					+ "(step = 1 day)\n"
					+ "    calculated NPV: " + calculatedNpv + "\n"
					+ "    expected NPV:   " + npv);

			if (Math.Abs(calculatedFairRate - fairRate) > tolerance)
				Assert.Fail(
					"Failed to reproduce fair rate with integral engine "
					+ "(step = 1 day)\n"
					+ "    calculated fair rate: " + calculatedFairRate + "\n"
					+ "    expected fair rate:   " + fairRate);

			cds.setPricingEngine(new IntegralCdsEngine(new Period(1,TimeUnit.Weeks),probabilityCurve,recoveryRate,discountCurve));

			calculatedNpv = cds.NPV();
			calculatedFairRate = cds.fairSpread();
			tolerance = 1.0e-5;

			if (Math.Abs(calculatedNpv - npv) > notional*tolerance*10)
				Assert.Fail(
					"Failed to reproduce NPV with integral engine "
					+"(step = 1 week)\n"
					+ "    calculated NPV: " + calculatedNpv + "\n"
					+ "    expected NPV:   " + npv);

			if (Math.Abs(calculatedFairRate - fairRate) > tolerance)
				Assert.Fail(
					"Failed to reproduce fair rate with integral engine "
					+"(step = 1 week)\n"
					+ "    calculated fair rate: " + calculatedFairRate + "\n"
					+ "    expected fair rate:   " + fairRate);
		}

		[TestMethod()]
		public void testCachedMarketValue() 
		{
			// Testing credit-default swap against cached market values...

			SavedSettings backup = new SavedSettings();

			Settings.setEvaluationDate(new Date(9,Month.June,2006));
			Date evalDate = Settings.evaluationDate();
			Calendar calendar = new UnitedStates();

			List<Date> discountDates = new List<Date>();
			discountDates.Add(evalDate);
			discountDates.Add(calendar.advance(evalDate, 1, TimeUnit.Weeks,  BusinessDayConvention.ModifiedFollowing));
			discountDates.Add(calendar.advance(evalDate, 1, TimeUnit.Months, BusinessDayConvention.ModifiedFollowing));
			discountDates.Add(calendar.advance(evalDate, 2, TimeUnit.Months, BusinessDayConvention.ModifiedFollowing));
			discountDates.Add(calendar.advance(evalDate, 3, TimeUnit.Months, BusinessDayConvention.ModifiedFollowing));
			discountDates.Add(calendar.advance(evalDate, 6, TimeUnit.Months, BusinessDayConvention.ModifiedFollowing));
			discountDates.Add(calendar.advance(evalDate,12, TimeUnit.Months, BusinessDayConvention.ModifiedFollowing));
			discountDates.Add(calendar.advance(evalDate, 2, TimeUnit.Years, BusinessDayConvention.ModifiedFollowing));
			discountDates.Add(calendar.advance(evalDate, 3, TimeUnit.Years, BusinessDayConvention.ModifiedFollowing));
			discountDates.Add(calendar.advance(evalDate, 4, TimeUnit.Years, BusinessDayConvention.ModifiedFollowing));
			discountDates.Add(calendar.advance(evalDate, 5, TimeUnit.Years, BusinessDayConvention.ModifiedFollowing));
			discountDates.Add(calendar.advance(evalDate, 6, TimeUnit.Years, BusinessDayConvention.ModifiedFollowing));
			discountDates.Add(calendar.advance(evalDate, 7, TimeUnit.Years, BusinessDayConvention.ModifiedFollowing));
			discountDates.Add(calendar.advance(evalDate, 8, TimeUnit.Years, BusinessDayConvention.ModifiedFollowing));
			discountDates.Add(calendar.advance(evalDate, 9, TimeUnit.Years, BusinessDayConvention.ModifiedFollowing));
			discountDates.Add(calendar.advance(evalDate,10, TimeUnit.Years, BusinessDayConvention.ModifiedFollowing));
			discountDates.Add(calendar.advance(evalDate,15, TimeUnit.Years, BusinessDayConvention.ModifiedFollowing));

			List<double> dfs = new List<double>();
			dfs.Add(1.0);
			dfs.Add(0.9990151375768731);
			dfs.Add(0.99570502636871183);
			dfs.Add(0.99118260474528685);
			dfs.Add(0.98661167950906203);
			dfs.Add(0.9732592953359388 );
			dfs.Add(0.94724424481038083);
			dfs.Add(0.89844996737120875 );
			dfs.Add(0.85216647839921411 );
			dfs.Add(0.80775477692556874 );
			dfs.Add(0.76517289234200347 );
			dfs.Add(0.72401019553182933 );
			dfs.Add(0.68503909569219212 );
			dfs.Add(0.64797499814013748 );
			dfs.Add(0.61263171936255534 );
			dfs.Add(0.5791942350748791  );
			dfs.Add(0.43518868769953606 );

			DayCounter curveDayCounter = new Actual360();

			RelinkableHandle<YieldTermStructure> discountCurve = new RelinkableHandle<YieldTermStructure>();
			discountCurve.linkTo( new InterpolatedDiscountCurve<LogLinear>( discountDates, dfs, curveDayCounter,null,null,null,new LogLinear() ) );

			DayCounter dayCounter = new Thirty360();
			List<Date> dates = new List<Date>();
			dates.Add(evalDate);
			dates.Add(calendar.advance(evalDate, 6, TimeUnit.Months, BusinessDayConvention.ModifiedFollowing));
			dates.Add(calendar.advance(evalDate, 1, TimeUnit.Years, BusinessDayConvention.ModifiedFollowing));
			dates.Add(calendar.advance(evalDate, 2, TimeUnit.Years, BusinessDayConvention.ModifiedFollowing));
			dates.Add(calendar.advance(evalDate, 3, TimeUnit.Years, BusinessDayConvention.ModifiedFollowing));
			dates.Add(calendar.advance(evalDate, 4, TimeUnit.Years, BusinessDayConvention.ModifiedFollowing));
			dates.Add(calendar.advance(evalDate, 5, TimeUnit.Years, BusinessDayConvention.ModifiedFollowing));
			dates.Add(calendar.advance(evalDate, 7, TimeUnit.Years, BusinessDayConvention.ModifiedFollowing));
			dates.Add(calendar.advance(evalDate,10, TimeUnit.Years, BusinessDayConvention.ModifiedFollowing));

			List<double> defaultProbabilities = new List<double>();
			defaultProbabilities.Add(0.0000);
			defaultProbabilities.Add(0.0047);
			defaultProbabilities.Add(0.0093);
			defaultProbabilities.Add(0.0286);
			defaultProbabilities.Add(0.0619);
			defaultProbabilities.Add(0.0953);
			defaultProbabilities.Add(0.1508);
			defaultProbabilities.Add(0.2288);
			defaultProbabilities.Add(0.3666);

			List<double> hazardRates = new List<double>();
			hazardRates.Add(0.0);
			for (int i=1; i<dates.Count; ++i) 
			{
				double t1 = dayCounter.yearFraction(dates[0], dates[i-1]);
				double t2 = dayCounter.yearFraction(dates[0], dates[i]);
				double S1 = 1.0 - defaultProbabilities[i-1];
				double S2 = 1.0 - defaultProbabilities[i];
				hazardRates.Add(Math.Log(S1/S2)/(t2-t1));
			}

			RelinkableHandle<DefaultProbabilityTermStructure> piecewiseFlatHazardRate = new RelinkableHandle<DefaultProbabilityTermStructure>();
			piecewiseFlatHazardRate.linkTo(new InterpolatedHazardRateCurve<BackwardFlat>(dates,hazardRates,new Thirty360()));

			// Testing credit default swap

			// Build the schedule
			Date issueDate = new Date(20, Month.March, 2006);
			Date maturity = new Date(20, Month.June, 2013);
			Frequency cdsFrequency = Frequency.Semiannual;
			BusinessDayConvention cdsConvention = BusinessDayConvention.ModifiedFollowing;

			Schedule schedule = new Schedule(issueDate, maturity, new Period(cdsFrequency), calendar,
									               cdsConvention, cdsConvention,
									               DateGeneration.Rule.Forward, false);

			// Build the CDS
			double recoveryRate = 0.25;
			double fixedRate=0.0224;
			DayCounter dayCount= new Actual360();
			double cdsNotional=100.0;

			CreditDefaultSwap cds = new CreditDefaultSwap(Protection.Side.Seller, cdsNotional, fixedRate,
										schedule, cdsConvention, dayCount, true, true);
			cds.setPricingEngine(new MidPointCdsEngine(piecewiseFlatHazardRate, recoveryRate,discountCurve));

			double calculatedNpv = cds.NPV();
			double calculatedFairRate = cds.fairSpread();

			double npv = -1.364048777;        // from Bloomberg we have 98.15598868 - 100.00;
			double fairRate =  0.0248429452; // from Bloomberg we have 0.0258378;

			double tolerance = 1e-9;

			if (Math.Abs(npv - calculatedNpv) > tolerance)
				Assert.Fail(
					"Failed to reproduce the npv for the given credit-default swap\n"
					+ "    computed NPV:  " + calculatedNpv + "\n"
					+ "    Given NPV:     " + npv);

			if ( Math.Abs( fairRate - calculatedFairRate ) > tolerance )
				Assert.Fail( "Failed to reproduce the fair rate for the given credit-default swap\n"
					+ "    computed fair rate:  " + calculatedFairRate + "\n"
					+ "    Given fair rate:     " + fairRate );
		}

		[TestMethod()]
		public void testImpliedHazardRate() 
		{
			// Testing implied hazard-rate for credit-default swaps...

			SavedSettings backup = new SavedSettings();

			// Initialize curves
			Calendar calendar = new TARGET();
			Date today = calendar.adjust(Date.Today);
			Settings.setEvaluationDate(today);

			double h1 = 0.30, h2 = 0.40;
			DayCounter dayCounter = new Actual365Fixed();

			List<Date> dates = new List<Date>(3);
			List<double> hazardRates = new List<double>(3);
			dates.Add(today);
			hazardRates.Add(h1);

			dates.Add(today + new Period(5,TimeUnit.Years));
			hazardRates.Add(h1);

			dates.Add(today + new Period(10,TimeUnit.Years));
			hazardRates.Add(h2);

			RelinkableHandle<DefaultProbabilityTermStructure> probabilityCurve = 
				new RelinkableHandle<DefaultProbabilityTermStructure>();
			probabilityCurve.linkTo(new InterpolatedHazardRateCurve<BackwardFlat>(dates,
																							hazardRates,
																							dayCounter));

			RelinkableHandle<YieldTermStructure> discountCurve = new RelinkableHandle<YieldTermStructure>();
			discountCurve.linkTo(new FlatForward(today,0.03,new Actual360()));


			Frequency frequency = Frequency.Semiannual;
			BusinessDayConvention convention = BusinessDayConvention.ModifiedFollowing;

			Date issueDate = calendar.advance(today, -6, TimeUnit.Months);
			double fixedRate = 0.0120;
			DayCounter cdsDayCount = new Actual360();
			double notional = 10000.0;
			double recoveryRate = 0.4;

			double? latestRate = null;
			for (int n=6; n<=10; ++n) 
			{
				Date maturity = calendar.advance(issueDate, n, TimeUnit.Years);
				Schedule schedule = new Schedule(issueDate, maturity, new Period(frequency), calendar,
										convention, convention,
										DateGeneration.Rule.Forward, false);

				CreditDefaultSwap cds = new CreditDefaultSwap(Protection.Side.Seller, notional, fixedRate,
											schedule, convention, cdsDayCount,true, true);
				cds.setPricingEngine(new MidPointCdsEngine(probabilityCurve, recoveryRate, discountCurve));

				double NPV = cds.NPV();
				double flatRate = cds.impliedHazardRate(NPV, discountCurve,
																	dayCounter,
																	recoveryRate);

				if (flatRate < h1 || flatRate > h2) {
					Assert.Fail("implied hazard rate outside expected range\n"
									+ "    maturity: " + n + " years\n"
									+ "    expected minimum: " + h1 + "\n"
									+ "    expected maximum: " + h2 + "\n"
									+ "    implied rate:     " + flatRate);
				}

				if (n > 6 && flatRate < latestRate) {
					Assert.Fail("implied hazard rate decreasing with swap maturity\n"
									+ "    maturity: " + n + " years\n"
									+ "    previous rate: " + latestRate + "\n"
									+ "    implied rate:  " + flatRate);
				}

				latestRate = flatRate;

				RelinkableHandle<DefaultProbabilityTermStructure> probability = new RelinkableHandle<DefaultProbabilityTermStructure>();
				probability.linkTo(new FlatHazardRate(	today,new Handle<Quote>(new SimpleQuote(flatRate)),dayCounter));

				CreditDefaultSwap cds2 = new CreditDefaultSwap(Protection.Side.Seller, notional, fixedRate,
												schedule, convention, cdsDayCount,true, true);
				cds2.setPricingEngine(new MidPointCdsEngine(probability,recoveryRate,discountCurve));

				double NPV2 = cds2.NPV();
				double tolerance = 1.0;
				if (Math.Abs(NPV-NPV2) > tolerance) {
					Assert.Fail("failed to reproduce NPV with implied rate\n"
									+ "    expected:   " + NPV + "\n"
									+ "    calculated: " + NPV2);
				}
			}
	}

		[TestMethod()]
		public void testFairSpread() 
		{

			// Testing fair-spread calculation for credit-default swaps...

			SavedSettings backup = new SavedSettings();

			// Initialize curves
			Calendar calendar = new TARGET();
			Date today = calendar.adjust(Date.Today);
			Settings.setEvaluationDate(today);

			Handle<Quote> hazardRate =new  Handle<Quote>(new SimpleQuote(0.01234));
			RelinkableHandle<DefaultProbabilityTermStructure> probabilityCurve = 
				new RelinkableHandle<DefaultProbabilityTermStructure>();
			probabilityCurve.linkTo(new FlatHazardRate(0, calendar, hazardRate, new Actual360()));

			RelinkableHandle<YieldTermStructure> discountCurve =
				new RelinkableHandle<YieldTermStructure>();
			discountCurve.linkTo(new FlatForward(today,0.06,new Actual360()));

			// Build the schedule
			Date issueDate = calendar.advance(today, -1, TimeUnit.Years);
			Date maturity = calendar.advance(issueDate, 10, TimeUnit.Years);
			BusinessDayConvention convention = BusinessDayConvention.Following;

			Schedule schedule =
				new MakeSchedule().from(issueDate)
									.to(maturity)
									.withFrequency(Frequency.Quarterly)
									.withCalendar(calendar)
									.withTerminationDateConvention(convention)
									.withRule(DateGeneration.Rule.TwentiethIMM).value();

			// Build the CDS
			double fixedRate = 0.001;
			DayCounter dayCount = new Actual360();
			double notional = 10000.0;
			double recoveryRate = 0.4;

			IPricingEngine engine = new MidPointCdsEngine(probabilityCurve,recoveryRate,discountCurve);

			CreditDefaultSwap cds = new CreditDefaultSwap(Protection.Side.Seller, notional, fixedRate,
										schedule, convention, dayCount, true, true);
			cds.setPricingEngine(engine);

			double fairRate = cds.fairSpread();

			CreditDefaultSwap fairCds = new CreditDefaultSwap(Protection.Side.Seller, notional, fairRate,
											schedule, convention, dayCount, true, true);
			fairCds.setPricingEngine(engine);

			double fairNPV = fairCds.NPV();
			double tolerance = 1e-10;

			if (Math.Abs(fairNPV) > tolerance)
				Assert.Fail(
					"Failed to reproduce null NPV with calculated fair spread\n"
					+ "    calculated spread: " + fairRate + "\n"
					+ "    calculated NPV:    " + fairNPV);
	
		}

		[TestMethod()]
		public void testFairUpfront() 
		{
			// Testing fair-upfront calculation for credit-default swaps...

			SavedSettings backup = new SavedSettings();

			// Initialize curves
			Calendar calendar = new TARGET();
			Date today = calendar.adjust(Date.Today);
			Settings.setEvaluationDate(today);

			Handle<Quote> hazardRate = new Handle<Quote>(new SimpleQuote(0.01234));
			RelinkableHandle<DefaultProbabilityTermStructure> probabilityCurve = 
				new RelinkableHandle<DefaultProbabilityTermStructure>();
			probabilityCurve.linkTo(new FlatHazardRate(0, calendar, hazardRate, new Actual360()));

			RelinkableHandle<YieldTermStructure> discountCurve = 
				new RelinkableHandle<YieldTermStructure>();
			discountCurve.linkTo(new FlatForward(today,0.06,new Actual360()));

			// Build the schedule
			Date issueDate = today;
			Date maturity = calendar.advance(issueDate, 10, TimeUnit.Years);
			BusinessDayConvention convention = BusinessDayConvention.Following;

			Schedule schedule =
				new MakeSchedule().from(issueDate)
									   .to(maturity)
									   .withFrequency(Frequency.Quarterly)
									   .withCalendar(calendar)
									   .withTerminationDateConvention(convention)
									   .withRule(DateGeneration.Rule.TwentiethIMM).value();

			// Build the CDS
			double fixedRate = 0.05;
			double upfront = 0.001;
			DayCounter dayCount = new Actual360();
			double notional = 10000.0;
			double recoveryRate = 0.4;

			IPricingEngine engine = new MidPointCdsEngine(probabilityCurve, recoveryRate,	discountCurve, true);

			CreditDefaultSwap cds = new CreditDefaultSwap(Protection.Side.Seller, notional, upfront, fixedRate,
										schedule, convention, dayCount, true, true);
			cds.setPricingEngine(engine);

			double fairUpfront = cds.fairUpfront();

			CreditDefaultSwap fairCds = new CreditDefaultSwap(Protection.Side.Seller, notional,
											fairUpfront, fixedRate,	schedule, convention, dayCount, true, true);
			fairCds.setPricingEngine(engine);

			double fairNPV = fairCds.NPV();
			double tolerance = 1e-10;

			if (Math.Abs(fairNPV) > tolerance)
				Assert.Fail(
					"Failed to reproduce null NPV with calculated fair upfront\n"
					+ "    calculated upfront: " + fairUpfront + "\n"
					+ "    calculated NPV:     " + fairNPV);

			// same with null upfront to begin with
			upfront = 0.0;
			CreditDefaultSwap cds2 = new CreditDefaultSwap(Protection.Side.Seller, notional, upfront, fixedRate,
										schedule, convention, dayCount, true, true);
			cds2.setPricingEngine(engine);

			fairUpfront = cds2.fairUpfront();

			CreditDefaultSwap fairCds2 = new CreditDefaultSwap(Protection.Side.Seller, notional,
												fairUpfront, fixedRate,	schedule, convention, dayCount, true, true);
			fairCds2.setPricingEngine(engine);

			fairNPV = fairCds2.NPV();

			if (Math.Abs(fairNPV) > tolerance)
				Assert.Fail(
					"Failed to reproduce null NPV with calculated fair upfront\n"
					+ "    calculated upfront: " + fairUpfront + "\n"
					+ "    calculated NPV:     " + fairNPV);
	
		}

	}
}
