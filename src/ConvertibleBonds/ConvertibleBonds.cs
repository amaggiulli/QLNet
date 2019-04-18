/*
  Copyright (C) 2008-2018  Andrea Maggiulli (a.maggiulli@gmail.com)

 This file is part of QLNet Project https://github.com/amaggiulli/qlnet

 QLNet is free software: you can redistribute it and/or modify it
 under the terms of the QLNet license.  You should have received a
 copy of the license along with this program; if not, license is
 available at <https://github.com/amaggiulli/QLNet/blob/develop/LICENSE>.

 QLNet is a based on QuantLib, a free-software/open-source library
 for financial quantitative analysts and developers - http://quantlib.org/
 The QuantLib license is available online at http://quantlib.org/license.shtml.

 This program is distributed in the hope that it will be useful, but WITHOUT
 ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
 FOR A PARTICULAR PURPOSE.  See the license for more details.
*/

using System;
using System.Collections.Generic;
using QLNet;

namespace ConvertibleBonds
{
   class ConvertibleBonds
   {
      static void Main(string[] args)
      {
         try
         {
            Option.Type type = Option.Type.Put;
            double underlying = 36.0;
            double spreadRate = 0.005;

            double dividendYield = 0.02;
            double riskFreeRate = 0.06;
            double volatility = 0.2;

            int settlementDays = 3;
            int length = 5;
            double redemption = 100.0;
            double conversionRatio = redemption / underlying; // at the money

            // set up dates/schedules
            Calendar calendar = new TARGET();
            Date today = calendar.adjust(Date.Today);

            Settings.Instance.setEvaluationDate(today);
            Date settlementDate = calendar.advance(today, settlementDays, TimeUnit.Days);
            Date exerciseDate = calendar.advance(settlementDate, length, TimeUnit.Years);
            Date issueDate = calendar.advance(exerciseDate, -length, TimeUnit.Years);

            BusinessDayConvention convention = BusinessDayConvention.ModifiedFollowing;

            Frequency frequency = Frequency.Annual;

            Schedule schedule = new Schedule(issueDate, exerciseDate,
                                             new Period(frequency), calendar, convention, convention,
                                             DateGeneration.Rule.Backward, false);

            DividendSchedule dividends = new DividendSchedule();
            CallabilitySchedule callability = new CallabilitySchedule();

            List<double> coupons = new InitializedList<double>(1, 0.05);
            DayCounter bondDayCount = new Thirty360();

            int[] callLength = {2, 4}; // Call dates, years 2,4.
            int[] putLength = {3}; // Put dates year 3.

            double[] callPrices = {101.5, 100.85};
            double[] putPrices = {105.0};

            // Load call schedules
            for (int i = 0; i < callLength.Length; i++)
            {
               SoftCallability s = new SoftCallability(
                  new Callability.Price(callPrices[i], Callability.Price.Type.Clean), schedule.date(callLength[i]),
                  1.20);
               callability.Add(s);
            }

            for (int j = 0; j < putLength.Length; j++)
            {
               Callability s = new Callability(new Callability.Price(putPrices[j], Callability.Price.Type.Clean),
                                               Callability.Type.Put, schedule.date(putLength[j]));
               callability.Add(s);
            }

            // Assume dividends are paid every 6 months .
            for (Date d = today + new Period(6, TimeUnit.Months); d < exerciseDate; d += new Period(6, TimeUnit.Months))
            {
               Dividend div = new FixedDividend(1.0, d);
               dividends.Add(div);
            }

            DayCounter dayCounter = new Actual365Fixed();
            double maturity = dayCounter.yearFraction(settlementDate, exerciseDate);

            Console.WriteLine("option type = " + type);
            Console.WriteLine("Time to maturity = " + maturity);
            Console.WriteLine("Underlying price = " + underlying);
            Console.WriteLine("Risk-free interest rate = {0:0.0%}", riskFreeRate);
            Console.WriteLine("Dividend yield = {0:0.0%}%", dividendYield);
            Console.WriteLine("Volatility = {0:0.0%}%", volatility);
            Console.WriteLine("");


            // write column headings
            int[] widths = {35, 14, 14};
            int totalWidth = widths[0] + widths[1] + widths[2];
            string rule = new string('-', totalWidth);
            string dblrule = new string('=', totalWidth);

            Console.WriteLine(dblrule);
            Console.WriteLine("Tsiveriotis-Fernandes method");
            Console.WriteLine(dblrule);
            Console.WriteLine("Tree Type                           European     American        ");
            Console.WriteLine(rule);


            Exercise exercise = new EuropeanExercise(exerciseDate);
            Exercise amexercise = new AmericanExercise(settlementDate, exerciseDate);

            Handle<Quote> underlyingH = new Handle<Quote>(new SimpleQuote(underlying));
            Handle<YieldTermStructure> flatTermStructure =
               new Handle<YieldTermStructure>(new FlatForward(settlementDate, riskFreeRate, dayCounter));
            Handle<YieldTermStructure> flatDividendTS =
               new Handle<YieldTermStructure>(new FlatForward(settlementDate, dividendYield, dayCounter));
            Handle<BlackVolTermStructure> flatVolTS =
               new Handle<BlackVolTermStructure>(new BlackConstantVol(settlementDate, calendar, volatility,
                                                                      dayCounter));

            BlackScholesMertonProcess stochasticProcess =
               new BlackScholesMertonProcess(underlyingH, flatDividendTS, flatTermStructure, flatVolTS);

            int timeSteps = 801;

            Handle<Quote> creditSpread = new Handle<Quote>(new SimpleQuote(spreadRate));

            Quote rate = new SimpleQuote(riskFreeRate);

            Handle<YieldTermStructure> discountCurve =
               new Handle<YieldTermStructure>(new FlatForward(today, new Handle<Quote>(rate), dayCounter));

            IPricingEngine engine = new BinomialConvertibleEngine<JarrowRudd>(stochasticProcess, timeSteps);

            ConvertibleFixedCouponBond europeanBond = new ConvertibleFixedCouponBond(exercise, conversionRatio,
                                                                                     dividends, callability, creditSpread, issueDate, settlementDays, coupons, bondDayCount, schedule,
                                                                                     redemption);

            europeanBond.setPricingEngine(engine);

            ConvertibleFixedCouponBond americanBond = new ConvertibleFixedCouponBond(amexercise, conversionRatio,
                                                                                     dividends, callability, creditSpread, issueDate, settlementDays, coupons, bondDayCount, schedule,
                                                                                     redemption);
            americanBond.setPricingEngine(engine);


            Console.WriteLine("Jarrow-Rudd                         {0:0.000000}   {1:0.000000}", europeanBond.NPV(), americanBond.NPV());

            americanBond.setPricingEngine(new BinomialConvertibleEngine<CoxRossRubinstein>(stochasticProcess, timeSteps));
            europeanBond.setPricingEngine(new BinomialConvertibleEngine<CoxRossRubinstein>(stochasticProcess, timeSteps));

            Console.WriteLine("CoxRossRubinstein                   {0:0.000000}   {1:0.000000}", europeanBond.NPV(), americanBond.NPV());

            americanBond.setPricingEngine(new BinomialConvertibleEngine<AdditiveEQPBinomialTree>(stochasticProcess, timeSteps));
            europeanBond.setPricingEngine(new BinomialConvertibleEngine<AdditiveEQPBinomialTree>(stochasticProcess, timeSteps));

            Console.WriteLine("AdditiveEQPBinomialTree             {0:0.000000}   {1:0.000000}", europeanBond.NPV(), americanBond.NPV());

            americanBond.setPricingEngine(new BinomialConvertibleEngine<Trigeorgis>(stochasticProcess, timeSteps));
            europeanBond.setPricingEngine(new BinomialConvertibleEngine<Trigeorgis>(stochasticProcess, timeSteps));

            Console.WriteLine("Trigeorgis                          {0:0.000000}   {1:0.000000}", europeanBond.NPV(), americanBond.NPV());

            americanBond.setPricingEngine(new BinomialConvertibleEngine<Tian>(stochasticProcess, timeSteps));
            europeanBond.setPricingEngine(new BinomialConvertibleEngine<Tian>(stochasticProcess, timeSteps));

            Console.WriteLine("Tian                                {0:0.000000}   {1:0.000000}", europeanBond.NPV(), americanBond.NPV());

            americanBond.setPricingEngine(new BinomialConvertibleEngine<LeisenReimer>(stochasticProcess, timeSteps));
            europeanBond.setPricingEngine(new BinomialConvertibleEngine<LeisenReimer>(stochasticProcess, timeSteps));

            Console.WriteLine("LeisenReimer                        {0:0.000000}   {1:0.000000}", europeanBond.NPV(), americanBond.NPV());

            americanBond.setPricingEngine(new BinomialConvertibleEngine<Joshi4>(stochasticProcess, timeSteps));
            europeanBond.setPricingEngine(new BinomialConvertibleEngine<Joshi4>(stochasticProcess, timeSteps));

            Console.WriteLine("Joshi4                              {0:0.000000}   {1:0.000000}", europeanBond.NPV(), americanBond.NPV());
            Console.WriteLine("===========================================================================");
         }
         catch (Exception e)
         {
            Console.WriteLine(e.ToString());
         }

         Console.ReadKey();
      }
   }
}
