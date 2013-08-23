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
    //! empirical-distribution risk measures
    /*! This class wraps a somewhat generic statistic tool and adds
        a number of risk measures (e.g.: value-at-risk, expected
        shortfall, etc.) based on the data distribution as reported by
        the underlying statistic tool.

        \todo add historical annualized volatility
    */
    public class GenericRiskStatistics<Stat> : IGeneralStatistics where Stat : IGeneralStatistics, new() {
        //typedef typename S::value_type value_type;

        #region wrap-up Stat
        protected Stat impl_ = new Stat();

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

        public void reset() { impl_.reset(); }
        public void add(double value, double weight) { impl_.add(value, weight); }
        public void addSequence(List<double> data, List<double> weight) { impl_.addSequence(data, weight); }

        public KeyValuePair<double, int> expectationValue(Func<KeyValuePair<double, double>, double> f,
                                                          Func<KeyValuePair<double, double>, bool> inRange) {
            return impl_.expectationValue(f, inRange);
        }
        #endregion


        /*! returns the variance of observations below the mean,
            \f[ \frac{N}{N-1}
                \mathrm{E}\left[ (x-\langle x \rangle)^2 \;|\;
                                  x < \langle x \rangle \right]. \f]

            See Markowitz (1959).
        */
        public double semiVariance() { return regret(this.mean()); }

        /*! returns the semi deviation, defined as the square root of the semi variance. */
        public double semiDeviation() { return Math.Sqrt(semiVariance()); }

        /*! returns the variance of observations below 0.0,
            \f[ \frac{N}{N-1}
                \mathrm{E}\left[ x^2 \;|\; x < 0\right]. \f]
        */
        public double downsideVariance() { return regret(0.0); }

        /*! returns the downside deviation, defined as the square root of the downside variance. */
        public double downsideDeviation() { return Math.Sqrt(downsideVariance()); }

        /*! returns the variance of observations below target,
            \f[ \frac{N}{N-1}
                \mathrm{E}\left[ (x-t)^2 \;|\;
                                  x < t \right]. \f]

            See Dembo and Freeman, "The Rules Of Risk", Wiley (2001).
        */
        public double regret(double target) {
            // average over the range below the target
            KeyValuePair<double, int> result = expectationValue(z => Math.Pow(z.Key - target, 2),
                                                                z => z.Key < target);
            double x = result.Key;
            int N = result.Value;
            if (!(N > 1)) throw new ApplicationException("samples under target <= 1, unsufficient");
            return (N/(N-1.0))*x;
        }

        //! potential upside (the reciprocal of VAR) at a given percentile
        public double potentialUpside(double centile) {
            if (!(centile < 1.0 && centile >= 0.9))
                throw new ApplicationException("percentile (" + centile + ") out of range [0.9, 1)");

            // potential upside must be a gain, i.e., floored at 0.0
            return Math.Max(percentile(centile), 0.0);
        }

        //! value-at-risk at a given percentile
        public double valueAtRisk(double centile) {
            if (!(centile < 1.0 && centile >= 0.9))
                throw new ApplicationException("percentile (" + centile + ") out of range [0.9, 1)");

            // must be a loss, i.e., capped at 0.0 and negated
            return -Math.Min(percentile(1.0 - centile), 0.0);
        }

        //! expected shortfall at a given percentile
        /*! returns the expected loss in case that the loss exceeded
            a VaR threshold,

            \f[ \mathrm{E}\left[ x \;|\; x < \mathrm{VaR}(p) \right], \f]

            that is the average of observations below the
            given percentile \f$ p \f$.
            Also know as conditional value-at-risk.

            See Artzner, Delbaen, Eber and Heath,
            "Coherent measures of risk", Mathematical Finance 9 (1999)
        */
        public double expectedShortfall(double centile) {
            if (!(centile < 1.0 && centile >= 0.9))
                throw new ApplicationException("percentile (" + centile + ") out of range [0.9, 1)");

            if (samples() == 0) throw new ApplicationException("empty sample set");

            double target = -valueAtRisk(centile);
            KeyValuePair<double, int> result = expectationValue(z => z.Key, z => z.Key < target);
            double x = result.Key;
            int N = result.Value;
            if (N == 0) throw new ApplicationException("no data below the target");
            // must be a loss, i.e., capped at 0.0 and negated
            return -Math.Min(x, 0.0);
        }

        /*! probability of missing the given target, defined as
            \f[ \mathrm{E}\left[ \Theta \;|\; (-\infty,\infty) \right] \f]
            where
            \f[ \Theta(x) = \left\{
                \begin{array}{ll}
                1 & x < t \\
                0 & x \geq t
                \end{array}
                \right. \f]
        */
        public double shortfall(double target) {
            if (samples() == 0) throw new ApplicationException("empty sample set");
            return expectationValue(x => x.Key < target ? 1 : 0, 
                                    x => true).Key;
        }

        /*! averaged shortfallness, defined as
            \f[ \mathrm{E}\left[ t-x \;|\; x<t \right] \f]
        */
        public double averageShortfall(double target) {
            KeyValuePair<double, int> result = expectationValue(z => target - z.Key, z => z.Key < target);
            double x = result.Key;
            int N = result.Value;
            if (N == 0) throw new ApplicationException("no data below the target");
            return x;
        }
    }

    //! default risk measures tool
    /*! \test the correctness of the returned values is tested by checking them against numerical calculations. */
    //typedef GenericRiskStatistics<GaussianStatistics> RiskStatistics;
    public class RiskStatistics : GenericRiskStatistics<GaussianStatistics> {
        public double gaussianPercentile(double value) {
            return ((GaussianStatistics)impl_).gaussianPercentile(value);
        }
        public double gaussianPotentialUpside(double value) {
            return ((GaussianStatistics)impl_).gaussianPotentialUpside(value);
        }
        public double gaussianValueAtRisk(double value) {
            return ((GaussianStatistics)impl_).gaussianValueAtRisk(value);
        }
        public double gaussianExpectedShortfall(double value) {
            return ((GaussianStatistics)impl_).gaussianExpectedShortfall(value);
        }
        public double gaussianShortfall(double value) {
            return ((GaussianStatistics)impl_).gaussianShortfall(value);
        }
        public double gaussianAverageShortfall(double value) {
            return ((GaussianStatistics)impl_).gaussianAverageShortfall(value);
        }
        public double gaussianRegret(double value) {
            return ((GaussianStatistics)impl_).gaussianRegret(value);
        }
        public double gaussianDownsideVariance() {
            return ((GaussianStatistics)impl_).gaussianDownsideVariance();
        }
    }

    //! default statistics tool
    /*! \test the correctness of the returned values is tested by checking them against numerical calculations. */
    public class Statistics : RiskStatistics { }
}
