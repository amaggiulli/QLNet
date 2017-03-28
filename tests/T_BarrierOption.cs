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
   public class T_BarrierOption
   {
      private void REPORT_FAILURE( string greekName,
                                   Barrier.Type barrierType, 
                                   double  barrier,
                                   double rebate,
                                   StrikedTypePayoff payoff,
                                   Exercise exercise,
                                   double s, 
                                   double q, 
                                   double r,
                                   Date today, 
                                   double v, 
                                   double expected, 
                                   double calculated, 
                                   double error, 
                                   double tolerance )
      {
         QAssert.Fail( barrierType + " "
                  + exercise + " "
                  + payoff.optionType() + " option with "
                  + payoff + " payoff:\n"
                  + "    underlying value: " + s + "\n"
                  + "    strike:           " + payoff.strike() + "\n"
                  + "    barrier:          " + barrier + "\n"
                  + "    rebate:           " + rebate + "\n"
                  + "    dividend yield:   " + q + "\n"
                  + "    risk-free rate:   " + r + "\n"
                  + "    reference date:   " + today + "\n"
                  + "    maturity:         " + exercise.lastDate() + "\n"
                  + "    volatility:       " + v + "\n\n"
                  + "    expected " + greekName + ":   " + expected + "\n"
                  + "    calculated " + greekName + ": " + calculated + "\n"
                  + "    error:            " + error + "\n"
                  + "    tolerance:        " + tolerance );
      }

      private void REPORT_FX_FAILURE( string greekName,
                             Barrier.Type barrierType,
                             double barrier,
                             double rebate,
                             StrikedTypePayoff payoff,
                             Exercise exercise,
                             double s,
                             double q,
                             double r,
                             Date today,
                             double vol25Put,
                             double atmVol,
                             double vol25Call,
                             double v,
                             double expected,
                             double calculated,
                             double error,
                             double tolerance )
      {
         QAssert.Fail( barrierType + " "
                  + exercise + " "
                  + payoff.optionType() + " option with "
                  + payoff + " payoff:\n"
                  + "    underlying value: " + s + "\n"
                  + "    strike:           " + payoff.strike() + "\n"
                  + "    barrier:          " + barrier + "\n"
                  + "    rebate:           " + rebate + "\n"
                  + "    dividend yield:   " + q + "\n"
                  + "    risk-free rate:   " + r + "\n"
                  + "    reference date:   " + today + "\n"
                  + "    maturity:         " + exercise.lastDate() + "\n"
                  + "    25PutVol:         " +  vol25Put + "\n" 
                  + "    atmVol:           " + atmVol + "\n" 
                  + "    25CallVol:        " + vol25Call + "\n" 
                  + "    volatility:       " + v + "\n\n"
                  + "    expected " + greekName + ":   " + expected + "\n"
                  + "    calculated " + greekName + ": " + calculated + "\n"
                  + "    error:            " + error + "\n"
                  + "    tolerance:        " + tolerance );
      }

      private struct BarrierOptionData
      {
         public BarrierOptionData(Barrier.Type type_,double volatility_,double strike_,double barrier_,double callValue_,
            double putValue_)
         {
            type=type_;
            volatility = volatility_;
            strike = strike_;
            barrier = barrier_;
            callValue = callValue_;
            putValue = putValue_;
         }

         public Barrier.Type type;
         public double volatility;
         public double strike;
         public double barrier;
         public double callValue;
         public double putValue;
      }

      private struct NewBarrierOptionData
      {
         public NewBarrierOptionData( Barrier.Type barrierType_,double barrier_,double rebate_,Option.Type type_,
            Exercise.Type exType_,double strike_,double s_,double q_,double r_,double t_,double v_,double result_,
            double tol_)
         {
            barrierType = barrierType_;
            barrier = barrier_;
            rebate = rebate_;
            type = type_;
            exType = exType_;
            strike = strike_;
            s = s_;        
            q = q_;        
            r = r_;        
            t = t_;        
            v = v_;        
            result = result_;   
            tol = tol_;      
         }

         public Barrier.Type barrierType;
         public double barrier;
         public double rebate;
         public Option.Type type;
         public Exercise.Type exType;
         public double strike;
         public double s;        // spot
         public double q;        // dividend
         public double r;        // risk-free rate
         public double t;        // time to maturity
         public double v;        // volatility
         public double result;   // result
         public double tol;      // tolerance
      }

      private struct BarrierFxOptionData
      {
         public Barrier.Type barrierType;
         public double barrier;
         public double rebate;
         public Option.Type type;
         public double strike;
         public double s;                 // spot
         public double q;                 // dividend
         public double r;                 // risk-free rate
         public double t;                 // time to maturity
         public double vol25Put;    // 25 delta put vol
         public double volAtm;      // atm vol
         public double vol25Call;   // 25 delta call vol
         public double v;           // volatility at strike
         public double result;            // result
         public double tol;               // tolerance

         public BarrierFxOptionData(Barrier.Type barrierType, double barrier, double rebate, Option.Type type,
            double strike, double s, double q, double r, double t, double vol25Put, double volAtm, double vol25Call,
            double v, double result, double tol) : this()
         {
            this.barrierType = barrierType;
            this.barrier = barrier;
            this.rebate = rebate;
            this.type = type;
            this.strike = strike;
            this.s = s;
            this.q = q;
            this.r = r;
            this.t = t;
            this.vol25Put = vol25Put;
            this.volAtm = volAtm;
            this.vol25Call = vol25Call;
            this.v = v;
            this.result = result;
            this.tol = tol;
         }
      }

      #if QL_DOTNET_FRAMEWORK
              [TestMethod()]
      #else
             [Fact]
      #endif
      public void testHaugValues() 
      {
         // Testing barrier options against Haug's values
         Exercise.Type european = Exercise.Type.European;
         Exercise.Type american = Exercise.Type.American;
         NewBarrierOptionData[] values = {
            /* The data below are from
               "Option pricing formulas", E.G. Haug, McGraw-Hill 1998 pag. 72
            */
            //     barrierType, barrier, rebate,         type, exercise, strk,     s,    q,    r,    t,    v,  result, tol
            new NewBarrierOptionData( Barrier.Type.DownOut,    95.0,    3.0, Option.Type.Call, european,   90, 100.0, 0.04, 0.08, 0.50, 0.25,  9.0246, 1.0e-4),
            new NewBarrierOptionData( Barrier.Type.DownOut,    95.0,    3.0, Option.Type.Call, european,  100, 100.0, 0.04, 0.08, 0.50, 0.25,  6.7924, 1.0e-4),
            new NewBarrierOptionData( Barrier.Type.DownOut,    95.0,    3.0, Option.Type.Call, european,  110, 100.0, 0.04, 0.08, 0.50, 0.25,  4.8759, 1.0e-4),
            new NewBarrierOptionData( Barrier.Type.DownOut,   100.0,    3.0, Option.Type.Call, european,   90, 100.0, 0.04, 0.08, 0.50, 0.25,  3.0000, 1.0e-4),
            new NewBarrierOptionData( Barrier.Type.DownOut,   100.0,    3.0, Option.Type.Call, european,  100, 100.0, 0.04, 0.08, 0.50, 0.25,  3.0000, 1.0e-4),
            new NewBarrierOptionData( Barrier.Type.DownOut,   100.0,    3.0, Option.Type.Call, european,  110, 100.0, 0.04, 0.08, 0.50, 0.25,  3.0000, 1.0e-4),
            new NewBarrierOptionData( Barrier.Type.UpOut,     105.0,    3.0, Option.Type.Call, european,   90, 100.0, 0.04, 0.08, 0.50, 0.25,  2.6789, 1.0e-4),
            new NewBarrierOptionData( Barrier.Type.UpOut,     105.0,    3.0, Option.Type.Call, european,  100, 100.0, 0.04, 0.08, 0.50, 0.25,  2.3580, 1.0e-4),
            new NewBarrierOptionData( Barrier.Type.UpOut,     105.0,    3.0, Option.Type.Call, european,  110, 100.0, 0.04, 0.08, 0.50, 0.25,  2.3453, 1.0e-4),

            new NewBarrierOptionData( Barrier.Type.DownIn,     95.0,    3.0, Option.Type.Call, european,   90, 100.0, 0.04, 0.08, 0.50, 0.25,  7.7627, 1.0e-4),
            new NewBarrierOptionData( Barrier.Type.DownIn,     95.0,    3.0, Option.Type.Call, european,  100, 100.0, 0.04, 0.08, 0.50, 0.25,  4.0109, 1.0e-4),
            new NewBarrierOptionData( Barrier.Type.DownIn,     95.0,    3.0, Option.Type.Call, european,  110, 100.0, 0.04, 0.08, 0.50, 0.25,  2.0576, 1.0e-4),
            new NewBarrierOptionData( Barrier.Type.DownIn,    100.0,    3.0, Option.Type.Call, european,   90, 100.0, 0.04, 0.08, 0.50, 0.25, 13.8333, 1.0e-4),
            new NewBarrierOptionData( Barrier.Type.DownIn,    100.0,    3.0, Option.Type.Call, european,  100, 100.0, 0.04, 0.08, 0.50, 0.25,  7.8494, 1.0e-4),
            new NewBarrierOptionData( Barrier.Type.DownIn,    100.0,    3.0, Option.Type.Call, european,  110, 100.0, 0.04, 0.08, 0.50, 0.25,  3.9795, 1.0e-4),
            new NewBarrierOptionData( Barrier.Type.UpIn,      105.0,    3.0, Option.Type.Call, european,   90, 100.0, 0.04, 0.08, 0.50, 0.25, 14.1112, 1.0e-4),
            new NewBarrierOptionData( Barrier.Type.UpIn,      105.0,    3.0, Option.Type.Call, european,  100, 100.0, 0.04, 0.08, 0.50, 0.25,  8.4482, 1.0e-4),
            new NewBarrierOptionData( Barrier.Type.UpIn,      105.0,    3.0, Option.Type.Call, european,  110, 100.0, 0.04, 0.08, 0.50, 0.25,  4.5910, 1.0e-4),

            new NewBarrierOptionData( Barrier.Type.DownOut,    95.0,    3.0, Option.Type.Call, european,   90, 100.0, 0.04, 0.08, 0.50, 0.30,  8.8334, 1.0e-4),
            new NewBarrierOptionData( Barrier.Type.DownOut,    95.0,    3.0, Option.Type.Call, european,  100, 100.0, 0.04, 0.08, 0.50, 0.30,  7.0285, 1.0e-4),
            new NewBarrierOptionData( Barrier.Type.DownOut,    95.0,    3.0, Option.Type.Call, european,  110, 100.0, 0.04, 0.08, 0.50, 0.30,  5.4137, 1.0e-4),
            new NewBarrierOptionData( Barrier.Type.DownOut,   100.0,    3.0, Option.Type.Call, european,   90, 100.0, 0.04, 0.08, 0.50, 0.30,  3.0000, 1.0e-4),
            new NewBarrierOptionData( Barrier.Type.DownOut,   100.0,    3.0, Option.Type.Call, european,  100, 100.0, 0.04, 0.08, 0.50, 0.30,  3.0000, 1.0e-4),
            new NewBarrierOptionData( Barrier.Type.DownOut,   100.0,    3.0, Option.Type.Call, european,  110, 100.0, 0.04, 0.08, 0.50, 0.30,  3.0000, 1.0e-4),
            new NewBarrierOptionData( Barrier.Type.UpOut,     105.0,    3.0, Option.Type.Call, european,   90, 100.0, 0.04, 0.08, 0.50, 0.30,  2.6341, 1.0e-4),
            new NewBarrierOptionData( Barrier.Type.UpOut,     105.0,    3.0, Option.Type.Call, european,  100, 100.0, 0.04, 0.08, 0.50, 0.30,  2.4389, 1.0e-4),
            new NewBarrierOptionData( Barrier.Type.UpOut,     105.0,    3.0, Option.Type.Call, european,  110, 100.0, 0.04, 0.08, 0.50, 0.30,  2.4315, 1.0e-4),

            new NewBarrierOptionData( Barrier.Type.DownIn,     95.0,    3.0, Option.Type.Call, european,   90, 100.0, 0.04, 0.08, 0.50, 0.30,  9.0093, 1.0e-4),
            new NewBarrierOptionData( Barrier.Type.DownIn,     95.0,    3.0, Option.Type.Call, european,  100, 100.0, 0.04, 0.08, 0.50, 0.30,  5.1370, 1.0e-4),
            new NewBarrierOptionData( Barrier.Type.DownIn,     95.0,    3.0, Option.Type.Call, european,  110, 100.0, 0.04, 0.08, 0.50, 0.30,  2.8517, 1.0e-4),
            new NewBarrierOptionData( Barrier.Type.DownIn,    100.0,    3.0, Option.Type.Call, european,   90, 100.0, 0.04, 0.08, 0.50, 0.30, 14.8816, 1.0e-4),
            new NewBarrierOptionData( Barrier.Type.DownIn,    100.0,    3.0, Option.Type.Call, european,  100, 100.0, 0.04, 0.08, 0.50, 0.30,  9.2045, 1.0e-4),
            new NewBarrierOptionData( Barrier.Type.DownIn,    100.0,    3.0, Option.Type.Call, european,  110, 100.0, 0.04, 0.08, 0.50, 0.30,  5.3043, 1.0e-4),
            new NewBarrierOptionData( Barrier.Type.UpIn,      105.0,    3.0, Option.Type.Call, european,   90, 100.0, 0.04, 0.08, 0.50, 0.30, 15.2098, 1.0e-4),
            new NewBarrierOptionData( Barrier.Type.UpIn,      105.0,    3.0, Option.Type.Call, european,  100, 100.0, 0.04, 0.08, 0.50, 0.30,  9.7278, 1.0e-4),
            new NewBarrierOptionData( Barrier.Type.UpIn,      105.0,    3.0, Option.Type.Call, european,  110, 100.0, 0.04, 0.08, 0.50, 0.30,  5.8350, 1.0e-4),


            //     barrierType, barrier, rebate,         type, exercise, strk,     s,    q,    r,    t,    v,  result, tol
            new NewBarrierOptionData( Barrier.Type.DownOut,    95.0,    3.0,  Option.Type.Put, european,   90, 100.0, 0.04, 0.08, 0.50, 0.25,  2.2798, 1.0e-4 ),
            new NewBarrierOptionData( Barrier.Type.DownOut,    95.0,    3.0,  Option.Type.Put, european,  100, 100.0, 0.04, 0.08, 0.50, 0.25,  2.2947, 1.0e-4 ),
            new NewBarrierOptionData( Barrier.Type.DownOut,    95.0,    3.0,  Option.Type.Put, european,  110, 100.0, 0.04, 0.08, 0.50, 0.25,  2.6252, 1.0e-4 ),
            new NewBarrierOptionData( Barrier.Type.DownOut,   100.0,    3.0,  Option.Type.Put, european,   90, 100.0, 0.04, 0.08, 0.50, 0.25,  3.0000, 1.0e-4 ),
            new NewBarrierOptionData( Barrier.Type.DownOut,   100.0,    3.0,  Option.Type.Put, european,  100, 100.0, 0.04, 0.08, 0.50, 0.25,  3.0000, 1.0e-4 ),
            new NewBarrierOptionData( Barrier.Type.DownOut,   100.0,    3.0,  Option.Type.Put, european,  110, 100.0, 0.04, 0.08, 0.50, 0.25,  3.0000, 1.0e-4 ),
            new NewBarrierOptionData( Barrier.Type.UpOut,     105.0,    3.0,  Option.Type.Put, european,   90, 100.0, 0.04, 0.08, 0.50, 0.25,  3.7760, 1.0e-4 ),
            new NewBarrierOptionData( Barrier.Type.UpOut,     105.0,    3.0,  Option.Type.Put, european,  100, 100.0, 0.04, 0.08, 0.50, 0.25,  5.4932, 1.0e-4 ),
            new NewBarrierOptionData( Barrier.Type.UpOut,     105.0,    3.0,  Option.Type.Put, european,  110, 100.0, 0.04, 0.08, 0.50, 0.25,  7.5187, 1.0e-4 ),

            new NewBarrierOptionData( Barrier.Type.DownIn,     95.0,    3.0,  Option.Type.Put, european,   90, 100.0, 0.04, 0.08, 0.50, 0.25,  2.9586, 1.0e-4 ),
            new NewBarrierOptionData( Barrier.Type.DownIn,     95.0,    3.0,  Option.Type.Put, european,  100, 100.0, 0.04, 0.08, 0.50, 0.25,  6.5677, 1.0e-4 ),
            new NewBarrierOptionData( Barrier.Type.DownIn,     95.0,    3.0,  Option.Type.Put, european,  110, 100.0, 0.04, 0.08, 0.50, 0.25, 11.9752, 1.0e-4 ),
            new NewBarrierOptionData( Barrier.Type.DownIn,    100.0,    3.0,  Option.Type.Put, european,   90, 100.0, 0.04, 0.08, 0.50, 0.25,  2.2845, 1.0e-4 ),
            new NewBarrierOptionData( Barrier.Type.DownIn,    100.0,    3.0,  Option.Type.Put, european,  100, 100.0, 0.04, 0.08, 0.50, 0.25,  5.9085, 1.0e-4 ),
            new NewBarrierOptionData( Barrier.Type.DownIn,    100.0,    3.0,  Option.Type.Put, european,  110, 100.0, 0.04, 0.08, 0.50, 0.25, 11.6465, 1.0e-4 ),
            new NewBarrierOptionData( Barrier.Type.UpIn,      105.0,    3.0,  Option.Type.Put, european,   90, 100.0, 0.04, 0.08, 0.50, 0.25,  1.4653, 1.0e-4 ),
            new NewBarrierOptionData( Barrier.Type.UpIn,      105.0,    3.0,  Option.Type.Put, european,  100, 100.0, 0.04, 0.08, 0.50, 0.25,  3.3721, 1.0e-4 ),
            new NewBarrierOptionData( Barrier.Type.UpIn,      105.0,    3.0,  Option.Type.Put, european,  110, 100.0, 0.04, 0.08, 0.50, 0.25,  7.0846, 1.0e-4 ),

            new NewBarrierOptionData( Barrier.Type.DownOut,    95.0,    3.0,  Option.Type.Put, european,   90, 100.0, 0.04, 0.08, 0.50, 0.30,  2.4170, 1.0e-4 ),
            new NewBarrierOptionData( Barrier.Type.DownOut,    95.0,    3.0,  Option.Type.Put, european,  100, 100.0, 0.04, 0.08, 0.50, 0.30,  2.4258, 1.0e-4 ),
            new NewBarrierOptionData( Barrier.Type.DownOut,    95.0,    3.0,  Option.Type.Put, european,  110, 100.0, 0.04, 0.08, 0.50, 0.30,  2.6246, 1.0e-4 ),
            new NewBarrierOptionData( Barrier.Type.DownOut,   100.0,    3.0,  Option.Type.Put, european,   90, 100.0, 0.04, 0.08, 0.50, 0.30,  3.0000, 1.0e-4 ),
            new NewBarrierOptionData( Barrier.Type.DownOut,   100.0,    3.0,  Option.Type.Put, european,  100, 100.0, 0.04, 0.08, 0.50, 0.30,  3.0000, 1.0e-4 ),
            new NewBarrierOptionData( Barrier.Type.DownOut,   100.0,    3.0,  Option.Type.Put, european,  110, 100.0, 0.04, 0.08, 0.50, 0.30,  3.0000, 1.0e-4 ),
            new NewBarrierOptionData( Barrier.Type.UpOut,     105.0,    3.0,  Option.Type.Put, european,   90, 100.0, 0.04, 0.08, 0.50, 0.30,  4.2293, 1.0e-4 ),
            new NewBarrierOptionData( Barrier.Type.UpOut,     105.0,    3.0,  Option.Type.Put, european,  100, 100.0, 0.04, 0.08, 0.50, 0.30,  5.8032, 1.0e-4 ),
            new NewBarrierOptionData( Barrier.Type.UpOut,     105.0,    3.0,  Option.Type.Put, european,  110, 100.0, 0.04, 0.08, 0.50, 0.30,  7.5649, 1.0e-4 ),

            new NewBarrierOptionData( Barrier.Type.DownIn,     95.0,    3.0,  Option.Type.Put, european,   90, 100.0, 0.04, 0.08, 0.50, 0.30,  3.8769, 1.0e-4 ),
            new NewBarrierOptionData( Barrier.Type.DownIn,     95.0,    3.0,  Option.Type.Put, european,  100, 100.0, 0.04, 0.08, 0.50, 0.30,  7.7989, 1.0e-4 ),
            new NewBarrierOptionData( Barrier.Type.DownIn,     95.0,    3.0,  Option.Type.Put, european,  110, 100.0, 0.04, 0.08, 0.50, 0.30, 13.3078, 1.0e-4 ),
            new NewBarrierOptionData( Barrier.Type.DownIn,    100.0,    3.0,  Option.Type.Put, european,   90, 100.0, 0.04, 0.08, 0.50, 0.30,  3.3328, 1.0e-4 ),
            new NewBarrierOptionData( Barrier.Type.DownIn,    100.0,    3.0,  Option.Type.Put, european,  100, 100.0, 0.04, 0.08, 0.50, 0.30,  7.2636, 1.0e-4 ),
            new NewBarrierOptionData( Barrier.Type.DownIn,    100.0,    3.0,  Option.Type.Put, european,  110, 100.0, 0.04, 0.08, 0.50, 0.30, 12.9713, 1.0e-4 ),
            new NewBarrierOptionData( Barrier.Type.UpIn,      105.0,    3.0,  Option.Type.Put, european,   90, 100.0, 0.04, 0.08, 0.50, 0.30,  2.0658, 1.0e-4 ),
            new NewBarrierOptionData( Barrier.Type.UpIn,      105.0,    3.0,  Option.Type.Put, european,  100, 100.0, 0.04, 0.08, 0.50, 0.30,  4.4226, 1.0e-4 ),
            new NewBarrierOptionData( Barrier.Type.UpIn,      105.0,    3.0,  Option.Type.Put, european,  110, 100.0, 0.04, 0.08, 0.50, 0.30,  8.3686, 1.0e-4 ),

            // Options with american exercise: values computed with 400 steps of Haug's VBA code (handles only out options)
            //     barrierType, barrier, rebate,         type, exercise, strk,     s,    q,    r,    t,    v,  result, tol
            new NewBarrierOptionData( Barrier.Type.DownOut,    95.0,    0.0, Option.Type.Call, american,   90, 100.0, 0.04, 0.08, 0.50, 0.25, 10.4655, 1.0e-4),
            new NewBarrierOptionData( Barrier.Type.DownOut,    95.0,    0.0, Option.Type.Call, american,  100, 100.0, 0.04, 0.08, 0.50, 0.25,  4.5159, 1.0e-4),
            new NewBarrierOptionData( Barrier.Type.DownOut,    95.0,    0.0, Option.Type.Call, american,  110, 100.0, 0.04, 0.08, 0.50, 0.25,  2.5971, 1.0e-4),
            new NewBarrierOptionData( Barrier.Type.DownOut,   100.0,    3.0, Option.Type.Call, american,   90, 100.0, 0.04, 0.08, 0.50, 0.25,  3.0000, 1.0e-4),
            new NewBarrierOptionData( Barrier.Type.DownOut,   100.0,    3.0, Option.Type.Call, american,  100, 100.0, 0.04, 0.08, 0.50, 0.25,  3.0000, 1.0e-4),
            new NewBarrierOptionData( Barrier.Type.DownOut,   100.0,    3.0, Option.Type.Call, american,  110, 100.0, 0.04, 0.08, 0.50, 0.25,  3.0000, 1.0e-4),
            new NewBarrierOptionData( Barrier.Type.UpOut,     105.0,    0.0, Option.Type.Call, american,   90, 100.0, 0.04, 0.08, 0.50, 0.25, 11.8076, 1.0e-4),
            new NewBarrierOptionData( Barrier.Type.UpOut,     105.0,    0.0, Option.Type.Call, american,  100, 100.0, 0.04, 0.08, 0.50, 0.25,  3.3993, 1.0e-4),
            new NewBarrierOptionData( Barrier.Type.UpOut,     105.0,    3.0, Option.Type.Call, american,  110, 100.0, 0.04, 0.08, 0.50, 0.25,  2.3457, 1.0e-4),

            new NewBarrierOptionData( Barrier.Type.DownOut,    95.0,    3.0,  Option.Type.Put, american,   90, 100.0, 0.04, 0.08, 0.50, 0.25,  2.2795, 1.0e-4 ),
            new NewBarrierOptionData( Barrier.Type.DownOut,    95.0,    0.0,  Option.Type.Put, american,  100, 100.0, 0.04, 0.08, 0.50, 0.25,  3.3512, 1.0e-4 ),
            new NewBarrierOptionData( Barrier.Type.DownOut,    95.0,    0.0,  Option.Type.Put, american,  110, 100.0, 0.04, 0.08, 0.50, 0.25, 11.5773, 1.0e-4 ),
            new NewBarrierOptionData( Barrier.Type.DownOut,   100.0,    3.0,  Option.Type.Put, american,   90, 100.0, 0.04, 0.08, 0.50, 0.25,  3.0000, 1.0e-4 ),
            new NewBarrierOptionData( Barrier.Type.DownOut,   100.0,    3.0,  Option.Type.Put, american,  100, 100.0, 0.04, 0.08, 0.50, 0.25,  3.0000, 1.0e-4 ),
            new NewBarrierOptionData( Barrier.Type.DownOut,   100.0,    3.0,  Option.Type.Put, american,  110, 100.0, 0.04, 0.08, 0.50, 0.25,  3.0000, 1.0e-4 ),
            new NewBarrierOptionData( Barrier.Type.UpOut,     105.0,    0.0,  Option.Type.Put, american,   90, 100.0, 0.04, 0.08, 0.50, 0.25,  1.4763, 1.0e-4 ),
            new NewBarrierOptionData( Barrier.Type.UpOut,     105.0,    0.0,  Option.Type.Put, american,  100, 100.0, 0.04, 0.08, 0.50, 0.25,  3.3001, 1.0e-4 ),
            new NewBarrierOptionData( Barrier.Type.UpOut,     105.0,    0.0,  Option.Type.Put, american,  110, 100.0, 0.04, 0.08, 0.50, 0.25, 10.0000, 1.0e-4 ),

            // some american in-options - results (roughly) verified with other numerical methods 
            //     barrierType, barrier, rebate,         type, exercise, strk,     s,    q,    r,    t,    v,  result, tol
            new NewBarrierOptionData( Barrier.Type.DownIn,     95.0,    3.0, Option.Type.Call, american,   90, 100.0, 0.04, 0.08, 0.50, 0.25,  7.7615, 1.0e-4),
            new NewBarrierOptionData( Barrier.Type.DownIn,     95.0,    3.0, Option.Type.Call, american,  100, 100.0, 0.04, 0.08, 0.50, 0.25,  4.0118, 1.0e-4),
            new NewBarrierOptionData( Barrier.Type.DownIn,     95.0,    3.0, Option.Type.Call, american,  110, 100.0, 0.04, 0.08, 0.50, 0.25,  2.0544, 1.0e-4),
            new NewBarrierOptionData( Barrier.Type.DownIn,    100.0,    3.0, Option.Type.Call, american,   90, 100.0, 0.04, 0.08, 0.50, 0.25, 13.8308, 1.0e-4),
            new NewBarrierOptionData( Barrier.Type.UpIn,      105.0,    3.0, Option.Type.Call, american,   90, 100.0, 0.04, 0.08, 0.50, 0.25, 14.1150, 1.0e-4),
            new NewBarrierOptionData( Barrier.Type.UpIn,      105.0,    3.0, Option.Type.Call, american,  110, 100.0, 0.04, 0.08, 0.50, 0.25,  4.5900, 1.0e-4),

            /*
               Data from "Going to Extreme: Correcting Simulation Bias in Exotic
               Option Valuation"
               D.R. Beaglehole, P.H. Dybvig and G. Zhou
               Financial Analysts Journal; Jan / Feb 1997; 53, 1
            */
            //    barrierType, barrier, rebate,         type, strike,     s,    q,    r,    t,    v,  result, tol
            // { Barrier::DownOut,    45.0,    0.0,  Option::Put,     50,  50.0,-0.05, 0.10, 0.25, 0.50,   4.032, 1.0e-3 },
            // { Barrier::DownOut,    45.0,    0.0,  Option::Put,     50,  50.0,-0.05, 0.10, 1.00, 0.50,   5.477, 1.0e-3 }

         };


         DayCounter dc = new Actual360();
         Date today = Date.Today;

         SimpleQuote spot = new SimpleQuote(0.0);
         SimpleQuote qRate = new SimpleQuote(0.0);
         YieldTermStructure qTS = Utilities.flatRate(today, qRate, dc);
         SimpleQuote rRate = new SimpleQuote(0.0);
         YieldTermStructure rTS = Utilities.flatRate(today, rRate, dc);
         SimpleQuote vol = new SimpleQuote(0.0);
         BlackVolTermStructure volTS = Utilities.flatVol(today, vol, dc);

         for (int i=0; i< values.Length; i++) 
         {
            Date exDate = today + Convert.ToInt32(values[i].t*360+0.5);

            spot .setValue(values[i].s);
            qRate.setValue(values[i].q);
            rRate.setValue(values[i].r);
            vol  .setValue(values[i].v);

            StrikedTypePayoff payoff = new PlainVanillaPayoff(values[i].type,values[i].strike);

            BlackScholesMertonProcess stochProcess = new BlackScholesMertonProcess( new Handle<Quote>(spot),
               new Handle<YieldTermStructure>(qTS),new Handle<YieldTermStructure>(rTS),
               new Handle<BlackVolTermStructure>(volTS));

            Exercise exercise;
            if (values[i].exType == Exercise.Type.European)
               exercise = new EuropeanExercise(exDate);
            else
               exercise = new AmericanExercise(exDate);

            BarrierOption barrierOption = new BarrierOption(values[i].barrierType,values[i].barrier,values[i].rebate,
               payoff,exercise);

            IPricingEngine engine;
            double calculated;
            double expected;
            double error;
            if (values[i].exType == Exercise.Type.European) 
            {
               // these engines support only european options
               engine = new AnalyticBarrierEngine(stochProcess);

               barrierOption.setPricingEngine(engine);

               calculated = barrierOption.NPV();
               expected = values[i].result;
               error = Math.Abs(calculated-expected);
               if (error>values[i].tol) {
                  REPORT_FAILURE("value", values[i].barrierType, values[i].barrier,
                                 values[i].rebate, payoff, exercise, values[i].s,
                                 values[i].q, values[i].r, today, values[i].v,
                                 expected, calculated, error, values[i].tol);
               }

               // TODO FdBlackScholesBarrierEngine
               //engine = new FdBlackScholesBarrierEngine(stochProcess, 200, 400);
               //barrierOption.setPricingEngine(engine);

               //calculated = barrierOption.NPV();
               //expected = values[i].result;
               //error = Math.Abs(calculated-expected);
               //if (error>5.0e-3) {
               //   REPORT_FAILURE("fd value", values[i].barrierType, values[i].barrier,
               //                  values[i].rebate, payoff, exercise, values[i].s,
               //                  values[i].q, values[i].r, today, values[i].v,
               //                  expected, calculated, error, values[i].tol);
               //}
            }

            engine = new BinomialBarrierEngine(
               (d, end, steps, strike) => new CoxRossRubinstein(d, end, steps, strike),
               (args, process, grid) => new DiscretizedBarrierOption(args, process, grid), 
               stochProcess, 400 );
            barrierOption.setPricingEngine(engine);

            calculated = barrierOption.NPV();
            expected = values[i].result;
            error = Math.Abs(calculated-expected);
            double tol = 1.1e-2;
            if (error>tol) {
               REPORT_FAILURE("Binomial (Boyle-lau) value", values[i].barrierType, values[i].barrier,
                              values[i].rebate, payoff, exercise, values[i].s,
                              values[i].q, values[i].r, today, values[i].v,
                              expected, calculated, error, tol);
            }

            // Note: here, to test Derman convergence, we force maxTimeSteps to 
            // timeSteps, effectively disabling Boyle-Lau barrier adjustment.
            // Production code should always enable Boyle-Lau. In most cases it
            // gives very good convergence with only a modest timeStep increment.


            engine = new BinomialBarrierEngine(
               ( d, end, steps, strike ) => new CoxRossRubinstein( d, end, steps, strike ),
               ( args, process, grid ) => new DiscretizedDermanKaniBarrierOption( args, process, grid ),
               stochProcess, 400 );
            barrierOption.setPricingEngine( engine );
            calculated = barrierOption.NPV();
            expected = values[i].result;
            error = Math.Abs( calculated - expected );
            tol = 4e-2;
            if ( error > tol )
            {
               REPORT_FAILURE( "Binomial (Derman) value", values[i].barrierType, values[i].barrier,
                              values[i].rebate, payoff, exercise, values[i].s,
                              values[i].q, values[i].r, today, values[i].v,
                              expected, calculated, error, tol );
            }
         }
      }

      #if QL_DOTNET_FRAMEWORK
              [TestMethod()]
      #else
             [Fact]
      #endif
      public void testBabsiriValues() 
      {
         // Testing barrier options against Babsiri's values

         /*
            Data from
            "Simulating Path-Dependent Options: A New Approach"
               - M. El Babsiri and G. Noel
               Journal of Derivatives; Winter 1998; 6, 2
         */
         BarrierOptionData[] values = {
            new BarrierOptionData( Barrier.Type.DownIn, 0.10,   100,       90,   0.07187,  0.0 ),
            new BarrierOptionData( Barrier.Type.DownIn, 0.15,   100,       90,   0.60638,  0.0 ),
            new BarrierOptionData( Barrier.Type.DownIn, 0.20,   100,       90,   1.64005,  0.0 ),
            new BarrierOptionData( Barrier.Type.DownIn, 0.25,   100,       90,   2.98495,  0.0 ),
            new BarrierOptionData( Barrier.Type.DownIn, 0.30,   100,       90,   4.50952,  0.0 ),
            new BarrierOptionData( Barrier.Type.UpIn,   0.10,   100,      110,   4.79148,  0.0 ),
            new BarrierOptionData( Barrier.Type.UpIn,   0.15,   100,      110,   7.08268,  0.0 ),
            new BarrierOptionData( Barrier.Type.UpIn,   0.20,   100,      110,   9.11008,  0.0 ),
            new BarrierOptionData( Barrier.Type.UpIn,   0.25,   100,      110,  11.06148,  0.0 ),
            new BarrierOptionData( Barrier.Type.UpIn,   0.30,   100,      110,  12.98351,  0.0 )
         };

         double underlyingPrice = 100.0;
         double rebate = 0.0;
         double r = 0.05;
         double q = 0.02;

         DayCounter dc = new Actual360();
         Date today = Date.Today;
         SimpleQuote underlying = new SimpleQuote(underlyingPrice);

         SimpleQuote qH_SME = new SimpleQuote(q);
         YieldTermStructure qTS = Utilities.flatRate(today, qH_SME, dc);

         SimpleQuote rH_SME = new SimpleQuote(r);
         YieldTermStructure rTS = Utilities.flatRate(today, rH_SME, dc);

         SimpleQuote volatility = new SimpleQuote(0.10);
         BlackVolTermStructure volTS = Utilities.flatVol(today, volatility, dc);

         Date exDate = today+360;
         Exercise exercise = new EuropeanExercise(exDate);

         for (int i=0; i< values.Length; i++) 
         {
            volatility.setValue(values[i].volatility);

            StrikedTypePayoff callPayoff = new PlainVanillaPayoff(Option.Type.Call,values[i].strike);

            BlackScholesMertonProcess stochProcess = new BlackScholesMertonProcess(
               new Handle<Quote>(underlying),
               new Handle<YieldTermStructure>(qTS),
               new Handle<YieldTermStructure>(rTS),
               new Handle<BlackVolTermStructure>(volTS));


            IPricingEngine engine = new AnalyticBarrierEngine(stochProcess);

            // analytic
            BarrierOption barrierCallOption = new BarrierOption(values[i].type,values[i].barrier,rebate,callPayoff,exercise);
            barrierCallOption.setPricingEngine(engine);
            double calculated = barrierCallOption.NPV();
            double expected = values[i].callValue;
            double error = Math.Abs(calculated-expected);
            double maxErrorAllowed = 1.0e-5;
            if (error>maxErrorAllowed) 
            {
               REPORT_FAILURE("value", values[i].type, values[i].barrier,
                              rebate, callPayoff, exercise, underlyingPrice,
                              q, r, today, values[i].volatility,
                              expected, calculated, error, maxErrorAllowed);
            }

            // TODO MakeMCBarrierEngine
            //double maxMcRelativeErrorAllowed = 2.0e-2;

            //IPricingEngine mcEngine =
            //   MakeMCBarrierEngine<LowDiscrepancy>(stochProcess)
            //   .withStepsPerYear(1)
            //   .withBrownianBridge()
            //   .withSamples(131071) // 2^17-1
            //   .withMaxSamples(1048575) // 2^20-1
            //   .withSeed(5);

            //barrierCallOption.setPricingEngine(mcEngine);
            //calculated = barrierCallOption.NPV();
            //error = std::fabs(calculated-expected)/expected;
            //if (error>maxMcRelativeErrorAllowed) {
            //   REPORT_FAILURE("value", values[i].type, values[i].barrier,
            //                  rebate, callPayoff, exercise, underlyingPrice,
            //                  q, r, today, values[i].volatility,
            //                  expected, calculated, error,
            //                  maxMcRelativeErrorAllowed);
            //}

         }
   
      }

      #if QL_DOTNET_FRAMEWORK
              [TestMethod()]
      #else
             [Fact]
      #endif
      public void testBeagleholeValues() 
      {
         // Testing barrier options against Beaglehole's values


         /*
            Data from
            "Going to Extreme: Correcting Simulation Bias in Exotic
            Option Valuation"
               - D.R. Beaglehole, P.H. Dybvig and G. Zhou
               Financial Analysts Journal; Jan / Feb 1997; 53, 1
         */
         BarrierOptionData[] values = {
            new BarrierOptionData( Barrier.Type.DownOut, 0.50,   50,      45,  5.477,  0.0)
         };

         double underlyingPrice = 50.0;
         double rebate = 0.0;
         double r = Math.Log(1.1);
         double q = 0.00;

         DayCounter dc = new Actual360();
         Date today = Date.Today;

         SimpleQuote underlying = new SimpleQuote(underlyingPrice);

         SimpleQuote qH_SME = new SimpleQuote(q);
         YieldTermStructure qTS = Utilities.flatRate(today, qH_SME, dc);

         SimpleQuote rH_SME = new SimpleQuote(r);
         YieldTermStructure rTS = Utilities.flatRate(today, rH_SME, dc);

         SimpleQuote volatility = new SimpleQuote(0.10);
         BlackVolTermStructure volTS = Utilities.flatVol(today, volatility, dc);


         Date exDate = today+360;
         Exercise exercise = new EuropeanExercise(exDate);

         for (int i=0; i<values.Length; i++) 
         {
            volatility.setValue(values[i].volatility);

            StrikedTypePayoff callPayoff = new PlainVanillaPayoff(Option.Type.Call,values[i].strike);

            BlackScholesMertonProcess stochProcess = new BlackScholesMertonProcess(
               new Handle<Quote>(underlying),
               new Handle<YieldTermStructure>(qTS),
               new Handle<YieldTermStructure>(rTS),
               new Handle<BlackVolTermStructure>(volTS));

            IPricingEngine engine = new AnalyticBarrierEngine(stochProcess);

            BarrierOption barrierCallOption = new BarrierOption(values[i].type,values[i].barrier,rebate,callPayoff,exercise);
            barrierCallOption.setPricingEngine(engine);
            double calculated = barrierCallOption.NPV();
            double expected = values[i].callValue;
            double maxErrorAllowed = 1.0e-3;
            double error = Math.Abs(calculated-expected);
            if (error > maxErrorAllowed) 
            {
               REPORT_FAILURE("value", values[i].type, values[i].barrier,
                              rebate, callPayoff, exercise, underlyingPrice,
                              q, r, today, values[i].volatility,
                              expected, calculated, error, maxErrorAllowed);
            }

            // TODO MakeMCBarrierEngine
            //double maxMcRelativeErrorAllowed = 0.01;
            //IPricingEngine mcEngine =
            //   MakeMCBarrierEngine<LowDiscrepancy>(stochProcess)
            //   .withStepsPerYear(1)
            //   .withBrownianBridge()
            //   .withSamples(131071) // 2^17-1
            //   .withMaxSamples(1048575) // 2^20-1
            //   .withSeed(10);

            //barrierCallOption.setPricingEngine(mcEngine);
            //calculated = barrierCallOption.NPV();
            //error = Math.Abs(calculated-expected)/expected;
            //if (error>maxMcRelativeErrorAllowed) 
            //{
            //   REPORT_FAILURE("value", values[i].type, values[i].barrier,
            //                  rebate, callPayoff, exercise, underlyingPrice,
            //                  q, r, today, values[i].volatility,
            //                  expected, calculated, error,
            //                  maxMcRelativeErrorAllowed);
            //}
         }
      }

      public void testLocalVolAndHestonComparison() 
      {
         // Testing local volatility and Heston FD engines for barrier options
         SavedSettings backup = new SavedSettings();

         Date settlementDate = new Date(5, Month.July, 2002);
         Settings.setEvaluationDate(settlementDate);

         DayCounter dayCounter = new Actual365Fixed();
         Calendar calendar = new TARGET();

         int[] t = { 13, 41, 75, 165, 256, 345, 524, 703 };
         double[] r = { 0.0357,0.0349,0.0341,0.0355,0.0359,0.0368,0.0386,0.0401 };

         List<double> rates = new InitializedList<double>(1, 0.0357);
         List<Date> dates = new InitializedList<Date>(1, settlementDate);
         for (int i = 0; i < 8; ++i) 
         {
            dates.Add(settlementDate + t[i]);
            rates.Add(r[i]);
         }
         
         Handle<YieldTermStructure> rTS = new Handle<YieldTermStructure>( new InterpolatedZeroCurve<Linear>(dates, rates, dayCounter));
         Handle<YieldTermStructure> qTS = new Handle<YieldTermStructure>(Utilities.flatRate(settlementDate, 0.0, dayCounter));

         Handle<Quote> s0 = new Handle<Quote>(new SimpleQuote(4500.00));
    
         double[] tmp = { 100 ,500 ,2000,3400,3600,3800,4000,4200,4400,4500,
                          4600,4800,5000,5200,5400,5600,7500,10000,20000,30000 };
         List<double> strikes = new List<double>(tmp);
    
         double[] v =
         { 1.015873, 1.015873, 1.015873, 0.89729, 0.796493, 0.730914, 0.631335, 0.568895,
            0.711309, 0.711309, 0.711309, 0.641309, 0.635593, 0.583653, 0.508045, 0.463182,
            0.516034, 0.500534, 0.500534, 0.500534, 0.448706, 0.416661, 0.375470, 0.353442,
            0.516034, 0.482263, 0.447713, 0.387703, 0.355064, 0.337438, 0.316966, 0.306859,
            0.497587, 0.464373, 0.430764, 0.374052, 0.344336, 0.328607, 0.310619, 0.301865,
            0.479511, 0.446815, 0.414194, 0.361010, 0.334204, 0.320301, 0.304664, 0.297180,
            0.461866, 0.429645, 0.398092, 0.348638, 0.324680, 0.312512, 0.299082, 0.292785,
            0.444801, 0.413014, 0.382634, 0.337026, 0.315788, 0.305239, 0.293855, 0.288660,
            0.428604, 0.397219, 0.368109, 0.326282, 0.307555, 0.298483, 0.288972, 0.284791,
            0.420971, 0.389782, 0.361317, 0.321274, 0.303697, 0.295302, 0.286655, 0.282948,
            0.413749, 0.382754, 0.354917, 0.316532, 0.300016, 0.292251, 0.284420, 0.281164,
            0.400889, 0.370272, 0.343525, 0.307904, 0.293204, 0.286549, 0.280189, 0.277767,
            0.390685, 0.360399, 0.334344, 0.300507, 0.287149, 0.281380, 0.276271, 0.274588,
            0.383477, 0.353434, 0.327580, 0.294408, 0.281867, 0.276746, 0.272655, 0.271617,
            0.379106, 0.349214, 0.323160, 0.289618, 0.277362, 0.272641, 0.269332, 0.268846,
            0.377073, 0.347258, 0.320776, 0.286077, 0.273617, 0.269057, 0.266293, 0.266265,
            0.399925, 0.369232, 0.338895, 0.289042, 0.265509, 0.255589, 0.249308, 0.249665,
            0.423432, 0.406891, 0.373720, 0.314667, 0.281009, 0.263281, 0.246451, 0.242166,
            0.453704, 0.453704, 0.453704, 0.381255, 0.334578, 0.305527, 0.268909, 0.251367,
            0.517748, 0.517748, 0.517748, 0.416577, 0.364770, 0.331595, 0.287423, 0.264285 };
    
         Matrix blackVolMatrix = new Matrix(strikes.Count, dates.Count-1);
         for (int i=0; i < strikes.Count; ++i)
            for (int j=1; j < dates.Count; ++j) 
            {
               blackVolMatrix[i,j-1] = v[i*(dates.Count-1)+j-1];
            }
    
         BlackVarianceSurface volTS = new BlackVarianceSurface(settlementDate, calendar,
            dates,strikes, blackVolMatrix,dayCounter);
         volTS.setInterpolation<Bicubic>();
         GeneralizedBlackScholesProcess localVolProcess = new BlackScholesMertonProcess(s0, qTS, rTS, 
            new Handle<BlackVolTermStructure>(volTS));
    
         double v0    = 0.195662;
         double kappa = 5.6628;
         double theta = 0.0745911;
         double sigma = 1.1619;
         double rho   = -0.511493;

         HestonProcess hestonProcess = new HestonProcess(rTS, qTS, s0, v0,kappa, theta, sigma, rho);

         HestonModel hestonModel =  new HestonModel(hestonProcess);

         // TODO FdHestonBarrierEngine
         //IPricingEngine fdHestonEngine = new FdHestonBarrierEngine(hestonModel, 100, 400, 50);

         // TODO FdBlackScholesBarrierEngine
         //IPricingEngine fdLocalVolEngine = new FdBlackScholesBarrierEngine(localVolProcess,100, 400, 0,FdmSchemeDesc.Douglas(), true, 0.35);
    
         double strike  = s0.link.value();
         double barrier = 3000;
         double rebate  = 100;
         Date exDate  = settlementDate + new Period(20, TimeUnit.Months);
    
         StrikedTypePayoff payoff = new PlainVanillaPayoff(Option.Type.Put, strike);

         Exercise exercise = new EuropeanExercise(exDate);

         BarrierOption barrierOption = new BarrierOption(Barrier.Type.DownOut,barrier, rebate, payoff, exercise);

         // TODO FdHestonBarrierEngine
         //barrierOption.setPricingEngine(fdHestonEngine);
         double expectedHestonNPV = 111.5;
         double calculatedHestonNPV = barrierOption.NPV();

         // TODO FdBlackScholesBarrierEngine
         //barrierOption.setPricingEngine(fdLocalVolEngine);
         double expectedLocalVolNPV = 132.8;
         double calculatedLocalVolNPV = barrierOption.NPV();
    
         double tol = 0.01;
    
         if (Math.Abs(expectedHestonNPV - calculatedHestonNPV) > tol*expectedHestonNPV) 
         {
            QAssert.Fail("Failed to reproduce Heston barrier price for "
                        + "\n    strike:     " + payoff.strike()
                        + "\n    barrier:    " + barrier
                        + "\n    maturity:   " + exDate
                        + "\n    calculated: " + calculatedHestonNPV
                        + "\n    expected:   " + expectedHestonNPV);
         }
         if (Math.Abs(expectedLocalVolNPV - calculatedLocalVolNPV) > tol*expectedLocalVolNPV) 
         {
            QAssert.Fail("Failed to reproduce Heston barrier price for "
                        + "\n    strike:     " + payoff.strike()
                        + "\n    barrier:    " + barrier
                        + "\n    maturity:   " + exDate
                        + "\n    calculated: " + calculatedLocalVolNPV
                        + "\n    expected:   " + expectedLocalVolNPV);
         }
      }

      #if QL_DOTNET_FRAMEWORK
         [TestMethod()]
      #else
         [Fact]
      #endif
      public void testVannaVolgaSimpleBarrierValues() 
      {
         // Testing barrier FX options against Vanna/Volga values
         SavedSettings backup = new SavedSettings();

         BarrierFxOptionData[] values = {

            //barrierType,barrier,rebate,type,strike,s,q,r,t,vol25Put,volAtm,vol25Call,vol, result, tol
            new BarrierFxOptionData( Barrier.Type.UpOut,1.5,0,     Option.Type.Call,1.13321,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.11638,0.148127, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.UpOut,1.5,0,     Option.Type.Call,1.22687,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.10088,0.075943, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.UpOut,1.5,0,     Option.Type.Call,1.31179,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.08925,0.0274771, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.UpOut,1.5,0,     Option.Type.Call,1.38843,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.08463,0.00573, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.UpOut,1.5,0,     Option.Type.Call,1.46047,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.08412,0.00012, 1.0e-4),

            new BarrierFxOptionData( Barrier.Type.UpOut,1.5,0,     Option.Type.Put,1.13321,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.11638,0.00697606, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.UpOut,1.5,0,     Option.Type.Put,1.22687,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.10088,0.020078, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.UpOut,1.5,0,     Option.Type.Put,1.31179,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.08925,0.0489395, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.UpOut,1.5,0,     Option.Type.Put,1.38843,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.08463,0.0969877, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.UpOut,1.5,0,     Option.Type.Put,1.46047,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.08412,0.157, 1.0e-4),

            new BarrierFxOptionData( Barrier.Type.UpIn,1.5,0,      Option.Type.Call,1.13321,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.11638,0.0322202, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.UpIn,1.5,0,      Option.Type.Call,1.22687,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.10088,0.0241491, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.UpIn,1.5,0,      Option.Type.Call,1.31179,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.08925,0.0164275, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.UpIn,1.5,0,      Option.Type.Call,1.38843,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.08463,0.01, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.UpIn,1.5,0,      Option.Type.Call,1.46047,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.08412,0.00489, 1.0e-4),

            new BarrierFxOptionData( Barrier.Type.UpIn,1.5,0,      Option.Type.Put,1.13321,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.11638,0.000560713, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.UpIn,1.5,0,      Option.Type.Put,1.22687,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.10088,0.000546804, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.UpIn,1.5,0,      Option.Type.Put,1.31179,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.08925,0.000130649, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.UpIn,1.5,0,      Option.Type.Put,1.38843,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.08463,0.000300828, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.UpIn,1.5,0,      Option.Type.Put,1.46047,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.08412,0.00135, 1.0e-4),

            new BarrierFxOptionData( Barrier.Type.DownOut,1.1,0,   Option.Type.Call,1.13321,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.11638,0.17746, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownOut,1.1,0,   Option.Type.Call,1.22687,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.10088,0.0994142, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownOut,1.1,0,   Option.Type.Call,1.31179,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.08925,0.0439, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownOut,1.1,0,   Option.Type.Call,1.38843,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.08463,0.01574, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownOut,1.1,0,   Option.Type.Call,1.46047,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.08412,0.00501, 1.0e-4),

            new BarrierFxOptionData( Barrier.Type.DownOut,1.3,0,   Option.Type.Call,1.13321,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.11638,0.00612, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownOut,1.3,0,   Option.Type.Call,1.22687,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.10088,0.00426, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownOut,1.3,0,   Option.Type.Call,1.31179,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.08925,0.00257, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownOut,1.3,0,   Option.Type.Call,1.38843,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.08463,0.00122, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownOut,1.3,0,   Option.Type.Call,1.46047,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.08412,0.00045, 1.0e-4),

            new BarrierFxOptionData( Barrier.Type.DownOut,1.1,0,  Option.Type.Put,1.13321,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.11638,0.00022, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownOut,1.1,0,  Option.Type.Put,1.22687,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.10088,0.00284, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownOut,1.1,0,  Option.Type.Put,1.31179,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.08925,0.02032, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownOut,1.1,0,  Option.Type.Put,1.38843,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.08463,0.058235, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownOut,1.1,0,  Option.Type.Put,1.46047,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.08412,0.109432, 1.0e-4),

            new BarrierFxOptionData( Barrier.Type.DownOut,1.3,0,  Option.Type.Put,1.13321,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.11638,0, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownOut,1.3,0,  Option.Type.Put,1.22687,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.10088,0, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownOut,1.3,0,  Option.Type.Put,1.31179,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.08925,0, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownOut,1.3,0,  Option.Type.Put,1.38843,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.08463,0.00017, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownOut,1.3,0,  Option.Type.Put,1.46047,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.08412,0.00083, 1.0e-4),

            new BarrierFxOptionData( Barrier.Type.DownIn,1.1,0,   Option.Type.Call,1.13321,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.11638,0.00289, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownIn,1.1,0,   Option.Type.Call,1.22687,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.10088,0.00067784, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownIn,1.1,0,   Option.Type.Call,1.31179,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.08925,0, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownIn,1.1,0,   Option.Type.Call,1.38843,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.08463,0, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownIn,1.1,0,   Option.Type.Call,1.46047,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.08412,0, 1.0e-4),

            new BarrierFxOptionData( Barrier.Type.DownIn,1.3,0,   Option.Type.Call,1.13321,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.11638,0.17423, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownIn,1.3,0,   Option.Type.Call,1.22687,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.10088,0.09584, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownIn,1.3,0,   Option.Type.Call,1.31179,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.08925,0.04133, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownIn,1.3,0,   Option.Type.Call,1.38843,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.08463,0.01452, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownIn,1.3,0,   Option.Type.Call,1.46047,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.08412,0.00456, 1.0e-4),

            new BarrierFxOptionData( Barrier.Type.DownIn,1.1,0,   Option.Type.Put,1.13321,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.11638,0.00732, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownIn,1.1,0,   Option.Type.Put,1.22687,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.10088,0.01778, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownIn,1.1,0,   Option.Type.Put,1.31179,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.08925,0.02875, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownIn,1.1,0,   Option.Type.Put,1.38843,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.08463,0.0390535, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownIn,1.1,0,   Option.Type.Put,1.46047,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.08412,0.0489236, 1.0e-4),

            new BarrierFxOptionData( Barrier.Type.DownIn,1.3,0,   Option.Type.Put,1.13321,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.11638,0.00753, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownIn,1.3,0,   Option.Type.Put,1.22687,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.10088,0.02062, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownIn,1.3,0,   Option.Type.Put,1.31179,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.08925,0.04907, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownIn,1.3,0,   Option.Type.Put,1.38843,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.08463,0.09711, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownIn,1.3,0,   Option.Type.Put,1.46047,1.30265,0.0003541,0.0033871,1,0.10087,0.08925,0.08463,0.08412,0.15752, 1.0e-4),

            new BarrierFxOptionData( Barrier.Type.UpOut,1.6,0,    Option.Type.Call,1.06145,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.12511,0.20493, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.UpOut,1.6,0,    Option.Type.Call,1.19545,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.1089,0.105577, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.UpOut,1.6,0,    Option.Type.Call,1.32238,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.09444,0.0358872, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.UpOut,1.6,0,    Option.Type.Call,1.44298,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.09197,0.00634958, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.UpOut,1.6,0,    Option.Type.Call,1.56345,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.09261,0, 1.0e-4),

            new BarrierFxOptionData( Barrier.Type.UpOut,1.6,0,    Option.Type.Put,1.06145,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.12511,0.0108218, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.UpOut,1.6,0,    Option.Type.Put,1.19545,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.1089,0.0313339, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.UpOut,1.6,0,    Option.Type.Put,1.32238,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.09444,0.0751237, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.UpOut,1.6,0,    Option.Type.Put,1.44298,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.09197,0.153407, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.UpOut,1.6,0,    Option.Type.Put,1.56345,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.09261,0.253767, 1.0e-4),

            new BarrierFxOptionData( Barrier.Type.UpIn,1.6,0,     Option.Type.Call,1.06145,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.12511,0.05402, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.UpIn,1.6,0,     Option.Type.Call,1.19545,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.1089,0.0410069, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.UpIn,1.6,0,     Option.Type.Call,1.32238,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.09444,0.0279562, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.UpIn,1.6,0,     Option.Type.Call,1.44298,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.09197,0.0173055, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.UpIn,1.6,0,     Option.Type.Call,1.56345,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.09261,0.00764, 1.0e-4),

            new BarrierFxOptionData( Barrier.Type.UpIn,1.6,0,     Option.Type.Put,1.06145,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.12511,0.000962737, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.UpIn,1.6,0,     Option.Type.Put,1.19545,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.1089,0.00102637, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.UpIn,1.6,0,     Option.Type.Put,1.32238,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.09444,0.000419834, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.UpIn,1.6,0,     Option.Type.Put,1.44298,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.09197,0.00159277, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.UpIn,1.6,0,     Option.Type.Put,1.56345,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.09261,0.00473629, 1.0e-4),

            new BarrierFxOptionData( Barrier.Type.DownOut,1,0,    Option.Type.Call,1.06145,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.12511,0.255098, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownOut,1,0,    Option.Type.Call,1.19545,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.1089,0.145701, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownOut,1,0,    Option.Type.Call,1.32238,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.09444,0.06384, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownOut,1,0,    Option.Type.Call,1.44298,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.09197,0.02366, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownOut,1,0,    Option.Type.Call,1.56345,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.09261,0.00764, 1.0e-4),

            new BarrierFxOptionData( Barrier.Type.DownOut,1.3,0,  Option.Type.Call,1.06145,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.12511,0.00592, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownOut,1.3,0,  Option.Type.Call,1.19545,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.1089,0.00421, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownOut,1.3,0,  Option.Type.Call,1.32238,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.09444,0.00256, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownOut,1.3,0,  Option.Type.Call,1.44298,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.09197,0.0012, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownOut,1.3,0,  Option.Type.Call,1.56345,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.09261,0.0004, 1.0e-4),

            new BarrierFxOptionData( Barrier.Type.DownOut,1,0,    Option.Type.Put,1.06145,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.12511,0, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownOut,1,0,    Option.Type.Put,1.19545,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.1089,0.00280549, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownOut,1,0,    Option.Type.Put,1.32238,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.09444,0.0279945, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownOut,1,0,    Option.Type.Put,1.44298,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.09197,0.0896352, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownOut,1,0,    Option.Type.Put,1.56345,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.09261,0.175182, 1.0e-4),

            new BarrierFxOptionData( Barrier.Type.DownOut,1.3,0,  Option.Type.Put,1.06145,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.12511,    0.00000, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownOut,1.3,0,  Option.Type.Put,1.19545,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.1089,     0.00000, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownOut,1.3,0,  Option.Type.Put,1.32238,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.09444,    0.00000, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownOut,1.3,0,  Option.Type.Put,1.44298,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.09197,0.0002, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownOut,1.3,0,  Option.Type.Put,1.56345,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.09261,0.00096, 1.0e-4),

            new BarrierFxOptionData( Barrier.Type.DownIn,1,0,     Option.Type.Call,1.06145,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.12511,0.00384783, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownIn,1,0,     Option.Type.Call,1.19545,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.1089,0.000883232, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownIn,1,0,     Option.Type.Call,1.32238,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.09444,0, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownIn,1,0,     Option.Type.Call,1.44298,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.09197,   0.00000, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownIn,1,0,     Option.Type.Call,1.56345,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.09261,   0.00000, 1.0e-4),

            new BarrierFxOptionData( Barrier.Type.DownIn,1.3,0,   Option.Type.Call,1.06145,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.12511,0.25302, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownIn,1.3,0,   Option.Type.Call,1.19545,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.1089,0.14238, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownIn,1.3,0,   Option.Type.Call,1.32238,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.09444,0.06128, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownIn,1.3,0,   Option.Type.Call,1.44298,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.09197,0.02245, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownIn,1.3,0,   Option.Type.Call,1.56345,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.09261,0.00725, 1.0e-4),

            new BarrierFxOptionData( Barrier.Type.DownIn,1,0,     Option.Type.Put,1.06145,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.12511,0.01178, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownIn,1,0,     Option.Type.Put,1.19545,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.1089,0.0295548, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownIn,1,0,     Option.Type.Put,1.32238,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.09444,0.047549, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownIn,1,0,     Option.Type.Put,1.44298,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.09197,0.0653642, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownIn,1,0,     Option.Type.Put,1.56345,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.09261,0.0833221, 1.0e-4),

            new BarrierFxOptionData( Barrier.Type.DownIn,1.3,0,   Option.Type.Put,1.06145,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.12511,0.01178, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownIn,1.3,0,   Option.Type.Put,1.19545,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.1089,0.03236, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownIn,1.3,0,   Option.Type.Put,1.32238,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.09444,0.07554, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownIn,1.3,0,   Option.Type.Put,1.44298,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.09197,0.15479, 1.0e-4),
            new BarrierFxOptionData( Barrier.Type.DownIn,1.3,0,   Option.Type.Put,1.56345,1.30265,0.0009418,0.0039788,2,0.10891,0.09525,0.09197,0.09261,0.25754, 1.0e-4),

         };

         DayCounter dc = new Actual365Fixed();
         Date today = new Date(5, Month.March, 2013);
         Settings.setEvaluationDate(today);

         SimpleQuote spot = new SimpleQuote(0.0);
         SimpleQuote qRate = new SimpleQuote(0.0);
         YieldTermStructure qTS = Utilities.flatRate(today, qRate, dc);
         SimpleQuote rRate = new SimpleQuote(0.0);
         YieldTermStructure rTS = Utilities.flatRate(today, rRate, dc);
         SimpleQuote vol25Put = new SimpleQuote(0.0);
         SimpleQuote volAtm = new SimpleQuote(0.0);
         SimpleQuote vol25Call = new SimpleQuote(0.0);

         for (int i=0; i< values.Length; i++) 
         {
            spot.setValue(values[i].s);
            qRate.setValue(values[i].q);
            rRate.setValue(values[i].r);
            vol25Put.setValue(values[i].vol25Put);
            volAtm.setValue(values[i].volAtm);
            vol25Call.setValue(values[i].vol25Call);

            StrikedTypePayoff payoff = new PlainVanillaPayoff(values[i].type,values[i].strike);
            Date exDate = today + (int)(values[i].t*365+0.5);
            Exercise exercise = new EuropeanExercise(exDate);

            Handle<DeltaVolQuote> volAtmQuote = new Handle<DeltaVolQuote>(
					new DeltaVolQuote( new Handle<Quote>(volAtm),
							             DeltaVolQuote.DeltaType.Fwd,
							             values[i].t,
							             DeltaVolQuote.AtmType.AtmDeltaNeutral));

            Handle<DeltaVolQuote> vol25PutQuote = new Handle<DeltaVolQuote>(
				   new DeltaVolQuote( -0.25,
							             new Handle<Quote>(vol25Put),
							             values[i].t,
							             DeltaVolQuote.DeltaType.Fwd));

            Handle<DeltaVolQuote> vol25CallQuote = new Handle<DeltaVolQuote>(
				   new DeltaVolQuote( 0.25,
							             new Handle<Quote>(vol25Call),
							             values[i].t,
							             DeltaVolQuote.DeltaType.Fwd));

            BarrierOption barrierOption = new BarrierOption(values[i].barrierType,values[i].barrier,values[i].rebate,
               payoff,exercise);

            double bsVanillaPrice = Utils.blackFormula(values[i].type, values[i].strike,
               spot.value()*qTS.discount(values[i].t)/rTS.discount(values[i].t),
				   values[i].v * Math.Sqrt(values[i].t), rTS.discount(values[i].t));
            IPricingEngine vannaVolgaEngine = new VannaVolgaBarrierEngine( volAtmQuote, vol25PutQuote, vol25CallQuote,
				   new Handle<Quote> (spot),
				   new Handle<YieldTermStructure> (rTS),
				   new Handle<YieldTermStructure> (qTS),
				   true,
				   bsVanillaPrice);
            barrierOption.setPricingEngine(vannaVolgaEngine);

            double calculated = barrierOption.NPV();
            double expected = values[i].result;
            double error = Math.Abs(calculated-expected);
            if (error>values[i].tol) 
            {
               REPORT_FX_FAILURE( "value", values[i].barrierType, values[i].barrier,
                                  values[i].rebate, payoff, exercise, values[i].s,
                                  values[i].q, values[i].r, today, values[i].vol25Put,
                                  values[i].volAtm, values[i].vol25Call, values[i].v,
                                  expected, calculated, error, values[i].tol);
            }
         }
      }
   }
}
