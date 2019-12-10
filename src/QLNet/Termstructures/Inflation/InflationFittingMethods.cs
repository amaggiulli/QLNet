using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static QLNet.AkimaSplineFitting;
//TODO Add Seri Log

namespace QLNet.Termstructures.Inflation
{
   public class InflationFittingMethods : FittedInflationBondCurve.FittingInflationMethod
   {


      /// <summary>
      /// Switches the use of the fitter to the smooth fitter
      /// </summary>
      private bool _useSmoothFitter = false;

      /// <summary>
      /// Switches the use of the fitter to the smooth fitter
      /// </summary>
      public bool useSmoothFitter { get { return _useSmoothFitter; }  set { _useSmoothFitter = value; } }

      private int size_;

      private int _ncurvePoints;

      //! N_th basis function coefficient to solve for when d(0)=1
      private int N_;

      private double[] _tenors;
      private Datum[] _initialCurveData;
      private Vector intialGuess_;
      private double _akimaWeightFactor = 1.0;


      public InflationFittingMethods(Datum[] initalCurve , 
         bool constrainatZero, Vector initialGuess = null, Vector weights = null, OptimizationMethod optimizationMethod = null, double akimaWeightFactor =1.0) : base(constrainatZero,weights, optimizationMethod)
      {

         //Extract the data for the curve tenors for the Akima Spline Fitting
         _ncurvePoints = initalCurve.Length;
         size_ = initialGuess.Count;
         _initialCurveData = initalCurve;
         _akimaWeightFactor = akimaWeightFactor;
         List<double> tenors = initalCurve.Select(x => x.tenor).ToList<double>();
         tenors.Sort();
         _tenors = tenors.ToArray();

         if (initialGuess == null)
         {
            initialGuess = new Vector(size_);
            for(int i =0; i < size_; ++i)
            {
               initialGuess[i] = 1.0;
            }
         }
         else
         {
            if (initialGuess.Count < size_)
            {
               
               initialGuess = new Vector(size_);
               for (int i = 0; i < size_; ++i)
               {
                  initialGuess[i] = 1.0;
               }
            }
         }

         intialGuess_ = initialGuess;
      }

      /// <summary>
      /// Method to clone the Inflation Fitter
      /// </summary>
      /// <returns></returns>
      public override FittedInflationBondCurve.FittingInflationMethod clone()
      {
         return MemberwiseClone() as FittedInflationBondCurve.FittingInflationMethod;
      }

      /// <summary>
      /// Method returns 
      /// </summary>
      /// <returns></returns>
      public override int size()
      {
         return size_;
      }

      internal double SvenssonCurve(Vector x, double t)
      {
         double kappa = x[size() - 2];
         double kappa_1 = x[size() - 1];

         double zeroRate = x[0] + (x[1] + x[2]) *
                           (1.0 - Math.Exp(-kappa * t)) /
                           ((kappa + Const.QL_EPSILON) * (t + Const.QL_EPSILON)) -
                           (x[2]) * Math.Exp(-kappa * t) +
                           x[3] * (((1.0 - Math.Exp(-kappa_1 * t)) / ((kappa_1 + Const.QL_EPSILON) * (t + Const.QL_EPSILON))) - Math.Exp(-kappa_1 * t));
         
         return zeroRate;
      }

      public double AkimaCurveValue(Vector x, double t)
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
            y[i] = x[i] * intialGuess_[i];
            diff[i] = (x[i + 1] * intialGuess_[i + 1] - x[i] * intialGuess_[i]) / (_tenors[i + 1] - _tenors[i]);
         }

         y[size_ - 1] = x[size_ - 1] * intialGuess_[size_ - 1];

         for (int i = 1; i < weights.Length; i++)
         {
            weights[i] = Math.Abs(diff[i] - diff[i - 1]) * _akimaWeightFactor;
         }

         var dd = new double[size_];

         double epsilon = 0.000000000001;

         for (int i = 2; i < dd.Length - 2; i++)
         {
            dd[i] = Math.Abs(weights[i - 1]) < epsilon && Math.Abs(weights[i + 1]) < epsilon
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

         int k = AkimaSpline.LeftSegmentIndex(_tenors, t);
         double xvalue = t - _tenors[k];
         d = c0[k] + xvalue * (c1[k] + xvalue * (c2[k] + xvalue * c3[k]));
         //}

         return d;

      }

      /// <summary>
      /// Method that gets the curve to feed to the objective function 
      /// </summary>
      /// <param name="x">Vector of parameters to be optimised</param>
      /// <param name="t"> The tenor of the curve</param>
      /// <returns></returns>
      public override ZeroIndex GetCurve(Vector x)
      {

         // now build the zero inflation curve
         RelinkableHandle<ZeroInflationTermStructure>  cpiUK = new RelinkableHandle<ZeroInflationTermStructure>(helper_.InflationIndex.zeroInflationTermStructure());
         Period observationLag = helper_.availabilityLag();
         Period contractObservationLag = helper_.availabilityLag();
         //InterpolationType contractObservationInterpolation = InterpolationType.Flat;

         Datum[] zciisData = new Datum[_ncurvePoints];
         List<Date> zciisD = new List<Date>();
         List<double> zciisR = new List<double>();

         //Apply the vector of adjustments

         if (_useSmoothFitter)
         {
            // Use the Swensson Curve Fitter
            for (int i = 0; i < _ncurvePoints; ++i)
            {
               zciisData[i] = _initialCurveData[i];
               zciisData[i].rate = SvenssonCurve(x, _initialCurveData[i].tenor);
            }
            
         }
         else
         {
            // Use the point by point fitting algorithm
            for (int i = 0; i < size_; ++i)
            {
               zciisData[i] = _initialCurveData[i];
               //zciisData[i].rate = AkimaCurveValue(x, _initialCurveData[i].tenor);
               zciisData[i].rate = x[i] * _initialCurveData[i].rate;

               zciisD.Add(zciisData[i].date);
               zciisR.Add(zciisData[i].rate);
            }


         }

         ZeroIndex inflationIndex = helper_.InflationIndex;


         // now build the helpers ...
         List<BootstrapHelper<ZeroInflationTermStructure>> helpers = makeHelpers(zciisData, _ncurvePoints, inflationIndex,
                                                                                 observationLag, helper_.fixingCalendar(), BusinessDayConvention.ModifiedFollowing, helper_.DayCounter);

         // we can use historical or first ZCIIS for this
         // we know historical is WAY off market-implied, so use market implied flat.
         double baseZeroRate = zciisData[0].rate / 100.0;
         //baseZeroRate = 0.026751143963392954;
         PiecewiseZeroInflationCurve<Linear> pCPIts = new PiecewiseZeroInflationCurve<Linear>(
            helper_.ReferenceDate, helper_.fixingCalendar(), helper_.DayCounter, observationLag, helper_.frequency(),false, baseZeroRate,
            new Handle<YieldTermStructure>(helper_.Curve), helpers);

         pCPIts.recalculate();
         cpiUK.linkTo(pCPIts);


         // make sure that the index has the latest zero inflation term structure
         inflationIndex.ZeroInflationTermStructureHandle.linkTo(pCPIts);

         return inflationIndex;

      }

   }
}
