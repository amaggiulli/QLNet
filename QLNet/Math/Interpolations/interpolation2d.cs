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
    //! base class for 2-D interpolations.
    /*! Classes derived from this class will provide interpolated
        values from two sequences of length \f$ N \f$ and \f$ M \f$,
        representing the discretized values of the \f$ x \f$ and \f$ y
        \f$ variables, and a \f$ N \times M \f$ matrix representing
        the tabulated function values.
    */

    // Interpolation factory
    /*public interface IInterpolationFactory2D
    {
        Interpolation2D interpolate( List<double> xBegin, int xSize,
                                     List<double> yBegin, int ySize,
                                     Matrix zData){
            bool global { get; }
            int requiredPoints { get; }
        }
    }*/

    public abstract class Interpolation2D : Extrapolator/*, IValue */
    {
        protected Impl impl_;

        public Interpolation2D() {}

        public double xMin(){return impl_.xMin();}

        public double xMax(){return impl_.xMax();}

        public List<double> xValues() {return impl_.xValues();}

        public int locateX(double x) {return impl_.locateX(x);}

        public double yMin() {return impl_.yMin();}

        public double yMax() {return impl_.yMax();}

        public List<double> yValues() {return impl_.yValues();}
        
        public int locateY(double y) {return impl_.locateY(y);}
        
        public Matrix zData() {return impl_.zData();}

        public bool isInRange(double x, double y){return impl_.isInRange(x,y);}
        
        public override void update(){impl_.calculate();}

        // main method to derive an interpolated point
        public double value(double x, double y) { return value(x, y, false); }
        
        public double value(double x, double y, bool allowExtrapolation){
            checkRange(x, y, allowExtrapolation);
            return impl_.value(x, y);
        }
  
        protected  void checkRange(double x, double y, bool extrapolate) {
            if (!(extrapolate || allowsExtrapolation() || impl_.isInRange(x,y)))
                throw new ArgumentException("interpolation range is [" + impl_.xMin() + ", " + impl_.xMax()
                                               +  "] X [" + x +  impl_.yMin() + ", " + impl_.yMax()
                                               + "]: extrapolation at (" +x+", "+y+ " not allowed");
        }
       
        //! abstract base class for 2-D interpolation implementations
        protected interface Impl //: IValue
        {
           void calculate();
           double xMin();
           double xMax();
           List<double> xValues();
           int locateX(double x);
           double yMin();
           double yMax();
           List<double> yValues();
           int locateY(double y);
           Matrix zData();
           bool isInRange(double x,double y);
           double value(double x, double y);
        }

        public abstract class templateImpl : Impl
        {
           protected List<double> xBegin_;
           protected List<double> yBegin_;
           protected int xSize_;
           protected int ySize_;
           protected Matrix zData_;

           // this method should be used for initialisation
           public templateImpl( List<double> xBegin, int xSize,
                                List<double> yBegin, int ySize,
                                Matrix zData) {
               xBegin_ = xBegin;
               xSize_ = xSize;
               yBegin_ = yBegin;
               ySize_ = ySize;
               zData_ = zData;

               if (xSize < 2)
                   throw new ArgumentException("not enough points to interpolate: at least 2 required, "
                                               + xSize + " provided");
               if (ySize < 2)
                   throw new ArgumentException("not enough points to interpolate: at least 2 required, "
                                               + ySize + " provided");
           }

           public double xMin() { return xBegin_.First(); }

           public double xMax() { return xBegin_[xSize_ - 1]; }

           public List<double> xValues() { return xBegin_.GetRange(0, xSize_); }

           public double yMin() { return yBegin_.First(); }

           public double yMax() { return yBegin_[ySize_ - 1]; }

           public List<double> yValues() { return yBegin_.GetRange(0, ySize_); }

           public Matrix zData() {return zData_;}

           public bool isInRange(double x, double y) {
               double x1 = xMin(), x2 = xMax();
               bool xIsInrange = (x >= x1 && x <= x2) || Utils.close(x, x1) || Utils.close(x, x2);
               if (!xIsInrange) return false;

               double y1 = yMin(), y2 = yMax();
               return (y >= y1 && y <= y2) || Utils.close(y, y1) || Utils.close(y, y2);
           }
        
           public int locateX(double x) {
               int result = xBegin_.BinarySearch(x);
               if (result < 0)
                   // The upper_bound() algorithm finds the last position in a sequence that value can occupy 
                   // without violating the sequence's ordering
                   // if BinarySearch does not find value the value, the index of the next larger item is returned
                   result = ~result - 1;

               // impose limits. we need the one before last at max or the first at min
               result = Math.Max(Math.Min(result, xSize_ - 2), 0);
               return result;
           }

           public int locateY(double y) {
               /*#if QL_EXTRA_SAFETY_CHECKS
               for (I2 k=yBegin_, l=yBegin_+1; l!=yEnd_; ++k, ++l)
                   QL_REQUIRE(*l > *k, "unsorted y values");
               #endif
               if (y < *yBegin_)
                   return 0;
               else if (y > *(yEnd_-1))
                   return yEnd_-yBegin_-2;
               else
                   return std::upper_bound(yBegin_,yEnd_-1,y)-yBegin_-1;*/
               int result = yBegin_.BinarySearch(y);
               if (result < 0)
                   // The upper_bound() algorithm finds the last position in a sequence that value can occupy 
                   // without violating the sequence's ordering
                   // if BinarySearch does not find value the value, the index of the next larger item is returned
                   result = ~result - 1;

               // impose limits. we need the one before last at max or the first at min
               result = Math.Max(Math.Min(result, ySize_ - 2), 0);
               return result;
           }
           
           public abstract double value(double x, double y);
           public abstract void calculate();

       }   
    }
}
