/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
  
 This file is part of QLNet Project https://github.com/amaggiulli/qlnet

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

namespace QLNet {
    
    //! base class for early exercise path pricers
    /*! Returns the value of an option on a given path and given time.
    */
    public static class EarlyExerciseTraits<PathType> where PathType : IPath {
        //typedef Real StateType;
        public static int pathLength(PathType path) { return path.length(); }
    }


    // template<class PathType, class TimeType=Size, class ValueType=Real>
    public interface IEarlyExercisePathPricer<PathType, StateType> where PathType : IPath {
        // typedef typename EarlyExerciseTraits<PathType>::StateType StateType;

        double value(PathType path, int t);

        StateType state(PathType path, int t);
        List<Func<StateType, double>> basisSystem();
    }
}
