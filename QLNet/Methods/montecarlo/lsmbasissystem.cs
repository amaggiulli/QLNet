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
    public static class LsmBasisSystem {
        public enum PolynomType {
            Monomial, Laguerre, Hermite, Hyperbolic,
            Legendre, Chebyshev, Chebyshev2th
        };

        public static List<Func<double, double>> pathBasisSystem(int order, PolynomType polynomType) {
            List<Func<double, double>> ret = new List<Func<double, double>>();
            for (int i=0; i<=order; ++i) {
                switch (polynomType) {
                    case PolynomType.Monomial:
                        ret.Add(new MonomialFct(i).value);
                        break;
                    case PolynomType.Laguerre:
                        ret.Add((x) => new GaussLaguerrePolynomial().weightedValue(i, x));
                        break;
                    case PolynomType.Hermite:
                        ret.Add((x) => new GaussHermitePolynomial().weightedValue(i, x));
                        break;
                    case PolynomType.Hyperbolic:
                        ret.Add((x) => new GaussHyperbolicPolynomial().weightedValue(i, x));
                        break;
                    case PolynomType.Legendre:
                        ret.Add((x) => new GaussLegendrePolynomial().weightedValue(i, x));
                        break;
                    case PolynomType.Chebyshev:
                        ret.Add((x) => new GaussChebyshevPolynomial().weightedValue(i, x));
                        break;
                    case PolynomType.Chebyshev2th:
                        ret.Add((x) => new GaussChebyshev2ndPolynomial().weightedValue(i, x));
                        break;
                    default:
                        throw new ApplicationException("unknown regression type");
                }
            }
            return ret;
        }


        public static List<Func<Vector, double>> multiPathBasisSystem(int dim, int order, PolynomType polynomType) {

            List<Func<double, double>> b = pathBasisSystem(order, polynomType);

            List<Func<Vector, double>> ret = new List<Func<Vector,double>>();
            ret.Add((xx) => 1.0);

            for (int i=1; i<=order; ++i) {
                List<Func<Vector, double>> a = w(dim, i, polynomType, b);

                foreach (var iter in a) {
                    ret.Add(iter);
                }
            }

            // remove-o-zap: now remove redundant functions.
            // usually we do have a lot of them due to the construction schema.
            // We use a more "hands on" method here.
            List<bool> rm = new InitializedList<bool>(ret.Count, true);

            Vector x = new Vector(dim), v = new Vector(ret.Count);
            MersenneTwisterUniformRng rng = new MersenneTwisterUniformRng(1234UL);

            for (int i=0; i<10; ++i) {
                int k;

                // calculate random x vector
                for (k=0; k<dim; ++k) {
                    x[k] = rng.next().value;
                }

                // get return values for all basis functions
                for (k = 0; k < ret.Count; ++k) {
                    v[k] = ret[k](x);
                }

                // find duplicates
                for (k = 0; k < ret.Count; ++k) {
                    if (v.First(xx => (Math.Abs(v[k] - xx) <= 10*v[k]*Const.QL_Epsilon)) == v.First() + k) {
                        // don't remove this item, it's unique!
                        rm[k] = false;
                    }
                }
            }

            int iter2 = 0;
            for (int i = 0; i < rm.Count; ++i) {
                if (rm[i]) {
                    ret.RemoveAt(iter2);
                }
                else {
                    ++iter2;
                }
            }

            return ret;
        }

        private static List<Func<Vector, double>> w(int dim, int order, PolynomType polynomType, List<Func<double, double>> b) {

           List<Func<Vector, double>> ret = new List<Func<Vector,double>>();

           for (int i=order; i>=1; --i) {
               List<Func<Vector, double>> left = w(dim, order-i, polynomType, b);

               for (int j=0; j<dim; ++j) {
                   Func<Vector, double> a = (xx => b[i](xx[j]));

                   if (i == order)
                       ret.Add(a);
                   else // add linear combinations
                       for (j=0; j<left.Count; ++j)
                           ret.Add( xx => a(xx * left[j](xx)));
               }
           }
           return ret;
        }
    }


    public class MonomialFct : IValue {
        private int order_;

        public MonomialFct(int order) {
            order_ = order;
        }

        public double value(double x) {
            double ret = 1.0;
            for (int i = 0; i < order_; ++i) {
                ret *= x;
            }
            return ret;
        }
    }
}
