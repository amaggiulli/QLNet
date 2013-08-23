using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QLNet
{
   /// <summary>
   /// Black-formula cap/floor engine
   /// \ingroup capfloorengines
   /// </summary>
   public class BlackCapFloorEngine : CapFloorEngine
   {
      private  Handle<YieldTermStructure> termStructure_;
      private Handle<OptionletVolatilityStructure> volatility_;

      public BlackCapFloorEngine(Handle<YieldTermStructure> termStructure, double vol)
         : this(termStructure, vol, new Actual365Fixed()) { }
      public BlackCapFloorEngine(Handle<YieldTermStructure> termStructure,
                                 double vol, DayCounter dc )
      {
         termStructure_ = termStructure;
         volatility_ = new Handle<OptionletVolatilityStructure>(new ConstantOptionletVolatility(0, new NullCalendar(), BusinessDayConvention.Following, vol, dc));
         termStructure_.registerWith(update );// registerWith(termStructure_);
      }

      public BlackCapFloorEngine(Handle<YieldTermStructure> termStructure, Handle<Quote> vol)
         : this(termStructure, vol, new Actual365Fixed()) { }

      public BlackCapFloorEngine(Handle<YieldTermStructure> termStructure,
                                 Handle<Quote> vol, DayCounter dc)
      {
         termStructure_ = termStructure;
         volatility_ = new Handle<OptionletVolatilityStructure> (new ConstantOptionletVolatility(
                             0, new NullCalendar(), BusinessDayConvention.Following, vol, dc));
         termStructure_.registerWith(update);
         volatility_.registerWith(update);
      }

      public BlackCapFloorEngine(Handle<YieldTermStructure> discountCurve,
                                 Handle<OptionletVolatilityStructure> vol)
      {
         termStructure_ = discountCurve;
         volatility_ = vol;
         termStructure_.registerWith(update);
         volatility_.registerWith(update);
      }

      public override void calculate() 
      {
         double value = 0.0;
         double vega = 0.0;
         int optionlets = arguments_.startDates.Count;
         List<double> values = new InitializedList<double>(optionlets);
         List<double> vegas = new InitializedList<double>(optionlets);
         List<double> stdDevs = new InitializedList<double>(optionlets);
         CapFloorType type = arguments_.type;
         Date today = volatility_.link.referenceDate();
         Date settlement = termStructure_.link.referenceDate();

         for (int i=0; i<optionlets; ++i) 
         {
            Date paymentDate = arguments_.endDates[i];
            if (paymentDate > settlement) 
            { 
               // discard expired caplets
               double d = arguments_.nominals[i] *
                          arguments_.gearings[i] *
                          termStructure_.link.discount(paymentDate) *
                          arguments_.accrualTimes[i];

               double? forward = arguments_.forwards[i];

               Date fixingDate = arguments_.fixingDates[i];
               double sqrtTime = 0.0;
               if (fixingDate > today)
                  sqrtTime = Math.Sqrt( volatility_.link.timeFromReference(fixingDate));

               if (type == CapFloorType.Cap || type == CapFloorType.Collar) 
               {
                  double? strike = arguments_.capRates[i];
                  if (sqrtTime>0.0) 
                  {
                     stdDevs[i] = Math.Sqrt(volatility_.link.blackVariance(fixingDate, strike.Value));
                     vegas[i] = Utils.blackFormulaStdDevDerivative(strike.Value, forward.Value, stdDevs[i], d) * sqrtTime;
                  }
                  // include caplets with past fixing date
                  values[i] = Utils.blackFormula(Option.Type.Call, strike.Value,
                                             forward.Value, stdDevs[i], d);
               }
                if (type == CapFloorType.Floor || type == CapFloorType.Collar) 
                {
                  double? strike = arguments_.floorRates[i];
                  double floorletVega = 0.0;
                    
                  if (sqrtTime>0.0) 
                  {
                     stdDevs[i] = Math.Sqrt(volatility_.link.blackVariance(fixingDate, strike.Value));
                     floorletVega = Utils.blackFormulaStdDevDerivative(strike.Value, forward.Value, stdDevs[i], d) * sqrtTime;
                  }
                  double floorlet = Utils.blackFormula(Option.Type.Put, strike.Value,
                                                 forward.Value, stdDevs[i], d);
                  if (type == CapFloorType.Floor) 
                  {
                     values[i] = floorlet;
                     vegas[i] = floorletVega;
                  } 
                  else 
                  {
                     // a collar is long a cap and short a floor
                     values[i] -= floorlet;
                     vegas[i] -= floorletVega;
                  }
                }
                value += values[i];
                vega += vegas[i];
            }
        }
        results_.value = value;
        results_.additionalResults["vega"] = vega;

        results_.additionalResults["optionletsPrice"] = values;
        results_.additionalResults["optionletsVega"] = vegas;
        results_.additionalResults["optionletsAtmForward"] = arguments_.forwards;
        if (type != CapFloorType.Collar)
            results_.additionalResults["optionletsStdDev"] = stdDevs;
    }
   }
}
