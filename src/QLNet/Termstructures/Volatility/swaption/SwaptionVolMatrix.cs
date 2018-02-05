/*
 Copyright (C) 2009 Philippe Real (ph_real@hotmail.com)

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
   //! At-the-money swaption-volatility matrix
   /*! This class provides the at-the-money volatility for a given
       swaption by interpolating a volatility matrix whose elements
       are the market volatilities of a set of swaption with given
       option date and swapLength.

       The volatility matrix <tt>M</tt> must be defined so that:
       - the number of rows equals the number of option dates
       - the number of columns equals the number of swap tenors
       - <tt>M[i][j]</tt> contains the volatility corresponding
         to the <tt>i</tt>-th option and <tt>j</tt>-th tenor.
   */
   public class SwaptionVolatilityMatrix : SwaptionVolatilityDiscrete
   {
      //! floating reference date, floating market data
      public SwaptionVolatilityMatrix(
         Calendar calendar,
         BusinessDayConvention bdc,
         List<Period> optionTenors,
         List<Period> swapTenors,
         List<List<Handle<Quote>>> vols,
         DayCounter dayCounter,
         bool flatExtrapolation = false,
         VolatilityType type = VolatilityType.ShiftedLognormal,
         List<List<double>> shifts = null)
         : base(optionTenors, swapTenors, 0, calendar, bdc, dayCounter)
      {
         volHandles_ = vols;
         shiftValues_ = shifts;
         volatilities_ = new Matrix(vols.Count, vols.First().Count);
         shifts_ = new Matrix(vols.Count, vols.First().Count, 0.0);
         volatilityType_ = type;
         checkInputs(volatilities_.rows(), volatilities_.columns(), shifts_.rows(), shifts_.columns());
         registerWithMarketData();

         // fill dummy handles to allow generic handle-based
         if (shiftValues_ == null)
         {
            shiftValues_ = new InitializedList<List<double>>(volatilities_.rows());
            for (int i = 0; i < volatilities_.rows(); ++i)
            {
               shiftValues_[i] = new InitializedList<double>(volatilities_.columns());
               for (int j = 0; j < volatilities_.columns(); ++j)
               {
                  shiftValues_[i][j] = shifts_.rows() > 0 ? shifts_[i, j] : 0.0;
               }
            }
         }

         if (flatExtrapolation)
         {
            interpolation_ = new FlatExtrapolator2D(new BilinearInterpolation(
                                                       swapLengths_, swapLengths_.Count,
                                                       optionTimes_, optionTimes_.Count, volatilities_));

            interpolationShifts_ = new FlatExtrapolator2D(new BilinearInterpolation(
                                                             swapLengths_, swapLengths_.Count,
                                                             optionTimes_, optionTimes_.Count, shifts_));
         }
         else
         {
            interpolation_ = new BilinearInterpolation(
               swapLengths_, swapLengths_.Count,
               optionTimes_, optionTimes_.Count,
               volatilities_);

            interpolationShifts_ = new BilinearInterpolation(
               swapLengths_, swapLengths_.Count,
               optionTimes_, optionTimes_.Count,
               shifts_);
         }
      }

      //! fixed reference date, floating market data
      public SwaptionVolatilityMatrix(
         Date referenceDate,
         Calendar calendar,
         BusinessDayConvention bdc,
         List<Period> optionTenors,
         List<Period> swapTenors,
         List<List<Handle<Quote>>> vols,
         DayCounter dayCounter,
         bool flatExtrapolation = false,
         VolatilityType type = VolatilityType.ShiftedLognormal,
         List<List<double>> shifts = null)
         : base(optionTenors, swapTenors, referenceDate, calendar, bdc, dayCounter)
      {
         volHandles_ = vols;
         shiftValues_ = shifts;
         volatilities_ = new Matrix(vols.Count, vols.First().Count);
         shifts_ = new Matrix(vols.Count, vols.First().Count, 0.0);
         volatilityType_ = type;
         checkInputs(volatilities_.rows(), volatilities_.columns(), shifts_.rows(), shifts_.columns());
         registerWithMarketData();

         // fill dummy handles to allow generic handle-based
         if (shiftValues_ == null)
         {
            shiftValues_ = new InitializedList<List<double>>(volatilities_.rows());
            for (int i = 0; i < volatilities_.rows(); ++i)
            {
               shiftValues_[i] = new InitializedList<double>(volatilities_.columns());
               for (int j = 0; j < volatilities_.columns(); ++j)
               {
                  shiftValues_[i][j] = shifts_.rows() > 0 ? shifts_[i, j] : 0.0;
               }
            }
         }

         if (flatExtrapolation)
         {
            interpolation_ = new FlatExtrapolator2D(new BilinearInterpolation(
                                                       swapLengths_, swapLengths_.Count,
                                                       optionTimes_, optionTimes_.Count, volatilities_));

            interpolationShifts_ = new FlatExtrapolator2D(new BilinearInterpolation(
                                                             swapLengths_, swapLengths_.Count,
                                                             optionTimes_, optionTimes_.Count, shifts_));
         }
         else
         {
            interpolation_ = new BilinearInterpolation(
               swapLengths_, swapLengths_.Count,
               optionTimes_, optionTimes_.Count,
               volatilities_);

            interpolationShifts_ = new BilinearInterpolation(
               swapLengths_, swapLengths_.Count,
               optionTimes_, optionTimes_.Count,
               shifts_);
         }
      }

      //! floating reference date, fixed market data
      public SwaptionVolatilityMatrix(
         Calendar calendar,
         BusinessDayConvention bdc,
         List<Period> optionTenors,
         List<Period> swapTenors,
         Matrix vols,
         DayCounter dayCounter,
         bool flatExtrapolation = false,
         VolatilityType type = VolatilityType.ShiftedLognormal,
         Matrix shifts = null)

         : base(optionTenors, swapTenors, 0, calendar, bdc, dayCounter)
      {
         volHandles_ = new InitializedList<List<Handle<Quote>>>(vols.rows());
         shiftValues_ = new InitializedList<List<double>>(vols.rows());
         volatilities_ = new Matrix(vols.rows(), vols.columns());
         shifts_ = shifts ?? new Matrix(vols.rows(), vols.columns(), 0.0);
         volatilityType_ = type;
         checkInputs(volatilities_.rows(), volatilities_.columns(), shifts_.rows(), shifts_.columns());

         // fill dummy handles to allow generic handle-based
         // computations later on
         for (int i = 0; i < vols.rows(); ++i)
         {
            volHandles_[i] = new InitializedList<Handle<Quote>>(vols.columns());
            shiftValues_[i] = new InitializedList<double>(vols.columns());
            for (int j = 0; j < vols.columns(); ++j)
               volHandles_[i][j] = new Handle<Quote>((new
                                                      SimpleQuote(vols[i, j])));
         }

         if (flatExtrapolation)
         {
            interpolation_ = new FlatExtrapolator2D(new BilinearInterpolation(
                                                       swapLengths_, swapLengths_.Count,
                                                       optionTimes_, optionTimes_.Count, volatilities_));

            interpolationShifts_ = new FlatExtrapolator2D(new BilinearInterpolation(
                                                             swapLengths_, swapLengths_.Count,
                                                             optionTimes_, optionTimes_.Count, shifts_));
         }
         else
         {
            interpolation_ = new BilinearInterpolation(
               swapLengths_, swapLengths_.Count,
               optionTimes_, optionTimes_.Count,
               volatilities_);

            interpolationShifts_ = new BilinearInterpolation(
               swapLengths_, swapLengths_.Count,
               optionTimes_, optionTimes_.Count,
               shifts_);
         }
      }

      //! fixed reference date, fixed market data
      public SwaptionVolatilityMatrix(
         Date referenceDate,
         Calendar calendar,
         BusinessDayConvention bdc,
         List<Period> optionTenors,
         List<Period> swapTenors,
         Matrix vols,
         DayCounter dayCounter,
         bool flatExtrapolation = false,
         VolatilityType type = VolatilityType.ShiftedLognormal,
         Matrix shifts = null)
         : base(optionTenors, swapTenors, referenceDate, calendar, bdc, dayCounter)
      {
         volHandles_ = new InitializedList<List<Handle<Quote>>>(vols.rows());
         shiftValues_ = new InitializedList<List<double>>(vols.rows());
         volatilities_ = new Matrix(vols.rows(), vols.columns());
         shifts_ = shifts ?? new Matrix(vols.rows(), vols.columns(), 0.0);
         checkInputs(vols.rows(), vols.columns(), shifts_.rows(), shifts_.columns());
         volatilityType_ = type;

         // fill dummy handles to allow generic handle-based
         // computations later on
         for (int i = 0; i < vols.rows(); ++i)
         {
            volHandles_[i] = new InitializedList<Handle<Quote>>(vols.columns());
            shiftValues_[i] = new InitializedList<double>(vols.columns());
            for (int j = 0; j < vols.columns(); ++j)
            {
               volHandles_[i][j] = new Handle<Quote>((new
                                                      SimpleQuote(vols[i, j])));
               shiftValues_[i][j] = shifts_.rows() > 0 ? shifts_[i, j] : 0.0;
            }
         }

         if (flatExtrapolation)
         {
            interpolation_ = new FlatExtrapolator2D(new BilinearInterpolation(
                                                       swapLengths_, swapLengths_.Count,
                                                       optionTimes_, optionTimes_.Count, volatilities_));

            interpolationShifts_ = new FlatExtrapolator2D(new BilinearInterpolation(
                                                             swapLengths_, swapLengths_.Count,
                                                             optionTimes_, optionTimes_.Count, shifts_));
         }
         else
         {
            interpolation_ = new BilinearInterpolation(
               swapLengths_, swapLengths_.Count,
               optionTimes_, optionTimes_.Count,
               volatilities_);

            interpolationShifts_ = new BilinearInterpolation(
               swapLengths_, swapLengths_.Count,
               optionTimes_, optionTimes_.Count,
               shifts_);
         }
      }

      // fixed reference date and fixed market data, option dates
      public SwaptionVolatilityMatrix(Date today,
                                      List<Date> optionDates,
                                      List<Period> swapTenors,
                                      Matrix vols,
                                      DayCounter dayCounter,
                                      bool flatExtrapolation = false,
                                      VolatilityType type = VolatilityType.ShiftedLognormal,
                                      Matrix shifts = null)
         : base(optionDates, swapTenors, today, new Calendar(), BusinessDayConvention.Following, dayCounter)
      {
         volHandles_ = new InitializedList<List<Handle<Quote>>>(vols.rows());
         shiftValues_ = new InitializedList<List<double>>(vols.rows());
         volatilities_ = new Matrix(vols.rows(), vols.columns());
         shifts_ = shifts ?? new Matrix(vols.rows(), vols.columns(), 0.0);
         checkInputs(vols.rows(), vols.columns(), shifts_.rows(), shifts_.columns());
         volatilityType_ = type;

         // fill dummy handles to allow generic handle-based
         // computations later on
         for (int i = 0; i < vols.rows(); ++i)
         {
            volHandles_[i] = new InitializedList<Handle<Quote>>(vols.columns());
            shiftValues_[i] = new InitializedList<double>(vols.columns());
            for (int j = 0; j < vols.columns(); ++j)
            {
               volHandles_[i][j] = new Handle<Quote>((new
                                                      SimpleQuote(vols[i, j])));
               shiftValues_[i][j] = shifts_.rows() > 0 ? shifts_[i, j] : 0.0;
            }
         }

         if (flatExtrapolation)
         {
            interpolation_ = new FlatExtrapolator2D(new BilinearInterpolation(
                                                       swapLengths_, swapLengths_.Count,
                                                       optionTimes_, optionTimes_.Count, volatilities_));

            interpolationShifts_ = new FlatExtrapolator2D(new BilinearInterpolation(
                                                             swapLengths_, swapLengths_.Count,
                                                             optionTimes_, optionTimes_.Count, shifts_));
         }
         else
         {
            interpolation_ = new BilinearInterpolation(
               swapLengths_, swapLengths_.Count,
               optionTimes_, optionTimes_.Count,
               volatilities_);

            interpolationShifts_ = new BilinearInterpolation(
               swapLengths_, swapLengths_.Count,
               optionTimes_, optionTimes_.Count,
               shifts_);
         }
      }

      // LazyObject interface
      //verifier protected QL public!!
      protected override void performCalculations()
      {
         base.performCalculations();

         // we might use iterators here...
         for (int i = 0; i < volatilities_.rows(); ++i)
         {
            for (int j = 0; j < volatilities_.columns(); ++j)
            {
               volatilities_[i, j] = volHandles_[i][j].link.value();
               if (shiftValues_.Count > 0)
                  shifts_[i, j] = shiftValues_[i][j];
            }
         }
      }

      // TermStructure interface
      public override Date maxDate()
      {
         return optionDates_.Last();
      }

      // VolatilityTermStructure interface
      public override double minStrike()
      {
         return double.MinValue;
      }

      public override double maxStrike()
      {
         return double.MaxValue;
      }

      // SwaptionVolatilityStructure interface
      public override Period maxSwapTenor()
      {
         return swapTenors_.Last();
      }

      // Other inspectors
      //! returns the lower indexes of surrounding volatility matrix corners
      public KeyValuePair<int, int> locate(Date optionDate,
                                           Period swapTenor)
      {
         return locate(timeFromReference(optionDate),
                       swapLength(swapTenor));
      }

      //! returns the lower indexes of surrounding volatility matrix corners
      public KeyValuePair<int, int> locate(double optionTime,
                                           double swapLength)
      {
         return new KeyValuePair<int, int>(interpolation_.locateY(optionTime),
                                           interpolation_.locateX(swapLength));
      }

      //Volatility type
      public override VolatilityType volatilityType()
      {
         return volatilityType_;
      }

      #region protected
      // defining the following method would break CMS test suite
      // to be further investigated
      protected override SmileSection smileSectionImpl(double optionTime, double swapLength)
      {
         double atmVol = volatilityImpl(optionTime, swapLength, 0.05);
         double shift = interpolationShifts_.value(optionTime, swapLength, true);
         return (SmileSection)new FlatSmileSection(optionTime, atmVol, dayCounter(), null, volatilityType(), shift);

      }

      protected override double volatilityImpl(double optionTime, double swapLength,
                                               double strike)
      {
         calculate();
         return interpolation_.value(swapLength, optionTime, true);
      }

      protected override double shiftImpl(double optionTime, double swapLength)
      {
         calculate();
         double tmp = interpolationShifts_.value(swapLength, optionTime, true);
         return tmp;
      }
      #endregion

      #region private
      private void checkInputs(int volRows,
                               int volsColumns,
                               int shiftRows,
                               int shiftsColumns)
      {
         Utils.QL_REQUIRE(nOptionTenors_ == volRows, () =>
                          "mismatch between number of option dates (" + nOptionTenors_ + ") and number of rows (" +
                          volRows + ") in the vol matrix");
         Utils.QL_REQUIRE(nSwapTenors_ == volsColumns, () =>
                          "mismatch between number of swap tenors (" + nSwapTenors_ + ") and number of rows (" +
                          volsColumns + ") in the vol matrix");

         if (shiftRows == 0 && shiftsColumns == 0)
         {
            shifts_ = new Matrix(volRows, volsColumns, 0.0);
            shiftRows = volRows;
            shiftsColumns = volsColumns;
         }

         Utils.QL_REQUIRE(nOptionTenors_ == shiftRows, () =>
                          "mismatch between number of option dates (" + nOptionTenors_ + ") and number of rows (" +
                          shiftRows + ") in the shift matrix");
         Utils.QL_REQUIRE(nSwapTenors_ == shiftsColumns, () =>
                          "mismatch between number of swap tenors (" + nSwapTenors_ + ") and number of rows (" +
                          shiftsColumns + ") in the shift matrix");
      }
      private void registerWithMarketData()
      {
         for (int i = 0; i < volHandles_.Count; ++i)
            for (int j = 0; j < volHandles_.First().Count; ++j)
               volHandles_[i][j].registerWith(update);
      }
      private List<List<Handle<Quote>>> volHandles_;
      private List<List<double>> shiftValues_;
      private Matrix volatilities_, shifts_;
      private Interpolation2D interpolation_, interpolationShifts_;
      VolatilityType volatilityType_;
      #endregion
   }

}
