/*
 Copyright (C) 2008-2014 Andrea Maggiulli (a.maggiulli@gmail.com)
 
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
	public class T_InflationCapFloorTest
	{

		class CommonVars 
		{
			// common data

			public Frequency frequency;
			public List<double> nominals;
			public Calendar calendar;
			public BusinessDayConvention convention;
			public int fixingDays;
			public Date evaluationDate;
			public int settlementDays;
			public Date settlement;
			public Period observationLag = new Period(0,TimeUnit.Months);
			public DayCounter dc;
			public YYUKRPIr iir;

			public RelinkableHandle<YieldTermStructure> nominalTS = new RelinkableHandle<YieldTermStructure>();
			public YoYInflationTermStructure yoyTS;
			public RelinkableHandle<YoYInflationTermStructure> hy = new RelinkableHandle<YoYInflationTermStructure>();

			// cleanup

			SavedSettings backup = new SavedSettings();

			// setup
			public CommonVars() 
			{
				// option variables
				nominals = new List<double>{1000000};
				frequency = Frequency.Annual;
				// usual setup
				calendar = new UnitedKingdom();
				convention = BusinessDayConvention.ModifiedFollowing;
				Date today = new Date(13, Month.August, 2007);
				evaluationDate = calendar.adjust(today);
				Settings.setEvaluationDate(evaluationDate);
				settlementDays = 0;
				fixingDays = 0;
				settlement = calendar.advance(today,settlementDays,TimeUnit.Days);
				dc = new Thirty360();

				// yoy index
				//      fixing data
				Date from = new Date(1, Month.January, 2005);
				Date to = new Date(13, Month.August, 2007);
				Schedule rpiSchedule = new MakeSchedule().from(from).to(to)
				.withConvention(BusinessDayConvention.ModifiedFollowing)
				.withCalendar(new UnitedKingdom())
				.withTenor(new Period(1,TimeUnit.Months)).value();
				double[] fixData = { 189.9, 189.9, 189.6, 190.5, 191.6, 192.0,
						192.2, 192.2, 192.6, 193.1, 193.3, 193.6,
						194.1, 193.4, 194.2, 195.0, 196.5, 197.7,
						198.5, 198.5, 199.2, 200.1, 200.4, 201.1,
						202.7, 201.6, 203.1, 204.4, 205.4, 206.2,
						207.3, -999.0, -999 };
				// link from yoy index to yoy TS
				bool interp = false;
				iir = new YYUKRPIr(interp, hy);
				for (int i=0; i<rpiSchedule.Count;i++) 
				{
						iir.addFixing(rpiSchedule[i], fixData[i]);
				}

				YieldTermStructure nominalFF = new FlatForward(evaluationDate, 0.05, new ActualActual());
				nominalTS.linkTo(nominalFF);

				// now build the YoY inflation curve
				Period observationLag = new Period(2,TimeUnit.Months);

				Datum[] yyData =  {
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

				// now build the helpers ...
				List<BootstrapHelper<YoYInflationTermStructure>> helpers =
				makeHelpers(yyData, yyData.Length, iir,
											observationLag,
											calendar, convention, dc);

				double baseYYRate = yyData[0].rate/100.0;
				PiecewiseYoYInflationCurve<Linear>  pYYTS = 
						new PiecewiseYoYInflationCurve<Linear>(
								evaluationDate, calendar, dc, observationLag,
								iir.frequency(),iir.interpolated(), baseYYRate,
								new Handle<YieldTermStructure>(nominalTS), helpers);
				pYYTS.recalculate();
				yoyTS = pYYTS as YoYInflationTermStructure;


				// make sure that the index has the latest yoy term structure
				hy.linkTo(pYYTS);
			}

			// utilities
			public List<CashFlow> makeYoYLeg(Date startDate, int length) 
			{
				YoYInflationIndex ii = iir as YoYInflationIndex;
				Date endDate = calendar.advance(startDate,new Period(length,TimeUnit.Years),BusinessDayConvention.Unadjusted);
				Schedule schedule = new Schedule(startDate, endDate, new Period(frequency), calendar,
										BusinessDayConvention.Unadjusted,
										BusinessDayConvention.Unadjusted,// ref periods & acc periods
										DateGeneration.Rule.Forward, false);
            return new yoyInflationLeg( schedule, calendar, ii, observationLag )
				.withPaymentDayCounter(dc)
				.withNotionals(nominals)
				.withPaymentAdjustment(convention);
			}


			public IPricingEngine makeEngine(double volatility,int which) 
			{

				YoYInflationIndex	yyii = iir as YoYInflationIndex;

				Handle<YoYOptionletVolatilitySurface> vol = 
					new Handle<YoYOptionletVolatilitySurface>(new ConstantYoYOptionletVolatility(volatility,
																			settlementDays,
																			calendar,
																			convention,
																			dc,
																			observationLag,
																			frequency,
																			iir.interpolated()));


				switch (which) {
						case 0:
							return new YoYInflationBlackCapFloorEngine(iir, vol);
							//break;
						case 1:
							return new YoYInflationUnitDisplacedBlackCapFloorEngine(iir, vol);
							//break;
						case 2:
							return new YoYInflationBachelierCapFloorEngine(iir, vol);
							//break;
						default:
							Assert.Fail("unknown engine request: which = "+which
											+"should be 0=Black,1=DD,2=Bachelier");
							break;
				}
				// make compiler happy
				Utils.QL_FAIL("never get here - no engine resolution");
				return null;
			}


			public YoYInflationCapFloor makeYoYCapFloor(CapFloorType type,
																	List<CashFlow> leg,
																	double strike,
																	double volatility,
																	int which) 
			{
				YoYInflationCapFloor result = null;
				switch (type) 
				{
					case CapFloorType.Cap:
						result = new YoYInflationCap(leg, new List<double>(){strike});
						break;
					case CapFloorType.Floor:
						result = new YoYInflationFloor( leg, new List<double>() { strike } );
						break;
					default:
						Utils.QL_FAIL("unknown YoYInflation cap/floor type");
						break;
				}
				result.setPricingEngine(makeEngine(volatility, which));
				return result;
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
		}

		[TestMethod()]
		public void testConsistency()
		{
			// Testing consistency between yoy inflation cap,floor and collar...
			CommonVars vars = new CommonVars();

			int[] lengths = { 1, 2, 3, 5, 7, 10, 15, 20 };
			double[] cap_rates = { 0.01, 0.025, 0.029, 0.03, 0.031, 0.035, 0.07 };
			double[] floor_rates = { 0.01, 0.025, 0.029, 0.03, 0.031, 0.035, 0.07 };
			double[] vols = { 0.001, 0.005, 0.010, 0.015, 0.020 };

			for (int whichPricer = 0; whichPricer < 3; whichPricer++) 
			{
				for (int i=0; i<lengths.Length; i++) 
				{
					for (int j=0; j<cap_rates.Length; j++) 
					{
						for (int k=0; k<floor_rates.Length; k++) 
						{
							for (int l=0; l<vols.Length; l++) 
							{

								List<CashFlow> leg = vars.makeYoYLeg(vars.evaluationDate,lengths[i]);

								YoYInflationCapFloor cap = vars.makeYoYCapFloor(CapFloorType.Cap,
                                           leg, cap_rates[j], vols[l], whichPricer);

								YoYInflationCapFloor floor = vars.makeYoYCapFloor(CapFloorType.Floor,
                                           leg, floor_rates[k], vols[l], whichPricer);

								YoYInflationCollar collar = new YoYInflationCollar(leg,new List<double>(){cap_rates[j]},
                                  new List<double>(){floor_rates[k]});
								
								collar.setPricingEngine(vars.makeEngine(vols[l], whichPricer));

								if (Math.Abs((cap.NPV()-floor.NPV())-collar.NPV()) > 1e-6) 
								{
									Assert.Fail(
                                   "inconsistency between cap, floor and collar:\n"
                                   + "    length:       " + lengths[i] + " years\n"
                                   + "    volatility:   " +  "\n"
                                   + "    cap value:    " + cap.NPV()
                                   + " at strike: " + "\n"
                                   + "    floor value:  " + floor.NPV()
                                   + " at strike: " +  "\n"
                                   + "    collar value: " + collar.NPV());

								}
								// test re-composition by optionlets, N.B. ONE per year
								double capletsNPV = 0.0;
								List<YoYInflationCapFloor> caplets = new List<YoYInflationCapFloor>();
								for (int m=0; m<lengths[i]*1; m++) 
								{
									caplets.Add(cap.optionlet(m));
									caplets[m].setPricingEngine(vars.makeEngine(vols[l], whichPricer));
									capletsNPV += caplets[m].NPV();
								}

								if (Math.Abs(cap.NPV() - capletsNPV) > 1e-6) 
								{
									Assert.Fail(
                                    "sum of caplet NPVs does not equal cap NPV:\n"
                                    + "    length:       " + lengths[i] + " years\n"
                                    + "    volatility:   " +  "\n"
                                    + "    cap value:    " + cap.NPV()
                                    + " at strike: " +  "\n"
                                    + "    sum of caplets value:  " + capletsNPV
                                    + " at strike (first): " + caplets[0].capRates()[0] + "\n"
                                    );
								}

								double floorletsNPV = 0.0;
								List<YoYInflationCapFloor>  floorlets = new List<YoYInflationCapFloor>();
								for (int m=0; m<lengths[i]*1; m++) 
								{
									floorlets.Add(floor.optionlet(m));
									floorlets[m].setPricingEngine(vars.makeEngine(vols[l], whichPricer));
									floorletsNPV += floorlets[m].NPV();
								}

								if (Math.Abs(floor.NPV() - floorletsNPV) > 1e-6) 
								{
									Assert.Fail(
                                    "sum of floorlet NPVs does not equal floor NPV:\n"
                                    + "    length:       " + lengths[i] + " years\n"
                                    + "    volatility:   " +  "\n"
                                    + "    cap value:    " + floor.NPV()
                                    + " at strike: " + floor_rates[j] + "\n"
                                    + "    sum of floorlets value:  " + floorletsNPV
                                    + " at strike (first): " + floorlets[0].floorRates()[0] + "\n"
                                    );
								}

								double collarletsNPV = 0.0;
								List<YoYInflationCapFloor>  collarlets = new List<YoYInflationCapFloor>();
								for (int m=0; m<lengths[i]*1; m++) 
								{
									collarlets.Add(collar.optionlet(m));
									collarlets[m].setPricingEngine(vars.makeEngine(vols[l], whichPricer));
									collarletsNPV += collarlets[m].NPV();
								}

								if (Math.Abs(collar.NPV() - collarletsNPV) > 1e-6) 
								{
									Assert.Fail(
                                    "sum of collarlet NPVs does not equal floor NPV:\n"
                                    + "    length:       " + lengths[i] + " years\n"
                                    + "    volatility:   " + vols[l] + "\n"
                                    + "    cap value:    " + collar.NPV()
                                    + " at strike floor: " + floor_rates[j]
                                    + " at strike cap: " + cap_rates[j] + "\n"
                                    + "    sum of collarlets value:  " + collarletsNPV
                                    + " at strike floor (first): " + collarlets[0].floorRates()[0]
                                    + " at strike cap (first): " + collarlets[0].capRates()[0] + "\n"
                                    );
							}
							}
						}
					}
				}
			} // pricer loop
			// remove circular refernce
			vars.hy.linkTo(new YoYInflationTermStructure());
		}

		// Test inflation cap/floor parity, i.e. that cap-floor = swap, note that this
		// is different from nominal because in nominal world standard cap/floors do
		// not have the first optionlet.  This is because they set in advance so
		// there is no point.  However, yoy inflation generally sets in arrears,
		// (actually in arrears with a lag of a few months) thus the first optionlet
		// is relevant.  Hence we can do a parity test without a special definition
		// of the YoY cap/floor instrument.
		[TestMethod()]
		public void testParity() 
		{

			// Testing yoy inflation cap/floor parity...

			 CommonVars vars = new CommonVars();

			 int[] lengths = { 1, 2, 3, 5, 7, 10, 15, 20 };
			 // vol is low ...
			 double[] strikes = { 0.0, 0.025, 0.029, 0.03, 0.031, 0.035, 0.07 };
			 // yoy inflation vol is generally very low
			 double[] vols = { 0.001, 0.005, 0.010, 0.015, 0.020 };

			 // cap-floor-swap parity is model-independent
			 for (int whichPricer = 0; whichPricer < 3; whichPricer++) {
				  for (int i=0; i<lengths.Length; i++) {
						for (int j=0; j<strikes.Length; j++) {
							 for (int k=0; k<vols.Length; k++) {

								  List<CashFlow> leg = vars.makeYoYLeg(vars.evaluationDate,lengths[i]);

								  Instrument cap = vars.makeYoYCapFloor(CapFloorType.Cap,
															leg, strikes[j], vols[k], whichPricer);

								  Instrument floor = vars.makeYoYCapFloor(CapFloorType.Floor,
															leg, strikes[j], vols[k], whichPricer);

								  Date from = vars.nominalTS.link.referenceDate();
								  Date to = from+new Period(lengths[i],TimeUnit.Years);
								  Schedule yoySchedule = new MakeSchedule().from(from).to(to)
								  .withTenor(new Period(1,TimeUnit.Years))
								  .withConvention(BusinessDayConvention.Unadjusted)
								  .withCalendar(new UnitedKingdom()).backwards().value();

								  YearOnYearInflationSwap swap = new YearOnYearInflationSwap
									  (YearOnYearInflationSwap.Type.Payer,
										1000000.0,
										yoySchedule,//fixed schedule, but same as yoy
										strikes[j],
										vars.dc,
										yoySchedule,
										vars.iir,
										vars.observationLag,
										0.0,        //spread on index
										vars.dc,
										new UnitedKingdom());

								  Handle<YieldTermStructure> hTS = new Handle<YieldTermStructure>(vars.nominalTS);
								  IPricingEngine sppe = new DiscountingSwapEngine(hTS);
								  swap.setPricingEngine(sppe);

								  // N.B. nominals are 10e6
								  if (Math.Abs((cap.NPV()-floor.NPV()) - swap.NPV()) > 1.0e-6) {
										Assert.Fail(
												 "put/call parity violated:\n"
												 + "    length:      " + lengths[i] + " years\n"
												 + "    volatility:  " + vols[k] + "\n"
												 + "    strike:      " + strikes[j] + "\n"
												 + "    cap value:   " + cap.NPV() + "\n"
												 + "    floor value: " + floor.NPV() + "\n"
												 + "    swap value:  " + swap.NPV());
								  }
							 }
						}
				  }
			 }
			 // remove circular refernce
			 vars.hy.linkTo(new YoYInflationTermStructure());
		}

		
		[TestMethod()]
		public void testCachedValue() 
		{
			// Testing Black yoy inflation cap/floor price  against cached values...
			CommonVars vars = new CommonVars();

			int whichPricer = 0; // black

			double K = 0.0295; // one centi-point is fair rate error i.e. < 1 cp
			int j = 2;
			List<CashFlow> leg = vars.makeYoYLeg(vars.evaluationDate,j);
			Instrument cap = vars.makeYoYCapFloor(CapFloorType.Cap,leg, K, 0.01, whichPricer);

			Instrument floor = vars.makeYoYCapFloor(CapFloorType.Floor,leg, K, 0.01, whichPricer);


			// close to atm prices
			double cachedCapNPVblack   = 219.452;
			double cachedFloorNPVblack =  314.641;
			// N.B. notionals are 10e6.
			Assert.IsTrue(Math.Abs(cap.NPV()-cachedCapNPVblack)<0.02,"yoy cap cached NPV wrong "
									+cap.NPV()+" should be "+cachedCapNPVblack+" Black pricer"
									+" diff was "+(Math.Abs(cap.NPV()-cachedCapNPVblack)));
			Assert.IsTrue(Math.Abs(floor.NPV()-cachedFloorNPVblack)<0.02,"yoy floor cached NPV wrong "
									+floor.NPV()+" should be "+cachedFloorNPVblack+" Black pricer"
									+" diff was "+(Math.Abs(floor.NPV()-cachedFloorNPVblack)));

			whichPricer = 1; // dd

			cap = vars.makeYoYCapFloor(CapFloorType.Cap,leg, K, 0.01, whichPricer);
			floor = vars.makeYoYCapFloor(CapFloorType.Floor,leg, K, 0.01, whichPricer);

			// close to atm prices
			double cachedCapNPVdd   = 9114.61;
			double cachedFloorNPVdd =  9209.8;
			// N.B. notionals are 10e6.
			Assert.IsTrue(Math.Abs(cap.NPV()-cachedCapNPVdd)<0.22,"yoy cap cached NPV wrong "
									+cap.NPV()+" should be "+cachedCapNPVdd+" dd Black pricer"
									+" diff was "+(Math.Abs(cap.NPV()-cachedCapNPVdd)));
			Assert.IsTrue(Math.Abs(floor.NPV()-cachedFloorNPVdd)<0.22,"yoy floor cached NPV wrong "
									+floor.NPV()+" should be "+cachedFloorNPVdd+" dd Black pricer"
									+" diff was "+(Math.Abs(floor.NPV()-cachedFloorNPVdd)));

			whichPricer = 2; // bachelier

			cap = vars.makeYoYCapFloor(CapFloorType.Cap,leg, K, 0.01, whichPricer);
			floor = vars.makeYoYCapFloor(CapFloorType.Floor,leg, K, 0.01, whichPricer);

			// close to atm prices
			double cachedCapNPVbac   = 8852.4;
			double cachedFloorNPVbac =  8947.59;
			// N.B. notionals are 10e6.
			Assert.IsTrue(Math.Abs(cap.NPV()-cachedCapNPVbac)<0.22,"yoy cap cached NPV wrong "
									+cap.NPV()+" should be "+cachedCapNPVbac+" bac Black pricer"
									+" diff was "+(Math.Abs(cap.NPV()-cachedCapNPVbac)));
			Assert.IsTrue(Math.Abs(floor.NPV()-cachedFloorNPVbac)<0.22,"yoy floor cached NPV wrong "
									+floor.NPV()+" should be "+cachedFloorNPVbac+" bac Black pricer"
									+" diff was "+(Math.Abs(floor.NPV()-cachedFloorNPVbac)));

			// remove circular refernce
			vars.hy.linkTo(new YoYInflationTermStructure());
	}
	
	}
}
