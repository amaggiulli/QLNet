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
using System.Text;
using QLNet;

namespace QLNet
{
    // simple quote class
    //! market element returning a stored value
    public class SimpleQuote : Quote
    {
        private double? value_;

        public SimpleQuote() { }
        public SimpleQuote(double? value) { value_ = value; }

        //! Quote interface
        public override double value()
        {
            if (!isValid()) throw new ArgumentException("invalid SimpleQuote");
            return value_.GetValueOrDefault();
        }
        public override bool isValid() { return value_ != null; }

        //! returns the difference between the new value and the old value
        public double setValue(double? value)
        {
            double? diff = value - value_;
            if (diff != 0)
            {
                value_ = value;
                notifyObservers();
            }
            return diff.GetValueOrDefault();
        }

        public void reset()
        {
            setValue(null);
        }
    }
}