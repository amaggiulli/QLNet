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
   public class CapFloorTermVolSurface : CapFloorTermVolatilityStructure
   {
      //! floating reference date, floating market data
      public CapFloorTermVolSurface( int settlementDays,
                                     Calendar calendar,
                                     BusinessDayConvention bdc,
                                     List<Period> optionTenors,
                                     List<double> strikes,
                                     List<List<Handle<Quote> > > vols,
                                     DayCounter dc = null)
         :base(settlementDays, calendar, bdc, dc ?? new Actual365Fixed())
      {
         nOptionTenors_ = optionTenors.Count;
         optionTenors_ = optionTenors;
         optionDates_ = new InitializedList<Date>(nOptionTenors_);
         optionTimes_ = new InitializedList<double>(nOptionTenors_);
         nStrikes_ = strikes.Count;
         strikes_ = strikes;
         volHandles_ = vols;
         vols_ = new Matrix(vols.Count, vols[0].Count);

                 
         checkInputs();
         initializeOptionDatesAndTimes();
         for (int i=0; i<nOptionTenors_; ++i)
            Utils.QL_REQUIRE(volHandles_[i].Count==nStrikes_,()=>
                             (i+1) + " row of vol handles has size " +
                             volHandles_[i].Count + " instead of " + nStrikes_);
         registerWithMarketData();
         for (int i=0; i<vols_.rows(); ++i)
            for (int j=0; j<vols_.columns(); ++j)
                vols_[i,j] = volHandles_[i][j].link.value();
         interpolate();
      }

      //! fixed reference date, floating market data
      public CapFloorTermVolSurface( Date settlementDate,
                                     Calendar calendar,
                                     BusinessDayConvention bdc,
                                     List<Period> optionTenors,
                                     List<double> strikes,
                                     List<List<Handle<Quote> > > vols,
                                     DayCounter dc = null)
         :base(settlementDate, calendar, bdc, dc?? new Actual365Fixed())
      {
         nOptionTenors_ = optionTenors.Count;
         optionTenors_ = optionTenors;
         optionDates_ = new InitializedList<Date>( nOptionTenors_ );
         optionTimes_ = new InitializedList<double>( nOptionTenors_ );
         nStrikes_ = strikes.Count;
         strikes_ = strikes;
         volHandles_ = vols;
         vols_ = new Matrix( vols.Count, vols[0].Count );

         checkInputs();
         initializeOptionDatesAndTimes();
         for (int i=0; i<nOptionTenors_; ++i)
            Utils.QL_REQUIRE(volHandles_[i].Count==nStrikes_,()=>
                       (i+1) + " row of vol handles has size " + volHandles_[i].Count + " instead of " + nStrikes_);
         registerWithMarketData();
         for (int i=0; i<vols_.rows(); ++i)
            for (int j=0; j<vols_.columns(); ++j)
                vols_[i,j] = volHandles_[i][j].link.value();
         interpolate();
      }

      //! fixed reference date, fixed market data
      public CapFloorTermVolSurface( Date settlementDate,
                                     Calendar calendar,
                                     BusinessDayConvention bdc,
                                     List<Period> optionTenors,
                                     List<double> strikes,
                                     Matrix vols,
                                     DayCounter dc = null)
         : base( settlementDate, calendar, bdc, dc ?? new Actual365Fixed() )
      {
         nOptionTenors_ = optionTenors.Count;
         optionTenors_ = optionTenors;
         optionDates_ = new InitializedList<Date>( nOptionTenors_ );
         optionTimes_ = new InitializedList<double>( nOptionTenors_ );
         nStrikes_ = strikes.Count;
         strikes_ = strikes;
         volHandles_ = new InitializedList<List<Handle<Quote>>>( vols.rows() );
         vols_ = vols;

         checkInputs();
         initializeOptionDatesAndTimes();
         // fill dummy handles to allow generic handle-based computations later
         for (int i=0; i<nOptionTenors_; ++i) 
         {
            volHandles_[i] = new InitializedList<Handle<Quote>>(nStrikes_); 
            for (int j=0; j<nStrikes_; ++j)
                volHandles_[i][j] = new Handle<Quote>(new SimpleQuote(vols_[i,j]));
        }
        interpolate();
      }

      //! floating reference date, fixed market data
      public CapFloorTermVolSurface( int settlementDays,
                                     Calendar calendar,
                                     BusinessDayConvention bdc,
                                     List<Period> optionTenors,
                                     List<double> strikes,
                                     Matrix vols,
                                     DayCounter dc = null)
         : base( settlementDays, calendar, bdc, dc ?? new Actual365Fixed() )
      {
         nOptionTenors_ = optionTenors.Count;
         optionTenors_ = optionTenors;
         optionDates_ = new InitializedList<Date>( nOptionTenors_ );
         optionTimes_ = new InitializedList<double>( nOptionTenors_ );
         nStrikes_ = strikes.Count;
         strikes_ = strikes;
         volHandles_ = new InitializedList<List<Handle<Quote>>>( vols.rows() );
         vols_ = vols;

         checkInputs();
         initializeOptionDatesAndTimes();
         // fill dummy handles to allow generic handle-based computations later
         for (int i=0; i<nOptionTenors_; ++i) 
         {
            volHandles_[i] = new InitializedList<Handle<Quote>>(nStrikes_);
            for (int j=0; j<nStrikes_; ++j)
                volHandles_[i][j] = new Handle<Quote>(new SimpleQuote(vols_[i,j]));
        }
        interpolate();
      }
      //! \name TermStructure interface
      //@{
      public override Date maxDate()
      {
         calculate();
         return optionDateFromTenor(optionTenors_.Last());
      }
      //@}
      //! \name VolatilityTermStructure interface
      //@{
      public override double minStrike() { return strikes_.First(); }
      public override double maxStrike() { return strikes_.Last(); }
      //@}
      //! \name LazyObject interface
      //@{
      public override void update()
      {
         // recalculate dates if necessary...
          if (moving_) {
            Date d = Settings.evaluationDate() ;
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

         for ( int i = 0; i < nOptionTenors_; ++i )
            for ( int j = 0; j < nStrikes_; ++j )
               vols_[i,j] = volHandles_[i][j].link.value();

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
      public List<double> strikes() {return strikes_;}
      //@}
      protected override double volatilityImpl(double t,double strike)
      {
         calculate();
         return interpolation_.value(strike, t, true);
      }
      
      private void checkInputs()
      {
         Utils.QL_REQUIRE(!optionTenors_.empty(),()=> "empty option tenor vector");
         Utils.QL_REQUIRE(nOptionTenors_==vols_.rows(),()=>
                          "mismatch between number of option tenors (" +
                          nOptionTenors_ + ") and number of volatility rows (" +
                          vols_.rows() + ")");
         Utils.QL_REQUIRE(optionTenors_[0]> new Period(0,TimeUnit.Days),()=>
                          "negative first option tenor: " + optionTenors_[0]);
         for (int i=1; i<nOptionTenors_; ++i)
            Utils.QL_REQUIRE(optionTenors_[i]>optionTenors_[i-1],()=>
                             "non increasing option tenor: " + i +
                             " is " + optionTenors_[i-1] + ", " +
                             (i+1) + " is " + optionTenors_[i]);

        Utils.QL_REQUIRE(nStrikes_==vols_.columns(),()=>
                         "mismatch between strikes(" + strikes_.Count +
                         ") and vol columns (" + vols_.columns() + ")");
        for (int j=1; j<nStrikes_; ++j)
            Utils.QL_REQUIRE(strikes_[j-1]<strikes_[j],()=>
                             "non increasing strikes: " + j +
                             " is " + strikes_[j-1] + ", " +
                             (j+1) + " is " + strikes_[j]);
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
         for ( int i = 0; i < nOptionTenors_; ++i )
            for ( int j = 0; j < nStrikes_; ++j )
               volHandles_[i][j] .registerWith(update);
      }
      private void interpolate()
      {
         interpolation_ = new BicubicSpline( strikes_,strikes_.Count,optionTimes_, optionTimes_.Count, vols_ );
      }
        
      private int nOptionTenors_;
      private List<Period> optionTenors_;
      private List<Date> optionDates_;
      private List<double> optionTimes_;
      private Date evaluationDate_;

      private int nStrikes_;
      private List<double> strikes_;

      private List<List<Handle<Quote> > > volHandles_;
      private Matrix vols_;

      // make it not mutable if possible
      private Interpolation2D interpolation_;

   }
}
