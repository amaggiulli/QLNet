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

   //! %CAD LIBOR rate
   /*! Canadian Dollar LIBOR discontinued as of 2013.
       \warning This is the rate fixed in London by BBA. Use CDOR if
                you're interested in the Canadian fixing by IDA.
   */
   public class CADLibor : Libor
   {
      public CADLibor( Period tenor )
         : base( "CADLibor", tenor, 2, new CADCurrency(), new Canada(), new Actual360(), new Handle<YieldTermStructure>() )
      {}

      public CADLibor( Period tenor, Handle<YieldTermStructure> h )
         : base( "CADLibor", tenor, 2, new CADCurrency(), new Canada(), new Actual360(), h )
      {}
   }

   //! Overnight %CAD %Libor index
   public class CADLiborON : DailyTenorLibor
   {
      public CADLiborON()
         : base( "CADLibor", 0, new CADCurrency(), new Canada(), new Actual360(), new Handle<YieldTermStructure>() )
      {}

      public CADLiborON( Handle<YieldTermStructure> h )
         : base( "CADLibor", 0, new CADCurrency(), new Canada(), new Actual360(), h )
      {}
   }

}