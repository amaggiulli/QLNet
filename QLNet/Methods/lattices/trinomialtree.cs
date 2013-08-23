/*
 Copyright (C) 2010 Philippe Real (ph_real@hotmail.com)
  
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
    public class TrinomialTree : Tree<TrinomialTree>
    {
        public enum Branches { branches = 3 };
        private  List<Branching> branchings_;
        protected double x0_;
        protected List<double> dx_;
        protected TimeGrid timeGrid_;

        public TrinomialTree(   StochasticProcess1D process,
                                TimeGrid timeGrid)
        : this( process,timeGrid,false){}
                              
        public TrinomialTree(   StochasticProcess1D process,
                                TimeGrid timeGrid,
                                bool isPositive /*= false*/)
            : base(timeGrid.size())
        {
            branchings_ = new List<Branching>();
            dx_ = new InitializedList<double>(1);
            timeGrid_=timeGrid;
            x0_ = process.x0();

            int nTimeSteps = timeGrid.size() - 1;
            int jMin = 0;
            int jMax = 0;

            for (int i=0; i<nTimeSteps; i++) {
                double t = timeGrid[i];
                double dt = timeGrid.dt(i);

                //Variance must be independent of x
                double v2 = process.variance(t, 0.0, dt);
                double v = Math.Sqrt(v2);
                dx_.Add(v*Math.Sqrt(3.0));

                Branching branching = new Branching();
                for (int j=jMin; j<=jMax; j++) {
                    double x = x0_ + j*dx_[i];
                    double m = process.expectation(t, x, dt);
                    int temp = (int)(Math.Floor((m-x0_)/dx_[i+1] + 0.5));

                    if (isPositive) {
                        while (x0_+(temp-1)*dx_[i+1]<=0) {
                            temp++;
                        }
                    }

                    double e = m - (x0_ + temp*dx_[i+1]);
                    double e2 = e*e;
                    double e3 = e*Math.Sqrt(3.0);

                    double p1 = (1.0 + e2/v2 - e3/v)/6.0;
                    double p2 = (2.0 - e2/v2)/3.0;
                    double p3 = (1.0 + e2/v2 + e3/v)/6.0;

                    branching.add(temp, p1, p2, p3);
                }
                branchings_.Add(branching);

                jMin = branching.jMin();
                jMax = branching.jMax();
            }
        }

        public double dx(int i) { return dx_[i]; }
        
        public TimeGrid timeGrid() { return timeGrid_; }

        public int size(int i) { return i == 0 ? 1 : branchings_[i - 1].size(); }

        public  double underlying(int i, int index) {
            if (i == 0)
                return x0_;
            else
                return x0_ + (branchings_[i - 1].jMin() +
                              (double)index) * dx(i);
        }
        
        public int descendant(int i, int index, int branch){
            return branchings_[i].descendant(index, branch);
        } 
        
        public double probability(int i, int index, int branch){
            return branchings_[i].probability(index, branch);
        }

        /* Branching scheme for a trinomial node.  Each node has three
           descendants, with the middle branch linked to the node
           which is closest to the expectation of the variable. */
        private class Branching {
            
            private List<int> k_;
            private List<List<double>> probs_;
            private int kMin_, jMin_, kMax_, jMax_;

            public Branching()
            {
                k_ = new List<int>();
                probs_ = new InitializedList<List<double>>(3);
                kMin_ = int.MaxValue  ;
                jMin_ = int.MaxValue ;
                kMax_ = int.MinValue;
                jMax_ = int.MinValue ;
            }
            public int descendant(int index, int branch) {
                return k_[index] - jMin_ - 1 + branch;
            }
            
            public double probability(int index, int branch) {
                return probs_[branch][index];
            }
            
            public int size() {
                return jMax_ - jMin_ + 1;
            }
            
            public int jMin() {
                return jMin_;
            } 
            
            public int jMax() {
                return jMax_;
            }
            
            public void add(int k, double p1, double p2, double p3) {
                // store
                k_.Add(k);
                probs_[0].Add(p1);
                probs_[1].Add(p2);
                probs_[2].Add(p3);
                // maintain invariants
                kMin_ = Math.Min(kMin_, k);
                jMin_ = kMin_ - 1;
                kMax_ = Math.Max(kMax_, k);
                jMax_ = kMax_ + 1;
            }
        }
    }
}
