/*
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
  
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

   //! %SEK %LIBOR rate
   /*! Sweden Krone LIBOR discontinued as of 2013.
   */ 
   public class SEKLibor : Libor
   {
      public SEKLibor( Period tenor )
         : base( "SEKLibor", tenor, 2, new SEKCurrency(), new Sweden(), new Actual360(), new Handle<YieldTermStructure>() )
      {}

      public SEKLibor( Period tenor, Handle<YieldTermStructure> h )
         : base( "SEKLibor", tenor, 2, new SEKCurrency(), new Sweden(), new Actual360(), h )
      {}

   }

}
