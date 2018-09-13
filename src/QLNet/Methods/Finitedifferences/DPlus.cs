/*
 Copyright (C) 2000, 2001, 2002, 2003 RiskMap srl
 Copyright (C) 2017 Jean-Camille Tournier (jean-camille.tournier@avivainvestors.com)

 This file is part of QLNet Project https://github.com/amaggiulli/qlnet

 QLNet is free software: you can redistribute it and/or modify it
 under the terms of the QLNet license.  You should have received a
 copy of the license along with this program; if not, license is
 available at <https://github.com/amaggiulli/QLNet/blob/develop/LICENSE>.

 QLNet is a based on QuantLib, a free-software/open-source library
 for financial quantitative analysts and developers - http://quantlib.org/
 The QuantLib license is available online at http://quantlib.org/license.shtml.

 This program is distributed in the hope that it will be useful, but WITHOUT
 ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
 FOR A PARTICULAR PURPOSE.  See the license for more details.
*/

namespace QLNet
{
   //! \f$ D_{+} \f$ matricial representation
   /*! The differential operator \f$ D_{+} \f$ discretizes the
       first derivative with the first-order formula
       \f[ \frac{\partial u_{i}}{\partial x} \approx
           \frac{u_{i+1}-u_{i}}{h} = D_{+} u_{i}
       \f]

       \ingroup findiff
   */
   public class DPlus : TridiagonalOperator
   {
      public DPlus(int gridPoints, double h)
         : base(gridPoints)
      {
         setFirstRow(-1.0 / h, 1.0 / h);
         setMidRows(0.0, -1.0 / h, 1.0 / h);
         setLastRow(-1.0 / h, 1.0 / h);                    // linear extrapolation
      }
   }
}
