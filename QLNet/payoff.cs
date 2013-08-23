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
	//! Abstract base class for option payoffs
	public class Payoff {
        //! \name Payoff interface
        //@{
        /*! \warning This method is used for output and comparison between
                payoffs. It is <b>not</b> meant to be used for writing
                switch-on-type code.
        */
        public virtual string name() { throw new NotImplementedException(); }
        public virtual string description() { throw new NotImplementedException(); }
        public virtual double value(double price) { throw new NotImplementedException(); }

        public virtual void accept(IAcyclicVisitor v) {
            if (v != null)
                v.visit(this);
            else
                throw new ApplicationException("not an event visitor");
        }
	}
}
