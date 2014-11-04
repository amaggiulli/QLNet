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
	public class T_InflationCapFlooredCouponTest
	{
		class CommonVars
		{
		   // common data
		   public int length;
         public Date startDate;
         public double volatility;

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
            nominals = new List<double>(){1000000};
            frequency = Frequency.Annual;
            // usual setup
            volatility = 0.01;
            length = 7;
            calendar = new UnitedKingdom();
            convention = BusinessDayConvention.ModifiedFollowing;
            Date today = new Date(13, Month.August, 2007);
            evaluationDate = calendar.adjust(today);
            Settings.setEvaluationDate(evaluationDate);
            settlementDays = 0;
            fixingDays = 0;
            settlement = calendar.advance(today,settlementDays,TimeUnit.Days);
            startDate = settlement;
            dc = new Thirty360();

            // yoy index
            //      fixing data
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
            List<BootstrapHelper<YoYInflationTermStructure> > helpers =
            makeHelpers(yyData, yyData.Length, iir,
                               observationLag,
                               calendar, convention, dc);

            double baseYYRate = yyData[0].rate/100.0;
            PiecewiseYoYInflationCurve<Linear>  pYYTS = new PiecewiseYoYInflationCurve<Linear>(
                                evaluationDate, calendar, dc, observationLag,
                                iir.frequency(),iir.interpolated(), baseYYRate,
                                new Handle<YieldTermStructure>(nominalTS), helpers);
            pYYTS.recalculate();
            yoyTS = pYYTS as YoYInflationTermStructure;


            // make sure that the index has the latest yoy term structure
            hy.linkTo(pYYTS);
        }

         // utilities
         public List<CashFlow> makeYoYLeg(Date startDate, int length,double gearing = 1.0,double spread = 0.0) 
         {
            YoYInflationIndex ii = iir as YoYInflationIndex;
            Date endDate = calendar.advance(startDate,new Period(length,TimeUnit.Years),BusinessDayConvention.Unadjusted);
            Schedule schedule = new Schedule(startDate, endDate, new Period(frequency), calendar,
                              BusinessDayConvention.Unadjusted,
                              BusinessDayConvention.Unadjusted,// ref periods & acc periods
                              DateGeneration.Rule.Forward, false);

            InitializedList<double> gearingVector = new InitializedList<double>(length, gearing);
            InitializedList<double> spreadVector = new InitializedList<double>(length, spread);

            return new yoyInflationLeg(schedule, calendar, ii, observationLag)
            .withPaymentDayCounter(dc)
            .withGearings(gearingVector)
            .withSpreads(spreadVector)
            .withNotionals(nominals)
            .withPaymentAdjustment(convention);
        }
		
         public List<CashFlow> makeFixedLeg(Date startDate,int length) 
         {
            Date endDate = calendar.advance(startDate, length, TimeUnit.Years, convention);
            Schedule schedule = new Schedule(startDate, endDate, new Period(frequency), calendar,
                              convention, convention,
                              DateGeneration.Rule.Forward, false);
            InitializedList<double> coupons = new InitializedList<double>(length, 0.0);
            return new FixedRateLeg(schedule)
            .withCouponRates(coupons, dc)
            .withNotionals(nominals);
        }
      
         public List<CashFlow> makeYoYCapFlooredLeg(int which, Date startDate,
                              int length,
                              List<double> caps,
                              List<double> floors,
                              double volatility,
                              double gearing = 1.0,
                              double spread = 0.0) 
         {
         
            Handle<YoYOptionletVolatilitySurface> vol = new Handle<YoYOptionletVolatilitySurface>(
               new ConstantYoYOptionletVolatility(volatility,
                                settlementDays,
                                calendar,
                                convention,
                                dc,
                                observationLag,
                                frequency,
                                iir.interpolated()));

            YoYInflationCouponPricer pricer = null;
            switch (which) {
                case 0:
                    pricer = new BlackYoYInflationCouponPricer(vol);
                    break;
                case 1:
                    pricer = new UnitDisplacedBlackYoYInflationCouponPricer(vol);
                    break;
                case 2:
                    pricer = new BachelierYoYInflationCouponPricer(vol);
                    break;
                default:
                    Assert.Fail("unknown coupon pricer request: which = "+which
                               +"should be 0=Black,1=DD,2=Bachelier");
                    break;
            }


            InitializedList<double> gearingVector = new InitializedList<double>(length, gearing);
            InitializedList<double> spreadVector = new InitializedList<double>(length, spread);

            YoYInflationIndex ii = iir as YoYInflationIndex;
            Date endDate = calendar.advance(startDate,new Period(length,TimeUnit.Years),BusinessDayConvention.Unadjusted);
            Schedule schedule = new Schedule(startDate, endDate, new Period(frequency), calendar,
                              BusinessDayConvention.Unadjusted,
                              BusinessDayConvention.Unadjusted,// ref periods & acc periods
                              DateGeneration.Rule.Forward, false);

            List<CashFlow> yoyLeg =  new yoyInflationLeg(schedule, calendar, ii, observationLag)
            .withPaymentDayCounter(dc)
            .withGearings(gearingVector)
            .withSpreads(spreadVector)
            .withCaps(caps)
            .withFloors(floors)
            .withNotionals(nominals)
            .withPaymentAdjustment(convention);

            for(int i=0; i<yoyLeg.Count; i++) 
            {
                ((YoYInflationCoupon)(yoyLeg[i])).setPricer(pricer);
            }

            //setCouponPricer(iborLeg, pricer);
            return yoyLeg;
        }
      
         
         public IPricingEngine makeEngine(double volatility, int which) 
         {

            YoYInflationIndex  yyii = iir as YoYInflationIndex;

            Handle<YoYOptionletVolatilitySurface> vol = new Handle<YoYOptionletVolatilitySurface>(
                    new ConstantYoYOptionletVolatility(volatility,
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
                    result = new YoYInflationFloor(leg, new List<double>(){ strike});
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

            List<BootstrapHelper<YoYInflationTermStructure>> instruments = 
               new List<BootstrapHelper<YoYInflationTermStructure>>();

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
      public void testDecomposition() 
      {
         // Testing collared coupon against its decomposition...

         CommonVars vars= new CommonVars();

         double tolerance = 1e-10;
         double npvVanilla,npvCappedLeg,npvFlooredLeg,npvCollaredLeg,npvCap,npvFloor,npvCollar;
         double error;
         double floorstrike = 0.05;
         double capstrike = 0.10;
         InitializedList<double> caps = new InitializedList<double>(vars.length,capstrike);
         List<double> caps0 = new List<double>();
         InitializedList<double> floors = new InitializedList<double>(vars.length,floorstrike);
         List<double> floors0 = new List<double>();
         double gearing_p = 0.5;
         double spread_p = 0.002;
         double gearing_n = -1.5;
         double spread_n = 0.12;
         // fixed leg with zero rate
         List<CashFlow> fixedLeg  = vars.makeFixedLeg(vars.startDate,vars.length);
         // floating leg with gearing=1 and spread=0
         List<CashFlow> floatLeg  = vars.makeYoYLeg(vars.startDate,vars.length);
         // floating leg with positive gearing (gearing_p) and spread<>0
         List<CashFlow> floatLeg_p = vars.makeYoYLeg(vars.startDate,vars.length,gearing_p,spread_p);
         // floating leg with negative gearing (gearing_n) and spread<>0
         List<CashFlow> floatLeg_n = vars.makeYoYLeg(vars.startDate,vars.length,gearing_n,spread_n);
         // Swap with null fixed leg and floating leg with gearing=1 and spread=0
         Swap vanillaLeg = new Swap(fixedLeg,floatLeg);
         // Swap with null fixed leg and floating leg with positive gearing and spread<>0
         Swap vanillaLeg_p = new Swap(fixedLeg,floatLeg_p);
         // Swap with null fixed leg and floating leg with negative gearing and spread<>0
         Swap vanillaLeg_n = new Swap(fixedLeg,floatLeg_n);

         IPricingEngine engine = new DiscountingSwapEngine(vars.nominalTS);

         vanillaLeg.setPricingEngine(engine);    // here use the autoset feature
         vanillaLeg_p.setPricingEngine(engine);
         vanillaLeg_n.setPricingEngine(engine);

         // CAPPED coupon - Decomposition of payoff
         // Payoff = Nom * Min(rate,strike) * accrualperiod =
         // = Nom * [rate + Min(0,strike-rate)] * accrualperiod =
         // = Nom * rate * accrualperiod - Nom * Max(rate-strike,0) * accrualperiod =
         // = VanillaFloatingLeg - Call
         //

         int whichPricer = 0;

         // Case gearing = 1 and spread = 0
         List<CashFlow> cappedLeg = vars.makeYoYCapFlooredLeg(whichPricer,vars.startDate,vars.length,
                              caps,floors0,vars.volatility);
         Swap capLeg = new Swap(fixedLeg,cappedLeg);
         capLeg.setPricingEngine(engine);
         YoYInflationCap cap = new YoYInflationCap(floatLeg, new List<double>(){capstrike});
         cap.setPricingEngine(vars.makeEngine(vars.volatility,whichPricer));
         npvVanilla = vanillaLeg.NPV();
         npvCappedLeg = capLeg.NPV();
         npvCap = cap.NPV();
         error = Math.Abs(npvCappedLeg - (npvVanilla-npvCap));
         if (error>tolerance) 
         {
            Assert.Fail("\nYoY Capped Leg: gearing=1, spread=0%, strike=" + capstrike*100 +
                        "%\n" +
                        "  Capped Floating Leg NPV: " + npvCappedLeg + "\n" +
                        "  Floating Leg NPV - Cap NPV: " + (npvVanilla - npvCap) + "\n" +
                        "  Diff: " + error );
         }

         // gearing = 1 and spread = 0
         // FLOORED coupon - Decomposition of payoff
         // Payoff = Nom * Max(rate,strike) * accrualperiod =
         // = Nom * [rate + Max(0,strike-rate)] * accrualperiod =
         // = Nom * rate * accrualperiod + Nom * Max(strike-rate,0) * accrualperiod =
         // = VanillaFloatingLeg + Put
         //

         List<CashFlow> flooredLeg = vars.makeYoYCapFlooredLeg(whichPricer,vars.startDate,vars.length,
                              caps0,floors,vars.volatility);
         Swap floorLeg = new Swap(fixedLeg,flooredLeg);
         floorLeg.setPricingEngine(engine);
         YoYInflationFloor floor= new YoYInflationFloor(floatLeg, new List<double>(){floorstrike});
         floor.setPricingEngine(vars.makeEngine(vars.volatility,whichPricer));
         npvFlooredLeg = floorLeg.NPV();
         npvFloor = floor.NPV();
         error = Math.Abs(npvFlooredLeg-(npvVanilla + npvFloor));
         if (error>tolerance) 
         {
            Assert.Fail("YoY Floored Leg: gearing=1, spread=0%, strike=" + floorstrike *100 +
                        "%\n" +
                        "  Floored Floating Leg NPV: " + npvFlooredLeg + "\n" +
                        "  Floating Leg NPV + Floor NPV: " + (npvVanilla + npvFloor) + "\n" +
                        "  Diff: " + error );
         }

         // gearing = 1 and spread = 0
         // COLLARED coupon - Decomposition of payoff
         // Payoff = Nom * Min(strikem,Max(rate,strikeM)) * accrualperiod =
         // = VanillaFloatingLeg - Collar
         //

         List<CashFlow> collaredLeg = vars.makeYoYCapFlooredLeg(whichPricer,vars.startDate,vars.length,
                              caps,floors,vars.volatility);
         Swap collarLeg = new Swap(fixedLeg,collaredLeg);
         collarLeg.setPricingEngine(engine);
         YoYInflationCollar collar = new YoYInflationCollar(floatLeg,
                     new List<double>(){capstrike},
                     new List<double>(){floorstrike});
         collar.setPricingEngine(vars.makeEngine(vars.volatility,whichPricer));
         npvCollaredLeg = collarLeg.NPV();
         npvCollar = collar.NPV();
         error = Math.Abs(npvCollaredLeg -(npvVanilla - npvCollar));
         if (error>tolerance) 
         {
            Assert.Fail("\nYoY Collared Leg: gearing=1, spread=0%, strike=" +
                        floorstrike*100 + "% and " + capstrike*100 + "%\n" +
                        "  Collared Floating Leg NPV: " + npvCollaredLeg + "\n" +
                        "  Floating Leg NPV - Collar NPV: " + (npvVanilla - npvCollar) + "\n" +
                        "  Diff: " + error );
         }

         // gearing = a and spread = b
         // CAPPED coupon - Decomposition of payoff
         // Payoff
         // = Nom * Min(a*rate+b,strike) * accrualperiod =
         // = Nom * [a*rate+b + Min(0,strike-a*rate-b)] * accrualperiod =
         // = Nom * a*rate+b * accrualperiod + Nom * Min(strike-b-a*rate,0) * accrualperiod
         // --> If a>0 (assuming positive effective strike):
         // Payoff = VanillaFloatingLeg - Call(a*rate+b,strike)
         // --> If a<0 (assuming positive effective strike):
         // Payoff = VanillaFloatingLeg + Nom * Min(strike-b+|a|*rate+,0) * accrualperiod =
         // = VanillaFloatingLeg + Put(|a|*rate+b,strike)
         //

         // Positive gearing
         List<CashFlow> cappedLeg_p = vars.makeYoYCapFlooredLeg(whichPricer,vars.startDate,vars.length,caps,floors0,
                              vars.volatility,gearing_p,spread_p);
         Swap capLeg_p = new Swap(fixedLeg,cappedLeg_p);
         capLeg_p.setPricingEngine(engine);
         YoYInflationCap cap_p = new YoYInflationCap(floatLeg_p,new List<double>(){capstrike});
         cap_p.setPricingEngine(vars.makeEngine(vars.volatility,whichPricer));
         npvVanilla = vanillaLeg_p.NPV();
         npvCappedLeg = capLeg_p.NPV();
         npvCap = cap_p.NPV();
         error = Math.Abs(npvCappedLeg - (npvVanilla-npvCap));
         if (error>tolerance) 
         {
            Assert.Fail("\nYoY Capped Leg: gearing=" + gearing_p + ", " +
                        "spread= " + spread_p *100 +
                        "%, strike=" + capstrike*100  + "%, " +
                        "effective strike= " + (capstrike-spread_p)/gearing_p*100 +
                        "%\n" +
                        "  Capped Floating Leg NPV: " + npvCappedLeg + "\n" +
                        "  Vanilla Leg NPV: " + npvVanilla + "\n" +
                        "  Cap NPV: " + npvCap + "\n" +
                        "  Floating Leg NPV - Cap NPV: " + (npvVanilla - npvCap) + "\n" +
                        "  Diff: " + error );
         }

         // Negative gearing
         List<CashFlow> cappedLeg_n = vars.makeYoYCapFlooredLeg(whichPricer,vars.startDate,vars.length,caps,floors0,
                              vars.volatility,gearing_n,spread_n);
         Swap capLeg_n = new Swap(fixedLeg,cappedLeg_n);
         capLeg_n.setPricingEngine(engine);
         YoYInflationFloor floor_n = new YoYInflationFloor(floatLeg,new List<double>(){(capstrike-spread_n)/gearing_n});
         floor_n.setPricingEngine(vars.makeEngine(vars.volatility,whichPricer));
         npvVanilla = vanillaLeg_n.NPV();
         npvCappedLeg = capLeg_n.NPV();
         npvFloor = floor_n.NPV();
         error = Math.Abs(npvCappedLeg - (npvVanilla+ gearing_n*npvFloor));
         if (error>tolerance) 
         {
            Assert.Fail("\nYoY Capped Leg: gearing=" + gearing_n + ", " +
                        "spread= " + spread_n *100 +
                        "%, strike=" + capstrike*100  + "%, " +
                        "effective strike= " + ((capstrike-spread_n)/gearing_n*100) +
                        "%\n" +
                        "  Capped Floating Leg NPV: " + npvCappedLeg + "\n" +
                        "  npv Vanilla: " + npvVanilla + "\n" +
                        "  npvFloor: " + npvFloor + "\n" +
                        "  Floating Leg NPV - Cap NPV: " + (npvVanilla + gearing_n*npvFloor) + "\n" +
                        "  Diff: " + error );
         }

         // gearing = a and spread = b
         // FLOORED coupon - Decomposition of payoff
         // Payoff
         // = Nom * Max(a*rate+b,strike) * accrualperiod =
         // = Nom * [a*rate+b + Max(0,strike-a*rate-b)] * accrualperiod =
         // = Nom * a*rate+b * accrualperiod + Nom * Max(strike-b-a*rate,0) * accrualperiod
         // --> If a>0 (assuming positive effective strike):
         // Payoff = VanillaFloatingLeg + Put(a*rate+b,strike)
         // --> If a<0 (assuming positive effective strike):
         // Payoff = VanillaFloatingLeg + Nom * Max(strike-b+|a|*rate+,0) * accrualperiod =
         // = VanillaFloatingLeg - Call(|a|*rate+b,strike)
         //

         // Positive gearing
         List<CashFlow> flooredLeg_p1 = vars.makeYoYCapFlooredLeg(whichPricer,vars.startDate,vars.length,caps0,floors,
                              vars.volatility,gearing_p,spread_p);
         Swap floorLeg_p1 = new Swap(fixedLeg,flooredLeg_p1);
         floorLeg_p1.setPricingEngine(engine);
         YoYInflationFloor floor_p1 = new YoYInflationFloor(floatLeg_p,new List<double>(){floorstrike});
         floor_p1.setPricingEngine(vars.makeEngine(vars.volatility,whichPricer));
         npvVanilla = vanillaLeg_p.NPV();
         npvFlooredLeg = floorLeg_p1.NPV();
         npvFloor = floor_p1.NPV();
         error = Math.Abs(npvFlooredLeg - (npvVanilla+npvFloor));
         if (error>tolerance) 
         {
            Assert.Fail("\nYoY Floored Leg: gearing=" + gearing_p + ", "
                        + "spread= " + spread_p *100+ "%, strike=" + floorstrike *100 + "%, "
                        + "effective strike= " + (floorstrike-spread_p)/gearing_p*100
                        + "%\n" +
                        "  Floored Floating Leg NPV: "    + npvFlooredLeg
                        + "\n" +
                        "  Floating Leg NPV + Floor NPV: " + (npvVanilla + npvFloor)
                        + "\n" +
                        "  Diff: " + error );
         }
         // Negative gearing
         List<CashFlow> flooredLeg_n = vars.makeYoYCapFlooredLeg(whichPricer,vars.startDate,vars.length,caps0,floors,
                              vars.volatility,gearing_n,spread_n);
         Swap floorLeg_n = new Swap(fixedLeg,flooredLeg_n);
         floorLeg_n.setPricingEngine(engine);
         YoYInflationCap cap_n = new YoYInflationCap(floatLeg,new List<double>(){(floorstrike-spread_n)/gearing_n});
         cap_n.setPricingEngine(vars.makeEngine(vars.volatility,whichPricer));
         npvVanilla = vanillaLeg_n.NPV();
         npvFlooredLeg = floorLeg_n.NPV();
         npvCap = cap_n.NPV();
         error = Math.Abs(npvFlooredLeg - (npvVanilla - gearing_n*npvCap));
         if (error>tolerance) 
         {
            Assert.Fail("\nYoY Capped Leg: gearing=" + gearing_n + ", " +
                        "spread= " + spread_n *100 +
                        "%, strike=" + floorstrike*100  + "%, " +
                        "effective strike= " + (floorstrike-spread_n)/gearing_n*100 +
                        "%\n" +
                        "  Capped Floating Leg NPV: " + npvFlooredLeg + "\n" +
                        "  Floating Leg NPV - Cap NPV: " + (npvVanilla - gearing_n*npvCap) + "\n" +
                        "  Diff: " + error );
         }
         // gearing = a and spread = b
         // COLLARED coupon - Decomposition of payoff
         // Payoff = Nom * Min(caprate,Max(a*rate+b,floorrate)) * accrualperiod
         // --> If a>0 (assuming positive effective strike):
         // Payoff = VanillaFloatingLeg - Collar(a*rate+b, floorrate, caprate)
         // --> If a<0 (assuming positive effective strike):
         // Payoff = VanillaFloatingLeg + Collar(|a|*rate+b, caprate, floorrate)
         //
         // Positive gearing
         List<CashFlow> collaredLeg_p = vars.makeYoYCapFlooredLeg(whichPricer,vars.startDate,vars.length,caps,floors,
                              vars.volatility,gearing_p,spread_p);
         Swap collarLeg_p1 = new Swap(fixedLeg,collaredLeg_p);
         collarLeg_p1.setPricingEngine(engine);
         YoYInflationCollar collar_p = new YoYInflationCollar(floatLeg_p,
                        new List<double>(){capstrike},
                        new List<double>(){floorstrike});
         collar_p.setPricingEngine(vars.makeEngine(vars.volatility,whichPricer));
         npvVanilla = vanillaLeg_p.NPV();
         npvCollaredLeg = collarLeg_p1.NPV();
         npvCollar = collar_p.NPV();
         error = Math.Abs(npvCollaredLeg - (npvVanilla - npvCollar));
         if (error>tolerance) 
         {
            Assert.Fail("\nYoY Collared Leg: gearing=" + gearing_p + ", "
                        + "spread= " + spread_p*100 + "%, strike="
                        + floorstrike*100 + "% and " + capstrike*100
                        + "%, "
                        + "effective strike=" + (floorstrike-spread_p)/gearing_p*100
                        +  "% and " + (capstrike-spread_p)/gearing_p*100
                        + "%\n" +
                        "  Collared Floating Leg NPV: "    + npvCollaredLeg
                        + "\n" +
                        "  Floating Leg NPV - Collar NPV: " + (npvVanilla - npvCollar)
                        + "\n" +
                        "  Diff: " + error );
         }
         // Negative gearing
         List<CashFlow> collaredLeg_n = vars.makeYoYCapFlooredLeg(whichPricer,vars.startDate,vars.length,caps,floors,
                              vars.volatility,gearing_n,spread_n);
         Swap collarLeg_n1 = new Swap(fixedLeg,collaredLeg_n);
         collarLeg_n1.setPricingEngine(engine);
         YoYInflationCollar collar_n = new YoYInflationCollar(floatLeg,
                        new List<double>(){(floorstrike-spread_n)/gearing_n},
                        new List<double>(){(capstrike-spread_n)/gearing_n});
         collar_n.setPricingEngine(vars.makeEngine(vars.volatility,whichPricer));
         npvVanilla = vanillaLeg_n.NPV();
         npvCollaredLeg = collarLeg_n1.NPV();
         npvCollar = collar_n.NPV();
         error = Math.Abs(npvCollaredLeg - (npvVanilla - gearing_n*npvCollar));
         if (error>tolerance) 
         {
            Assert.Fail("\nYoY Collared Leg: gearing=" + gearing_n + ", "
                        + "spread= " + spread_n*100 + "%, strike="
                        + floorstrike*100 + "% and " + capstrike*100
                        + "%, "
                        + "effective strike=" + (floorstrike-spread_n)/gearing_n*100
                        +  "% and " + (capstrike-spread_n)/gearing_n*100
                        + "%\n" +
                        "  Collared Floating Leg NPV: "    + npvCollaredLeg
                        + "\n" +
                        "  Floating Leg NPV - Collar NPV: " + (npvVanilla - gearing_n*npvCollar)
                        + "\n" +
                        "  Diff: " + error );
         }
         // remove circular refernce
         vars.hy.linkTo(new YoYInflationTermStructure());
   }

      [TestMethod()]
      public void testInstrumentEquality() 
      {

         // Testing inflation capped/floored coupon against inflation capfloor instrument...

         CommonVars vars = new CommonVars();

         int[] lengths = { 1, 2, 3, 5, 7, 10, 15, 20 };
         // vol is low ...
         double[] strikes = { 0.01, 0.025, 0.029, 0.03, 0.031, 0.035, 0.07 };
         // yoy inflation vol is generally very low
         double[] vols = { 0.001, 0.005, 0.010, 0.015, 0.020 };

         // this is model independent
         // capped coupon = fwd - cap, and fwd = swap(0)
         // floored coupon = fwd + floor
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
                        .withCalendar(new UnitedKingdom())
                        .withConvention(BusinessDayConvention.Unadjusted)
                        .backwards().value();

                        YearOnYearInflationSwap swap = new YearOnYearInflationSwap(YearOnYearInflationSwap.Type.Payer,
                                                         1000000.0,
                                                         yoySchedule,//fixed schedule, but same as yoy
                                                         0.0,//strikes[j],
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

                        List<CashFlow> leg2 = vars.makeYoYCapFlooredLeg(whichPricer, from,
                                                            lengths[i],
                                                            new InitializedList<double>(lengths[i],strikes[j]),//cap
                                                            new List<double>(),//floor
                                                            vols[k],
                                                            1.0,   // gearing
                                                            0.0);// spread

                        List<CashFlow> leg3 = vars.makeYoYCapFlooredLeg(whichPricer, from,
                                                            lengths[i],
                                                            new List<double>(),// cap
                                                            new InitializedList<double>(lengths[i],strikes[j]),//floor
                                                            vols[k],
                                                            1.0,   // gearing
                                                            0.0);// spread

                        // N.B. nominals are 10e6
                        double capped = CashFlows.npv(leg2,vars.nominalTS,false);
                        if ( Math.Abs(capped - (swap.NPV() - cap.NPV())) > 1.0e-6) 
                        {
                           Assert.Fail(
                                       "capped coupon != swap(0) - cap:\n"
                                       + "    length:      " + lengths[i] + " years\n"
                                       + "    volatility:  " + vols[k] + "\n"
                                       + "    strike:      " + strikes[j] + "\n"
                                       + "    cap value:   " + cap.NPV() + "\n"
                                       + "    swap value:  " + swap.NPV() + "\n"
                                       + "   capped coupon " + capped);
                        }


                        // N.B. nominals are 10e6
                        double floored = CashFlows.npv(leg3,vars.nominalTS,false);
                        if ( Math.Abs(floored - (swap.NPV() + floor.NPV())) > 1.0e-6) 
                        {
                           Assert.Fail(
                                       "floored coupon != swap(0) + floor :\n"
                                       + "    length:      " + lengths[i] + " years\n"
                                       + "    volatility:  " + vols[k] + "\n"
                                       + "    strike:      " + strikes[j] + "\n"
                                       + "    floor value: " + floor.NPV() + "\n"
                                       + "    swap value:  " + swap.NPV() + "\n"
                                       + "  floored coupon " + floored);
                        }
                     }
               }
            }
         
         }
         // remove circular refernce
         vars.hy.linkTo(new YoYInflationTermStructure());
      }
   }
}
