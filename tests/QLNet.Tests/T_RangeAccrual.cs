//  Copyright (C) 2008-2018 Andrea Maggiulli (a.maggiulli@gmail.com)
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
   public class T_RangeAccrual
   {
      private class CommonVars
      {
         // General settings
         public Date referenceDate, today, settlement;
         public Calendar calendar;

         // Volatility Stuctures
         public List<Handle<SwaptionVolatilityStructure>> swaptionVolatilityStructures;
         public Handle<SwaptionVolatilityStructure> atmVol;
         public Handle<SwaptionVolatilityStructure> flatSwaptionVolatilityCube1;
         public Handle<SwaptionVolatilityStructure> flatSwaptionVolatilityCube2;
         public Handle<SwaptionVolatilityStructure> swaptionVolatilityCubeBySabr;

         public List<Period> atmOptionTenors, optionTenors;
         public List<Period> atmSwapTenors, swapTenors;
         public List<double> strikeSpreads;

         public Matrix atmVolMatrix, volSpreadsMatrix;
         public List<List<Handle<Quote>>> volSpreads;

         public DayCounter dayCounter;
         public BusinessDayConvention optionBDC;
         public int swapSettlementDays;
         public bool vegaWeightedSmileFit;

         // Range Accrual valuation
         public double infiniteLowerStrike, infiniteUpperStrike;
         public double gearing, correlation;
         public double spread;
         public Date startDate;
         public Date endDate;
         public Date paymentDate;
         public int fixingDays;
         public DayCounter rangeCouponDayCount;
         public Schedule observationSchedule;
         // Observation Schedule conventions
         public Frequency observationsFrequency;
         public BusinessDayConvention observationsConvention;

         // Term Structure
         public RelinkableHandle<YieldTermStructure> termStructure = new RelinkableHandle<YieldTermStructure>();

         // indices and index conventions
         public Frequency fixedLegFrequency;
         public BusinessDayConvention fixedLegConvention;
         public DayCounter fixedLegDayCounter;
         public IborIndex iborIndex;

         // Range accrual pricers properties
         public List<bool> byCallSpread;
         public double flatVol;
         public List<SmileSection> smilesOnExpiry;
         public List<SmileSection> smilesOnPayment;

         //test parameters
         public double rateTolerance;
         public double priceTolerance;


         // cleanup
         SavedSettings backup = new SavedSettings();

         public void createYieldCurve()
         {

            // Yield Curve
            List<Date> dates = new List<Date>();
            dates.Add(new Date(39147)); dates.Add(new Date(39148)); dates.Add(new Date(39151));
            dates.Add(new Date(39153)); dates.Add(new Date(39159)); dates.Add(new Date(39166));
            dates.Add(new Date(39183)); dates.Add(new Date(39294)); dates.Add(new Date(39384));
            dates.Add(new Date(39474)); dates.Add(new Date(39567)); dates.Add(new Date(39658));
            dates.Add(new Date(39748)); dates.Add(new Date(39839)); dates.Add(new Date(39931));
            dates.Add(new Date(40250)); dates.Add(new Date(40614)); dates.Add(new Date(40978));
            dates.Add(new Date(41344)); dates.Add(new Date(41709)); dates.Add(new Date(42074));
            dates.Add(new Date(42441)); dates.Add(new Date(42805)); dates.Add(new Date(43170));
            dates.Add(new Date(43535)); dates.Add(new Date(43900)); dates.Add(new Date(44268));
            dates.Add(new Date(44632)); dates.Add(new Date(44996)); dates.Add(new Date(45361));
            dates.Add(new Date(45727)); dates.Add(new Date(46092)); dates.Add(new Date(46459));
            dates.Add(new Date(46823)); dates.Add(new Date(47188)); dates.Add(new Date(47553));
            dates.Add(new Date(47918)); dates.Add(new Date(48283)); dates.Add(new Date(48650));
            dates.Add(new Date(49014)); dates.Add(new Date(49379)); dates.Add(new Date(49744));
            dates.Add(new Date(50110)); dates.Add(new Date(53762)); dates.Add(new Date(57415));
            dates.Add(new Date(61068));

            List<double> zeroRates = new List<double>();
            zeroRates.Add(0.02676568527); zeroRates.Add(0.02676568527);
            zeroRates.Add(0.02676333038); zeroRates.Add(0.02682286201);
            zeroRates.Add(0.02682038347); zeroRates.Add(0.02683030208);
            zeroRates.Add(0.02700136766); zeroRates.Add(0.02932526033);
            zeroRates.Add(0.03085568949); zeroRates.Add(0.03216370631);
            zeroRates.Add(0.03321234116); zeroRates.Add(0.03404978072);
            zeroRates.Add(0.03471117149); zeroRates.Add(0.03527141916);
            zeroRates.Add(0.03574660393); zeroRates.Add(0.03691715582);
            zeroRates.Add(0.03796468718); zeroRates.Add(0.03876457629);
            zeroRates.Add(0.03942029708); zeroRates.Add(0.03999925325);
            zeroRates.Add(0.04056663618); zeroRates.Add(0.04108743922);
            zeroRates.Add(0.04156156761); zeroRates.Add(0.0419979179);
            zeroRates.Add(0.04239486483); zeroRates.Add(0.04273799032);
            zeroRates.Add(0.04305531203); zeroRates.Add(0.04336417578);
            zeroRates.Add(0.04364017665); zeroRates.Add(0.04388153459);
            zeroRates.Add(0.04408005012); zeroRates.Add(0.04424764425);
            zeroRates.Add(0.04437504759); zeroRates.Add(0.04447696334);
            zeroRates.Add(0.04456212318); zeroRates.Add(0.04464090072);
            zeroRates.Add(0.0447068707); zeroRates.Add(0.04475921774);
            zeroRates.Add(0.04477418345); zeroRates.Add(0.04477880755);
            zeroRates.Add(0.04476692489); zeroRates.Add(0.04473779454);
            zeroRates.Add(0.04468646066); zeroRates.Add(0.04430951558);
            zeroRates.Add(0.04363922313); zeroRates.Add(0.04363601992);

            termStructure.linkTo(new InterpolatedZeroCurve<Linear>(dates, zeroRates, new Actual365Fixed()));
         }

         public void createVolatilityStructures()
         {

            // ATM swaptionvol matrix
            optionBDC = BusinessDayConvention.Following;

            atmOptionTenors = new List<Period>();
            atmOptionTenors.Add(new Period(1, TimeUnit.Months));
            atmOptionTenors.Add(new Period(6, TimeUnit.Months));
            atmOptionTenors.Add(new Period(1, TimeUnit.Years));
            atmOptionTenors.Add(new Period(5, TimeUnit.Years));
            atmOptionTenors.Add(new Period(10, TimeUnit.Years));
            atmOptionTenors.Add(new Period(30, TimeUnit.Years));

            atmSwapTenors = new List<Period>();
            atmSwapTenors.Add(new Period(1, TimeUnit.Years));
            atmSwapTenors.Add(new Period(5, TimeUnit.Years));
            atmSwapTenors.Add(new Period(10, TimeUnit.Years));
            atmSwapTenors.Add(new Period(30, TimeUnit.Years));

            atmVolMatrix = new Matrix(atmOptionTenors.Count, atmSwapTenors.Count);

            atmVolMatrix[0, 0] = flatVol; atmVolMatrix[0, 1] = flatVol; atmVolMatrix[0, 2] = flatVol; atmVolMatrix[0, 3] = flatVol;
            atmVolMatrix[1, 0] = flatVol; atmVolMatrix[1, 1] = flatVol; atmVolMatrix[1, 2] = flatVol; atmVolMatrix[1, 3] = flatVol;
            atmVolMatrix[2, 0] = flatVol; atmVolMatrix[2, 1] = flatVol; atmVolMatrix[2, 2] = flatVol; atmVolMatrix[2, 3] = flatVol;
            atmVolMatrix[3, 0] = flatVol; atmVolMatrix[3, 1] = flatVol; atmVolMatrix[3, 2] = flatVol; atmVolMatrix[3, 3] = flatVol;
            atmVolMatrix[4, 0] = flatVol; atmVolMatrix[4, 1] = flatVol; atmVolMatrix[4, 2] = flatVol; atmVolMatrix[4, 3] = flatVol;
            atmVolMatrix[5, 0] = flatVol; atmVolMatrix[5, 1] = flatVol; atmVolMatrix[5, 2] = flatVol; atmVolMatrix[5, 3] = flatVol;

            int nRowsAtmVols = atmVolMatrix.rows();
            int nColsAtmVols = atmVolMatrix.columns();


            //swaptionvolcube
            optionTenors = new List<Period>();
            optionTenors.Add(new Period(1, TimeUnit.Years));
            optionTenors.Add(new Period(10, TimeUnit.Years));
            optionTenors.Add(new Period(30, TimeUnit.Years));

            swapTenors = new List<Period>();
            swapTenors.Add(new Period(2, TimeUnit.Years));
            swapTenors.Add(new Period(10, TimeUnit.Years));
            swapTenors.Add(new Period(30, TimeUnit.Years));

            strikeSpreads = new List<double>();
            strikeSpreads.Add(-0.020);
            strikeSpreads.Add(-0.005);
            strikeSpreads.Add(+0.000);
            strikeSpreads.Add(+0.005);
            strikeSpreads.Add(+0.020);

            int nRows = optionTenors.Count * swapTenors.Count;
            int nCols = strikeSpreads.Count;
            volSpreadsMatrix = new Matrix(nRows, nCols);
            volSpreadsMatrix[0, 0] = 0.0599; volSpreadsMatrix[0, 1] = 0.0049;
            volSpreadsMatrix[0, 2] = 0.0000;
            volSpreadsMatrix[0, 3] = -0.0001; volSpreadsMatrix[0, 4] = 0.0127;

            volSpreadsMatrix[1, 0] = 0.0729; volSpreadsMatrix[1, 1] = 0.0086;
            volSpreadsMatrix[1, 2] = 0.0000;
            volSpreadsMatrix[1, 3] = -0.0024; volSpreadsMatrix[1, 4] = 0.0098;

            volSpreadsMatrix[2, 0] = 0.0738; volSpreadsMatrix[2, 1] = 0.0102;
            volSpreadsMatrix[2, 2] = 0.0000;
            volSpreadsMatrix[2, 3] = -0.0039; volSpreadsMatrix[2, 4] = 0.0065;

            volSpreadsMatrix[3, 0] = 0.0465; volSpreadsMatrix[3, 1] = 0.0063;
            volSpreadsMatrix[3, 2] = 0.0000;
            volSpreadsMatrix[3, 3] = -0.0032; volSpreadsMatrix[3, 4] = -0.0010;

            volSpreadsMatrix[4, 0] = 0.0558; volSpreadsMatrix[4, 1] = 0.0084;
            volSpreadsMatrix[4, 2] = 0.0000;
            volSpreadsMatrix[4, 3] = -0.0050; volSpreadsMatrix[4, 4] = -0.0057;

            volSpreadsMatrix[5, 0] = 0.0576; volSpreadsMatrix[5, 1] = 0.0083;
            volSpreadsMatrix[5, 2] = 0.0000;
            volSpreadsMatrix[5, 3] = -0.0043; volSpreadsMatrix[5, 4] = -0.0014;

            volSpreadsMatrix[6, 0] = 0.0437; volSpreadsMatrix[6, 1] = 0.0059;
            volSpreadsMatrix[6, 2] = 0.0000;
            volSpreadsMatrix[6, 3] = -0.0030; volSpreadsMatrix[6, 4] = -0.0006;

            volSpreadsMatrix[7, 0] = 0.0533; volSpreadsMatrix[7, 1] = 0.0078;
            volSpreadsMatrix[7, 2] = 0.0000;
            volSpreadsMatrix[7, 3] = -0.0045; volSpreadsMatrix[7, 4] = -0.0046;

            volSpreadsMatrix[8, 0] = 0.0545; volSpreadsMatrix[8, 1] = 0.0079;
            volSpreadsMatrix[8, 2] = 0.0000;
            volSpreadsMatrix[8, 3] = -0.0042; volSpreadsMatrix[8, 4] = -0.0020;


            swapSettlementDays = 2;
            fixedLegFrequency = Frequency.Annual;
            fixedLegConvention = BusinessDayConvention.Unadjusted;
            fixedLegDayCounter = new Thirty360();
            SwapIndex swapIndexBase = new EuriborSwapIsdaFixA(new Period(2, TimeUnit.Years), termStructure);

            SwapIndex shortSwapIndexBase = new EuriborSwapIsdaFixA(new Period(1, TimeUnit.Years), termStructure);

            vegaWeightedSmileFit = false;

            // ATM Volatility structure
            List<List<Handle<Quote>>> atmVolsHandle;
            atmVolsHandle = new InitializedList<List<Handle<Quote>>>(nRowsAtmVols);
            int i;
            for (i = 0; i < nRowsAtmVols; i++)
            {
               atmVolsHandle[i] = new InitializedList<Handle<Quote>>(nColsAtmVols);
               for (int j = 0; j < nColsAtmVols; j++)
               {
                  atmVolsHandle[i][j] = new Handle<Quote>(new SimpleQuote(atmVolMatrix[i, j]));
               }
            }

            dayCounter = new Actual365Fixed();

            atmVol = new Handle<SwaptionVolatilityStructure>(new SwaptionVolatilityMatrix(calendar,
                                                                                          optionBDC, atmOptionTenors, atmSwapTenors, atmVolsHandle, dayCounter));

            // Volatility Cube without smile
            List<List<Handle<Quote>>> parametersGuess = new InitializedList<List<Handle<Quote>>>(optionTenors.Count * swapTenors.Count);
            for (i = 0; i < optionTenors.Count * swapTenors.Count; i++)
            {
               parametersGuess[i] = new InitializedList<Handle<Quote>>(4);
               parametersGuess[i][0] = new Handle<Quote>(new SimpleQuote(0.2));
               parametersGuess[i][1] = new Handle<Quote>(new SimpleQuote(0.5));
               parametersGuess[i][2] = new Handle<Quote>(new SimpleQuote(0.4));
               parametersGuess[i][3] = new Handle<Quote>(new SimpleQuote(0.0));
            }
            List<bool> isParameterFixed = new InitializedList<bool>(4, false);
            isParameterFixed[1] = true;

            List<List<Handle<Quote>>> nullVolSpreads = new InitializedList<List<Handle<Quote>>>(nRows);
            for (i = 0; i < optionTenors.Count * swapTenors.Count; i++)
            {
               nullVolSpreads[i] = new InitializedList<Handle<Quote>>(nCols);
               for (int j = 0; j < strikeSpreads.Count; j++)
               {
                  nullVolSpreads[i][j] = new Handle<Quote>(new SimpleQuote(0.0));
               }
            }

            SwaptionVolCube1x flatSwaptionVolatilityCube1ptr = new SwaptionVolCube1x(
               atmVol, optionTenors, swapTenors, strikeSpreads, nullVolSpreads, swapIndexBase,
               shortSwapIndexBase, vegaWeightedSmileFit, parametersGuess, isParameterFixed,
               false);
            flatSwaptionVolatilityCube1 = new Handle<SwaptionVolatilityStructure>(flatSwaptionVolatilityCube1ptr);
            flatSwaptionVolatilityCube1.link.enableExtrapolation();

            SwaptionVolCube2 flatSwaptionVolatilityCube2ptr = new SwaptionVolCube2(atmVol,
                                                                                   optionTenors, swapTenors, strikeSpreads, nullVolSpreads, swapIndexBase,
                                                                                   shortSwapIndexBase, vegaWeightedSmileFit);
            flatSwaptionVolatilityCube2 = new Handle<SwaptionVolatilityStructure>(flatSwaptionVolatilityCube2ptr);
            flatSwaptionVolatilityCube2.link.enableExtrapolation();


            // Volatility Cube with smile
            volSpreads = new InitializedList<List<Handle<Quote>>>(nRows);
            for (i = 0; i < optionTenors.Count * swapTenors.Count; i++)
            {
               volSpreads[i] = new InitializedList<Handle<Quote>>(nCols);
               for (int j = 0; j < strikeSpreads.Count; j++)
               {
                  volSpreads[i][j] = new Handle<Quote>(new SimpleQuote(volSpreadsMatrix[i, j]));
               }
            }

            SwaptionVolCube1x swaptionVolatilityCubeBySabrPtr = new SwaptionVolCube1x(
               atmVol,
               optionTenors,
               swapTenors,
               strikeSpreads,
               volSpreads,
               swapIndexBase,
               shortSwapIndexBase,
               vegaWeightedSmileFit,
               parametersGuess,
               isParameterFixed,
               false);
            swaptionVolatilityCubeBySabr = new Handle<SwaptionVolatilityStructure>(swaptionVolatilityCubeBySabrPtr);
            swaptionVolatilityCubeBySabr.link.enableExtrapolation();

            swaptionVolatilityStructures = new List<Handle<SwaptionVolatilityStructure>>();
            swaptionVolatilityStructures.Add(flatSwaptionVolatilityCube2);
            swaptionVolatilityStructures.Add(swaptionVolatilityCubeBySabr);
         }

         public void createSmileSections()
         {
            List<double> strikes = new List<double>(), stdDevsOnExpiry = new List<double>(), stdDevsOnPayment = new List<double>();
            strikes.Add(0.003); stdDevsOnExpiry.Add(2.45489828353233); stdDevsOnPayment.Add(1.66175264544155);
            strikes.Add(0.004); stdDevsOnExpiry.Add(2.10748097295326); stdDevsOnPayment.Add(1.46691241671427);
            strikes.Add(0.005); stdDevsOnExpiry.Add(1.87317517200074); stdDevsOnPayment.Add(1.32415790098009);
            strikes.Add(0.006); stdDevsOnExpiry.Add(1.69808302023488); stdDevsOnPayment.Add(1.21209617319357);
            strikes.Add(0.007); stdDevsOnExpiry.Add(1.55911989073644); stdDevsOnPayment.Add(1.12016686638666);
            strikes.Add(0.008); stdDevsOnExpiry.Add(1.44436083444893); stdDevsOnPayment.Add(1.04242066059821);
            strikes.Add(0.009); stdDevsOnExpiry.Add(1.34687413874126); stdDevsOnPayment.Add(0.975173254741177);
            strikes.Add(0.01); stdDevsOnExpiry.Add(1.26228953588707); stdDevsOnPayment.Add(0.916013813275761);
            strikes.Add(0.011); stdDevsOnExpiry.Add(1.18769456816136); stdDevsOnPayment.Add(0.863267064731419);
            strikes.Add(0.012); stdDevsOnExpiry.Add(1.12104324191799); stdDevsOnPayment.Add(0.815743793189994);
            strikes.Add(0.013); stdDevsOnExpiry.Add(1.06085561121201); stdDevsOnPayment.Add(0.772552896805455);
            strikes.Add(0.014); stdDevsOnExpiry.Add(1.00603120341767); stdDevsOnPayment.Add(0.733033340026564);
            strikes.Add(0.015); stdDevsOnExpiry.Add(0.955725690399709); stdDevsOnPayment.Add(0.696673144338147);
            strikes.Add(0.016); stdDevsOnExpiry.Add(0.909281318404816); stdDevsOnPayment.Add(0.663070503816902);
            strikes.Add(0.017); stdDevsOnExpiry.Add(0.866185798452041); stdDevsOnPayment.Add(0.631911102538957);
            strikes.Add(0.018); stdDevsOnExpiry.Add(0.826018547612582); stdDevsOnPayment.Add(0.602948672357772);
            strikes.Add(0.019); stdDevsOnExpiry.Add(0.788447526732122); stdDevsOnPayment.Add(0.575982310311697);
            strikes.Add(0.02); stdDevsOnExpiry.Add(0.753200779931885); stdDevsOnPayment.Add(0.550849997883271);
            strikes.Add(0.021); stdDevsOnExpiry.Add(0.720053785498); stdDevsOnPayment.Add(0.527428600999225);
            strikes.Add(0.022); stdDevsOnExpiry.Add(0.688823131326177); stdDevsOnPayment.Add(0.505604706697337);
            strikes.Add(0.023); stdDevsOnExpiry.Add(0.659357028088728); stdDevsOnPayment.Add(0.485294065348527);
            strikes.Add(0.024); stdDevsOnExpiry.Add(0.631532146956907); stdDevsOnPayment.Add(0.466418908064414);
            strikes.Add(0.025); stdDevsOnExpiry.Add(0.605247295045587); stdDevsOnPayment.Add(0.448904706326966);
            strikes.Add(0.026); stdDevsOnExpiry.Add(0.580413928580285); stdDevsOnPayment.Add(0.432686652729201);
            strikes.Add(0.027); stdDevsOnExpiry.Add(0.556962477452476); stdDevsOnPayment.Add(0.417699939864133);
            strikes.Add(0.028); stdDevsOnExpiry.Add(0.534829696108958); stdDevsOnPayment.Add(0.403876519954429);
            strikes.Add(0.029); stdDevsOnExpiry.Add(0.513968150384827); stdDevsOnPayment.Add(0.391145104852406);
            strikes.Add(0.03); stdDevsOnExpiry.Add(0.494330406115181); stdDevsOnPayment.Add(0.379434406410383);
            strikes.Add(0.031); stdDevsOnExpiry.Add(0.475869029135118); stdDevsOnPayment.Add(0.368669896110328);
            strikes.Add(0.032); stdDevsOnExpiry.Add(0.458549234390376); stdDevsOnPayment.Add(0.358777045434208);
            strikes.Add(0.033); stdDevsOnExpiry.Add(0.442329912271372); stdDevsOnPayment.Add(0.349678085493644);
            strikes.Add(0.034); stdDevsOnExpiry.Add(0.427163628613205); stdDevsOnPayment.Add(0.341304968511301);
            strikes.Add(0.035); stdDevsOnExpiry.Add(0.413009273806291); stdDevsOnPayment.Add(0.333586406339497);
            strikes.Add(0.036); stdDevsOnExpiry.Add(0.399819413685729); stdDevsOnPayment.Add(0.326457591571248);
            strikes.Add(0.037); stdDevsOnExpiry.Add(0.387546614086615); stdDevsOnPayment.Add(0.31985630909585);
            strikes.Add(0.038); stdDevsOnExpiry.Add(0.376137116288728); stdDevsOnPayment.Add(0.313728768765505);
            strikes.Add(0.039); stdDevsOnExpiry.Add(0.365540323849504); stdDevsOnPayment.Add(0.308024420802767);
            strikes.Add(0.04); stdDevsOnExpiry.Add(0.35570564032638); stdDevsOnPayment.Add(0.30269822405978);
            strikes.Add(0.041); stdDevsOnExpiry.Add(0.346572982443814); stdDevsOnPayment.Add(0.297710321981251);
            strikes.Add(0.042); stdDevsOnExpiry.Add(0.338091753759242); stdDevsOnPayment.Add(0.293025394530372);
            strikes.Add(0.043); stdDevsOnExpiry.Add(0.330211357830103); stdDevsOnPayment.Add(0.288612334151791);
            strikes.Add(0.044); stdDevsOnExpiry.Add(0.322881198213832); stdDevsOnPayment.Add(0.284443273660505);
            strikes.Add(0.045); stdDevsOnExpiry.Add(0.316056686795423); stdDevsOnPayment.Add(0.280494558352965);
            strikes.Add(0.046); stdDevsOnExpiry.Add(0.309691654321036); stdDevsOnPayment.Add(0.276744153710797);
            strikes.Add(0.047); stdDevsOnExpiry.Add(0.303745307408855); stdDevsOnPayment.Add(0.273174237697079);
            strikes.Add(0.048); stdDevsOnExpiry.Add(0.298180014954725); stdDevsOnPayment.Add(0.269767960385995);
            strikes.Add(0.049); stdDevsOnExpiry.Add(0.292961308132149); stdDevsOnPayment.Add(0.266511064148011);
            strikes.Add(0.05); stdDevsOnExpiry.Add(0.288057880392292); stdDevsOnPayment.Add(0.263391235575797);
            strikes.Add(0.051); stdDevsOnExpiry.Add(0.283441587463978); stdDevsOnPayment.Add(0.260399077595342);
            strikes.Add(0.052); stdDevsOnExpiry.Add(0.279088079809224); stdDevsOnPayment.Add(0.257518712391935);
            strikes.Add(0.053); stdDevsOnExpiry.Add(0.274968896929089); stdDevsOnPayment.Add(0.254747223632261);
            strikes.Add(0.054); stdDevsOnExpiry.Add(0.271067594979739); stdDevsOnPayment.Add(0.252074566168237);
            strikes.Add(0.055); stdDevsOnExpiry.Add(0.267364567839682); stdDevsOnPayment.Add(0.249494259259166);
            strikes.Add(0.056); stdDevsOnExpiry.Add(0.263842422981787); stdDevsOnPayment.Add(0.246999498127314);
            strikes.Add(0.057); stdDevsOnExpiry.Add(0.26048629770105); stdDevsOnPayment.Add(0.244584774143087);
            strikes.Add(0.058); stdDevsOnExpiry.Add(0.257282594203533); stdDevsOnPayment.Add(0.242244902713927);
            strikes.Add(0.059); stdDevsOnExpiry.Add(0.254218979606362); stdDevsOnPayment.Add(0.23997567135838);
            strikes.Add(0.06); stdDevsOnExpiry.Add(0.251284385937726); stdDevsOnPayment.Add(0.237772543557956);
            strikes.Add(0.061); stdDevsOnExpiry.Add(0.248469326364644); stdDevsOnPayment.Add(0.235632278942307);
            strikes.Add(0.062); stdDevsOnExpiry.Add(0.245764630281902); stdDevsOnPayment.Add(0.233550665029978);
            strikes.Add(0.063); stdDevsOnExpiry.Add(0.243162391995349); stdDevsOnPayment.Add(0.231525109524691);
            strikes.Add(0.064); stdDevsOnExpiry.Add(0.240655338266368); stdDevsOnPayment.Add(0.22955269609313);
            strikes.Add(0.065); stdDevsOnExpiry.Add(0.238237144539637); stdDevsOnPayment.Add(0.227630508401982);
            strikes.Add(0.066); stdDevsOnExpiry.Add(0.235901802487603); stdDevsOnPayment.Add(0.225756278192003);
            strikes.Add(0.067); stdDevsOnExpiry.Add(0.233643936238243); stdDevsOnPayment.Add(0.223927413166912);
            strikes.Add(0.068); stdDevsOnExpiry.Add(0.2314584861473); stdDevsOnPayment.Add(0.222142617178571);
            strikes.Add(0.069); stdDevsOnExpiry.Add(0.229341341253818); stdDevsOnPayment.Add(0.220398973893664);
            strikes.Add(0.07); stdDevsOnExpiry.Add(0.22728807436907); stdDevsOnPayment.Add(0.218695187164053);
            strikes.Add(0.071); stdDevsOnExpiry.Add(0.225295206987632); stdDevsOnPayment.Add(0.217029636804562);
            strikes.Add(0.072); stdDevsOnExpiry.Add(0.223359576831843); stdDevsOnPayment.Add(0.215400702630017);
            strikes.Add(0.073); stdDevsOnExpiry.Add(0.221477389168511); stdDevsOnPayment.Add(0.213806764455244);
            strikes.Add(0.074); stdDevsOnExpiry.Add(0.219646430403273); stdDevsOnPayment.Add(0.212246202095067);
            strikes.Add(0.075); stdDevsOnExpiry.Add(0.21786353825847); stdDevsOnPayment.Add(0.210718367475417);
            strikes.Add(0.076); stdDevsOnExpiry.Add(0.21612649913974); stdDevsOnPayment.Add(0.20922164041112);
            strikes.Add(0.077); stdDevsOnExpiry.Add(0.214433415680486); stdDevsOnPayment.Add(0.20775504879107);
            strikes.Add(0.078); stdDevsOnExpiry.Add(0.212781441830814); stdDevsOnPayment.Add(0.206317296467129);
            strikes.Add(0.079); stdDevsOnExpiry.Add(0.21116931267966); stdDevsOnPayment.Add(0.20490741132819);
            strikes.Add(0.08); stdDevsOnExpiry.Add(0.209594814632662); stdDevsOnPayment.Add(0.203524745300185);
            strikes.Add(0.081); stdDevsOnExpiry.Add(0.20805636655099); stdDevsOnPayment.Add(0.202168002234973);
            strikes.Add(0.082); stdDevsOnExpiry.Add(0.20655270352358); stdDevsOnPayment.Add(0.20083621002145);
            strikes.Add(0.083); stdDevsOnExpiry.Add(0.20508161195607); stdDevsOnPayment.Add(0.199529044622581);
            strikes.Add(0.084); stdDevsOnExpiry.Add(0.203642775620693); stdDevsOnPayment.Add(0.198245209890227);
            strikes.Add(0.085); stdDevsOnExpiry.Add(0.202233980923088); stdDevsOnPayment.Add(0.196984381787351);
            strikes.Add(0.086); stdDevsOnExpiry.Add(0.200854279179957); stdDevsOnPayment.Add(0.195745912239886);
            strikes.Add(0.087); stdDevsOnExpiry.Add(0.199503037935767); stdDevsOnPayment.Add(0.19452850509969);
            strikes.Add(0.088); stdDevsOnExpiry.Add(0.198178676051688); stdDevsOnPayment.Add(0.193332160366764);
            strikes.Add(0.089); stdDevsOnExpiry.Add(0.196880244844423); stdDevsOnPayment.Add(0.192155905930003);
            strikes.Add(0.09); stdDevsOnExpiry.Add(0.195606795630673); stdDevsOnPayment.Add(0.190999417752372);
            strikes.Add(0.091); stdDevsOnExpiry.Add(0.194357695954907); stdDevsOnPayment.Add(0.189861723722766);
            strikes.Add(0.092); stdDevsOnExpiry.Add(0.19313168090606); stdDevsOnPayment.Add(0.188742823841186);
            strikes.Add(0.093); stdDevsOnExpiry.Add(0.191928434256365); stdDevsOnPayment.Add(0.187641745996527);
            strikes.Add(0.094); stdDevsOnExpiry.Add(0.190746691094761); stdDevsOnPayment.Add(0.186558166151753);
            strikes.Add(0.095); stdDevsOnExpiry.Add(0.189586451421245); stdDevsOnPayment.Add(0.185491436232795);
            strikes.Add(0.096); stdDevsOnExpiry.Add(0.188446134096988); stdDevsOnPayment.Add(0.184441556239653);
            strikes.Add(0.097); stdDevsOnExpiry.Add(0.18732573912199); stdDevsOnPayment.Add(0.183407878098257);
            strikes.Add(0.098); stdDevsOnExpiry.Add(0.186224317812954); stdDevsOnPayment.Add(0.182390725845642);
            strikes.Add(0.099); stdDevsOnExpiry.Add(0.185141553942112); stdDevsOnPayment.Add(0.181386859111458);
            strikes.Add(0.1); stdDevsOnExpiry.Add(0.184076498826167); stdDevsOnPayment.Add(0.180399194229021);
            strikes.Add(0.101); stdDevsOnExpiry.Add(0.18302915246512); stdDevsOnPayment.Add(0.17942643505019);
            strikes.Add(0.102); stdDevsOnExpiry.Add(0.181999514858969); stdDevsOnPayment.Add(0.178466637352756);
            strikes.Add(0.103); stdDevsOnExpiry.Add(0.180984739957821); stdDevsOnPayment.Add(0.177521421321893);
            strikes.Add(0.104); stdDevsOnExpiry.Add(0.179986725128272); stdDevsOnPayment.Add(0.176590462920567);
            strikes.Add(0.105); stdDevsOnExpiry.Add(0.179004521687023); stdDevsOnPayment.Add(0.175677650593196);
            strikes.Add(0.106); stdDevsOnExpiry.Add(0.178041924367268); stdDevsOnPayment.Add(0.17476516230286);
            strikes.Add(0.107); stdDevsOnExpiry.Add(0.177083754236237); stdDevsOnPayment.Add(0.173873088345724);
            strikes.Add(0.108); stdDevsOnExpiry.Add(0.176145822682231); stdDevsOnPayment.Add(0.173000456610684);
            strikes.Add(0.109); stdDevsOnExpiry.Add(0.175227181021952); stdDevsOnPayment.Add(0.172122316246049);
            strikes.Add(0.11); stdDevsOnExpiry.Add(0.174309488044971); stdDevsOnPayment.Add(0.171266858473859);
            strikes.Add(0.111); stdDevsOnExpiry.Add(0.173412982328314); stdDevsOnPayment.Add(0.170434407331149);
            strikes.Add(0.112); stdDevsOnExpiry.Add(0.172536715188681); stdDevsOnPayment.Add(0.169585106262623);
            strikes.Add(0.113); stdDevsOnExpiry.Add(0.171706301075121); stdDevsOnPayment.Add(0.168765292564274);
            strikes.Add(0.114); stdDevsOnExpiry.Add(0.17079651379229); stdDevsOnPayment.Add(0.167976586421278);
            strikes.Add(0.115); stdDevsOnExpiry.Add(0.169963569856602); stdDevsOnPayment.Add(0.167267917425907);
            strikes.Add(0.116); stdDevsOnExpiry.Add(0.169192922790819); stdDevsOnPayment.Add(0.166364178135514);
            strikes.Add(0.117); stdDevsOnExpiry.Add(0.168289776291075); stdDevsOnPayment.Add(0.165629586177349);
            strikes.Add(0.118); stdDevsOnExpiry.Add(0.167505847659119); stdDevsOnPayment.Add(0.165014239848036);
            strikes.Add(0.119); stdDevsOnExpiry.Add(0.166813308851542); stdDevsOnPayment.Add(0.164618590628398);
            strikes.Add(0.12); stdDevsOnExpiry.Add(0.166305130831553); stdDevsOnPayment.Add(0.164530452554899);
            strikes.Add(0.121); stdDevsOnExpiry.Add(0.166077130612255); stdDevsOnPayment.Add(0.162925173083904);
            strikes.Add(0.122); stdDevsOnExpiry.Add(0.164586116695486); stdDevsOnPayment.Add(0.162717141307485);
            strikes.Add(0.123); stdDevsOnExpiry.Add(0.164242693341591); stdDevsOnPayment.Add(0.162840275380755);
            strikes.Add(0.124); stdDevsOnExpiry.Add(0.164213284159352); stdDevsOnPayment.Add(0.163289714748189);
            strikes.Add(0.125); stdDevsOnExpiry.Add(0.164516546586962); stdDevsOnPayment.Add(0.16401944615083);
            strikes.Add(0.126); stdDevsOnExpiry.Add(0.165118644253458); stdDevsOnPayment.Add(0.164961421811344);
            strikes.Add(0.127); stdDevsOnExpiry.Add(0.165959810111063); stdDevsOnPayment.Add(0.166058935248619);
            strikes.Add(0.128); stdDevsOnExpiry.Add(0.166976798606573); stdDevsOnPayment.Add(0.16725625209265);
            strikes.Add(0.129); stdDevsOnExpiry.Add(0.168115851019766); stdDevsOnPayment.Add(0.16851675615849);
            strikes.Add(0.13); stdDevsOnExpiry.Add(0.169332063007866); stdDevsOnPayment.Add(0.16981808889073);
            strikes.Add(0.131); stdDevsOnExpiry.Add(0.170600136349594); stdDevsOnPayment.Add(0.171139511919136);
            strikes.Add(0.132); stdDevsOnExpiry.Add(0.171891926773773); stdDevsOnPayment.Add(0.172468711836379);
            strikes.Add(0.133); stdDevsOnExpiry.Add(0.173201742180614); stdDevsOnPayment.Add(0.173801476161007);
            strikes.Add(0.134); stdDevsOnExpiry.Add(0.17451282249852); stdDevsOnPayment.Add(0.175129703967145);
            strikes.Add(0.135); stdDevsOnExpiry.Add(0.175823902816426); stdDevsOnPayment.Add(0.17645371929183);
            strikes.Add(0.136); stdDevsOnExpiry.Add(0.177132453312204); stdDevsOnPayment.Add(0.177767365431397);
            strikes.Add(0.137); stdDevsOnExpiry.Add(0.178433098113831); stdDevsOnPayment.Add(0.179076475052476);
            strikes.Add(0.138); stdDevsOnExpiry.Add(0.17972646967684); stdDevsOnPayment.Add(0.180372947229192);
            strikes.Add(0.139); stdDevsOnExpiry.Add(0.181011935545698); stdDevsOnPayment.Add(0.181660994443001);
            strikes.Add(0.14); stdDevsOnExpiry.Add(0.182286965898278); stdDevsOnPayment.Add(0.182938996508727);
            strikes.Add(0.141); stdDevsOnExpiry.Add(0.18355314187341); stdDevsOnPayment.Add(0.18420889764858);
            strikes.Add(0.142); stdDevsOnExpiry.Add(0.184810147243326); stdDevsOnPayment.Add(0.185468105566281);
            strikes.Add(0.143); stdDevsOnExpiry.Add(0.186056717096965); stdDevsOnPayment.Add(0.186718888521073);
            strikes.Add(0.144); stdDevsOnExpiry.Add(0.187295381256453); stdDevsOnPayment.Add(0.187958006142609);
            strikes.Add(0.145); stdDevsOnExpiry.Add(0.188523609899662); stdDevsOnPayment.Add(0.189190318986411);
            strikes.Add(0.146); stdDevsOnExpiry.Add(0.189745197759785); stdDevsOnPayment.Add(0.190412586682131);
            strikes.Add(0.147); stdDevsOnExpiry.Add(0.190955085192566); stdDevsOnPayment.Add(0.191624809229768);
            strikes.Add(0.148); stdDevsOnExpiry.Add(0.186502914474815); stdDevsOnPayment.Add(0.192830226999672);
            strikes.Add(0.149); stdDevsOnExpiry.Add(0.187658094504074); stdDevsOnPayment.Add(0.194024951547423);
            strikes.Add(0.15); stdDevsOnExpiry.Add(0.188817069266526); stdDevsOnPayment.Add(0.195212547280407);
            strikes.Add(0.151); stdDevsOnExpiry.Add(0.189958019046315); stdDevsOnPayment.Add(0.196391394013447);
            strikes.Add(0.152); stdDevsOnExpiry.Add(0.191090746904187); stdDevsOnPayment.Add(0.197560195598405);
            strikes.Add(0.153); stdDevsOnExpiry.Add(0.192215885295675); stdDevsOnPayment.Add(0.19871895203528);
            strikes.Add(0.154); stdDevsOnExpiry.Add(0.193335331587374); stdDevsOnPayment.Add(0.199872523879597);
            strikes.Add(0.155); stdDevsOnExpiry.Add(0.194446555957158); stdDevsOnPayment.Add(0.195112095799581);
            strikes.Add(0.156); stdDevsOnExpiry.Add(0.195547028582896); stdDevsOnPayment.Add(0.196220302459009);
            strikes.Add(0.157); stdDevsOnExpiry.Add(0.196646236297571); stdDevsOnPayment.Add(0.197317167822215);
            strikes.Add(0.158); stdDevsOnExpiry.Add(0.197736589634797); stdDevsOnPayment.Add(0.198405608222512);
            strikes.Add(0.159); stdDevsOnExpiry.Add(0.198811131583722); stdDevsOnPayment.Add(0.19949340054874);
            strikes.Add(0.16); stdDevsOnExpiry.Add(0.199887570899243); stdDevsOnPayment.Add(0.200565963134326);
            strikes.Add(0.161); stdDevsOnExpiry.Add(0.20095167733189); stdDevsOnPayment.Add(0.201636905534738);
            strikes.Add(0.162); stdDevsOnExpiry.Add(0.20200756184262); stdDevsOnPayment.Add(0.202695534527823);
            strikes.Add(0.163); stdDevsOnExpiry.Add(0.203061232758988); stdDevsOnPayment.Add(0.203753839483873);
            strikes.Add(0.164); stdDevsOnExpiry.Add(0.204112690080994); stdDevsOnPayment.Add(0.204791730106723);
            strikes.Add(0.165); stdDevsOnExpiry.Add(0.205146754875869); stdDevsOnPayment.Add(0.205839341840621);
            strikes.Add(0.166); stdDevsOnExpiry.Add(0.206178289848616); stdDevsOnPayment.Add(0.206869779611668);
            strikes.Add(0.167); stdDevsOnExpiry.Add(0.207207294999235); stdDevsOnPayment.Add(0.207893412604981);
            strikes.Add(0.168); stdDevsOnExpiry.Add(0.208224599722511); stdDevsOnPayment.Add(0.208916397524225);
            strikes.Add(0.169); stdDevsOnExpiry.Add(0.209234947434935); stdDevsOnPayment.Add(0.209924476739862);
            strikes.Add(0.17); stdDevsOnExpiry.Add(0.210235175858846); stdDevsOnPayment.Add(0.210934824214744);
            strikes.Add(0.171); stdDevsOnExpiry.Add(0.211231609549565); stdDevsOnPayment.Add(0.211933506356369);
            strikes.Add(0.172); stdDevsOnExpiry.Add(0.212231205517945); stdDevsOnPayment.Add(0.212931216386889);
            strikes.Add(0.173); stdDevsOnExpiry.Add(0.213219101058981); stdDevsOnPayment.Add(0.213916613010082);
            strikes.Add(0.174); stdDevsOnExpiry.Add(0.214192133895015); stdDevsOnPayment.Add(0.2149003894481);
            strikes.Add(0.175); stdDevsOnExpiry.Add(0.215167064097645); stdDevsOnPayment.Add(0.215876064960245);
            strikes.Add(0.176); stdDevsOnExpiry.Add(0.216146105261233); stdDevsOnPayment.Add(0.216845259731692);
            strikes.Add(0.177); stdDevsOnExpiry.Add(0.217099215748008); stdDevsOnPayment.Add(0.217806029540231);
            strikes.Add(0.178); stdDevsOnExpiry.Add(0.218056437195741); stdDevsOnPayment.Add(0.218763558978421);
            strikes.Add(0.179); stdDevsOnExpiry.Add(0.219005120493791); stdDevsOnPayment.Add(0.219722060527715);
            strikes.Add(0.18); stdDevsOnExpiry.Add(0.219951273969714); stdDevsOnPayment.Add(0.220671489040032);
            strikes.Add(0.181); stdDevsOnExpiry.Add(0.220885410790527); stdDevsOnPayment.Add(0.221608280107987);
            strikes.Add(0.182); stdDevsOnExpiry.Add(0.221831248038684); stdDevsOnPayment.Add(0.222542154842628);
            strikes.Add(0.183); stdDevsOnExpiry.Add(0.222757162937581); stdDevsOnPayment.Add(0.223469224799535);
            strikes.Add(0.184); stdDevsOnExpiry.Add(0.223673907231264); stdDevsOnPayment.Add(0.224396942830512);
            strikes.Add(0.185); stdDevsOnExpiry.Add(0.224599189674629); stdDevsOnPayment.Add(0.225315911861546);
            strikes.Add(0.186); stdDevsOnExpiry.Add(0.225503601085437); stdDevsOnPayment.Add(0.226230992448161);
            strikes.Add(0.187); stdDevsOnExpiry.Add(0.226412755912736); stdDevsOnPayment.Add(0.227133759627449);
            strikes.Add(0.188); stdDevsOnExpiry.Add(0.227313372590352); stdDevsOnPayment.Add(0.228049488288135);
            strikes.Add(0.189); stdDevsOnExpiry.Add(0.228216519090096); stdDevsOnPayment.Add(0.228941886282305);
            strikes.Add(0.19); stdDevsOnExpiry.Add(0.229108597618029); stdDevsOnPayment.Add(0.229833312165371);
            strikes.Add(0.191); stdDevsOnExpiry.Add(0.229988343263088); stdDevsOnPayment.Add(0.230724738048437);
            strikes.Add(0.192); stdDevsOnExpiry.Add(0.230883267840916); stdDevsOnPayment.Add(0.231599638042722);
            strikes.Add(0.193); stdDevsOnExpiry.Add(0.231748467008738); stdDevsOnPayment.Add(0.232481018777706);
            strikes.Add(0.194); stdDevsOnExpiry.Add(0.232617460909752); stdDevsOnPayment.Add(0.233360131253445);
            strikes.Add(0.195); stdDevsOnExpiry.Add(0.233487087266298); stdDevsOnPayment.Add(0.234224662062612);
            strikes.Add(0.196); stdDevsOnExpiry.Add(0.234348491700928); stdDevsOnPayment.Add(0.235093405353234);
            strikes.Add(0.197); stdDevsOnExpiry.Add(0.2352057851746); stdDevsOnPayment.Add(0.235946918903214);
            strikes.Add(0.198); stdDevsOnExpiry.Add(0.236064976014868); stdDevsOnPayment.Add(0.236808209342033);
            strikes.Add(0.199); stdDevsOnExpiry.Add(0.236907723011302); stdDevsOnPayment.Add(0.237655242151315);
            strikes.Add(0.2); stdDevsOnExpiry.Add(0.237747940185609); stdDevsOnPayment.Add(0.238496766331003);
            strikes.Add(0.201); stdDevsOnExpiry.Add(0.238590687182044); stdDevsOnPayment.Add(0.239337318399586);
            strikes.Add(0.202); stdDevsOnExpiry.Add(0.239419203929008); stdDevsOnPayment.Add(0.24017916661631);
            strikes.Add(0.203); stdDevsOnExpiry.Add(0.240250882953632); stdDevsOnPayment.Add(0.241003840870182);
            strikes.Add(0.204); stdDevsOnExpiry.Add(0.241075604967404); stdDevsOnPayment.Add(0.241832727605508);
            strikes.Add(0.205); stdDevsOnExpiry.Add(0.24190317303107); stdDevsOnPayment.Add(0.242654809563101);
            strikes.Add(0.206); stdDevsOnExpiry.Add(0.24272251917282); stdDevsOnPayment.Add(0.243478511705869);
            strikes.Add(0.207); stdDevsOnExpiry.Add(0.243530164887227); stdDevsOnPayment.Add(0.244289576404275);
            strikes.Add(0.208); stdDevsOnExpiry.Add(0.24434287024589); stdDevsOnPayment.Add(0.245096752658261);
            strikes.Add(0.209); stdDevsOnExpiry.Add(0.245145772543807); stdDevsOnPayment.Add(0.245903280838178);
            strikes.Add(0.21); stdDevsOnExpiry.Add(0.245951204663852); stdDevsOnPayment.Add(0.246714021499549);
            strikes.Add(0.211); stdDevsOnExpiry.Add(0.246737030662404); stdDevsOnPayment.Add(0.247507912235104);
            strikes.Add(0.212); stdDevsOnExpiry.Add(0.247526967621914); stdDevsOnPayment.Add(0.248306663526183);
            strikes.Add(0.213); stdDevsOnExpiry.Add(0.24831563967036); stdDevsOnPayment.Add(0.2490927773729);
            strikes.Add(0.214); stdDevsOnExpiry.Add(0.249111584957424); stdDevsOnPayment.Add(0.249878243145547);
            strikes.Add(0.215); stdDevsOnExpiry.Add(0.249880650884377); stdDevsOnPayment.Add(0.250649127251622);
            strikes.Add(0.216); stdDevsOnExpiry.Add(0.250656357594417); stdDevsOnPayment.Add(0.251433620913165);
            strikes.Add(0.217); stdDevsOnExpiry.Add(0.251434594126584); stdDevsOnPayment.Add(0.25220580116738);
            strikes.Add(0.218); stdDevsOnExpiry.Add(0.252199549092579); stdDevsOnPayment.Add(0.252970852606827);
            strikes.Add(0.219); stdDevsOnExpiry.Add(0.252961025553147); stdDevsOnPayment.Add(0.253737200194414);
            strikes.Add(0.22); stdDevsOnExpiry.Add(0.253727877885738); stdDevsOnPayment.Add(0.254501279522756);
            strikes.Add(0.221); stdDevsOnExpiry.Add(0.254480499968858); stdDevsOnPayment.Add(0.255269571332552);
            strikes.Add(0.222); stdDevsOnExpiry.Add(0.25523533564634); stdDevsOnPayment.Add(0.256016476698044);
            strikes.Add(0.223); stdDevsOnExpiry.Add(0.255984162996268); stdDevsOnPayment.Add(0.256770834915338);
            strikes.Add(0.224); stdDevsOnExpiry.Add(0.25673583639609); stdDevsOnPayment.Add(0.257510611466062);
            strikes.Add(0.225); stdDevsOnExpiry.Add(0.257477706735166); stdDevsOnPayment.Add(0.258255572609344);
            strikes.Add(0.226); stdDevsOnExpiry.Add(0.258220525757539); stdDevsOnPayment.Add(0.25900280201187);
            strikes.Add(0.227); stdDevsOnExpiry.Add(0.258953541719166); stdDevsOnPayment.Add(0.259739986266314);
            strikes.Add(0.228); stdDevsOnExpiry.Add(0.259691301097284); stdDevsOnPayment.Add(0.260471337854129);
            strikes.Add(0.229); stdDevsOnExpiry.Add(0.260414197770398); stdDevsOnPayment.Add(0.26120171733084);
            strikes.Add(0.23); stdDevsOnExpiry.Add(0.261138359354577); stdDevsOnPayment.Add(0.26193598525197);
            strikes.Add(0.231); stdDevsOnExpiry.Add(0.261857461294499); stdDevsOnPayment.Add(0.262664096469436);
            strikes.Add(0.232); stdDevsOnExpiry.Add(0.262581939106444); stdDevsOnPayment.Add(0.263373413538876);
            strikes.Add(0.233); stdDevsOnExpiry.Add(0.263292502896683); stdDevsOnPayment.Add(0.264094071904539);
            strikes.Add(0.234); stdDevsOnExpiry.Add(0.264006545192349); stdDevsOnPayment.Add(0.26480533319619);
            strikes.Add(0.235); stdDevsOnExpiry.Add(0.264707305921843); stdDevsOnPayment.Add(0.265520482932259);
            strikes.Add(0.236); stdDevsOnExpiry.Add(0.265417869712082); stdDevsOnPayment.Add(0.266215866409198);
            strikes.Add(0.237); stdDevsOnExpiry.Add(0.266128433502322); stdDevsOnPayment.Add(0.266921619071255);
            strikes.Add(0.238); stdDevsOnExpiry.Add(0.266818442487771); stdDevsOnPayment.Add(0.267621215029648);
            strikes.Add(0.239); stdDevsOnExpiry.Add(0.267506237878858); stdDevsOnPayment.Add(0.268319190802866);
            strikes.Add(0.24); stdDevsOnExpiry.Add(0.268213955619203); stdDevsOnPayment.Add(0.269024295390853);
            strikes.Add(0.241); stdDevsOnExpiry.Add(0.268901118554758); stdDevsOnPayment.Add(0.269714494275234);
            strikes.Add(0.242); stdDevsOnExpiry.Add(0.269581956934992); stdDevsOnPayment.Add(0.270383630752344);
            strikes.Add(0.243); stdDevsOnExpiry.Add(0.270257103215438); stdDevsOnPayment.Add(0.271079662303353);
            strikes.Add(0.244); stdDevsOnExpiry.Add(0.270943317467695); stdDevsOnPayment.Add(0.271764028521105);
            strikes.Add(0.245); stdDevsOnExpiry.Add(0.271623207164631); stdDevsOnPayment.Add(0.272445154368508);
            strikes.Add(0.246); stdDevsOnExpiry.Add(0.272295191167417); stdDevsOnPayment.Add(0.273122067734456);
            strikes.Add(0.247); stdDevsOnExpiry.Add(0.272981721647439); stdDevsOnPayment.Add(0.273801573396684);
            strikes.Add(0.248); stdDevsOnExpiry.Add(0.2736334670732); stdDevsOnPayment.Add(0.274467145466411);
            strikes.Add(0.249); stdDevsOnExpiry.Add(0.274298494065133); stdDevsOnPayment.Add(0.275112951277007);
            strikes.Add(0.25); stdDevsOnExpiry.Add(0.274975221484409); stdDevsOnPayment.Add(0.275792456939235);
            strikes.Add(0.251); stdDevsOnExpiry.Add(0.275627599365702); stdDevsOnPayment.Add(0.276450252120124);
            strikes.Add(0.252); stdDevsOnExpiry.Add(0.276287250485613); stdDevsOnPayment.Add(0.277106427115838);
            strikes.Add(0.253); stdDevsOnExpiry.Add(0.27693614986148); stdDevsOnPayment.Add(0.277760009815272);
            strikes.Add(0.254); stdDevsOnExpiry.Add(0.277595168525859); stdDevsOnPayment.Add(0.278409055996218);
            strikes.Add(0.255); stdDevsOnExpiry.Add(0.278247230179386); stdDevsOnPayment.Add(0.279076248251119);
            strikes.Add(0.256); stdDevsOnExpiry.Add(0.27889233482206); stdDevsOnPayment.Add(0.27972043387654);
            strikes.Add(0.257); stdDevsOnExpiry.Add(0.279533328503776); stdDevsOnPayment.Add(0.280366887761207);
            strikes.Add(0.258); stdDevsOnExpiry.Add(0.280163886669214); stdDevsOnPayment.Add(0.281011073386628);
            strikes.Add(0.259); stdDevsOnExpiry.Add(0.280801401845504); stdDevsOnPayment.Add(0.281636464864025);
            strikes.Add(0.26); stdDevsOnExpiry.Add(0.281444609121582); stdDevsOnPayment.Add(0.282278058193167);
            strikes.Add(0.261); stdDevsOnExpiry.Add(0.282062518176379); stdDevsOnPayment.Add(0.2829177073001);
            strikes.Add(0.262); stdDevsOnExpiry.Add(0.282703195630329); stdDevsOnPayment.Add(0.28354990355523);
            strikes.Add(0.263); stdDevsOnExpiry.Add(0.283321420912892); stdDevsOnPayment.Add(0.284173026773382);
            strikes.Add(0.264); stdDevsOnExpiry.Add(0.283962098366842); stdDevsOnPayment.Add(0.284814296065489);
            strikes.Add(0.265); stdDevsOnExpiry.Add(0.284589810482385); stdDevsOnPayment.Add(0.285413116506022);
            strikes.Add(0.266); stdDevsOnExpiry.Add(0.285200130070798); stdDevsOnPayment.Add(0.286036563761209);
            strikes.Add(0.267); stdDevsOnExpiry.Add(0.285821833858787); stdDevsOnPayment.Add(0.286665843683024);
            strikes.Add(0.268); stdDevsOnExpiry.Add(0.286418239425495); stdDevsOnPayment.Add(0.287277625604954);
            strikes.Add(0.269); stdDevsOnExpiry.Add(0.287055438374019); stdDevsOnPayment.Add(0.28788454697136);
            strikes.Add(0.27); stdDevsOnExpiry.Add(0.287650895257428); stdDevsOnPayment.Add(0.288504105782127);
            strikes.Add(0.271); stdDevsOnExpiry.Add(0.288259001251479); stdDevsOnPayment.Add(0.289124960741035);
            strikes.Add(0.272); stdDevsOnExpiry.Add(0.288866474789997); stdDevsOnPayment.Add(0.289720216774184);
            strikes.Add(0.273); stdDevsOnExpiry.Add(0.289457504484683); stdDevsOnPayment.Add(0.29032389777024);
            strikes.Add(0.274); stdDevsOnExpiry.Add(0.290065294250967); stdDevsOnPayment.Add(0.290925958581123);
            strikes.Add(0.275); stdDevsOnExpiry.Add(0.290661383589909); stdDevsOnPayment.Add(0.291521538651306);
            strikes.Add(0.276); stdDevsOnExpiry.Add(0.291270122039491); stdDevsOnPayment.Add(0.292117442758525);
            strikes.Add(0.277); stdDevsOnExpiry.Add(0.291862732873007); stdDevsOnPayment.Add(0.292714643013883);
            strikes.Add(0.278); stdDevsOnExpiry.Add(0.292448070467904); stdDevsOnPayment.Add(0.293307306750752);
            strikes.Add(0.279); stdDevsOnExpiry.Add(0.293031826923971); stdDevsOnPayment.Add(0.293893489746923);
            strikes.Add(0.28); stdDevsOnExpiry.Add(0.293630129857275); stdDevsOnPayment.Add(0.294490365965247);
            strikes.Add(0.281); stdDevsOnExpiry.Add(0.294205348163659); stdDevsOnPayment.Add(0.295054514443043);
            strikes.Add(0.282); stdDevsOnExpiry.Add(0.294780250242278); stdDevsOnPayment.Add(0.295640697439214);
            strikes.Add(0.283); stdDevsOnExpiry.Add(0.295369066342601); stdDevsOnPayment.Add(0.296229796768699);
            strikes.Add(0.284); stdDevsOnExpiry.Add(0.295941122371326); stdDevsOnPayment.Add(0.296821812431499);
            strikes.Add(0.285); stdDevsOnExpiry.Add(0.296506537616964); stdDevsOnPayment.Add(0.297392117612959);
            strikes.Add(0.286); stdDevsOnExpiry.Add(0.297095986172819); stdDevsOnPayment.Add(0.297958858387035);
            strikes.Add(0.287); stdDevsOnExpiry.Add(0.297663931240585); stdDevsOnPayment.Add(0.298539532753612);
            strikes.Add(0.288); stdDevsOnExpiry.Add(0.298226500436329); stdDevsOnPayment.Add(0.299106597564723);
            strikes.Add(0.289); stdDevsOnExpiry.Add(0.298813735397823); stdDevsOnPayment.Add(0.299686299820195);
            strikes.Add(0.29); stdDevsOnExpiry.Add(0.299351322600051); stdDevsOnPayment.Add(0.3002384589277);
            strikes.Add(0.291); stdDevsOnExpiry.Add(0.299931284322926); stdDevsOnPayment.Add(0.300800663183287);
            strikes.Add(0.292); stdDevsOnExpiry.Add(0.300491639924308); stdDevsOnPayment.Add(0.301358978994454);
            strikes.Add(0.293); stdDevsOnExpiry.Add(0.30104408983154); stdDevsOnPayment.Add(0.30192507169446);
            strikes.Add(0.294); stdDevsOnExpiry.Add(0.30161646208803); stdDevsOnPayment.Add(0.30247496254272);
            strikes.Add(0.295); stdDevsOnExpiry.Add(0.302157527795685); stdDevsOnPayment.Add(0.303038462946447);
            strikes.Add(0.296); stdDevsOnExpiry.Add(0.302717883397067); stdDevsOnPayment.Add(0.303587057646567);
            strikes.Add(0.297); stdDevsOnExpiry.Add(0.303255786827061); stdDevsOnPayment.Add(0.304149261902154);
            strikes.Add(0.298); stdDevsOnExpiry.Add(0.303781989829713); stdDevsOnPayment.Add(0.304700124861519);
            strikes.Add(0.299); stdDevsOnExpiry.Add(0.304330645003752); stdDevsOnPayment.Add(0.305239646524661);
            strikes.Add(0.3); stdDevsOnExpiry.Add(0.304881197544388); stdDevsOnPayment.Add(0.305780140298908);
            strikes.Add(0.301); stdDevsOnExpiry.Add(0.305453569800878); stdDevsOnPayment.Add(0.306327114813854);
            strikes.Add(0.302); stdDevsOnExpiry.Add(0.305970602198316); stdDevsOnPayment.Add(0.306869876847346);
            strikes.Add(0.303); stdDevsOnExpiry.Add(0.306495540289904); stdDevsOnPayment.Add(0.307408102362348);
            strikes.Add(0.304); stdDevsOnExpiry.Add(0.307054947207988); stdDevsOnPayment.Add(0.307955400914329);
            strikes.Add(0.305); stdDevsOnExpiry.Add(0.307578936616277); stdDevsOnPayment.Add(0.308478072651655);
            strikes.Add(0.306); stdDevsOnExpiry.Add(0.308105139618929); stdDevsOnPayment.Add(0.30902828753695);
            strikes.Add(0.307); stdDevsOnExpiry.Add(0.308639564543498); stdDevsOnPayment.Add(0.309550959274277);
            strikes.Add(0.308); stdDevsOnExpiry.Add(0.30916892982381); stdDevsOnPayment.Add(0.310072982937534);
            strikes.Add(0.309); stdDevsOnExpiry.Add(0.309708414392635); stdDevsOnPayment.Add(0.310612504600676);
            strikes.Add(0.31); stdDevsOnExpiry.Add(0.310226711701136); stdDevsOnPayment.Add(0.311149433967539);
            strikes.Add(0.311); stdDevsOnExpiry.Add(0.310757025664747); stdDevsOnPayment.Add(0.311663356704923);
            strikes.Add(0.312); stdDevsOnExpiry.Add(0.311267733506864); stdDevsOnPayment.Add(0.312172742923818);
            strikes.Add(0.313); stdDevsOnExpiry.Add(0.31179520142058); stdDevsOnPayment.Add(0.312693470438935);
            strikes.Add(0.314); stdDevsOnExpiry.Add(0.312293892607588); stdDevsOnPayment.Add(0.313225215213239);
            strikes.Add(0.315); stdDevsOnExpiry.Add(0.312807130271834); stdDevsOnPayment.Add(0.313724880321086);
            strikes.Add(0.316); stdDevsOnExpiry.Add(0.313339025374274); stdDevsOnPayment.Add(0.314244311688064);
            strikes.Add(0.317); stdDevsOnExpiry.Add(0.313838981472347); stdDevsOnPayment.Add(0.31478739775859);
            strikes.Add(0.318); stdDevsOnExpiry.Add(0.314357595008614); stdDevsOnPayment.Add(0.315282202310914);
            strikes.Add(0.319); stdDevsOnExpiry.Add(0.314857234878921); stdDevsOnPayment.Add(0.315793208714983);
            strikes.Add(0.32); stdDevsOnExpiry.Add(0.315366361582208); stdDevsOnPayment.Add(0.316289633452481);
            strikes.Add(0.321); stdDevsOnExpiry.Add(0.315885291346242); stdDevsOnPayment.Add(0.31680258407876);
            strikes.Add(0.322); stdDevsOnExpiry.Add(0.316385879899846); stdDevsOnPayment.Add(0.317304193408817);
            strikes.Add(0.323); stdDevsOnExpiry.Add(0.316888682047813); stdDevsOnPayment.Add(0.317796729701896);
            strikes.Add(0.324); stdDevsOnExpiry.Add(0.317366185974499); stdDevsOnPayment.Add(0.318296394809743);
            strikes.Add(0.325); stdDevsOnExpiry.Add(0.317897448621407); stdDevsOnPayment.Add(0.318796059917591);
            strikes.Add(0.326); stdDevsOnExpiry.Add(0.318374952548092); stdDevsOnPayment.Add(0.319321972025266);
            strikes.Add(0.327); stdDevsOnExpiry.Add(0.318880916973719); stdDevsOnPayment.Add(0.319809971799856);
            strikes.Add(0.328); stdDevsOnExpiry.Add(0.319367907733385); stdDevsOnPayment.Add(0.320308988833634);
            strikes.Add(0.329); stdDevsOnExpiry.Add(0.319854898493051); stdDevsOnPayment.Add(0.320818051015494);
            strikes.Add(0.33); stdDevsOnExpiry.Add(0.320354538363358); stdDevsOnPayment.Add(0.321299570049386);
            strikes.Add(0.331); stdDevsOnExpiry.Add(0.320847853678344); stdDevsOnPayment.Add(0.321782061194382);
            strikes.Add(0.332); stdDevsOnExpiry.Add(0.321319033049709); stdDevsOnPayment.Add(0.322270709043042);
            strikes.Add(0.333); stdDevsOnExpiry.Add(0.321799699254055); stdDevsOnPayment.Add(0.322769726076819);
            strikes.Add(0.334); stdDevsOnExpiry.Add(0.322302501402021); stdDevsOnPayment.Add(0.323246384555187);
            strikes.Add(0.335); stdDevsOnExpiry.Add(0.322783167606367); stdDevsOnPayment.Add(0.323742809292685);
            strikes.Add(0.336); stdDevsOnExpiry.Add(0.323279645199013); stdDevsOnPayment.Add(0.324182851586107);
            strikes.Add(0.337); stdDevsOnExpiry.Add(0.323735013182078); stdDevsOnPayment.Add(0.324681868619885);
            strikes.Add(0.338); stdDevsOnExpiry.Add(0.324212517108763); stdDevsOnPayment.Add(0.325174404912964);
            strikes.Add(0.339); stdDevsOnExpiry.Add(0.324693183313109); stdDevsOnPayment.Add(0.325647498983947);
            strikes.Add(0.34); stdDevsOnExpiry.Add(0.325170687239794); stdDevsOnPayment.Add(0.326114112314233);
            strikes.Add(0.341); stdDevsOnExpiry.Add(0.325638704333499); stdDevsOnPayment.Add(0.326590446755566);
            strikes.Add(0.342); stdDevsOnExpiry.Add(0.326132019648485); stdDevsOnPayment.Add(0.327066781196899);
            strikes.Add(0.343); stdDevsOnExpiry.Add(0.326571576243249); stdDevsOnPayment.Add(0.327536634897533);
            strikes.Add(0.344); stdDevsOnExpiry.Add(0.327049080169934); stdDevsOnPayment.Add(0.328019450079565);
            strikes.Add(0.345); stdDevsOnExpiry.Add(0.327507610430659); stdDevsOnPayment.Add(0.32848606340985);
            strikes.Add(0.346); stdDevsOnExpiry.Add(0.327997763467985); stdDevsOnPayment.Add(0.328949436369786);
            strikes.Add(0.347); stdDevsOnExpiry.Add(0.328443644618068); stdDevsOnPayment.Add(0.329409568959373);
            strikes.Add(0.348); stdDevsOnExpiry.Add(0.328933797655394); stdDevsOnPayment.Add(0.329876182289659);
            strikes.Add(0.349); stdDevsOnExpiry.Add(0.329363867417177); stdDevsOnPayment.Add(0.330346035990293);
            strikes.Add(0.35); stdDevsOnExpiry.Add(0.329841371343863); stdDevsOnPayment.Add(0.330799687839182);
            strikes.Add(0.351); stdDevsOnExpiry.Add(0.330284090216286); stdDevsOnPayment.Add(0.331279262650864);
            strikes.Add(0.352); stdDevsOnExpiry.Add(0.330755269587652); stdDevsOnPayment.Add(0.331723193388705);
            strikes.Add(0.353); stdDevsOnExpiry.Add(0.331204313015395); stdDevsOnPayment.Add(0.332163883756196);
            strikes.Add(0.354); stdDevsOnExpiry.Add(0.331647031887819); stdDevsOnPayment.Add(0.332624016345783);
            strikes.Add(0.355); stdDevsOnExpiry.Add(0.332140347202805); stdDevsOnPayment.Add(0.333093870046418);
            strikes.Add(0.356); stdDevsOnExpiry.Add(0.332579903797569); stdDevsOnPayment.Add(0.333560483376703);
            strikes.Add(0.357); stdDevsOnExpiry.Add(0.333022622669992); stdDevsOnPayment.Add(0.334001173744195);
            strikes.Add(0.358); stdDevsOnExpiry.Add(0.333474828375396); stdDevsOnPayment.Add(0.334464546704131);
            strikes.Add(0.359); stdDevsOnExpiry.Add(0.33392387180314); stdDevsOnPayment.Add(0.334889035219877);
            strikes.Add(0.36); stdDevsOnExpiry.Add(0.334341292454282); stdDevsOnPayment.Add(0.335342687068766);
            strikes.Add(0.361); stdDevsOnExpiry.Add(0.334799822715007); stdDevsOnPayment.Add(0.335793098547305);
            strikes.Add(0.362); stdDevsOnExpiry.Add(0.335271002086372); stdDevsOnPayment.Add(0.336246750396193);
            strikes.Add(0.363); stdDevsOnExpiry.Add(0.335701071848155); stdDevsOnPayment.Add(0.33666799854159);
            strikes.Add(0.364); stdDevsOnExpiry.Add(0.336159602108879); stdDevsOnPayment.Add(0.337144332982923);
            strikes.Add(0.365); stdDevsOnExpiry.Add(0.336583347315342); stdDevsOnPayment.Add(0.337555860017272);
            strikes.Add(0.366); stdDevsOnExpiry.Add(0.336994443411164); stdDevsOnPayment.Add(0.337993310014414);
            strikes.Add(0.367); stdDevsOnExpiry.Add(0.337459298227208); stdDevsOnPayment.Add(0.338440481122604);
            strikes.Add(0.368); stdDevsOnExpiry.Add(0.337892530266652); stdDevsOnPayment.Add(0.33886496963835);
            strikes.Add(0.369); stdDevsOnExpiry.Add(0.338335249139075); stdDevsOnPayment.Add(0.339331582968636);
            strikes.Add(0.37); stdDevsOnExpiry.Add(0.338768481178518); stdDevsOnPayment.Add(0.339749590743683);
            strikes.Add(0.371); stdDevsOnExpiry.Add(0.339163765886039); stdDevsOnPayment.Add(0.340180560000127);
            strikes.Add(0.372); stdDevsOnExpiry.Add(0.339634945257404); stdDevsOnPayment.Add(0.340601808145523);
            strikes.Add(0.373); stdDevsOnExpiry.Add(0.340039716797906); stdDevsOnPayment.Add(0.341058700364761);
            strikes.Add(0.374); stdDevsOnExpiry.Add(0.34048559794799); stdDevsOnPayment.Add(0.341473467769459);
            strikes.Add(0.375); stdDevsOnExpiry.Add(0.340896694043811); stdDevsOnPayment.Add(0.341897956285205);
            strikes.Add(0.376); stdDevsOnExpiry.Add(0.341336250638575); stdDevsOnPayment.Add(0.342348367763744);
            strikes.Add(0.377); stdDevsOnExpiry.Add(0.341753671289717); stdDevsOnPayment.Add(0.34277285627949);
            strikes.Add(0.378); stdDevsOnExpiry.Add(0.34217741649618); stdDevsOnPayment.Add(0.343174662202791);
            strikes.Add(0.379); stdDevsOnExpiry.Add(0.342604323980302); stdDevsOnPayment.Add(0.343608871829585);
            strikes.Add(0.38); stdDevsOnExpiry.Add(0.343021744631445); stdDevsOnPayment.Add(0.344000956641838);
            strikes.Add(0.381); stdDevsOnExpiry.Add(0.343404380228325); stdDevsOnPayment.Add(0.344428685527933);
            strikes.Add(0.382); stdDevsOnExpiry.Add(0.343843936823088); stdDevsOnPayment.Add(0.344875856636123);
            strikes.Add(0.383); stdDevsOnExpiry.Add(0.344277168862531); stdDevsOnPayment.Add(0.34525822033733);
            strikes.Add(0.384); stdDevsOnExpiry.Add(0.344700914068994); stdDevsOnPayment.Add(0.34570215107517);
            strikes.Add(0.385); stdDevsOnExpiry.Add(0.345121496997796); stdDevsOnPayment.Add(0.346139601072313);
            strikes.Add(0.386); stdDevsOnExpiry.Add(0.345538917648939); stdDevsOnPayment.Add(0.346502522551424);
            strikes.Add(0.387); stdDevsOnExpiry.Add(0.345912066412839); stdDevsOnPayment.Add(0.346956174400312);
            strikes.Add(0.388); stdDevsOnExpiry.Add(0.346354785285262); stdDevsOnPayment.Add(0.34737418217536);
            strikes.Add(0.389); stdDevsOnExpiry.Add(0.346746907715123); stdDevsOnPayment.Add(0.347756545876566);
            strikes.Add(0.39); stdDevsOnExpiry.Add(0.347151679255625); stdDevsOnPayment.Add(0.348171313281264);
            strikes.Add(0.391); stdDevsOnExpiry.Add(0.347562775351446); stdDevsOnPayment.Add(0.348576359574914);
            strikes.Add(0.392); stdDevsOnExpiry.Add(0.347986520557909); stdDevsOnPayment.Add(0.348968444387168);
            strikes.Add(0.393); stdDevsOnExpiry.Add(0.348407103486711); stdDevsOnPayment.Add(0.349415615495358);
            strikes.Add(0.394); stdDevsOnExpiry.Add(0.348773927695291); stdDevsOnPayment.Add(0.349820661789009);
            strikes.Add(0.395); stdDevsOnExpiry.Add(0.349203997457074); stdDevsOnPayment.Add(0.350232188823358);
            strikes.Add(0.396); stdDevsOnExpiry.Add(0.349624580385876); stdDevsOnPayment.Add(0.350637235117008);
            strikes.Add(0.397); stdDevsOnExpiry.Add(0.349981917761475); stdDevsOnPayment.Add(0.351029319929262);
            strikes.Add(0.398); stdDevsOnExpiry.Add(0.350396176134957); stdDevsOnPayment.Add(0.351434366222912);
            strikes.Add(0.399); stdDevsOnExpiry.Add(0.350797785397799); stdDevsOnPayment.Add(0.351803768442721);
            strikes.Add(0.4); stdDevsOnExpiry.Add(0.351174096439359); stdDevsOnPayment.Add(0.352250939550912);
            strikes.Add(0.401); stdDevsOnExpiry.Add(0.351597841645821); stdDevsOnPayment.Add(0.352620341770721);
            strikes.Add(0.402); stdDevsOnExpiry.Add(0.351986801798022); stdDevsOnPayment.Add(0.35303186880507);
            strikes.Add(0.403); stdDevsOnExpiry.Add(0.352356788284262); stdDevsOnPayment.Add(0.353407751765577);
            strikes.Add(0.404); stdDevsOnExpiry.Add(0.352793182601365); stdDevsOnPayment.Add(0.353799836577831);
            strikes.Add(0.405); stdDevsOnExpiry.Add(0.353194791864206); stdDevsOnPayment.Add(0.354208123241831);
            strikes.Add(0.406); stdDevsOnExpiry.Add(0.353548966962145); stdDevsOnPayment.Add(0.354587246572688);
            strikes.Add(0.407); stdDevsOnExpiry.Add(0.353953738502647); stdDevsOnPayment.Add(0.354995533236687);
            strikes.Add(0.408); stdDevsOnExpiry.Add(0.354333211821867); stdDevsOnPayment.Add(0.355374656567544);
            strikes.Add(0.409); stdDevsOnExpiry.Add(0.354725334251728); stdDevsOnPayment.Add(0.355776462490846);
            strikes.Add(0.41); stdDevsOnExpiry.Add(0.355104807570948); stdDevsOnPayment.Add(0.356129662858909);
            strikes.Add(0.411); stdDevsOnExpiry.Add(0.355503254556129); stdDevsOnPayment.Add(0.356554151374654);
            strikes.Add(0.412); stdDevsOnExpiry.Add(0.355863754209388); stdDevsOnPayment.Add(0.356910592113067);
            strikes.Add(0.413); stdDevsOnExpiry.Add(0.35627168802755); stdDevsOnPayment.Add(0.357279994332876);
            strikes.Add(0.414); stdDevsOnExpiry.Add(0.356660648179751); stdDevsOnPayment.Add(0.357691521367225);
            strikes.Add(0.415); stdDevsOnExpiry.Add(0.357036959221311); stdDevsOnPayment.Add(0.358080365809129);
            strikes.Add(0.416); stdDevsOnExpiry.Add(0.35740062115223); stdDevsOnPayment.Add(0.358472450621383);
            strikes.Add(0.417); stdDevsOnExpiry.Add(0.357795905859751); stdDevsOnPayment.Add(0.358838612470843);
            strikes.Add(0.418); stdDevsOnExpiry.Add(0.35815008095769); stdDevsOnPayment.Add(0.359211255061002);
            strikes.Add(0.419); stdDevsOnExpiry.Add(0.358561177053512); stdDevsOnPayment.Add(0.359635743576747);
            strikes.Add(0.42); stdDevsOnExpiry.Add(0.358915352151451); stdDevsOnPayment.Add(0.35999218431516);
            strikes.Add(0.421); stdDevsOnExpiry.Add(0.359313799136632); stdDevsOnPayment.Add(0.360361586534969);
            strikes.Add(0.422); stdDevsOnExpiry.Add(0.359674298789891); stdDevsOnPayment.Add(0.360756911717572);
            strikes.Add(0.423); stdDevsOnExpiry.Add(0.36002847388783); stdDevsOnPayment.Add(0.361116592826334);
            strikes.Add(0.424); stdDevsOnExpiry.Add(0.360455381371953); stdDevsOnPayment.Add(0.361502196897889);
            strikes.Add(0.425); stdDevsOnExpiry.Add(0.360809556469892); stdDevsOnPayment.Add(0.361891041339793);
            strikes.Add(0.426); stdDevsOnExpiry.Add(0.361141595624209); stdDevsOnPayment.Add(0.362260443559603);
            strikes.Add(0.427); stdDevsOnExpiry.Add(0.361562178553012); stdDevsOnPayment.Add(0.362594201705571);
            strikes.Add(0.428); stdDevsOnExpiry.Add(0.361922678206271); stdDevsOnPayment.Add(0.363021930591666);
            strikes.Add(0.429); stdDevsOnExpiry.Add(0.36227369102655); stdDevsOnPayment.Add(0.363358929107983);
            strikes.Add(0.43); stdDevsOnExpiry.Add(0.362637352957469); stdDevsOnPayment.Add(0.363741292809189);
            strikes.Add(0.431); stdDevsOnExpiry.Add(0.362994690333068); stdDevsOnPayment.Add(0.364075050955157);
            strikes.Add(0.432); stdDevsOnExpiry.Add(0.363342540875687); stdDevsOnPayment.Add(0.364473616508109);
            strikes.Add(0.433); stdDevsOnExpiry.Add(0.363737825583208); stdDevsOnPayment.Add(0.364820336135474);
            strikes.Add(0.434); stdDevsOnExpiry.Add(0.364095162958807); stdDevsOnPayment.Add(0.365186497984934);
            strikes.Add(0.435); stdDevsOnExpiry.Add(0.364455662612066); stdDevsOnPayment.Add(0.365562380945441);
            strikes.Add(0.436); stdDevsOnExpiry.Add(0.364866758707888); stdDevsOnPayment.Add(0.365889658350711);
            strikes.Add(0.437); stdDevsOnExpiry.Add(0.365208284695186); stdDevsOnPayment.Add(0.366291464274012);
            strikes.Add(0.438); stdDevsOnExpiry.Add(0.365584595736746); stdDevsOnPayment.Add(0.366657626123472);
            strikes.Add(0.439); stdDevsOnExpiry.Add(0.365907148058083); stdDevsOnPayment.Add(0.367001105380488);
            strikes.Add(0.44); stdDevsOnExpiry.Add(0.366270809989003); stdDevsOnPayment.Add(0.367380228711345);
            strikes.Add(0.441); stdDevsOnExpiry.Add(0.366612335976301); stdDevsOnPayment.Add(0.367710746486964);
            strikes.Add(0.442); stdDevsOnExpiry.Add(0.366998133850841); stdDevsOnPayment.Add(0.368083389077122);
            strikes.Add(0.443); stdDevsOnExpiry.Add(0.367333335282819); stdDevsOnPayment.Add(0.368452791296931);
            strikes.Add(0.444); stdDevsOnExpiry.Add(0.367693834936078); stdDevsOnPayment.Add(0.368793030183598);
            strikes.Add(0.445); stdDevsOnExpiry.Add(0.368073308255299); stdDevsOnPayment.Add(0.369162432403407);
            strikes.Add(0.446); stdDevsOnExpiry.Add(0.368440132463878); stdDevsOnPayment.Add(0.369492950179026);
            strikes.Add(0.447); stdDevsOnExpiry.Add(0.368765847062875); stdDevsOnPayment.Add(0.369878554250581);
            strikes.Add(0.448); stdDevsOnExpiry.Add(0.369104210772513); stdDevsOnPayment.Add(0.370196110544803);
            strikes.Add(0.449); stdDevsOnExpiry.Add(0.369490008647054); stdDevsOnPayment.Add(0.370559032023914);
            strikes.Add(0.45); stdDevsOnExpiry.Add(0.369825210079032); stdDevsOnPayment.Add(0.370896030540231);
            strikes.Add(0.451); stdDevsOnExpiry.Add(0.370204683398252); stdDevsOnPayment.Add(0.371278394241437);
            strikes.Add(0.452); stdDevsOnExpiry.Add(0.370514586608948); stdDevsOnPayment.Add(0.37159271016531);
            strikes.Add(0.453); stdDevsOnExpiry.Add(0.370856112596247); stdDevsOnPayment.Add(0.371968593125818);
            strikes.Add(0.454); stdDevsOnExpiry.Add(0.371197638583545); stdDevsOnPayment.Add(0.37232503386423);
            strikes.Add(0.455); stdDevsOnExpiry.Add(0.371586598735746); stdDevsOnPayment.Add(0.372665272750896);
            strikes.Add(0.456); stdDevsOnExpiry.Add(0.371874366002821); stdDevsOnPayment.Add(0.373002271267214);
            strikes.Add(0.457); stdDevsOnExpiry.Add(0.37222854110076); stdDevsOnPayment.Add(0.373358712005626);
            strikes.Add(0.458); stdDevsOnExpiry.Add(0.372579553921038); stdDevsOnPayment.Add(0.373685989410896);
            strikes.Add(0.459); stdDevsOnExpiry.Add(0.372917917630676); stdDevsOnPayment.Add(0.374045670519657);
            strikes.Add(0.46); stdDevsOnExpiry.Add(0.373332176004159); stdDevsOnPayment.Add(0.374382669035975);
            strikes.Add(0.461); stdDevsOnExpiry.Add(0.373597807327613); stdDevsOnPayment.Add(0.374748830885435);
            strikes.Add(0.462); stdDevsOnExpiry.Add(0.373977280646833); stdDevsOnPayment.Add(0.375095550512799);
            strikes.Add(0.463); stdDevsOnExpiry.Add(0.374287183857529); stdDevsOnPayment.Add(0.375406626066323);
            strikes.Add(0.464); stdDevsOnExpiry.Add(0.374616060734187); stdDevsOnPayment.Add(0.375727422730894);
            strikes.Add(0.465); stdDevsOnExpiry.Add(0.375001858608727); stdDevsOnPayment.Add(0.376096824950703);
            strikes.Add(0.466); stdDevsOnExpiry.Add(0.375311761819424); stdDevsOnPayment.Add(0.376420861985624);
            strikes.Add(0.467); stdDevsOnExpiry.Add(0.375599529086499); stdDevsOnPayment.Add(0.376754620131592);
            strikes.Add(0.468); stdDevsOnExpiry.Add(0.376004300627001); stdDevsOnPayment.Add(0.377098099388607);
            strikes.Add(0.469); stdDevsOnExpiry.Add(0.376292067894076); stdDevsOnPayment.Add(0.377425376793877);
            strikes.Add(0.47); stdDevsOnExpiry.Add(0.376646242992015); stdDevsOnPayment.Add(0.377755894569496);
            strikes.Add(0.471); stdDevsOnExpiry.Add(0.376952983925051); stdDevsOnPayment.Add(0.378079931604416);
            strikes.Add(0.472); stdDevsOnExpiry.Add(0.377332457244272); stdDevsOnPayment.Add(0.378449333824225);
            strikes.Add(0.473); stdDevsOnExpiry.Add(0.377661334120929); stdDevsOnPayment.Add(0.378776611229495);
            strikes.Add(0.474); stdDevsOnExpiry.Add(0.377999697830567); stdDevsOnPayment.Add(0.379113609745812);
            strikes.Add(0.475); stdDevsOnExpiry.Add(0.378277978264662); stdDevsOnPayment.Add(0.379463569743526);
            strikes.Add(0.476); stdDevsOnExpiry.Add(0.378635315640261); stdDevsOnPayment.Add(0.379761683815653);
            strikes.Add(0.477); stdDevsOnExpiry.Add(0.378948381128618); stdDevsOnPayment.Add(0.380095441961621);
            strikes.Add(0.478); stdDevsOnExpiry.Add(0.379264608894634); stdDevsOnPayment.Add(0.380471324922129);
            strikes.Add(0.479); stdDevsOnExpiry.Add(0.379574512105331); stdDevsOnPayment.Add(0.380779160105303);
            strikes.Add(0.48); stdDevsOnExpiry.Add(0.379900226704328); stdDevsOnPayment.Add(0.381122639362319);
            strikes.Add(0.481); stdDevsOnExpiry.Add(0.380276537745888); stdDevsOnPayment.Add(0.381414272693747);
            strikes.Add(0.482); stdDevsOnExpiry.Add(0.380605414622546); stdDevsOnPayment.Add(0.381773953802509);
            strikes.Add(0.483); stdDevsOnExpiry.Add(0.380896344167281); stdDevsOnPayment.Add(0.382068827504286);
            strikes.Add(0.484); stdDevsOnExpiry.Add(0.381234707876919); stdDevsOnPayment.Add(0.382405826020603);
            strikes.Add(0.485); stdDevsOnExpiry.Add(0.381595207530179); stdDevsOnPayment.Add(0.382716901574127);
            strikes.Add(0.486); stdDevsOnExpiry.Add(0.381835540632351); stdDevsOnPayment.Add(0.383031217498);
            strikes.Add(0.487); stdDevsOnExpiry.Add(0.382139119287727); stdDevsOnPayment.Add(0.38331637008873);
            strikes.Add(0.488); stdDevsOnExpiry.Add(0.382486969830346); stdDevsOnPayment.Add(0.383685772308539);
            strikes.Add(0.489); stdDevsOnExpiry.Add(0.382847469483605); stdDevsOnPayment.Add(0.383964444158571);
            strikes.Add(0.49); stdDevsOnExpiry.Add(0.38313207447302); stdDevsOnPayment.Add(0.384301442674888);
            strikes.Add(0.491); stdDevsOnExpiry.Add(0.383479925015639); stdDevsOnPayment.Add(0.384661123783649);
            strikes.Add(0.492); stdDevsOnExpiry.Add(0.383796152781656); stdDevsOnPayment.Add(0.384917113041236);
            strikes.Add(0.493); stdDevsOnExpiry.Add(0.384106055992352); stdDevsOnPayment.Add(0.385280034520347);
            strikes.Add(0.494); stdDevsOnExpiry.Add(0.384457068812631); stdDevsOnPayment.Add(0.38559435044422);
            strikes.Add(0.495); stdDevsOnExpiry.Add(0.384744836079706); stdDevsOnPayment.Add(0.385931348960537);
            strikes.Add(0.496); stdDevsOnExpiry.Add(0.385048414735082); stdDevsOnPayment.Add(0.386222982291966);
            strikes.Add(0.497); stdDevsOnExpiry.Add(0.38537412933408); stdDevsOnPayment.Add(0.386559980808283);
            strikes.Add(0.498); stdDevsOnExpiry.Add(0.385709330766058); stdDevsOnPayment.Add(0.386848373769362);
            strikes.Add(0.499); stdDevsOnExpiry.Add(0.386038207642715); stdDevsOnPayment.Add(0.387146487841489);
            strikes.Add(0.5); stdDevsOnExpiry.Add(0.386338624020431); stdDevsOnPayment.Add(0.387460803765362);
            strikes.Add(0.501); stdDevsOnExpiry.Add(0.386623229009846); stdDevsOnPayment.Add(0.387765398578187);
            strikes.Add(0.502); stdDevsOnExpiry.Add(0.386952105886504); stdDevsOnPayment.Add(0.388108877835202);
            strikes.Add(0.503); stdDevsOnExpiry.Add(0.38724935998656); stdDevsOnPayment.Add(0.388374588203837);
            strikes.Add(0.504); stdDevsOnExpiry.Add(0.387584561418537); stdDevsOnPayment.Add(0.388718067460853);
            strikes.Add(0.505); stdDevsOnExpiry.Add(0.387856517297312); stdDevsOnPayment.Add(0.388980537459138);
            strikes.Add(0.506); stdDevsOnExpiry.Add(0.388213854672911); stdDevsOnPayment.Add(0.3893402185679);
            strikes.Add(0.507); stdDevsOnExpiry.Add(0.388514271050627); stdDevsOnPayment.Add(0.389605928936535);
            strikes.Add(0.508); stdDevsOnExpiry.Add(0.388748279597479); stdDevsOnPayment.Add(0.389994773378439);
            strikes.Add(0.509); stdDevsOnExpiry.Add(0.389073994196477); stdDevsOnPayment.Add(0.390247522265677);
            strikes.Add(0.51); stdDevsOnExpiry.Add(0.389431331572076); stdDevsOnPayment.Add(0.39056183818955);
            strikes.Add(0.511); stdDevsOnExpiry.Add(0.389643204175307); stdDevsOnPayment.Add(0.390882634854121);
            strikes.Add(0.512); stdDevsOnExpiry.Add(0.390025839772187); stdDevsOnPayment.Add(0.391193710407644);
            strikes.Add(0.513); stdDevsOnExpiry.Add(0.390351554371185); stdDevsOnPayment.Add(0.391491824479771);
            strikes.Add(0.514); stdDevsOnExpiry.Add(0.390607698861658); stdDevsOnPayment.Add(0.391828822996088);
            strikes.Add(0.515); stdDevsOnExpiry.Add(0.390901790684054); stdDevsOnPayment.Add(0.392081571883326);
            strikes.Add(0.516); stdDevsOnExpiry.Add(0.391218018450071); stdDevsOnPayment.Add(0.392399128177548);
            strikes.Add(0.517); stdDevsOnExpiry.Add(0.391524759383107); stdDevsOnPayment.Add(0.392726405582818);
            strikes.Add(0.518); stdDevsOnExpiry.Add(0.391809364372522); stdDevsOnPayment.Add(0.393034240765992);
            strikes.Add(0.519); stdDevsOnExpiry.Add(0.392093969361938); stdDevsOnPayment.Add(0.393261066690437);
            strikes.Add(0.52); stdDevsOnExpiry.Add(0.392419683960935); stdDevsOnPayment.Add(0.393617507428849);
            strikes.Add(0.521); stdDevsOnExpiry.Add(0.392723262616311); stdDevsOnPayment.Add(0.393948025204468);
            strikes.Add(0.522); stdDevsOnExpiry.Add(0.393001543050406); stdDevsOnPayment.Add(0.39422345668415);
            strikes.Add(0.523); stdDevsOnExpiry.Add(0.393336744482384); stdDevsOnPayment.Add(0.394476205571388);
            strikes.Add(0.524); stdDevsOnExpiry.Add(0.393611862638818); stdDevsOnPayment.Add(0.394813204087705);
            strikes.Add(0.525); stdDevsOnExpiry.Add(0.393940739515476); stdDevsOnPayment.Add(0.395075674085991);
            strikes.Add(0.526); stdDevsOnExpiry.Add(0.394168423507008); stdDevsOnPayment.Add(0.395435355194752);
            strikes.Add(0.527); stdDevsOnExpiry.Add(0.394459353051744); stdDevsOnPayment.Add(0.39568810408199);
            strikes.Add(0.528); stdDevsOnExpiry.Add(0.394800879039042); stdDevsOnPayment.Add(0.396012141116911);
            strikes.Add(0.529); stdDevsOnExpiry.Add(0.395025400752914); stdDevsOnPayment.Add(0.396274611115196);
            strikes.Add(0.53); stdDevsOnExpiry.Add(0.395347953074251); stdDevsOnPayment.Add(0.396572725187323);
            strikes.Add(0.531); stdDevsOnExpiry.Add(0.395613584397705); stdDevsOnPayment.Add(0.396893521851894);
            strikes.Add(0.532); stdDevsOnExpiry.Add(0.395951948107343); stdDevsOnPayment.Add(0.397162472590878);
            strikes.Add(0.533); stdDevsOnExpiry.Add(0.39626817587336); stdDevsOnPayment.Add(0.397483269255449);
            strikes.Add(0.534); stdDevsOnExpiry.Add(0.396505346697872); stdDevsOnPayment.Add(0.397774902586878);
            strikes.Add(0.535); stdDevsOnExpiry.Add(0.39683106129687); stdDevsOnPayment.Add(0.398034132214814);
            strikes.Add(0.536); stdDevsOnExpiry.Add(0.397137802229906); stdDevsOnPayment.Add(0.398329005916591);
            strikes.Add(0.537); stdDevsOnExpiry.Add(0.39739078444272); stdDevsOnPayment.Add(0.398656283321861);
            strikes.Add(0.538); stdDevsOnExpiry.Add(0.397672227154475); stdDevsOnPayment.Add(0.39890579183875);
            strikes.Add(0.539); stdDevsOnExpiry.Add(0.397931533922608); stdDevsOnPayment.Add(0.399184463688781);
            strikes.Add(0.54); stdDevsOnExpiry.Add(0.398269897632246); stdDevsOnPayment.Add(0.399463135538813);
            strikes.Add(0.541); stdDevsOnExpiry.Add(0.398484932513138); stdDevsOnPayment.Add(0.399770970721987);
            strikes.Add(0.542); stdDevsOnExpiry.Add(0.398826458500436); stdDevsOnPayment.Add(0.400023719609225);
            strikes.Add(0.543); stdDevsOnExpiry.Add(0.399126874878152); stdDevsOnPayment.Add(0.400334795162749);
            strikes.Add(0.544); stdDevsOnExpiry.Add(0.399398830756927); stdDevsOnPayment.Add(0.400603745901733);
            strikes.Add(0.545); stdDevsOnExpiry.Add(0.399680273468682); stdDevsOnPayment.Add(0.400914821455256);
            strikes.Add(0.546); stdDevsOnExpiry.Add(0.399917444293194); stdDevsOnPayment.Add(0.40122589700878);
            strikes.Add(0.547); stdDevsOnExpiry.Add(0.400274781668793); stdDevsOnPayment.Add(0.401465684414621);
            strikes.Add(0.548); stdDevsOnExpiry.Add(0.400543575269907); stdDevsOnPayment.Add(0.401812404041986);
            strikes.Add(0.549); stdDevsOnExpiry.Add(0.400828180259323); stdDevsOnPayment.Add(0.402016547373986);
            strikes.Add(0.55); stdDevsOnExpiry.Add(0.401103298415757); stdDevsOnPayment.Add(0.402347065149604);
            strikes.Add(0.551); stdDevsOnExpiry.Add(0.401356280628571); stdDevsOnPayment.Add(0.402612775518239);
            strikes.Add(0.552); stdDevsOnExpiry.Add(0.401666183839267); stdDevsOnPayment.Add(0.402875245516525);
            strikes.Add(0.553); stdDevsOnExpiry.Add(0.401874894164838); stdDevsOnPayment.Add(0.403186321070048);
            strikes.Add(0.554); stdDevsOnExpiry.Add(0.402238556095758); stdDevsOnPayment.Add(0.403468233290429);
            strikes.Add(0.555); stdDevsOnExpiry.Add(0.40246624008729); stdDevsOnPayment.Add(0.403714501436968);
            strikes.Add(0.556); stdDevsOnExpiry.Add(0.402747682799045); stdDevsOnPayment.Add(0.403989932916651);
            strikes.Add(0.557); stdDevsOnExpiry.Add(0.403070235120382); stdDevsOnPayment.Add(0.404297768099825);
            strikes.Add(0.558); stdDevsOnExpiry.Add(0.403326379610856); stdDevsOnPayment.Add(0.404589401431253);
            strikes.Add(0.559); stdDevsOnExpiry.Add(0.403535089936427); stdDevsOnPayment.Add(0.404884275133031);
            strikes.Add(0.56); stdDevsOnExpiry.Add(0.403797558982221); stdDevsOnPayment.Add(0.405104620316777);
            strikes.Add(0.561); stdDevsOnExpiry.Add(0.404126435858878); stdDevsOnPayment.Add(0.405393013277856);
            strikes.Add(0.562); stdDevsOnExpiry.Add(0.404423689958934); stdDevsOnPayment.Add(0.405668444757538);
            strikes.Add(0.563); stdDevsOnExpiry.Add(0.404733593169631); stdDevsOnPayment.Add(0.405901751422681);
            strikes.Add(0.564); stdDevsOnExpiry.Add(0.404977088549464); stdDevsOnPayment.Add(0.406203105865157);
            strikes.Add(0.565); stdDevsOnExpiry.Add(0.405220583929296); stdDevsOnPayment.Add(0.406510941048331);
            strikes.Add(0.566); stdDevsOnExpiry.Add(0.405533649417653); stdDevsOnPayment.Add(0.406741007343125);
            strikes.Add(0.567); stdDevsOnExpiry.Add(0.405742359743224); stdDevsOnPayment.Add(0.407042361785601);
            strikes.Add(0.568); stdDevsOnExpiry.Add(0.406083885730522); stdDevsOnPayment.Add(0.407295110672839);
            strikes.Add(0.569); stdDevsOnExpiry.Add(0.406295758333754); stdDevsOnPayment.Add(0.407593224744966);
            strikes.Add(0.57); stdDevsOnExpiry.Add(0.406643608876372); stdDevsOnPayment.Add(0.40786217548395);
            strikes.Add(0.571); stdDevsOnExpiry.Add(0.406814371870021); stdDevsOnPayment.Add(0.408082520667695);
            strikes.Add(0.572); stdDevsOnExpiry.Add(0.407114788247737); stdDevsOnPayment.Add(0.408416278813663);
            strikes.Add(0.573); stdDevsOnExpiry.Add(0.407434178291414); stdDevsOnPayment.Add(0.408636623997409);
            strikes.Add(0.574); stdDevsOnExpiry.Add(0.407623914951024); stdDevsOnPayment.Add(0.40891853621779);
            strikes.Add(0.575); stdDevsOnExpiry.Add(0.407936980439381); stdDevsOnPayment.Add(0.409252294363758);
            strikes.Add(0.576); stdDevsOnExpiry.Add(0.408265857316039); stdDevsOnPayment.Add(0.409449956955059);
            strikes.Add(0.577); stdDevsOnExpiry.Add(0.408496703585231); stdDevsOnPayment.Add(0.409748071027186);
            strikes.Add(0.578); stdDevsOnExpiry.Add(0.408774984019326); stdDevsOnPayment.Add(0.410000819914424);
            strikes.Add(0.579); stdDevsOnExpiry.Add(0.409024803954479); stdDevsOnPayment.Add(0.410295693616202);
            strikes.Add(0.58); stdDevsOnExpiry.Add(0.409261974778992); stdDevsOnPayment.Add(0.410567884725535);
            strikes.Add(0.581); stdDevsOnExpiry.Add(0.409518119269465); stdDevsOnPayment.Add(0.410755826205789);
            strikes.Add(0.582); stdDevsOnExpiry.Add(0.409831184757822); stdDevsOnPayment.Add(0.411125228425598);
            strikes.Add(0.583); stdDevsOnExpiry.Add(0.410109465191917); stdDevsOnPayment.Add(0.411394179164582);
            strikes.Add(0.584); stdDevsOnExpiry.Add(0.41034979829409); stdDevsOnPayment.Add(0.411614524348328);
            strikes.Add(0.585); stdDevsOnExpiry.Add(0.410583806840942); stdDevsOnPayment.Add(0.411867273235566);
            strikes.Add(0.586); stdDevsOnExpiry.Add(0.410931657383561); stdDevsOnPayment.Add(0.412171868048391);
            strikes.Add(0.587); stdDevsOnExpiry.Add(0.41109925809955); stdDevsOnPayment.Add(0.41237925175074);
            strikes.Add(0.588); stdDevsOnExpiry.Add(0.411374376255984); stdDevsOnPayment.Add(0.412670885082168);
            strikes.Add(0.589); stdDevsOnExpiry.Add(0.411690604022001); stdDevsOnPayment.Add(0.41294631656185);
            strikes.Add(0.59); stdDevsOnExpiry.Add(0.411943586234814); stdDevsOnPayment.Add(0.413215267300834);
            strikes.Add(0.591); stdDevsOnExpiry.Add(0.412215542113589); stdDevsOnPayment.Add(0.413458295077025);
            strikes.Add(0.592); stdDevsOnExpiry.Add(0.412348357775316); stdDevsOnPayment.Add(0.413707803593913);
            strikes.Add(0.593); stdDevsOnExpiry.Add(0.412677234651974); stdDevsOnPayment.Add(0.413931389148008);
            strikes.Add(0.594); stdDevsOnExpiry.Add(0.412908080921166); stdDevsOnPayment.Add(0.4141679361835);
            strikes.Add(0.595); stdDevsOnExpiry.Add(0.41318003679994); stdDevsOnPayment.Add(0.414479011737024);
            strikes.Add(0.596); stdDevsOnExpiry.Add(0.413382422570191); stdDevsOnPayment.Add(0.414747962476008);
            strikes.Add(0.597); stdDevsOnExpiry.Add(0.413701812613868); stdDevsOnPayment.Add(0.415000711363246);
            strikes.Add(0.598); stdDevsOnExpiry.Add(0.413954794826682); stdDevsOnPayment.Add(0.415282623583626);
            strikes.Add(0.599); stdDevsOnExpiry.Add(0.414229912983116); stdDevsOnPayment.Add(0.415538612841214);
            strikes.Add(0.6); stdDevsOnExpiry.Add(0.414508193417211); stdDevsOnPayment.Add(0.415778400247055);
            strikes.Add(0.601); stdDevsOnExpiry.Add(0.414748526519384); stdDevsOnPayment.Add(0.416005226171499);
            strikes.Add(0.602); stdDevsOnExpiry.Add(0.415020482398158); stdDevsOnPayment.Add(0.41632602283607);
            strikes.Add(0.603); stdDevsOnExpiry.Add(0.415276626888632); stdDevsOnPayment.Add(0.416513964316324);
            strikes.Add(0.604); stdDevsOnExpiry.Add(0.415583367821668); stdDevsOnPayment.Add(0.416795876536705);
            strikes.Add(0.605); stdDevsOnExpiry.Add(0.415731994871696); stdDevsOnPayment.Add(0.417119913571625);
            strikes.Add(0.606); stdDevsOnExpiry.Add(0.416060871748354); stdDevsOnPayment.Add(0.417217124682101);
            strikes.Add(0.607); stdDevsOnExpiry.Add(0.416335989904788); stdDevsOnPayment.Add(0.417550882828069);
            strikes.Add(0.608); stdDevsOnExpiry.Add(0.416532051119719); stdDevsOnPayment.Add(0.417861958381593);
            strikes.Add(0.609); stdDevsOnExpiry.Add(0.416772384221892); stdDevsOnPayment.Add(0.41811794763918);
            strikes.Add(0.61); stdDevsOnExpiry.Add(0.417028528712365); stdDevsOnPayment.Add(0.418286446897339);
            strikes.Add(0.611); stdDevsOnExpiry.Add(0.417281510925179); stdDevsOnPayment.Add(0.418587801339814);
            strikes.Add(0.612); stdDevsOnExpiry.Add(0.417455436196488); stdDevsOnPayment.Add(0.418817867634608);
            strikes.Add(0.613); stdDevsOnExpiry.Add(0.417721067519942); stdDevsOnPayment.Add(0.419047933929401);
            strikes.Add(0.614); stdDevsOnExpiry.Add(0.418068918062561); stdDevsOnPayment.Add(0.419313644298036);
            strikes.Add(0.615); stdDevsOnExpiry.Add(0.418204896001948); stdDevsOnPayment.Add(0.41962471985156);
            strikes.Add(0.616); stdDevsOnExpiry.Add(0.418533772878605); stdDevsOnPayment.Add(0.419812661331814);
            strikes.Add(0.617); stdDevsOnExpiry.Add(0.418770943703118); stdDevsOnPayment.Add(0.420036246885909);
            strikes.Add(0.618); stdDevsOnExpiry.Add(0.419042899581892); stdDevsOnPayment.Add(0.420282515032448);
            strikes.Add(0.619); stdDevsOnExpiry.Add(0.419229473963842); stdDevsOnPayment.Add(0.420596830956321);
            strikes.Add(0.62); stdDevsOnExpiry.Add(0.419529890341558); stdDevsOnPayment.Add(0.420807455029019);
            strikes.Add(0.621); stdDevsOnExpiry.Add(0.41974492522245); stdDevsOnPayment.Add(0.421037521323813);
            strikes.Add(0.622); stdDevsOnExpiry.Add(0.420026367934205); stdDevsOnPayment.Add(0.421335635395939);
            strikes.Add(0.623); stdDevsOnExpiry.Add(0.420282512424678); stdDevsOnPayment.Add(0.421523576876193);
            strikes.Add(0.624); stdDevsOnExpiry.Add(0.420427977197046); stdDevsOnPayment.Add(0.421857335022161);
            strikes.Add(0.625); stdDevsOnExpiry.Add(0.420658823466239); stdDevsOnPayment.Add(0.422061478354161);
            strikes.Add(0.626); stdDevsOnExpiry.Add(0.421019323119498); stdDevsOnPayment.Add(0.422259140945463);
            strikes.Add(0.627); stdDevsOnExpiry.Add(0.421193248390807); stdDevsOnPayment.Add(0.422589658721081);
            strikes.Add(0.628); stdDevsOnExpiry.Add(0.421395634161058); stdDevsOnPayment.Add(0.422774359830986);
            strikes.Add(0.629); stdDevsOnExpiry.Add(0.421702375094094); stdDevsOnPayment.Add(0.423036829829271);
            strikes.Add(0.63); stdDevsOnExpiry.Add(0.421882624920724); stdDevsOnPayment.Add(0.423273376864763);
            strikes.Add(0.631); stdDevsOnExpiry.Add(0.422176716743119); stdDevsOnPayment.Add(0.423493722048509);
            strikes.Add(0.632); stdDevsOnExpiry.Add(0.422401238456991); stdDevsOnPayment.Add(0.423720547972953);
            strikes.Add(0.633); stdDevsOnExpiry.Add(0.422651058392145); stdDevsOnPayment.Add(0.423931172045652);
            strikes.Add(0.634); stdDevsOnExpiry.Add(0.434664551223124); stdDevsOnPayment.Add(0.424248728339874);
            strikes.Add(0.635); stdDevsOnExpiry.Add(0.434920695713598); stdDevsOnPayment.Add(0.424478794634667);
            strikes.Add(0.636); stdDevsOnExpiry.Add(0.423403680475265); stdDevsOnPayment.Add(0.424715341670159);
            strikes.Add(0.637); stdDevsOnExpiry.Add(0.435432984694545); stdDevsOnPayment.Add(0.424990773149841);
            strikes.Add(0.638); stdDevsOnExpiry.Add(0.423846399347688); stdDevsOnPayment.Add(0.425172233889397);
            strikes.Add(0.639); stdDevsOnExpiry.Add(0.435910488621231); stdDevsOnPayment.Add(0.425428223146984);
            strikes.Add(0.64); stdDevsOnExpiry.Add(0.436163470834044); stdDevsOnPayment.Add(0.425645327960381);
            strikes.Add(0.641); stdDevsOnExpiry.Add(0.436413290769197); stdDevsOnPayment.Add(0.425894836477269);
            strikes.Add(0.642); stdDevsOnExpiry.Add(0.43664413703839); stdDevsOnPayment.Add(0.42617674869765);
            strikes.Add(0.643); stdDevsOnExpiry.Add(0.436897119251203); stdDevsOnPayment.Add(0.426361449807555);
            strikes.Add(0.644); stdDevsOnExpiry.Add(0.437159588296997); stdDevsOnPayment.Add(0.426607717954094);
            strikes.Add(0.645); stdDevsOnExpiry.Add(0.437358811789588); stdDevsOnPayment.Add(0.42678917869365);
            strikes.Add(0.646); stdDevsOnExpiry.Add(0.437602307169421); stdDevsOnPayment.Add(0.427165061654157);
            strikes.Add(0.647); stdDevsOnExpiry.Add(0.437852127104574); stdDevsOnPayment.Add(0.427340041653014);
            strikes.Add(0.648); stdDevsOnExpiry.Add(0.438076648818446); stdDevsOnPayment.Add(0.427608992391998);
            strikes.Add(0.649); stdDevsOnExpiry.Add(0.43833279330892); stdDevsOnPayment.Add(0.427780732020506);
            strikes.Add(0.65); stdDevsOnExpiry.Add(0.438569964133432); stdDevsOnPayment.Add(0.428033480907744);
            strikes.Add(0.651); stdDevsOnExpiry.Add(0.438810297235605); stdDevsOnPayment.Add(0.428308912387426);
            strikes.Add(0.652); stdDevsOnExpiry.Add(0.439056954893098); stdDevsOnPayment.Add(0.428548699793267);
            strikes.Add(0.653); stdDevsOnExpiry.Add(0.43927831432931); stdDevsOnPayment.Add(0.428713958681077);
            strikes.Add(0.654); stdDevsOnExpiry.Add(0.439534458819784); stdDevsOnPayment.Add(0.429012072753204);
            strikes.Add(0.655); stdDevsOnExpiry.Add(0.439765305088976); stdDevsOnPayment.Add(0.429193533492759);
            strikes.Add(0.656); stdDevsOnExpiry.Add(0.440015125024129); stdDevsOnPayment.Add(0.429443042009648);
            strikes.Add(0.657); stdDevsOnExpiry.Add(0.440264944959282); stdDevsOnPayment.Add(0.429686069785838);
            strikes.Add(0.658); stdDevsOnExpiry.Add(0.440454681618893); stdDevsOnPayment.Add(0.429857809414346);
            strikes.Add(0.659); stdDevsOnExpiry.Add(0.440723475220007); stdDevsOnPayment.Add(0.430185086819615);
            strikes.Add(0.66); stdDevsOnExpiry.Add(0.440957483766859); stdDevsOnPayment.Add(0.430402191633012);
            strikes.Add(0.661); stdDevsOnExpiry.Add(0.441210465979673); stdDevsOnPayment.Add(0.43057393126152);
            strikes.Add(0.662); stdDevsOnExpiry.Add(0.441403364916943); stdDevsOnPayment.Add(0.430758632371424);
            strikes.Add(0.663); stdDevsOnExpiry.Add(0.441681645351038); stdDevsOnPayment.Add(0.431076188665647);
            strikes.Add(0.664); stdDevsOnExpiry.Add(0.441858732900007); stdDevsOnPayment.Add(0.431290053108694);
            strikes.Add(0.665); stdDevsOnExpiry.Add(0.44209590372452); stdDevsOnPayment.Add(0.431484475329646);
            strikes.Add(0.666); stdDevsOnExpiry.Add(0.442374184158615); stdDevsOnPayment.Add(0.431721022365138);
            strikes.Add(0.667); stdDevsOnExpiry.Add(0.442592381317166); stdDevsOnPayment.Add(0.432022376807614);
            strikes.Add(0.668); stdDevsOnExpiry.Add(0.442813740753378); stdDevsOnPayment.Add(0.432194116436122);
            strikes.Add(0.669); stdDevsOnExpiry.Add(0.443076209799172); stdDevsOnPayment.Add(0.432417701990217);
            strikes.Add(0.67); stdDevsOnExpiry.Add(0.443275433291763); stdDevsOnPayment.Add(0.432644527914661);
            strikes.Add(0.671); stdDevsOnExpiry.Add(0.443531577782236); stdDevsOnPayment.Add(0.43293616124609);
            strikes.Add(0.672); stdDevsOnExpiry.Add(0.443705503053546); stdDevsOnPayment.Add(0.433030131986216);
            strikes.Add(0.673); stdDevsOnExpiry.Add(0.443964809821679); stdDevsOnPayment.Add(0.433376851613581);
            strikes.Add(0.674); stdDevsOnExpiry.Add(0.444183006980231); stdDevsOnPayment.Add(0.433603677538025);
            strikes.Add(0.675); stdDevsOnExpiry.Add(0.444439151470705); stdDevsOnPayment.Add(0.433772176796184);
            strikes.Add(0.676); stdDevsOnExpiry.Add(0.444676322295217); stdDevsOnPayment.Add(0.434083252349708);
            strikes.Add(0.677); stdDevsOnExpiry.Add(0.44491665539739); stdDevsOnPayment.Add(0.434290636052057);
            strikes.Add(0.678); stdDevsOnExpiry.Add(0.44510322977934); stdDevsOnPayment.Add(0.43440080864393);
            strikes.Add(0.679); stdDevsOnExpiry.Add(0.445324589215552); stdDevsOnPayment.Add(0.43464383642012);
            strikes.Add(0.68); stdDevsOnExpiry.Add(0.445602869649647); stdDevsOnPayment.Add(0.434909546788755);
            strikes.Add(0.681); stdDevsOnExpiry.Add(0.445757821254995); stdDevsOnPayment.Add(0.435084526787612);
            strikes.Add(0.682); stdDevsOnExpiry.Add(0.446029777133769); stdDevsOnPayment.Add(0.43537616011904);
            strikes.Add(0.683); stdDevsOnExpiry.Add(0.446282759346583); stdDevsOnPayment.Add(0.435567341969643);
            strikes.Add(0.684); stdDevsOnExpiry.Add(0.446478820561513); stdDevsOnPayment.Add(0.435719639376056);
            strikes.Add(0.685); stdDevsOnExpiry.Add(0.446655908110483); stdDevsOnPayment.Add(0.435969147892944);
            strikes.Add(0.686); stdDevsOnExpiry.Add(0.446940513099898); stdDevsOnPayment.Add(0.43627050233542);
            strikes.Add(0.687); stdDevsOnExpiry.Add(0.44716187253611); stdDevsOnPayment.Add(0.448587150032745);
            strikes.Add(0.688); stdDevsOnExpiry.Add(0.447424341581904); stdDevsOnPayment.Add(0.436633423814531);
            strikes.Add(0.689); stdDevsOnExpiry.Add(0.447604591408533); stdDevsOnPayment.Add(0.436827846035483);
            strikes.Add(0.69); stdDevsOnExpiry.Add(0.447800652623464); stdDevsOnPayment.Add(0.44929679113922);
            strikes.Add(0.691); stdDevsOnExpiry.Add(0.448085257612879); stdDevsOnPayment.Add(0.449510655582268);
            strikes.Add(0.692); stdDevsOnExpiry.Add(0.44830029249377); stdDevsOnPayment.Add(0.449750442988109);
            strikes.Add(0.693); stdDevsOnExpiry.Add(0.448505840541681); stdDevsOnPayment.Add(0.449986990023601);
            strikes.Add(0.694); stdDevsOnExpiry.Add(0.448708226311932); stdDevsOnPayment.Add(0.450226777429442);
            strikes.Add(0.695); stdDevsOnExpiry.Add(0.448935910303464); stdDevsOnPayment.Add(0.450447122613188);
            strikes.Add(0.696); stdDevsOnExpiry.Add(0.449141458351375); stdDevsOnPayment.Add(0.450638304463791);
            strikes.Add(0.697); stdDevsOnExpiry.Add(0.449365980065247); stdDevsOnPayment.Add(0.450878091869632);
            strikes.Add(0.698); stdDevsOnExpiry.Add(0.449565203557838); stdDevsOnPayment.Add(0.451091956312679);
            strikes.Add(0.699); stdDevsOnExpiry.Add(0.449871944490874); stdDevsOnPayment.Add(0.451296099644679);
            strikes.Add(0.7); stdDevsOnExpiry.Add(0.450061681150484); stdDevsOnPayment.Add(0.451519685198774);
            strikes.Add(0.701); stdDevsOnExpiry.Add(0.450229281866473); stdDevsOnPayment.Add(0.45174003038252);
            strikes.Add(0.702); stdDevsOnExpiry.Add(0.450472777246306); stdDevsOnPayment.Add(0.451979817788361);
            strikes.Add(0.703); stdDevsOnExpiry.Add(0.45073208401444); stdDevsOnPayment.Add(0.452222845564551);
            strikes.Add(0.704); stdDevsOnExpiry.Add(0.450940794340011); stdDevsOnPayment.Add(0.452436710007599);
            strikes.Add(0.705); stdDevsOnExpiry.Add(0.451162153776223); stdDevsOnPayment.Add(0.452653814820995);
            strikes.Add(0.706); stdDevsOnExpiry.Add(0.451374026379454); stdDevsOnPayment.Add(0.452848237041948);
            strikes.Add(0.707); stdDevsOnExpiry.Add(0.451570087594384); stdDevsOnPayment.Add(0.453094505188487);
            strikes.Add(0.708); stdDevsOnExpiry.Add(0.451813582974217); stdDevsOnPayment.Add(0.453292167779789);
            strikes.Add(0.709); stdDevsOnExpiry.Add(0.452028617855109); stdDevsOnPayment.Add(0.453525474444931);
            strikes.Add(0.71); stdDevsOnExpiry.Add(0.45223416590302); stdDevsOnPayment.Add(0.453729617776931);
            strikes.Add(0.711); stdDevsOnExpiry.Add(0.45243338939561); stdDevsOnPayment.Add(0.453959684071725);
            strikes.Add(0.712); stdDevsOnExpiry.Add(0.452635775165861); stdDevsOnPayment.Add(0.454212432958962);
            strikes.Add(0.713); stdDevsOnExpiry.Add(0.452869783712714); stdDevsOnPayment.Add(0.454390653328169);
            strikes.Add(0.714); stdDevsOnExpiry.Add(0.453056358094664); stdDevsOnPayment.Add(0.454617479252613);
            strikes.Add(0.715); stdDevsOnExpiry.Add(0.453274555253215); stdDevsOnPayment.Add(0.454837824436359);
            strikes.Add(0.716); stdDevsOnExpiry.Add(0.453578133908591); stdDevsOnPayment.Add(0.455019285175914);
            strikes.Add(0.717); stdDevsOnExpiry.Add(0.453761546012881); stdDevsOnPayment.Add(0.455252591841057);
            strikes.Add(0.718); stdDevsOnExpiry.Add(0.453970256338452); stdDevsOnPayment.Add(0.455479417765501);
            strikes.Add(0.719); stdDevsOnExpiry.Add(0.454156830720402); stdDevsOnPayment.Add(0.455657638134707);
            strikes.Add(0.72); stdDevsOnExpiry.Add(0.454346567380012); stdDevsOnPayment.Add(0.45589094479985);
            strikes.Add(0.721); stdDevsOnExpiry.Add(0.454590062759845); stdDevsOnPayment.Add(0.456098328502199);
            strikes.Add(0.722); stdDevsOnExpiry.Add(0.454795610807756); stdDevsOnPayment.Add(0.45634135627839);
            strikes.Add(0.723); stdDevsOnExpiry.Add(0.455032781632269); stdDevsOnPayment.Add(0.45654225924004);
            strikes.Add(0.724); stdDevsOnExpiry.Add(0.455257303346141); stdDevsOnPayment.Add(0.45674640257204);
            strikes.Add(0.725); stdDevsOnExpiry.Add(0.455469175949372); stdDevsOnPayment.Add(0.456960267015087);
            strikes.Add(0.726); stdDevsOnExpiry.Add(0.455684210830264); stdDevsOnPayment.Add(0.457200054420929);
            strikes.Add(0.727); stdDevsOnExpiry.Add(0.455908732544135); stdDevsOnPayment.Add(0.457378274790135);
            strikes.Add(0.728); stdDevsOnExpiry.Add(0.456054197316503); stdDevsOnPayment.Add(0.457621302566325);
            strikes.Add(0.729); stdDevsOnExpiry.Add(0.456281881308035); stdDevsOnPayment.Add(0.457835167009372);
            strikes.Add(0.73); stdDevsOnExpiry.Add(0.456525376687868); stdDevsOnPayment.Add(0.458039310341372);
            strikes.Add(0.731); stdDevsOnExpiry.Add(0.456683490570877); stdDevsOnPayment.Add(0.458233732562325);
            strikes.Add(0.732); stdDevsOnExpiry.Add(0.45693647278369); stdDevsOnPayment.Add(0.458467039227467);
            strikes.Add(0.733); stdDevsOnExpiry.Add(0.45712937172096); stdDevsOnPayment.Add(0.458671182559467);
            strikes.Add(0.734); stdDevsOnExpiry.Add(0.457322270658231); stdDevsOnPayment.Add(0.458907729594959);
            strikes.Add(0.735); stdDevsOnExpiry.Add(0.457521494150821); stdDevsOnPayment.Add(0.459079469223467);
            strikes.Add(0.736); stdDevsOnExpiry.Add(0.457736529031713); stdDevsOnPayment.Add(0.459299814407213);
            strikes.Add(0.737); stdDevsOnExpiry.Add(0.457970537578565); stdDevsOnPayment.Add(0.459481275146768);
            strikes.Add(0.738); stdDevsOnExpiry.Add(0.458141300572214); stdDevsOnPayment.Add(0.459701620330514);
            strikes.Add(0.739); stdDevsOnExpiry.Add(0.458406931895669); stdDevsOnPayment.Add(0.459931686625307);
            strikes.Add(0.74); stdDevsOnExpiry.Add(0.458587181722298); stdDevsOnPayment.Add(0.460116387735212);
            strikes.Add(0.741); stdDevsOnExpiry.Add(0.458846488490432); stdDevsOnPayment.Add(0.460317290696863);
            strikes.Add(0.742); stdDevsOnExpiry.Add(0.459045711983022); stdDevsOnPayment.Add(0.460534395510259);
            strikes.Add(0.743); stdDevsOnExpiry.Add(0.459216474976672); stdDevsOnPayment.Add(0.460757981064355);
            strikes.Add(0.744); stdDevsOnExpiry.Add(0.459412536191602); stdDevsOnPayment.Add(0.46098156661845);
            strikes.Add(0.745); stdDevsOnExpiry.Add(0.459656031571435); stdDevsOnPayment.Add(0.461156546617307);
            strikes.Add(0.746); stdDevsOnExpiry.Add(0.459829956842744); stdDevsOnPayment.Add(0.461370411060354);
            strikes.Add(0.747); stdDevsOnExpiry.Add(0.460054478556616); stdDevsOnPayment.Add(0.461545391059211);
            strikes.Add(0.748); stdDevsOnExpiry.Add(0.460244215216226); stdDevsOnPayment.Add(0.46179165920575);
            strikes.Add(0.749); stdDevsOnExpiry.Add(0.460396004543914); stdDevsOnPayment.Add(0.461966639204608);
            strikes.Add(0.75); stdDevsOnExpiry.Add(0.460671122700349); stdDevsOnPayment.Add(0.462212907351147);
            strikes.Add(0.751); stdDevsOnExpiry.Add(0.460857697082299); stdDevsOnPayment.Add(0.462378166238956);
            strikes.Add(0.752); stdDevsOnExpiry.Add(0.461028460075948); stdDevsOnPayment.Add(0.462598511422702);
            strikes.Add(0.753); stdDevsOnExpiry.Add(0.461215034457898); stdDevsOnPayment.Add(0.462792933643655);
            strikes.Add(0.754); stdDevsOnExpiry.Add(0.461458529837731); stdDevsOnPayment.Add(0.4630132788274);
            strikes.Add(0.755); stdDevsOnExpiry.Add(0.461657753330322); stdDevsOnPayment.Add(0.4632174221594);
            strikes.Add(0.756); stdDevsOnExpiry.Add(0.461869625933553); stdDevsOnPayment.Add(0.463411844380352);
            strikes.Add(0.757); stdDevsOnExpiry.Add(0.462122608146366); stdDevsOnPayment.Add(0.463632189564098);
            strikes.Add(0.758); stdDevsOnExpiry.Add(0.462264910641074); stdDevsOnPayment.Add(0.463820131044352);
            strikes.Add(0.759); stdDevsOnExpiry.Add(0.462470458688985); stdDevsOnPayment.Add(0.464014553265304);
            strikes.Add(0.76); stdDevsOnExpiry.Add(0.462676006736896); stdDevsOnPayment.Add(0.464254340671146);
            strikes.Add(0.761); stdDevsOnExpiry.Add(0.462910015283748); stdDevsOnPayment.Add(0.464432561040352);
            strikes.Add(0.762); stdDevsOnExpiry.Add(0.463087102832718); stdDevsOnPayment.Add(0.464646425483399);
            strikes.Add(0.763); stdDevsOnExpiry.Add(0.463245216715726); stdDevsOnPayment.Add(0.464850568815399);
            strikes.Add(0.764); stdDevsOnExpiry.Add(0.463482387540239); stdDevsOnPayment.Add(0.464999625851462);
            strikes.Add(0.765); stdDevsOnExpiry.Add(0.46369109786581); stdDevsOnPayment.Add(0.465281538071843);
            strikes.Add(0.766); stdDevsOnExpiry.Add(0.463909295024361); stdDevsOnPayment.Add(0.465443556589303);
            strikes.Add(0.767); stdDevsOnExpiry.Add(0.46407373346269); stdDevsOnPayment.Add(0.465650940291652);
            strikes.Add(0.768); stdDevsOnExpiry.Add(0.464285606065922); stdDevsOnPayment.Add(0.465861564364351);
            strikes.Add(0.769); stdDevsOnExpiry.Add(0.464475342725532); stdDevsOnPayment.Add(0.466036544363208);
            strikes.Add(0.77); stdDevsOnExpiry.Add(0.464668241662802); stdDevsOnPayment.Add(0.466218005102763);
            strikes.Add(0.771); stdDevsOnExpiry.Add(0.464845329211771); stdDevsOnPayment.Add(0.466483715471398);
            strikes.Add(0.772); stdDevsOnExpiry.Add(0.465031903593721); stdDevsOnPayment.Add(0.466632772507461);
            strikes.Add(0.773); stdDevsOnExpiry.Add(0.465256425307593); stdDevsOnPayment.Add(0.466807752506318);
            strikes.Add(0.774); stdDevsOnExpiry.Add(0.465427188301242); stdDevsOnPayment.Add(0.467060501393556);
            strikes.Add(0.775); stdDevsOnExpiry.Add(0.465645385459794); stdDevsOnPayment.Add(0.467229000651715);
            strikes.Add(0.776); stdDevsOnExpiry.Add(0.465806661620462); stdDevsOnPayment.Add(0.467416942131968);
            strikes.Add(0.777); stdDevsOnExpiry.Add(0.466021696501354); stdDevsOnPayment.Add(0.467591922130826);
            strikes.Add(0.778); stdDevsOnExpiry.Add(0.466255705048206); stdDevsOnPayment.Add(0.467815507684921);
            strikes.Add(0.779); stdDevsOnExpiry.Add(0.466489713595059); stdDevsOnPayment.Add(0.467996968424476);
            strikes.Add(0.78); stdDevsOnExpiry.Add(0.466676287977009); stdDevsOnPayment.Add(0.468188150275079);
            strikes.Add(0.781); stdDevsOnExpiry.Add(0.466843888692998); stdDevsOnPayment.Add(0.468395533977428);
            strikes.Add(0.782); stdDevsOnExpiry.Add(0.466992515743026); stdDevsOnPayment.Add(0.468576994716984);
            strikes.Add(0.783); stdDevsOnExpiry.Add(0.467213875179237); stdDevsOnPayment.Add(0.468823262863523);
            strikes.Add(0.784); stdDevsOnExpiry.Add(0.467470019669711); stdDevsOnPayment.Add(0.469017685084475);
            strikes.Add(0.785); stdDevsOnExpiry.Add(0.467656594051661); stdDevsOnPayment.Add(0.469228309157174);
            strikes.Add(0.786); stdDevsOnExpiry.Add(0.467811545657009); stdDevsOnPayment.Add(0.46944541397057);
            strikes.Add(0.787); stdDevsOnExpiry.Add(0.467938036763416); stdDevsOnPayment.Add(0.46956854804384);
            strikes.Add(0.788); stdDevsOnExpiry.Add(0.468172045310268); stdDevsOnPayment.Add(0.469788893227586);
            strikes.Add(0.789); stdDevsOnExpiry.Add(0.468374431080519); stdDevsOnPayment.Add(0.470035161374125);
            strikes.Add(0.79); stdDevsOnExpiry.Add(0.468595790516731); stdDevsOnPayment.Add(0.470200420261935);
            strikes.Add(0.791); stdDevsOnExpiry.Add(0.468801338564642); stdDevsOnPayment.Add(0.470407803964284);
            strikes.Add(0.792); stdDevsOnExpiry.Add(0.468972101558291); stdDevsOnPayment.Add(0.470514736185807);
            strikes.Add(0.793); stdDevsOnExpiry.Add(0.46913970227428); stdDevsOnPayment.Add(0.470770725443395);
            strikes.Add(0.794); stdDevsOnExpiry.Add(0.469307302990269); stdDevsOnPayment.Add(0.470974868775394);
            strikes.Add(0.795); stdDevsOnExpiry.Add(0.469550798370102); stdDevsOnPayment.Add(0.471127166181807);
            strikes.Add(0.796); stdDevsOnExpiry.Add(0.46970891225311); stdDevsOnPayment.Add(0.471337790254505);
            strikes.Add(0.797); stdDevsOnExpiry.Add(0.469952407632943); stdDevsOnPayment.Add(0.47156461617895);
            strikes.Add(0.798); stdDevsOnExpiry.Add(0.470151631125534); stdDevsOnPayment.Add(0.471733115437108);
            strikes.Add(0.799); stdDevsOnExpiry.Add(0.47026231084364); stdDevsOnPayment.Add(0.471937258769108);
            strikes.Add(0.8); stdDevsOnExpiry.Add(0.470499481668152); stdDevsOnPayment.Add(0.47205067173133);
            strikes.Add(0.801); stdDevsOnExpiry.Add(0.470670244661801); stdDevsOnPayment.Add(0.472283978396473);
            strikes.Add(0.802); stdDevsOnExpiry.Add(0.470878954987373); stdDevsOnPayment.Add(0.472442756543584);
            strikes.Add(0.803); stdDevsOnExpiry.Add(0.4710275820374); stdDevsOnPayment.Add(0.472737630245361);
            strikes.Add(0.804); stdDevsOnExpiry.Add(0.471220480974671); stdDevsOnPayment.Add(0.472880206540726);
            strikes.Add(0.805); stdDevsOnExpiry.Add(0.471432353577902); stdDevsOnPayment.Add(0.473081109502377);
            strikes.Add(0.806); stdDevsOnExpiry.Add(0.471615765682192); stdDevsOnPayment.Add(0.473246368390186);
            strikes.Add(0.807); stdDevsOnExpiry.Add(0.471837125118404); stdDevsOnPayment.Add(0.473450511722186);
            strikes.Add(0.808); stdDevsOnExpiry.Add(0.472026861778014); stdDevsOnPayment.Add(0.473625491721043);
            strikes.Add(0.809); stdDevsOnExpiry.Add(0.472184975661022); stdDevsOnPayment.Add(0.4738004717199);
            strikes.Add(0.81); stdDevsOnExpiry.Add(0.472346251821691); stdDevsOnPayment.Add(0.473991653570503);
            strikes.Add(0.811); stdDevsOnExpiry.Add(0.472573935813223); stdDevsOnPayment.Add(0.474179595050757);
            strikes.Add(0.812); stdDevsOnExpiry.Add(0.47271307603027); stdDevsOnPayment.Add(0.474377257642059);
            strikes.Add(0.813); stdDevsOnExpiry.Add(0.472940760021802); stdDevsOnPayment.Add(0.474513353196725);
            strikes.Add(0.814); stdDevsOnExpiry.Add(0.473168444013335); stdDevsOnPayment.Add(0.474730458010122);
            strikes.Add(0.815); stdDevsOnExpiry.Add(0.473310746508042); stdDevsOnPayment.Add(0.474931360971772);
            strikes.Add(0.816); stdDevsOnExpiry.Add(0.473528943666594); stdDevsOnPayment.Add(0.475148465785169);
            strikes.Add(0.817); stdDevsOnExpiry.Add(0.473658597050661); stdDevsOnPayment.Add(0.475359089857867);
            strikes.Add(0.818); stdDevsOnExpiry.Add(0.473857820543251); stdDevsOnPayment.Add(0.475485464301486);
            strikes.Add(0.819); stdDevsOnExpiry.Add(0.474012772148599); stdDevsOnPayment.Add(0.475702569114883);
            strikes.Add(0.82); stdDevsOnExpiry.Add(0.474291052582694); stdDevsOnPayment.Add(0.475906712446883);
            strikes.Add(0.821); stdDevsOnExpiry.Add(0.474436517355062); stdDevsOnPayment.Add(0.476046048371899);
            strikes.Add(0.822); stdDevsOnExpiry.Add(0.474661039068934); stdDevsOnPayment.Add(0.476269633925994);
            strikes.Add(0.823); stdDevsOnExpiry.Add(0.474765394231719); stdDevsOnPayment.Add(0.476399248739962);
            strikes.Add(0.824); stdDevsOnExpiry.Add(0.475027863277513); stdDevsOnPayment.Add(0.476626074664406);
            strikes.Add(0.825); stdDevsOnExpiry.Add(0.475198626271162); stdDevsOnPayment.Add(0.476807535403962);
            strikes.Add(0.826); stdDevsOnExpiry.Add(0.475321955099909); stdDevsOnPayment.Add(0.476953352069676);
            strikes.Add(0.827); stdDevsOnExpiry.Add(0.475552801369101); stdDevsOnPayment.Add(0.477144533920279);
            strikes.Add(0.828); stdDevsOnExpiry.Add(0.475685617030829); stdDevsOnPayment.Add(0.477355157992977);
            strikes.Add(0.829); stdDevsOnExpiry.Add(0.47590065191172); stdDevsOnPayment.Add(0.477539859102882);
            strikes.Add(0.83); stdDevsOnExpiry.Add(0.476074577183029); stdDevsOnPayment.Add(0.477718079472088);
            strikes.Add(0.831); stdDevsOnExpiry.Add(0.476201068289436); stdDevsOnPayment.Add(0.477876857619199);
            strikes.Add(0.832); stdDevsOnExpiry.Add(0.47645721277991); stdDevsOnPayment.Add(0.478097202802945);
            strikes.Add(0.833); stdDevsOnExpiry.Add(0.476593190719297); stdDevsOnPayment.Add(0.478281903912849);
            strikes.Add(0.834); stdDevsOnExpiry.Add(0.476861984320411); stdDevsOnPayment.Add(0.478489287615198);
            strikes.Add(0.835); stdDevsOnExpiry.Add(0.476975826316177); stdDevsOnPayment.Add(0.478589739096024);
            strikes.Add(0.836); stdDevsOnExpiry.Add(0.477203510307709); stdDevsOnPayment.Add(0.478803603539071);
            strikes.Add(0.837); stdDevsOnExpiry.Add(0.477323676858796); stdDevsOnPayment.Add(0.478991545019325);
            strikes.Add(0.838); stdDevsOnExpiry.Add(0.477538711739687); stdDevsOnPayment.Add(0.479166525018182);
            strikes.Add(0.839); stdDevsOnExpiry.Add(0.477722123843977); stdDevsOnPayment.Add(0.479399831683325);
            strikes.Add(0.84); stdDevsOnExpiry.Add(0.477946645557849); stdDevsOnPayment.Add(0.479503523534499);
            strikes.Add(0.841); stdDevsOnExpiry.Add(0.478098434885537); stdDevsOnPayment.Add(0.479688224644404);
            strikes.Add(0.842); stdDevsOnExpiry.Add(0.478291333822807); stdDevsOnPayment.Add(0.479979857975832);
            strikes.Add(0.843); stdDevsOnExpiry.Add(0.478370390764311); stdDevsOnPayment.Add(0.480073828715959);
            strikes.Add(0.844); stdDevsOnExpiry.Add(0.478550640590941); stdDevsOnPayment.Add(0.48027473167761);
            strikes.Add(0.845); stdDevsOnExpiry.Add(0.478797298248434); stdDevsOnPayment.Add(0.480427029084022);
            strikes.Add(0.846); stdDevsOnExpiry.Add(0.478987034908044); stdDevsOnPayment.Add(0.480624691675324);
            strikes.Add(0.847); stdDevsOnExpiry.Add(0.479119850569771); stdDevsOnPayment.Add(0.48080291204453);
            strikes.Add(0.848); stdDevsOnExpiry.Add(0.479338047728323); stdDevsOnPayment.Add(0.48096493056199);
            strikes.Add(0.849); stdDevsOnExpiry.Add(0.479568893997515); stdDevsOnPayment.Add(0.481162593153292);
            strikes.Add(0.85); stdDevsOnExpiry.Add(0.479749143824145); stdDevsOnPayment.Add(0.48137321722599);
            strikes.Add(0.851); stdDevsOnExpiry.Add(0.47985033670927); stdDevsOnPayment.Add(0.481509312780656);
            strikes.Add(0.852); stdDevsOnExpiry.Add(0.480043235646541); stdDevsOnPayment.Add(0.481719936853355);
            strikes.Add(0.853); stdDevsOnExpiry.Add(0.48022348547317); stdDevsOnPayment.Add(0.481875474630116);
            strikes.Add(0.854); stdDevsOnExpiry.Add(0.480324678358296); stdDevsOnPayment.Add(0.48210554092491);
            strikes.Add(0.855); stdDevsOnExpiry.Add(0.48060295879239); stdDevsOnPayment.Add(0.48226755944237);
            strikes.Add(0.856); stdDevsOnExpiry.Add(0.480716800788156); stdDevsOnPayment.Add(0.482432818330179);
            strikes.Add(0.857); stdDevsOnExpiry.Add(0.480988756666931); stdDevsOnPayment.Add(0.482607798329037);
            strikes.Add(0.858); stdDevsOnExpiry.Add(0.481096274107377); stdDevsOnPayment.Add(0.482773057216846);
            strikes.Add(0.859); stdDevsOnExpiry.Add(0.481238576602084); stdDevsOnPayment.Add(0.482977200548846);
            strikes.Add(0.86); stdDevsOnExpiry.Add(0.481456773760636); stdDevsOnPayment.Add(0.48316190165875);
            strikes.Add(0.861); stdDevsOnExpiry.Add(0.481573778034062); stdDevsOnPayment.Add(0.483278554991322);
            strikes.Add(0.862); stdDevsOnExpiry.Add(0.481833084802196); stdDevsOnPayment.Add(0.483495659804718);
            strikes.Add(0.863); stdDevsOnExpiry.Add(0.481978549574564); stdDevsOnPayment.Add(0.483647957211131);
            strikes.Add(0.864); stdDevsOnExpiry.Add(0.482136663457572); stdDevsOnPayment.Add(0.483819696839639);
            strikes.Add(0.865); stdDevsOnExpiry.Add(0.482345373783143); stdDevsOnPayment.Add(0.484001157579194);
            strikes.Add(0.866); stdDevsOnExpiry.Add(0.482462378056569); stdDevsOnPayment.Add(0.484208541281543);
            strikes.Add(0.867); stdDevsOnExpiry.Add(0.482705873436402); stdDevsOnPayment.Add(0.484289550540273);
            strikes.Add(0.868); stdDevsOnExpiry.Add(0.482908259206653); stdDevsOnPayment.Add(0.48454878016821);
            strikes.Add(0.869); stdDevsOnExpiry.Add(0.483072697644982); stdDevsOnPayment.Add(0.484769125351956);
            strikes.Add(0.87); stdDevsOnExpiry.Add(0.483180215085428); stdDevsOnPayment.Add(0.484911701647321);
            strikes.Add(0.871); stdDevsOnExpiry.Add(0.483461657797183); stdDevsOnPayment.Add(0.485086681646178);
            strikes.Add(0.872); stdDevsOnExpiry.Add(0.483537552461027); stdDevsOnPayment.Add(0.485219536830495);
            strikes.Add(0.873); stdDevsOnExpiry.Add(0.483692504066375); stdDevsOnPayment.Add(0.485446362754939);
            strikes.Add(0.874); stdDevsOnExpiry.Add(0.483831644283422); stdDevsOnPayment.Add(0.485559775717161);
            strikes.Add(0.875); stdDevsOnExpiry.Add(0.484037192331333); stdDevsOnPayment.Add(0.485760678678812);
            strikes.Add(0.876); stdDevsOnExpiry.Add(0.484242740379244); stdDevsOnPayment.Add(0.485987504603256);
            strikes.Add(0.877); stdDevsOnExpiry.Add(0.484366069207991); stdDevsOnPayment.Add(0.486104157935828);
            strikes.Add(0.878); stdDevsOnExpiry.Add(0.484596915477183); stdDevsOnPayment.Add(0.486282378305034);
            strikes.Add(0.879); stdDevsOnExpiry.Add(0.484707595195289); stdDevsOnPayment.Add(0.486489762007383);
            strikes.Add(0.88); stdDevsOnExpiry.Add(0.484849897689996); stdDevsOnPayment.Add(0.486612896080653);
            strikes.Add(0.881); stdDevsOnExpiry.Add(0.48509655534749); stdDevsOnPayment.Add(0.486758712746367);
            strikes.Add(0.882); stdDevsOnExpiry.Add(0.485254669230498); stdDevsOnPayment.Add(0.486956375337668);
            strikes.Add(0.883); stdDevsOnExpiry.Add(0.485498164610331); stdDevsOnPayment.Add(0.48715403792897);
            strikes.Add(0.884); stdDevsOnExpiry.Add(0.485574059274175); stdDevsOnPayment.Add(0.487290133483636);
            strikes.Add(0.885); stdDevsOnExpiry.Add(0.485691063547601); stdDevsOnPayment.Add(0.487416507927255);
            strikes.Add(0.886); stdDevsOnExpiry.Add(0.485899773873172); stdDevsOnPayment.Add(0.487610930148207);
            strikes.Add(0.887); stdDevsOnExpiry.Add(0.486171729751947); stdDevsOnPayment.Add(0.487782669776715);
            strikes.Add(0.888); stdDevsOnExpiry.Add(0.486307707691334); stdDevsOnPayment.Add(0.488009495701159);
            strikes.Add(0.889); stdDevsOnExpiry.Add(0.486519580294565); stdDevsOnPayment.Add(0.488165033477921);
            strikes.Add(0.89); stdDevsOnExpiry.Add(0.486627097735011); stdDevsOnPayment.Add(0.488314090513985);
            strikes.Add(0.891); stdDevsOnExpiry.Add(0.486769400229719); stdDevsOnPayment.Add(0.488476109031445);
            strikes.Add(0.892); stdDevsOnExpiry.Add(0.486943325501028); stdDevsOnPayment.Add(0.488631646808207);
            strikes.Add(0.893); stdDevsOnExpiry.Add(0.487107763939357); stdDevsOnPayment.Add(0.488829309399508);
            strikes.Add(0.894); stdDevsOnExpiry.Add(0.487297500598967); stdDevsOnPayment.Add(0.488962164583825);
            strikes.Add(0.895); stdDevsOnExpiry.Add(0.487420829427713); stdDevsOnPayment.Add(0.489172788656524);
            strikes.Add(0.896); stdDevsOnExpiry.Add(0.487642188863925); stdDevsOnPayment.Add(0.489318605322238);
            strikes.Add(0.897); stdDevsOnExpiry.Add(0.487809789579914); stdDevsOnPayment.Add(0.489493585321095);
            strikes.Add(0.898); stdDevsOnExpiry.Add(0.487952092074622); stdDevsOnPayment.Add(0.489655603838555);
            strikes.Add(0.899); stdDevsOnExpiry.Add(0.48810388140231); stdDevsOnPayment.Add(0.48987918939265);
            strikes.Add(0.9); stdDevsOnExpiry.Add(0.488334727671502); stdDevsOnPayment.Add(0.49000232346592);
            strikes.Add(0.901); stdDevsOnExpiry.Add(0.48847703016621); stdDevsOnPayment.Add(0.490232389760713);
            strikes.Add(0.902); stdDevsOnExpiry.Add(0.488660442270499); stdDevsOnPayment.Add(0.490349043093285);
            strikes.Add(0.903); stdDevsOnExpiry.Add(0.488847016652449); stdDevsOnPayment.Add(0.490475417536904);
            strikes.Add(0.904); stdDevsOnExpiry.Add(0.488938722704594); stdDevsOnPayment.Add(0.490699003090999);
            strikes.Add(0.905); stdDevsOnExpiry.Add(0.48904624014504); stdDevsOnPayment.Add(0.490838339016015);
            strikes.Add(0.906); stdDevsOnExpiry.Add(0.489264437303591); stdDevsOnPayment.Add(0.491026280496268);
            strikes.Add(0.907); stdDevsOnExpiry.Add(0.489409902075959); stdDevsOnPayment.Add(0.491175337532332);
            strikes.Add(0.908); stdDevsOnExpiry.Add(0.489647072900472); stdDevsOnPayment.Add(0.491392442345728);
            strikes.Add(0.909); stdDevsOnExpiry.Add(0.489760914896238); stdDevsOnPayment.Add(0.491515576418998);
            strikes.Add(0.91); stdDevsOnExpiry.Add(0.48999492344309); stdDevsOnPayment.Add(0.491658152714363);
            strikes.Add(0.911); stdDevsOnExpiry.Add(0.49018149782504); stdDevsOnPayment.Add(0.491872017157411);
            strikes.Add(0.912); stdDevsOnExpiry.Add(0.490190984658021); stdDevsOnPayment.Add(0.491975709008585);
            strikes.Add(0.913); stdDevsOnExpiry.Add(0.490466102814455); stdDevsOnPayment.Add(0.492170131229537);
            strikes.Add(0.914); stdDevsOnExpiry.Add(0.490611567586823); stdDevsOnPayment.Add(0.49236131308014);
            strikes.Add(0.915); stdDevsOnExpiry.Add(0.490785492858132); stdDevsOnPayment.Add(0.492529812338299);
            strikes.Add(0.916); stdDevsOnExpiry.Add(0.490908821686879); stdDevsOnPayment.Add(0.492701551966807);
            strikes.Add(0.917); stdDevsOnExpiry.Add(0.491032150515626); stdDevsOnPayment.Add(0.492831166780775);
            strikes.Add(0.918); stdDevsOnExpiry.Add(0.491244023118857); stdDevsOnPayment.Add(0.492983464187188);
            strikes.Add(0.919); stdDevsOnExpiry.Add(0.491373676502924); stdDevsOnPayment.Add(0.493194088259886);
            strikes.Add(0.92); stdDevsOnExpiry.Add(0.491534952663592); stdDevsOnPayment.Add(0.493245934185473);
            strikes.Add(0.921); stdDevsOnExpiry.Add(0.491737338433843); stdDevsOnPayment.Add(0.493557009738997);
            strikes.Add(0.922); stdDevsOnExpiry.Add(0.491952373314735); stdDevsOnPayment.Add(0.493641259368076);
            strikes.Add(0.923); stdDevsOnExpiry.Add(0.49205672847752); stdDevsOnPayment.Add(0.493845402700076);
            strikes.Add(0.924); stdDevsOnExpiry.Add(0.492227491471169); stdDevsOnPayment.Add(0.493978257884393);
            strikes.Add(0.925); stdDevsOnExpiry.Add(0.492376118521197); stdDevsOnPayment.Add(0.494130555290806);
            strikes.Add(0.926); stdDevsOnExpiry.Add(0.492569017458467); stdDevsOnPayment.Add(0.49431849677106);
            strikes.Add(0.927); stdDevsOnExpiry.Add(0.492682859454234); stdDevsOnPayment.Add(0.494457832696075);
            strikes.Add(0.928); stdDevsOnExpiry.Add(0.492882082946824); stdDevsOnPayment.Add(0.494665216398424);
            strikes.Add(0.929); stdDevsOnExpiry.Add(0.49298643810961); stdDevsOnPayment.Add(0.494749466027504);
            strikes.Add(0.93); stdDevsOnExpiry.Add(0.493214122101142); stdDevsOnPayment.Add(0.494986013062996);
            strikes.Add(0.931); stdDevsOnExpiry.Add(0.493350100040529); stdDevsOnPayment.Add(0.495105906765916);
            strikes.Add(0.932); stdDevsOnExpiry.Add(0.493524025311838); stdDevsOnPayment.Add(0.495329492320011);
            strikes.Add(0.933); stdDevsOnExpiry.Add(0.493713761971448); stdDevsOnPayment.Add(0.495446145652583);
            strikes.Add(0.934); stdDevsOnExpiry.Add(0.493887687242758); stdDevsOnPayment.Add(0.495562798985154);
            strikes.Add(0.935); stdDevsOnExpiry.Add(0.494052125681086); stdDevsOnPayment.Add(0.495763701946804);
            strikes.Add(0.936); stdDevsOnExpiry.Add(0.494143831733231); stdDevsOnPayment.Add(0.495948403056709);
            strikes.Add(0.937); stdDevsOnExpiry.Add(0.494355704336463); stdDevsOnPayment.Add(0.496084498611376);
            strikes.Add(0.938); stdDevsOnExpiry.Add(0.494539116440752); stdDevsOnPayment.Add(0.496282161202677);
            strikes.Add(0.939); stdDevsOnExpiry.Add(0.494615011104596); stdDevsOnPayment.Add(0.496437698979439);
            strikes.Add(0.94); stdDevsOnExpiry.Add(0.494782611820585); stdDevsOnPayment.Add(0.496599717496899);
            strikes.Add(0.941); stdDevsOnExpiry.Add(0.494886966983371); stdDevsOnPayment.Add(0.496790899347502);
            strikes.Add(0.942); stdDevsOnExpiry.Add(0.495174734250446); stdDevsOnPayment.Add(0.496878389346931);
            strikes.Add(0.943); stdDevsOnExpiry.Add(0.495323361300474); stdDevsOnPayment.Add(0.497037167494042);
            strikes.Add(0.944); stdDevsOnExpiry.Add(0.495459339239861); stdDevsOnPayment.Add(0.497176503419057);
            strikes.Add(0.945); stdDevsOnExpiry.Add(0.495519422515405); stdDevsOnPayment.Add(0.497338521936518);
            strikes.Add(0.946); stdDevsOnExpiry.Add(0.495721808285655); stdDevsOnPayment.Add(0.497562107490613);
            strikes.Add(0.947); stdDevsOnExpiry.Add(0.495921031778246); stdDevsOnPayment.Add(0.497701443415628);
            strikes.Add(0.948); stdDevsOnExpiry.Add(0.496066496550614); stdDevsOnPayment.Add(0.497837538970295);
            strikes.Add(0.949); stdDevsOnExpiry.Add(0.496218285878302); stdDevsOnPayment.Add(0.49801899970985);
            strikes.Add(0.95); stdDevsOnExpiry.Add(0.496385886594291); stdDevsOnPayment.Add(0.498219902671501);
            strikes.Add(0.951); stdDevsOnExpiry.Add(0.496525026811338); stdDevsOnPayment.Add(0.498268508226739);
            strikes.Add(0.952); stdDevsOnExpiry.Add(0.496676816139026); stdDevsOnPayment.Add(0.498498574521533);
            strikes.Add(0.953); stdDevsOnExpiry.Add(0.496793820412452); stdDevsOnPayment.Add(0.498628189335501);
            strikes.Add(0.954); stdDevsOnExpiry.Add(0.497049964902926); stdDevsOnPayment.Add(0.498812890445405);
            strikes.Add(0.955); stdDevsOnExpiry.Add(0.497189105119973); stdDevsOnPayment.Add(0.499013793407056);
            strikes.Add(0.956); stdDevsOnExpiry.Add(0.497347219002982); stdDevsOnPayment.Add(0.499172571554167);
            strikes.Add(0.957); stdDevsOnExpiry.Add(0.497476872387049); stdDevsOnPayment.Add(0.49924385970185);
            strikes.Add(0.958); stdDevsOnExpiry.Add(0.4976792581573); stdDevsOnPayment.Add(0.499503089329786);
            strikes.Add(0.959); stdDevsOnExpiry.Add(0.497770964209444); stdDevsOnPayment.Add(0.499593819699564);
            strikes.Add(0.96); stdDevsOnExpiry.Add(0.497998648200977); stdDevsOnPayment.Add(0.499726674883881);
            strikes.Add(0.961); stdDevsOnExpiry.Add(0.498042920088219); stdDevsOnPayment.Add(0.499937298956579);
            strikes.Add(0.962); stdDevsOnExpiry.Add(0.498204196248888); stdDevsOnPayment.Add(0.500031269696706);
            strikes.Add(0.963); stdDevsOnExpiry.Add(0.49842871796276); stdDevsOnPayment.Add(0.500203009325214);
            strikes.Add(0.964); stdDevsOnExpiry.Add(0.498517261737244); stdDevsOnPayment.Add(0.500345585620579);
            strikes.Add(0.965); stdDevsOnExpiry.Add(0.498672213342592); stdDevsOnPayment.Add(0.500436315990357);
            strikes.Add(0.966); stdDevsOnExpiry.Add(0.49881767811496); stdDevsOnPayment.Add(0.500689064877594);
            strikes.Add(0.967); stdDevsOnExpiry.Add(0.499076984883094); stdDevsOnPayment.Add(0.500792756728769);
            strikes.Add(0.968); stdDevsOnExpiry.Add(0.499200313711841); stdDevsOnPayment.Add(0.501006621171816);
            strikes.Add(0.969); stdDevsOnExpiry.Add(0.499415348592732); stdDevsOnPayment.Add(0.50107466894915);
            strikes.Add(0.97); stdDevsOnExpiry.Add(0.499513379200197); stdDevsOnPayment.Add(0.501343619688134);
            strikes.Add(0.971); stdDevsOnExpiry.Add(0.499665168527885); stdDevsOnPayment.Add(0.501431109687562);
            strikes.Add(0.972); stdDevsOnExpiry.Add(0.499769523690671); stdDevsOnPayment.Add(0.50160284931607);
            strikes.Add(0.973); stdDevsOnExpiry.Add(0.499911826185378); stdDevsOnPayment.Add(0.50172598338934);
            strikes.Add(0.974); stdDevsOnExpiry.Add(0.500098400567328); stdDevsOnPayment.Add(0.501894482647498);
            strikes.Add(0.975); stdDevsOnExpiry.Add(0.500281812671618); stdDevsOnPayment.Add(0.502037058942863);
            strikes.Add(0.976); stdDevsOnExpiry.Add(0.500370356446103); stdDevsOnPayment.Add(0.502263884867308);
            strikes.Add(0.977); stdDevsOnExpiry.Add(0.50050000983017); stdDevsOnPayment.Add(0.502344894126038);
            strikes.Add(0.978); stdDevsOnExpiry.Add(0.500702395600421); stdDevsOnPayment.Add(0.502506912643498);
            strikes.Add(0.979); stdDevsOnExpiry.Add(0.500844698095128); stdDevsOnPayment.Add(0.502568479680133);
            strikes.Add(0.98); stdDevsOnExpiry.Add(0.501091355752621); stdDevsOnPayment.Add(0.502827709308069);
            strikes.Add(0.981); stdDevsOnExpiry.Add(0.501129303084543); stdDevsOnPayment.Add(0.502980006714482);
            strikes.Add(0.982); stdDevsOnExpiry.Add(0.501338013410114); stdDevsOnPayment.Add(0.503096660047053);
            strikes.Add(0.983); stdDevsOnExpiry.Add(0.501540399180365); stdDevsOnPayment.Add(0.503375331897085);
            strikes.Add(0.984); stdDevsOnExpiry.Add(0.501644754343151); stdDevsOnPayment.Add(0.503423937452323);
            strikes.Add(0.985); stdDevsOnExpiry.Add(0.501720649006995); stdDevsOnPayment.Add(0.503621600043624);
            strikes.Add(0.986); stdDevsOnExpiry.Add(0.501954657553847); stdDevsOnPayment.Add(0.503806301153529);
            strikes.Add(0.987); stdDevsOnExpiry.Add(0.502096960048555); stdDevsOnPayment.Add(0.503929435226798);
            strikes.Add(0.988); stdDevsOnExpiry.Add(0.502210802044321); stdDevsOnPayment.Add(0.504078492262862);
            strikes.Add(0.989); stdDevsOnExpiry.Add(0.502406863259251); stdDevsOnPayment.Add(0.504175703373338);
            strikes.Add(0.99); stdDevsOnExpiry.Add(0.502596599918861); stdDevsOnPayment.Add(0.50437012559429);
            strikes.Add(0.991); stdDevsOnExpiry.Add(0.502659845472065); stdDevsOnPayment.Add(0.504447894482671);
            strikes.Add(0.992); stdDevsOnExpiry.Add(0.502811634799753); stdDevsOnPayment.Add(0.504713604851306);
            strikes.Add(0.993); stdDevsOnExpiry.Add(0.502903340851898); stdDevsOnPayment.Add(0.504713604851306);
            strikes.Add(0.994); stdDevsOnExpiry.Add(0.503083590678527); stdDevsOnPayment.Add(0.504924228924004);
            strikes.Add(0.995); stdDevsOnExpiry.Add(0.503304950114739); stdDevsOnPayment.Add(0.505095968552512);
            strikes.Add(0.996); stdDevsOnExpiry.Add(0.503384007056243); stdDevsOnPayment.Add(0.505189939292639);
            strikes.Add(0.997); stdDevsOnExpiry.Add(0.503529471828611); stdDevsOnPayment.Add(0.505390842254289);
            strikes.Add(0.998); stdDevsOnExpiry.Add(0.503712883932901); stdDevsOnPayment.Add(0.505611187438035);
            strikes.Add(0.999); stdDevsOnExpiry.Add(0.503858348705269); stdDevsOnPayment.Add(0.505695437067115);
            strikes.Add(1); stdDevsOnExpiry.Add(0.504029111698918); stdDevsOnPayment.Add(0.505818571140384);
            strikes.Add(1.001); stdDevsOnExpiry.Add(0.504127142306383); stdDevsOnPayment.Add(0.505964387806099);
            strikes.Add(1.002); stdDevsOnExpiry.Add(0.504301067577692); stdDevsOnPayment.Add(0.506139367804955);

            //Create smiles on Expiry Date
            smilesOnExpiry = new List<SmileSection>();
            smilesOnExpiry.Add(new FlatSmileSection(startDate, flatVol, rangeCouponDayCount));
            double dummyAtmLevel = 0;
            smilesOnExpiry.Add(new InterpolatedSmileSection<Linear>(startDate,
                                                                    strikes, stdDevsOnExpiry, dummyAtmLevel, rangeCouponDayCount));
            //Create smiles on Payment Date
            smilesOnPayment = new List<SmileSection>();
            smilesOnPayment.Add(new FlatSmileSection(endDate, flatVol, rangeCouponDayCount));
            smilesOnPayment.Add(new InterpolatedSmileSection<Linear>(endDate,
                                                                     strikes, stdDevsOnPayment, dummyAtmLevel, rangeCouponDayCount, new Linear()));

            Utils.QL_REQUIRE(smilesOnExpiry.Count == smilesOnPayment.Count, () =>
                             "smilesOnExpiry.size()!=smilesOnPayment.size()");
         }

         public CommonVars()
         {

            //General Settings
            calendar = new TARGET();
            today = new Date(39147); // 6 Mar 2007
            Settings.setEvaluationDate(today);
            settlement = today;
            //create Yield Curve
            createYieldCurve();
            referenceDate = termStructure.link.referenceDate();
            // Ibor index
            iborIndex = new Euribor6M(termStructure);

            // create Volatility Structures
            flatVol = 0.1;
            createVolatilityStructures();

            // Range Accrual valuation
            gearing = 1.0;
            spread = 0.0;
            infiniteLowerStrike = 1.0e-9;
            infiniteUpperStrike = 1.0;
            correlation = 1.0;

            startDate = new Date(42800); //6 Mar 2017
            endDate = new Date(42984);   //6 Sep 2017
            paymentDate = endDate;   //6 Sep 2017
            fixingDays = 2;
            rangeCouponDayCount = iborIndex.dayCounter();

            // observations schedule
            observationsConvention = BusinessDayConvention.ModifiedFollowing;
            observationsFrequency = Frequency.Daily;
            observationSchedule = new Schedule(startDate, endDate,
                                               new Period(observationsFrequency), calendar, observationsConvention, observationsConvention,
                                               DateGeneration.Rule.Forward, false);
            // Range accrual pricers properties
            byCallSpread = new List<bool>();
            byCallSpread.Add(true);
            byCallSpread.Add(false);

            //Create smiles sections
            createSmileSections();

            //test parameters
            rateTolerance = 2.0e-8;
            priceTolerance = 2.0e-4;
         }
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testInfiniteRange()
      {
         // Testing infinite range accrual floaters
         CommonVars vars = new CommonVars();

         //Coupon
         RangeAccrualFloatersCoupon coupon = new RangeAccrualFloatersCoupon(vars.paymentDate,
                                                                            1.0,
                                                                            vars.iborIndex,
                                                                            vars.startDate,
                                                                            vars.endDate,
                                                                            vars.fixingDays,
                                                                            vars.rangeCouponDayCount,
                                                                            vars.gearing, vars.spread,
                                                                            vars.startDate, vars.endDate,
                                                                            vars.observationSchedule,
                                                                            vars.infiniteLowerStrike,
                                                                            vars.infiniteUpperStrike);
         Date fixingDate = coupon.fixingDate();

         for (int z = 0; z < vars.smilesOnPayment.Count; z++)
         {
            for (int i = 0; i < vars.byCallSpread.Count; i++)
            {
               RangeAccrualPricer bgmPricer = new RangeAccrualPricerByBgm(vars.correlation,
                                                                          vars.smilesOnExpiry[z], vars.smilesOnPayment[z],
                                                                          true, vars.byCallSpread[i]);

               coupon.setPricer(bgmPricer);

               //Computation
               double rate = coupon.rate();
               double indexfixing = vars.iborIndex.fixing(fixingDate);
               double difference = rate - indexfixing;

               if (Math.Abs(difference) > vars.rateTolerance)
               {
                  QAssert.Fail("\n" +
                               "i:\t" + i + "\n" +
                               "fixingDate:\t" + fixingDate + "\n" +
                               "startDate:\t" + vars.startDate + "\n" +
                               "range accrual rate:\t" + rate + "\n" +
                               "index fixing:\t" + indexfixing + "\n" +
                               "difference:\t" + difference + "\n" +
                               "tolerance: \t" + vars.rateTolerance);
               }

            }
         }
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testPriceMonotonicityWithRespectToLowerStrike()
      {
         // Testing price monotonicity with respect to the lower strike
         CommonVars vars = new CommonVars();

         for (int z = 0; z < vars.smilesOnPayment.Count; z++)
         {
            for (int i = 0; i < vars.byCallSpread.Count; i++)
            {
               RangeAccrualPricer bgmPricer = new RangeAccrualPricerByBgm(vars.correlation,
                                                                          vars.smilesOnExpiry[z],
                                                                          vars.smilesOnPayment[z],
                                                                          true,
                                                                          vars.byCallSpread[i]);

               double effectiveLowerStrike;
               double previousPrice = 100.0;

               for (int k = 1; k < 100; k++)
               {
                  effectiveLowerStrike = 0.005 + k * 0.001;
                  RangeAccrualFloatersCoupon coupon = new RangeAccrualFloatersCoupon(
                     vars.paymentDate,
                     1.0,
                     vars.iborIndex,
                     vars.startDate,
                     vars.endDate,
                     vars.fixingDays,
                     vars.rangeCouponDayCount,
                     vars.gearing, vars.spread,
                     vars.startDate, vars.endDate,
                     vars.observationSchedule,
                     effectiveLowerStrike,
                     vars.infiniteUpperStrike);

                  coupon.setPricer(bgmPricer);

                  //Computation
                  double price = coupon.price(vars.termStructure);

                  if (previousPrice <= price)
                  {
                     QAssert.Fail("\n" +
                                  "i:\t" + i + "\n" +
                                  "k:\t" + k + "\n" +
                                  "Price at lower strike\t" + (effectiveLowerStrike - 0.001) +
                                  ": \t" + previousPrice + "\n" +
                                  "Price at lower strike\t" + effectiveLowerStrike +
                                  ": \t" + price + "\n");
                  }

                  previousPrice = price;
               }
            }
         }
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testPriceMonotonicityWithRespectToUpperStrike()
      {

         // Testing price monotonicity with respect to the upper strike

         CommonVars vars = new CommonVars();

         for (int z = 0; z < vars.smilesOnPayment.Count; z++)
         {
            for (int i = 0; i < vars.byCallSpread.Count; i++)
            {
               RangeAccrualPricer bgmPricer = new RangeAccrualPricerByBgm(vars.correlation,
                                                                          vars.smilesOnExpiry[z],
                                                                          vars.smilesOnPayment[z],
                                                                          true,
                                                                          vars.byCallSpread[i]);

               double effectiveUpperStrike;
               double previousPrice = 0.0;

               for (int k = 1; k < 95; k++)
               {
                  effectiveUpperStrike = 0.006 + k * 0.001;
                  RangeAccrualFloatersCoupon coupon = new RangeAccrualFloatersCoupon(
                     vars.paymentDate,
                     1.0,
                     vars.iborIndex,
                     vars.startDate,
                     vars.endDate,
                     vars.fixingDays,
                     vars.rangeCouponDayCount,
                     vars.gearing, vars.spread,
                     vars.startDate, vars.endDate,
                     vars.observationSchedule,
                     .004,
                     effectiveUpperStrike);

                  coupon.setPricer(bgmPricer);

                  //Computation
                  double price = coupon.price(vars.termStructure);

                  if (previousPrice > price)
                  {
                     Assert.Fail("\n" +
                                 "i:\t" + i + "\n" +
                                 "k:\t" + k + "\n" +
                                 "Price at upper strike\t" + (effectiveUpperStrike - 0.001) +
                                 ": \t" + previousPrice + "\n" +
                                 "Price at upper strike\t" + effectiveUpperStrike +
                                 ": \t" + price + "\n");
                  }
                  previousPrice = price;
               }
            }
         }
      }
   }
}
