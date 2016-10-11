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

namespace QLNet
{
   //! Implied vol term structure at a given date in the future
   /*! The given date will be the implied reference date.
       \note This term structure will remain linked to the original
             structure, i.e., any changes in the latter will be reflected
             in this structure as well.

       \warning It doesn't make financial sense to have an
                asset-dependant implied Vol Term Structure.  This
                class should be used with term structures that are
                time dependant only.
   */
   public class ImpliedVolTermStructure : BlackVarianceTermStructure
   {
      public ImpliedVolTermStructure( Handle<BlackVolTermStructure> originalTS, Date referenceDate )
         :base(referenceDate)
      {
         originalTS_ = originalTS;
         originalTS_.registerWith(update);
      }
      //! \name TermStructure interface
      //@{
      public override DayCounter dayCounter() { return originalTS_.link.dayCounter(); }
      public override Date maxDate() { return originalTS_.link.maxDate(); }
      //@}
      //! \name VolatilityTermStructure interface
      //@{
      public override double minStrike() { return originalTS_.link.minStrike(); }
      public override double maxStrike() { return originalTS_.link.maxStrike(); }
      //@}
      //! \name Visitability
      //@{
      public virtual void accept( IAcyclicVisitor v )
      {
         if ( v != null )
            v.visit( this );
         else
            throw new Exception( "not an event visitor" );
      }
      //@}
      protected override double blackVarianceImpl(double t, double strike)
      {
         /* timeShift (and/or variance) variance at evaluation date
           cannot be cached since the original curve could change
           between invocations of this method */
         double timeShift = dayCounter().yearFraction( originalTS_.link.referenceDate(),referenceDate() );
         /* t is relative to the current reference date
            and needs to be converted to the time relative
            to the reference date of the original curve */
         return originalTS_.link.blackForwardVariance( timeShift,timeShift + t,strike,true );
      }
      
      private Handle<BlackVolTermStructure> originalTS_;

   }
}
