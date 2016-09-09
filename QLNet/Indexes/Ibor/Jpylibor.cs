/*
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
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

namespace QLNet
{

   //! %JPY %LIBOR rate
   /*! Japanese Yen LIBOR fixed by ICE.

       See <https://www.theice.com/marketdata/reports/170>.

       \warning This is the rate fixed in London by ICE. Use TIBOR if
                you're interested in the Tokio fixing.
   */
   public class JPYLibor : Libor
   {
      public JPYLibor( Period tenor )
         : base( "JPYLibor", tenor, 2, new JPYCurrency(), new Japan(), new Actual360(), new Handle<YieldTermStructure>() )
      {}

      public JPYLibor( Period tenor, Handle<YieldTermStructure> h )
         : base( "JPYLibor", tenor, 2, new JPYCurrency(), new Japan(), new Actual360(), h )
      {}

   }

   //! base class for the one day deposit ICE %JPY %LIBOR indexes
   public class DailyTenorJPYLibor : DailyTenorLibor
   {
      public DailyTenorJPYLibor( int settlementDays ) : this( settlementDays, new Handle<YieldTermStructure>() ) 
      {}

      public DailyTenorJPYLibor( int settlementDays, Handle<YieldTermStructure> h )
         : base( "JPYLibor", settlementDays, new JPYCurrency(), new Japan(), new Actual360(), h ) 
      {}
   }
}
