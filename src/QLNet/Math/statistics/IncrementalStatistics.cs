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
   /// <summary>
   /// Statistics tool based on incremental accumulation
   /// </summary>
   /// <remarks>
   /// It can accumulate a set of data and return statistics (e.g: mean,
   /// variance, skewness, kurtosis, error estimation, etc.)
   /// Warning: high moments are numerically unstable for high
   /// average/standardDeviation ratios.
   /// </remarks>
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

      /// <summary>
      /// Returns the number of collected samples.
      /// </summary>
      public int samples() { return sampleNumber_; }

      /// <summary>
      /// Returns the sum of data weights.
      /// </summary>
      public double weightSum() { return sampleWeight_; }

      /// <summary>
      /// Returns the weighted mean.
      /// </summary>
      public double mean()
      {
         Utils.QL_REQUIRE(sampleWeight_ > 0.0, () => "sampleWeight_=0, insufficient");
         return sum_ / sampleWeight_;
      }

      /// <summary>
      /// Returns the variance.
      /// </summary>
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

      /// <summary>
      /// Returns the standard deviation, defined as the square root of the variance.
      /// </summary>
      public double standardDeviation() { return Math.Sqrt(variance()); }

      /// <summary>
      /// Returns the downside variance.
      /// </summary>
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


      /// <summary>
      /// Returns the error estimate.
      /// </summary>
      /// <remarks>
      /// The estimate is the square root of the ratio of the variance to
      /// the number of samples.
      /// </remarks>
      public double errorEstimate()
      {
         double var = variance();
         Utils.QL_REQUIRE(samples() > 0, () => "empty sample set");
         return Math.Sqrt(var / samples());
      }

      /// <summary>
      /// Returns the skewness.
      /// </summary>
      /// <remarks>
      /// This evaluates to 0 for a Gaussian distribution.
      /// </remarks>
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

      /// <summary>
      /// Returns the excess kurtosis.
      /// </summary>
      /// <remarks>
      /// This evaluates to 0 for a Gaussian distribution.
      /// </remarks>
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

      /// <summary>
      /// Returns the minimum sample value.
      /// </summary>
      public double min()
      {
         Utils.QL_REQUIRE(samples() > 0, () => "empty sample set");
         return min_;
      }

      /// <summary>
      /// Returns the maximum sample value.
      /// </summary>
      public double max()
      {
         Utils.QL_REQUIRE(samples() > 0, () => "empty sample set");
         return max_;
      }

      /// <summary>
      /// Returns the downside deviation, defined as the square root of the downside variance.
      /// </summary>
      public double downsideDeviation() { return Math.Sqrt(downsideVariance()); }


      // Modifiers
      /// <summary>
      /// Adds a datum to the set, optionally with a weight.
      /// </summary>
      /// <internalremarks>
      /// Precondition: the weight must be positive or null.
      /// </internalremarks>
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

      /// <summary>
      /// Resets the data to an empty set.
      /// </summary>
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

      /// <summary>
      /// Adds a sequence of data to the set with default weight.
      /// </summary>
      public void addSequence(List<double> list)
      {
         foreach (double v in list)
            add
               (v, 1);
      }
      /// <summary>
      /// Adds a sequence of data to the set, each with its own weight.
      /// </summary>
      /// <internalremarks>
      /// Precondition: weights must be positive or null.
      /// </internalremarks>
      public void addSequence(List<double> data, List<double> weight)
      {
         for (int i = 0; i < data.Count; i++)
            add
               (data[i], weight[i]);
      }
   }
}
