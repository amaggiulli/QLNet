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
*/using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// ===========================================================================
// NOTE: The following copyright notice applies to the original code,
//
// Copyright (C) 2002 Peter Jдckel "Monte Carlo Methods in Finance".
// All rights reserved.
//
// Permission to use, copy, modify, and distribute this software is freely
// granted, provided that this notice is preserved.
// ===========================================================================
namespace QLNet {
    //! Builds Wiener process paths using Gaussian variates
    /*! This class generates normalized (i.e., unit-variance) paths as
        sequences of variations. In order to obtain the actual path of
        the underlying, the returned variations must be multiplied by
        the integrated variance (including time) over the
        corresponding time step.

        \ingroup mcarlo
    */
    public class BrownianBridge {
        private int size_;
        public int size() { return size_; }

        private List<double> t_;
        public List<double> times() { return t_; }

        private List<double> sqrtdt_;
        private List<int> bridgeIndex_, leftIndex_, rightIndex_;
        private List<double> leftWeight_, rightWeight_, stdDev_;


        //! unit-time path
        public BrownianBridge(int steps) {
            size_ = steps;
            t_ = new InitializedList<double>(size_);
            sqrtdt_ = new InitializedList<double>(size_);
            bridgeIndex_ = new InitializedList<int>(size_);
            leftIndex_ = new InitializedList<int>(size_);
            rightIndex_ = new InitializedList<int>(size_);
            leftWeight_ = new InitializedList<double>(size_);
            rightWeight_ = new InitializedList<double>(size_);
            stdDev_ = new InitializedList<double>(size_);
            for (int i=0; i<size_; ++i)
                t_[i] = i+1;
            initialize();
        }

        //! generic times
        /*! \note the starting time of the path is assumed to be 0 and must not be included */
        public BrownianBridge(List<double> times) {
            size_ = times.Count;
            t_ = new InitializedList<double>(size_);
            sqrtdt_ = new InitializedList<double>(size_);
            bridgeIndex_ = new InitializedList<int>(size_);
            leftIndex_ = new InitializedList<int>(size_);
            rightIndex_ = new InitializedList<int>(size_);
            leftWeight_ = new InitializedList<double>(size_);
            rightWeight_ = new InitializedList<double>(size_);
            stdDev_ = new InitializedList<double>(size_);
            initialize();
        }

        //! generic times
        public BrownianBridge(TimeGrid timeGrid) {
            size_ = timeGrid.size()-1;
            t_ = new InitializedList<double>(size_);
            sqrtdt_ = new InitializedList<double>(size_);
            sqrtdt_ = new InitializedList<double>(size_);
            bridgeIndex_ = new InitializedList<int>(size_);
            leftIndex_ = new InitializedList<int>(size_);
            rightIndex_ = new InitializedList<int>(size_);
            leftWeight_ = new InitializedList<double>(size_);
            rightWeight_ = new InitializedList<double>(size_);
            stdDev_ = new InitializedList<double>(size_);
            for (int i=0; i<size_; ++i)
                t_[i] = timeGrid[i+1];
            initialize();
        }


        private void initialize() {
            sqrtdt_[0] = Math.Sqrt(t_[0]);
            for (int i=1; i<size_; ++i)
                sqrtdt_[i] = Math.Sqrt(t_[i]-t_[i-1]);

            // map is used to indicate which points are already constructed.
            // If map[i] is zero, path point i is yet unconstructed.
            // map[i]-1 is the index of the variate that constructs
            // the path point # i.
            List<int> map = new InitializedList<int>(size_);

            //  The first point in the construction is the global step.
            map[size_-1] = 1;
            //  The global step is constructed from the first variate.
            bridgeIndex_[0] = size_-1;
            //  The variance of the global step
            stdDev_[0] = Math.Sqrt(t_[size_-1]);
            //  The global step to the last point in time is special.
            leftWeight_[0] = rightWeight_[0] = 0.0;
            for (int j=0, i=1; i<size_; ++i) {
                // Find the next unpopulated entry in the map.
                while (map[j] != 0)
                    ++j;
                int k = j;
                // Find the next populated entry in the map from there.
                while (map[k] == 0)
                    ++k;
                // l-1 is now the index of the point to be constructed next.
                int l = j + ((k-1-j)>>1);
                map[l] = i;
                // The i-th Gaussian variate will be used to set point l-1.
                bridgeIndex_[i] = l;
                leftIndex_[i]   = j;
                rightIndex_[i]  = k;
                if (j != 0) {
                    leftWeight_[i]= (t_[k]-t_[l])/(t_[k]-t_[j-1]);
                    rightWeight_[i] = (t_[l]-t_[j-1])/(t_[k]-t_[j-1]);
                    stdDev_[i] =
                        Math.Sqrt(((t_[l]-t_[j-1])*(t_[k]-t_[l]))
                                  /(t_[k]-t_[j-1]));
                } else {
                    leftWeight_[i]  = (t_[k]-t_[l])/t_[k];
                    rightWeight_[i] =  t_[l]/t_[k];
                    stdDev_[i] = Math.Sqrt(t_[l] * (t_[k] - t_[l]) / t_[k]);
                }
                j=k+1;
                if (j>=size_)
                    j=0;    //  wrap around
            }
        }

        //! \name Brownian-bridge constructor
        public void transform(List<double> begin, List<double> output) {
            if (begin.Count == 0) throw new ApplicationException("invalid sequence");
            if (begin.Count != size_) throw new ApplicationException("incompatible sequence size");
            // We use output to store the path...
            output[size_-1] = stdDev_[0] * begin[0];
            for (int i=1; i<size_; ++i) {
                int j = leftIndex_[i];
                int k = rightIndex_[i];
                int l = bridgeIndex_[i];
                if (j != 0) {
                    output[l] =
                        leftWeight_[i] * output[j-1] +
                        rightWeight_[i] * output[k]   +
                        stdDev_[i] * begin[i];
                } else {
                    output[l] =
                        rightWeight_[i] * output[k]   +
                        stdDev_[i] * begin[i];
                }
            }
            // ...after which, we calculate the variations and
            // normalize to unit times
            for (int i = size_ - 1; i >= 1; --i) {
                output[i] -= output[i-1];
                output[i] /= sqrtdt_[i];
            }
            output[0] /= sqrtdt_[0];
        }
    }
}
