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
using System.Reflection;

namespace QLNet {
    //! Statistics analysis of N-dimensional (sequence) data
    /*! It provides 1-dimensional statistics as discrepancy plus
        N-dimensional (sequence) statistics (e.g. mean,
        variance, skewness, kurtosis, etc.) with one component for each
        dimension of the sample space.

        For most of the statistics this class relies on
        the StatisticsType underlying class to provide 1-D methods that
        will be iterated for all the components of the N-D data. These
        lifted methods are the union of all the methods that might be
        requested to the 1-D underlying StatisticsType class, with the
        usual compile-time checks provided by the template approach.

        \test the correctness of the returned values is tested by
              checking them against numerical calculations.
    */
    public class GenericSequenceStatistics<S> where S : IGeneralStatistics, new() {
        protected int dimension_;
        public int size() { return dimension_; }

        protected List<S> stats_;
        protected List<double> results_;
        protected Matrix quadraticSum_;

        //public GenericSequenceStatistics(int dimension = 0) {
        public GenericSequenceStatistics(int dimension) {
            dimension_ = 0;
            reset(dimension);
        }

        //! returns the covariance Matrix
        public Matrix covariance() {
            double sampleWeight = weightSum();
            if (!(sampleWeight > 0.0)) throw new ApplicationException("sampleWeight=0, unsufficient");

            double sampleNumber = samples();
            if (!(sampleNumber > 1.0)) throw new ApplicationException("sample number <=1, unsufficient");

            List<double> m = mean();
            double inv = 1.0/sampleWeight;

            Matrix result = inv*quadraticSum_;
            result -= Matrix.outerProduct(m, m);

            result *= (sampleNumber/(sampleNumber-1.0));
            return result;
        }
        //! returns the correlation Matrix
        public Matrix correlation() {
            Matrix correlation = covariance();
            Vector variances = correlation.diagonal();
            for (int i=0; i<dimension_; i++){
                for (int j=0; j<dimension_; j++){
                    if (i==j) {
                        if (variances[i]==0.0) {
                            correlation[i,j] = 1.0;
                        } else {
                            correlation[i,j] *= 1.0/Math.Sqrt(variances[i]*variances[j]);
                        }
                    } else {
                        if (variances[i]==0.0 && variances[j]==0) {
                            correlation[i,j] = 1.0;
                        } else if (variances[i]==0.0 || variances[j]==0.0) {
                            correlation[i,j] = 0.0;
                        } else {
                            correlation[i,j] *= 1.0/Math.Sqrt(variances[i]*variances[j]);
                        }
                    }
                } // j for
            } // i for
            return correlation;
        }

        //! \name 1-D inspectors lifted from underlying statistics class
        public int samples() { return (stats_.Count == 0) ? 0 : stats_[0].samples(); }
        public double weightSum(){ return (stats_.Count == 0) ? 0.0 : stats_[0].weightSum(); }

        //@}
        //! \name N-D inspectors lifted from underlying statistics class
        // no argument list
        private List<double> noArg(string method) {
            // do not check for null - in this case we throw anyways
            for (int i = 0; i < dimension_; i++) {
                MethodInfo methodInfo = stats_[i].GetType().GetMethod(method);
                results_[i] = (double)methodInfo.Invoke(stats_[i], new object[] { });
            }
            return results_;
        }
        // single argument list
        private List<double> singleArg(double x, string method) {
            // do not check for null - in this case we throw anyways
            for (int i = 0; i < dimension_; i++) {
                MethodInfo methodInfo = stats_[i].GetType().GetMethod(method);
                results_[i] = (double)methodInfo.Invoke(stats_[i], new object[] { x });
            }
            return results_;
        }
        
        // void argument list
        public List<double> mean() {
            for (int i=0; i<dimension_; i++) results_[i] = stats_[i].mean();
            return results_;
        }
        public List<double> variance() {
            for (int i=0; i<dimension_; i++) results_[i] = stats_[i].variance();
            return results_;
        }
        public List<double> standardDeviation() {
            for (int i=0; i<dimension_; i++) results_[i] = stats_[i].standardDeviation();
            return results_;
        }
        public List<double> downsideVariance() { return noArg("downsideVariance"); }
        public List<double> downsideDeviation() { return noArg("downsideDeviation"); }
        public List<double> semiVariance() { return noArg("semiVariance"); }
        public List<double> semiDeviation() { return noArg("semiDeviation"); }
        public List<double> errorEstimate() { return noArg("errorEstimate"); }
        public List<double> skewness() {
            for (int i=0; i<dimension_; i++) results_[i] = stats_[i].skewness();
            return results_;
        }
        public List<double> kurtosis() {
            for (int i=0; i<dimension_; i++) results_[i] = stats_[i].kurtosis();
            return results_;
        }
        public List<double> min() {
            for (int i=0; i<dimension_; i++) results_[i] = stats_[i].min();
            return results_;
        }
        public List<double> max() {
            for (int i=0; i<dimension_; i++) results_[i] = stats_[i].max();
            return results_;
        }

        // single argument list
        public List<double> gaussianPercentile(double x) { return singleArg(x, "gaussianPercentile"); }
        public List<double> percentile(double x) {
            for (int i=0; i<dimension_; i++) results_[i] = stats_[i].percentile(x);
            return results_;
        }
        public List<double> gaussianPotentialUpside(double x) { return singleArg(x, "gaussianPotentialUpside"); }
        public List<double> potentialUpside(double x) { return singleArg(x, "potentialUpside"); }
        public List<double> gaussianValueAtRisk(double x) { return singleArg(x, "gaussianValueAtRisk"); }
        public List<double> valueAtRisk(double x) { return singleArg(x, "valueAtRisk"); }
        public List<double> gaussianExpectedShortfall(double x) { return singleArg(x, "gaussianExpectedShortfall"); }
        public List<double> expectedShortfall(double x) { return singleArg(x, "expectedShortfall"); }
        public List<double> gaussianShortfall(double x) { return singleArg(x, "gaussianShortfall"); }
        public List<double> shortfall(double x) { return singleArg(x, "shortfall"); }
        public List<double> gaussianAverageShortfall(double x) { return singleArg(x, "gaussianAverageShortfall"); }
        public List<double> averageShortfall(double x) { return singleArg(x, "averageShortfall"); }
        public List<double> regret(double x) { return singleArg(x, "regret"); }


        //! \name Modifiers
        //public void reset(Size dimension = 0) {
        public virtual void reset(int dimension) {
            // (re-)initialize
            if (dimension > 0) {
                if (dimension == dimension_) {
                    for (int i=0; i<dimension_; ++i)
                        stats_[i].reset();
                } else {
                    dimension_ = dimension;
                    stats_ = new InitializedList<S>(dimension);
                    results_ = new InitializedList<double>(dimension);
                }
                quadraticSum_ = new Matrix(dimension_, dimension_, 0.0);
            } else {
                dimension_ = dimension;
            }
        }

        //philippe2009_16
        public virtual void add(List<double> begin) { add(begin, 1); }

        //public void add(S sample, double weight = 1.0) {
        public virtual void add(List<double> begin, double weight) {
            if (dimension_ == 0) {
                // stat wasn't initialized yet
                int dimension = begin.Count;
                if(!(dimension>0)) throw new ApplicationException("sample error: end<=begin");
                reset(dimension);
            }

            if (begin.Count != dimension_) 
                throw new ApplicationException("sample size mismatch: " + dimension_ +
                       " required, " + begin.Count + " provided");

            quadraticSum_ += weight * Matrix.outerProduct(begin, begin);

            for (int i=0; i<dimension_; ++i)
                stats_[i].add(begin[i], weight);        
        }
    }

    //! default multi-dimensional statistics tool
    /*! \test the correctness of the returned values is tested by
              checking them against numerical calculations.
    */
    // typedef GenericSequenceStatistics<Statistics> SequenceStatistics;
    public class SequenceStatistics : GenericSequenceStatistics<RiskStatistics> {
        public SequenceStatistics(int dimension) : base(dimension) { }
    }
}
