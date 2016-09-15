/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
  
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

namespace QLNet {
    //! Simple stock class
    public class Stock : Instrument {
        private Handle<Quote> quote_;

        public Stock(Handle<Quote> quote) {
            quote_ = quote;
            quote_.registerWith(update);
        }

        public override bool isExpired() { return false; }

        protected override void performCalculations() {
            if (quote_.empty())
                throw new Exception("null quote set");
            NPV_ = quote_.link.value();
        }
    }
}
