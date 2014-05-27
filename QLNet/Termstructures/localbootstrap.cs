/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2014 Andrea Maggiulli (a.maggiulli@gmail.com)
 Copyright (C) 2014  Edem Dawui (edawui@gmail.com)
  
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

namespace QLNet 
{
	public class LocalBootstrapForYield : LocalBootstrap<PiecewiseYieldCurve, YieldTermStructure>
	{
		public LocalBootstrapForYield()
			: base()
		{}
	}

    // penalty function class for solving using a multi-dimensional solver
	public class PenaltyFunction<T, U> : CostFunction
		where T : Curve<U>, new()
		where U : TermStructure
	{
        //typedef typename Curve::traits_type Traits;
        //typedef typename Traits::helper helper;
        //typedef typename std::vector< boost::shared_ptr<helper> >::const_iterator helper_iterator;

        private T curve_;
        private int initialIndex_;
        private int localisation_, start_, end_;
		  private List<BootstrapHelper<U>> rateHelpers_;

		  public PenaltyFunction( T curve, int initialIndex, List<BootstrapHelper<U>> rateHelpers, int start, int end )
        {
            curve_ = curve;
            initialIndex_ = initialIndex;
            rateHelpers_ = rateHelpers;
            start_ = start;
            end_ = end;
            localisation_ = end - start;
        }

        public override double value(Vector x) {
            x.ForEach((j, v) => curve_.updateGuess(curve_.data_, v, j + initialIndex_));

            curve_.interpolation_.update();

            double penalty = rateHelpers_.GetRange(start_, localisation_)
                                         .Aggregate(0.0, (acc, v) => Math.Abs(v.quoteError()));
            return penalty;
        }

        public override Vector values(Vector x) {
            x.ForEach((j, v) => curve_.updateGuess(curve_.data_, v, j + initialIndex_));

            curve_.interpolation_.update();

            var penalties = rateHelpers_.GetRange(start_, localisation_).Select(c => Math.Abs(c.quoteError())).ToList();
            return new Vector(penalties);
        }
    }


    //! Localised-term-structure bootstrapper for most curve types.
    /*! This algorithm enables a localised fitting for non-local
        interpolation methods.

        As in the similar class (IterativeBootstrap) the input term
        structure is solved on a number of market instruments which
        are passed as a vector of handles to BootstrapHelper
        instances. Their maturities mark the boundaries of the
        interpolated segments.

        Unlike the IterativeBootstrap class, the solution for each
        interpolated segment is derived using a local
        approximation. This restricts the risk profile s.t.  the risk
        is localised. Therefore, we obtain a local IR risk profile
        whilst using a smoother interpolation method. Particularly
        good for the convex-monotone spline method.
    */
    public class LocalBootstrap <T,U>:IBootStrap<T>
        where T : Curve<U>, new()
        where U : TermStructure
	 {
        //typedef typename Curve::traits_type Traits;
        //typedef typename Curve::interpolator_type Interpolator;
      
        private bool validCurve_;
        private T ts_; // yes, it is a workaround
        int localisation_;
        bool forcePositive_;
        
        //public LocalBootstrap(Size localisation = 2, bool forcePositive = true) {
        public LocalBootstrap() : this(2, true) { }
        public LocalBootstrap(int localisation, bool forcePositive) {
            localisation_ = localisation;
            forcePositive_ = forcePositive;
        }

        public void setup(T ts) {
            ts_ = ts;

            int n = ts_.instruments_.Count;
            if (!(n >= ts_.interpolator_.requiredPoints))
                throw new ArgumentException("not enough instruments: " + n + " provided, " +
                       (ts_.interpolator_.requiredPoints) + " required");

            if (!(n > localisation_))
                throw new ApplicationException("not enough instruments: " + n + " provided, " + localisation_ + " required.");

            //ts_.instruments_.ForEach(i => i.registerWith(ts_.update));
				ts_.instruments_.ForEach( x => ts_.registerWith( x ) ); 
        }

        public void calculate() {

            validCurve_ = false;
            int nInsts = ts_.instruments_.Count, i;

            // ensure rate helpers are sorted
            ts_.instruments_.Sort((x, y) => x.latestDate().CompareTo(y.latestDate()));

            // check that there is no instruments with the same maturity
            for (i = 1; i < nInsts; ++i) {
                Date m1 = ts_.instruments_[i - 1].latestDate(),
                     m2 = ts_.instruments_[i].latestDate();
                if (m1 == m2) throw new ArgumentException("two instruments have the same maturity (" + m1 + ")");
            }

            // check that there is no instruments with invalid quote
            if ((i = ts_.instruments_.FindIndex(x => !x.quoteIsValid())) != -1)
                throw new ArgumentException("instrument " + i + " (maturity: " + ts_.instruments_[i].latestDate() +
                       ") has an invalid quote");

            // setup instruments and register with them
				ts_.instruments_.ForEach( j => ts_.setTermStructure( j ) );

            // set initial guess only if the current curve cannot be used as guess
            if (validCurve_) {
                if (ts_.data_.Count != nInsts + 1)
                    throw new ArgumentException("dimension mismatch: expected " + nInsts + 1 + ", actual " + ts_.data_.Count);
            } else {
                ts_.data_ = new InitializedList<double>(nInsts + 1);
                ts_.data_[0] = ts_.initialValue();
            }

            // calculate dates and times
            ts_.dates_ = new InitializedList<Date>(nInsts + 1);
            ts_.times_ = new InitializedList<double>(nInsts + 1);
            ts_.dates_[0] = ts_.initialDate();
            ts_.times_[0] = ts_.timeFromReference(ts_.dates_[0]);
            for (i = 0; i < nInsts; ++i) {
                ts_.dates_[i + 1] = ts_.instruments_[i].latestDate();
                ts_.times_[i + 1] = ts_.timeFromReference(ts_.dates_[i + 1]);
                if (!validCurve_)
                    ts_.data_[i+1] = ts_.data_[i];
            }

            LevenbergMarquardt solver = new LevenbergMarquardt(ts_.accuracy_, ts_.accuracy_, ts_.accuracy_);
            EndCriteria endCriteria = new EndCriteria(100, 10, 0.00, ts_.accuracy_, 0.00);
            PositiveConstraint posConstraint = new PositiveConstraint();
            NoConstraint noConstraint = new NoConstraint();
            Constraint solverConstraint = forcePositive_ ? (Constraint)posConstraint : (Constraint)noConstraint;

            // now start the bootstrapping.
            int iInst = localisation_ - 1;

            int dataAdjust = (ts_.interpolator_ as ConvexMonotone).dataSizeAdjustment;

            do {
                int initialDataPt = iInst+1-localisation_+dataAdjust;
                Vector startArray = new Vector(localisation_+1-dataAdjust);
                for (int j = 0; j < startArray.size()-1; ++j)
                    startArray[j] = ts_.data_[initialDataPt+j];

                // here we are extending the interpolation a point at a
                // time... but the local interpolator can make an
                // approximation for the final localisation period.
                // e.g. if the localisation is 2, then the first section
                // of the curve will be solved using the first 2
                // instruments... with the local interpolator making
                // suitable boundary conditions.
                ts_.interpolation_ = (ts_.interpolator_ as ConvexMonotone).localInterpolate(ts_.times_, iInst + 2, ts_.data_, 
                                                localisation_, ts_.interpolation_ as ConvexMonotoneInterpolation, nInsts + 1);

                if (iInst >= localisation_) {
						 startArray[localisation_ - dataAdjust] = ts_.guess( iInst, ts_, false, 0 );
                } else {
                    startArray[localisation_-dataAdjust] = ts_.data_[0];
                }

					 var currentCost = new PenaltyFunction<T, U>( ts_, initialDataPt, ts_.instruments_,
																									 iInst - localisation_ + 1, iInst + 1 );
					 Problem toSolve = new Problem( currentCost, solverConstraint, startArray );
                EndCriteria.Type endType = solver.minimize(toSolve, endCriteria);

                // check the end criteria
                if (!(endType == EndCriteria.Type.StationaryFunctionAccuracy ||
                           endType == EndCriteria.Type.StationaryFunctionValue))
                    throw new ApplicationException("Unable to strip yieldcurve to required accuracy ");
                ++iInst;
            } while (iInst < nInsts);

            validCurve_ = true;
        }
    }
}
