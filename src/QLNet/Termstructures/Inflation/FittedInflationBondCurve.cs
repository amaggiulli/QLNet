using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QLNet.Termstructures.Inflation
{
   public class FittedInflationBondCurve : ZeroIndex
   {
      #region private data members

      // target accuracy level to be used in the optimization routine
      private double accuracy_;
      // max number of evaluations to be used in the optimization routine
      private int maxEvaluations_;
      // sets the scale in the (Simplex) optimization routine
      private double simplexLambda_;
      // max number of evaluations where no improvement to solution is made
      private int maxStationaryStateIterations_;
      // a guess solution may be passed into the constructor to speed calcs
      private Vector guessSolution_;
      private Date maxDate_;
      private List<CPIBondHelper> bondHelpers_;
      private FittingInflationMethod fittingMethod_; // TODO Clone
      private DayCounter _dayCounter;
      private Calendar _calendar;

      private int _flatOrder = 2;

      private int _smoothOrder = 4;

      private double _smoothWeight = 500.0;

      private double _flatWeight = 500.0;

      private Vector _errors;

      private YieldTermStructure curve_;

      private ZeroIndex _zeroInflationIndex;

      // Model Bond values from the fit solution
      private Vector _modelValues;

      public DayCounter DayCounter { get { return _dayCounter; } }

      public DateTime ReferenceDate { get { return referenceDate_; } }

      public YieldTermStructure Curve { get { return curve_; } }

      public ZeroIndex InflationIndex { get { return _zeroInflationIndex; } }

      public Vector Errors { get { return _errors; } protected set { _errors = value; } }

      public Vector ModelPrediction { get { return _modelValues; } protected set { _modelValues = value; } }

      public int SmoothOrder { get { return _smoothOrder; } set { _smoothOrder = value; } }

      public int FlatOrder { get { return _flatOrder; } set { _flatOrder = value; } }

      public double SmoothWeight { get { return _smoothWeight; } set { _smoothWeight = value; } }

      public double FlatWeight { get { return _flatWeight; } set { _flatWeight = value; } }

      public double Cost { get => fittingMethod_.minimumCostValue(); }



      #endregion

      public FittedInflationBondCurve(DateTime referenceDate, string familyName,
                                      Region region,
                                      bool revised,
                                      bool interpolated,
                                      Frequency frequency,
                                      Period availabilityLag,
                                      Currency currency,
                                      int settlementDays,
                                      Calendar calendar,
                                      List<CPIBondHelper> bondHelpers,
                                      DayCounter dayCounter,
                                      FittingInflationMethod fittingMethod,
                                      YieldTermStructure nominalCurve,
                                      ZeroIndex inflationIndex,
                                       Handle<ZeroInflationTermStructure> ts = null,
                                       Vector guess = null,
                                      double accuracy = 1.0e-10,
                                      int maxEvaluations = 10000,
                                      double simplexLambda = 1.0,
                                      int maxStationaryStateIterations = 100
                                     ) : base(familyName, region, revised, interpolated, frequency, availabilityLag, currency, ts)
      {
         referenceDate_ = referenceDate;
         _calendar = calendar;
         accuracy_ = accuracy;
         maxEvaluations_ = maxEvaluations;
         simplexLambda_ = simplexLambda;
         maxStationaryStateIterations_ = maxStationaryStateIterations;
         guessSolution_ = guess ?? new Vector();
         bondHelpers_ = bondHelpers;
         fittingMethod_ = fittingMethod;
         curve_ = nominalCurve;
         _zeroInflationIndex = inflationIndex;
         _dayCounter = dayCounter;
         fittingMethod_.curve_ = this.curve_;
         fittingMethod_.helper_ = this;
         setup();
      }

      private void setup()
      {
         for (int i = 0; i < bondHelpers_.Count; ++i)
         {
            bondHelpers_[i].registerWith(curve_.update);
            bondHelpers_[i].SetCPIIndex(this);
         }
      }

      /// <summary>
      /// Method to initialise and call the fitting
      /// </summary>
      public void performCalculations()
      {
         fittingMethod_.init();
         fittingMethod_.calculate();

      }

      public override Calendar fixingCalendar()
      {
         return _calendar;
      }


      #region fitting class
      public class FittingInflationMethod
      {
         /// <summary>
         /// Indicates if the smooth fitter is to be used
         /// </summary>
         public bool UseSmoothFitter { get; set; }

         protected Vector _upperBound;

         /// <summary>
         /// Defines the upper bound for the smooth curve fitting process
         /// </summary>
         public Vector UpperBound { get { return _upperBound; } set { _upperBound = value; } }

         protected Vector _lowerBound;

         /// <summary>
         /// Defines the lower bound for the smooth curve fitting process
         /// </summary>
         public Vector LowerBound { get { return _lowerBound; } set { _lowerBound = value; } }

         public struct Datum
         {
            public Date date;
            public double rate;
            public double tenor;


            /// <summary>
            /// Constructor for the struct
            /// </summary>
            /// <param name="d">date</param>
            /// <param name="t">tenor</param>
            /// <param name="r">rate</param>
            public Datum(Date d, double t, double r)
            {
               date = d;
               rate = r;
               tenor = t;
            }
         }
         public static List<BootstrapHelper<ZeroInflationTermStructure>> makeHelpers(Datum[] iiData, int N,
                                                                                 ZeroIndex ii, Period observationLag,
                                                                                 QLNet.Calendar calendar,
                                                                                 BusinessDayConvention bdc,
                                                                                 DayCounter dc)
         {
            List<BootstrapHelper<ZeroInflationTermStructure>> instruments = new List<BootstrapHelper<ZeroInflationTermStructure>>();
            for (int i = 0; i < N; i++)
            {
               Date maturity = iiData[i].date;
               Handle<Quote> quote = new Handle<Quote>(new SimpleQuote(iiData[i].rate / 100.0));
               BootstrapHelper<ZeroInflationTermStructure> anInstrument = new ZeroCouponInflationSwapHelper(quote, observationLag, maturity,
                     calendar, bdc, dc, ii);
               instruments.Add(anInstrument);
            }
            return instruments;
         }
         // internal class

         protected RelinkableHandle<ZeroInflationTermStructure> zeroInflationCurve_ = new RelinkableHandle<ZeroInflationTermStructure>();
         public class FittingInflationCost : CostFunction
         {


            public bool UseSmoothFitter => false;

            private double _smoothOrder = 2.0;

            private double _smoothWeight = 500.0;

            private double _flatOrder = 4.0;

            private double _flatWeight = 500.0;

            public FittingInflationCost(FittingInflationMethod fittingMethod)
            {
               fittingMethod_ = fittingMethod;
            }

            public override double value(Vector x)
            {
               //TODO modify the overall objective function match the RF process if required
               double squaredError = 0.0;
               Vector vals = values(x);
               for (int i = 0; i < vals.size(); ++i)
               {
                  squaredError += vals[i];
               }
               //double flat = FlatnessMeasure(x);
               // double smooth = SmoothnessMeasure(x);
               return squaredError;// +flat + smooth;
            }

            /// <summary>
            /// Assigns a Smoothness weight the curve ensures that the curve does not deviate wildly
            /// </summary>
            /// <param name="x"></param>
            /// <returns></returns>
            public double SmoothnessMeasure(Vector x)
            {
               double tot = 0.0;
               double v = 0;
               for (int i = 1; i < x.size() - 2; ++i)
               {
                  v = 2 * x[i] - x[i - 1] - x[i + 1];
                  tot += Math.Pow((double)i, _smoothOrder) * v * v;
               }

               return _smoothWeight * tot;
            }


            /// <summary>
            /// Ensures the curve looks sensible
            /// </summary>
            /// <param name="x"></param>
            /// <returns></returns>
            public double FlatnessMeasure(Vector x)
            {

               double tot = 0.0;
               for (int i = 0; i < x.size() - 2; ++i)
               {
                  tot += Math.Pow((double)i, _flatOrder) * (x[i] - x[i + 1]) * (x[i] - x[i + 1]);
               }

               return _flatWeight * tot;
            }

            public override Vector values(Vector x)
            {
               //TODO This method requires the new assignment of the forward inflation curve to each of the bonds
               // Each bond then needs to be assigned to a pricing method
               // The discount curve can remain static in this case and needs to be relinked each time the bond index is updated.

               Date refDate = fittingMethod_.helper_.ReferenceDate;
               DayCounter dc = fittingMethod_.curve_.dayCounter();
               int n = fittingMethod_.helper_.bondHelpers_.Count;
               Vector values = new Vector(n);
               // Call the GetCurve method from the fitting method

               ZeroIndex newIndex = fittingMethod_.GetCurve(x);

               for (int i = 0; i < n; ++i)
               {
                  CPIBondHelper helper = fittingMethod_.helper_.bondHelpers_[i];
                  helper.SetCPIIndex(newIndex);

                  Bond bond = helper.bond();
                  //TODO Assign the new inflation index curve to the bond
                  Date bondSettlement = bond.settlementDate();

                  // CleanPrice_i = sum( cf_k * d(t_k) ) - accruedAmount
                  double modelPrice = 0.0;
                  List<CashFlow> cf = bond.cashflows();
                  for (int k = firstCashFlow_[i]; k < cf.Count; ++k)
                  {
                     double tenor = dc.yearFraction(refDate, cf[k].date());
                     modelPrice += cf[k].amount() * fittingMethod_.curve_.discount(tenor, true);
                  }
                  if (helper.useCleanPrice())
                     modelPrice -= bond.accruedAmount(bondSettlement);

                  // adjust price (NPV) for forward settlement
                  if (bondSettlement != refDate)
                  {
                     double tenor = dc.yearFraction(refDate, bondSettlement);
                     modelPrice /= fittingMethod_.curve_.discount(tenor, true);
                  }
                  double marketPrice = helper.quote().link.value();
                  double error = modelPrice - marketPrice;
                  double weightedError = fittingMethod_.weights_[i] * error;
                  values[i] = weightedError * weightedError;
               }
               return values;
            }

            public Vector Error(Vector x)
            {
               //TODO This method requires the new assignment of the forward inflation curve to each of the bonds
               // Each bond then needs to be assigned to a pricing method
               // The discount curve can remain static in this case and needs to be relinked each time the bond index is updated.

               Date refDate = fittingMethod_.helper_.ReferenceDate;
               DayCounter dc = fittingMethod_.curve_.dayCounter();
               int n = fittingMethod_.helper_.bondHelpers_.Count;
               Vector values = new Vector(n);
               // Call the GetCurve method from the fitting method

               ZeroIndex newIndex = fittingMethod_.GetCurve(x);

               for (int i = 0; i < n; ++i)
               {
                  CPIBondHelper helper = fittingMethod_.helper_.bondHelpers_[i];
                  helper.SetCPIIndex(newIndex);

                  Bond bond = helper.bond();
                  //TODO Assign the new inflation index curve to the bond
                  Date bondSettlement = bond.settlementDate();

                  // CleanPrice_i = sum( cf_k * d(t_k) ) - accruedAmount
                  double modelPrice = 0.0;
                  List<CashFlow> cf = bond.cashflows();
                  for (int k = firstCashFlow_[i]; k < cf.Count; ++k)
                  {
                     double tenor = dc.yearFraction(refDate, cf[k].date());
                     modelPrice += cf[k].amount() * fittingMethod_.curve_.discount(tenor, true);
                  }
                  if (helper.useCleanPrice())
                     modelPrice -= bond.accruedAmount(bondSettlement);

                  // adjust price (NPV) for forward settlement
                  if (bondSettlement != refDate)
                  {
                     double tenor = dc.yearFraction(refDate, bondSettlement);
                     modelPrice /= fittingMethod_.curve_.discount(tenor, true);
                  }
                  double marketPrice = helper.quote().link.value();
                  double error = modelPrice - marketPrice;
                  double weightedError = fittingMethod_.weights_[i] * error;
                  values[i] = weightedError;
               }
               return values;
            }

            public Vector ModelBondValues(Vector x)
            {
               //TODO This method requires the new assignment of the forward inflation curve to each of the bonds
               // Each bond then needs to be assigned to a pricing method
               // The discount curve can remain static in this case and needs to be relinked each time the bond index is updated.

               Date refDate = fittingMethod_.helper_.ReferenceDate;
               DayCounter dc = fittingMethod_.curve_.dayCounter();
               int n = fittingMethod_.helper_.bondHelpers_.Count;
               Vector values = new Vector(n);
               // Call the GetCurve method from the fitting method

               ZeroIndex newIndex = fittingMethod_.GetCurve(x);

               for (int i = 0; i < n; ++i)
               {
                  CPIBondHelper helper = fittingMethod_.helper_.bondHelpers_[i];
                  helper.SetCPIIndex(newIndex);

                  Bond bond = helper.bond();
                  //TODO Assign the new inflation index curve to the bond
                  Date bondSettlement = bond.settlementDate();

                  // CleanPrice_i = sum( cf_k * d(t_k) ) - accruedAmount
                  double modelPrice = 0.0;
                  List<CashFlow> cf = bond.cashflows();
                  for (int k = firstCashFlow_[i]; k < cf.Count; ++k)
                  {
                     double tenor = dc.yearFraction(refDate, cf[k].date());
                     modelPrice += cf[k].amount() * fittingMethod_.curve_.discount(tenor, true);
                  }
                  if (helper.useCleanPrice())
                     modelPrice -= bond.accruedAmount(bondSettlement);

                  // adjust price (NPV) for forward settlement
                  if (bondSettlement != refDate)
                  {
                     double tenor = dc.yearFraction(refDate, bondSettlement);
                     modelPrice /= fittingMethod_.curve_.discount(tenor, true);
                  }
                  double marketPrice = helper.quote().link.value();
                  double error = modelPrice - marketPrice;
                  double weightedError = fittingMethod_.weights_[i] * error;
                  values[i] = modelPrice;
               }
               return values;
            }

            private FittingInflationMethod fittingMethod_;
            internal List<int> firstCashFlow_;


         }

         //! total number of coefficients to fit/solve for
         public virtual int size() { throw new NotImplementedException(); }
         //! output array of results of optimization problem
         public Vector solution() { return solution_; }
         //! final number of iterations used in the optimization problem
         public int numberOfIterations() { return numberOfIterations_; }
         //! final value of cost function after optimization
         public double minimumCostValue() { return costValue_; }
         //! clone of the current object
         public virtual FittingInflationMethod clone() { throw new NotImplementedException(); }
         //! return whether there is a constraint at zero
         public bool constrainAtZero() { return constrainAtZero_; }
         //! return weights being used
         public Vector weights() { return weights_; }
         //! return optimization method being used
         public OptimizationMethod optimizationMethod() { return optimizationMethod_; }
         //! open discountFunction to public
         //TODO This needs to produce the adjusted curve object
         public virtual ZeroIndex GetCurve(Vector x) { return GetCurve(x); }

         //! constructor
         protected FittingInflationMethod(bool constrainAtZero = true,
                                 Vector weights = null,
                                 OptimizationMethod optimizationMethod = null)
         {
            constrainAtZero_ = constrainAtZero;
            weights_ = weights ?? new Vector();
            calculateWeights_ = weights_.empty();
            optimizationMethod_ = optimizationMethod;
         }
         //! rerun every time instruments/referenceDate changes
         internal virtual void init()
         {
            // yield conventions
            DayCounter yieldDC = curve_.dayCounter();
            Compounding yieldComp = Compounding.Compounded;
            Frequency yieldFreq = Frequency.Annual;

            int n = helper_.bondHelpers_.Count;
            costFunction_ = new FittingInflationCost(this);
            costFunction_.firstCashFlow_ = new InitializedList<int>(n);

            for (int i = 0; i < helper_.bondHelpers_.Count; ++i)
            {
               Bond bond = helper_.bondHelpers_[i].bond();
               List<CashFlow> cf = bond.cashflows();
               Date bondSettlement = bond.settlementDate();
               for (int k = 0; k < cf.Count; ++k)
               {
                  if (!cf[k].hasOccurred(bondSettlement, false))
                  {
                     costFunction_.firstCashFlow_[i] = k;
                     break;
                  }
               }
            }

            if (calculateWeights_)
            {
               //if (weights_.empty())
               weights_ = new Vector(n);

               double squaredSum = 0.0;
               for (int i = 0; i < helper_.bondHelpers_.Count; ++i)
               {
                  Bond bond = helper_.bondHelpers_[i].bond();

                  double cleanPrice = helper_.bondHelpers_[i].quote().link.value();

                  Date bondSettlement = bond.settlementDate();
                  double ytm = BondFunctions.yield(bond, cleanPrice, yieldDC, yieldComp, yieldFreq, bondSettlement);

                  double dur = BondFunctions.duration(bond, ytm, yieldDC, yieldComp, yieldFreq,
                                                      Duration.Type.Modified, bondSettlement);
                  weights_[i] = 1.0 / dur;
                  squaredSum += weights_[i] * weights_[i];
               }
               weights_ /= Math.Sqrt(squaredSum);
            }

            Utils.QL_REQUIRE(weights_.size() == n, () =>
                             "Given weights do not cover all boostrapping helpers");

         }

         //! discount function called by FittedBondDiscountCurve
         internal virtual double discountFunction(Vector x, double t) { throw new NotImplementedException(); }

         //! constrains discount function to unity at \f$ T=0 \f$, if true
         protected bool constrainAtZero_;
         //! internal reference to the FittedBondDiscountCurve instance
         internal YieldTermStructure curve_;

         internal FittedInflationBondCurve helper_;
         //! solution array found from optimization, set in calculate()
         internal Vector solution_;
         //! optional guess solution to be passed into constructor.
         /*! The idea is to use a previous solution as a guess solution to
            the discount curve, in an attempt to speed up calculations.
         */
         protected Vector guessSolution_;
         //! base class sets this cost function used in the optimization routine
         protected FittingInflationCost costFunction_;

         // curve optimization called here- adjust optimization parameters here
         internal void calculate()
         {
            FittingInflationCost costFunction = costFunction_;
            //Constraint constraint = new BoundaryConstraint(0.0, helper_.guessSolution_[0] * 3);


            Constraint constraint = new Constraint();

            if (this.UseSmoothFitter)
            {

               if (this.helper_.fittingMethod_.UpperBound == null)
               {

                  throw new NullReferenceException("FittedInflationBondCurve - fitting bounds cannot be null");
               }
               else
               {
                  constraint = new NonhomogeneousBoundaryConstraint(helper_.fittingMethod_.LowerBound, helper_.fittingMethod_.UpperBound);
               }





            }
            else
            {
               constraint = new BoundaryConstraint(-5.0, 25.0);
            }

            // start with the guess solution, if it exists
            Vector x = new Vector(size(), 0.0);
            if (!helper_.guessSolution_.empty())
            {
               x = helper_.guessSolution_;
            }

            if (helper_.maxEvaluations_ == 0)
            {
               //Don't calculate, simply use given parameters to provide a fitted curve.
               //This turns the fittedbonddiscountcurve into an evaluator of the parametric
               //curve, for example allowing to use the parameters for a credit spread curve
               //calculated with bonds in one currency to be coupled to a discount curve in
               //another currency.
               return;
            }

            //workaround for backwards compatibility
            OptimizationMethod optimization = optimizationMethod_;
            if (optimization == null)
            {
               optimization = new Simplex(helper_.simplexLambda_);
            }

            Problem problem = new Problem(costFunction, constraint, x);

            double rootEpsilon = helper_.accuracy_;
            double functionEpsilon = helper_.accuracy_;
            double gradientNormEpsilon = helper_.accuracy_;

            EndCriteria endCriteria = new EndCriteria(helper_.maxEvaluations_,
                                                      helper_.maxStationaryStateIterations_,
                                                      rootEpsilon,
                                                      functionEpsilon,
                                                      gradientNormEpsilon);

            optimization.minimize(problem, endCriteria);
            solution_ = problem.currentValue();

            numberOfIterations_ = problem.functionEvaluation();
            costValue_ = problem.functionValue();

            // save the results as the guess solution, in case of recalculation
            helper_.guessSolution_ = solution_;

            // Provide quality metrics for the fit
            helper_._errors = this.costFunction_.Error(solution_);

            //Provide bond values for diagnostic checks
            helper_._modelValues = this.costFunction_.ModelBondValues(solution_);
         }

         // array of normalized (duration) weights, one for each bond helper
         private Vector weights_;
         // whether or not the weights should be calculated internally
         private bool calculateWeights_;
         // total number of iterations used in the optimization routine
         // (possibly including gradient evaluations)
         private int numberOfIterations_;
         // final value for the minimized cost function
         private double costValue_;
         // optimization method to be used, if none provided use Simplex
         private OptimizationMethod optimizationMethod_;

      }


      #endregion

   }


}
