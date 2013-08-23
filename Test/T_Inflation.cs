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

      //[TestMethod()]
      //public void testZeroTermStructure() 
      //{
      //   // Testing zero inflation term structure...

      //   SavedSettings backup;

      //   // try the Zero UK
      //   Calendar calendar = new UnitedKingdom();
      //   BusinessDayConvention bdc = BusinessDayConvention.ModifiedFollowing;
      //   Date evaluationDate = new Date(13, Month.August, 2007);
      //   evaluationDate = calendar.adjust(evaluationDate);
      //   Settings.setEvaluationDate(evaluationDate);

      //   // fixing data
      //   Date from = new Date(1, Month.January, 2005);
      //   Date to = new Date(13, Month.August, 2007);
      //   Schedule rpiSchedule = new MakeSchedule().from(from).to(to)
      //                          .withTenor(new Period(1,TimeUnit.Months))
      //                          .withCalendar(new UnitedKingdom())
      //                          .withConvention(BusinessDayConvention.ModifiedFollowing)
      //                          .value();

      //   double[] fixData = { 189.9, 189.9, 189.6, 190.5, 191.6, 192.0,
      //                        192.2, 192.2, 192.6, 193.1, 193.3, 193.6,
      //                        194.1, 193.4, 194.2, 195.0, 196.5, 197.7,
      //                        198.5, 198.5, 199.2, 200.1, 200.4, 201.1,
      //                        202.7, 201.6, 203.1, 204.4, 205.4, 206.2,
      //                        207.3, 206.1,  -999.0 };   

      //   RelinkableHandle<ZeroInflationTermStructure> hz = new RelinkableHandle<ZeroInflationTermStructure>();
      //   bool interp = false;
      //   UKRPI iiUKRPI = new UKRPI(interp, hz);
      //   for (int i=0; i<rpiSchedule.Count;i++) 
      //   {
      //      iiUKRPI.addFixing(rpiSchedule[i], fixData[i]);
      //   }


      //   ZeroInflationIndex ii = iiUKRPI as ZeroInflationIndex;
      //   YieldTermStructure nominalTS = nominalTermStructure();


      //   // now build the zero inflation curve
    
      //   Datum[] zcData = {
      //     new Datum( new Date(13, Month.August, 2008), 2.93 ),
      //     new Datum( new Date(13, Month.August, 2009), 2.95 ),
      //     new Datum( new Date(13, Month.August, 2010), 2.965 ),
      //     new Datum( new Date(15, Month.August, 2011), 2.98 ),
      //     new Datum( new Date(13, Month.August, 2012), 3.0 ),
      //     new Datum( new Date(13, Month.August, 2014), 3.06 ),
      //     new Datum( new Date(13, Month.August, 2017), 3.175 ),
      //     new Datum( new Date(13, Month.August, 2019), 3.243 ),
      //     new Datum( new Date(15, Month.August, 2022), 3.293 ),
      //     new Datum( new Date(14, Month.August, 2027), 3.338 ),
      //     new Datum( new Date(13, Month.August, 2032), 3.348 ),
      //     new Datum( new Date(15, Month.August, 2037), 3.348 ),
      //     new Datum( new Date(13, Month.August, 2047), 3.308 ),
      //     new Datum( new Date(13, Month.August, 2057), 3.228 )};

    
      //   Period observationLag = new Period(2,TimeUnit.Months);
      //   DayCounter dc = new Thirty360();
      //   Frequency frequency = Frequency.Monthly;
      //   List<BootstrapHelper<ZeroInflationTermStructure>>  helpers =
      //      makeHelpers(zcData, zcData.Length, ii,
      //                              observationLag,
      //                              calendar, bdc, dc);

      //   double baseZeroRate = zcData[0].rate/100.0;
      //   //PiecewiseZeroInflationCurve<Linear> pZITS = new PiecewiseZeroInflationCurve<Linear>(
      //   //               evaluationDate, calendar, dc, observationLag,
      //   //               frequency, ii.interpolated(), baseZeroRate,
      //   //               new Handle<YieldTermStructure>(nominalTS), helpers);
      //   //pZITS.recalculate();

      //}
   }
}
