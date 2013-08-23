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
    //! Numerical lattice engine for swaptions
    /*! \ingroup swaptionengines

        \warning This engine is not guaranteed to work if the
                 underlying swap has a start date in the past, i.e.,
                 before today's date. When using this engine, prune
                 the initial part of the swap so that it starts at
                 \f$ t \geq 0 \f$.

        \test calculations are checked against cached results
    */
    public class TreeSwaptionEngine
    : LatticeShortRateModelEngine<Swaption.Arguments,
                                  Swaption.Results>
    {
       
        private Handle<YieldTermStructure> termStructure_;

        /*! \name Constructors
            \note the term structure is only needed when the short-rate
                  model cannot provide one itself.
        */
        //@{
        public TreeSwaptionEngine(ShortRateModel model,
                           int timeSteps)
            : this(model, timeSteps, new Handle<YieldTermStructure>())
        { }
        public TreeSwaptionEngine(ShortRateModel model,
                           int timeSteps,
                           Handle<YieldTermStructure> termStructure)
            : base(model, timeSteps) {
            termStructure_=termStructure;
            termStructure_.registerWith(update);
        }
        public TreeSwaptionEngine(ShortRateModel model,
                                TimeGrid timeGrid)  
            : this(model, timeGrid, new Handle<YieldTermStructure>())
        { }
        public TreeSwaptionEngine(ShortRateModel model,
                           TimeGrid timeGrid,
                           Handle<YieldTermStructure> termStructure)
            : base(model, timeGrid) {
            termStructure_=new Handle<YieldTermStructure>();
            termStructure_=termStructure;
            termStructure_.registerWith(update);
        }
        //@}
        public override void calculate(){

        if(!(base.arguments_.settlementType==Settlement.Type.Physical))
        throw new ArgumentException("cash-settled swaptions not priced with tree engine");
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

        DiscretizedSwaption swaption = new DiscretizedSwaption(arguments_, referenceDate, dayCounter);
        Lattice lattice ;

        if (lattice_!=null) {
            lattice = lattice_;
        } else {
            List<double> times = swaption.mandatoryTimes();
            TimeGrid timeGrid = new TimeGrid(times, timeSteps_);
            lattice = model_.tree(timeGrid);
        }

        List<double> stoppingTimes = new InitializedList<double>(arguments_.exercise.dates().Count);
        for (int i=0; i<stoppingTimes.Count; ++i)
            stoppingTimes[i] =
                dayCounter.yearFraction(referenceDate,
                                        arguments_.exercise.date(i));

        swaption.initialize(lattice, stoppingTimes.Last());

        double nextExercise;
            /*std::find_if(stoppingTimes.begin(),
                          stoppingTimes.end(),
                          std::bind2nd(std::greater_equal<Time>(), 0.0));*/

        List<double> listExercise = new List<double>();
        listExercise.AddRange(stoppingTimes.FindAll(x => x >= 0));
        nextExercise=listExercise[0];
        swaption.rollback(nextExercise);

        results_.value = swaption.presentValue();
    }
      
    }
}
