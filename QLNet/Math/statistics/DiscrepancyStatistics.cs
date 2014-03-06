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


namespace QLNet
{
    //! Statistic tool for sequences with discrepancy calculation
    /*! It inherit from SequenceStatistics<Statistics> and adds
        \f$ L^2 \f$ discrepancy calculation
    */
    public class DiscrepancyStatistics : SequenceStatistics
    {

        private double adiscr_, cdiscr_;
        private double bdiscr_, ddiscr_;


        // typedef SequenceStatistics::value_type value_type;

        // constructor
        public DiscrepancyStatistics(int dimension)
            : base(dimension){}

        public void add<Sequence>(Sequence sample,
                                    double weight){add(sample, weight);}

        public void add<Sequence>(Sequence sample) { add(sample, 1); }

        public override void add(List<double> begin) { add(begin, 1); }

        public override void add(List<double> begin, double weight){
            base.add(begin, weight);
            int k, m, N;
            N = samples();

            double r_ik, r_jk, temp;
            temp = 1.0;

            for (k = 0; k < dimension_; ++k){
                r_ik = begin[k]; //i=N
                temp *= (1.0 - r_ik * r_ik);
            }
            cdiscr_ += temp;

            for (m = 0; m < N - 1; m++){
                temp = 1.0;
                for (k = 0; k < dimension_; ++k){
                    // running i=1..(N-1)
                    //r_ik = stats_[k].data()[m].first;
                    r_ik = 0;
                    // fixed j=N
                    r_jk = begin[k];
                    temp *= (1.0 - Math.Max(r_ik, r_jk));
                }
                adiscr_ += temp;

                temp = 1.0;
                for (k = 0; k < dimension_; ++k){
                    // fixed i=N
                    r_ik = begin[k];
                    // running j=1..(N-1)
                    //r_jk = stats_[k].data()[m].first;
                    r_jk =0;
                    temp *= (1.0 - Math.Max(r_ik, r_jk));
                }
                adiscr_ += temp;
            }
            temp = 1.0;
            for (k = 0; k < dimension_; ++k){
                // fixed i=N, j=N
                r_ik = r_jk = begin[k];
                temp *= (1.0 - Math.Max(r_ik, r_jk));
            }
            adiscr_ += temp;
        }

        void reset() { reset(0); }


        public override void reset(int dimension){
            if (dimension == 0)           // if no size given,
                dimension = dimension_;   // keep the current one
            if (!(dimension != 1))
                throw new ArgumentException("dimension==1 not allowed");

            base.reset(dimension);

            adiscr_ = 0.0;
            //bdiscr_ = 1.0/Math.Pow(2.0, Integer(dimension-1));
            bdiscr_ = 1.0 / Math.Pow(2.0, dimension - 1);
            cdiscr_ = 0.0;
            //ddiscr_ = 1.0/Math.Pow(3.0, Integer(dimension));
            ddiscr_ = 1.0 / Math.Pow(3.0, dimension);
        }

        public double discrepancy()
        {
            int N = samples();
            /*
            Size i;
            Real r_ik, r_jk, cdiscr = adiscr = 0.0, temp = 1.0;

            for (i=0; i<N; i++) {
                Real temp = 1.0;
                for (Size k=0; k<dimension_; k++) {
                    r_ik = stats_[k].sampleData()[i].first;
                    temp *= (1.0 - r_ik*r_ik);
                }
                cdiscr += temp;
            }

            for (i=0; i<N; i++) {
                for (Size j=0; j<N; j++) {
                    Real temp = 1.0;
                    for (Size k=0; k<dimension_; k++) {
                        r_jk = stats_[k].sampleData()[j].first;
                        r_ik = stats_[k].sampleData()[i].first;
                        temp *= (1.0 - std::max(r_ik, r_jk));
                    }
                    adiscr += temp;
                }
            }
            */
            return Math.Sqrt(adiscr_ / (N * N) - bdiscr_ / N * cdiscr_ + ddiscr_);
        }
    }
}