/*
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
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
    public abstract class VanillaOptionPricer {
        public abstract double value(double strike, Option.Type optionType, double deflator);
    }

    //===========================================================================//
    //                          BlackVanillaOptionPricer                         //
    //===========================================================================//
    public class BlackVanillaOptionPricer : VanillaOptionPricer {
        private double forwardValue_;
        private Date expiryDate_;
        private Period swapTenor_;
        private SwaptionVolatilityStructure volatilityStructure_;
        private SmileSection smile_;

        public BlackVanillaOptionPricer(double forwardValue, Date expiryDate, Period swapTenor, SwaptionVolatilityStructure volatilityStructure) {
            forwardValue_ = forwardValue;
            expiryDate_ = expiryDate;
            swapTenor_ = swapTenor;
            volatilityStructure_ = volatilityStructure;
            smile_ = volatilityStructure_.smileSection(expiryDate_, swapTenor_);
        }

        public override double value(double strike, Option.Type optionType, double deflator) {
            double variance = smile_.variance(strike);
            return deflator * Utils.blackFormula(optionType, strike, forwardValue_, Math.Sqrt(variance));
        }
	}

    public abstract class GFunction {
        public abstract double value(double x);
        public abstract double firstDerivative(double x);
        public abstract double secondDerivative(double x);
    }

    public class GFunctionFactory {
        public enum YieldCurveModel : int {
            Standard,
            ExactYield,
            ParallelShifts,
            NonParallelShifts
        }
        public static GFunction newGFunctionStandard(int q, double delta, int swapLength) {
            return new GFunctionStandard(q, delta, swapLength) as GFunction;
        }
        public static GFunction newGFunctionExactYield(CmsCoupon coupon) {
            return new GFunctionExactYield(coupon) as GFunction;
        }
        public static GFunction newGFunctionWithShifts(CmsCoupon coupon, Handle<Quote> meanReversion) {
            return new GFunctionWithShifts(coupon, meanReversion) as GFunction;
        }

        //===========================================================================//
        //                              GFunctionStandard                            //
        //===========================================================================//
        private class GFunctionStandard : GFunction {
            // number of period per year 
            protected int q_;
            //             fraction of a period between the swap start date and the pay date  
            protected double delta_;
            // length of swap
            protected int swapLength_;

            public GFunctionStandard(int q, double delta, int swapLength) {
                q_ = q;
                delta_ = delta;
                swapLength_ = swapLength;
            }

            public override double value(double x) {
                double n = swapLength_ * q_;
                return x / Math.Pow((1.0 + x / q_), delta_) * 1.0 / (1.0 - 1.0 / Math.Pow((1.0 + x / q_), n));
            }

            public override double firstDerivative(double x) {
                double n = swapLength_ * q_;
                double a = 1.0 + x / q_;
                double AA = a - delta_ / q_ * x;
                double B = Math.Pow(a, (n - delta_ - 1.0)) / (Math.Pow(a, n) - 1.0);

                double secNum = n * x * Math.Pow(a, (n - 1.0));
                double secDen = q_ * Math.Pow(a, delta_) * (Math.Pow(a, n) - 1.0) * (Math.Pow(a, n) - 1.0);
                double sec = secNum / secDen;

                return AA * B - sec;
            }

            public override double secondDerivative(double x) {
                double n = swapLength_ * q_;
                double a = 1.0 + x / q_;
                double AA = a - delta_ / q_ * x;
                double A1 = (1.0 - delta_) / q_;
                double B = Math.Pow(a, (n - delta_ - 1.0)) / (Math.Pow(a, n) - 1.0);
                double Num = (1.0 + delta_ - n) * Math.Pow(a, (n - delta_ - 2.0)) - (1.0 + delta_) * Math.Pow(a, (2.0 * n - delta_ - 2.0));
                double Den = (Math.Pow(a, n) - 1.0) * (Math.Pow(a, n) - 1.0);
                double B1 = 1.0 / q_ * Num / Den;

                double C = x / Math.Pow(a, delta_);
                double C1 = (Math.Pow(a, delta_) - delta_ / q_ * x * Math.Pow(a, (delta_ - 1.0))) / Math.Pow(a, 2 * delta_);

                double D = Math.Pow(a, (n - 1.0)) / ((Math.Pow(a, n) - 1.0) * (Math.Pow(a, n) - 1.0));
                double D1 = ((n - 1.0) * Math.Pow(a, (n - 2.0)) * (Math.Pow(a, n) - 1.0) - 2 * n * Math.Pow(a, (2 * (n - 1.0)))) / (q_ * (Math.Pow(a, n) - 1.0) * (Math.Pow(a, n) - 1.0) * (Math.Pow(a, n) - 1.0));

                return A1 * B + AA * B1 - n / q_ * (C1 * D + C * D1);
            }
		}

        //===========================================================================//
        //                              GFunctionExactYield                          //
        //===========================================================================//
        private class GFunctionExactYield : GFunction {
            //             fraction of a period between the swap start date and the pay date  
            protected double delta_;
            // accruals fraction
            protected List<double> accruals_;

            public GFunctionExactYield(CmsCoupon coupon) {

                SwapIndex swapIndex = coupon.swapIndex();
                VanillaSwap swap = swapIndex.underlyingSwap(coupon.fixingDate());

                Schedule schedule = swap.fixedSchedule();
                Handle<YieldTermStructure> rateCurve = swapIndex.forwardingTermStructure();

                DayCounter dc = swapIndex.dayCounter();

                double swapStartTime = dc.yearFraction(rateCurve.link.referenceDate(), schedule.startDate());
                double swapFirstPaymentTime = dc.yearFraction(rateCurve.link.referenceDate(), schedule.date(1));

                double paymentTime = dc.yearFraction(rateCurve.link.referenceDate(), coupon.date());

                delta_ = (paymentTime - swapStartTime) / (swapFirstPaymentTime - swapStartTime);

                List<CashFlow> fixedLeg = new List<CashFlow>(swap.fixedLeg());
                int n = fixedLeg.Count;
                accruals_ = new List<double>();
                for (int i = 0; i < n; ++i) {
                    Coupon coupon1 = fixedLeg[i] as Coupon;
                    accruals_.Add(coupon1.accrualPeriod());
                }
            }

            public override double value(double x) {
                double product = 1.0;
                for (int i = 0; i < accruals_.Count; i++) {
                    product *= 1.0 / (1.0 + accruals_[i] * x);
                }
                return x * Math.Pow(1.0 + accruals_[0] * x, -delta_) * (1.0 / (1.0 - product));
            }

            public override double firstDerivative(double x) {
                double c = -1.0;
                double derC = 0.0;
                List<double> b = new List<double>();
                for (int i = 0; i < accruals_.Count; i++) {
                    double temp = 1.0 / (1.0 + accruals_[i] * x);
                    b.Add(temp);
                    c *= temp;
                    derC += accruals_[i] * temp;
                }
                c += 1.0;
                c = 1.0 / c;
                derC *= (c - c * c);

                return -delta_ * accruals_[0] * Math.Pow(b[0], delta_ + 1.0) * x * c + Math.Pow(b[0], delta_) * c + Math.Pow(b[0], delta_) * x * derC;
                //double dx = 1.0e-8;
                //return (operator()(x+dx)-operator()(x-dx))/(2.0*dx);
            }

            public override double secondDerivative(double x) {
                double c = -1.0;
                double sum = 0.0;
                double sumOfSquare = 0.0;
                List<double> b = new List<double>();
                for (int i = 0; i < accruals_.Count; i++) {
                    double temp = 1.0 / (1.0 + accruals_[i] * x);
                    b.Add(temp);
                    c *= temp;
                    sum += accruals_[i] * temp;
                    sumOfSquare += Math.Pow(accruals_[i] * temp, 2.0);
                }
                c += 1.0;
                c = 1.0 / c;
                double derC = sum * (c - c * c);

                return (-delta_ * accruals_[0] * Math.Pow(b[0], delta_ + 1.0) * c + Math.Pow(b[0], delta_) * derC) * (-delta_ * accruals_[0] * b[0] * x + 1.0 + x * (1.0 - c) * sum) + Math.Pow(b[0], delta_) * c * (delta_ * Math.Pow(accruals_[0] * b[0], 2.0) * x - delta_ * accruals_[0] * b[0] - x * derC * sum + (1.0 - c) * sum - x * (1.0 - c) * sumOfSquare);
                //double dx = 1.0e-8;
                //return (firstDerivative(x+dx)-firstDerivative(x-dx))/(2.0*dx);
            }
        }

        private class GFunctionWithShifts : GFunction {
            private double swapStartTime_;

            private double shapedPaymentTime_;
            private List<double> shapedSwapPaymentTimes_;

            private List<double> accruals_;
            private List<double> swapPaymentDiscounts_;
            private double discountAtStart_;
            private double discountRatio_;

            private double swapRateValue_;
            private Handle<Quote> meanReversion_;

            private double calibratedShift_;
            private double tmpRs_;
            private double accuracy_;

            private ObjectiveFunction objectiveFunction_;

            //* function describing the non-parallel shape of the curve shift*/
            private double shapeOfShift(double s) {
                double x = s - swapStartTime_;
                double meanReversion = meanReversion_.link.value();
                if (meanReversion > 0) {
                    return (1.0 - Math.Exp(-meanReversion * x)) / meanReversion;
                } else {
                    return x;
                }
            }
            //* calibration of shift*/
            private double calibrationOfShift(double Rs) {
                if (Rs != tmpRs_) {
                    double initialGuess;
                    double N = 0;
                    double D = 0;
                    for (int i = 0; i < accruals_.Count; i++) {
                        N += accruals_[i] * swapPaymentDiscounts_[i];
                        D += accruals_[i] * swapPaymentDiscounts_[i] * shapedSwapPaymentTimes_[i];
                    }
                    N *= Rs;
                    D *= Rs;
                    N += accruals_.Last() * swapPaymentDiscounts_.Last() - objectiveFunction_.gFunctionWithShifts().discountAtStart_;
                    D += accruals_.Last() * swapPaymentDiscounts_.Last() * shapedSwapPaymentTimes_.Last();
                    initialGuess = N / D;

                    objectiveFunction_.setSwapRateValue(Rs);
                    Newton solver = new Newton();
                    solver.setMaxEvaluations(1000);

                    // these boundaries migth not be big enough if the volatility
                    // of big swap rate values is too high . In this case the G function
                    // is not even integrable, so better to fix the vol than increasing
                    // these values
                    double lower = -20;
                    double upper = 20.0;

                    try {
                        calibratedShift_ = solver.solve(objectiveFunction_, accuracy_, Math.Max(Math.Min(initialGuess, upper * .99), lower * .99), lower, upper);
                    } catch (Exception e) {
                        throw new ApplicationException("meanReversion: " + meanReversion_.link.value() + ", swapRateValue: " + swapRateValue_ + ", swapStartTime: " + swapStartTime_ + ", shapedPaymentTime: " + shapedPaymentTime_ + "\n error message: " + e.Message);
                    }
                    tmpRs_ = Rs;
                }
                return calibratedShift_;
            }

            private double functionZ(double x) {
                return Math.Exp(-shapedPaymentTime_ * x) / (1.0 - discountRatio_ * Math.Exp(-shapedSwapPaymentTimes_.Last() * x));
            }

            private double derRs_derX(double x) {
                double sqrtDenominator = 0;
                double derSqrtDenominator = 0;
                for (int i = 0; i < accruals_.Count; i++) {
                    sqrtDenominator += accruals_[i] * swapPaymentDiscounts_[i] * Math.Exp(-shapedSwapPaymentTimes_[i] * x);
                    derSqrtDenominator -= shapedSwapPaymentTimes_[i] * accruals_[i] * swapPaymentDiscounts_[i] * Math.Exp(-shapedSwapPaymentTimes_[i] * x);
                }
                double denominator = sqrtDenominator * sqrtDenominator;

                double numerator = 0;
                numerator += shapedSwapPaymentTimes_.Last() * swapPaymentDiscounts_.Last() * Math.Exp(-shapedSwapPaymentTimes_.Last() * x) * sqrtDenominator;
                numerator -= (discountAtStart_ - swapPaymentDiscounts_.Last() * Math.Exp(-shapedSwapPaymentTimes_.Last() * x)) * derSqrtDenominator;
                if (!(denominator != 0))
                    throw new ApplicationException("GFunctionWithShifts::derRs_derX: denominator == 0");
                return numerator / denominator;
            }

            private double derZ_derX(double x) {
                double sqrtDenominator = (1.0 - discountRatio_ * Math.Exp(-shapedSwapPaymentTimes_.Last() * x));
                double denominator = sqrtDenominator * sqrtDenominator;
                if (!(denominator != 0))
                    throw new ApplicationException("GFunctionWithShifts::derZ_derX: denominator == 0");

                double numerator = 0;
                numerator -= shapedPaymentTime_ * Math.Exp(-shapedPaymentTime_ * x) * sqrtDenominator;
                numerator -= shapedSwapPaymentTimes_.Last() * Math.Exp(-shapedPaymentTime_ * x) * (1.0 - sqrtDenominator);

                return numerator / denominator;
            }

            private double der2Rs_derX2(double x) {
                double denOfRfunztion = 0.0;
                double derDenOfRfunztion = 0.0;
                double der2DenOfRfunztion = 0.0;
                for (int i = 0; i < accruals_.Count; i++) {
                    denOfRfunztion += accruals_[i] * swapPaymentDiscounts_[i] * Math.Exp(-shapedSwapPaymentTimes_[i] * x);
                    derDenOfRfunztion -= shapedSwapPaymentTimes_[i] * accruals_[i] * swapPaymentDiscounts_[i] * Math.Exp(-shapedSwapPaymentTimes_[i] * x);
                    der2DenOfRfunztion += shapedSwapPaymentTimes_[i] * shapedSwapPaymentTimes_[i] * accruals_[i] * swapPaymentDiscounts_[i] * Math.Exp(-shapedSwapPaymentTimes_[i] * x);
                }

                double denominator = Math.Pow(denOfRfunztion, 4);

                double numOfDerR = 0;
                numOfDerR += shapedSwapPaymentTimes_.Last() * swapPaymentDiscounts_.Last() * Math.Exp(-shapedSwapPaymentTimes_.Last() * x) * denOfRfunztion;
                numOfDerR -= (discountAtStart_ - swapPaymentDiscounts_.Last() * Math.Exp(-shapedSwapPaymentTimes_.Last() * x)) * derDenOfRfunztion;

                double denOfDerR = Math.Pow(denOfRfunztion, 2);

                double derNumOfDerR = 0.0;
                derNumOfDerR -= shapedSwapPaymentTimes_.Last() * shapedSwapPaymentTimes_.Last() * swapPaymentDiscounts_.Last() * Math.Exp(-shapedSwapPaymentTimes_.Last() * x) * denOfRfunztion;
                derNumOfDerR += shapedSwapPaymentTimes_.Last() * swapPaymentDiscounts_.Last() * Math.Exp(-shapedSwapPaymentTimes_.Last() * x) * derDenOfRfunztion;

                derNumOfDerR -= (shapedSwapPaymentTimes_.Last() * swapPaymentDiscounts_.Last() * Math.Exp(-shapedSwapPaymentTimes_.Last() * x)) * derDenOfRfunztion;
                derNumOfDerR -= (discountAtStart_ - swapPaymentDiscounts_.Last() * Math.Exp(-shapedSwapPaymentTimes_.Last() * x)) * der2DenOfRfunztion;

                double derDenOfDerR = 2 * denOfRfunztion * derDenOfRfunztion;

                double numerator = derNumOfDerR * denOfDerR - numOfDerR * derDenOfDerR;
                if (!(denominator != 0))
                    throw new ApplicationException("GFunctionWithShifts::der2Rs_derX2: denominator == 0");
                return numerator / denominator;
            }

            private double der2Z_derX2(double x) {
                double denOfZfunction = (1.0 - discountRatio_ * Math.Exp(-shapedSwapPaymentTimes_.Last() * x));
                double derDenOfZfunction = shapedSwapPaymentTimes_.Last() * discountRatio_ * Math.Exp(-shapedSwapPaymentTimes_.Last() * x);
                double denominator = Math.Pow(denOfZfunction, 4);
                if (!(denominator != 0))
                    throw new ApplicationException("GFunctionWithShifts::der2Z_derX2: denominator == 0");

                double numOfDerZ = 0;
                numOfDerZ -= shapedPaymentTime_ * Math.Exp(-shapedPaymentTime_ * x) * denOfZfunction;
                numOfDerZ -= shapedSwapPaymentTimes_.Last() * Math.Exp(-shapedPaymentTime_ * x) * (1.0 - denOfZfunction);

                double denOfDerZ = Math.Pow(denOfZfunction, 2);
                double derNumOfDerZ = (-shapedPaymentTime_ * Math.Exp(-shapedPaymentTime_ * x) * (-shapedPaymentTime_ + (shapedPaymentTime_ * discountRatio_ - shapedSwapPaymentTimes_.Last() * discountRatio_) * Math.Exp(-shapedSwapPaymentTimes_.Last() * x)) - shapedSwapPaymentTimes_.Last() * Math.Exp(-shapedPaymentTime_ * x) * (shapedPaymentTime_ * discountRatio_ - shapedSwapPaymentTimes_.Last() * discountRatio_) * Math.Exp(-shapedSwapPaymentTimes_.Last() * x));

                double derDenOfDerZ = 2 * denOfZfunction * derDenOfZfunction;
                double numerator = derNumOfDerZ * denOfDerZ - numOfDerZ * derDenOfDerZ;

                return numerator / denominator;
            }

            private class ObjectiveFunction : ISolver1d {
                private GFunctionWithShifts o_;
                private double Rs_;
                private double derivative_;

                public ObjectiveFunction(GFunctionWithShifts o, double Rs) {
                    o_ = o;
                    Rs_ = Rs;
                }
                public override double value(double x) {
                    double result = 0;
                    derivative_ = 0;
                    for (int i = 0; i < o_.accruals_.Count; i++) {
                        double temp = o_.accruals_[i] * o_.swapPaymentDiscounts_[i] * Math.Exp(-o_.shapedSwapPaymentTimes_[i] * x);
                        result += temp;
                        derivative_ -= o_.shapedSwapPaymentTimes_[i] * temp;
                    }
                    result *= Rs_;
                    derivative_ *= Rs_;
                    double temp1 = o_.swapPaymentDiscounts_.Last() * Math.Exp(-o_.shapedSwapPaymentTimes_.Last() * x);

                    result += temp1 - o_.discountAtStart_;
                    derivative_ -= o_.shapedSwapPaymentTimes_.Last() * temp1;
                    return result;
                }

                public override double derivative(double UnnamedParameter1) { return derivative_; }
                public void setSwapRateValue(double x) { Rs_ = x; }
                public GFunctionWithShifts gFunctionWithShifts() { return o_; }
            }

            //===========================================================================//
            //                            GFunctionWithShifts                            //
            //===========================================================================//
            public GFunctionWithShifts(CmsCoupon coupon, Handle<Quote> meanReversion) {
                meanReversion_ = meanReversion;
                calibratedShift_ = 0.03;
                tmpRs_ = 10000000.0;
                accuracy_ = 1.0e-14;

                SwapIndex swapIndex = coupon.swapIndex();
                VanillaSwap swap = swapIndex.underlyingSwap(coupon.fixingDate());

                swapRateValue_ = swap.fairRate();

                objectiveFunction_ = new ObjectiveFunction(this, swapRateValue_);

                Schedule schedule = swap.fixedSchedule();
                Handle<YieldTermStructure> rateCurve = swapIndex.forwardingTermStructure();
                DayCounter dc = swapIndex.dayCounter();

                swapStartTime_ = dc.yearFraction(rateCurve.link.referenceDate(), schedule.startDate());
                discountAtStart_ = rateCurve.link.discount(schedule.startDate());

                double paymentTime = dc.yearFraction(rateCurve.link.referenceDate(), coupon.date());

                shapedPaymentTime_ = shapeOfShift(paymentTime);

                List<CashFlow> fixedLeg = new List<CashFlow>(swap.fixedLeg());
                int n = fixedLeg.Count;

                shapedSwapPaymentTimes_ = new List<double>();
                swapPaymentDiscounts_ = new List<double>();
                accruals_ = new List<double>();

                for (int i = 0; i < n; ++i) {
                    Coupon coupon1 = fixedLeg[i] as Coupon;
                    accruals_.Add(coupon1.accrualPeriod());
                    Date paymentDate = new Date(coupon1.date().serialNumber());
                    double swapPaymentTime = dc.yearFraction(rateCurve.link.referenceDate(), paymentDate);
                    shapedSwapPaymentTimes_.Add(shapeOfShift(swapPaymentTime));
                    swapPaymentDiscounts_.Add(rateCurve.link.discount(paymentDate));
                }
                discountRatio_ = swapPaymentDiscounts_.Last() / discountAtStart_;
            }

            public override double value(double Rs) {
                double calibratedShift = calibrationOfShift(Rs);
                return Rs * functionZ(calibratedShift);
            }

            public override double firstDerivative(double Rs) {
                //double dRs = 1.0e-8;
                //return (operator()(Rs+dRs)-operator()(Rs-dRs))/(2.0*dRs);
                double calibratedShift = calibrationOfShift(Rs);
                return functionZ(calibratedShift) + Rs * derZ_derX(calibratedShift) / derRs_derX(calibratedShift);
            }

            public override double secondDerivative(double Rs) {
                //double dRs = 1.0e-8;
                //return (firstDerivative(Rs+dRs)-firstDerivative(Rs-dRs))/(2.0*dRs);
                double calibratedShift = calibrationOfShift(Rs);
                return 2.0 * derZ_derX(calibratedShift) / derRs_derX(calibratedShift) + Rs * der2Z_derX2(calibratedShift) / Math.Pow(derRs_derX(calibratedShift), 2.0) - Rs * derZ_derX(calibratedShift) * der2Rs_derX2(calibratedShift) / Math.Pow(derRs_derX(calibratedShift), 3.0);
            }
        }
	}


    //===========================================================================//
    //                             HaganPricer                               //
    //===========================================================================//
    //! Base class for the pricing of a CMS coupon via static replication as in Hagan's "Conundrums..." article
    public abstract class HaganPricer : CmsCouponPricer {
        public override double swapletRate() {
            return swapletPrice() / (coupon_.accrualPeriod() * discount_);
        }

        public override double capletPrice(double effectiveCap) {
            // caplet is equivalent to call option on fixing
            Date today = Settings.evaluationDate();
            if (fixingDate_ <= today) {
                // the fixing is determined
                double Rs = Math.Max(coupon_.swapIndex().fixing(fixingDate_) - effectiveCap, 0.0);
                double price = (gearing_ * Rs) * (coupon_.accrualPeriod() * discount_);
                return price;
            } else {
                double cutoffNearZero = 1e-10;
                double capletPrice = 0;
                if (effectiveCap < cutoffForCaplet_) {
                    double effectiveStrikeForMax = Math.Max(effectiveCap, cutoffNearZero);
                    capletPrice = optionletPrice(Option.Type.Call, effectiveStrikeForMax);
                }
                return gearing_ * capletPrice;
            }
        }

        public override double capletRate(double effectiveCap) {
            return capletPrice(effectiveCap) / (coupon_.accrualPeriod() * discount_);
        }

        public override double floorletPrice(double effectiveFloor) {
            // floorlet is equivalent to put option on fixing
            Date today = Settings.evaluationDate();
            if (fixingDate_ <= today) {
                // the fixing is determined
                double Rs = Math.Max(effectiveFloor - coupon_.swapIndex().fixing(fixingDate_), 0.0);
                double price = (gearing_ * Rs) * (coupon_.accrualPeriod() * discount_);
                return price;
            } else {
                double cutoffNearZero = 1e-10;
                double floorletPrice = 0;
                if (effectiveFloor > cutoffForFloorlet_) {
                    double effectiveStrikeForMin = Math.Max(effectiveFloor, cutoffNearZero);
                    floorletPrice = optionletPrice(Option.Type.Put, effectiveStrikeForMin);
                }
                return gearing_ * floorletPrice;
            }
        }
        public override double floorletRate(double effectiveFloor) {
            return floorletPrice(effectiveFloor) / (coupon_.accrualPeriod() * discount_);
        }
        // 
        public double meanReversion() {
            return meanReversion_.link.value();
        }
        public void setMeanReversion(Handle<Quote> meanReversion) {
            if (meanReversion_ != null)
                meanReversion_.unregisterWith(update);
            meanReversion_ = meanReversion;
            if (meanReversion_ != null)
                meanReversion_.registerWith(update);
            update();
        }

        protected HaganPricer(Handle<SwaptionVolatilityStructure> swaptionVol, GFunctionFactory.YieldCurveModel modelOfYieldCurve, Handle<Quote> meanReversion)
            : base(swaptionVol) {
            modelOfYieldCurve_ = modelOfYieldCurve;
            cutoffForCaplet_ = 2;
            cutoffForFloorlet_ = 0;
            meanReversion_ = meanReversion;

            if (meanReversion_.link != null)
                meanReversion_.registerWith(update);
        }

        public override void initialize(FloatingRateCoupon coupon) {
            coupon_ = coupon as CmsCoupon;
            Utils.QL_REQUIRE( coupon_ != null, () => "CMS coupon needed" );
            gearing_ = coupon_.gearing();
            spread_ = coupon_.spread();

            fixingDate_ = coupon_.fixingDate();
            paymentDate_ = coupon_.date();
            SwapIndex swapIndex = coupon_.swapIndex();
            rateCurve_ = swapIndex.forwardingTermStructure().link;

            Date today = Settings.evaluationDate();

            if (paymentDate_ > today)
                discount_ = rateCurve_.discount(paymentDate_);
            else
                discount_ = 1.0;

            spreadLegValue_ = spread_ * coupon_.accrualPeriod() * discount_;

            if (fixingDate_ > today) {
                swapTenor_ = swapIndex.tenor();
                VanillaSwap swap = swapIndex.underlyingSwap(fixingDate_);

                swapRateValue_ = swap.fairRate();

                double bp = 1.0e-4;
                annuity_ = (swap.floatingLegBPS() / bp);

                int q = (int)swapIndex.fixedLegTenor().frequency();
                Schedule schedule = swap.fixedSchedule();
                DayCounter dc = swapIndex.dayCounter();
                //DayCounter dc = coupon.dayCounter();
                double startTime = dc.yearFraction(rateCurve_.referenceDate(), swap.startDate());
                double swapFirstPaymentTime = dc.yearFraction(rateCurve_.referenceDate(), schedule.date(1));
                double paymentTime = dc.yearFraction(rateCurve_.referenceDate(), paymentDate_);
                double delta = (paymentTime - startTime) / (swapFirstPaymentTime - startTime);

                switch (modelOfYieldCurve_) {
                    case GFunctionFactory.YieldCurveModel.Standard:
                        gFunction_ = GFunctionFactory.newGFunctionStandard(q, delta, swapTenor_.length());
                        break;
                    case GFunctionFactory.YieldCurveModel.ExactYield:
                        gFunction_ = GFunctionFactory.newGFunctionExactYield(coupon_);
                        break;
                    case GFunctionFactory.YieldCurveModel.ParallelShifts: {
                            Handle<Quote> nullMeanReversionQuote = new Handle<Quote>(new SimpleQuote(0.0));
                            gFunction_ = GFunctionFactory.newGFunctionWithShifts(coupon_, nullMeanReversionQuote);
                        }
                        break;
                    case GFunctionFactory.YieldCurveModel.NonParallelShifts:
                        gFunction_ = GFunctionFactory.newGFunctionWithShifts(coupon_, meanReversion_);
                        break;
                    default:
                        throw new ApplicationException("unknown/illegal gFunction type");
                }
                vanillaOptionPricer_ = new BlackVanillaOptionPricer(swapRateValue_, fixingDate_, swapTenor_, swaptionVolatility().link);
            }
        }

        protected YieldTermStructure rateCurve_;
        protected GFunctionFactory.YieldCurveModel modelOfYieldCurve_;
        protected GFunction gFunction_;
        protected CmsCoupon coupon_;
        protected Date paymentDate_;
        protected Date fixingDate_;
        protected double swapRateValue_;
        protected double discount_;
        protected double annuity_;
        protected double gearing_;
        protected double spread_;
        protected double spreadLegValue_;
        protected double cutoffForCaplet_;
        protected double cutoffForFloorlet_;
        protected Handle<Quote> meanReversion_;
        protected Period swapTenor_;
        protected VanillaOptionPricer vanillaOptionPricer_;
    }


    //===========================================================================//
    //                  NumericHaganPricer                    //
    //===========================================================================//
    //    ! Prices a cms coupon via static replication as in Hagan's
    //        "Conundrums..." article via numerical integration based on
    //        prices of vanilla swaptions
    public class NumericHaganPricer : HaganPricer {
        public double upperLimit_;
        public double stdDeviationsForUpperLimit_;
        public double lowerLimit_;
        public double requiredStdDeviations_;
        public double precision_;
        public double refiningIntegrationTolerance_;

        public NumericHaganPricer(Handle<SwaptionVolatilityStructure> swaptionVol, GFunctionFactory.YieldCurveModel modelOfYieldCurve, Handle<Quote> meanReversion, double lowerLimit, double upperLimit, double precision)
            : base(swaptionVol, modelOfYieldCurve, meanReversion) {
            upperLimit_ = upperLimit;
            lowerLimit_ = lowerLimit;
            requiredStdDeviations_ = 8;
            precision_ = precision;
            refiningIntegrationTolerance_ = 0.0001;
        }

        protected override double optionletPrice(Option.Type optionType, double strike) {
            ConundrumIntegrand integrand = new ConundrumIntegrand(vanillaOptionPricer_, rateCurve_, gFunction_, fixingDate_, paymentDate_, annuity_, swapRateValue_, strike, optionType);
            stdDeviationsForUpperLimit_ = requiredStdDeviations_;
            double a;
            double b;
            double integralValue;
            if (optionType == Option.Type.Call) {
                upperLimit_ = resetUpperLimit(stdDeviationsForUpperLimit_);
                //    while(upperLimit_ <= strike){
                //        stdDeviationsForUpperLimit_ += 1.;
                //        upperLimit_ = resetUpperLimit(stdDeviationsForUpperLimit_);
                //    }
                integralValue = integrate(strike, upperLimit_, integrand);
                //refineIntegration(integralValue, *integrand);
            } else {
                a = Math.Min(strike, lowerLimit_);
                b = strike;
                integralValue = integrate(a, b, integrand);
            }

            double dFdK = integrand.firstDerivativeOfF(strike);
            double swaptionPrice = vanillaOptionPricer_.value(strike, optionType, annuity_);

            // v. HAGAN, Conundrums..., formule 2.17a, 2.18a
            return coupon_.accrualPeriod() * (discount_ / annuity_) * ((1 + dFdK) * swaptionPrice + ((int)optionType) * integralValue);
        }

        public double upperLimit() { return upperLimit_; }
        public double stdDeviations() { return stdDeviationsForUpperLimit_; }

        public double integrate(double a, double b, ConundrumIntegrand integrand) {
            double result = .0;
            //double abserr =.0;
            //double alpha = 1.0;

            //double epsabs = precision_;
            //double epsrel = 1.0; // we are interested only in absolute precision
            //size_t neval =0;

            // we use the non adaptive algorithm only for semi infinite interval
            if (a > 0) {
                // we estimate the actual boudary by testing integrand values
                double upperBoundary = 2 * a;
                while (integrand.value(upperBoundary) > precision_)
                    upperBoundary *= 2.0;
                // sometimes b < a because of a wrong estimation of b based on stdev
                if (b > a)
                    upperBoundary = Math.Min(upperBoundary, b);

                GaussKronrodNonAdaptive gaussKronrodNonAdaptive = new GaussKronrodNonAdaptive(precision_, 1000000, 1.0);
                // if the integration intervall is wide enough we use the
                // following change variable x -> a + (b-a)*(t/(a-b))^3
                if (upperBoundary > 2 * a) {
                    VariableChange variableChange = new VariableChange(integrand.value, a, upperBoundary, 3);
                    result = gaussKronrodNonAdaptive.value(variableChange.value, .0, 1.0);
                } else {
                    result = gaussKronrodNonAdaptive.value(integrand.value, a, upperBoundary);
                }

                // if the expected precision has not been reached we use the old algorithm
                if (!gaussKronrodNonAdaptive.integrationSuccess()) {
                    GaussKronrodAdaptive integrator = new GaussKronrodAdaptive(precision_, 1000000);
                    result = integrator.value(integrand.value, a, b);
                }
            } // if a < b we use the old algorithm
            else {
                GaussKronrodAdaptive integrator = new GaussKronrodAdaptive(precision_, 1000000);
                result = integrator.value(integrand.value, a, b);
            }
            return result;
        }

        public override double swapletPrice() {
            Date today = Settings.evaluationDate();
            if (fixingDate_ <= today) {
                // the fixing is determined
                double Rs = coupon_.swapIndex().fixing(fixingDate_);
                double price = (gearing_ * Rs + spread_) * (coupon_.accrualPeriod() * discount_);
                return price;
            } else {
                double atmCapletPrice = optionletPrice(Option.Type.Call, swapRateValue_);
                double atmFloorletPrice = optionletPrice(Option.Type.Put, swapRateValue_);
                return gearing_ * (coupon_.accrualPeriod() * discount_ * swapRateValue_ + atmCapletPrice - atmFloorletPrice) + spreadLegValue_;
            }
        }

        public double resetUpperLimit(double stdDeviationsForUpperLimit) {
            //return 1.0;
            double variance = swaptionVolatility().link.blackVariance(fixingDate_, swapTenor_, swapRateValue_);
            return swapRateValue_ * Math.Exp(stdDeviationsForUpperLimit * Math.Sqrt(variance));
        }

        public double refineIntegration(double integralValue, ConundrumIntegrand integrand) {
            double percDiff = 1000.0;
            while (Math.Abs(percDiff) < refiningIntegrationTolerance_) {
                stdDeviationsForUpperLimit_ += 1.0;
                double lowerLimit = upperLimit_;
                upperLimit_ = resetUpperLimit(stdDeviationsForUpperLimit_);
                double diff = integrate(lowerLimit, upperLimit_, integrand);
                percDiff = diff / integralValue;
                integralValue += diff;
            }
            return integralValue;
        }

        #region Nested classes
        public class VariableChange {
            private double a_, b_, width_;
            private Func<double, double> f_;
            private int k_;

            public VariableChange(Func<double, double> f, double a, double b, int k) {
                a_ = a;
                b_ = b;
                width_ = b - a;
                f_ = f;
                k_ = k;
            }

            public double value(double x) {
                double newVar;
                double temp = width_;
                for (int i = 1; i < k_; ++i) {
                    temp *= x;
                }
                newVar = a_ + x * temp;
                return f_(newVar) * k_ * temp;
            }
        }

        public class Spy {
            Func<double, double> f_;
            private List<double> abscissas = new List<double>();
            private List<double> functionValues = new List<double>();

            public Spy(Func<double, double> f) {
                f_ = f;
            }
            public double value(double x) {
                abscissas.Add(x);
                double value = f_(x);
                functionValues.Add(value);
                return value;
            }
        }

        //===========================================================================//
        //                              ConundrumIntegrand                           //
        //===========================================================================//
        public class ConundrumIntegrand : IValue {
            public ConundrumIntegrand(VanillaOptionPricer o, YieldTermStructure curve, GFunction gFunction, Date fixingDate, Date paymentDate, double annuity, double forwardValue, double strike, Option.Type optionType) {
                vanillaOptionPricer_ = o;
                forwardValue_ = forwardValue;
                annuity_ = annuity;
                fixingDate_ = fixingDate;
                paymentDate_ = paymentDate;
                strike_ = strike;
                optionType_ = optionType;
                gFunction_ = gFunction;
            }

            public double value(double x) {
                double option = vanillaOptionPricer_.value(x, optionType_, annuity_);
                return option * secondDerivativeOfF(x);
            }

            protected double functionF(double x) {
                double Gx = gFunction_.value(x);
                double GR = gFunction_.value(forwardValue_);
                return (x - strike_) * (Gx / GR - 1.0);
            }

            public double firstDerivativeOfF(double x) {
                double Gx = gFunction_.value(x);
                double GR = gFunction_.value(forwardValue_);
                double G1 = gFunction_.firstDerivative(x);
                return (Gx / GR - 1.0) + G1 / GR * (x - strike_);
            }

            public double secondDerivativeOfF(double x) {
                double GR = gFunction_.value(forwardValue_);
                double G1 = gFunction_.firstDerivative(x);
                double G2 = gFunction_.secondDerivative(x);
                return 2.0 * G1 / GR + (x - strike_) * G2 / GR;
            }

            protected double strike() { return strike_; }
            protected double annuity() { return annuity_; }
            protected Date fixingDate() { return fixingDate_; }
            protected void setStrike(double strike) { strike_ = strike; }

            protected VanillaOptionPricer vanillaOptionPricer_;
            protected double forwardValue_;
            protected double annuity_;
            protected Date fixingDate_;
            protected Date paymentDate_;
            protected double strike_;
            protected Option.Type optionType_;
            protected GFunction gFunction_;
        } 
        #endregion
    }

    //===========================================================================//
    //                          AnalyticHaganPricer                           //
    //===========================================================================//
    public class AnalyticHaganPricer : HaganPricer {
        public AnalyticHaganPricer(Handle<SwaptionVolatilityStructure> swaptionVol, GFunctionFactory.YieldCurveModel modelOfYieldCurve, Handle<Quote> meanReversion)
            : base(swaptionVol, modelOfYieldCurve, meanReversion) {
        }

        //Hagan, 3.5b, 3.5c
        protected override double optionletPrice(Option.Type optionType, double strike) {
            double variance = swaptionVolatility().link.blackVariance(fixingDate_, swapTenor_, swapRateValue_);
            double firstDerivativeOfGAtForwardValue = gFunction_.firstDerivative(swapRateValue_);
            double price = 0;

            double CK = vanillaOptionPricer_.value(strike, optionType, annuity_);
            price += (discount_ / annuity_) * CK;
            double sqrtSigma2T = Math.Sqrt(variance);
            double lnRoverK = Math.Log(swapRateValue_ / strike);
            double d32 = (lnRoverK + 1.5 * variance) / sqrtSigma2T;
            double d12 = (lnRoverK + .5 * variance) / sqrtSigma2T;
            double dminus12 = (lnRoverK - .5 * variance) / sqrtSigma2T;

            CumulativeNormalDistribution cumulativeOfNormal = new CumulativeNormalDistribution();
            double N32 = cumulativeOfNormal.value(((int)optionType) * d32);
            double N12 = cumulativeOfNormal.value(((int)optionType) * d12);
            double Nminus12 = cumulativeOfNormal.value(((int)optionType) * dminus12);

            price += ((int)optionType) * firstDerivativeOfGAtForwardValue * annuity_ * swapRateValue_ * (swapRateValue_ * Math.Exp(variance) * N32 - (swapRateValue_ + strike) * N12 + strike * Nminus12);
            price *= coupon_.accrualPeriod();
            return price;
        }

        //Hagan 3.4c
        public override double swapletPrice() {
            Date today = Settings.evaluationDate();
            if (fixingDate_ <= today) {
                // the fixing is determined
                double Rs = coupon_.swapIndex().fixing(fixingDate_);
                double price = (gearing_ * Rs + spread_) * (coupon_.accrualPeriod() * discount_);
                return price;
            } else {
                double variance = swaptionVolatility().link.blackVariance(fixingDate_, swapTenor_, swapRateValue_);
                double firstDerivativeOfGAtForwardValue = gFunction_.firstDerivative(swapRateValue_);
                double price = 0;
                price += discount_ * swapRateValue_;
                price += firstDerivativeOfGAtForwardValue * annuity_ * swapRateValue_ * swapRateValue_ * (Math.Exp(variance) - 1.0);
                return gearing_ * price * coupon_.accrualPeriod() + spreadLegValue_;
            }
        }
    }
}
