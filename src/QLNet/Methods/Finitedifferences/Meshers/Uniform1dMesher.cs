/*
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
   /// <summary>
   ///  One-dimensional simple uniform grid mesher
   /// </summary>
   public class Uniform1dMesher : Fdm1dMesher
   {
      public Uniform1dMesher(double start, double end, int size)
         : base(size)
      {
         Utils.QL_REQUIRE(end > start, () => "end must be large than start");

         double dx = (end - start) / (size - 1);

         for (int i = 0; i < size - 1; ++i)
         {
            locations_[i] = start + i * dx;
            dplus_[i] = dminus_[i + 1] = dx;
         }

         locations_[locations_.Count - 1] = end;
         dplus_[dplus_.Count - 1] = null;
         dminus_[0] = null;
      }
   }
}
