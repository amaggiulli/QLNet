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
using System;

namespace QLNet
{
   public class SecondOrderMixedDerivativeOp : NinePointLinearOp
   {
      public SecondOrderMixedDerivativeOp(int d0, int d1, FdmMesher mesher)
         : base(d0, d1, mesher)
      {
         FdmLinearOpLayout layout = mesher.layout();
         FdmLinearOpIterator endIter = layout.end();

         for (FdmLinearOpIterator iter = layout.begin(); iter != endIter; ++iter)
         {
            int i = iter.index();
            double? hm_d0 = mesher.dminus(iter, d0_);
            double? hp_d0 = mesher.dplus(iter, d0_);
            double? hm_d1 = mesher.dminus(iter, d1_);
            double? hp_d1 = mesher.dplus(iter, d1_);

            double? zetam1 = hm_d0 * (hm_d0 + hp_d0);
            double? zeta0 = hm_d0 * hp_d0;
            double? zetap1 = hp_d0 * (hm_d0 + hp_d0);
            double? phim1 = hm_d1 * (hm_d1 + hp_d1);
            double? phi0 = hm_d1 * hp_d1;
            double? phip1 = hp_d1 * (hm_d1 + hp_d1);

            int c0 = iter.coordinates()[d0_];
            int c1 = iter.coordinates()[d1_];
            if (c0 == 0 && c1 == 0)
            {
               // lower left corner
               a00_[i] = a01_[i] = a02_[i] = a10_[i] = a20_[i] = 0.0;
               a11_[i] = a22_[i] = 1.0 / (hp_d0.Value * hp_d1.Value);
               a21_[i] = a12_[i] = -a11_[i];
            }
            else if (c0 == layout.dim()[d0_] - 1 && c1 == 0)
            {
               // upper left corner
               a22_[i] = a21_[i] = a20_[i] = a10_[i] = a00_[i] = 0.0;
               a01_[i] = a12_[i] = 1.0 / (hm_d0.Value * hp_d1.Value);
               a11_[i] = a02_[i] = -a01_[i];
            }
            else if (c0 == 0 && c1 == layout.dim()[d1_] - 1)
            {
               // lower right corner
               a00_[i] = a01_[i] = a02_[i] = a12_[i] = a22_[i] = 0.0;
               a10_[i] = a21_[i] = 1.0 / (hp_d0.Value * hm_d1.Value);
               a20_[i] = a11_[i] = -a10_[i];
            }
            else if (c0 == layout.dim()[d0_] - 1 && c1 == layout.dim()[d1_] - 1)
            {
               // upper right corner
               a20_[i] = a21_[i] = a22_[i] = a12_[i] = a02_[i] = 0.0;
               a00_[i] = a11_[i] = 1.0 / (hm_d0.Value * hm_d1.Value);
               a10_[i] = a01_[i] = -a00_[i];
            }
            else if (c0 == 0)
            {
               // lower side
               a00_[i] = a01_[i] = a02_[i] = 0.0;
               a10_[i] = hp_d1.Value / (hp_d0.Value * phim1.Value);
               a20_[i] = -a10_[i];
               a21_[i] = (hp_d1.Value - hm_d1.Value) / (hp_d0.Value * phi0.Value);
               a11_[i] = -a21_[i];
               a22_[i] = hm_d1.Value / (hp_d0.Value * phip1.Value);
               a12_[i] = -a22_[i];
            }
            else if (c0 == layout.dim()[d0_] - 1)
            {
               // upper side
               a20_[i] = a21_[i] = a22_[i] = 0.0;
               a00_[i] = hp_d1.Value / (hm_d0.Value * phim1.Value);
               a10_[i] = -a00_[i];
               a11_[i] = (hp_d1.Value - hm_d1.Value) / (hm_d0.Value * phi0.Value);
               a01_[i] = -a11_[i];
               a12_[i] = hm_d1.Value / (hm_d0.Value * phip1.Value);
               a02_[i] = -a12_[i];
            }
            else if (c1 == 0)
            {
               // left side
               a00_[i] = a10_[i] = a20_[i] = 0.0;
               a01_[i] = hp_d0.Value / (zetam1.Value * hp_d1.Value);
               a02_[i] = -a01_[i];
               a12_[i] = (hp_d0.Value - hm_d0.Value) / (zeta0.Value * hp_d1.Value);
               a11_[i] = -a12_[i];
               a22_[i] = hm_d0.Value / (zetap1.Value * hp_d1.Value);
               a21_[i] = -a22_[i];
            }
            else if (c1 == layout.dim()[d1_] - 1)
            {
               // right side
               a22_[i] = a12_[i] = a02_[i] = 0.0;
               a00_[i] = hp_d0.Value / (zetam1.Value * hm_d1.Value);
               a01_[i] = -a00_[i];
               a11_[i] = (hp_d0.Value - hm_d0.Value) / (zeta0.Value * hm_d1.Value);
               a10_[i] = -a11_[i];
               a21_[i] = hm_d0.Value / (zetap1.Value * hm_d1.Value);
               a20_[i] = -a21_[i];
            }
            else
            {
               a00_[i] = hp_d0.Value * hp_d1.Value / (zetam1.Value * phim1.Value);
               a10_[i] = -(hp_d0.Value - hm_d0.Value) * hp_d1.Value / (zeta0.Value * phim1.Value);
               a20_[i] = -hm_d0.Value * hp_d1.Value / (zetap1.Value * phim1.Value);
               a01_[i] = -hp_d0.Value * (hp_d1.Value - hm_d1.Value) / (zetam1.Value * phi0.Value);
               a11_[i] = (hp_d0.Value - hm_d0.Value) * (hp_d1.Value - hm_d1.Value) / (zeta0.Value * phi0.Value);
               a21_[i] = hm_d0.Value * (hp_d1.Value - hm_d1.Value) / (zetap1.Value * phi0.Value);
               a02_[i] = -hp_d0.Value * hm_d1.Value / (zetam1.Value * phip1.Value);
               a12_[i] = hm_d1.Value * (hp_d0.Value - hm_d0.Value) / (zeta0.Value * phip1.Value);
               a22_[i] = hm_d0.Value * hm_d1.Value / (zetap1.Value * phip1.Value);
            }
         }
      }
   }
}
