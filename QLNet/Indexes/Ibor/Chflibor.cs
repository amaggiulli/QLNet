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

    //! %CHF %LIBOR rate
    //    ! Swiss Franc LIBOR fixed by BBA.
    //
    //        See <http://www.bba.org.uk/bba/jsp/polopoly.jsp?d=225&a=1414>.
    //
    //        \warning This is the rate fixed in London by BBA. Use ZIBOR if
    //                 you're interested in the Zurich fixing.
    //    
    public class CHFLibor : Libor {
        public CHFLibor(Period tenor)
            : base("CHFLibor", tenor, 2, new CHFCurrency(), new Switzerland(), new Actual360(), new Handle<YieldTermStructure>()) {
        }

        public CHFLibor(Period tenor, Handle<YieldTermStructure> h)
            : base("CHFLibor", tenor, 2, new CHFCurrency(), new Switzerland(), new Actual360(), h) {
        }
    }

    //! base class for the one day deposit BBA %CHF %LIBOR indexes
    public class DailyTenorCHFLibor : DailyTenorLibor {
        public DailyTenorCHFLibor(int settlementDays, Handle<YieldTermStructure> h)
            : base("CHFLibor", settlementDays, new CHFCurrency(), new Switzerland(), new Actual360(), h) {}
    };
}
