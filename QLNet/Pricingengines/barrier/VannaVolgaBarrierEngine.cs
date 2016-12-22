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

namespace QLNet
{
   public class VannaVolgaBarrierEngine : GenericEngine<DividendBarrierOption.Arguments,DividendBarrierOption.Results>
   {
      public VannaVolgaBarrierEngine( Handle<DeltaVolQuote> atmVol,
                                      Handle<DeltaVolQuote> vol25Put,
                                      Handle<DeltaVolQuote> vol25Call,
                                      Handle<Quote> spotFX,
                                      Handle<YieldTermStructure> domesticTS,
                                      Handle<YieldTermStructure> foreignTS,
                                      bool adaptVanDelta = false,
                                      double bsPriceWithSmile = 0.0)
      {
         atmVol_ = atmVol; 
         vol25Put_ = vol25Put; 
         vol25Call_ = vol25Call; 
         T_ = atmVol_.link.maturity();
         spotFX_ = spotFX; 
         domesticTS_ = domesticTS; 
         foreignTS_ = foreignTS;
         adaptVanDelta_ = adaptVanDelta;
         bsPriceWithSmile_ = bsPriceWithSmile;

         Utils.QL_REQUIRE( vol25Put_.link.delta() == -0.25,()=> "25 delta put is required by vanna volga method" );
         Utils.QL_REQUIRE( vol25Call_.link.delta() == 0.25,()=> "25 delta call is required by vanna volga method" );

         Utils.QL_REQUIRE( vol25Put_.link.maturity() == vol25Call_.link.maturity() && 
                           vol25Put_.link.maturity() == atmVol_.link.maturity(),()=>
             "Maturity of 3 vols are not the same" );

         Utils.QL_REQUIRE( !domesticTS_.empty(),()=> "domestic yield curve is not defined" );
         Utils.QL_REQUIRE( !foreignTS_.empty(),()=> "foreign yield curve is not defined" );

         atmVol_.registerWith(update);
         vol25Put_.registerWith(update);
         vol25Call_.registerWith(update);
         spotFX_.registerWith(update);
         domesticTS_.registerWith(update);
         foreignTS_.registerWith(update);

      }

      public override void calculate()
      {
         double sigmaShift_vega = 0.0001;
         double sigmaShift_volga = 0.0001;
         double spotShift_delta = 0.0001 * spotFX_.link.value();
         double sigmaShift_vanna = 0.0001;

         Handle<Quote> x0Quote = new Handle<Quote>(new SimpleQuote(spotFX_.link.value())); //used for shift
         Handle<Quote> atmVolQuote = new Handle<Quote>(new SimpleQuote(atmVol_.link.value())); //used for shift

         BlackVolTermStructure blackVolTS = new BlackConstantVol(Settings.evaluationDate(),
                  new NullCalendar(), atmVolQuote, new Actual365Fixed());
         BlackScholesMertonProcess stochProcess = new BlackScholesMertonProcess(x0Quote,foreignTS_,domesticTS_,
            new Handle<BlackVolTermStructure>(blackVolTS));

         IPricingEngine engineBS = new AnalyticBarrierEngine(stochProcess);

         BlackDeltaCalculator blackDeltaCalculatorAtm = new BlackDeltaCalculator(
            Option.Type.Call, atmVol_.link.deltaType(), x0Quote.link.value(),
            domesticTS_.link.discount(T_), foreignTS_.link.discount(T_),
            atmVol_.link.value() * Math.Sqrt(T_));
         double atmStrike = blackDeltaCalculatorAtm.atmStrike(atmVol_.link.atmType());

         double call25Vol = vol25Call_.link.value();
         double put25Vol = vol25Put_.link.value();

         BlackDeltaCalculator blackDeltaCalculatorPut25 = new BlackDeltaCalculator(
            Option.Type.Put, vol25Put_.link.deltaType(), x0Quote.link.value(), 
            domesticTS_.link.discount(T_), foreignTS_.link.discount(T_),
            put25Vol * Math.Sqrt(T_));
         double put25Strike = blackDeltaCalculatorPut25.strikeFromDelta(-0.25);
         BlackDeltaCalculator blackDeltaCalculatorCall25 = new BlackDeltaCalculator(
            Option.Type.Call, vol25Call_.link.deltaType(), x0Quote.link.value(), 
            domesticTS_.link.discount(T_), foreignTS_.link.discount(T_),
            call25Vol * Math.Sqrt(T_));
         double call25Strike = blackDeltaCalculatorCall25.strikeFromDelta(0.25);

         //here use vanna volga interpolated smile to price vanilla
         List<double> strikes = new List<double>();
         List<double> vols = new List<double>();
         strikes.Add(put25Strike);
         vols.Add(put25Vol);
         strikes.Add(atmStrike);
         vols.Add(atmVol_.link.value());
         strikes.Add(call25Strike);
         vols.Add(call25Vol);
         VannaVolga vannaVolga = new VannaVolga(x0Quote.link.value(), domesticTS_.link.discount(T_), 
            foreignTS_.link.discount(T_), T_);
         Interpolation interpolation = vannaVolga.interpolate(strikes, strikes.Count, vols);
         interpolation.enableExtrapolation();
         StrikedTypePayoff payoff = arguments_.payoff as StrikedTypePayoff;
         double strikeVol = interpolation.value(payoff.strike());

         //vannila option price
         double vanillaOption = Utils.blackFormula(payoff.optionType(), payoff.strike(), 
            x0Quote.link.value()* foreignTS_.link.discount(T_)/ domesticTS_.link.discount(T_), 
            strikeVol * Math.Sqrt(T_),
            domesticTS_.link.discount(T_));

         //spot > barrier up&out 0
         if(x0Quote.link.value() >= arguments_.barrier && arguments_.barrierType == Barrier.Type.UpOut)
         {
            results_.value = 0.0;
            results_.additionalResults["VanillaPrice"] = adaptVanDelta_? bsPriceWithSmile_ : vanillaOption;
            results_.additionalResults["BarrierInPrice"] = adaptVanDelta_? bsPriceWithSmile_ : vanillaOption;
            results_.additionalResults["BarrierOutPrice"] = 0.0;
         }
         //spot > barrier up&in vanilla
         else if(x0Quote.link.value() >= arguments_.barrier && arguments_.barrierType == Barrier.Type.UpIn)
         {
            results_.value = adaptVanDelta_? bsPriceWithSmile_ : vanillaOption;
            results_.additionalResults["VanillaPrice"] = adaptVanDelta_? bsPriceWithSmile_ : vanillaOption;
            results_.additionalResults["BarrierInPrice"] = adaptVanDelta_? bsPriceWithSmile_ : vanillaOption;
            results_.additionalResults["BarrierOutPrice"] = 0.0;
         }
         //spot < barrier down&out 0
         else if(x0Quote.link.value() <= arguments_.barrier && arguments_.barrierType == Barrier.Type.DownOut)
         {
            results_.value = 0.0;
            results_.additionalResults["VanillaPrice"] = adaptVanDelta_? bsPriceWithSmile_ : vanillaOption;
            results_.additionalResults["BarrierInPrice"] = adaptVanDelta_? bsPriceWithSmile_ : vanillaOption;
            results_.additionalResults["BarrierOutPrice"] = 0.0;
         }
         //spot < barrier down&in vanilla
         else if(x0Quote.link.value() <= arguments_.barrier && arguments_.barrierType == Barrier.Type.DownIn)
         {
            results_.value = adaptVanDelta_? bsPriceWithSmile_ : vanillaOption;
            results_.additionalResults["VanillaPrice"] = adaptVanDelta_? bsPriceWithSmile_ : vanillaOption;
            results_.additionalResults["BarrierInPrice"] = adaptVanDelta_? bsPriceWithSmile_ : vanillaOption;
            results_.additionalResults["BarrierOutPrice"] = 0.0;
         }
         else
         {
            //set up BS barrier option pricing
            //only calculate out barrier option price
            // in barrier price = vanilla - out barrier
            Barrier.Type barrierTyp;
            if(arguments_.barrierType == Barrier.Type.UpOut)
                  barrierTyp = arguments_.barrierType;
            else if(arguments_.barrierType == Barrier.Type.UpIn)
                  barrierTyp = Barrier.Type.UpOut;
            else if(arguments_.barrierType == Barrier.Type.DownOut)
                  barrierTyp = arguments_.barrierType;
            else
                  barrierTyp = Barrier.Type.DownOut;

            BarrierOption barrierOption = new BarrierOption(barrierTyp,
               arguments_.barrier.GetValueOrDefault(),
               arguments_.rebate.GetValueOrDefault(),
               (StrikedTypePayoff)arguments_.payoff,
               arguments_.exercise);

            barrierOption.setPricingEngine(engineBS);

            //BS price with atm vol
            double priceBS = barrierOption.NPV();

            double priceAtmCallBS = Utils.blackFormula(Option.Type.Call,atmStrike,
               x0Quote.link.value()* foreignTS_.link.discount(T_)/ domesticTS_.link.discount(T_), 
               atmVol_.link.value() * Math.Sqrt(T_),
               domesticTS_.link.discount(T_));
            double price25CallBS = Utils.blackFormula(Option.Type.Call,call25Strike,
               x0Quote.link.value()* foreignTS_.link.discount(T_)/ domesticTS_.link.discount(T_), 
               atmVol_.link.value() * Math.Sqrt(T_),
               domesticTS_.link.discount(T_));
            double price25PutBS = Utils.blackFormula(Option.Type.Put,put25Strike,
               x0Quote.link.value()* foreignTS_.link.discount(T_)/ domesticTS_.link.discount(T_),
               atmVol_.link.value() * Math.Sqrt(T_),
               domesticTS_.link.discount(T_));

            //market price
            double priceAtmCallMkt = Utils.blackFormula(Option.Type.Call,atmStrike,
               x0Quote.link.value()* foreignTS_.link.discount(T_)/ domesticTS_.link.discount(T_), 
               atmVol_.link.value() * Math.Sqrt(T_),
               domesticTS_.link.discount(T_));

            double price25CallMkt = Utils.blackFormula(Option.Type.Call,call25Strike,
               x0Quote.link.value()* foreignTS_.link.discount(T_)/ domesticTS_.link.discount(T_), 
               call25Vol * Math.Sqrt(T_),
               domesticTS_.link.discount(T_));
            double price25PutMkt = Utils.blackFormula(Option.Type.Put,put25Strike,
               x0Quote.link.value()* foreignTS_.link.discount(T_)/ domesticTS_.link.discount(T_),
               put25Vol * Math.Sqrt(T_),
               domesticTS_.link.discount(T_));


            //Analytical Black Scholes formula for vanilla option
            NormalDistribution norm = new NormalDistribution();
            double d1atm = (Math.Log(x0Quote.link.value()* foreignTS_.link.discount(T_)/ 
                                     domesticTS_.link.discount(T_)/atmStrike) 
                            + 0.5*Math.Pow(atmVolQuote.link.value(),2.0) * T_)/
                                          (atmVolQuote.link.value() * Math.Sqrt(T_));
            double vegaAtm_Analytical = x0Quote.link.value() * norm.value(d1atm) * Math.Sqrt(T_) * 
               foreignTS_.link.discount(T_);
            double vannaAtm_Analytical = vegaAtm_Analytical/x0Quote.link.value() *
               (1.0 - d1atm/(atmVolQuote.link.value()*Math.Sqrt(T_)));
            double volgaAtm_Analytical = vegaAtm_Analytical * d1atm * (d1atm - atmVolQuote.link.value() * Math.Sqrt(T_))/
               atmVolQuote.link.value();

            double d125call = (Math.Log(x0Quote.link.value()* foreignTS_.link.discount(T_)/ 
               domesticTS_.link.discount(T_)/call25Strike) 
               + 0.5*Math.Pow(atmVolQuote.link.value(),2.0) * T_)/(atmVolQuote.link.value() * Math.Sqrt(T_));
            double vega25Call_Analytical = x0Quote.link.value() * norm.value(d125call) * Math.Sqrt(T_) * 
               foreignTS_.link.discount(T_);
            double vanna25Call_Analytical = vega25Call_Analytical/x0Quote.link.value() *
               (1.0 - d125call/(atmVolQuote.link.value()*Math.Sqrt(T_)));
            double volga25Call_Analytical = vega25Call_Analytical * d125call * 
               (d125call - atmVolQuote.link.value() * Math.Sqrt(T_))/atmVolQuote.link.value();

            double d125Put = (Math.Log(x0Quote.link.value()* foreignTS_.link.discount(T_)/ 
               domesticTS_.link.discount(T_)/put25Strike) 
               + 0.5*Math.Pow(atmVolQuote.link.value(),2.0) * T_)/(atmVolQuote.link.value() * Math.Sqrt(T_));
            double vega25Put_Analytical = x0Quote.link.value() * norm.value(d125Put) * Math.Sqrt(T_) * 
               foreignTS_.link.discount(T_);
            double vanna25Put_Analytical = vega25Put_Analytical/x0Quote.link.value() *
               (1.0 - d125Put/(atmVolQuote.link.value()*Math.Sqrt(T_)));
            double volga25Put_Analytical = vega25Put_Analytical * d125Put * 
               (d125Put - atmVolQuote.link.value() * Math.Sqrt(T_))/atmVolQuote.link.value();


            //BS vega
            ((SimpleQuote)atmVolQuote.currentLink()).setValue(atmVolQuote.link.value() + sigmaShift_vega);
            barrierOption.recalculate();
            double vegaBarBS = (barrierOption.NPV() - priceBS)/sigmaShift_vega;

            ((SimpleQuote)atmVolQuote.currentLink()).setValue(atmVolQuote.link.value() - sigmaShift_vega);//setback

            //BS volga

            //vegaBar2
            //base NPV
            ((SimpleQuote)atmVolQuote.currentLink()).setValue(atmVolQuote.link.value() + sigmaShift_volga);
            barrierOption.recalculate();
            double priceBS2 = barrierOption.NPV();

            //shifted npv
            ((SimpleQuote)atmVolQuote.currentLink()).setValue(atmVolQuote.link.value() + sigmaShift_vega);
            barrierOption.recalculate();
            double vegaBarBS2 = (barrierOption.NPV() - priceBS2)/sigmaShift_vega;
            double volgaBarBS = (vegaBarBS2 - vegaBarBS)/sigmaShift_volga;

            ((SimpleQuote)atmVolQuote.currentLink()).setValue(atmVolQuote.link.value() 
                                                                                       - sigmaShift_volga 
                                                                                       - sigmaShift_vega);//setback

            //BS Delta
            //base delta
            ((SimpleQuote)x0Quote.currentLink()).setValue(x0Quote.link.value() + spotShift_delta);//shift forth
            barrierOption.recalculate();
            double priceBS_delta1 = barrierOption.NPV();

            ((SimpleQuote)x0Quote.currentLink()).setValue(x0Quote.link.value() - 2 * spotShift_delta);//shift back
            barrierOption.recalculate();
            double priceBS_delta2 = barrierOption.NPV();

            ((SimpleQuote)x0Quote.currentLink()).setValue(x0Quote.link.value() +  spotShift_delta);//set back
            double deltaBar1 = (priceBS_delta1 - priceBS_delta2)/(2.0*spotShift_delta);

            //shifted delta
            ((SimpleQuote)atmVolQuote.currentLink()).setValue(atmVolQuote.link.value() + sigmaShift_vanna);//shift sigma
            ((SimpleQuote)x0Quote.currentLink()).setValue(x0Quote.link.value() + spotShift_delta);//shift forth
            barrierOption.recalculate();
            priceBS_delta1 = barrierOption.NPV();

            ((SimpleQuote)x0Quote.currentLink()).setValue(x0Quote.link.value() - 2 * spotShift_delta);//shift back
            barrierOption.recalculate();
            priceBS_delta2 = barrierOption.NPV();

            ((SimpleQuote)x0Quote.currentLink()).setValue(x0Quote.link.value() +  spotShift_delta);//set back
            double deltaBar2 = (priceBS_delta1 - priceBS_delta2)/(2.0*spotShift_delta);

            double vannaBarBS = (deltaBar2 - deltaBar1)/sigmaShift_vanna;

            ((SimpleQuote)atmVolQuote.currentLink()).setValue(atmVolQuote.link.value() - sigmaShift_vanna);//set back

            //Matrix
            Matrix A = new Matrix(3,3,0.0);

            //analytical
            A[0,0] = vegaAtm_Analytical;
            A[0,1] = vega25Call_Analytical;
            A[0,2] = vega25Put_Analytical;
            A[1,0] = vannaAtm_Analytical;
            A[1,1] = vanna25Call_Analytical;
            A[1,2] = vanna25Put_Analytical;
            A[2,0] = volgaAtm_Analytical;
            A[2,1] = volga25Call_Analytical;
            A[2,2] = volga25Put_Analytical;

            Vector b = new Vector(3,0.0);
            b[0] = vegaBarBS;
            b[1] = vannaBarBS;
            b[2] = volgaBarBS;

            //Vector q = inverse(A) * b; TODO implements transpose
            Vector q = Matrix.inverse(A) * b;


            //touch probability
            CumulativeNormalDistribution cnd = new CumulativeNormalDistribution();
            double mu = domesticTS_.link.zeroRate(T_, Compounding.Continuous).value() - 
               foreignTS_.link.zeroRate(T_, Compounding.Continuous).value() - 
               Math.Pow(atmVol_.link.value(), 2.0)/2.0;
            double h2 = (Math.Log(arguments_.barrier.GetValueOrDefault()/x0Quote.link.value()) + mu*T_)/
               (atmVol_.link.value()*Math.Sqrt(T_));
            double h2Prime = (Math.Log(x0Quote.link.value()/arguments_.barrier.GetValueOrDefault()) + mu*T_)/
               (atmVol_.link.value()*Math.Sqrt(T_));
            double probTouch = 0.0;
            if(arguments_.barrierType == Barrier.Type.UpIn || arguments_.barrierType == Barrier.Type.UpOut)
               probTouch = cnd.value(h2Prime) + Math.Pow(arguments_.barrier.GetValueOrDefault()/x0Quote.link.value(), 
                  2.0*mu/Math.Pow(atmVol_.link.value(), 2.0))*cnd.value(-h2);
            else
               probTouch = cnd.value(-h2Prime) + Math.Pow(arguments_.barrier.GetValueOrDefault()/x0Quote.link.value(), 
                  2.0*mu/Math.Pow(atmVol_.link.value(), 2.0))*cnd.value(h2);
            double p_survival = 1.0 - probTouch;

            double lambda = p_survival ;
            double adjust = q[0]*(priceAtmCallMkt - priceAtmCallBS) 
                        + q[1]*(price25CallMkt - price25CallBS)
                        + q[2]*(price25PutMkt - price25PutBS);
            double outPrice = priceBS + lambda*adjust;//
            double inPrice;

            //adapt Vanilla delta
            if(adaptVanDelta_ == true)
            {
               outPrice += lambda*(bsPriceWithSmile_ - vanillaOption);
               //capfloored by (0, vanilla)
               outPrice = Math.Max(0.0, Math.Min(bsPriceWithSmile_, outPrice));
               inPrice = bsPriceWithSmile_ - outPrice;
            }
            else
            {
               //capfloored by (0, vanilla)
               outPrice = Math.Max(0.0, Math.Min(vanillaOption , outPrice));
               inPrice = vanillaOption - outPrice;
            }

            if(arguments_.barrierType == Barrier.Type.DownOut || arguments_.barrierType == Barrier.Type.UpOut)
               results_.value = outPrice;
            else
               results_.value = inPrice;

            results_.additionalResults["VanillaPrice"] = vanillaOption;
            results_.additionalResults["BarrierInPrice"] = inPrice;
            results_.additionalResults["BarrierOutPrice"] = outPrice;
            results_.additionalResults["lambda"] = lambda;
         }
      }

      private Handle<DeltaVolQuote> atmVol_;
      private Handle<DeltaVolQuote> vol25Put_;
      private Handle<DeltaVolQuote> vol25Call_;
      private double T_;
      private Handle<Quote> spotFX_;
      private Handle<YieldTermStructure> domesticTS_;
      private Handle<YieldTermStructure> foreignTS_;
      private bool adaptVanDelta_;
      private double bsPriceWithSmile_;

   }
}
