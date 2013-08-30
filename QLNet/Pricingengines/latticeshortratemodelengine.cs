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

namespace QLNet{

    //! Engine for a short-rate model specialized on a lattice
    /*! Derived engines only need to implement the <tt>calculate()</tt>
        method
    */
    public class LatticeShortRateModelEngine<ArgumentsType, ResultsType>
                : GenericModelEngine<ShortRateModel, ArgumentsType,ResultsType>
                where ArgumentsType : IPricingEngineArguments, new()
                where ResultsType :  IPricingEngineResults, new()
       
    {
        protected TimeGrid timeGrid_;
        protected int timeSteps_;
        protected Lattice lattice_;

        public LatticeShortRateModelEngine( ShortRateModel model,
                                            int timeSteps)
            : base(model)
        {
            timeSteps_=timeSteps; 
            if(!(timeSteps>0))
                   throw new ArgumentException("timeSteps must be positive, " + timeSteps +
                   " not allowed");
        }

        public LatticeShortRateModelEngine( ShortRateModel model,
                                            TimeGrid timeGrid)
        : base(model) {
            timeGrid_ = new TimeGrid(timeGrid.Last(),timeGrid.size()-1 /*timeGrid.dt(1) - timeGrid.dt(0)*/);
            timeGrid_=timeGrid;
            timeSteps_=0;
            lattice_=this.model_.tree(timeGrid);
            //model_.registerWith(update); 
        }

        /*public override void update()
        {
            if (!timeGrid_.empty())
                lattice_ = this.model_.tree(timeGrid_);
            this.notifyObservers();
        }*/

        #region PricingEngine
       /* protected OneAssetOption.Arguments arguments_ = new OneAssetOption.Arguments();
        protected OneAssetOption.Results results_ = new OneAssetOption.Results();

        public IPricingEngineArguments getArguments() { return arguments_; }
        public IPricingEngineResults getResults() { return results_; }
        public void reset() { results_.reset(); }
        */
        #region Observer & Observable
        public override void update()
        {
            if (!timeGrid_.empty())
                lattice_ = this.model_.tree(timeGrid_);
            notifyObservers();
        }
        #endregion
        #endregion
    }
 
}