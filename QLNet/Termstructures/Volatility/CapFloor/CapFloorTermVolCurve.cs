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

using System.Collections.Generic;
using System.Linq;

namespace QLNet
{
   //! Cap/floor at-the-money term-volatility vector
   /*! This class provides the at-the-money volatility for a given cap/floor
       interpolating a volatility vector whose elements are the market
       volatilities of a set of caps/floors with given length.
   */
   public class CapFloorTermVolCurve : CapFloorTermVolatilityStructure
   {
      //! floating reference date, floating market data
      public CapFloorTermVolCurve( int settlementDays,
                                   Calendar calendar,
                                   BusinessDayConvention bdc,
                                   List<Period> optionTenors,
                                   List<Handle<Quote> > vols,
                                   DayCounter dc = null ) // Actual365Fixed()
         :base(settlementDays, calendar, bdc, dc?? new Actual365Fixed())
      {
         nOptionTenors_ = optionTenors.Count;
         optionTenors_ = optionTenors;
         optionDates_ = new InitializedList<Date>(nOptionTenors_);
         optionTimes_ = new InitializedList<double>(nOptionTenors_);
         volHandles_ = vols;
         vols_ = new InitializedList<double>( vols.Count); // do not initialize with nOptionTenors_

         checkInputs();
         initializeOptionDatesAndTimes();
         registerWithMarketData();
         interpolate();
      }

      //! fixed reference date, floating market data
      public CapFloorTermVolCurve( Date settlementDate,
                                   Calendar calendar,
                                   BusinessDayConvention bdc,
                                   List<Period> optionTenors,
                                   List<Handle<Quote> > vols,
                                   DayCounter dc = null ) // Actual365Fixed()
         :base(settlementDate, calendar, bdc, dc?? new Actual365Fixed())
      {
         nOptionTenors_ = optionTenors.Count;
         optionTenors_ = optionTenors;
         optionDates_ = new InitializedList<Date>( nOptionTenors_ );
         optionTimes_ = new InitializedList<double>( nOptionTenors_ );
         volHandles_ = vols;
         vols_ = new InitializedList<double>( vols.Count ); // do not initialize with nOptionTenors_

         checkInputs();
         initializeOptionDatesAndTimes();
         registerWithMarketData();
         interpolate();
      }
      //! fixed reference date, fixed market data
      public CapFloorTermVolCurve( Date settlementDate,
                                   Calendar calendar,
                                   BusinessDayConvention bdc,
                                   List<Period> optionTenors,
                                   List<double> vols,
                                   DayCounter dc = null ) // Actual365Fixed()
         :base(settlementDate, calendar, bdc, dc??new Actual365Fixed())
      {
         nOptionTenors_ = optionTenors.Count;
         optionTenors_ = optionTenors;
         optionDates_ = new InitializedList<Date>( nOptionTenors_ );
         optionTimes_ = new InitializedList<double>( nOptionTenors_ );
         volHandles_ = new InitializedList<Handle<Quote>>(vols.Count);
         vols_ = vols; // do not initialize with nOptionTenors_

         checkInputs();
         initializeOptionDatesAndTimes();
         // fill dummy handles to allow generic handle-based computations later
         for (int i=0; i<nOptionTenors_; ++i)
            volHandles_[i] = new Handle<Quote>(new SimpleQuote(vols_[i]));
         interpolate();
      }
      //! floating reference date, fixed market data
      public CapFloorTermVolCurve( int settlementDays,
                                   Calendar calendar,
                                   BusinessDayConvention bdc,
                                   List<Period> optionTenors,
                                   List<double> vols,
                                   DayCounter dc = null) // Actual365Fixed()
         :base(settlementDays, calendar, bdc, dc??new Actual365Fixed())
      {
         nOptionTenors_ = optionTenors.Count;
         optionTenors_ = optionTenors;
         optionDates_ = new InitializedList<Date>( nOptionTenors_ );
         optionTimes_ = new InitializedList<double>( nOptionTenors_ );
         volHandles_ = new InitializedList<Handle<Quote>>( vols.Count );
         vols_ = vols; // do not initialize with nOptionTenors_

         checkInputs();
         initializeOptionDatesAndTimes();
         // fill dummy handles to allow generic handle-based computations later
         for (int i=0; i<nOptionTenors_; ++i)
            volHandles_[i] = new Handle<Quote>(new SimpleQuote(vols_[i]));
         interpolate();
         
      }
      //! \name TermStructure interface
      //@{
      public override Date maxDate()
      {
         calculate();
         return optionDateFromTenor( optionTenors_.Last() );
      }
      //@}
      //! \name VolatilityTermStructure interface
      //@{
      public override double minStrike() { return double.MinValue; }
      public override double maxStrike() { return double.MaxValue; }
      //@}
      //! \name LazyObject interface
      //@{
      public override void update()
      {
         // recalculate dates if necessary...
         if (moving_)
         {
            Date d = QLNet.Settings.evaluationDate();
            if (evaluationDate_ != d)
            {
               evaluationDate_ = d;
               initializeOptionDatesAndTimes();
            }
         }
         base.update();
      }

      protected override void performCalculations()
      {
         // check if date recalculation must be called here

         for ( int i = 0; i < vols_.Count; ++i )
            vols_[i] = volHandles_[i].link.value();

         interpolation_.update();
      }
      //@}
      //! \name some inspectors
      //@{
      public List<Period> optionTenors() { return optionTenors_; }
      public List<Date> optionDates()
      {
         // what if quotes are not available?
         calculate();
         return optionDates_;
      }
      public List<double> optionTimes()
      {
         // what if quotes are not available?
         calculate();
         return optionTimes_;
      }
      //@}
      
      protected override double volatilityImpl(double t,double r)
      {
         calculate();
         return interpolation_.value( t, true );
      }
      
      private void checkInputs()
      {
         Utils.QL_REQUIRE(!optionTenors_.empty(),()=> "empty option tenor vector");
         Utils.QL_REQUIRE(nOptionTenors_==vols_.Count,()=>
                   "mismatch between number of option tenors (" +
                   nOptionTenors_ + ") and number of volatilities (" +
                   vols_.Count + ")");
         Utils.QL_REQUIRE(optionTenors_[0]> new Period(0,TimeUnit.Days),()=> 
            "negative first option tenor: " + optionTenors_[0]);
         for (int i=1; i<nOptionTenors_; ++i)
            Utils.QL_REQUIRE(optionTenors_[i]>optionTenors_[i-1],()=>
                       "non increasing option tenor: " + (i) +
                       " is " + optionTenors_[i-1] + ", " +
                       (i+1) + " is " + optionTenors_[i]);
      }
      private void initializeOptionDatesAndTimes()
      {
         for ( int i = 0; i < nOptionTenors_; ++i )
         {
            optionDates_[i] = optionDateFromTenor( optionTenors_[i] );
            optionTimes_[i] = timeFromReference( optionDates_[i] );
         }
      }

      private void registerWithMarketData()
      {
         for (int i = 0; i < volHandles_.Count; ++i)
            volHandles_[i].registerWith(update);
      }
      private void interpolate()
      {
         interpolation_ = new CubicInterpolation( optionTimes_, optionTimes_.Count,vols_,
                                                  CubicInterpolation.DerivativeApprox.Spline, false,
                                                  CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0,
                                                  CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0 );
      }

      int nOptionTenors_;
      List<Period> optionTenors_;
      List<Date> optionDates_;
      List<double> optionTimes_;
      Date evaluationDate_;

      List<Handle<Quote> > volHandles_;
      List<double> vols_;

      // make it not mutable if possible
      Interpolation interpolation_;

   }
}
