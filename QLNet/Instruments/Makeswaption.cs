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
/*! \file makeswaption.hpp
    \brief Helper class to instantiate standard market swaption.
*/
    public class MakeSwaption
    {
        private SwapIndex swapIndex_;
        private Settlement.Type delivery_;
        private VanillaSwap underlyingSwap_;

        private Period optionTenor_;
        private BusinessDayConvention optionConvention_;
        private Date fixingDate_;
        private Date exerciseDate_;
        private Exercise exercise_;

        private double?  strike_;

        IPricingEngine engine_;


        public MakeSwaption(SwapIndex swapIndex,
                            Period optionTenor,
                            double? strike = null)
        {
            swapIndex_ = swapIndex;
            delivery_ = Settlement.Type.Physical;
            optionTenor_ = optionTenor;
            optionConvention_ = BusinessDayConvention.ModifiedFollowing;
            strike_ = strike;
        }
        
        public MakeSwaption(SwapIndex swapIndex,
                            Period optionTenor)
            : this(swapIndex, optionTenor,1) {}

        public MakeSwaption withSettlementType(Settlement.Type delivery){
            delivery_ = delivery;
            return this;
        }
        public MakeSwaption withOptionConvention(BusinessDayConvention bdc){
            optionConvention_ = bdc;
            return this;
        }

        public MakeSwaption withExerciseDate(Date exerciseDate){
            exerciseDate_ = exerciseDate;
            return this;
        }
        public MakeSwaption withPricingEngine(IPricingEngine engine){
            engine_ = engine;
            return this;
        }

        // swap creator
        public static implicit operator Swaption(MakeSwaption o) { return o.value(); }
        
        public Swaption value(){
            Date evaluationDate = Settings.evaluationDate();
            Calendar fixingCalendar = swapIndex_.fixingCalendar();
            fixingDate_ = fixingCalendar.advance(evaluationDate, optionTenor_, optionConvention_);

           if (exerciseDate_ == null) {
                exercise_ =new EuropeanExercise(fixingDate_);
           } 
           else {
                if(exerciseDate_ <= fixingDate_)
                throw new ArgumentException(
                        "exercise date (" + exerciseDate_ + ") must be less "+
                        "than or equal to fixing date (" + fixingDate_ + ")");
                exercise_ = new EuropeanExercise(exerciseDate_);
            }

            double usedStrike;
            if (strike_ == null)
            {
               // ATM on the forecasting curve
               if (!swapIndex_.forwardingTermStructure().empty())
                  throw new ArgumentException(
                         "no forecasting term structure set to " + swapIndex_.name());
               VanillaSwap temp =
                   swapIndex_.underlyingSwap(fixingDate_);
               temp.setPricingEngine(new DiscountingSwapEngine(
                                           swapIndex_.forwardingTermStructure()));
               usedStrike = temp.fairRate();
            }
            else
               usedStrike = strike_.Value;

            BusinessDayConvention bdc = swapIndex_.fixedLegConvention();
            underlyingSwap_ =new MakeVanillaSwap(   swapIndex_.tenor(),
                                                    swapIndex_.iborIndex(), 
                                                    usedStrike)
                .withEffectiveDate(swapIndex_.valueDate(fixingDate_))
                .withFixedLegCalendar(swapIndex_.fixingCalendar())
                .withFixedLegDayCount(swapIndex_.dayCounter())
                .withFixedLegConvention(bdc)
                .withFixedLegTerminationDateConvention(bdc);

           Swaption swaption=new Swaption(underlyingSwap_, exercise_, delivery_);
           swaption.setPricingEngine(engine_);
           return swaption;
        }
    }
}
