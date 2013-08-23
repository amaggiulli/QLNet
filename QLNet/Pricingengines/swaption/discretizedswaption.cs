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
    public class DiscretizedSwaption :  DiscretizedOption {
        
        private Swaption.Arguments arguments_;
        private double lastPayment_;
       
        public DiscretizedSwaption(Swaption.Arguments args,
                                    Date referenceDate,
                                    DayCounter dayCounter)
            : base(new DiscretizedSwap(args, referenceDate, dayCounter), args.exercise.type(), new List<double>())
        {
            arguments_=args;
            exerciseTimes_ = new InitializedList<double>(arguments_.exercise.dates().Count);
            for (int i = 0; i < exerciseTimes_.Count; ++i)
                exerciseTimes_[i] =
                    dayCounter.yearFraction(referenceDate,
                                            arguments_.exercise.date(i));

            // Date adjustments can get time vectors out of synch.
            // Here, we try and collapse similar dates which could cause
            // a mispricing.
            for (int i=0; i<arguments_.exercise.dates().Count; i++) {
                Date exerciseDate = arguments_.exercise.date(i);
            for (int j = 0; j < arguments_.fixedPayDates.Count; j++) {
                if (withinNextWeek(exerciseDate,
                                   arguments_.fixedPayDates[j])
                    // coupons in the future are dealt with below
                    && arguments_.fixedResetDates[j] < referenceDate)
                    arguments_.fixedPayDates[j] = exerciseDate;
            }
            for (int j = 0; j < arguments_.fixedResetDates.Count; j++) {
                if (withinPreviousWeek(exerciseDate,
                                       arguments_.fixedResetDates[j]))
                    arguments_.fixedResetDates[j] = exerciseDate;
            }
            for (int j = 0; j < arguments_.floatingResetDates.Count; j++) {
                if (withinPreviousWeek(exerciseDate,
                                       arguments_.floatingResetDates[j]))
                    arguments_.floatingResetDates[j] = exerciseDate;
            }
        }

        double lastFixedPayment =
            dayCounter.yearFraction(referenceDate,
                                    arguments_.fixedPayDates.Last() );
        double lastFloatingPayment =
            dayCounter.yearFraction(referenceDate,
                                    arguments_.floatingPayDates.Last());
        lastPayment_ = Math.Max(lastFixedPayment,lastFloatingPayment);

        underlying_ =   new DiscretizedSwap(arguments_,
                                            referenceDate,
                                            dayCounter);

        }

        public override void reset(int size){ 
            underlying_.initialize(method(), lastPayment_);
            base.reset(size);
        }

        public bool withinPreviousWeek( Date d1,  Date d2){
            return d2 >= d1-7 && d2 <= d1;
        }

        public bool withinNextWeek( Date d1,  Date d2) {
            return d2 >= d1 && d2 <= d1+7;
        }
    }
}
