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

    //namespace detail {

        public class BilinearInterpolationImpl  : Interpolation2D.templateImpl 
        {
          
            public BilinearInterpolationImpl(List<double> xBegin, int xSize,
                                      List<double>  yBegin,int ySize,
                                      Matrix zData)
            :base(xBegin,xSize,yBegin,ySize,zData){
                calculate();
            }

            public override void calculate() { }

            public override double value(double x, double y)
            {
                int i = this.locateX(x), j = this.locateY(y);

                double z1 = this.zData_[j,i];
                double z2 = this.zData_[j, i + 1];
                double z3 = this.zData_[j + 1, i];
                double z4 = this.zData_[j + 1, i + 1];

                double t = (x - this.xBegin_[i]) /
                    (this.xBegin_[i+1]-this.xBegin_[i]);
                double u = (y - this.yBegin_[j]) /
                    (this.yBegin_[j+1]-this.yBegin_[j]);

                return (1.0-t)*(1.0-u)*z1 + t*(1.0-u)*z2
                     + (1.0-t)*u*z3 + t*u*z4;
            }
        }
  

        //! %bilinear interpolation between discrete points
        public class BilinearInterpolation : Interpolation2D
        {

            /*! \pre the \f$ x \f$ and \f$ y \f$ values must be sorted. */
            public BilinearInterpolation(List<double> xBegin, int xSize,
                                          List<double> yBegin, int ySize,
                                          Matrix zData){
                impl_ = (Interpolation2D.Impl)(
                      new BilinearInterpolationImpl(xBegin, xSize,
                                                    yBegin, ySize,zData));
            }
        }

        //! bilinear-interpolation factory
        public class Bilinear
        {
            Interpolation2D interpolate(List<double> xBegin, int xSize,
                                          List<double> yBegin, int ySize,
                                          Matrix zData)
            {
                return new BilinearInterpolation(xBegin, xSize, yBegin, ySize, zData);
            }
        }
    }
