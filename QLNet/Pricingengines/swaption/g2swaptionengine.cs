/*
 Copyright (C) 2010 Philippe Real (ph_real@hotmail.com)
  
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

/*! \file g2swaptionengine.hpp
    \brief Swaption pricing engine for two-factor additive Gaussian Model G2++
*/


using System;

namespace QLNet
{
    //! %Swaption priced by means of the Black formula
    /*! \ingroup swaptionengines

        \warning The engine assumes that the exercise date equals the
                 start date of the passed swap.
    */
    public class G2SwaptionEngine : GenericModelEngine<G2, Swaption.Arguments,
                                                           Swaption.Results> {
        double range_;
        int intervals_;
      
        // range is the number of standard deviations to use in the
        // exponential term of the integral for the european swaption.
        // intervals is the number of intervals to use in the integration.
        public G2SwaptionEngine(G2 model,
                         double range,
                         int intervals)
        : base(model){

            range_ = range;
            intervals_ = intervals;
        }

        public override void calculate() {
            if(!(arguments_.settlementType == Settlement.Type.Physical))
                       throw new ApplicationException("cash-settled swaptions not priced with G2 engine");

            // adjust the fixed rate of the swap for the spread on the
            // floating leg (which is not taken into account by the
            // model)
            VanillaSwap swap = arguments_.swap;
            swap.setPricingEngine(new DiscountingSwapEngine(model_.termStructure()));
            double correction = swap.spread *
                Math.Abs(swap.floatingLegBPS() / swap.fixedLegBPS());
            double fixedRate = swap.fixedRate - correction;

            results_.value =  model_.swaption(arguments_, fixedRate,
                                               range_, intervals_);
        }
 
    }

}