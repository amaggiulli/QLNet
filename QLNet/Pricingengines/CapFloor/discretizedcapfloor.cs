/*
 Copyright (C) 2009 Philippe Real (ph_real@hotmail.com)
  
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

namespace QLNet
{
    public class DiscretizedCapFloor : DiscretizedAsset {
      private  CapFloor.Arguments arguments_;
      private  List<double>  startTimes_;
      private  List<double>  endTimes_;

      public  DiscretizedCapFloor(CapFloor.Arguments args,
                                    Date referenceDate,
                                    DayCounter dayCounter)  
      {
          arguments_ = args;

          startTimes_= new InitializedList<double>(args.startDates.Count);
          for (int i = 0; i < startTimes_.Count; ++i)
              startTimes_[i] = dayCounter.yearFraction(referenceDate,
                                                       args.startDates[i]);
          
          endTimes_ = new InitializedList<double>(args.endDates.Count);
          for (int i = 0; i < endTimes_.Count; ++i)
              endTimes_[i] = dayCounter.yearFraction(referenceDate,
                                                     args.endDates[i]);
      }

      public override void reset(int size) {
          values_ = new Vector(size, 0.0);
          adjustValues();
      }

      public override List<double> mandatoryTimes() {
        List<double>  times = startTimes_;
        //copy(endTimes_.begin(), endTimes_.end(),
        //          std::back_inserter(times));
        for (int j = 0; j < endTimes_.Count; j++)
            times.Insert(0, endTimes_[j]);
        return times;
      }

      protected  override void preAdjustValuesImpl()
      {
         for (int i=0; i<startTimes_.Count; i++) {
            if (isOnTime(startTimes_[i])) {
                double end = endTimes_[i];
                double tenor = arguments_.accrualTimes[i];
                DiscretizedDiscountBond bond=new DiscretizedDiscountBond();
                bond.initialize(method(), end);
                bond.rollback(time_);

                CapFloorType type = arguments_.type;
                double gearing = arguments_.gearings[i];
                double nominal = arguments_.nominals[i];

                if ( (type == CapFloorType.Cap) ||
                     (type == CapFloorType.Collar)) {
                    double accrual =(double)( 1.0 + arguments_.capRates[i]*tenor);
                    double strike = 1.0/accrual;
                    for (int j=0; j<values_.size(); j++)
                        values_[j] += nominal*accrual*gearing*
                            Math.Max(strike - bond.values()[j], 0.0);
                }

                if ( (type == CapFloorType.Floor) ||
                     (type == CapFloorType.Collar)) {
                    double accrual =(double)( 1.0 + arguments_.floorRates[i]*tenor);
                    double strike = 1.0/accrual;
                    double mult = (type == CapFloorType.Floor)?1.0:-1.0;
                    for (int j=0; j<values_.size(); j++)
                        values_[j] += nominal*accrual*mult*gearing*
                            Math.Max(bond.values()[j] - strike, 0.0);
                }
            }
         }
      }

      protected  override void postAdjustValuesImpl()
      {
           for (int i=0; i<endTimes_.Count; i++) {
                if (isOnTime(endTimes_[i])) {
                    if (startTimes_[i] < 0.0) {
                        double nominal = arguments_.nominals[i];
                        double accrual = arguments_.accrualTimes[i];
                        double fixing = (double)arguments_.forwards[i];
                        double gearing = arguments_.gearings[i];
                         CapFloorType type = arguments_.type;

                        if (type == CapFloorType.Cap || type == CapFloorType.Collar) {
                            double cap =(double) arguments_.capRates[i];
                            double capletRate = Math.Max(fixing - cap, 0.0);
                            values_ += capletRate*accrual*nominal*gearing;
                        }

                        if (type == CapFloorType.Floor || type == CapFloorType.Collar) {
                            double floor = (double)arguments_.floorRates[i];
                            double floorletRate = Math.Max(floor-fixing, 0.0);
                            if (type == CapFloorType.Floor)
                                values_ += floorletRate*accrual*nominal*gearing;
                            else
                                values_ -= floorletRate*accrual*nominal*gearing;
                        }
                    }
                }
            }
        }
    }
}
