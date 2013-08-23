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
    //! Longstaff-Schwarz path pricer for early exercise options
    /*! References:

        Francis Longstaff, Eduardo Schwartz, 2001. Valuing American Options
        by Simulation: A Simple Least-Squares Approach, The Review of
        Financial Studies, Volume 14, No. 1, 113-147

        \ingroup mcarlo

        \test the correctness of the returned value is tested by
              reproducing results available in web/literature
    */
    public class LongstaffSchwartzPathPricer<PathType> : PathPricer<PathType> where PathType : IPath {
        protected bool  calibrationPhase_;
        protected IEarlyExercisePathPricer<PathType, double> pathPricer_;

        protected List<Vector> coeff_;
        protected List<double> dF_;

        protected List<PathType> paths_ = new List<PathType>();
        protected List<Func<double, double>> v_;

        public LongstaffSchwartzPathPricer(TimeGrid times, IEarlyExercisePathPricer<PathType, double> pathPricer,
                                           YieldTermStructure termStructure) {
            calibrationPhase_ = true;
            pathPricer_ = pathPricer;
            coeff_ = new InitializedList<Vector>(times.size()-1);
            dF_ = new InitializedList<double>(times.size()-1);
            v_ = pathPricer_.basisSystem();

            for (int i=0; i<times.size()-1; ++i) {
                dF_[i] =   termStructure.discount(times[i+1])
                         / termStructure.discount(times[i]);
            }
        }
        

        public double value(PathType path) {
            if (calibrationPhase_) {
                // store paths for the calibration
                paths_.Add((PathType)path.Clone());
                // result doesn't matter
                return 0.0;
            }

            int len = EarlyExerciseTraits<PathType>.pathLength(path);
            double price = pathPricer_.value(path, len-1);
            for (int i = len - 2; i > 0; --i) {
                price*=dF_[i];

                double exercise = pathPricer_.value(path, i);
                if (exercise > 0.0) {
                    double regValue  = pathPricer_.state(path, i);

                    double continuationValue = 0.0;
                    for (int l = 0; l < v_.Count; ++l) {
                        continuationValue += coeff_[i][l] * v_[l](regValue);
                    }

                    if (continuationValue < exercise) {
                        price = exercise;
                    }
                }
            }

            return price*dF_[0];
        }

        public void calibrate() {
            int n = paths_.Count;
            Vector prices = new Vector(n), exercise = new Vector(n);
            int len = EarlyExerciseTraits<PathType>.pathLength(paths_[0]);

            for(int i = 0; i<paths_.Count; i++)
                prices[i] = pathPricer_.value(paths_[i], len-1);

            for (int i=len-2; i>0; --i) {
                List<double> y = new List<double>();
                List<double> x = new List<double>();

                //roll back step
                for (int j=0; j<n; ++j) {
                    exercise[j]=pathPricer_.value(paths_[j], i);

                    if (exercise[j]>0.0) {
                        x.Add(pathPricer_.state(paths_[j], i));
                        y.Add(dF_[i]*prices[j]);
                    }
                }

                if (v_.Count <= x.Count) {
                    coeff_[i] = new LinearLeastSquaresRegression<double>(x, y, v_).coefficients();
                }
                else {
                // if number of itm paths is smaller then the number of
                // calibration functions -> no early exercise
                    coeff_[i] = new Vector(v_.Count);
                }

                for (int j=0, k=0; j<n; ++j) {
                    prices[j]*=dF_[i];
                    if (exercise[j]>0.0) {
                        double continuationValue = 0.0;
                        for (int l = 0; l < v_.Count; ++l) {
                            continuationValue += coeff_[i][l] * v_[l](x[k]);
                        }
                        if (continuationValue < exercise[j]) {
                            prices[j] = exercise[j];
                        }
                        ++k;
                    }
                }
            }

            // remove calibration paths
            paths_.Clear();
            // entering the calculation phase
            calibrationPhase_ = false;
        }
    }
}
