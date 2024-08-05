/*
 Copyright (C) 2024 Konstantin Novitsky (novitk@gmail.com)

 This file is part of QLNet Project https://github.com/amaggiulli/qlnet

 QLNet is free software: you can redistribute it and/or modify it
 under the terms of the QLNet license.  You should have received a
 copy of the license along with this program; if not, license is
 available at <https://github.com/amaggiulli/QLNet/blob/develop/LICENSE>.

 QLNet is a based on QuantLib, a free-software/open-source library
 for financial quantitative analysts and developers - http://quantlib.org/
 The QuantLib license is available online at http://quantlib.org/license.shtml.

 This program is distributed in the hope that it will be useful, but WITHOUT
 ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
 FOR A PARTICULAR PURPOSE.  See the license for more details.
*/

namespace QLNet
{
   /*! CORRA (Canadian Overnight Repo Rate Average) rate fixed by the RBA.
   See <https://www.isda.org/2023/10/16/overview-of-the-canadian-derivatives-market/>.
   */
   public class Corra : OvernightIndex
   {
      public Corra(Handle<YieldTermStructure> h = null)
         : base("Corra", 0, new CADCurrency(), new Canada(), new Actual365Fixed(),
            h ?? new Handle<YieldTermStructure>())
      { }
   }

}
