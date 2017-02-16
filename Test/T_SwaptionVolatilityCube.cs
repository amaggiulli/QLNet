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
#if QL_DOTNET_FRAMEWORK
using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
   using Xunit;
#endif
using QLNet;

namespace TestSuite
{
#if QL_DOTNET_FRAMEWORK
   [TestClass()]
#endif
   public class T_SwaptionVolatilityCube
   {
      public class CommonVars 
      {
         // global data
         public SwaptionMarketConventions conventions = new SwaptionMarketConventions();
         public AtmVolatility atm = new AtmVolatility();
         public RelinkableHandle<SwaptionVolatilityStructure> atmVolMatrix;
         public VolatilityCube cube = new VolatilityCube();
         public RelinkableHandle<YieldTermStructure> termStructure = new RelinkableHandle<YieldTermStructure>();
         public SwapIndex swapIndexBase, shortSwapIndexBase;
         public bool vegaWeighedSmileFit;

         // cleanup
        //public SavedSettings backup = new SavedSettings();

         // utilities
         public void makeAtmVolTest( SwaptionVolatilityCube volCube, double tolerance ) 
         {
            for (int i=0; i<atm.tenors.options.Count; i++) 
            {
               for (int j=0; j<atm.tenors.swaps.Count; j++) 
               {
                  double strike = volCube.atmStrike(atm.tenors.options[i], atm.tenors.swaps[j]);
                  double expVol = atmVolMatrix.link.volatility(atm.tenors.options[i],atm.tenors.swaps[j],strike, true);
                  double actVol = volCube.volatility(atm.tenors.options[i], atm.tenors.swaps[j], strike, true);
                  double error = Math.Abs(expVol-actVol);
                  if (error>tolerance)
                     QAssert.Fail("recovery of atm vols failed:" +
                                 "\nexpiry time = " + atm.tenors.options[i] +
                                 "\nswap length = " + atm.tenors.swaps[j] +
                                 "\n atm strike = " + strike +
                                 "\n   exp. vol = " + expVol +
                                 "\n actual vol = " + actVol +
                                 "\n      error = " + error +
                                 "\n  tolerance = " + tolerance);
               }
            }
         }
         public void makeVolSpreadsTest(SwaptionVolatilityCube volCube,double tolerance) 
         {
            for (int i=0; i<cube.tenors.options.Count; i++) 
            {
               for (int j=0; j<cube.tenors.swaps.Count; j++) 
               {
                  for (int k=0; k<cube.strikeSpreads.Count; k++) 
                  {
                     double atmStrike = volCube.atmStrike(cube.tenors.options[i],cube.tenors.swaps[j]);
                     double atmVol = atmVolMatrix.link.volatility(cube.tenors.options[i],cube.tenors.swaps[j],
                        atmStrike, true);
                     double vol = volCube.volatility(cube.tenors.options[i],cube.tenors.swaps[j],
                        atmStrike+cube.strikeSpreads[k], true);
                     double spread = vol-atmVol;
                     double expVolSpread = cube.volSpreads[i*cube.tenors.swaps.Count+j,k];
                     double error = Math.Abs(expVolSpread-spread);
                     if (error>tolerance)
                        QAssert.Fail("\nrecovery of smile vol spreads failed:" +
                                 "\n    option tenor = " + cube.tenors.options[i] +
                                 "\n      swap tenor = " + cube.tenors.swaps[j] +
                                 "\n      atm strike = " + atmStrike +
                                 "\n   strike spread = " + cube.strikeSpreads[k] +
                                 "\n         atm vol = " + atmVol +
                                 "\n      smiled vol = " + vol +
                                 "\n      vol spread = " + spread +
                                 "\n exp. vol spread = " + expVolSpread +
                                 "\n           error = " + error +
                                 "\n       tolerance = " + tolerance);
                  }
               }
            }
         }

         public CommonVars() 
         {
            Settings.setEvaluationDate(new Date(16, Month.September, 2015));
            conventions.setConventions();

            // ATM swaptionvolmatrix
            atm.setMarketData();

            atmVolMatrix = new RelinkableHandle<SwaptionVolatilityStructure>(
               new SwaptionVolatilityMatrix( conventions.calendar,conventions.optionBdc,atm.tenors.options,
                  atm.tenors.swaps,atm.volsHandle,conventions.dayCounter));
            // Swaptionvolcube
            cube.setMarketData();

            termStructure.linkTo(Utilities.flatRate(0.05, new Actual365Fixed()));

            swapIndexBase = new EuriborSwapIsdaFixA(new Period(2,TimeUnit.Years), termStructure);
            shortSwapIndexBase = new EuriborSwapIsdaFixA(new Period(1,TimeUnit.Years), termStructure);

            vegaWeighedSmileFit=false;
         }
      }

      #if QL_DOTNET_FRAMEWORK
         [TestMethod()]
      #else
         [Fact]
      #endif   
      public void testAtmVols() 
      {
         // Testing swaption volatility cube (atm vols)

         CommonVars vars = new CommonVars();

         SwaptionVolCube2 volCube = new SwaptionVolCube2(vars.atmVolMatrix,
                                                         vars.cube.tenors.options,
                                                         vars.cube.tenors.swaps,
                                                         vars.cube.strikeSpreads,
                                                         vars.cube.volSpreadsHandle,
                                                         vars.swapIndexBase,
                                                         vars.shortSwapIndexBase,
                                                         vars.vegaWeighedSmileFit);

         double tolerance = 1.0e-16;
         vars.makeAtmVolTest(volCube, tolerance);
}

      #if QL_DOTNET_FRAMEWORK
         [TestMethod()]
      #else
         [Fact]
      #endif   
      public void testSmile() 
      {
         // Testing swaption volatility cube (smile)
         CommonVars vars = new CommonVars();

         SwaptionVolCube2 volCube = new SwaptionVolCube2(vars.atmVolMatrix,
                                                         vars.cube.tenors.options,
                                                         vars.cube.tenors.swaps,
                                                         vars.cube.strikeSpreads,
                                                         vars.cube.volSpreadsHandle,
                                                         vars.swapIndexBase,
                                                         vars.shortSwapIndexBase,
                                                         vars.vegaWeighedSmileFit);

         double tolerance = 1.0e-16;
         vars.makeVolSpreadsTest(volCube, tolerance);
      }

      #if QL_DOTNET_FRAMEWORK
         [TestMethod()]
      #else
         [Fact]
      #endif   
      public void testSabrVols() 
      {
         // Testing swaption volatility cube (sabr interpolation)
         CommonVars vars = new CommonVars();

         List<List<Handle<Quote> > > parametersGuess = new InitializedList<List<Handle<Quote>>>(
            vars.cube.tenors.options.Count*vars.cube.tenors.swaps.Count);
         for (int i=0; i<vars.cube.tenors.options.Count*vars.cube.tenors.swaps.Count; i++) 
         {
            parametersGuess[i] = new InitializedList<Handle<Quote> >(4);
            parametersGuess[i][0] = new Handle<Quote>(new SimpleQuote(0.2));
            parametersGuess[i][1] = new Handle<Quote>(new SimpleQuote(0.5));
            parametersGuess[i][2] = new Handle<Quote>(new SimpleQuote(0.4));
            parametersGuess[i][3] = new Handle<Quote>(new SimpleQuote(0.0));
          }
          List<bool> isParameterFixed = new InitializedList<bool>(4, false);

          SwaptionVolCube1x volCube = new SwaptionVolCube1x
             ( vars.atmVolMatrix,vars.cube.tenors.options,vars.cube.tenors.swaps,vars.cube.strikeSpreads,
               vars.cube.volSpreadsHandle,vars.swapIndexBase,vars.shortSwapIndexBase,vars.vegaWeighedSmileFit,
               parametersGuess,isParameterFixed,true);
          double tolerance = 3.0e-4;
          vars.makeAtmVolTest(volCube, tolerance);

          tolerance = 12.0e-4;
          vars.makeVolSpreadsTest(volCube, tolerance);
      }

      #if QL_DOTNET_FRAMEWORK
         [TestMethod()]
      #else
         [Fact]
      #endif   
      public void testSpreadedCube() 
      {

         // Testing spreaded swaption volatility cube
         CommonVars vars = new CommonVars();

         List<List<Handle<Quote> > > parametersGuess = 
            new InitializedList<List<Handle<Quote>>>(vars.cube.tenors.options.Count*vars.cube.tenors.swaps.Count);
         for (int i=0; i<vars.cube.tenors.options.Count*vars.cube.tenors.swaps.Count; i++) 
         {
            parametersGuess[i] =  new InitializedList<Handle<Quote>>(4);
            parametersGuess[i][0] = new Handle<Quote>(new SimpleQuote(0.2));
            parametersGuess[i][1] = new Handle<Quote>(new SimpleQuote(0.5));
            parametersGuess[i][2] = new Handle<Quote>(new SimpleQuote(0.4));
            parametersGuess[i][3] = new Handle<Quote>(new SimpleQuote(0.0));
         }
         List<bool> isParameterFixed = new InitializedList<bool>(4, false);

         Handle<SwaptionVolatilityStructure> volCube = new Handle<SwaptionVolatilityStructure>( 
            new SwaptionVolCube1x(vars.atmVolMatrix,
                                  vars.cube.tenors.options,
                                  vars.cube.tenors.swaps,
                                  vars.cube.strikeSpreads,
                                  vars.cube.volSpreadsHandle,
                                  vars.swapIndexBase,
                                  vars.shortSwapIndexBase,
                                  vars.vegaWeighedSmileFit,
                                  parametersGuess,
                                  isParameterFixed,
                                  true));

         SimpleQuote spread  = new SimpleQuote(0.0001);
         Handle<Quote> spreadHandle = new Handle<Quote>(spread);
         SwaptionVolatilityStructure spreadedVolCube = new SpreadedSwaptionVolatility(volCube, spreadHandle);
         List<double> strikes = new List<double>();
         for (int k=1; k<100; k++)
            strikes.Add(k*.01);
         for (int i=0; i<vars.cube.tenors.options.Count; i++) 
         {
            for (int j=0; j<vars.cube.tenors.swaps.Count; j++) 
            {
               SmileSection smileSectionByCube = volCube.link.smileSection(vars.cube.tenors.options[i], 
                  vars.cube.tenors.swaps[j]);
               SmileSection smileSectionBySpreadedCube = spreadedVolCube.smileSection(vars.cube.tenors.options[i], 
                  vars.cube.tenors.swaps[j]);
               for (int k=0; k<strikes.Count; k++) 
               {
                  double strike = strikes[k];
                  double diff = spreadedVolCube.volatility(vars.cube.tenors.options[i], vars.cube.tenors.swaps[j], strike)
                              - volCube.link.volatility(vars.cube.tenors.options[i], vars.cube.tenors.swaps[j], strike);
                  if (Math.Abs(diff-spread.value())>1e-16)
                     QAssert.Fail("\ndiff!=spread in volatility method:" +
                                 "\nexpiry time = " + vars.cube.tenors.options[i] +
                                 "\nswap length = " + vars.cube.tenors.swaps[j] +
                                 "\n atm strike = " + (strike) +
                                 "\ndiff = " + diff +
                                 "\nspread = " + spread.value());

                  diff = smileSectionBySpreadedCube.volatility(strike) - smileSectionByCube.volatility(strike);
                  if (Math.Abs(diff-spread.value())>1e-16)
                     QAssert.Fail("\ndiff!=spread in smile section method:" +
                                 "\nexpiry time = " + vars.cube.tenors.options[i] +
                                 "\nswap length = " + vars.cube.tenors.swaps[j] +
                                 "\n atm strike = " + (strike) +
                                 "\ndiff = " + diff +
                                 "\nspread = " + spread.value());
               }
            }
         }

         //testing observability
         Flag f = new Flag();
         spreadedVolCube.registerWith(f.update);
         volCube.link.update();
         if(!f.isUp())
            QAssert.Fail("SpreadedSwaptionVolatilityStructure does not propagate notifications");
         
         f.lower();
         spread.setValue(.001);
         if(!f.isUp())
            QAssert.Fail("SpreadedSwaptionVolatilityStructure does not propagate notifications");
      }

      #if QL_DOTNET_FRAMEWORK
         [TestMethod()]
      #else
         [Fact]
      #endif  
      public void testObservability() 
      {
         // Testing volatility cube observability
         CommonVars vars = new CommonVars();

         List<List<Handle<Quote> > > parametersGuess = 
            new InitializedList<List<Handle<Quote>>>(vars.cube.tenors.options.Count*vars.cube.tenors.swaps.Count);
         for (int i=0; i<vars.cube.tenors.options.Count*vars.cube.tenors.swaps.Count; i++) 
         {
            parametersGuess[i] = new InitializedList<Handle<Quote>>(4);
            parametersGuess[i][0] = new Handle<Quote>(new SimpleQuote(0.2));
            parametersGuess[i][1] = new Handle<Quote>(new SimpleQuote(0.5));
            parametersGuess[i][2] = new Handle<Quote>(new SimpleQuote(0.4));
            parametersGuess[i][3] = new Handle<Quote>(new SimpleQuote(0.0));
         }
         List<bool> isParameterFixed = new InitializedList<bool>(4, false);

         SwaptionVolCube1x volCube1_0, volCube1_1;
         // VolCube created before change of reference date
         volCube1_0 = new SwaptionVolCube1x(vars.atmVolMatrix,
                                            vars.cube.tenors.options,
                                            vars.cube.tenors.swaps,
                                            vars.cube.strikeSpreads,
                                            vars.cube.volSpreadsHandle,
                                            vars.swapIndexBase,
                                            vars.shortSwapIndexBase,
                                            vars.vegaWeighedSmileFit,
                                            parametersGuess,
                                            isParameterFixed,
                                            true);

         Date referenceDate = Settings.evaluationDate();
         Settings.setEvaluationDate(vars.conventions.calendar.advance(referenceDate, new Period(1, TimeUnit.Days),
            vars.conventions.optionBdc));

         // VolCube created after change of reference date
         volCube1_1 = new SwaptionVolCube1x(vars.atmVolMatrix,
                                            vars.cube.tenors.options,
                                            vars.cube.tenors.swaps,
                                            vars.cube.strikeSpreads,
                                            vars.cube.volSpreadsHandle,
                                            vars.swapIndexBase,
                                            vars.shortSwapIndexBase,
                                            vars.vegaWeighedSmileFit,
                                            parametersGuess,
                                            isParameterFixed,
                                            true);
         double dummyStrike = 0.03;
         for (int i=0;i<vars.cube.tenors.options.Count; i++ ) 
         {
            for (int j=0; j<vars.cube.tenors.swaps.Count; j++) 
            {
               for (int k=0; k<vars.cube.strikeSpreads.Count; k++) 
               {
                  double v0 = volCube1_0.volatility(vars.cube.tenors.options[i],
                                                    vars.cube.tenors.swaps[j],
                                                    dummyStrike + vars.cube.strikeSpreads[k],
                                                    false);
                  double v1 = volCube1_1.volatility(vars.cube.tenors.options[i],
                                                    vars.cube.tenors.swaps[j],
                                                    dummyStrike + vars.cube.strikeSpreads[k],
                                                    false);
                     if (Math.Abs(v0 - v1) > 1e-14)
                     QAssert.Fail(" option tenor = " + vars.cube.tenors.options[i] +
                                    " swap tenor = " + vars.cube.tenors.swaps[j] +
                                    " strike = " + (dummyStrike+vars.cube.strikeSpreads[k])+
                                    "  v0 = " + (v0) +
                                    "  v1 = " + (v1) +
                                    "  error = " + Math.Abs(v1-v0));
               }
            }
         }

         Settings.setEvaluationDate(referenceDate);

         SwaptionVolCube2 volCube2_0, volCube2_1;
         // VolCube created before change of reference date
         volCube2_0 = new SwaptionVolCube2(vars.atmVolMatrix,
                                           vars.cube.tenors.options,
                                           vars.cube.tenors.swaps,
                                           vars.cube.strikeSpreads,
                                           vars.cube.volSpreadsHandle,
                                           vars.swapIndexBase,
                                           vars.shortSwapIndexBase,
                                           vars.vegaWeighedSmileFit);
         Settings.setEvaluationDate(vars.conventions.calendar.advance(referenceDate, new Period(1, TimeUnit.Days),
            vars.conventions.optionBdc));

         // VolCube created after change of reference date
         volCube2_1 = new SwaptionVolCube2(vars.atmVolMatrix,
                                           vars.cube.tenors.options,
                                           vars.cube.tenors.swaps,
                                           vars.cube.strikeSpreads,
                                           vars.cube.volSpreadsHandle,
                                           vars.swapIndexBase,
                                           vars.shortSwapIndexBase,
                                           vars.vegaWeighedSmileFit);

         for (int i=0;i<vars.cube.tenors.options.Count; i++ ) 
         {
            for (int j=0; j<vars.cube.tenors.swaps.Count; j++) 
            {
               for (int k=0; k<vars.cube.strikeSpreads.Count; k++) 
               {
                  double v0 = volCube2_0.volatility(vars.cube.tenors.options[i],
                                                    vars.cube.tenors.swaps[j],
                                                    dummyStrike + vars.cube.strikeSpreads[k],
                                                    false);
                  double v1 = volCube2_1.volatility(vars.cube.tenors.options[i],
                                                    vars.cube.tenors.swaps[j],
                                                    dummyStrike + vars.cube.strikeSpreads[k],
                                                    false);
                  if (Math.Abs(v0 - v1) > 1e-14)
                     QAssert.Fail(" option tenor = " + vars.cube.tenors.options[i] +
                                 " swap tenor = " + vars.cube.tenors.swaps[j] +
                                 " strike = " + (dummyStrike+vars.cube.strikeSpreads[k])+
                                 "  v0 = " + (v0) +
                                 "  v1 = " + (v1) +
                                 "  error = " + Math.Abs(v1-v0));
               }
            }
         }

         Settings.setEvaluationDate(referenceDate);
      }
   }
}
