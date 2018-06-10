/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)

 This file is part of QLNet Project https://github.com/amaggiulli/qlnet

 QLNet is free software: you can redistribute it and/or modify it
 under the terms of the QLNet license.  You should have received a
 copy of the license along with this program; if not, license is
 available at <https://github.com/amaggiulli/QLNet/blob/develop/LICENSE>.

 QLNet is a based on QuantLib, a free-software/open-source library
 for financial quantitative analysts and developers - http://quantlib.org/
 The QuantLib license is available online at http://quantlib.org/license.shtml.

 This program is distributed in the hope that it will be useful, but WITHOUT
 ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
 FOR A PARTICULAR PURPOSE.  See the license for more details.
*/
using System;
using System.Collections.Generic;

namespace QLNet
{
   //! Statistics tool based on incremental accumulation
   /*! It can accumulate a set of data and return statistics (e.g: mean,
       variance, skewness, kurtosis, error estimation, etc.)

       \warning high moments are numerically unstable for high
                average/standardDeviation ratios.
   */
   public class IncrementalStatistics : IGeneralStatistics
   {
      protected int sampleNumber_, downsideSampleNumber_;
      protected double sampleWeight_, downsideSampleWeight_;
      protected double sum_, quadraticSum_, downsideQuadraticSum_, cubicSum_, fourthPowerSum_;
      protected double min_, max_;

      public IncrementalStatistics() { reset(); }

      #region required IGeneralStatistics methods not supported by this class
      public KeyValuePair<double, int> expectationValue(Func<KeyValuePair<double, double>, double> f,
                                                        Func<KeyValuePair<double, double>, bool> inRange)
      {
         throw new NotSupportedException();
      }
      public double percentile(double percent) { throw new NotSupportedException(); }
      #endregion

      //! number of samples collected
      public int samples() { return sampleNumber_; }

      //! sum of data weights
      public double weightSum() { return sampleWeight_; }

      /*! returns the mean, defined as
          \f[ \langle x \rangle = \frac{\sum w_i x_i}{\sum w_i}. \f]
      */
      public double mean()
      {
         Utils.QL_REQUIRE(sampleWeight_ > 0.0, () => "sampleWeight_=0, insufficient");
         return sum_ / sampleWeight_;
      }

      /*! returns the variance, defined as
          \f[ \frac{N}{N-1} \left\langle \left(
              x-\langle x \rangle \right)^2 \right\rangle. \f]
      */
      public double variance()
      {
         Utils.QL_REQUIRE(sampleWeight_ > 0.0, () => "sampleWeight_=0, insufficient");
         Utils.QL_REQUIRE(sampleNumber_ > 1, () => "sample number <=1, insufficient");

         double m = mean();
         double v = quadraticSum_ / sampleWeight_;
         v -= m * m;
         v *= sampleNumber_ / (sampleNumber_ - 1.0);

         Utils.QL_REQUIRE(v >= 0.0, () => "negative variance (" + v + ")");
         return v;
      }

      /*! returns the standard deviation \f$ \sigma \f$, defined as the square root of the variance. */
      public double standardDeviation() { return Math.Sqrt(variance()); }

      /*! returns the downside variance
      */
      public double downsideVariance()
      {
         if (downsideSampleWeight_.IsEqual(0.0))
         {
            Utils.QL_REQUIRE(sampleWeight_ > 0.0, () => "sampleWeight_=0, insufficient");
            return 0.0;
         }

         Utils.QL_REQUIRE(downsideSampleNumber_ > 1, () => "sample number below zero <=1, insufficient");
         return (downsideSampleNumber_ / (downsideSampleNumber_ - 1.0)) *
                (downsideQuadraticSum_ / downsideSampleWeight_);
      }


      /*! returns the error estimate \f$ \epsilon \f$, defined as the
       * square root of the ratio of the variance to the number of samples. */
      public double errorEstimate()
      {
         double var = variance();
         Utils.QL_REQUIRE(samples() > 0, () => "empty sample set");
         return Math.Sqrt(var / samples());
      }

      /*! returns the skewness, defined as
          \f[ \frac{N^2}{(N-1)(N-2)} \frac{\left\langle \left(
              x-\langle x \rangle \right)^3 \right\rangle}{\sigma^3}. \f]
          The above evaluates to 0 for a Gaussian distribution.
      */
      public double skewness()
      {
         Utils.QL_REQUIRE(sampleNumber_ > 2, () => "sample number <=2, insufficient");

         double s = standardDeviation();
         if (s.IsEqual(0.0))
            return 0.0;

         double m = mean();
         double result = cubicSum_ / sampleWeight_;
         result -= 3.0 * m * (quadraticSum_ / sampleWeight_);
         result += 2.0 * m * m * m;
         result /= s * s * s;
         result *= sampleNumber_ / (sampleNumber_ - 1.0);
         result *= sampleNumber_ / (sampleNumber_ - 2.0);
         return result;
      }

      /*! returns the excess kurtosis
          The above evaluates to 0 for a Gaussian distribution.
      */
      public double kurtosis()
      {
         Utils.QL_REQUIRE(sampleNumber_ > 3, () => "sample number <=3, insufficient");

         double m = mean();
         double v = variance();

         double c = (sampleNumber_ - 1.0) / (sampleNumber_ - 2.0);
         c *= (sampleNumber_ - 1.0) / (sampleNumber_ - 3.0);
         c *= 3.0;

         if (v.IsEqual(0.0))
            return c;

         double result = fourthPowerSum_ / sampleWeight_;
         result -= 4.0 * m * (cubicSum_ / sampleWeight_);
         result += 6.0 * m * m * (quadraticSum_ / sampleWeight_);
         result -= 3.0 * m * m * m * m;
         result /= v * v;
         result *= sampleNumber_ / (sampleNumber_ - 1.0);
         result *= sampleNumber_ / (sampleNumber_ - 2.0);
         result *= (sampleNumber_ + 1.0) / (sampleNumber_ - 3.0);

         return result - c;
      }

      /*! returns the minimum sample value */
      public double min()
      {
         Utils.QL_REQUIRE(samples() > 0, () => "empty sample set");
         return min_;
      }

      /*! returns the maximum sample value */
      public double max()
      {
         Utils.QL_REQUIRE(samples() > 0, () => "empty sample set");
         return max_;
      }

      /*! returns the downside deviation, defined as the square root of the downside variance. */
      public double downsideDeviation() { return Math.Sqrt(downsideVariance()); }


      // Modifiers
      //! adds a datum to the set, possibly with a weight
      /*! \pre weight must be positive or null */
      public void add
         (double value) { add(value, 1); }
      public void add
         (double value, double weight)
      {
         Utils.QL_REQUIRE(weight >= 0.0, () => "negative weight (" + weight + ") not allowed");

         int oldSamples = sampleNumber_;
         sampleNumber_++;
         Utils.QL_REQUIRE(sampleNumber_ > oldSamples, () => "maximum number of samples reached");

         sampleWeight_ += weight;

         double temp = weight * value;
         sum_ += temp;
         temp *= value;
         quadraticSum_ += temp;
         if (value < 0.0)
         {
            downsideQuadraticSum_ += temp;
            downsideSampleNumber_++;
            downsideSampleWeight_ += weight;
         }
         temp *= value;
         cubicSum_ += temp;
         temp *= value;
         fourthPowerSum_ += temp;
         if (oldSamples == 0)
         {
            min_ = max_ = value;
         }
         else
         {
            min_ = Math.Min(value, min_);
            max_ = Math.Max(value, max_);
         }
      }

      //! resets the data to a null set
      public void reset()
      {
         min_ = double.MaxValue;
         max_ = double.MinValue;
         sampleNumber_ = 0;
         downsideSampleNumber_ = 0;
         sampleWeight_ = 0.0;
         downsideSampleWeight_ = 0.0;
         sum_ = 0.0;
         quadraticSum_ = 0.0;
         downsideQuadraticSum_ = 0.0;
         cubicSum_ = 0.0;
         fourthPowerSum_ = 0.0;
      }

      //! adds a sequence of data to the set, with default weight
      public void addSequence(List<double> list)
      {
         foreach (double v in list)
            add
               (v, 1);
      }
      //! adds a sequence of data to the set, each with its weight
      /*! \pre weights must be positive or null */
      public void addSequence(List<double> data, List<double> weight)
      {
         for (int i = 0; i < data.Count; i++)
            add
               (data[i], weight[i]);
      }
   }
}
