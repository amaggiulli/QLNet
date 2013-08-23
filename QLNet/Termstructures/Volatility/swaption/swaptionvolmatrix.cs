/*
 Copyright (C) 2009 Philippe Real (ph_real@hotmail.com)
  
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
    //! At-the-money swaption-volatility matrix
    /*! This class provides the at-the-money volatility for a given
        swaption by interpolating a volatility matrix whose elements
        are the market volatilities of a set of swaption with given
        option date and swapLength.

        The volatility matrix <tt>M</tt> must be defined so that:
        - the number of rows equals the number of option dates;
        - the number of columns equals the number of swap tenors;
        - <tt>M[i][j]</tt> contains the volatility corresponding
          to the <tt>i</tt>-th option and <tt>j</tt>-th tenor.
    */
    public class SwaptionVolatilityMatrix :  SwaptionVolatilityDiscrete
    {
        //! floating reference date, floating market data
        public SwaptionVolatilityMatrix(
                    Calendar calendar,
                    BusinessDayConvention bdc,
                    List<Period> optionTenors,
                    List<Period> swapTenors,
                    List<List<Handle<Quote> > > vols,
                    DayCounter dayCounter)
            : base(optionTenors, swapTenors, 0, calendar, bdc, dayCounter)
        {
            volHandles_=vols;
            volatilities_ = new Matrix(vols.Count, vols.First().Count );
            checkInputs(volatilities_.rows(), volatilities_.columns());
            registerWithMarketData();
            interpolation_ =
            new BilinearInterpolation(swapLengths_, swapLengths_.Count,
            optionTimes_, optionTimes_.Count,
            volatilities_);
        }
        
        //! fixed reference date, floating market data
        public SwaptionVolatilityMatrix(
                    Date referenceDate,
                    Calendar calendar,
                    BusinessDayConvention bdc,
                    List<Period> optionTenors,
                    List<Period> swapTenors,
                    List<List<Handle<Quote> > > vols,
                    DayCounter dayCounter)
            : base(optionTenors, swapTenors, referenceDate, calendar, bdc, dayCounter)
        {
            volHandles_ = vols;
            volatilities_ = new Matrix(vols.Count, vols.First().Count );
            checkInputs(volatilities_.rows(), volatilities_.columns());
            registerWithMarketData();
            interpolation_ =new BilinearInterpolation(swapLengths_, swapLengths_.Count,
                                                        optionTimes_, optionTimes_.Count,
                                                        volatilities_);
        }
        
        //! floating reference date, fixed market data
        public SwaptionVolatilityMatrix(
                    Calendar calendar,
                    BusinessDayConvention bdc,
                    List<Period> optionTenors,
                    List<Period> swapTenors,
                    Matrix vols,
                    DayCounter dayCounter)

            : base(optionTenors, swapTenors, 0, calendar, bdc, dayCounter)
        {
            volHandles_ = new InitializedList<List<Handle<Quote>>>(vols.rows());
            volatilities_ = new Matrix(vols.rows(), vols.columns());
            checkInputs(vols.rows(), vols.columns());

            // fill dummy handles to allow generic handle-based
            // computations later on
            for (int i=0; i<vols.rows(); ++i) {
                volHandles_[i] = new InitializedList<Handle<Quote>>(vols.columns());
                for (int j=0; j<vols.columns(); ++j)
                    volHandles_[i][j] = new Handle<Quote>((new
                        SimpleQuote(vols[i,j])));
            }

            interpolation_ =
            new BilinearInterpolation(swapLengths_, swapLengths_.Count,
            optionTimes_, optionTimes_.Count,
            volatilities_);
        }
        
        //! fixed reference date, fixed market data
        public SwaptionVolatilityMatrix(
                    Date referenceDate,
                    Calendar calendar,
                    BusinessDayConvention bdc,
                    List<Period> optionTenors,
                    List<Period> swapTenors,
                    Matrix vols,
                    DayCounter dayCounter)
            : base(optionTenors, swapTenors, referenceDate, calendar, bdc, dayCounter)
        {
            volHandles_ = new InitializedList<List<Handle<Quote>>>(vols.rows());
            volatilities_ = new Matrix(vols.rows(), vols.columns());
            checkInputs(vols.rows(), vols.columns());

            // fill dummy handles to allow generic handle-based
            // computations later on
            for (int i = 0; i < vols.rows()-1; ++i)
            {
                volHandles_[i] = new InitializedList<Handle<Quote>>(vols.columns());
                for (int j = 0; j < vols.columns(); ++j)
                    volHandles_[i][j] = new Handle<Quote>((new
                        SimpleQuote(vols[i, j])));
            }

            interpolation_ =
            new BilinearInterpolation(swapLengths_, swapLengths_.Count,
            optionTimes_, optionTimes_.Count,
            volatilities_);
        }

        // fixed reference date and fixed market data, option dates
        public SwaptionVolatilityMatrix(Date today,
                                 List<Date> optionDates,
                                 List<Period> swapTenors,
                                 Matrix vols,
                                 DayCounter dayCounter)
            : base(optionDates, swapTenors, today, new Calendar(), BusinessDayConvention.Following, dayCounter)
        {
            volHandles_ = new InitializedList<List<Handle<Quote>>>(vols.rows());
            volatilities_ = new Matrix(vols.rows(), vols.columns());
            checkInputs(vols.rows(), vols.columns());

            // fill dummy handles to allow generic handle-based
            // computations later on
            for (int i = 0; i < vols.rows(); ++i){
                volHandles_[i] = new InitializedList<Handle<Quote>>(vols.columns());
                for (int j = 0; j < vols.columns(); ++j)
                    volHandles_[i][j] = new Handle<Quote>((new
                        SimpleQuote(vols[i, j])));
            }

            interpolation_ =
            new BilinearInterpolation(swapLengths_, swapLengths_.Count,
            optionTimes_, optionTimes_.Count,
            volatilities_);
        }

        //! \name LazyObject interface
        //@{
        //verifier protected QL public!!
        protected override void performCalculations() 
        {
            base.performCalculations();

            // we might use iterators here...
            for (int i=0; i<volatilities_.rows(); ++i)
                for (int j=0; j<volatilities_.columns(); ++j)
                    volatilities_[i,j] = volHandles_[i][j].link.value() ;
        }

        //@}
        //! \name TermStructure interface
        //@{   
        public override Date maxDate()  {
            return optionDates_.Last();
        }

        //@}
        //! \name VolatilityTermStructure interface
        //@{
        public override double minStrike()  {
            return double.MinValue;
        }

        public override double maxStrike()  {
            return double.MaxValue;
        }
        //@}
        //! \name SwaptionVolatilityStructure interface
        //@{
        public override Period maxSwapTenor()  {
            return swapTenors_.Last();
        }

        //@}
        //! \name Other inspectors
        //@{
        //! returns the lower indexes of surrounding volatility matrix corners
        public KeyValuePair<int,int> locate(  Date optionDate,
                                              Period swapTenor)  {
            return locate(timeFromReference(optionDate),
                          swapLength(swapTenor));
        }

        //! returns the lower indexes of surrounding volatility matrix corners
        public KeyValuePair<int,int> locate(  double optionTime,
                                              double swapLength)  {
            return new KeyValuePair<int, int>(interpolation_.locateY(optionTime),
                                  interpolation_.locateX(swapLength));
        }
        //@}
        #region protected
        // defining the following method would break CMS test suite
        // to be further investigated
        protected override SmileSection smileSectionImpl(double optionTime, double swapLength) 
        {
            double atmVol = volatilityImpl(optionTime, swapLength, 0.05);
            return (SmileSection) new FlatSmileSection(optionTime, atmVol, dayCounter());

        } 

        protected override double volatilityImpl(double optionTime,double swapLength,
                                                double strike) {
            calculate();
            return interpolation_.value(swapLength, optionTime, true);
        }
        #endregion 

        #region private
        private void checkInputs(int volRows,
                                int volsColumns) 
        {
            if(!(nOptionTenors_ == volRows))
                throw new ArgumentException("mismatch between number of option dates (" +
                   nOptionTenors_ + ") and number of rows (" + volRows +
                   ") in the vol matrix");
            if(!(nSwapTenors_ == volsColumns))
                throw new ArgumentException("mismatch between number of swap tenors (" +
                       nSwapTenors_ + ") and number of rows (" + volsColumns +
                       ") in the vol matrix");


        }
        private void registerWithMarketData()
        {
            for (int i = 0; i < volHandles_.Count; ++i)
                for (int j = 0; j < volHandles_.First().Count; ++j)
                    volHandles_[i][j].registerWith(update);
        }
        private List<List<Handle<Quote>>> volHandles_;
        private Matrix volatilities_;
        private Interpolation2D interpolation_;
        #endregion
    }

}
