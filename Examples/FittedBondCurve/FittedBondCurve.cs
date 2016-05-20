//  Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)
//  
//  This file is part of QLNet Project https://github.com/amaggiulli/qlnet
//  QLNet is free software: you can redistribute it and/or modify it
//  under the terms of the QLNet license.  You should have received a
//  copy of the license along with this program; if not, license is  
//  available online at <http://qlnet.sourceforge.net/License.html>.
//   
//  QLNet is a based on QuantLib, a free-software/open-source library
//  for financial quantitative analysts and developers - http://quantlib.org/
//  The QuantLib license is available online at http://quantlib.org/license.shtml.
//  
//  This program is distributed in the hope that it will be useful, but WITHOUT
//  ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
//  FOR A PARTICULAR PURPOSE.  See the license for more details.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QLNet;

/*  This example shows how to fit a term structure to a set of bonds
    using four different fitting methodologies. Though fitting is most
    useful for large numbers of bonds with non-smooth yield tenor
    structures, for comparison purposes, relatively smooth bond yields
    are fit here and compared to known solutions (par coupons), or
    results generated from the bootstrap fitting method.
*/

namespace FittedBondCurve
{
   class FittedBondCurve
   {
      // par-rate approximation
      public static double parRate(YieldTermStructure yts,List<Date> dates,DayCounter resultDayCounter) 
      {
         Utils.QL_REQUIRE(dates.Count >= 2,()=> "at least two dates are required");
         double sum = 0.0;
         double dt;
         for (int i=1; i<dates.Count; ++i) 
         {
            dt = resultDayCounter.yearFraction(dates[i-1], dates[i]);
            Utils.QL_REQUIRE(dt>=0.0,()=> "unsorted dates");
            sum += yts.discount(dates[i]) * dt;
         }
         double result = yts.discount(dates.First()) - yts.discount(dates.Last());
         return result/sum;
      }

      public static void printOutput(string tag, FittedBondDiscountCurve curve) 
      {
         Console.WriteLine(tag) ;
         Console.WriteLine("reference date : " + curve.referenceDate());
         Console.WriteLine("number of iterations : " + curve.fitResults().numberOfIterations() +"\n");
      }

      static void Main( string[] args )
      {
         try
         {

            DateTime timer = DateTime.Now;

            int numberOfBonds = 15;

            double[] cleanPrice = new double[numberOfBonds];

            for (int i=0; i<numberOfBonds; i++) 
            {
               cleanPrice[i]=100.0;
            }

            List<SimpleQuote> quote = new List<SimpleQuote>();
            for (int i=0; i<numberOfBonds; i++) 
            {
               SimpleQuote cp = new SimpleQuote(cleanPrice[i]);
               quote.Add(cp);
            }

            RelinkableHandle<Quote>[] quoteHandle = new RelinkableHandle<Quote>[numberOfBonds];
            for (int i=0; i<numberOfBonds; i++) 
            {
               quoteHandle[i] = new RelinkableHandle<Quote>(); 
               quoteHandle[i].linkTo(quote[i]);
            }

            int[] lengths = { 2, 4, 6, 8, 10, 12, 14, 16,
                              18, 20, 22, 24, 26, 28, 30 };
            double[] coupons = { 0.0200, 0.0225, 0.0250, 0.0275, 0.0300,
                                 0.0325, 0.0350, 0.0375, 0.0400, 0.0425,
                                 0.0450, 0.0475, 0.0500, 0.0525, 0.0550 };

            Frequency frequency = Frequency.Annual;
            DayCounter dc = new SimpleDayCounter();
            BusinessDayConvention accrualConvention = BusinessDayConvention.ModifiedFollowing;
            BusinessDayConvention convention = BusinessDayConvention.ModifiedFollowing;
            double redemption = 100.0;

            Calendar calendar = new TARGET();
            Date today = calendar.adjust(Date.Today);
            Date origToday = today;
            Settings.setEvaluationDate(today);

            // changing bondSettlementDays=3 increases calculation
            // time of exponentialsplines fitting method
            int bondSettlementDays = 0;
            int curveSettlementDays = 0;

            Date bondSettlementDate = calendar.advance(today, new Period(bondSettlementDays,TimeUnit.Days));

            Console.WriteLine();
            Console.WriteLine("Today's date: " + today );
            Console.WriteLine("Bonds' settlement date: " + bondSettlementDate );
            Console.WriteLine("Calculating fit for 15 bonds.....\n" );

            List<BondHelper> instrumentsA = new List<BondHelper>();
            List<RateHelper> instrumentsB = new List<RateHelper>();

            for (int j=0; j< lengths.Length; j++) 
            {
               Date maturity = calendar.advance(bondSettlementDate, new Period(lengths[j],TimeUnit.Years));

               Schedule schedule = new Schedule(bondSettlementDate, maturity, new Period(frequency),
                                                calendar, accrualConvention, accrualConvention,
                                                DateGeneration.Rule.Backward, false);

               BondHelper helperA = new FixedRateBondHelper(quoteHandle[j],
                                                            bondSettlementDays,
                                                            100.0,
                                                            schedule,
                                                            new InitializedList<double>(1,coupons[j]), 
                                                            dc,
                                                            convention,
                                                            redemption);

              RateHelper helperB = new FixedRateBondHelper(quoteHandle[j],
                                                           bondSettlementDays,
                                                           100.0,
                                                           schedule,
                                                           new InitializedList<double>(1, coupons[j]),
                                                           dc,
                                                           convention,
                                                           redemption);
               instrumentsA.Add(helperA);
               instrumentsB.Add(helperB);
            }


            bool constrainAtZero = true;
            double tolerance = 1.0e-10;
            int max = 5000;

            YieldTermStructure ts0 = new PiecewiseYieldCurve<Discount,LogLinear>(curveSettlementDays,
                                                                                 calendar,
                                                                                 instrumentsB,
                                                                                 dc);

            ExponentialSplinesFitting exponentialSplines = new ExponentialSplinesFitting(constrainAtZero);

            FittedBondDiscountCurve ts1 = new FittedBondDiscountCurve(curveSettlementDays,
                                                                     calendar,
                                                                     instrumentsA,
                                                                     dc,
                                                                     exponentialSplines,
                                                                     tolerance,
                                                                     max);

            printOutput("(a) exponential splines", ts1);


            SimplePolynomialFitting simplePolynomial = new SimplePolynomialFitting(3, constrainAtZero);

            FittedBondDiscountCurve ts2 = new FittedBondDiscountCurve(curveSettlementDays,
                                                                      calendar,
                                                                      instrumentsA,
                                                                      dc,
                                                                      simplePolynomial,
                                                                      tolerance,
                                                                      max);

            printOutput("(b) simple polynomial", ts2);


            NelsonSiegelFitting nelsonSiegel = new NelsonSiegelFitting();

            FittedBondDiscountCurve ts3 = new FittedBondDiscountCurve(curveSettlementDays,
                                                                      calendar,
                                                                      instrumentsA,
                                                                      dc,
                                                                      nelsonSiegel,
                                                                      tolerance,
                                                                      max);

            printOutput("(c) Nelson-Siegel", ts3);


            // a cubic bspline curve with 11 knot points, implies
            // n=6 (constrained problem) basis functions

            double[] knots =  { -30.0, -20.0,  0.0,  5.0, 10.0, 15.0,
                                 20.0,  25.0, 30.0, 40.0, 50.0 };

            List<double> knotVector = new List<double>();
            for (int i=0; i< knots.Length; i++) 
            {
               knotVector.Add(knots[i]);
            }

            CubicBSplinesFitting cubicBSplines = new CubicBSplinesFitting(knotVector, constrainAtZero);

            FittedBondDiscountCurve ts4 = new FittedBondDiscountCurve(curveSettlementDays,
                                                                      calendar,
                                                                      instrumentsA,
                                                                      dc,
                                                                      cubicBSplines,
                                                                      tolerance,
                                                                      max);

            printOutput("(d) cubic B-splines", ts4);

            SvenssonFitting svensson = new SvenssonFitting();

            FittedBondDiscountCurve ts5 = new FittedBondDiscountCurve(curveSettlementDays,
                                                                      calendar,
                                                                      instrumentsA,
                                                                      dc,
                                                                      svensson,
                                                                      tolerance,
                                                                      max);

            printOutput("(e) Svensson", ts5);

            Handle<YieldTermStructure> discountCurve = new Handle<YieldTermStructure>(
               new FlatForward(curveSettlementDays, calendar, 0.01, dc));

            SpreadFittingMethod nelsonSiegelSpread = new SpreadFittingMethod(new NelsonSiegelFitting(),discountCurve);

            FittedBondDiscountCurve ts6 = new FittedBondDiscountCurve( curveSettlementDays,
                                                                       calendar,
                                                                       instrumentsA,
                                                                       dc,
                                                                       nelsonSiegelSpread,
                                                                       tolerance,
                                                                       max);

            printOutput("(f) Nelson-Siegel spreaded", ts6);


            Console.WriteLine("Output par rates for each curve. In this case, ");
            Console.WriteLine("par rates should equal coupons for these par bonds.\n");

            Console.WriteLine( " tenor" + " | "
                             + "coupon" + " | "
                             + "bstrap" + " | "
                             + "   (a)" + " | "
                             + "   (b)" + " | "
                             + "   (c)" + " | "
                             + "   (d)" + " | "
                             + "   (e)" + " | "
                             + "   (f)" );

            for (int i=0; i<instrumentsA.Count; i++) 
            {

               List<CashFlow> cfs = instrumentsA[i].bond().cashflows();

               int cfSize = instrumentsA[i].bond().cashflows().Count;
               List<Date> keyDates = new List<Date>();
               keyDates.Add(bondSettlementDate);

               for (int j=0; j<cfSize-1; j++) 
               {
                  if (!cfs[j].hasOccurred(bondSettlementDate, false)) 
                  {
                     Date myDate =  cfs[j].date();
                     keyDates.Add(myDate);
                  }
               }

               double tenor = dc.yearFraction(today, cfs[cfSize-1].date());
               double test = parRate(ts0, keyDates, dc);

               Console.WriteLine( tenor.ToString( "##.000" ).PadLeft( 6 ) + " | "
                      + ( 100.0 * coupons[i] ).ToString( "##.000" ).PadLeft( 6 ) + " | "
                      // piecewise bootstrap
                      + ( 100.0 * parRate( ts0, keyDates, dc ) ).ToString( "##.000" ).PadLeft(6) + " | "
                      // exponential splines
                      + ( 100.0 * parRate( ts1, keyDates, dc ) ).ToString( "##.000" ).PadLeft( 6 ) + " | "
                      // simple polynomial
                      + ( 100.0 * parRate( ts2, keyDates, dc ) ).ToString( "##.000" ).PadLeft( 6 ) + " | "
                      // Nelson-Siegel
                      + ( 100.0 * parRate( ts3, keyDates, dc ) ).ToString( "##.000" ).PadLeft( 6 ) + " | "
                      // cubic bsplines
                      + ( 100.0 * parRate( ts4, keyDates, dc ) ).ToString( "##.000" ).PadLeft( 6 ) + " | "
                      // Svensson
                      + ( 100.0 * parRate( ts5, keyDates, dc ) ).ToString( "##.000" ).PadLeft( 6 ) + " | "
                      // Nelson-Siegel Spreaded
                      + ( 100.0 * parRate( ts6, keyDates, dc ) ).ToString( "##.000" ).PadLeft( 6 ) );
            }

            Console.WriteLine("\n\n"); 
            Console.WriteLine( "Now add 23 months to today. Par rates should be ");
            Console.WriteLine( "automatically recalculated because today's date "  );
            Console.WriteLine( "changes.  Par rates will NOT equal coupons (YTM "  );
            Console.WriteLine( "will, with the correct compounding), but the "     );
            Console.WriteLine( "piecewise yield curve par rates can be used as "   );
            Console.WriteLine( "a benchmark for correct par rates.");
            Console.WriteLine();

            today = calendar.advance(origToday,23,TimeUnit.Months,convention);
            Settings.setEvaluationDate(today);
            bondSettlementDate = calendar.advance(today, new Period(bondSettlementDays,TimeUnit.Days));

            printOutput("(a) exponential splines", ts1);

            printOutput("(b) simple polynomial", ts2);

            printOutput("(c) Nelson-Siegel", ts3);

            printOutput("(d) cubic B-splines", ts4);

            printOutput("(e) Svensson", ts5);

            printOutput("(f) Nelson-Siegel spreaded", ts6);

            Console.WriteLine("\n");


            Console.WriteLine( " tenor" + " | "
                             + "coupon" + " | "
                             + "bstrap" + " | "
                             + "   (a)" + " | "
                             + "   (b)" + " | "
                             + "   (c)" + " | "
                             + "   (d)" + " | "
                             + "   (e)" + " | "
                             + "   (f)" );

            for (int i=0; i<instrumentsA.Count; i++) 
            {
               List<CashFlow> cfs = instrumentsA[i].bond().cashflows();

               int cfSize = instrumentsA[i].bond().cashflows().Count;
               List<Date> keyDates = new List<Date>();
               keyDates.Add(bondSettlementDate);

               for (int j=0; j<cfSize-1; j++) 
               {
                  if (!cfs[j].hasOccurred(bondSettlementDate, false)) 
                  {
                     Date myDate =  cfs[j].date();
                     keyDates.Add(myDate);
                  }
               }

               double tenor = dc.yearFraction(today, cfs[cfSize-1].date());
              
               double test = parRate( ts0, keyDates, dc );

               Console.WriteLine( tenor.ToString( "##.000" ).PadLeft( 6 ) + " | "
                                  + ( 100.0 * coupons[i] ).ToString( "#.000" ).PadLeft( 6 ) + " | "
                                  // piecewise bootstrap
                                  + ( 100.0 * parRate( ts0, keyDates, dc ) ).ToString( "##.000" ).PadLeft( 6 ) + " | "
                                  // exponential splines
                                  + ( 100.0 * parRate( ts1, keyDates, dc ) ).ToString( "##.000" ).PadLeft( 6 ) + " | "
                                  // simple polynomial
                                  + ( 100.0 * parRate( ts2, keyDates, dc ) ).ToString( "##.000" ).PadLeft( 6 ) + " | "
                                  // Nelson-Siegel
                                  + ( 100.0 * parRate( ts3, keyDates, dc ) ).ToString( "##.000" ).PadLeft( 6 ) + " | "
                                  // cubic bsplines
                                  + ( 100.0 * parRate( ts4, keyDates, dc ) ).ToString( "##.000" ).PadLeft( 6 ) + " | "
                                  // Svensson
                                  + ( 100.0 * parRate( ts5, keyDates, dc ) ).ToString( "##.000" ).PadLeft( 6 ) + " | "
                                  // Nelson-Siegel Spreaded
                                  + ( 100.0 * parRate( ts6, keyDates, dc ) ).ToString( "##.000" ).PadLeft( 6 ) );
            }

            Console.WriteLine("\n\n");
            Console.WriteLine("Now add one more month, for a total of two years ");
            Console.WriteLine("from the original date. The first instrument is "  );
            Console.WriteLine("now expired and par rates should again equal "    );
            Console.WriteLine("coupon values, since clean prices did not change.");
            Console.WriteLine("\n");

            instrumentsA.RemoveRange(0, 1); // TODO
            instrumentsB.RemoveRange(0,1);  // TODO

            today = calendar.advance(origToday,24,TimeUnit.Months,convention);
            Settings.setEvaluationDate(today);
            bondSettlementDate = calendar.advance(today, new Period(bondSettlementDays,TimeUnit.Days));

            YieldTermStructure ts00 = new PiecewiseYieldCurve<Discount,LogLinear>(curveSettlementDays,
                                                                                  calendar,
                                                                                  instrumentsB,
                                                                                  dc);

            FittedBondDiscountCurve ts11 = new FittedBondDiscountCurve(curveSettlementDays,
                                                                       calendar,
                                                                       instrumentsA,
                                                                       dc,
                                                                       exponentialSplines,
                                                                       tolerance,
                                                                       max);

            printOutput("(a) exponential splines", ts11);


            FittedBondDiscountCurve ts22 = new FittedBondDiscountCurve(curveSettlementDays,
                                                                       calendar,
                                                                       instrumentsA,
                                                                       dc,
                                                                       simplePolynomial,
                                                                       tolerance,
                                                                       max);

            printOutput("(b) simple polynomial", ts22);


            FittedBondDiscountCurve ts33 = new FittedBondDiscountCurve(curveSettlementDays,
                                                                       calendar,
                                                                       instrumentsA,
                                                                       dc,
                                                                       nelsonSiegel,
                                                                       tolerance,
                                                                       max);

            printOutput("(c) Nelson-Siegel", ts33);


            FittedBondDiscountCurve ts44 = new FittedBondDiscountCurve(curveSettlementDays,
                                                                       calendar,
                                                                       instrumentsA,
                                                                       dc,
                                                                       cubicBSplines,
                                                                       tolerance,
                                                                       max);

            printOutput("(d) cubic B-splines", ts44);

            FittedBondDiscountCurve ts55 = new FittedBondDiscountCurve(curveSettlementDays,
                                                                       calendar,
                                                                       instrumentsA,
                                                                       dc,
                                                                       svensson,
                                                                       tolerance,
                                                                       max);

            printOutput("(e) Svensson", ts55);

            FittedBondDiscountCurve ts66 = new FittedBondDiscountCurve(curveSettlementDays,
                                                                       calendar,
                                                                       instrumentsA,
                                                                       dc,
                                                                       nelsonSiegelSpread,
                                                                       tolerance,
                                                                       max);

            printOutput("(f) Nelson-Siegel spreaded", ts66);

            Console.WriteLine( " tenor" + " | "
                             + "coupon" + " | "
                             + "bstrap" + " | "
                             + "   (a)" + " | "
                             + "   (b)" + " | "
                             + "   (c)" + " | "
                             + "   (d)" + " | "
                             + "   (e)" + " | "
                             + "   (f)" );

            for (int i=0; i<instrumentsA.Count; i++) 
            {
               List<CashFlow> cfs = instrumentsA[i].bond().cashflows();

               int cfSize = instrumentsA[i].bond().cashflows().Count;
               List<Date> keyDates = new List<Date>();
               keyDates.Add(bondSettlementDate);

               for (int j=0; j<cfSize-1; j++) 
               {
                  if (!cfs[j].hasOccurred(bondSettlementDate, false)) 
                  {
                     Date myDate =  cfs[j].date();
                     keyDates.Add(myDate);
                  }
               }

               double tenor = dc.yearFraction(today, cfs[cfSize-1].date());

               Console.WriteLine( tenor.ToString( "##.000" ).PadLeft( 6 ) + " | "
                                  + ( 100.0 * coupons[i + 1] ).ToString( "##.000" ).PadLeft( 6 ) + " | "
                                  // piecewise bootstrap
                                  + ( 100.0 * parRate( ts00, keyDates, dc ) ).ToString( "##.000" ).PadLeft( 6 ) + " | "
                                  // exponential splines
                                  + ( 100.0 * parRate( ts11, keyDates, dc ) ).ToString( "##.000" ).PadLeft( 6 ) + " | "
                                  // simple polynomial
                                  + ( 100.0 * parRate( ts22, keyDates, dc ) ).ToString( "##.000" ).PadLeft( 6 ) + " | "
                                  // Nelson-Siegel
                                  + ( 100.0 * parRate( ts33, keyDates, dc ) ).ToString( "##.000" ).PadLeft( 6 ) + " | "
                                  // cubic bsplines
                                  + ( 100.0 * parRate( ts44, keyDates, dc ) ).ToString( "##.000" ).PadLeft( 6 ) + " | "
                                  // Svensson
                                  + ( 100.0 * parRate( ts55, keyDates, dc ) ).ToString( "##.000" ).PadLeft( 6 ) + " | "
                                  // Nelson-Siegel Spreaded
                                  + ( 100.0 * parRate( ts66, keyDates, dc ) ).ToString( "##.000" ).PadLeft( 6 ) );
            }


            Console.WriteLine("\n\n");
            Console.WriteLine("Now decrease prices by a small amount, corresponding");
            Console.WriteLine("to a theoretical five basis point parallel + shift of");
            Console.WriteLine("the yield curve. Because bond quotes change, the new ");
            Console.WriteLine("par rates should be recalculated automatically.");
            Console.WriteLine("\n");

            for (int k=0; k<lengths.Length -1; k++) 
            {

               double P = instrumentsA[k].quote().link.value();
               Bond b = instrumentsA[k].bond();
               double ytm = BondFunctions.yield(b, P, dc, Compounding.Compounded, frequency,today);
               double dur = BondFunctions.duration(b, ytm,dc, Compounding.Compounded, frequency,
                  Duration.Type.Modified,today);

               const double bpsChange = 5.0;
               // dP = -dur * P * dY
               double deltaP = -dur * P * (bpsChange/10000.0);
               quote[k+1].setValue(P + deltaP);
            }


            Console.WriteLine( " tenor" + " | "
                             + "coupon" + " | "
                             + "bstrap" + " | "
                             + "   (a)" + " | "
                             + "   (b)" + " | "
                             + "   (c)" + " | "
                             + "   (d)" + " | "
                             + "   (e)" + " | "
                             + "   (f)" );

            for (int i=0; i<instrumentsA.Count; i++) 
            {
               List<CashFlow> cfs = instrumentsA[i].bond().cashflows();

               int cfSize = instrumentsA[i].bond().cashflows().Count;
               List<Date> keyDates = new List<Date>();
               keyDates.Add(bondSettlementDate);

               for (int j=0; j<cfSize-1; j++) 
               {
                  if (!cfs[j].hasOccurred(bondSettlementDate, false)) 
                  {
                     Date myDate =  cfs[j].date();
                     keyDates.Add(myDate);
                  }
               }

               double tenor = dc.yearFraction(today, cfs[cfSize-1].date());

               Console.WriteLine( tenor.ToString( "##.000" ).PadLeft( 6 ) + " | "
                                  + ( 100.0 * coupons[i + 1] ).ToString( "##.000" ).PadLeft( 6 ) + " | "
                                  // piecewise bootstrap
                                  + ( 100.0 * parRate( ts00, keyDates, dc ) ).ToString( "##.000" ).PadLeft( 6 ) + " | "
                                  // exponential splines
                                  + ( 100.0 * parRate( ts11, keyDates, dc ) ).ToString( "##.000" ).PadLeft( 6 ) + " | "
                                  // simple polynomial
                                  + ( 100.0 * parRate( ts22, keyDates, dc ) ).ToString( "##.000" ).PadLeft( 6 ) + " | "
                                  // Nelson-Siegel
                                  + ( 100.0 * parRate( ts33, keyDates, dc ) ).ToString( "##.000" ).PadLeft( 6 ) + " | "
                                  // cubic bsplines
                                  + ( 100.0 * parRate( ts44, keyDates, dc ) ).ToString( "##.000" ).PadLeft( 6 ) + " | "
                                  // Svensson
                                  + ( 100.0 * parRate( ts55, keyDates, dc ) ).ToString( "##.000" ).PadLeft( 6 ) + " | "
                                  // Nelson-Siegel Spreaded
                                  + ( 100.0 * parRate( ts66, keyDates, dc ) ).ToString( "##.000" ).PadLeft( 6 ) );
            }



            Console.WriteLine(" \nRun completed in {0}", DateTime.Now - timer);
            Console.WriteLine();

            Console.Write("Press any key to continue ...");
            Console.ReadKey();
         } 
            
         catch (Exception e) 
         {
            Console.WriteLine( e.Message );
            return ;
         } 
      
      }
   }
}
