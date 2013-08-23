/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
  
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

namespace QLNet {
    //! single-factor random walk
    /*! \ingroup mcarlo

        \note the path includes the initial asset value as its first point.
    */
    public class Path : ICloneable, IPath {
        private TimeGrid timeGrid_;
        private Vector values_;

        // required for generics
        public Path() { }

        public Path(TimeGrid timeGrid) : this(timeGrid, new Vector()) { }
        public Path(TimeGrid timeGrid, Vector values) {
            timeGrid_ = timeGrid;
            values_ = (Vector)values.Clone();
            if (values_.empty())
                values_ = new Vector(timeGrid_.size());

            if (values_.size() != timeGrid_.size())
                throw new ApplicationException("different number of times and asset values");
        }

        //! \name inspectors
        public bool empty() { return timeGrid_.empty(); }
        public int length() { return timeGrid_.size(); }

        //! asset value at the \f$ i \f$-th point
        public double this[int i] { get { return values_[i]; } set { values_[i] = value; } }
        public double value(int i) { return values_[i]; }
        
        //! time at the \f$ i \f$-th point
        public double time(int i) { return timeGrid_[i]; }
        
        //! initial asset value
        public double front() { return values_.First(); }
        public void setFront(double value) { values_[0] = value; }
        
        //! final asset value
        public double back() { return values_.Last(); }
        
        //! time grid
        public TimeGrid timeGrid() { return timeGrid_; }

        // ICloneable interface
        public object Clone() {
            Path temp = (Path)this.MemberwiseClone();
            temp.values_ = new Vector(this.values_);
            return temp;
        }
    }
}
