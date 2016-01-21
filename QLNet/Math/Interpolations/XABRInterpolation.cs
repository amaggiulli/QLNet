/*
 Copyright (C) 2008-2015  Andrea Maggiulli (a.maggiulli@gmail.com)

 This file is part of QLNet Project https://github.com/amaggiulli/qlnet

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

namespace QLNet
{
   public interface IWrapper
   {
      double volatility( double x );
   }

   public interface IModel
   {
      //IModel modelInstance_ { get; set; }
      void defaultValues( List<double?> param, List<bool> b, double forward, double expiryTIme );
      double dilationFactor();
      int dimension();
      Vector direct( Vector x, List<bool> b, List<double?> c, double d );
      double eps1();
      double eps2();
      void guess( Vector values, List<bool> paramIsFixed, double forward, double expiryTime, List<double> r );
      IWrapper instance( double t, double forward, List<double?> param );
      Vector inverse( Vector y, List<bool> b, List<double?> c, double d );
      //double volatility( double x );
   }

   public class XABRCoeffHolder<Model> where Model : IModel, new()
   {
      public XABRCoeffHolder( double t, double forward, List<double?> _params, List<bool> paramIsFixed )
      {
         t_ = t;
         forward_ = forward;
         params_ = _params;
         paramIsFixed_ = new InitializedList<bool>( paramIsFixed.Count, false );
         weights_ = new List<double>();
         error_ = null;
         maxError_ = null;
         XABREndCriteria_ = EndCriteria.Type.None;

         Utils.QL_REQUIRE( t > 0.0, () => "expiry time must be positive: " + t + " not allowed" );
         Utils.QL_REQUIRE( _params.Count == new Model().dimension(), () =>
                   "wrong number of parameters (" + _params.Count
                                                  + "), should be "
                                                  + new Model().dimension() );
         Utils.QL_REQUIRE( paramIsFixed.Count == new Model().dimension(), () =>
                   "wrong number of fixed parameters flags (" + paramIsFixed.Count + "), should be "
                   + new Model().dimension() );

         for ( int i = 0; i < _params.Count; ++i )
         {
            if ( _params[i] != null )
               paramIsFixed_[i] = paramIsFixed[i];
         }
         new Model().defaultValues( params_, paramIsFixed_, forward_, t_ );
         updateModelInstance();
      }

      public void updateModelInstance()
      {
         // forward might have changed
         Utils.QL_REQUIRE( forward_ > 0.0, () => "forward must be positive: " + forward_ + " not allowed" );
         modelInstance_ = new Model().instance( t_, forward_, params_ );
         model_ = new Model();
      }

      /*! Expiry, Forward */
      public double t_;
      public double forward_;
      /*! Parameters */
      public List<double?> params_;
      public List<bool> paramIsFixed_;
      public List<double> weights_;
      /*! Interpolation results */
      public double? error_, maxError_;
      public EndCriteria.Type XABREndCriteria_;
      /*! Model instance (if required) */
      public IWrapper modelInstance_;
      public IModel model_;

   }

   //template <class I1, class I2, typename Model>
   public class XABRInterpolationImpl<Model> : Interpolation.templateImpl where Model : IModel, new()
   {
      public XABRInterpolationImpl( List<double> xBegin, int size, List<double> yBegin, double t,
                                    double forward, List<double?> _params,
                                    List<bool> paramIsFixed, bool vegaWeighted,
                                    EndCriteria endCriteria,
                                    OptimizationMethod optMethod,
                                    double errorAccept, bool useMaxError, int maxGuesses )
         : base( xBegin, size, yBegin )
      {
          // XABRCoeffHolder<Model>(t, forward, params, paramIsFixed),
          endCriteria_ = endCriteria; 
          optMethod_ = optMethod;
          errorAccept_ = errorAccept; 
          useMaxError_ = useMaxError;
          maxGuesses_ = maxGuesses; 
          forward_ = forward;
          vegaWeighted_ = vegaWeighted;

         // if no optimization method or endCriteria is provided, we provide one
         if (optMethod_ == null)
            optMethod_ = new LevenbergMarquardt(1e-8, 1e-8, 1e-8);
            // optMethod_ = boost::shared_ptr<OptimizationMethod>(new
            //    Simplex(0.01));
         if (endCriteria_ == null) 
         {
            endCriteria_ = new EndCriteria(60000, 100, 1e-8, 1e-8, 1e-8);
         }
         coeff_ = new XABRCoeffHolder<Model>( t, forward, _params, paramIsFixed );
         this.coeff_.weights_ = new InitializedList<double>( size, 1.0 / size );
      }
      
      public override void update()
      {
         this.coeff_.updateModelInstance();

         // we should also check that y contains positive values only

         // we must update weights if it is vegaWeighted
         if (vegaWeighted_) 
         {
            coeff_.weights_.Clear();
            double weightsSum = 0.0;

            for ( int i = 0; i < xBegin_.Count; i++ )
            {
               double stdDev = Math.Sqrt( ( yBegin_[i] ) * ( yBegin_[i] ) * this.coeff_.t_ );
               coeff_.weights_.Add( Utils.blackFormulaStdDevDerivative( xBegin_[i], forward_, stdDev ) );
               weightsSum += coeff_.weights_.Last();
            }

            // weight normalization
            for( int i = 0 ; i < coeff_.weights_.Count; i++ )
               coeff_.weights_[i] /=  weightsSum;
        }

        // there is nothing to optimize
        if ( coeff_.paramIsFixed_.Aggregate( ( a, b ) => b & a ) )
        {
           coeff_.error_ = interpolationError();
           coeff_.maxError_ = interpolationMaxError();
           coeff_.XABREndCriteria_ = EndCriteria.Type.None;
           return;
        }
        else
        {
           XABRError costFunction = new XABRError( this );

           Vector guess = new Vector( coeff_.model_.dimension() );
           for ( int i = 0; i < guess.size(); ++i )
                guess[i] = coeff_.params_[i].Value;

            int iterations = 0;
            int freeParameters = 0;
            double bestError = double.MaxValue;
            Vector bestParameters = new Vector();
            for (int i = 0; i < coeff_.model_.dimension(); ++i)
                if (!coeff_.paramIsFixed_[i])
                    ++freeParameters;
            HaltonRsg halton = new HaltonRsg(freeParameters, 42);
            EndCriteria.Type tmpEndCriteria;
            double tmpInterpolationError;

            do {

                if (iterations > 0) {
                   Sample<List<double> > s = halton.nextSequence();
                    coeff_.model_.guess(guess, coeff_.paramIsFixed_, forward_, coeff_.t_, s.value);
                    for (int i = 0; i < coeff_.paramIsFixed_.Count; ++i)
                        if (coeff_.paramIsFixed_[i])
                            guess[i] = coeff_.params_[i].Value;
                }

                Vector inversedTransformatedGuess = new Vector(coeff_.model_.inverse(guess, coeff_.paramIsFixed_, coeff_.params_, forward_));

                ProjectedCostFunction rainedXABRError = new ProjectedCostFunction(costFunction, inversedTransformatedGuess, 
                                                                                  coeff_.paramIsFixed_);

                Vector projectedGuess = new Vector(rainedXABRError.project(inversedTransformatedGuess));

                NoConstraint raint = new NoConstraint();
                Problem problem = new Problem(rainedXABRError, raint, projectedGuess);
                tmpEndCriteria = optMethod_.minimize(problem, endCriteria_);
               // TEST
               // coeff_.params_[3] = 0.0099999966589763966;
               // TEST
                Vector projectedResult = new Vector(problem.currentValue());
                Vector transfResult = new Vector(rainedXABRError.include(projectedResult));
                //transfResult[3] = 0.0099999966589763966; ;
                Vector result = coeff_.model_.direct( transfResult, coeff_.paramIsFixed_, coeff_.params_, forward_ );
                //result[3] = 0.0099999966589763966;
                tmpInterpolationError = useMaxError_ ? interpolationMaxError()
                                                     : interpolationError();

                if (tmpInterpolationError < bestError) 
                {
                    bestError = tmpInterpolationError;
                    bestParameters = result;
                    coeff_.XABREndCriteria_ = tmpEndCriteria;
                }

            } while (++iterations < maxGuesses_ &&
                     tmpInterpolationError > errorAccept_);

            for (int i = 0; i < bestParameters.size(); ++i)
               coeff_.params_[i] = bestParameters[i];

            coeff_.error_ = interpolationError();
            coeff_.maxError_ = interpolationMaxError();
        }
    }

      public override double value( double x )
      {
         Utils.QL_REQUIRE( x > 0.0, () => "strike must be positive: " + x + " not allowed" );
         return coeff_.modelInstance_.volatility( x );
      }

      public override double primitive( double d ) { Utils.QL_FAIL( "XABR primitive not implemented" ); return 0; }
      public override double derivative( double d ) { Utils.QL_FAIL( "XABR derivative not implemented" ); return 0; }
      public override double secondDerivative( double d ) { Utils.QL_FAIL( "XABR secondDerivative not implemented" ); return 0; }

      // calculate total squared weighted difference (L2 norm)
      public double interpolationSquaredError()  
      {
         double error, totalError = 0.0;
         for ( int i = 0; i < xBegin_.Count; i++ )
         {
            error = ( value( xBegin_[i] ) - yBegin_[i] );
            totalError += error * error * ( coeff_.weights_[i] );
         }
         return totalError;
      }

      // calculate weighted differences
      public Vector interpolationErrors( Vector v )  
      {
         Vector results = new Vector(xBegin_.Count);

         for ( int i = 0 ; i < xBegin_.Count ; i++)
            results[i] = (value(xBegin_[i]) - yBegin_[i]) * Math.Sqrt(coeff_.weights_[i]);

         return results;


    }

      public double interpolationError()  
      {
        int n = xBegin_.Count;
        double squaredError = interpolationSquaredError();
        return Math.Sqrt(n * squaredError / (n - 1));
    }

      public double interpolationMaxError()  
      {
         double error, maxError = Double.MinValue;

          for ( int i = 0 ; i < xBegin_.Count ; i++)
          {
               error = Math.Abs(value(xBegin_[i]) - yBegin_[i]);
               maxError = Math.Max(maxError, error);
          }

        return maxError;
    }


      private class XABRError : CostFunction
      {
         public XABRError( XABRInterpolationImpl<Model> xabr ) { xabr_ = xabr; }

         public override double value( Vector x )
         {
            Vector y = new Model().direct( x, xabr_.coeff_.paramIsFixed_, xabr_.coeff_.params_, xabr_.forward_ );
            for ( int i = 0; i < xabr_.coeff_.params_.Count; ++i )
               xabr_.coeff_.params_[i] = y[i];
            xabr_.coeff_.updateModelInstance();
            return xabr_.interpolationSquaredError();
         }

         public override Vector values( Vector x )
         {
            Vector y = new Model().direct( x, xabr_.coeff_.paramIsFixed_, xabr_.coeff_.params_, xabr_.forward_ );
            for ( int i = 0; i < xabr_.coeff_.params_.Count; ++i )
               xabr_.coeff_.params_[i] = y[i];
            xabr_.coeff_.updateModelInstance();
            return xabr_.interpolationErrors( x );
         }

         private XABRInterpolationImpl<Model> xabr_;
      }

      private EndCriteria endCriteria_;
      private OptimizationMethod optMethod_;
      private double errorAccept_;
      private bool useMaxError_;
      private int maxGuesses_;
      private double forward_;
      private bool vegaWeighted_;
      //private NoConstraint constraint_;
      public XABRCoeffHolder<Model> coeff_;
   };

}
