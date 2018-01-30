﻿/*
 Copyright (C) 2000, 2001, 2002, 2003 RiskMap srl
 Copyright (C) 2017 Jean-Camille Tournier (jean-camille.tournier@avivainvestors.com)

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

using System.Collections.Generic;

namespace QLNet
{
   //! %Forward Euler scheme for finite difference methods
   /*! See sect. \ref findiff for details on the method.

       In this implementation, the passed operator must be derived
       from either TimeConstantOperator or TimeDependentOperator.
       Also, it must implement at least the following interface:

       // copy constructor/assignment
       // (these will be provided by the compiler if none is defined)
       Operator(const Operator&);
       Operator& operator=(const Operator&);

       // inspectors
       Size size();

       // modifiers
       void setTime(Time t);

       // operator interface
       array_type applyTo(const array_type&);
       static Operator identity(Size size);

       // operator algebra
       Operator operator*(Real, const Operator&);
       Operator operator-(const Operator&, const Operator&);
       \endcode

       \todo add Richardson extrapolation

       \ingroup findiff
   */

   public class ExplicitEuler<Operator> : MixedScheme<Operator> where Operator : IOperator
   {
      // constructors
      public ExplicitEuler() { }  // required for generics

      public ExplicitEuler(Operator L, List<BoundaryCondition<IOperator>> bcs)
          : base(L, 0.0, bcs)
      { }
   }
}
