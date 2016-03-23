/*
 Copyright (C) 2009 Philippe Real (ph_real@hotmail.com)
  
 This file is part of QLNet Project https://github.com/amaggiulli/qlnet

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


// ===========================================================================
// NOTE: The following copyright notice applies to the original code,
//
// Copyright (C) 2002 Peter Jäckel "Monte Carlo Methods in Finance".
// All rights reserved.
//
// Permission to use, copy, modify, and distribute this software is freely
// granted, provided that this notice is preserved.
// ===========================================================================

namespace QLNet
{
  //! Prime numbers calculator
    /*! Taken from "Monte Carlo Methods in Finance", by Peter Jäckel
     */
    public class PrimeNumbers {
        //! Get and store one after another.
      
        public static ulong[]  firstPrimes= {
            // the first two primes are mandatory for bootstrapping
            2,  3,
            // optional additional precomputed primes
            5,  7, 11, 13, 17, 19, 23, 29,
            31, 37, 41, 43, 47 };
      
        private static List<ulong> primeNumbers_ = new List<ulong>();

        private PrimeNumbers() { }

        public  static ulong get(int absoluteIndex)
        {        
            if (primeNumbers_.empty()) 
                primeNumbers_.AddRange(firstPrimes);

			while (primeNumbers_.Count<=absoluteIndex)
                nextPrimeNumber();
            
			return primeNumbers_[absoluteIndex];
        }

        private static ulong nextPrimeNumber()
        {
            ulong p, n, m = primeNumbers_.Last(); 
            do {
                // skip the even numbers
                m += 2;
                n = (ulong)Math.Sqrt((double)m);
                // i=1 since the even numbers have already been skipped
                int i = 1;
                do {
                    p = primeNumbers_[i];
                    ++i;
                }
                while ((m % p != 0) && p <= n);
            } while ( p<=n );
            primeNumbers_.Add(m);
            return m;
        }
    }
}
