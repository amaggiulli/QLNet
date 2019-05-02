//  Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)
//
//  This file is part of QLNet Project https://github.com/amaggiulli/qlnet
//  QLNet is free software: you can redistribute it and/or modify it
//  under the terms of the QLNet license.  You should have received a
//  copy of the license along with this program; if not, license is
//  available at <https://github.com/amaggiulli/QLNet/blob/develop/LICENSE>.
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
#if NET452
using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
using Xunit;
#endif
using QLNet;

namespace TestSuite
{
#if NET452
   [TestClass()]
#endif
   public class T_InflationCPICapFloor
   {
      internal struct Datum
      {
         public Date date;
         public double rate;

         public Datum(Date d, double r)
         {
            date = d;
            rate = r;
         }
      }

      private class CommonVars
      {
         private List<BootstrapHelper<ZeroInflationTermStructure>> makeHelpers(Datum[] iiData, int N,
                                                                               ZeroInflationIndex ii, Period observationLag,
                                                                               Calendar calendar,
                                                                               BusinessDayConvention bdc,
                                                                               DayCounter dc)
         {
            List<BootstrapHelper<ZeroInflationTermStructure>> instruments = new List<BootstrapHelper<ZeroInflationTermStructure>>();
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

         // common data
         public int length;
         public Date startDate;
         public double baseZeroRate;
         public double volatility;

         public Frequency frequency;
         public List<double> nominals;
         public Calendar calendar;
         public BusinessDayConvention convention;
         public int fixingDays;
         public Date evaluationDate;
         public int settlementDays;
         public Date settlement;
         public Period observationLag, contractObservationLag;
         public InterpolationType contractObservationInterpolation;
         public DayCounter dcZCIIS, dcNominal;
         public List<Date> zciisD;
         public List<double> zciisR;
         public UKRPI ii;
         public RelinkableHandle<ZeroInflationIndex> hii;
         public int zciisDataLength;

         public RelinkableHandle<YieldTermStructure> nominalUK;
         public RelinkableHandle<ZeroInflationTermStructure> cpiUK;
         public RelinkableHandle<ZeroInflationTermStructure> hcpi;

         public List<double> cStrikesUK;
         public List<double> fStrikesUK;
         public List<Period> cfMaturitiesUK;
         public Matrix cPriceUK;
         public Matrix fPriceUK;

         public CPICapFloorTermPriceSurface cpiCFsurfUK;

         // cleanup

         public SavedSettings backup;

         // setup
         public CommonVars()
         {
            backup = new SavedSettings();
            nominalUK = new RelinkableHandle<YieldTermStructure>();
            cpiUK = new RelinkableHandle<ZeroInflationTermStructure>();
            hcpi = new RelinkableHandle<ZeroInflationTermStructure>();
            zciisD = new List<Date>();
            zciisR = new List<double>();
            hii = new RelinkableHandle<ZeroInflationIndex>();

            nominals = new InitializedList<double>(1, 1000000);
            // option variables
            frequency = Frequency.Annual;
            // usual setup
            volatility = 0.01;
            length = 7;
            calendar = new UnitedKingdom();
            convention = BusinessDayConvention.ModifiedFollowing;
            Date today = new Date(1, Month.June, 2010);
            evaluationDate = calendar.adjust(today);
            Settings.setEvaluationDate(evaluationDate);
            settlementDays = 0;
            fixingDays = 0;
            settlement = calendar.advance(today, settlementDays, TimeUnit.Days);
            startDate = settlement;
            dcZCIIS = new ActualActual();
            dcNominal = new ActualActual();

            // uk rpi index
            //      fixing data
            Date from = new Date(1, Month.July, 2007);
            Date to = new Date(1, Month.June, 2010);
            Schedule rpiSchedule = new MakeSchedule().from(from).to(to)
            .withTenor(new Period(1, TimeUnit.Months))
            .withCalendar(new UnitedKingdom())
            .withConvention(BusinessDayConvention.ModifiedFollowing).value();
            double[] fixData =
            {
               206.1, 207.3, 208.0, 208.9, 209.7, 210.9,
               209.8, 211.4, 212.1, 214.0, 215.1, 216.8,   //  2008
               216.5, 217.2, 218.4, 217.7, 216.0, 212.9,
               210.1, 211.4, 211.3, 211.5, 212.8, 213.4,   //  2009
               213.4, 214.4, 215.3, 216.0, 216.6, 218.0,
               217.9, 219.2, 220.7, 222.8, -999, -999,     //  2010
               -999
            };

            // link from cpi index to cpi TS
            bool interp = false;// this MUST be false because the observation lag is only 2 months
            // for ZCIIS; but not for contract if the contract uses a bigger lag.
            ii = new UKRPI(interp, hcpi);
            for (int i = 0; i < rpiSchedule.Count; i++)
            {
               ii.addFixing(rpiSchedule[i], fixData[i], true);// force overwrite in case multiple use
            }

            Datum[] nominalData =
            {
               new Datum(new Date(2, Month.June, 2010), 0.499997),
               new Datum(new Date(3, Month.June, 2010), 0.524992),
               new Datum(new Date(8, Month.June, 2010), 0.524974),
               new Datum(new Date(15, Month.June, 2010), 0.549942),
               new Datum(new Date(22, Month.June, 2010), 0.549913),
               new Datum(new Date(1, Month.July, 2010), 0.574864),
               new Datum(new Date(2, Month.August, 2010), 0.624668),
               new Datum(new Date(1, Month.September, 2010), 0.724338),
               new Datum(new Date(16, Month.September, 2010), 0.769461),
               new Datum(new Date(1, Month.December, 2010), 0.997501),
               //{ Date( 16, December, 2010), 0.838164 ),
               new Datum(new Date(17, Month.March, 2011), 0.916996),
               new Datum(new Date(16, Month.June, 2011), 0.984339),
               new Datum(new Date(22, Month.September, 2011), 1.06085),
               new Datum(new Date(22, Month.December, 2011), 1.141788),
               new Datum(new Date(1, Month.June, 2012), 1.504426),
               new Datum(new Date(3, Month.June, 2013), 1.92064),
               new Datum(new Date(2, Month.June, 2014), 2.290824),
               new Datum(new Date(1, Month.June, 2015), 2.614394),
               new Datum(new Date(1, Month.June, 2016), 2.887445),
               new Datum(new Date(1, Month.June, 2017), 3.122128),
               new Datum(new Date(1, Month.June, 2018), 3.322511),
               new Datum(new Date(3, Month.June, 2019), 3.483997),
               new Datum(new Date(1, Month.June, 2020), 3.616896),
               new Datum(new Date(1, Month.June, 2022), 3.8281),
               new Datum(new Date(2, Month.June, 2025), 4.0341),
               new Datum(new Date(3, Month.June, 2030), 4.070854),
               new Datum(new Date(1, Month.June, 2035), 4.023202),
               new Datum(new Date(1, Month.June, 2040), 3.954748),
               new Datum(new Date(1, Month.June, 2050), 3.870953),
               new Datum(new Date(1, Month.June, 2060), 3.85298),
               new Datum(new Date(2, Month.June, 2070), 3.757542),
               new Datum(new Date(3, Month.June, 2080), 3.651379)
            };
            int nominalDataLength = 33 - 1;

            List<Date> nomD = new List<Date>();
            List<double> nomR = new List<double>();
            for (int i = 0; i < nominalDataLength; i++)
            {
               nomD.Add(nominalData[i].date);
               nomR.Add(nominalData[i].rate / 100.0);
            }
            YieldTermStructure nominalTS = new InterpolatedZeroCurve<Linear>(nomD, nomR, dcNominal);
            nominalUK.linkTo(nominalTS);

            // now build the zero inflation curve
            observationLag = new Period(2, TimeUnit.Months);
            contractObservationLag = new Period(3, TimeUnit.Months);
            contractObservationInterpolation = InterpolationType.Flat;

            Datum[] zciisData =
            {
               new Datum(new Date(1, Month.June, 2011), 3.087),
               new Datum(new Date(1, Month.June, 2012), 3.12),
               new Datum(new Date(1, Month.June, 2013), 3.059),
               new Datum(new Date(1, Month.June, 2014), 3.11),
               new Datum(new Date(1, Month.June, 2015), 3.15),
               new Datum(new Date(1, Month.June, 2016), 3.207),
               new Datum(new Date(1, Month.June, 2017), 3.253),
               new Datum(new Date(1, Month.June, 2018), 3.288),
               new Datum(new Date(1, Month.June, 2019), 3.314),
               new Datum(new Date(1, Month.June, 2020), 3.401),
               new Datum(new Date(1, Month.June, 2022), 3.458),
               new Datum(new Date(1, Month.June, 2025), 3.52),
               new Datum(new Date(1, Month.June, 2030), 3.655),
               new Datum(new Date(1, Month.June, 2035), 3.668),
               new Datum(new Date(1, Month.June, 2040), 3.695),
               new Datum(new Date(1, Month.June, 2050), 3.634),
               new Datum(new Date(1, Month.June, 2060), 3.629),
            };
            zciisDataLength = 17;
            for (int i = 0; i < zciisDataLength; i++)
            {
               zciisD.Add(zciisData[i].date);
               zciisR.Add(zciisData[i].rate);
            }

            // now build the helpers ...
            List<BootstrapHelper<ZeroInflationTermStructure> >  helpers = makeHelpers(zciisData, zciisDataLength, ii,
                                                                                      observationLag, calendar, convention, dcZCIIS);

            // we can use historical or first ZCIIS for this
            // we know historical is WAY off market-implied, so use market implied flat.
            baseZeroRate = zciisData[0].rate / 100.0;
            PiecewiseZeroInflationCurve<Linear> pCPIts = new PiecewiseZeroInflationCurve<Linear>(
               evaluationDate, calendar, dcZCIIS, observationLag, ii.frequency(), ii.interpolated(), baseZeroRate,
               new Handle<YieldTermStructure>(nominalTS), helpers);
            pCPIts.recalculate();
            cpiUK.linkTo(pCPIts);
            hii.linkTo(ii);

            // make sure that the index has the latest zero inflation term structure
            hcpi.linkTo(pCPIts);

            // cpi CF price surf data
            Period[] cfMat = {new Period(3, TimeUnit.Years),
                      new Period(5, TimeUnit.Years),
                      new Period(7, TimeUnit.Years),
                      new Period(10, TimeUnit.Years),
                      new Period(15, TimeUnit.Years),
                      new Period(20, TimeUnit.Years),
                      new Period(30, TimeUnit.Years)
            };
            double[] cStrike = {3, 4, 5, 6};
            double[] fStrike = {-1, 0, 1, 2};
            int ncStrikes = 4, nfStrikes = 4, ncfMaturities = 7;

            double[][] cPrice =
            {
               new double[4] {227.6, 100.27, 38.8, 14.94},
               new double[4] {345.32, 127.9, 40.59, 14.11},
               new double[4] {477.95, 170.19, 50.62, 16.88},
               new double[4] {757.81, 303.95, 107.62, 43.61},
               new double[4] {1140.73, 481.89, 168.4, 63.65},
               new double[4] {1537.6, 607.72, 172.27, 54.87},
               new double[4] {2211.67, 839.24, 184.75, 45.03}
            };

            double[][] fPrice =
            {
               new double[4] {15.62, 28.38, 53.61, 104.6},
               new double[4] {21.45, 36.73, 66.66, 129.6},
               new double[4] {24.45, 42.08, 77.04, 152.24},
               new double[4] {39.25, 63.52, 109.2, 203.44},
               new double[4] {36.82, 63.62, 116.97, 232.73},
               new double[4] {39.7, 67.47, 121.79, 238.56},
               new double[4] {41.48, 73.9, 139.75, 286.75}
            };

            // now load the data into vector and Matrix classes
            cStrikesUK = new List<double>();
            fStrikesUK = new List<double>();
            cfMaturitiesUK = new List<Period>();
            for (int i = 0; i < ncStrikes; i++)
               cStrikesUK.Add(cStrike[i]);
            for (int i = 0; i < nfStrikes; i++)
               fStrikesUK.Add(fStrike[i]);
            for (int i = 0; i < ncfMaturities; i++)
               cfMaturitiesUK.Add(cfMat[i]);
            cPriceUK = new Matrix(ncStrikes, ncfMaturities);
            fPriceUK = new Matrix(nfStrikes, ncfMaturities);
            for (int i = 0; i < ncStrikes; i++)
            {
               for (int j = 0; j < ncfMaturities; j++)
               {
                  (cPriceUK)[i, j] = cPrice[j][i] / 10000.0;
               }
            }
            for (int i = 0; i < nfStrikes; i++)
            {
               for (int j = 0; j < ncfMaturities; j++)
               {
                  (fPriceUK)[i, j] = fPrice[j][i] / 10000.0;
               }
            }
         }
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void cpicapfloorpricesurface()
      {
         // check inflation leg vs calculation directly from inflation TS
         CommonVars common = new CommonVars();

         double nominal = 1.0;
         InterpolatedCPICapFloorTermPriceSurface<Bilinear> cpiSurf = new InterpolatedCPICapFloorTermPriceSurface<Bilinear>(
            nominal,
            common.baseZeroRate,
            common.observationLag,
            common.calendar,
            common.convention,
            common.dcZCIIS,
            common.hii,
            common.nominalUK,
            common.cStrikesUK,
            common.fStrikesUK,
            common.cfMaturitiesUK,
            common.cPriceUK,
            common.fPriceUK);

         // test code - note order of indices
         for (int i = 0; i < common.fStrikesUK.Count; i++)
         {
            double qK = common.fStrikesUK[i];
            int nMat = common.cfMaturitiesUK.Count;
            for (int j = 0; j < nMat; j++)
            {
               Period t = common.cfMaturitiesUK[j];
               double a = common.fPriceUK[i, j];
               double b = cpiSurf.floorPrice(t, qK);

               Utils.QL_REQUIRE(Math.Abs(a - b) < 1e-7, () => "cannot reproduce cpi floor data from surface: "
                                + a + " vs constructed = " + b);
            }

         }

         for (int i = 0; i < common.cStrikesUK.Count; i++)
         {
            double qK = common.cStrikesUK[i];
            int nMat = common.cfMaturitiesUK.Count;
            for (int j = 0; j < nMat; j++)
            {
               Period t = common.cfMaturitiesUK[j];
               double a = common.cPriceUK[i, j];
               double b = cpiSurf.capPrice(t, qK);

               QAssert.IsTrue(Math.Abs(a - b) < 1e-7, "cannot reproduce cpi cap data from surface: "
                              + a + " vs constructed = " + b);
            }
         }

         // remove circular refernce
         common.hcpi.linkTo(null);
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void cpicapfloorpricer()
      {
         CommonVars common = new CommonVars();
         double nominal = 1.0;
         CPICapFloorTermPriceSurface cpiCFpriceSurf = new InterpolatedCPICapFloorTermPriceSurface
         <Bilinear>(nominal,
                    common.baseZeroRate,
                    common.observationLag,
                    common.calendar,
                    common.convention,
                    common.dcZCIIS,
                    common.hii,
                    common.nominalUK,
                    common.cStrikesUK,
                    common.fStrikesUK,
                    common.cfMaturitiesUK,
                    common.cPriceUK,
                    common.fPriceUK);

         common.cpiCFsurfUK = cpiCFpriceSurf;

         // interpolation pricer first
         // N.B. no new instrument required but we do need a new pricer

         Date startDate = Settings.evaluationDate();
         Date maturity = (startDate + new Period(3, TimeUnit.Years));
         Calendar fixCalendar = new UnitedKingdom(), payCalendar = new UnitedKingdom();
         BusinessDayConvention fixConvention = BusinessDayConvention.Unadjusted,
                               payConvention = BusinessDayConvention.ModifiedFollowing;
         double strike = 0.03;
         double baseCPI = common.hii.link.fixing(fixCalendar.adjust(startDate - common.observationLag, fixConvention));
         InterpolationType observationInterpolation = InterpolationType.AsIndex;
         CPICapFloor aCap = new CPICapFloor(Option.Type.Call,
                                            nominal,
                                            startDate,   // start date of contract (only)
                                            baseCPI,
                                            maturity,    // this is pre-adjustment!
                                            fixCalendar,
                                            fixConvention,
                                            payCalendar,
                                            payConvention,
                                            strike,
                                            common.hii,
                                            common.observationLag,
                                            observationInterpolation);

         Handle<CPICapFloorTermPriceSurface> cpiCFsurfUKh = new Handle<CPICapFloorTermPriceSurface>(common.cpiCFsurfUK);
         IPricingEngine engine = new InterpolatingCPICapFloorEngine(cpiCFsurfUKh);

         aCap.setPricingEngine(engine);

         Date d = common.cpiCFsurfUK.cpiOptionDateFromTenor(new Period(3, TimeUnit.Years));


         double cached = cpiCFsurfUKh.link.capPrice(d, strike);
         QAssert.IsTrue(Math.Abs(cached - aCap.NPV()) < 1e-10, "InterpolatingCPICapFloorEngine does not reproduce cached price: "
                        + cached + " vs " + aCap.NPV());

         // remove circular refernce
         common.hcpi.linkTo(null);
      }
   }
}
