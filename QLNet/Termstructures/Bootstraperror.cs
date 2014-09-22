/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008, 2009 , 2010  Andrea Maggiulli (a.maggiulli@gmail.com)
 Copyright (C) 2014  Edem Dawui (edawui@gmail.com)
  
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

namespace QLNet {
    //! bootstrap error
	public class BootstrapError<T, U> : ISolver1d
		where T : Curve<U>
		where U : TermStructure
	{

        private T curve_;
		  private BootstrapHelper<U> helper_;
        private int segment_;

		  public BootstrapError( T curve, BootstrapHelper<U> helper, int segment )
        {
            curve_ = curve;
            helper_ = helper;
            segment_ = segment; 
        }

        public override double value(double guess) 
		  {
            curve_.updateGuess(curve_.data(), guess, segment_);
            curve_.interpolation_.update();
            return helper_.quoteError();
        }
    }
}
