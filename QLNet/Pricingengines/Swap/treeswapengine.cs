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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QLNet
{
/*! \file treeswapengine.hpp
    \brief Numerical lattice engine for swaps
*/
    public class TreeVanillaSwapEngine
    : LatticeShortRateModelEngine<VanillaSwap.Arguments,
                                  VanillaSwap.Results>
    {
       
        private Handle<YieldTermStructure> termStructure_;

        /*! \name Constructors
            \note the term structure is only needed when the short-rate
                  model cannot provide one itself.
        */
        //@{
        public TreeVanillaSwapEngine(ShortRateModel model,
                           int timeSteps,
                           Handle<YieldTermStructure> termStructure)
            : base(model, timeSteps){
            termStructure_ = termStructure;
            termStructure_.registerWith(update);
        }

        public TreeVanillaSwapEngine(ShortRateModel model,
                           TimeGrid timeGrid,
                           Handle<YieldTermStructure> termStructure)
            : base(model, timeGrid){
            termStructure_=termStructure;
            termStructure_.registerWith(update);
        }
        
        public override void calculate(){
            if(base.model_==null)
            throw new ArgumentException("no model specified");

            Date referenceDate;
            DayCounter dayCounter;

            ITermStructureConsistentModel tsmodel =
                (ITermStructureConsistentModel)base.model_;
            try {
                if (tsmodel!=null) {
                    referenceDate = tsmodel.termStructure().link.referenceDate();
                    dayCounter = tsmodel.termStructure().link.dayCounter();
                } else {
                    referenceDate = termStructure_.link.referenceDate();
                    dayCounter = termStructure_.link.dayCounter();
                }
            }
            catch{
                referenceDate = termStructure_.link.referenceDate();
                dayCounter = termStructure_.link.dayCounter();
            }

            DiscretizedSwap swap = new DiscretizedSwap(arguments_, referenceDate, dayCounter);
            List<double> times = swap.mandatoryTimes();
            Lattice lattice ;

            if (lattice_!=null) {
                lattice = lattice_;
            } else {
                TimeGrid timeGrid = new TimeGrid(times, timeSteps_);
                lattice = model_.tree(timeGrid);
            }

            swap.initialize(lattice, times.Last());
            swap.rollback(0.0);

            results_.value = swap.presentValue();
        }
      
    }
}
