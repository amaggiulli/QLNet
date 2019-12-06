//  Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)
//
//  This file is part of QLNet Project https://github.com/amaggiulli/qlnet
//  QLNet is free software: you can redistribute it and/or modify it
//  under the terms of the QLNet license.  You should have received a
//  copy of the license along with this program; if not, license is
//  available at <https://github.com/amaggiulli/QLNet/blob/develop/LICENSE>.
//
//  QLNet is a based on QuantLib, a free-software/open-source library
//  for financial quantitative analysts and developers - http://quantlib.org/
//  The QuantLib license is available online at http://quantlib.org/license.shtml.
//
//  This program is distributed in the hope that it will be useful, but WITHOUT
//  ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
//  FOR A PARTICULAR PURPOSE.  See the license for more details.
using System;
using System.Collections.Generic;
using System.Linq;

namespace QLNet
{
   //! Exponential-splines fitting method
   /*! Fits a discount function to the exponential form
       See:Li, B., E. DeWetering, G. Lucas, R. Brenner
       and A. Shapiro (2001): "Merrill Lynch Exponential Spline
       Model." Merrill Lynch Working Paper

       \warning convergence may be slow
   */
   public class ExponentialSplinesFitting : FittedBondDiscountCurve.FittingMethod
   {
      public ExponentialSplinesFitting(bool constrainAtZero = true,
                                       Vector weights = null,
                                       OptimizationMethod optimizationMethod = null)
         : base(constrainAtZero, weights, optimizationMethod)
      {}

      public override FittedBondDiscountCurve.FittingMethod clone()
      {
         return MemberwiseClone() as FittedBondDiscountCurve.FittingMethod;
      }

      public override int size() { return constrainAtZero_ ? 9 : 10; }

      internal override double discountFunction(Vector x, double t)
      {
         double d = 0.0;
         int N = size();
         double kappa = x[N - 1];
         double coeff = 0;

         if (!constrainAtZero_)
         {
            for (int i = 0; i < N - 1; ++i)
            {
               d += x[i] * Math.Exp(-kappa * (i + 1) * t);
            }
         }
         else
         {
            //  notation:
            //  d(t) = coeff* exp(-kappa*1*t) + x[0]* exp(-kappa*2*t) +
            //  x[1]* exp(-kappa*3*t) + ..+ x[7]* exp(-kappa*9*t)
            for (int i = 0; i < N - 1; i++)
            {
               d += x[i] * Math.Exp(-kappa * (i + 2) * t);
               coeff += x[i];
            }
            coeff = 1.0 - coeff;
            d += coeff * Math.Exp(-kappa * t);
         }
         return d;
      }
   }

   //! Nelson-Siegel fitting method
   /*! Fits a discount function to the form
       \f$ d(t) = \exp^{-r t}, \f$ where the zero rate \f$r\f$ is defined as
       \f[
       r \equiv c_0 + (c_0 + c_1)*(1 - exp^{-\kappa*t}/(\kappa t) -
       c_2 exp^{ - \kappa t}.
       \f]
       See: Nelson, C. and A. Siegel (1985): "Parsimonious modeling of yield
       curves for US Treasury bills." NBER Working Paper Series, no 1594.
   */
   public class NelsonSiegelFitting :  FittedBondDiscountCurve.FittingMethod
   {
      public NelsonSiegelFitting(Vector weights = null, OptimizationMethod optimizationMethod = null)
         : base(true, weights, optimizationMethod)
      {}

      public override FittedBondDiscountCurve.FittingMethod clone()
      {
         return MemberwiseClone() as FittedBondDiscountCurve.FittingMethod;
      }

      public override int size() { return 4; }

      internal override double discountFunction(Vector x, double t)
      {
         double kappa = x[size() - 1];
         double zeroRate = x[0] + (x[1] + x[2]) *
                           (1.0 - Math.Exp(-kappa * t)) /
                           ((kappa + Const.QL_EPSILON) * (t + Const.QL_EPSILON)) -
                           (x[2]) * Math.Exp(-kappa * t);
         double d = Math.Exp(-zeroRate * t) ;
         return d;
      }
   }

   //! Svensson Fitting method
   /*! Fits a discount function to the form

       See: Svensson, L. (1994). Estimating and interpreting forward
       interest rates: Sweden 1992-4.
       Discussion paper, Centre for Economic Policy Research(1051).
   */
   public class SvenssonFitting : FittedBondDiscountCurve.FittingMethod
   {
      public SvenssonFitting(Vector weights = null, OptimizationMethod optimizationMethod = null)
         : base(true, weights, optimizationMethod)
      {}

      public override FittedBondDiscountCurve.FittingMethod clone()
      {
         return MemberwiseClone() as FittedBondDiscountCurve.FittingMethod ;
      }

      public override int size() { return 6; }

      internal override double discountFunction(Vector x, double t)
      {
         double kappa = x[size() - 2];
         double kappa_1 = x[size() - 1];

         double zeroRate = x[0] + (x[1] + x[2]) *
                           (1.0 - Math.Exp(-kappa * t)) /
                           ((kappa + Const.QL_EPSILON) * (t + Const.QL_EPSILON)) -
                           (x[2]) * Math.Exp(-kappa * t) +
                           x[3] * (((1.0 - Math.Exp(-kappa_1 * t)) / ((kappa_1 + Const.QL_EPSILON) * (t + Const.QL_EPSILON))) - Math.Exp(-kappa_1 * t));
         double d = Math.Exp(-zeroRate * t) ;
         return d;
      }

   }

   //! CubicSpline B-splines fitting method
   /*! Fits a discount function to a set of cubic B-splines
       \f$ N_{i,3}(t) \f$, i.e.,
       \f[
       d(t) = \sum_{i=0}^{n}  c_i * N_{i,3}(t)
       \f]

       See: McCulloch, J. 1971, "Measuring the Term Structure of
       Interest Rates." Journal of Business, 44: 19-31

       McCulloch, J. 1975, "The tax adjusted yield curve."
       Journal of Finance, XXX811-30

       \warning "The results are extremely sensitive to the number
                 and location of the knot points, and there is no
                 optimal way of selecting them." James, J. and
                 N. Webber, "Interest Rate Modelling" John Wiley,
                 2000, pp. 440.
   */
   public class CubicBSplinesFitting : FittedBondDiscountCurve.FittingMethod
   {
      public CubicBSplinesFitting(List<double> knots, bool constrainAtZero = true, Vector weights = null,
                                  OptimizationMethod optimizationMethod = null)
         : base(constrainAtZero, weights, optimizationMethod)
      {
         splines_ = new BSpline(3, knots.Count - 5, knots);

         Utils.QL_REQUIRE(knots.Count >= 8, () => "At least 8 knots are required");
         int basisFunctions = knots.Count - 4;

         if (constrainAtZero)
         {
            size_ = basisFunctions - 1;

            // Note: A small but nonzero N_th basis function at t=0 may
            // lead to an ill conditioned problem
            N_ = 1;

            Utils.QL_REQUIRE(Math.Abs(splines_.value(N_, 0.0)) > Const.QL_EPSILON, () =>
                             "N_th cubic B-spline must be nonzero at t=0");
         }
         else
         {
            size_ = basisFunctions;
            N_ = 0;
         }

      }

      //! cubic B-spline basis functions
      public double basisFunction(int i, double t) { return splines_.value(i, t); }
      public override FittedBondDiscountCurve.FittingMethod clone()
      {
         return MemberwiseClone() as FittedBondDiscountCurve.FittingMethod;
      }

      public override int size() { return size_; }

      internal override double discountFunction(Vector x, double t)
      {
         double d = 0.0;

         if (!constrainAtZero_)
         {
            for (int i = 0; i < size_; ++i)
            {
               d += x[i] * splines_.value(i, t);
            }
         }
         else
         {
            double T = 0.0;
            double sum = 0.0;
            for (int i = 0; i < size_; ++i)
            {
               if (i < N_)
               {
                  d += x[i] * splines_.value(i, t);
                  sum += x[i] * splines_.value(i, T);
               }
               else
               {
                  d += x[i] * splines_.value(i + 1, t);
                  sum += x[i] * splines_.value(i + 1, T);
               }
            }
            double coeff = 1.0 - sum;
            coeff /= splines_.value(N_, T);
            d += coeff * splines_.value(N_, t);
         }

         return d;

      }

      private BSpline splines_;
      private int size_;
      //! N_th basis function coefficient to solve for when d(0)=1
      private int N_;
   }

   //! Simple polynomial fitting method
   /*
         This is a simple/crude, but fast and robust, means of fitting
         a yield curve.
   */
   public class SimplePolynomialFitting : FittedBondDiscountCurve.FittingMethod
   {
      public SimplePolynomialFitting(int degree,
                                     bool constrainAtZero = true,
                                     Vector weights = null,
                                     OptimizationMethod optimizationMethod = null)
         : base(constrainAtZero, weights, optimizationMethod)
      {
         size_ = constrainAtZero ? degree : degree + 1;
      }
      public override FittedBondDiscountCurve.FittingMethod clone()
      {
         return MemberwiseClone() as FittedBondDiscountCurve.FittingMethod;
      }

      public override int size() { return size_; }

      internal override double discountFunction(Vector x, double t)
      {
         double d = 0.0;

         if (!constrainAtZero_)
         {
            for (int i = 0; i < size_; ++i)
               d += x[i] * BernsteinPolynomial.get((uint)i, (uint)i, t);
         }
         else
         {
            d = 1.0;
            for (int i = 0; i < size_; ++i)
               d += x[i] * BernsteinPolynomial.get((uint)i + 1, (uint)i + 1, t);
         }
         return d;
      }

      private int size_;
   }

   //! Spread fitting method helper
   /*  Fits a spread curve on top of a discount function according to given parametric method
   */
   public class SpreadFittingMethod : FittedBondDiscountCurve.FittingMethod
   {
      public SpreadFittingMethod(FittedBondDiscountCurve.FittingMethod method, Handle<YieldTermStructure> discountCurve)
         : base(method != null ? method.constrainAtZero() : true,
                method != null ? method.weights() : null,
                method != null ? method.optimizationMethod() : null)
      {
         method_ = method;
         discountingCurve_ = discountCurve;

         Utils.QL_REQUIRE(method != null, () => "Fitting method is empty");
         Utils.QL_REQUIRE(!discountingCurve_.empty(), () => "Discounting curve cannot be empty");
      }

      public override FittedBondDiscountCurve.FittingMethod clone()
      {
         return MemberwiseClone() as FittedBondDiscountCurve.FittingMethod;
      }

      internal override void init()
      {
         //In case discount curve has a different reference date,
         //discount to this curve's reference date
         if (curve_.referenceDate() != discountingCurve_.link.referenceDate())
         {
            rebase_ = discountingCurve_.link.discount(curve_.referenceDate());
         }
         else
         {
            rebase_ = 1.0;
         }

         //Call regular init
         base.init();
      }

      public override int size() { return method_.size(); }

      internal override double discountFunction(Vector x, double t)
      {
         return method_.discount(x, t) * discountingCurve_.link.discount(t, true) / rebase_;
      }
      // underlying parametric method
      private FittedBondDiscountCurve.FittingMethod method_;
      // adjustment in case underlying discount curve has different reference date
      private double rebase_;
      // discount curve from on top of which the spread will be calculated
      private Handle<YieldTermStructure> discountingCurve_;

   }

   public class RiskFirstGiltCurveMethod : FittedBondDiscountCurve.FittingMethod
   {

      #region private data members
      // underlying parametric method
      private FittedBondDiscountCurve.FittingMethod method_;
      // adjustment in case underlying discount curve has different reference date
      private double rebase_;
      // discount curve from on top of which the spread will be calculated
      private Handle<YieldTermStructure> discountingCurve_;

      private BSpline splines_;
      private int size_;
      //! N_th basis function coefficient to solve for when d(0)=1
      private int N_;
      #endregion



      public RiskFirstGiltCurveMethod(List<double> tenorPoints, bool constrainAtZero = true, Vector weights = null,
                                  OptimizationMethod optimizationMethod = null)
         : base(constrainAtZero, weights, optimizationMethod)
      { 
         splines_ = new BSpline(3, tenorPoints.Count - 5, tenorPoints);

      Utils.QL_REQUIRE(tenorPoints.Count >= 8, () => "At least 8 knots are required");
         int basisFunctions = tenorPoints.Count - 4;

         if (constrainAtZero)
         {
            size_ = basisFunctions - 1;

            // Note: A small but nonzero N_th basis function at t=0 may
            // lead to an ill conditioned problem
            N_ = 1;

            Utils.QL_REQUIRE(Math.Abs(splines_.value(N_, 0.0)) > Const.QL_EPSILON, () =>
                             "N_th cubic B-spline must be nonzero at t=0");
         }
         else
         {
            size_ = basisFunctions;
            N_ = 0;
         }

      }

       //! cubic B-spline basis functions
      public double basisFunction(int i, double t) { return splines_.value(i, t); }
      public override FittedBondDiscountCurve.FittingMethod clone()
      {
         return MemberwiseClone() as FittedBondDiscountCurve.FittingMethod;
      }

      public override int size() { return size_; }

      internal override double discountFunction(Vector x, double t)
      {
         double d = 0.0;

         if (!constrainAtZero_)
         {
            for (int i = 0; i < size_; ++i)
            {
               d += x[i] * splines_.value(i, t);
            }
         }
         else
         {
            double T = 0.0;
            double sum = 0.0;
            for (int i = 0; i < size_; ++i)
            {
               if (i < N_)
               {
                  d += x[i] * splines_.value(i, t);
                  sum += x[i] * splines_.value(i, T);
               }
               else
               {
                  d += x[i] * splines_.value(i + 1, t);
                  sum += x[i] * splines_.value(i + 1, T);
               }
            }
            double coeff = 1.0 - sum;
            coeff /= splines_.value(N_, T);
            d += coeff * splines_.value(N_, t);
         }

         return d;

      }


    

      #region static function
      public static Vector GenerateWeights(DateTime referenceDate, List<DateTime> bondMuturitiyDates, int pricefitOrder,double priceMinimum)
      {

         if (bondMuturitiyDates.Count < 4)
         {
            throw new Exception("QFCurveService :: The number of maturity dates must be beigger than 3");
         }
         Vector weights = new Vector();
         foreach (DateTime date in bondMuturitiyDates)
         {
            TimeSpan ts = date.Subtract(referenceDate);
            weights.Add(Math.Exp(-0.02 * pricefitOrder * ts.TotalDays / 365.25) + priceMinimum);
         }

         return weights;
      }
      #endregion
   }

   public class AkimaSplineFitting : FittedBondDiscountCurve.FittingMethod
   {

       internal class AkimaSpline
      {
         private double[] _x;
         private double[] _c0;
         private double[] _c1;
         private double[] _c2;
         private double[] _c3;

         public AkimaSpline(double[] x, double[] c0, double[] c1, double[] c2, double[] c3)
         {
            if (x.Length != c0.Length + 1 || x.Length != c1.Length + 1 || x.Length != c2.Length + 1 || x.Length != c3.Length + 1)
            {
               throw new ArgumentException("Input arrays must be of the same length");
            }

            if (x.Length < 2)
            {
               throw new ArgumentException("The length of data must be greater than 2");
            }

            _x = x;
            _c0 = c0;
            _c1 = c1;
            _c2 = c2;
            _c3 = c3;
         }

         public AkimaSpline(double[] x)
         {
            int n = x.Length - 1;

            _x = x;

            _c0 = new double[x.Length - 1];
            _c1 = new double[x.Length - 1];
            _c2 = new double[x.Length - 1];
            _c3 = new double[x.Length - 1];
         }

         public AkimaSpline(double[] x, double[] y)
         {

         }

         public double Value(double t)
         {
            int k = LeftSegmentIndex(t);
            var x = t - _x[k];
            return _c0[k] + x * (_c1[k] + x * (_c2[k] + x * _c3[k]));
         }

         /// <summary>
         /// Find the index of the greatest sample point smaller than t,
         /// or the left index of the closest segment for extrapolation.
         /// </summary>
         public  int LeftSegmentIndex(double t)
         {
            int index = Array.BinarySearch(_x, t);
            if (index < 0)
            {
               index = ~index - 1;
            }

            return Math.Min(Math.Max(index, 0), _x.Length - 2);
         }

         public static  int LeftSegmentIndex(double[] xvalues, double t)
         {
            int index = Array.BinarySearch(xvalues, t);
            if (index < 0)
            {
               index = ~index - 1;
            }

            return Math.Min(Math.Max(index, 0), xvalues.Length - 2);
         }

         /// <summary>
         /// Three-Point Differentiation Helper.
         /// </summary>
         /// <param name="xx">Sample Points t.</param>
         /// <param name="yy">Sample Values x(t).</param>
         /// <param name="indexT">Index of the point of the differentiation.</param>
         /// <param name="index0">Index of the first sample.</param>
         /// <param name="index1">Index of the second sample.</param>
         /// <param name="index2">Index of the third sample.</param>
         /// <returns>The derivative approximation.</returns>
         public static double DifferentiateThreePoint(double[] xx, double[] yy, int indexT, int index0, int index1, int index2)
         {
            double x0 = yy[index0];
            double x1 = yy[index1];
            double x2 = yy[index2];

            double t = xx[indexT] - xx[index0];
            double t1 = xx[index1] - xx[index0];
            double t2 = xx[index2] - xx[index0];

            double a = (x2 - x0 - (t2 / t1 * (x1 - x0))) / (t2 * t2 - t1 * t2);
            double b = (x1 - x0 - a * t1 * t1) / t1;
            return (2 * a * t) + b;
         }
      }

      
      public AkimaSplineFitting(List<double> tenors, List<double> initialGuess = null,bool constrainAtZero = true, Vector weights = null,
                                  OptimizationMethod optimizationMethod = null, double akimaWeightFactor = 1.0)
         : base(constrainAtZero, weights, optimizationMethod)
      {
         if (initialGuess == null)
         {
            initialGuess = (new double[tenors.Count]).ToList();
            for(int i = 0; i < tenors.Count; ++i)
            {
               initialGuess[i] = 1.0;
            }

            splines_ = new AkimaSpline(tenors.ToArray(), initialGuess.ToArray());
         }
         else
         {
            if (tenors.Count == initialGuess.Count)
            {
               splines_ = new AkimaSpline(tenors.ToArray(), initialGuess.ToArray());
            }
            else
            {
               throw new Exception("The list of tenor values must match the list of yield values");
            }
         }

         // Throw exception if the number of points is below the threshold
         //Utils.QL_REQUIRE(tenors.Count >= 8, () => "At least 8 knots are required");
         int numberOfBasisFunctions = tenors.Count;

         _tenors = tenors.ToArray();
         _initialGuess = initialGuess.ToArray();
         _akimaWeightFactor = akimaWeightFactor;


         if (constrainAtZero)
         {
            size_ = numberOfBasisFunctions;

            // Note: A small but nonzero N_th basis function at t=0 may
            // lead to an ill conditioned problem
            N_ = 1;
         }
         else
         {
            size_ = numberOfBasisFunctions;
            N_ = 0;
         }

      }

      public override FittedBondDiscountCurve.FittingMethod clone()
      {
         return MemberwiseClone() as FittedBondDiscountCurve.FittingMethod;
      }

      public override int size() { return size_; }

      /// <summary>
      /// Method that 
      /// </summary>
      /// <param name="x"></param>
      /// <param name="t"></param>
      /// <returns></returns>
      internal override double discountFunction(Vector x, double t)
      {
         double d = 0.0;

         double[] diff = new double[size_ - 1];
         double[] weights = new double[size_ - 1];

         //if (!constrainAtZero_)
         //{
        //   
         //}
         //else
         //{

            double[] y = new double[size_];

            for (int i = 0; i < diff.Length; i++)
            {
               y[i] = x[i] * _initialGuess[i];
               diff[i] = (x[i + 1]*_initialGuess[i+1] - x[i]*_initialGuess[i]) / (_tenors[i + 1] - _tenors[i]);
            }

            y[size_ - 1] = x[size_ - 1] * _initialGuess[size_ - 1];

            for (int i = 1; i < weights.Length; i++)
            {
               weights[i] = Math.Abs(diff[i] - diff[i - 1])* _akimaWeightFactor;
            }

            var dd = new double[size_];

            double epsilon = 0.000000000001;

            for (int i = 2; i < dd.Length - 2; i++)
            {
               dd[i] = Math.Abs(weights[i - 1])< epsilon && Math.Abs(weights[i + 1])<epsilon
                   ? (((_tenors[i + 1] - _tenors[i]) * diff[i - 1]) + ((_tenors[i] - _tenors[i - 1]) * diff[i])) / (_tenors[i + 1] - _tenors[i - 1])
                   : ((weights[i + 1] * diff[i - 1]) + (weights[i - 1] * diff[i])) / (weights[i + 1] + weights[i - 1]);
            }

            

            dd[0] = AkimaSpline.DifferentiateThreePoint(_tenors, y, 0, 0, 1, 2);
            dd[1] = AkimaSpline.DifferentiateThreePoint(_tenors, y, 1, 0, 1, 2);
            dd[size_ - 2] = AkimaSpline.DifferentiateThreePoint(_tenors, y, size_ - 2, size_ - 3, size_ - 2, size_ - 1);
            dd[size_ - 1] = AkimaSpline.DifferentiateThreePoint(_tenors, y, size_ - 1, size_ - 3, size_ - 2, size_ - 1);

            var c0 = new double[size_ - 1];
            var c1 = new double[size_ - 1];
            var c2 = new double[size_ - 1];
            var c3 = new double[size_ - 1];
            for (int i = 0; i < c1.Length; i++)
            {
               double w = _tenors[i + 1] - _tenors[i];
               double w2 = w * w;
               c0[i] = y[i];
               c1[i] = dd[i];
               c2[i] = (3 * (y[i + 1] - y[i]) / w - 2 * dd[i] - dd[i + 1]) / w;
               c3[i] = (2 * (y[i] - y[i + 1]) / w + dd[i] + dd[i + 1]) / w2;
            }

            int k = AkimaSpline.LeftSegmentIndex(_tenors,t);
            double xvalue = t - _tenors[k];
            d= c0[k] + xvalue * (c1[k] + xvalue * (c2[k] + xvalue * c3[k]));
         //}

         return d;

      }

      #region static function
      public static Vector GenerateWeights(DateTime referenceDate, List<DateTime> bondMuturitiyDates, int pricefitOrder, double priceMinimum)
      {

         if (bondMuturitiyDates.Count < 4)
         {
            throw new Exception("QFCurveService :: The number of maturity dates must be beigger than 3");
         }

         Vector weights = new Vector();

         foreach (DateTime date in bondMuturitiyDates)
         {
            TimeSpan ts = date.Subtract(referenceDate);
            weights.Add(Math.Exp(-0.02 * pricefitOrder * ts.TotalDays / 365.25) + priceMinimum);
         }

         return weights;
      }
      #endregion

      private AkimaSpline splines_;
      private int size_;
      //! N_th basis function coefficient to solve for when d(0)=1
      private int N_;

      private double[] _tenors;
      private double[] _initialGuess;

      private double _akimaWeightFactor;
   }
}
