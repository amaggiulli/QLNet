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

namespace QLNet
{
   /*! The swaption vol cube is made up of ordered swaption vol surface
       layers, each layer referring to a swap index of a given length
       (in years), all indexes belonging to the same family. In order
       to identify the family (and its market conventions) an index of
       whatever length from that family must be passed in as
       swapIndexBase.
 
       Often for short swap length the swap index family is different,
       e.g. the EUR case: swap vs 6M Euribor is used for length>1Y,
       while swap vs 3M Euribor is used for the 1Y length. The
       shortSwapIndexBase is used to identify this second family.
   */
   public class SwaptionVolCube2 : SwaptionVolatilityCube
   {
      public SwaptionVolCube2( Handle<SwaptionVolatilityStructure> atmVolStructure,
                               List<Period> optionTenors,
                               List<Period> swapTenors,
                               List<double> strikeSpreads,
                               List<List<Handle<Quote> > > volSpreads,
                               SwapIndex swapIndexBase,
                               SwapIndex shortSwapIndexBase,
                               bool vegaWeightedSmileFit)
         :base(atmVolStructure, optionTenors, swapTenors,strikeSpreads, volSpreads, swapIndexBase,
               shortSwapIndexBase,vegaWeightedSmileFit)
      {
         volSpreadsInterpolator_= new List<Interpolation2D>();
         volSpreadsMatrix_ = new List<Matrix>(nStrikes_);
         for ( int i = 0 ; i < nStrikes_; i++)
            volSpreadsMatrix_.Add (new Matrix(optionTenors.Count, swapTenors.Count, 0.0));
      }
      //! \name LazyObject interface
      //@{
      protected override void performCalculations()
      {
         base.performCalculations();
         //! set volSpreadsMatrix_ by volSpreads_ quotes
         for ( int i = 0; i < nStrikes_; i++ )
            for ( int j = 0; j < nOptionTenors_; j++ )
               for ( int k = 0; k < nSwapTenors_; k++ )
               {
                  Matrix p = volSpreadsMatrix_[i];
                  p[j,k] = volSpreads_[j * nSwapTenors_ + k][i].link.value();
               }
         //! create volSpreadsInterpolator_ 
         for (int i=0; i<nStrikes_; i++) 
         {
            volSpreadsInterpolator_.Add(new BilinearInterpolation( swapLengths_, swapLengths_.Count,
                optionTimes_, optionTimes_.Count,volSpreadsMatrix_[i]));
            volSpreadsInterpolator_[i].enableExtrapolation();
         }
      }
      //@}
      //! \name SwaptionVolatilityCube inspectors
      //@{
      public  Matrix volSpreads(int i) { return volSpreadsMatrix_[i]; }

      protected override SmileSection smileSectionImpl( Date optionDate, Period swapTenor)
      {
         calculate();
         double atmForward = atmStrike(optionDate, swapTenor);
         double atmVol = atmVol_.link.volatility(optionDate,swapTenor,atmForward);
         double optionTime = timeFromReference(optionDate);
         double exerciseTimeSqrt = Math.Sqrt(optionTime);
         List<double> strikes, stdDevs;
         strikes=new List<double>(nStrikes_);
         stdDevs=new List<double>(nStrikes_);
         double length = swapLength(swapTenor);
         for (int i=0; i<nStrikes_; ++i) 
         {
            strikes.Add(atmForward + strikeSpreads_[i]);
            stdDevs.Add(exerciseTimeSqrt*(atmVol + volSpreadsInterpolator_[i].value(length, optionTime)));
         }
         double shift = atmVol_.link.shift(optionTime,length);
         return new InterpolatedSmileSection<Linear>(optionTime,strikes,stdDevs,atmForward,new Linear(),
            new Actual365Fixed(), volatilityType(),shift);
      }

      protected override SmileSection smileSectionImpl( double optionTime, double swapLength)
      {
         calculate();
         Date optionDate = optionDateFromTime(optionTime);
         Rounding rounder = new Rounding(0);
         Period swapTenor = new Period((int)(rounder.Round(swapLength*12.0)), TimeUnit.Months);
         // ensure that option date is valid fixing date
         optionDate =
            swapTenor > shortSwapIndexBase_.tenor()
                ? swapIndexBase_.fixingCalendar().adjust(optionDate, BusinessDayConvention.Following)
                : shortSwapIndexBase_.fixingCalendar().adjust(optionDate,BusinessDayConvention.Following);
        return smileSectionImpl(optionDate, swapTenor);
      }
      //@}
      private List<Interpolation2D> volSpreadsInterpolator_;
      private List<Matrix> volSpreadsMatrix_;
   }
}
