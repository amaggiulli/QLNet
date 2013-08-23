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
    public interface IConvergenceSteps {
        int initialSamples();
        int nextSamples(int current);
    }

    public class DoublingConvergenceSteps : IConvergenceSteps {
        public DoublingConvergenceSteps() { }

        public int initialSamples() { return 1; }
        public int nextSamples(int current) { return 2 * current + 1; }
    }

    //! statistics class with convergence table
    /*! This class decorates another statistics class adding a
        convergence table calculation. The table tracks the
        convergence of the mean.

        It is possible to specify the number of samples at which the
        mean should be stored by mean of the second template
        parameter; the default is to store \f$ 2^{n-1} \f$ samples at
        the \f$ n \f$-th step. Any passed class must implement the
        following interface:
        \code
        Size initialSamples() const;
        Size nextSamples(Size currentSamples) const;
        \endcode
        as well as a copy constructor.

        \test results are tested against known good values.
    */
    public class ConvergenceStatistics<T> : ConvergenceStatistics<T, DoublingConvergenceSteps> 
            where T : IGeneralStatistics, new() {
        public ConvergenceStatistics(T stats, DoublingConvergenceSteps rule) : base(stats, rule) { }
        public ConvergenceStatistics() : base(new DoublingConvergenceSteps()) { }
        public ConvergenceStatistics(DoublingConvergenceSteps rule) : base(rule) { }
    }

    public class ConvergenceStatistics<T, U> : IGeneralStatistics 
            where T : IGeneralStatistics, new()
            where U : IConvergenceSteps, new() {

        private List<KeyValuePair<int, double>> table_ = new List<KeyValuePair<int,double>>();
        public List<KeyValuePair<int, double>>convergenceTable() { return table_; }

        private U samplingRule_;
        private int nextSampleSize_;

        //public ConvergenceStatistics(T stats, U rule = U());
        public ConvergenceStatistics(T stats, U rule) {
            impl_ = stats;
            samplingRule_ = rule;

            reset();
        }

        public ConvergenceStatistics() : this(new U()) { }
        public ConvergenceStatistics(U rule) {
            samplingRule_ = rule;
            reset();
        }

        public void reset() {
            impl_.reset();
            nextSampleSize_ = samplingRule_.initialSamples();
            table_.Clear();
        }

        public void add(double value) { add(value, 1); }
        public void add(double value, double weight) {
            impl_.add(value, weight);
            if (samples() == nextSampleSize_) {
                table_.Add(new KeyValuePair<int, double>(samples(), mean()));
                nextSampleSize_ = samplingRule_.nextSamples(nextSampleSize_);
            }
        }

        //! adds a sequence of data to the set, with default weight
        public void addSequence(List<double> list) {
            foreach(double v in list) 
                add(v, 1);
        }
        //! adds a sequence of data to the set, each with its weight
        public void addSequence(List<double> data, List<double> weight) {
            for(int i=0; i< data.Count; i++)
                add(data[i], weight[i]);
        }

        #region wrap-up Stat
        protected T impl_ = new T();

        public int samples() { return impl_.samples(); }
        public double mean() { return impl_.mean(); }
        public double min() { return impl_.min(); }
        public double max() { return impl_.max(); }
        public double standardDeviation() { return impl_.standardDeviation(); }
        public double variance() { return impl_.variance(); }
        public double skewness() { return impl_.skewness(); }
        public double kurtosis() { return impl_.kurtosis(); }
        public double percentile(double percent) { return impl_.percentile(percent); }
        public double weightSum() { return impl_.weightSum(); }
        public double errorEstimate() { return impl_.errorEstimate(); }

        public KeyValuePair<double, int> expectationValue(Func<KeyValuePair<double, double>, double> f,
                                                          Func<KeyValuePair<double, double>, bool> inRange) {
            return impl_.expectationValue(f, inRange);
        }
        #endregion
    }
}
