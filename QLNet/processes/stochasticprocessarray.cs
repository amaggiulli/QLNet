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
    //! %Array of correlated 1-D stochastic processes
    /*! \ingroup processes */
    public class StochasticProcessArray : StochasticProcess {
        protected List<StochasticProcess1D> processes_;
        protected Matrix sqrtCorrelation_;

        public StochasticProcessArray(List<StochasticProcess1D> processes, Matrix correlation) {
            processes_ = processes;
            sqrtCorrelation_ = MatrixUtilitites.pseudoSqrt(correlation, MatrixUtilitites.SalvagingAlgorithm.Spectral);

            if (processes.Count == 0)
                throw new ApplicationException("no processes given");
            if(correlation.rows() != processes.Count)
                throw new ApplicationException("mismatch between number of processes and size of correlation matrix");
            for (int i=0; i<processes_.Count; i++)
                processes_[i].registerWith(update);
        }


        // stochastic process interface
        public override int size() { return processes_.Count; }

        public override Vector initialValues() {
            Vector tmp = new Vector(size());
            for (int i=0; i<size(); ++i)
                tmp[i] = processes_[i].x0();
            return tmp;
        }

        public override Vector drift(double t, Vector x) {
            Vector tmp = new Vector(size());
            for (int i=0; i<size(); ++i)
                tmp[i] = processes_[i].drift(t, x[i]);
            return tmp;
        }

        public override Vector expectation(double t0, Vector x0, double dt) {
            Vector tmp = new Vector(size());
            for (int i=0; i<size(); ++i)
                tmp[i] = processes_[i].expectation(t0, x0[i], dt);
            return tmp;
        }

        public override Matrix diffusion(double t, Vector x) {
            Matrix tmp = sqrtCorrelation_;
            for (int i=0; i<size(); ++i) {
                double sigma = processes_[i].diffusion(t, x[i]);
                for (int j= 0; j<tmp.columns(); j++) {
                    tmp[i,j] *= sigma;
                }
            }
            return tmp;
        }

        public override Matrix covariance(double t0, Vector x0, double dt) {
            Matrix tmp = stdDeviation(t0, x0, dt);
            return tmp*Matrix.transpose(tmp);
        }

        public override Matrix stdDeviation(double t0, Vector x0, double dt) {
            Matrix tmp = sqrtCorrelation_;
            for (int i=0; i<size(); ++i) {
                double sigma = processes_[i].stdDeviation(t0, x0[i], dt);
                for (int j= 0; j<tmp.columns(); j++) {
                    tmp[i,j] *= sigma;
                }
            }
            return tmp;
        }

        public override Vector apply(Vector x0, Vector dx) {
            Vector tmp = new Vector(size());
            for (int i=0; i<size(); ++i)
                tmp[i] = processes_[i].apply(x0[i],dx[i]);
            return tmp;
        }

        public override Vector evolve(double t0, Vector x0, double dt, Vector dw) {
            Vector dz = sqrtCorrelation_ * dw;

            Vector tmp = new Vector(size());
            for (int i=0; i<size(); ++i)
                tmp[i] = processes_[i].evolve(t0, x0[i], dt, dz[i]);
            return tmp;
        }

        public override double time(Date d) { return processes_[0].time(d); }

        // inspectors
        public StochasticProcess1D process(int i) { return processes_[i]; }
        public Matrix correlation() { return sqrtCorrelation_ * Matrix.transpose(sqrtCorrelation_); }
    }
}
