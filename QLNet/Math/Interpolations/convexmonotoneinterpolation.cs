/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
  
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

namespace QLNet {
    //the first value in the y-vector is ignored.

    #region Helpers
    public interface ISectionHelper {
        double value(double x);
        double primitive(double x);
        double fNext();
    }

    public class ComboHelper : ISectionHelper {
        private double quadraticity_;
        ISectionHelper quadraticHelper_;
        ISectionHelper convMonoHelper_;

        public ComboHelper(ISectionHelper quadraticHelper, ISectionHelper convMonoHelper, double quadraticity) {
            quadraticity_ = quadraticity;
            quadraticHelper_ = quadraticHelper;
            convMonoHelper_ = convMonoHelper;
            if (!(quadraticity < 1.0 && quadraticity > 0.0))
                throw new ApplicationException("Quadratic value must lie between 0 and 1");
        }

        public double value(double x) {
            return (quadraticity_ * quadraticHelper_.value(x) + (1.0 - quadraticity_) * convMonoHelper_.value(x));
        }
        public double primitive(double x) {
            return (quadraticity_ * quadraticHelper_.primitive(x) + (1.0 - quadraticity_) * convMonoHelper_.primitive(x));
        }
        public double fNext() {
            return (quadraticity_ * quadraticHelper_.fNext() + (1.0 - quadraticity_) * convMonoHelper_.fNext());
        }
    }

    public class EverywhereConstantHelper : ISectionHelper {
        private double value_;
        private double prevPrimitive_;
        private double xPrev_;

        public EverywhereConstantHelper(double value, double prevPrimitive, double xPrev) {
            value_ = value;
            prevPrimitive_ = prevPrimitive;
            xPrev_ = xPrev;
        }

        public double value(double x) { return value_; }
        public double primitive(double x) { return prevPrimitive_ + (x - xPrev_) * value_; }
        public double fNext() { return value_; }
    }

    public class ConvexMonotone2Helper : ISectionHelper {
        private double xPrev_, xScaling_, gPrev_, gNext_, fAverage_, eta2_, prevPrimitive_;

        public ConvexMonotone2Helper(double xPrev, double xNext, double gPrev, double gNext, double fAverage, double eta2,
                                     double prevPrimitive) {
            xPrev_ = xPrev;
            xScaling_ = xNext - xPrev;
            gPrev_ = gPrev;
            gNext_ = gNext;
            fAverage_ = fAverage;
            eta2_ = eta2;
            prevPrimitive_ = prevPrimitive;
        }

        public double value(double x) {
            double xVal = (x - xPrev_) / xScaling_;
            if (xVal <= eta2_) {
                return (fAverage_ + gPrev_);
            } else {
                return (fAverage_ + gPrev_ + (gNext_ - gPrev_) / ((1 - eta2_) * (1 - eta2_)) * (xVal - eta2_) * (xVal - eta2_));
            }
        }

        public double primitive(double x) {
            double xVal = (x - xPrev_) / xScaling_;
            if (xVal <= eta2_) {
                return (prevPrimitive_ + xScaling_ * (fAverage_ * xVal + gPrev_ * xVal));
            } else {
                return (prevPrimitive_ + xScaling_ * (fAverage_ * xVal + gPrev_ * xVal + (gNext_ - gPrev_) / ((1 - eta2_) * (1 - eta2_)) *
                        (1.0 / 3.0 * (xVal * xVal * xVal - eta2_ * eta2_ * eta2_) - eta2_ * xVal * xVal + eta2_ * eta2_ * xVal)));
            }
        }
        public double fNext() { return (fAverage_ + gNext_); }
    }

    public class ConvexMonotone3Helper : ISectionHelper {
        private double xPrev_, xScaling_, gPrev_, gNext_, fAverage_, eta3_, prevPrimitive_;

        public ConvexMonotone3Helper(double xPrev, double xNext,
                              double gPrev, double gNext,
                              double fAverage, double eta3,
                              double prevPrimitive) {
            xPrev_ = xPrev;
            xScaling_ = xNext - xPrev;
            gPrev_ = gPrev;
            gNext_ = gNext;
            fAverage_ = fAverage;
            eta3_ = eta3;
            prevPrimitive_ = prevPrimitive;
        }

        public double value(double x) {
            double xVal = (x - xPrev_) / xScaling_;
            if (xVal <= eta3_) {
                return (fAverage_ + gNext_ + (gPrev_ - gNext_) / (eta3_ * eta3_) * (eta3_ - xVal) * (eta3_ - xVal));
            } else {
                return (fAverage_ + gNext_);
            }
        }

        public double primitive(double x) {
            double xVal = (x - xPrev_) / xScaling_;
            if (xVal <= eta3_) {
                return (prevPrimitive_ + xScaling_ * (fAverage_ * xVal + gNext_ * xVal + (gPrev_ - gNext_) / (eta3_ * eta3_) *
                        (1.0 / 3.0 * xVal * xVal * xVal - eta3_ * xVal * xVal + eta3_ * eta3_ * xVal)));
            } else {
                return (prevPrimitive_ + xScaling_ * (fAverage_ * xVal + gNext_ * xVal + (gPrev_ - gNext_) / (eta3_ * eta3_) *
                        (1.0 / 3.0 * eta3_ * eta3_ * eta3_)));
            }
        }
        public double fNext() { return (fAverage_ + gNext_); }
    }

    public class ConvexMonotone4Helper : ISectionHelper {
        protected double xPrev_, xScaling_, gPrev_, gNext_, fAverage_, eta4_, prevPrimitive_;
        protected double A_;

        public ConvexMonotone4Helper(double xPrev, double xNext, double gPrev, double gNext,
                              double fAverage, double eta4, double prevPrimitive) {
            xPrev_ = xPrev;
            xScaling_ = xNext - xPrev;
            gPrev_ = gPrev;
            gNext_ = gNext;
            fAverage_ = fAverage;
            eta4_ = eta4;
            prevPrimitive_ = prevPrimitive;
            A_ = -0.5 * (eta4_ * gPrev_ + (1 - eta4_) * gNext_);
        }

        public virtual double value(double x) {
            double xVal = (x - xPrev_) / xScaling_;
            if (xVal <= eta4_) {
                return (fAverage_ + A_ + (gPrev_ - A_) * (eta4_ - xVal) * (eta4_ - xVal) / (eta4_ * eta4_));
            } else {
                return (fAverage_ + A_ + (gNext_ - A_) * (xVal - eta4_) * (xVal - eta4_) / ((1 - eta4_) * (1 - eta4_)));
            }
        }

        public virtual double primitive(double x) {
            double xVal = (x - xPrev_) / xScaling_;
            double retVal;
            if (xVal <= eta4_) {
                retVal = prevPrimitive_ + xScaling_ * (fAverage_ + A_ + (gPrev_ - A_) / (eta4_ * eta4_) *
                        (eta4_ * eta4_ - eta4_ * xVal + 1.0 / 3.0 * xVal * xVal)) * xVal;
            } else {
                retVal = prevPrimitive_ + xScaling_ * (fAverage_ * xVal + A_ * xVal + (gPrev_ - A_) * (1.0 / 3.0 * eta4_) +
                         (gNext_ - A_) / ((1 - eta4_) * (1 - eta4_)) *
                         (1.0 / 3.0 * xVal * xVal * xVal - eta4_ * xVal * xVal + eta4_ * eta4_ * xVal - 1.0 / 3.0 * eta4_ * eta4_ * eta4_));
            }
            return retVal;
        }
        public double fNext() { return (fAverage_ + gNext_); }
    }

    public class ConvexMonotone4MinHelper : ConvexMonotone4Helper {
        private bool splitRegion_;
        private double xRatio_, x2_, x3_;

        public ConvexMonotone4MinHelper(double xPrev, double xNext, double gPrev, double gNext,
                                        double fAverage, double eta4, double prevPrimitive)
            : base(xPrev, xNext, gPrev, gNext, fAverage, eta4, prevPrimitive) {

            splitRegion_ = false;
            if (A_ + fAverage_ <= 0.0) {
                splitRegion_ = true;
                double fPrev = gPrev_ + fAverage_;
                double fNext = gNext_ + fAverage_;
                double reqdShift = (eta4_ * fPrev + (1 - eta4_) * fNext) / 3.0 - fAverage_;
                double reqdPeriod = reqdShift * xScaling_ / (fAverage_ + reqdShift);
                double xAdjust = xScaling_ - reqdPeriod;
                xRatio_ = xAdjust / xScaling_;

                fAverage_ += reqdShift;
                gNext_ = fNext - fAverage_;
                gPrev_ = fPrev - fAverage_;
                A_ = -(eta4_ * gPrev_ + (1.0 - eta4) * gNext_) / 2.0;
                x2_ = xPrev_ + xAdjust * eta4_;
                x3_ = xPrev_ + xScaling_ - xAdjust * (1.0 - eta4_);
            }
        }

        public override double value(double x) {
            if (!splitRegion_)
                return base.value(x);

            double xVal = (x - xPrev_) / xScaling_;
            if (x <= x2_) {
                xVal /= xRatio_;
                return (fAverage_ + A_ + (gPrev_ - A_) * (eta4_ - xVal) * (eta4_ - xVal) / (eta4_ * eta4_));
            } else if (x < x3_) {
                return 0.0;
            } else {
                xVal = 1.0 - (1.0 - xVal) / xRatio_;
                return (fAverage_ + A_ + (gNext_ - A_) * (xVal - eta4_) * (xVal - eta4_) / ((1 - eta4_) * (1 - eta4_)));
            }
        }

        public override double primitive(double x) {
            if (!splitRegion_)
                return base.primitive(x);

            double xVal = (x - xPrev_) / xScaling_;
            if (x <= x2_) {
                xVal /= xRatio_;
                return (prevPrimitive_ + xScaling_ * xRatio_ * (fAverage_ + A_ + (gPrev_ - A_) / (eta4_ * eta4_) *
                        (eta4_ * eta4_ - eta4_ * xVal + 1.0 / 3.0 * xVal * xVal)) * xVal);
            } else if (x <= x3_) {
                return (prevPrimitive_ + xScaling_ * xRatio_ * (fAverage_ * eta4_ + A_ * eta4_ + (gPrev_ - A_) / (eta4_ * eta4_) *
                        (1.0 / 3.0 * eta4_ * eta4_ * eta4_)));
            } else {
                xVal = 1.0 - (1.0 - xVal) / xRatio_;
                return (prevPrimitive_ + xScaling_ * xRatio_ * (fAverage_ * xVal + A_ * xVal + (gPrev_ - A_) * (1.0 / 3.0 * eta4_) +
                        (gNext_ - A_) / ((1.0 - eta4_) * (1.0 - eta4_)) *
                        (1.0 / 3.0 * xVal * xVal * xVal - eta4_ * xVal * xVal + eta4_ * eta4_ * xVal - 1.0 / 3.0 * eta4_ * eta4_ * eta4_)));
            }
        }
    }

    public class ConstantGradHelper : ISectionHelper {
        private double fPrev_, prevPrimitive_, xPrev_, fGrad_, fNext_;

        public ConstantGradHelper(double fPrev, double prevPrimitive, double xPrev, double xNext, double fNext) {
            fPrev_ = fPrev;
            prevPrimitive_ = prevPrimitive;
            xPrev_ = xPrev;
            fGrad_ = ((fNext - fPrev) / (xNext - xPrev));
            fNext_ = fNext;
        }

        public double value(double x) { return (fPrev_ + (x - xPrev_) * fGrad_); }
        public double primitive(double x) { return (prevPrimitive_ + (x - xPrev_) * (fPrev_ + 0.5 * (x - xPrev_) * fGrad_)); }
        public double fNext() { return fNext_; }
    }

    public class QuadraticHelper : ISectionHelper {
        private double xPrev_, xNext_, fPrev_, fNext_, fAverage_, prevPrimitive_;
        private double xScaling_, a_, b_, c_;

        public QuadraticHelper(double xPrev, double xNext, double fPrev, double fNext, double fAverage, double prevPrimitive) {
            xPrev_ = xPrev;
            xNext_ = xNext;
            fPrev_ = fPrev;
            fNext_ = fNext;
            fAverage_ = fAverage;
            prevPrimitive_ = prevPrimitive;
            a_ = 3 * fPrev_ + 3 * fNext_ - 6 * fAverage_;
            b_ = -(4 * fPrev_ + 2 * fNext_ - 6 * fAverage_);
            c_ = fPrev_;
            xScaling_ = xNext_ - xPrev_;
        }

        public double value(double x) {
            double xVal = (x - xPrev_) / xScaling_;
            return (a_ * xVal * xVal + b_ * xVal + c_);
        }

        public double primitive(double x) {
            double xVal = (x - xPrev_) / xScaling_;
            return (prevPrimitive_ + xScaling_ * (a_ / 3 * xVal * xVal + b_ / 2 * xVal + c_) * xVal);
        }

        public double fNext() { return fNext_; }
    }

    public class QuadraticMinHelper : ISectionHelper {
        private bool splitRegion_;
        private double x1_, x2_, x3_, x4_;
        private double a_, b_, c_;
        private double primitive1_, primitive2_;
        private double fAverage_, fPrev_, fNext_, xScaling_, xRatio_;

        public QuadraticMinHelper(double xPrev, double xNext, double fPrev, double fNext, double fAverage, double prevPrimitive) {
            splitRegion_ = false;
            x1_ = xPrev;
            x4_ = xNext;
            primitive1_ = prevPrimitive;
            fAverage_ = fAverage;
            fPrev_ = fPrev;
            fNext_ = fNext;
            a_ = 3 * fPrev_ + 3 * fNext_ - 6 * fAverage_;
            b_ = -(4 * fPrev_ + 2 * fNext_ - 6 * fAverage_);
            c_ = fPrev_;
            double d = b_ * b_ - 4 * a_ * c_;
            xScaling_ = x4_ - x1_;
            xRatio_ = 1.0;
            if (d > 0) {
                double aAv = 36;
                double bAv = -24 * (fPrev_ + fNext_);
                double cAv = 4 * (fPrev_ * fPrev_ + fPrev_ * fNext_ + fNext_ * fNext_);
                double dAv = bAv * bAv - 4.0 * aAv * cAv;
                if (dAv >= 0.0) {
                    splitRegion_ = true;
                    double avRoot = (-bAv - Math.Sqrt(dAv)) / (2 * aAv);

                    xRatio_ = fAverage_ / avRoot;
                    xScaling_ *= xRatio_;

                    a_ = 3 * fPrev_ + 3 * fNext_ - 6 * avRoot;
                    b_ = -(4 * fPrev_ + 2 * fNext_ - 6 * avRoot);
                    c_ = fPrev_;
                    double xRoot = -b_ / (2 * a_);
                    x2_ = x1_ + xRatio_ * (x4_ - x1_) * xRoot;
                    x3_ = x4_ - xRatio_ * (x4_ - x1_) * (1 - xRoot);
                    primitive2_ =
                        primitive1_ + xScaling_ * (a_ / 3 * xRoot * xRoot + b_ / 2 * xRoot + c_) * xRoot;
                }
            }
        }

        public double value(double x) {
            double xVal = (x - x1_) / (x4_ - x1_);
            if (splitRegion_) {
                if (x <= x2_) {
                    xVal /= xRatio_;
                } else if (x < x3_) {
                    return 0.0;
                } else {
                    xVal = 1.0 - (1.0 - xVal) / xRatio_;
                }
            }

            return c_ + b_ * xVal + a_ * xVal * xVal;
        }

        public double primitive(double x) {
            double xVal = (x - x1_) / (x4_ - x1_);
            if (splitRegion_) {
                if (x < x2_) {
                    xVal /= xRatio_;
                } else if (x < x3_) {
                    return primitive2_;
                } else {
                    xVal = 1.0 - (1.0 - xVal) / xRatio_;
                }
            }
            return primitive1_ + xScaling_ * (a_ / 3 * xVal * xVal + b_ / 2 * xVal + c_) * xVal;
        }

        public double fNext() { return fNext_; }
    } 
    #endregion


    public class ConvexMonotoneImpl : Interpolation.templateImpl {
        //typedef std::map<Real, boost::shared_ptr<SectionHelper> > helper_map;
        //Dictionary<double,SectionHelper>

        private Dictionary<double, ISectionHelper> sectionHelpers_ = new Dictionary<double,ISectionHelper>();
        private Dictionary<double, ISectionHelper> preSectionHelpers_ = new Dictionary<double,ISectionHelper>();
        private ISectionHelper extrapolationHelper_;
        private bool forcePositive_, constantLastPeriod_;
        private double quadraticity_;
        private double monotonicity_;
      
        public enum SectionType {
            EverywhereConstant,
            ConstantGradient,
            QuadraticMinimum,
            QuadraticMaximum
        };

        public ConvexMonotoneImpl(List<double> xBegin, int size, List<double> yBegin,
                                  double quadraticity, double monotonicity, bool forcePositive, bool constantLastPeriod,
                                  Dictionary<double, ISectionHelper> preExistingHelpers)
            : base(xBegin,size,yBegin) {
            preSectionHelpers_ = preExistingHelpers;
            forcePositive_ = forcePositive;
            constantLastPeriod_ = constantLastPeriod;
            quadraticity_ = quadraticity;
            monotonicity_ = monotonicity;

            if (!(monotonicity_ >= 0 && monotonicity_ <= 1))
                throw new ApplicationException("Monotonicity must lie between 0 and 1");
            if(!(quadraticity_ >= 0 && quadraticity_ <= 1))
                       throw new ApplicationException("Quadraticity must lie between 0 and 1");
            if(!(size_ >= 2))
                       throw new ApplicationException("Single point provided, not supported by convex " +
                       "monotone method as first point is ignored");
            if(!((size_ - preExistingHelpers.Count) > 1))
                        throw new ApplicationException("Too many existing helpers have been supplied");
        }

        public override void update() {
            sectionHelpers_.Clear();
            if (size_ == 2) { //single period
                ISectionHelper singleHelper = new EverywhereConstantHelper(yBegin_[1], 0.0, xBegin_[0]);
                sectionHelpers_.Add(xBegin_[1], singleHelper);
                extrapolationHelper_ = singleHelper;
                return;
            }

            List<double> f = new InitializedList<double>(size_);
            sectionHelpers_ = new Dictionary<double,ISectionHelper>(preSectionHelpers_);
            int startPoint = sectionHelpers_.Count+1;

            //first derive the boundary forwards.
            for (int i=startPoint; i<size_-1; ++i) {
                double dxPrev = xBegin_[i] - xBegin_[i-1];
                double dx = xBegin_[i+1] - xBegin_[i];
                f[i] = dxPrev/(dx+dxPrev) * yBegin_[i]
                     + dx/(dx+dxPrev) * yBegin_[i+1];
            }

            if (startPoint > 1)
                f[startPoint-1] = preSectionHelpers_.Last().Value.fNext();
            if (startPoint == 1)
                f[0] = 1.5 * yBegin_[1] - 0.5 * f[1];

            f[size_-1] = 1.5 * yBegin_[size_-1] - 0.5 * f[size_-2];

            if (forcePositive_) {
                if (f[0] < 0)
                    f[0] = 0.0;
                if (f[size_-1] < 0.0)
                    f[size_-1] = 0.0;
            }

            double primitive = 0.0;
            for (int i = 0; i < startPoint-1; ++i)
                primitive += yBegin_[i+1] * (xBegin_[i+1]-xBegin_[i]);

            int endPoint = size_;
            //constantLastPeriod_ = false;
            if (constantLastPeriod_)
                endPoint = endPoint-1;

            for (int i=startPoint; i< endPoint; ++i) {
                double gPrev = f[i-1] - yBegin_[i];
                double gNext = f[i] - yBegin_[i];
                //first deal with the zero gradient case
                if ( Math.Abs(gPrev) < 1.0E-14 && Math.Abs(gNext) < 1.0E-14 ) {
                    ISectionHelper singleHelper = new ConstantGradHelper(f[i - 1], primitive,
                                                            xBegin_[i-1],
                                                            xBegin_[i],
                                                            f[i]);
                    sectionHelpers_.Add(xBegin_[i], singleHelper);
                } else {
                    double quadraticity = quadraticity_;
                    ISectionHelper quadraticHelper = null;
                    ISectionHelper convMonotoneHelper = null;
                    if (quadraticity_ > 0.0) {
                        if (gPrev >= -2.0*gNext && gPrev > -0.5*gNext && forcePositive_) {
                            quadraticHelper = new QuadraticMinHelper(xBegin_[i-1],
                                                           xBegin_[i],
                                                           f[i-1], f[i],
                                                           yBegin_[i],
                                                           primitive);
                        } else {
                            quadraticHelper = new QuadraticHelper(xBegin_[i-1],
                                                        xBegin_[i],
                                                        f[i-1], f[i],
                                                        yBegin_[i],
                                                        primitive);
                        }
                    }
                    if (quadraticity_ < 1.0) {

                        if ((gPrev > 0.0 && -0.5*gPrev >= gNext && gNext >= -2.0*gPrev) ||
                            (gPrev < 0.0 && -0.5*gPrev <= gNext && gNext <= -2.0*gPrev)) {
                            quadraticity = 1.0;
                            if (quadraticity_ == 0) {
                                if (forcePositive_) {
                                    quadraticHelper = new QuadraticMinHelper(
                                                           xBegin_[i-1],
                                                           xBegin_[i],
                                                           f[i-1], f[i],
                                                           yBegin_[i],
                                                           primitive);
                                } else {
                                    quadraticHelper = new QuadraticHelper(
                                                           xBegin_[i-1],
                                                           xBegin_[i],
                                                           f[i-1], f[i],
                                                           yBegin_[i],
                                                           primitive);
                                }
                            }
                        }
                        else if ( (gPrev < 0.0 && gNext > -2.0*gPrev) ||
                                  (gPrev > 0.0 && gNext < -2.0*gPrev)) {

                            double eta = (gNext + 2.0*gPrev)/(gNext - gPrev);
                            double b2 = (1.0 + monotonicity_)/2.0;
                            if (eta < b2) {
                                convMonotoneHelper = new ConvexMonotone2Helper(
                                                           xBegin_[i-1],
                                                           xBegin_[i],
                                                           gPrev, gNext,
                                                           yBegin_[i],
                                                           eta, primitive);
                            } else {
                                if (forcePositive_) {
                                    convMonotoneHelper = new ConvexMonotone4MinHelper(
                                                           xBegin_[i-1],
                                                           xBegin_[i],
                                                           gPrev, gNext,
                                                           yBegin_[i],
                                                           b2, primitive);
                                } else {
                                    convMonotoneHelper = new ConvexMonotone4Helper(
                                                           xBegin_[i-1],
                                                           xBegin_[i],
                                                           gPrev, gNext,
                                                           yBegin_[i],
                                                           b2, primitive);
                                }
                            }
                        }
                        else if ( (gPrev > 0.0 && gNext < 0.0 && gNext > -0.5*gPrev) ||
                                  (gPrev < 0.0 && gNext > 0.0 && gNext < -0.5*gPrev) ) {
                            double eta = gNext/(gNext-gPrev) * 3.0;
                            double b3 = (1.0 - monotonicity_) / 2.0;
                            if (eta > b3) {
                                convMonotoneHelper = new ConvexMonotone3Helper(
                                                           xBegin_[i-1],
                                                           xBegin_[i],
                                                           gPrev, gNext,
                                                           yBegin_[i],
                                                           eta, primitive);
                            } else {
                                if (forcePositive_) {
                                    convMonotoneHelper = new ConvexMonotone4MinHelper(
                                                           xBegin_[i-1],
                                                           xBegin_[i],
                                                           gPrev, gNext,
                                                           yBegin_[i],
                                                           b3, primitive);
                                } else {
                                    convMonotoneHelper = new ConvexMonotone4Helper(
                                                           xBegin_[i-1],
                                                           xBegin_[i],
                                                           gPrev, gNext,
                                                           yBegin_[i],
                                                           b3, primitive);
                                }
                            }
                        } else {
                            double eta = gNext/(gPrev + gNext);
                            double b2 = (1.0 + monotonicity_) / 2.0;
                            double b3 = (1.0 - monotonicity_) / 2.0;
                            if (eta > b2)
                                eta = b2;
                            if (eta < b3)
                                eta = b3;
                            if (forcePositive_) {
                                convMonotoneHelper = new ConvexMonotone4MinHelper(
                                                           xBegin_[i-1],
                                                           xBegin_[i],
                                                           gPrev, gNext,
                                                           yBegin_[i],
                                                           eta, primitive);
                            } else {
                                convMonotoneHelper = new ConvexMonotone4Helper(
                                                           xBegin_[i-1],
                                                           xBegin_[i],
                                                           gPrev, gNext,
                                                           yBegin_[i],
                                                           eta, primitive);
                            }
                        }
                    }

                    if (quadraticity == 1.0) {
                        sectionHelpers_.Add(xBegin_[i], quadraticHelper);
                    } else if (quadraticity == 0.0) {
                        sectionHelpers_.Add(xBegin_[i], convMonotoneHelper);
                    } else {
                        sectionHelpers_.Add(xBegin_[i], new ComboHelper(quadraticHelper, convMonotoneHelper, quadraticity));
                    }
                }
                primitive += yBegin_[i] * (xBegin_[i]-xBegin_[i-1]);
            }

            if (constantLastPeriod_) {
                sectionHelpers_.Add(xBegin_[size_-1], new EverywhereConstantHelper(yBegin_[size_-1], primitive, xBegin_[size_-2]));
                extrapolationHelper_ = sectionHelpers_[xBegin_[size_-1]];
            } else {
                extrapolationHelper_ = new EverywhereConstantHelper((sectionHelpers_.Last()).Value.value(xBegin_.Last()),
                                                                    primitive, xBegin_.Last());
            }
        }

        public override double value(double x) {
            if (x >= xBegin_.Last()) {
                return extrapolationHelper_.value(x);
            }

            double i;
            if (x > sectionHelpers_.Keys.Last())
                i = sectionHelpers_.Keys.Last();
            else if (x < sectionHelpers_.Keys.First())
                i = sectionHelpers_.Keys.First();
            else
                i = sectionHelpers_.Keys.First(y => x < y);
            return sectionHelpers_[i].value(x);
        }

        public override double primitive(double x) {
            if (x >= xBegin_.Last()) {
                return extrapolationHelper_.primitive(x);
            }

            double i;
            if (x >= sectionHelpers_.Keys.Last())
                i = sectionHelpers_.Keys.Last();
            else if (x <= sectionHelpers_.Keys.First())
                i = sectionHelpers_.Keys.First();
            else
                i = sectionHelpers_.Keys.First(y => x < y);
            return sectionHelpers_[i].primitive(x);
        }

        public override double derivative(double x) {
            throw new NotImplementedException("Convex-monotone spline derivative not implemented");
        }

        public override double secondDerivative(double x) {
            throw new NotImplementedException("Convex-monotone spline second derivative not implemented");
        }

        public Dictionary<double, ISectionHelper> getExistingHelpers() {
            Dictionary<double, ISectionHelper> retArray = new Dictionary<double,ISectionHelper>(sectionHelpers_);
            if (constantLastPeriod_)
                retArray.Remove(xBegin_.Last());
            return retArray;
        }
    }

    //! Convex monotone yield-curve interpolation method.
    /*! Enhances implementation of the convex monotone method
        described in "Interpolation Methods for Curve Construction" by
        Hagan & West AMF Vol 13, No2 2006.

        A setting of monotonicity = 1 and quadraticity = 0 will
        reproduce the basic Hagan/West method. However, this can
        produce excessive gradients which can mean P&L swings for some
        curves.  Setting monotonicity < 1 and/or quadraticity > 0
        produces smoother curves.  Extra enhancement to avoid negative
        values (if required) is in place.
    */
    public class ConvexMonotoneInterpolation : Interpolation {
        //typedef std::map<Real, boost::shared_ptr<SectionHelper> > helper_map;
        //Dictionary<double,ISectionHelper>
      
        //public ConvexMonotoneInterpolation(List<double> xBegin, int size, List<double> yBegin, double quadraticity,
        //                            double monotonicity, bool forcePositive,
        //                            bool flatFinalPeriod = false,
        //                            Dictionary<double,ISectionHelper> preExistingHelpers = new Dictionary<double,ISectionHelper>()) {
        public ConvexMonotoneInterpolation(List<double> xBegin, int size, List<double> yBegin, double quadraticity,
                                    double monotonicity, bool forcePositive, bool flatFinalPeriod)
            : this(xBegin, size, yBegin, quadraticity, monotonicity, forcePositive, flatFinalPeriod, 
                   new Dictionary<double, ISectionHelper>()) { }
        public ConvexMonotoneInterpolation(List<double> xBegin, int size, List<double> yBegin, double quadraticity,
                                    double monotonicity, bool forcePositive, bool flatFinalPeriod,
                                    Dictionary<double,ISectionHelper> preExistingHelpers) {
            impl_ = new ConvexMonotoneImpl(xBegin, size, yBegin, quadraticity, monotonicity, forcePositive,
                                           flatFinalPeriod, preExistingHelpers);
            impl_.update();
        }

        // public ConvexMonotoneInterpolation(Interpolation interp) : base(interp) { }

        public Dictionary<double,ISectionHelper> getExistingHelpers() {
            ConvexMonotoneImpl derived = impl_ as ConvexMonotoneImpl;
            return derived.getExistingHelpers();
        }
    }


    //! Convex-monotone interpolation factory and traits
    public class ConvexMonotone : IInterpolationFactory {
        private double quadraticity_, monotonicity_;
        private bool forcePositive_;

        //public ConvexMonotone(double quadraticity = 0.3, double monotonicity = 0.7, bool forcePositive = true) {
        public ConvexMonotone() : this(0.3, 0.7, true) { }
        public ConvexMonotone(double quadraticity, double monotonicity, bool forcePositive) {
            quadraticity_ = quadraticity;
            monotonicity_ = monotonicity;
            forcePositive_ = forcePositive;
        }

        public Interpolation interpolate(List<double> xBegin, int size, List<double> yBegin) {
            return new ConvexMonotoneInterpolation(xBegin, size, yBegin, quadraticity_, monotonicity_, forcePositive_, false);
        }

        public Interpolation localInterpolate(List<double> xBegin, int size, List<double> yBegin, int localisation,
                                              ConvexMonotoneInterpolation prevInterpolation, int finalSize) {
            int length = size;
            if (length - localisation == 1) { // the first time this
                                              // function is called
                return new ConvexMonotoneInterpolation(xBegin, size, yBegin, quadraticity_, monotonicity_, forcePositive_,
                                                       length != finalSize);
            }

            ConvexMonotoneInterpolation interp = prevInterpolation;
            return new ConvexMonotoneInterpolation(xBegin, size, yBegin, quadraticity_, monotonicity_,
                                                   forcePositive_, length != finalSize, interp.getExistingHelpers());
        }
        
        public bool global { get { return true; } }
        public int requiredPoints { get { return 2; } }
        public int dataSizeAdjustment { get { return 1; } }
    }
}
