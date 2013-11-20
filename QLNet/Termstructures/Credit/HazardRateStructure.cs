/*
 Copyright (C) 2008-2013  Andrea Maggiulli (a.maggiulli@gmail.com)

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
   //! Hazard-rate term structure
   /*! This abstract class acts as an adapter to
      DefaultProbabilityTermStructure allowing the programmer to implement
      only the <tt>hazardRateImpl(Time)</tt> method in derived classes.

      Survival/default probabilities and default densities are calculated
      from hazard rates.

      Hazard rates are defined with annual frequency and continuous
      compounding.

      \ingroup defaultprobabilitytermstructures
   */
   public class HazardRateStructure : DefaultProbabilityTermStructure 
   {
      #region Constructors

      public HazardRateStructure(DayCounter dc = null,List<Handle<Quote> > jumps = null,List<Date> jumpDates = null)
         : base(dc, jumps, jumpDates) {}
         
      public HazardRateStructure(Date referenceDate,Calendar cal = null,DayCounter dc = null,
         List<Handle<Quote> > jumps = null,List<Date> jumpDates = null)
         : base(referenceDate, cal, dc, jumps, jumpDates) { }
      
      public HazardRateStructure(int settlementDays,Calendar cal,DayCounter dc = null,
         List<Handle<Quote> > jumps = null,List<Date> jumpDates = null)
         : base(settlementDays, cal, dc, jumps, jumpDates) { }

      #endregion

      #region Calculations

      // This method must be implemented in derived classes to
      // perform the actual calculations. When it is called,
      // range check has already been performed; therefore, it
      // must assume that extrapolation is required.

      //! hazard rate calculation
      protected virtual double hazardRateImpl(double t) 
          {throw new NotImplementedException("HazardRateStructure.hazardRateImpl");}

      #endregion

      #region DefaultProbabilityTermStructure implementation

      /*! survival probability calculation
         implemented in terms of the hazard rate \f$ h(t) \f$ as
         \f[
         S(t) = \exp\left( - \int_0^t h(\tau) d\tau \right).
         \f]

         \warning This default implementation uses numerical integration,
                  which might be inefficient and inaccurate.
                  Derived classes should override it if a more efficient
                  implementation is available.
      */
      protected override double survivalProbabilityImpl(double t)
      {
         GaussChebyshevIntegration integral = new GaussChebyshevIntegration(48);
        // this stores the address of the method to integrate (so that
        // we don't have to insert its full expression inside the
        // integral below--it's long enough already)

        // the Gauss-Chebyshev quadratures integrate over [-1,1],
        // hence the remapping (and the Jacobian term t/2)
        return Math.Exp(-integral.value(hazardRateImpl) * t/2.0);
        // return 0;
      }
      
      //! default density calculation
      protected override double defaultDensityImpl(double t)
      {
         return hazardRateImpl(t) * survivalProbabilityImpl(t);
      }
      
      #endregion
   }
}
