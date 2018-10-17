/*
 Copyright (C) 2018 Jean-Camille Tournier (jean-camille.tournier@avivainvestors.com)
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
using System.Threading.Tasks;
using QLNet;

namespace CDS
{
   class CDS
   {
      static void Main(string[] args)
      {
         try
         {
            int example = 0;
            if (args.Length == 2)
               example = Convert.ToInt32(args[1]);

            if (example == 0 || example == 1)
            {
               Console.WriteLine("***** Running example #1 *****");
               example01();
            }

            if (example == 0 || example == 2)
            {
               Console.WriteLine("***** Running example #2 *****");
               example02();
            }

            if (example == 0 || example == 3)
            {
               Console.WriteLine("***** Running example #3 *****");
               example03();
            }

            Console.ReadKey();

            return;
         }
         catch (Exception e)
         {
            Console.WriteLine(e.Message);
            return;
         }
      }

      private static void example01()
      {

         DateTime timer = DateTime.Now;
         Console.WriteLine();

         /*********************
          ***  MARKET DATA  ***
          *********************/

         Calendar calendar = new TARGET();
         Date todaysDate = new Date(15, 05, 2007);

         // must be a business day
         todaysDate = calendar.adjust(todaysDate);

         Settings.setEvaluationDate(todaysDate);

         // dummy curve
         Quote flatRate =  new SimpleQuote(0.01);
         Handle<YieldTermStructure> tsCurve = new Handle<YieldTermStructure>(new FlatForward(todaysDate, new Handle<Quote>(flatRate), new Actual365Fixed()));

         /*
           In Lehmans Brothers "guide to exotic credit derivatives"
           p. 32 there's a simple case, zero flat curve with a flat CDS
           curve with constant market spreads of 150 bp and RR = 50%
           corresponds to a flat 3% hazard rate. The implied 1-year
           survival probability is 97.04% and the 2-years is 94.18%
         */

         // market
         double recovery_rate = 0.5;
         double[] quoted_spreads = new double[] { 0.0150, 0.0150, 0.0150, 0.0150 };
         List<Period> tenors = new List<Period>();
         tenors.Add(new Period(3, TimeUnit.Months));
         tenors.Add(new Period(6, TimeUnit.Months));
         tenors.Add(new Period(1, TimeUnit.Years));
         tenors.Add(new Period(2, TimeUnit.Years));
         List<Date> maturities = new List<Date>();
         for (int i = 0; i < 4; i++)
         {
            maturities.Add(
               calendar.adjust(todaysDate + tenors[i], BusinessDayConvention.Following));
         }

         List<BootstrapHelper<DefaultProbabilityTermStructure>> instruments = new List<BootstrapHelper<DefaultProbabilityTermStructure>>();
         for (int i = 0; i < 4; i++)
         {
            instruments.Add(
               new SpreadCdsHelper(new Handle<Quote>(new SimpleQuote(quoted_spreads[i])),
                                   tenors[i], 0, calendar, Frequency.Quarterly, BusinessDayConvention.Following,
                                   DateGeneration.Rule.TwentiethIMM, new Actual365Fixed(),
                                   recovery_rate, tsCurve));

         }

         // Bootstrap hazard rates
         PiecewiseDefaultCurve<HazardRate, BackwardFlat> hazardRateStructure = new PiecewiseDefaultCurve<HazardRate, BackwardFlat>(
            todaysDate, instruments, new Actual365Fixed());

         Dictionary<Date, double> hr_curve_data = hazardRateStructure.nodes();

         Console.WriteLine("Calibrated hazard rate values: ");
         foreach (KeyValuePair<Date, double> tmp in hr_curve_data)
         {
            Console.WriteLine("hazard rate on " + tmp.Key + " is "
                              + tmp.Value);
         }
         Console.WriteLine();

         Console.WriteLine("Some survival probability values: ");
         Console.WriteLine("1Y survival probability: " + hazardRateStructure.survivalProbability(todaysDate + new Period(1, TimeUnit.Years)));
         Console.WriteLine("               expected: " + (0.9704).ToString());
         Console.WriteLine("2Y survival probability: " + hazardRateStructure.survivalProbability(todaysDate + new Period(2, TimeUnit.Years)));
         Console.WriteLine("               expected: " + (0.9418).ToString());

         Console.WriteLine();
         Console.WriteLine();

         // reprice instruments
         double nominal = 1000000.0;
         Handle<DefaultProbabilityTermStructure> probability = new Handle<DefaultProbabilityTermStructure>(hazardRateStructure);
         IPricingEngine engine = new MidPointCdsEngine(probability, recovery_rate, tsCurve);

         Schedule cdsSchedule = new MakeSchedule()
         .from(todaysDate)
         .to(maturities[0])
         .withFrequency(Frequency.Quarterly)
         .withCalendar(calendar)
         .withTerminationDateConvention(BusinessDayConvention.Unadjusted)
         .withRule(DateGeneration.Rule.TwentiethIMM)
         .value();

         CreditDefaultSwap cds_3m = new CreditDefaultSwap(CreditDefaultSwap.Protection.Side.Seller, nominal, quoted_spreads[0],
                                                          cdsSchedule, BusinessDayConvention.Following, new Actual365Fixed());

         cdsSchedule = new MakeSchedule()
         .from(todaysDate)
         .to(maturities[1])
         .withFrequency(Frequency.Quarterly)
         .withCalendar(calendar)
         .withTerminationDateConvention(BusinessDayConvention.Unadjusted)
         .withRule(DateGeneration.Rule.TwentiethIMM)
         .value();

         CreditDefaultSwap cds_6m = new CreditDefaultSwap(CreditDefaultSwap.Protection.Side.Seller, nominal, quoted_spreads[1],
                                                          cdsSchedule, BusinessDayConvention.Following, new Actual365Fixed());

         cdsSchedule = new MakeSchedule()
         .from(todaysDate)
         .to(maturities[2])
         .withFrequency(Frequency.Quarterly)
         .withCalendar(calendar)
         .withTerminationDateConvention(BusinessDayConvention.Unadjusted)
         .withRule(DateGeneration.Rule.TwentiethIMM)
         .value();

         CreditDefaultSwap cds_1y = new CreditDefaultSwap(CreditDefaultSwap.Protection.Side.Seller, nominal, quoted_spreads[2],
                                                          cdsSchedule, BusinessDayConvention.Following, new Actual365Fixed());

         cdsSchedule = new MakeSchedule()
         .from(todaysDate)
         .to(maturities[3])
         .withFrequency(Frequency.Quarterly)
         .withCalendar(calendar)
         .withTerminationDateConvention(BusinessDayConvention.Unadjusted)
         .withRule(DateGeneration.Rule.TwentiethIMM)
         .value();

         CreditDefaultSwap cds_2y = new CreditDefaultSwap(CreditDefaultSwap.Protection.Side.Seller, nominal, quoted_spreads[3],
                                                          cdsSchedule, BusinessDayConvention.Following, new Actual365Fixed());

         cds_3m.setPricingEngine(engine);
         cds_6m.setPricingEngine(engine);
         cds_1y.setPricingEngine(engine);
         cds_2y.setPricingEngine(engine);

         Console.WriteLine("Repricing of quoted CDSs employed for calibration: ");
         Console.WriteLine("3M fair spread: " + cds_3m.fairSpread().ToString() + "\n"
                           + "   NPV:         " + cds_3m.NPV().ToString() + "\n"
                           + "   default leg: " + cds_3m.defaultLegNPV().ToString() + "\n"
                           + "   coupon leg:  " + cds_3m.couponLegNPV().ToString() + "\n");

         Console.WriteLine("6M fair spread: " + cds_6m.fairSpread() + "\n"
                           + "   NPV:         " + cds_6m.NPV() + "\n"
                           + "   default leg: " + cds_6m.defaultLegNPV() + "\n"
                           + "   coupon leg:  " + cds_6m.couponLegNPV() + "\n");

         Console.WriteLine("1Y fair spread: " + cds_1y.fairSpread() + "\n"
                           + "   NPV:         " + cds_1y.NPV() + "\n"
                           + "   default leg: " + cds_1y.defaultLegNPV() + "\n"
                           + "   coupon leg:  " + cds_1y.couponLegNPV() + "\n");

         Console.WriteLine("2Y fair spread: " + cds_2y.fairSpread() + "\n"
                           + "   NPV:         " + cds_2y.NPV() + "\n"
                           + "   default leg: " + cds_2y.defaultLegNPV() + "\n"
                           + "   coupon leg:  " + cds_2y.couponLegNPV() + "\n");

         Console.WriteLine();
         Console.WriteLine();

         double seconds = (DateTime.Now - timer).TotalSeconds;
         int hours = (int)(seconds / 3600);
         seconds -= hours * 3600;
         int minutes = (int)(seconds / 60);
         seconds -= minutes * 60;
         Console.Write("Run completed in ");
         if (hours > 0)
            Console.Write(hours + " h ");
         if (hours > 0 || minutes > 0)
            Console.Write(minutes + " m ");
         Console.Write(seconds + " s\n");
      }

      private static void example02()
      {

         Date todaysDate = new Date(25, 09, 2014);
         Settings.setEvaluationDate(todaysDate);

         Date termDate = new TARGET().adjust(todaysDate + new Period(2, TimeUnit.Years), BusinessDayConvention.Following);

         Schedule cdsSchedule =
            new MakeSchedule().from(todaysDate).to(termDate)
         .withFrequency(Frequency.Quarterly)
         .withCalendar(new WeekendsOnly())
         .withConvention(BusinessDayConvention.ModifiedFollowing)
         .withTerminationDateConvention(BusinessDayConvention.ModifiedFollowing)
         .withRule(DateGeneration.Rule.CDS)
         .value();

         Date evaluationDate = new Date(21, 10, 2014);

         Settings.setEvaluationDate(evaluationDate);

         // set up ISDA IR curve helpers

         DepositRateHelper dp1m =
            new DepositRateHelper(0.000060, new Period(1, TimeUnit.Months), 2,
                                  new TARGET(), BusinessDayConvention.ModifiedFollowing,
                                  false, new Actual360());
         DepositRateHelper dp2m =
            new DepositRateHelper(0.000450, new Period(2, TimeUnit.Months), 2,
                                  new TARGET(), BusinessDayConvention.ModifiedFollowing,
                                  false, new Actual360());
         DepositRateHelper dp3m =
            new DepositRateHelper(0.000810, new Period(3, TimeUnit.Months), 2,
                                  new TARGET(), BusinessDayConvention.ModifiedFollowing,
                                  false, new Actual360());
         DepositRateHelper dp6m =
            new DepositRateHelper(0.001840, new Period(6, TimeUnit.Months), 2,
                                  new TARGET(), BusinessDayConvention.ModifiedFollowing,
                                  false, new Actual360());
         DepositRateHelper dp9m =
            new DepositRateHelper(0.002560, new Period(9, TimeUnit.Months), 2,
                                  new TARGET(), BusinessDayConvention.ModifiedFollowing,
                                  false, new Actual360());
         DepositRateHelper dp12m =
            new DepositRateHelper(0.003370, new Period(12, TimeUnit.Months), 2,
                                  new TARGET(), BusinessDayConvention.ModifiedFollowing,
                                  false, new Actual360());

         // intentionally we do not provide a fixing for the euribor index used for
         // bootstrapping in order to be compliant with the ISDA specification
         IborIndex euribor6m = new Euribor(new Period(6, TimeUnit.Months));

         // check if indexed coupon is defined (it should not to be 100% consistent with
         // the ISDA spec)

#if QL_USE_INDEXED_COUPON
         Console.Writeline("Warning: QL_USED_INDEXED_COUPON is defined, which is not "
                           + "precisely consistent with the specification of the ISDA rate "
                           + "curve.");
#endif

         SwapRateHelper sw2y = new SwapRateHelper(
            0.002230, new Period(2, TimeUnit.Years), new TARGET(), Frequency.Annual, BusinessDayConvention.ModifiedFollowing, new Thirty360(),
            euribor6m);
         SwapRateHelper sw3y = new SwapRateHelper(
            0.002760, new Period(3, TimeUnit.Years), new TARGET(), Frequency.Annual, BusinessDayConvention.ModifiedFollowing, new Thirty360(),
            euribor6m);
         SwapRateHelper sw4y = new SwapRateHelper(
            0.003530, new Period(4, TimeUnit.Years), new TARGET(), Frequency.Annual, BusinessDayConvention.ModifiedFollowing, new Thirty360(),
            euribor6m);
         SwapRateHelper sw5y = new SwapRateHelper(
            0.004520, new Period(5, TimeUnit.Years), new TARGET(), Frequency.Annual, BusinessDayConvention.ModifiedFollowing, new Thirty360(),
            euribor6m);
         SwapRateHelper sw6y = new SwapRateHelper(
            0.005720, new Period(6, TimeUnit.Years), new TARGET(), Frequency.Annual, BusinessDayConvention.ModifiedFollowing, new Thirty360(),
            euribor6m);
         SwapRateHelper sw7y = new SwapRateHelper(
            0.007050, new Period(7, TimeUnit.Years), new TARGET(), Frequency.Annual, BusinessDayConvention.ModifiedFollowing, new Thirty360(),
            euribor6m);
         SwapRateHelper sw8y = new SwapRateHelper(
            0.008420, new Period(8, TimeUnit.Years), new TARGET(), Frequency.Annual, BusinessDayConvention.ModifiedFollowing, new Thirty360(),
            euribor6m);
         SwapRateHelper sw9y = new SwapRateHelper(
            0.009720, new Period(9, TimeUnit.Years), new TARGET(), Frequency.Annual, BusinessDayConvention.ModifiedFollowing, new Thirty360(),
            euribor6m);
         SwapRateHelper sw10y = new SwapRateHelper(
            0.010900, new Period(10, TimeUnit.Years), new TARGET(), Frequency.Annual, BusinessDayConvention.ModifiedFollowing, new Thirty360(),
            euribor6m);
         SwapRateHelper sw12y = new SwapRateHelper(
            0.012870, new Period(12, TimeUnit.Years), new TARGET(), Frequency.Annual, BusinessDayConvention.ModifiedFollowing, new Thirty360(),
            euribor6m);
         SwapRateHelper sw15y = new SwapRateHelper(
            0.014970, new Period(15, TimeUnit.Years), new TARGET(), Frequency.Annual, BusinessDayConvention.ModifiedFollowing, new Thirty360(),
            euribor6m);
         SwapRateHelper sw20y = new SwapRateHelper(
            0.017000, new Period(20, TimeUnit.Years), new TARGET(), Frequency.Annual, BusinessDayConvention.ModifiedFollowing, new Thirty360(),
            euribor6m);
         SwapRateHelper sw30y = new SwapRateHelper(
            0.018210, new Period(30, TimeUnit.Years), new TARGET(), Frequency.Annual, BusinessDayConvention.ModifiedFollowing, new Thirty360(),
            euribor6m);

         List<RateHelper> isdaRateHelper = new List<RateHelper>();

         isdaRateHelper.Add(dp1m);
         isdaRateHelper.Add(dp2m);
         isdaRateHelper.Add(dp3m);
         isdaRateHelper.Add(dp6m);
         isdaRateHelper.Add(dp9m);
         isdaRateHelper.Add(dp12m);
         isdaRateHelper.Add(sw2y);
         isdaRateHelper.Add(sw3y);
         isdaRateHelper.Add(sw4y);
         isdaRateHelper.Add(sw5y);
         isdaRateHelper.Add(sw6y);
         isdaRateHelper.Add(sw7y);
         isdaRateHelper.Add(sw8y);
         isdaRateHelper.Add(sw9y);
         isdaRateHelper.Add(sw10y);
         isdaRateHelper.Add(sw12y);
         isdaRateHelper.Add(sw15y);
         isdaRateHelper.Add(sw20y);
         isdaRateHelper.Add(sw30y);

         Handle<YieldTermStructure> rateTs = new Handle<YieldTermStructure>(
            new PiecewiseYieldCurve<Discount, LogLinear>(
               0, new WeekendsOnly(), isdaRateHelper, new Actual365Fixed()));

         rateTs.currentLink().enableExtrapolation();

         // output rate curve
         Console.WriteLine("ISDA rate curve: ");
         for (int i = 0; i < isdaRateHelper.Count; i++)
         {
            Date d = isdaRateHelper[i].latestDate();
            Console.WriteLine(d.ToShortDateString() + "\t" + rateTs.currentLink().zeroRate(d, new Actual365Fixed(), Compounding.Continuous).rate() + "\t" +
                              rateTs.currentLink().discount(d)); ;
         }

         // build reference credit curve (flat)
         DefaultProbabilityTermStructure defaultTs0 =
            new FlatHazardRate(0, new WeekendsOnly(), 0.016739207493630, new Actual365Fixed());

         // reference CDS
         Schedule sched = new Schedule(new Date(22, 09, 2014), new Date(20, 12, 2019), new Period(3, TimeUnit.Months),
                                       new WeekendsOnly(), BusinessDayConvention.Following, BusinessDayConvention.Unadjusted, DateGeneration.Rule.CDS, false, null, null);
         CreditDefaultSwap trade =
            new CreditDefaultSwap(CreditDefaultSwap.Protection.Side.Buyer, 100000000.0, 0.01, sched,
                                  BusinessDayConvention.Following, new Actual360(), true, true,
                                  new Date(22, 10, 2014), null,
                                  new Actual360(true), true);

         FixedRateCoupon cp = new FixedRateCoupon(trade.coupons()[0] as FixedRateCoupon);
         Console.WriteLine("first period = " + cp.accrualStartDate().ToShortDateString() + " to " + cp.accrualEndDate().ToShortDateString() +
                           " accrued amount = " + cp.accruedAmount(new Date(24, 10, 2014)));

         // price with isda engine
         IsdaCdsEngine engine = new IsdaCdsEngine(
            new Handle<DefaultProbabilityTermStructure>(defaultTs0), 0.4, rateTs,
            false, IsdaCdsEngine.NumericalFix.Taylor, IsdaCdsEngine.AccrualBias.NoBias, IsdaCdsEngine.ForwardsInCouponPeriod.Piecewise);

         trade.setPricingEngine(engine);

         Console.WriteLine("reference trade NPV = " + trade.NPV());


         // build credit curve with one cds
         List<BootstrapHelper<DefaultProbabilityTermStructure>> isdaCdsHelper = new List<BootstrapHelper<DefaultProbabilityTermStructure>>();

         CdsHelper cds5y = new SpreadCdsHelper(
            0.00672658551, new Period(4, TimeUnit.Years) + new Period(6, TimeUnit.Months), 1, new WeekendsOnly(), Frequency.Quarterly,
            BusinessDayConvention.Following, DateGeneration.Rule.CDS, new Actual360(), 0.4, rateTs, true, true,
            null, new Actual360(true), true, CreditDefaultSwap.PricingModel.ISDA);

         isdaCdsHelper.Add(cds5y);

         Handle<DefaultProbabilityTermStructure> defaultTs = new Handle<DefaultProbabilityTermStructure>(new PiecewiseDefaultCurve<SurvivalProbability, LogLinear>(
                  0, new WeekendsOnly(), isdaCdsHelper, new Actual365Fixed()));

         Console.WriteLine("ISDA credit curve: ");
         for (int i = 0; i < isdaCdsHelper.Count; i++)
         {
            Date d = isdaCdsHelper[i].latestDate();
            double pd = defaultTs.currentLink().defaultProbability(d);
            double t = defaultTs.currentLink().timeFromReference(d);
            Console.WriteLine(d.ToLongDateString() + ";" + pd.ToString() + ";" + (1.0 - pd).ToString() + ";" +
                              (-Math.Log(1.0 - pd) / t).ToString());
         }

         // // set up sample CDS trade

         // ext.Rule.shared_ptr<CreditDefaultSwap> trade =
         //     MakeCreditDefaultSwap(5 , TimeUnit.Years), 0.03);

         // // set up isda engine

         // // ext.Rule.shared_ptr<IsdaCdsEngine> isdaPricer =
         // //     ext.Rule.make_shared<IsdaCdsEngine>(
         // //         isdaCdsHelper, 0.4, isdaRateHelper);
         // ext.Rule.shared_ptr<IsdaCdsEngine> isdaPricer =
         //     ext.Rule.make_shared<IsdaCdsEngine>(defaultTs,0.40,rateTs);

         // check the curves built by the engine

         // Handle<YieldTermStructure> isdaYts = isdaPricer->isdaRateCurve();
         // Handle<DefaultProbabilityTermStructure> isdaCts = isdaPricer->isdaCreditCurve();

         // std.Rule.cout << "isda rate 1m " << dp1m->latestDate() << " "
         //           << isdaYts->zeroRate(dp1m->latestDate(), Actual365Fixed(),
         //                                   Continuous) << std.Rule.endl;
         // std.Rule.cout << "isda rate 3m " << dp3m->latestDate() << " "
         //           << isdaYts->zeroRate(dp3m->latestDate(), Actual365Fixed(),
         //                                   Continuous) << std.Rule.endl;
         // std.Rule.cout << "isda rate 6m " << dp6m->latestDate() << " "
         //           << isdaYts->zeroRate(dp6m->latestDate(), Actual365Fixed(),
         //                                   Continuous) << std.Rule.endl;

         // std.Rule.cout << "isda hazard 5y " << cds5y->latestDate() << " "
         //           << isdaCts->hazardRate(cds5y->latestDate()) << std.Rule.endl;

         // price the trade

         // trade->setPricingEngine(isdaPricer);

         // Real npv = trade->NPV();

         // std.Rule.cout << "Pricing of example trade with ISDA engine:" << std.Rule.endl;
         // std.Rule.cout << "NPV = " << npv << std.Rule.endl;
      }

      private static void example03()
      {

         // this is the example from Apdx E in pricing and risk management of CDS, OpenGamma

         Date tradeDate = new Date(13, 06, 2011);

         Settings.setEvaluationDate(tradeDate);

         DepositRateHelper dp1m =
            new DepositRateHelper(0.00445, new Period(1, TimeUnit.Months), 2,
                                  new WeekendsOnly(), BusinessDayConvention.ModifiedFollowing,
                                  false, new Actual360());
         DepositRateHelper dp2m =
            new DepositRateHelper(0.00949, new Period(2, TimeUnit.Months), 2,
                                  new WeekendsOnly(), BusinessDayConvention.ModifiedFollowing,
                                  false, new Actual360());
         DepositRateHelper dp3m =
            new DepositRateHelper(0.01234, new Period(3, TimeUnit.Months), 2,
                                  new WeekendsOnly(), BusinessDayConvention.ModifiedFollowing,
                                  false, new Actual360());
         DepositRateHelper dp6m =
            new DepositRateHelper(0.01776, new Period(6, TimeUnit.Months), 2,
                                  new WeekendsOnly(), BusinessDayConvention.ModifiedFollowing,
                                  false, new Actual360());
         DepositRateHelper dp9m =
            new DepositRateHelper(0.01935, new Period(9, TimeUnit.Months), 2,
                                  new WeekendsOnly(), BusinessDayConvention.ModifiedFollowing,
                                  false, new Actual360());
         DepositRateHelper dp1y =
            new DepositRateHelper(0.02084, new Period(12, TimeUnit.Months), 2,
                                  new WeekendsOnly(), BusinessDayConvention.ModifiedFollowing,
                                  false, new Actual360());

         // this index is probably not important since we are not using
         // QL_USE_INDEXED_COUPON - define it "isda compliant" anyway
         IborIndex euribor6m = new IborIndex(
            "IsdaIbor", new Period(6, TimeUnit.Months), 2, new EURCurrency(), new WeekendsOnly(),
            BusinessDayConvention.ModifiedFollowing, false, new Actual360());

         SwapRateHelper sw2y = new SwapRateHelper(
            0.01652, new Period(2, TimeUnit.Years), new WeekendsOnly(), Frequency.Annual, BusinessDayConvention.ModifiedFollowing, new Thirty360(),
            euribor6m);
         SwapRateHelper sw3y = new SwapRateHelper(
            0.02018, new Period(3, TimeUnit.Years), new WeekendsOnly(), Frequency.Annual, BusinessDayConvention.ModifiedFollowing, new Thirty360(),
            euribor6m);
         SwapRateHelper sw4y = new SwapRateHelper(
            0.02303, new Period(4, TimeUnit.Years), new WeekendsOnly(), Frequency.Annual, BusinessDayConvention.ModifiedFollowing, new Thirty360(),
            euribor6m);
         SwapRateHelper sw5y = new SwapRateHelper(
            0.02525, new Period(5, TimeUnit.Years), new WeekendsOnly(), Frequency.Annual, BusinessDayConvention.ModifiedFollowing, new Thirty360(),
            euribor6m);
         SwapRateHelper sw6y = new SwapRateHelper(
            0.02696, new Period(6, TimeUnit.Years), new WeekendsOnly(), Frequency.Annual, BusinessDayConvention.ModifiedFollowing, new Thirty360(),
            euribor6m);
         SwapRateHelper sw7y = new SwapRateHelper(
            0.02825, new Period(7, TimeUnit.Years), new WeekendsOnly(), Frequency.Annual, BusinessDayConvention.ModifiedFollowing, new Thirty360(),
            euribor6m);
         SwapRateHelper sw8y = new SwapRateHelper(
            0.02931, new Period(8, TimeUnit.Years), new WeekendsOnly(), Frequency.Annual, BusinessDayConvention.ModifiedFollowing, new Thirty360(),
            euribor6m);
         SwapRateHelper sw9y = new SwapRateHelper(
            0.03017, new Period(9, TimeUnit.Years), new WeekendsOnly(), Frequency.Annual, BusinessDayConvention.ModifiedFollowing, new Thirty360(),
            euribor6m);
         SwapRateHelper sw10y = new SwapRateHelper(
            0.03092, new Period(10, TimeUnit.Years), new WeekendsOnly(), Frequency.Annual, BusinessDayConvention.ModifiedFollowing, new Thirty360(),
            euribor6m);
         SwapRateHelper sw11y = new SwapRateHelper(
            0.03160, new Period(11, TimeUnit.Years), new WeekendsOnly(), Frequency.Annual, BusinessDayConvention.ModifiedFollowing, new Thirty360(),
            euribor6m);
         SwapRateHelper sw12y = new SwapRateHelper(
            0.03231, new Period(12, TimeUnit.Years), new WeekendsOnly(), Frequency.Annual, BusinessDayConvention.ModifiedFollowing, new Thirty360(),
            euribor6m);
         SwapRateHelper sw15y = new SwapRateHelper(
            0.03367, new Period(15, TimeUnit.Years), new WeekendsOnly(), Frequency.Annual, BusinessDayConvention.ModifiedFollowing, new Thirty360(),
            euribor6m);
         SwapRateHelper sw20y = new SwapRateHelper(
            0.03419, new Period(20, TimeUnit.Years), new WeekendsOnly(), Frequency.Annual, BusinessDayConvention.ModifiedFollowing, new Thirty360(),
            euribor6m);
         SwapRateHelper sw25y = new SwapRateHelper(
            0.03411, new Period(25, TimeUnit.Years), new WeekendsOnly(), Frequency.Annual, BusinessDayConvention.ModifiedFollowing, new Thirty360(),
            euribor6m);
         SwapRateHelper sw30y = new SwapRateHelper(
            0.03412, new Period(30, TimeUnit.Years), new WeekendsOnly(), Frequency.Annual, BusinessDayConvention.ModifiedFollowing, new Thirty360(),
            euribor6m);

         List<RateHelper> isdaYieldHelpers = new List<RateHelper>();

         isdaYieldHelpers.Add(dp1m);
         isdaYieldHelpers.Add(dp2m);
         isdaYieldHelpers.Add(dp3m);
         isdaYieldHelpers.Add(dp6m);
         isdaYieldHelpers.Add(dp9m);
         isdaYieldHelpers.Add(dp1y);
         isdaYieldHelpers.Add(sw2y);
         isdaYieldHelpers.Add(sw3y);
         isdaYieldHelpers.Add(sw4y);
         isdaYieldHelpers.Add(sw5y);
         isdaYieldHelpers.Add(sw6y);
         isdaYieldHelpers.Add(sw7y);
         isdaYieldHelpers.Add(sw8y);
         isdaYieldHelpers.Add(sw9y);
         isdaYieldHelpers.Add(sw10y);
         isdaYieldHelpers.Add(sw11y);
         isdaYieldHelpers.Add(sw12y);
         isdaYieldHelpers.Add(sw15y);
         isdaYieldHelpers.Add(sw20y);
         isdaYieldHelpers.Add(sw25y);
         isdaYieldHelpers.Add(sw30y);

         // build yield curve
         Handle<YieldTermStructure> isdaYts = new Handle<YieldTermStructure>(
            new PiecewiseYieldCurve<Discount, LogLinear>(
               0, new WeekendsOnly(), isdaYieldHelpers, new Actual365Fixed()));

         isdaYts.currentLink().enableExtrapolation();


         CreditDefaultSwap.PricingModel model = CreditDefaultSwap.PricingModel.ISDA;
         CdsHelper cds6m = new SpreadCdsHelper(
            0.007927, new Period(6, TimeUnit.Months), 1, new WeekendsOnly(), Frequency.Quarterly, BusinessDayConvention.Following,
            DateGeneration.Rule.CDS, new Actual360(), 0.4, isdaYts, true, true, null,
            new Actual360(true), true, model);
         CdsHelper cds1y = new SpreadCdsHelper(
            0.007927, new Period(1, TimeUnit.Years), 1, new WeekendsOnly(), Frequency.Quarterly, BusinessDayConvention.Following,
            DateGeneration.Rule.CDS, new Actual360(), 0.4, isdaYts, true, true, null,
            new Actual360(true), true, model);
         CdsHelper cds3y = new SpreadCdsHelper(
            0.012239, new Period(3, TimeUnit.Years), 1, new WeekendsOnly(), Frequency.Quarterly, BusinessDayConvention.Following,
            DateGeneration.Rule.CDS, new Actual360(), 0.4, isdaYts, true, true, null,
            new Actual360(true), true, model);
         CdsHelper cds5y = new SpreadCdsHelper(
            0.016979, new Period(5, TimeUnit.Years), 1, new WeekendsOnly(), Frequency.Quarterly, BusinessDayConvention.Following,
            DateGeneration.Rule.CDS, new Actual360(), 0.4, isdaYts, true, true, null,
            new Actual360(true), true, model);
         CdsHelper cds7y = new SpreadCdsHelper(
            0.019271, new Period(7, TimeUnit.Years), 1, new WeekendsOnly(), Frequency.Quarterly, BusinessDayConvention.Following,
            DateGeneration.Rule.CDS, new Actual360(), 0.4, isdaYts, true, true, null,
            new Actual360(true), true, model);
         CdsHelper cds10y = new SpreadCdsHelper(
            0.020860, new Period(10, TimeUnit.Years), 1, new WeekendsOnly(), Frequency.Quarterly, BusinessDayConvention.Following,
            DateGeneration.Rule.CDS, new Actual360(), 0.4, isdaYts, true, true, null,
            new Actual360(true), true, model);

         List<BootstrapHelper<DefaultProbabilityTermStructure>> isdaCdsHelpers = new List<BootstrapHelper<DefaultProbabilityTermStructure>>();

         isdaCdsHelpers.Add(cds6m);
         isdaCdsHelpers.Add(cds1y);
         isdaCdsHelpers.Add(cds3y);
         isdaCdsHelpers.Add(cds5y);
         isdaCdsHelpers.Add(cds7y);
         isdaCdsHelpers.Add(cds10y);

         // build credit curve
         Handle<DefaultProbabilityTermStructure> isdaCts =
            new Handle<DefaultProbabilityTermStructure>(new PiecewiseDefaultCurve<SurvivalProbability, LogLinear>(
                                                           0, new WeekendsOnly(), isdaCdsHelpers, new Actual365Fixed()));

         // set up isda engine
         IsdaCdsEngine isdaPricer =
            new IsdaCdsEngine(
            isdaCts, 0.4, isdaYts);

         // check the curves
         Console.WriteLine("ISDA yield curve:");
         Console.WriteLine("date;time;zeroyield");
         for (int i = 0; i < isdaYieldHelpers.Count; i++)
         {
            Date d = isdaYieldHelpers[i].latestDate();
            double t = isdaYts.currentLink().timeFromReference(d);
            Console.WriteLine(d.ToLongDateString() + ";" + t.ToString() + ";"
                              + isdaYts.currentLink().zeroRate(d, new Actual365Fixed(), Compounding.Continuous).rate().ToString());
         }

         Console.WriteLine("ISDA credit curve:");
         Console.WriteLine("date;time;survivalprob");
         for (int i = 0; i < isdaCdsHelpers.Count; i++)
         {
            Date d = isdaCdsHelpers[i].latestDate();
            double t = isdaCts.currentLink().timeFromReference(d);
            Console.WriteLine(d.ToShortDateString() + ";" + t.ToString() + ";" + isdaCts.currentLink().survivalProbability(d));
         }
      }
   }
}
