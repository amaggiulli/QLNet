//  Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)
//  
//  This file is part of QLNet Project https://github.com/amaggiulli/qlnet
//  QLNet is free software: you can redistribute it and/or modify it
//  under the terms of the QLNet license.  You should have received a
//  copy of the license along with this program; if not, license is  
//  available online at <http://qlnet.sourceforge.net/License.html>.
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
using System.Text;

namespace QLNet
{
   //! Discount curve fitted to a set of fixed-coupon bonds
   /*! This class fits a discount function \f$ d(t) \f$ over a set of
       bonds, using a user defined fitting method. The discount
       function is fit in such a way so that all cashflows of all
       input bonds, when discounted using \f$ d(t) \f$, will
       reproduce the set of input bond prices in an optimized
       sense. Minimized price errors are weighted by the inverse of
       their respective bond duration.

       The FittedBondDiscountCurve class acts as a generic wrapper,
       while its inner class FittingMethod provides the
       implementation details. Developers thus need only derive new
       fitting methods from the latter.

       <b> Example: </b>
       \link FittedBondCurve.cpp
       compares various bond discount curve fitting methodologies
       \endlink

       \warning The method can be slow if there are many bonds to
                fit. Speed also depends on the particular choice of
                fitting method chosen and its convergence properties
                under optimization.  See also todo list for
                BondDiscountCurveFittingMethod.

       \todo refactor the bond helper class so that it is pure
             virtual and returns a generic bond or its cash
             flows. Derived classes would include helpers for
             fixed-rate and zero-coupon bonds. In this way, both
             bonds and bills can be used to fit a discount curve
             using the exact same machinery. At present, only
             fixed-coupon bonds are supported. An even better way to
             move forward might be to get rate helpers to return
             cashflows, in which case this class could be used to fit
             any set of cash flows, not just bonds.

       \todo add more fitting diagnostics: smoothness, standard
             deviation, student-t test, etc. Generic smoothness
             method may be useful for smoothing splines fitting. See
             Fisher, M., D. Nychka and D. Zervos: "Fitting the term
             structure of interest rates with smoothing splines."
             Board of Governors of the Federal Reserve System,
             Federal Resere Board Working Paper, 95-1.

       \todo add extrapolation routines

       \ingroup yieldtermstructures
   */
   public class FittedBondDiscountCurve : YieldTermStructure
   {
      //! \name Constructors
      //@{
      //! reference date based on current evaluation date
      public FittedBondDiscountCurve( int settlementDays,
                                      Calendar calendar,
                                      List<BondHelper> bondHelpers,
                                      DayCounter dayCounter,
                                      FittingMethod fittingMethod,
                                      double accuracy = 1.0e-10,
                                      int maxEvaluations = 10000,
                                      Vector guess = null,
                                      double simplexLambda = 1.0,
                                      int maxStationaryStateIterations = 100)
         :base(settlementDays, calendar, dayCounter)
      {
         accuracy_ = accuracy;
         maxEvaluations_ = maxEvaluations;
         simplexLambda_ = simplexLambda;
         maxStationaryStateIterations_ = maxStationaryStateIterations;
         guessSolution_ = guess ?? new Vector();
         bondHelpers_ = bondHelpers;
         fittingMethod_ = fittingMethod;

         fittingMethod_.curve_ = this;
         setup();
      }

      //! curve reference date fixed for life of curve
      public FittedBondDiscountCurve( Date referenceDate,
                                      List<BondHelper> bondHelpers,
                                      DayCounter dayCounter,
                                      FittingMethod fittingMethod,
                                      double accuracy = 1.0e-10,
                                      int maxEvaluations = 10000,
                                      Vector guess = null,
                                      double simplexLambda = 1.0,
                                      int maxStationaryStateIterations = 100)
         :base(referenceDate, new Calendar(), dayCounter)
      {
         accuracy_ = accuracy;
         maxEvaluations_ = maxEvaluations;
         simplexLambda_ = simplexLambda;
         maxStationaryStateIterations_ = maxStationaryStateIterations;
         guessSolution_ = guess;
         bondHelpers_ = bondHelpers;
         fittingMethod_ = fittingMethod;

         fittingMethod_.curve_ = this;
        setup();
      }
      //@}

      //! \name Inspectors
      //@{
      //! total number of bonds used to fit the yield curve
      public int numberOfBonds() {return bondHelpers_.Count;}
      //! the latest date for which the curve can return values
      public override Date maxDate()
      {
         calculate();
         return maxDate_;
      }
      //! class holding the results of the fit
      public FittingMethod fitResults()
      {
         calculate();
         return fittingMethod_.clone();
      }
      //@}

      //! \name Observer interface
      //@{
      public override void update() {base.update();}
      //@}

      private void setup()
      {
         for (int i=0; i<bondHelpers_.Count; ++i)
            bondHelpers_[i].registerWith(update);
      }

      protected override void performCalculations()
      {
                 
         Utils.QL_REQUIRE(!bondHelpers_.empty(),()=> "no bondHelpers given");

         maxDate_ = Date.minDate();
         Date refDate = referenceDate();

         // double check bond quotes still valid and/or instruments not expired
         for (int i=0; i<bondHelpers_.Count; ++i) 
         {
            Bond bond = bondHelpers_[i].bond();
            Utils.QL_REQUIRE(bondHelpers_[i].quote().link.isValid(),()=>
                       (i+1) + " bond (maturity: " +
                       bond.maturityDate() + ") has an invalid price quote");
            Date bondSettlement = bond.settlementDate();
            Utils.QL_REQUIRE(bondSettlement>=refDate,()=>
                       (i+1) + " bond settlemente date (" +
                       bondSettlement + ") before curve reference date (" +
                       refDate + ")");
            Utils.QL_REQUIRE(BondFunctions.isTradable(bond, bondSettlement),()=>
                       (i+1) + " bond non tradable at " +
                       bondSettlement + " settlement date (maturity" +
                       " being " + bond.maturityDate() + ")");
            maxDate_ = Date.Max(maxDate_, bondHelpers_[i].pillarDate());
            bondHelpers_[i].setTermStructure((FittedBondDiscountCurve)this);
         }
         fittingMethod_.init();
         fittingMethod_.calculate();
   
      }

      protected override double discountImpl(double t)
      {
         calculate();
         return fittingMethod_.discountFunction(fittingMethod_.solution_, t);
      }
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
      private List<BondHelper> bondHelpers_;
      private FittingMethod fittingMethod_; // TODO Clone

      //! Base fitting method used to construct a fitted bond discount curve
      /*! This base class provides the specific methodology/strategy
          used to construct a FittedBondDiscountCurve.  Derived classes
          need only define the virtual function discountFunction() based
          on the particular fitting method to be implemented, as well as
          size(), the number of variables to be solved for/optimized. The
          generic fitting methodology implemented here can be termed
          nonlinear, in contrast to (typically faster, computationally)
          linear fitting method.

          Optional parameters for FittingMethod include an Array of
          weights, which will be used as weights to each bond. If not given
          or empty, then the bonds will be weighted by inverse duration

          \todo derive the special-case class LinearFittingMethods from
                FittingMethod. A linear fitting to a set of basis
                functions \f$ b_i(t) \f$ is any fitting of the form
                \f[
                d(t) = \sum_{i=0} c_i b_i(t)
                \f]
                i.e., linear in the unknown coefficients \f$ c_i
                \f$. Such a fitting can be reduced to a linear algebra
                problem \f$ Ax = b \f$, and for large numbers of bonds,
                would typically be much faster computationally than the
                generic non-linear fitting method.

          \warning some parameters to the Simplex optimization method
                   may need to be tweaked internally to the class,
                   depending on the fitting method used, in order to get
                   proper/reasonable/faster convergence.
      */
      public class FittingMethod
      {
         //friend class FittedBondDiscountCurve;
         // internal class
         public class FittingCost : CostFunction
         {
            public FittingCost(FittedBondDiscountCurve.FittingMethod fittingMethod)
            {
               fittingMethod_ = fittingMethod;
            }

            public override double value(Vector x)
            {
               double squaredError = 0.0;
		         Vector vals = values(x);
		         for (int i = 0; i<vals.size(); ++i) 
               {
                  squaredError += vals[i];
               }
               return squaredError;
            }
            public override Vector values(Vector x)
            {
               Date refDate  = fittingMethod_.curve_.referenceDate();
               DayCounter dc = fittingMethod_.curve_.dayCounter();
               int n = fittingMethod_.curve_.bondHelpers_.Count;
               Vector values = new Vector(n);
               for (int i=0; i<n; ++i) 
               {
                  BondHelper helper = fittingMethod_.curve_.bondHelpers_[i];

                  Bond bond = helper.bond();
                  Date bondSettlement = bond.settlementDate();

                  // CleanPrice_i = sum( cf_k * d(t_k) ) - accruedAmount
                  double modelPrice = 0.0;
                  List<CashFlow> cf = bond.cashflows();
                  for (int k=firstCashFlow_[i]; k<cf.Count; ++k) 
                  {
                     double tenor = dc.yearFraction(refDate, cf[k].date());
                     modelPrice += cf[k].amount() * fittingMethod_.discountFunction(x, tenor);
                  }
                  if (helper.useCleanPrice())
                     modelPrice -= bond.accruedAmount(bondSettlement);

                  // adjust price (NPV) for forward settlement
                  if (bondSettlement != refDate ) 
                  {
                     double tenor = dc.yearFraction(refDate, bondSettlement);
                     modelPrice /= fittingMethod_.discountFunction(x, tenor);
                  }
                  double marketPrice = helper.quote().link.value();
                  double error = modelPrice - marketPrice;
                  double weightedError = fittingMethod_.weights_[i] * error;
                  values[i] = weightedError * weightedError;
               }
               return values;
            }
         
            private FittedBondDiscountCurve.FittingMethod fittingMethod_;
            internal List<int> firstCashFlow_;

            
         }
         
         //! total number of coefficients to fit/solve for
         public virtual int size() { throw new NotImplementedException(); }
         //! output array of results of optimization problem
         public Vector solution() { return solution_;}
         //! final number of iterations used in the optimization problem
         public int numberOfIterations() {return numberOfIterations_;}
         //! final value of cost function after optimization
         public double minimumCostValue() { return costValue_;}
         //! clone of the current object
         public virtual FittingMethod clone() { throw new NotImplementedException(); }
		   //! return whether there is a constraint at zero
		   public bool constrainAtZero() {return constrainAtZero_;}
		   //! return weights being used
		   public Vector weights() {return weights_;}
		   //! return optimization method being used
		   public OptimizationMethod optimizationMethod() {return optimizationMethod_;}
		   //! open discountFunction to public
		   public double discount(Vector x, double t) {return discountFunction(x, t);}
      
         //! constructor
         protected FittingMethod(bool constrainAtZero = true, 
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

            int n = curve_.bondHelpers_.Count;
            costFunction_ = new FittingCost(this);
            costFunction_.firstCashFlow_ = new InitializedList<int>(n);

            for (int i=0; i<curve_.bondHelpers_.Count; ++i) 
            {
               Bond bond = curve_.bondHelpers_[i].bond();
               List<CashFlow> cf = bond.cashflows();
               Date bondSettlement = bond.settlementDate();
               for (int k=0; k<cf.Count; ++k) 
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
               for (int i=0; i<curve_.bondHelpers_.Count; ++i) 
               {
                  Bond bond = curve_.bondHelpers_[i].bond();

                  double cleanPrice = curve_.bondHelpers_[i].quote().link.value();

                  Date bondSettlement = bond.settlementDate();
                  double ytm = BondFunctions.yield(bond, cleanPrice,yieldDC, yieldComp, yieldFreq, bondSettlement);

                  double dur = BondFunctions.duration(bond, ytm, yieldDC, yieldComp, yieldFreq, 
                     Duration.Type.Modified, bondSettlement);
                  weights_[i] = 1.0/dur;
                  squaredSum += weights_[i]*weights_[i];
               }
               weights_ /= Math.Sqrt(squaredSum);
            }

            Utils.QL_REQUIRE(weights_.size() == n,()=>
               "Given weights do not cover all boostrapping helpers");
   
         }

         //! discount function called by FittedBondDiscountCurve
         internal virtual double discountFunction( Vector x, double t ) { throw new NotImplementedException(); }

         //! constrains discount function to unity at \f$ T=0 \f$, if true
         protected bool constrainAtZero_;
         //! internal reference to the FittedBondDiscountCurve instance
         internal FittedBondDiscountCurve curve_;
         //! solution array found from optimization, set in calculate()
         internal Vector solution_;
         //! optional guess solution to be passed into constructor.
         /*! The idea is to use a previous solution as a guess solution to
            the discount curve, in an attempt to speed up calculations.
         */
         protected Vector guessSolution_;
         //! base class sets this cost function used in the optimization routine
         protected FittingCost costFunction_;
      
         // curve optimization called here- adjust optimization parameters here
         internal void calculate()
         {
            FittingCost costFunction = costFunction_;
            Constraint constraint = new NoConstraint();

            // start with the guess solution, if it exists
            Vector x = new Vector(size(), 0.0);
            if (!curve_.guessSolution_.empty()) 
            {
               x = curve_.guessSolution_;
            }

		      if(curve_.maxEvaluations_ == 0)
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
            if(optimization == null)
            {
		         optimization = new Simplex(curve_.simplexLambda_);
		      }
	
            Problem problem = new Problem(costFunction, constraint, x);

		      double rootEpsilon = curve_.accuracy_;
            double functionEpsilon =  curve_.accuracy_;
            double gradientNormEpsilon = curve_.accuracy_;

            EndCriteria endCriteria = new EndCriteria(curve_.maxEvaluations_,
                                                      curve_.maxStationaryStateIterations_,
                                                      rootEpsilon,
                                                      functionEpsilon,
                                                      gradientNormEpsilon);

            optimization.minimize(problem,endCriteria);
            solution_ = problem.currentValue();

            numberOfIterations_ = problem.functionEvaluation();
            costValue_ = problem.functionValue();

            // save the results as the guess solution, in case of recalculation
            curve_.guessSolution_ = solution_;
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
   }
}
