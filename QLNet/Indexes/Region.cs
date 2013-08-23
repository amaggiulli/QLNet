/*
 Copyright (C) 2008, 2009 , 2010  Andrea Maggiulli (a.maggiulli@gmail.com)
 * 
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

namespace QLNet
{
   //! Region class, used for inflation applicability.
   public class Region
   {

      //! \name Inspectors
      //@{
      public string name() 
      {
         return data_.name;
      }
      public string code()
      {
         return data_.code;
      }
      //@}
      
      protected  Region() {}
      protected struct Data
      {
        public string name;
        public string code;
        public Data(string Name, string Code)
        {
           name = Name;
           code = Code;
        }
      }
      protected Data data_;

      public static bool operator ==(Region r1, Region r2) 
      {
        return r1.name() == r2.name();
    
      }

      public static bool operator !=(Region r1, Region r2)
      {
         return !(r1.name() == r2.name());
      }

   }

   //! Australia as geographical/economic region
   public class AustraliaRegion : Region 
   {
      public AustraliaRegion()
      {
        Data AUdata = new Data("Australia","AU");
        data_ = AUdata;
      }
    
   }

   //! European Union as geographical/economic region
   public class EURegion : Region 
   {
      public EURegion()
      {
          Data EUdata = new Data("EU","EU");
          data_ = EUdata;
      }
   }

   //! France as geographical/economic region
   public class FranceRegion : Region 
   {
      public FranceRegion()
      {
         Data FRdata = new Data("France","FR");
         data_ = FRdata;
      }
   }

    
   //! United Kingdom as geographical/economic region
   public class UKRegion : Region 
   {
      public UKRegion()
      {
         Data UKdata = new Data("UK","UK");
         data_ = UKdata;
      }
   }

   //! USA as geographical/economic region
   public class USRegion : Region 
   {
      public USRegion()
      {
         Data UKdata = new Data("USA","US");
         data_ = UKdata;
      }
   }


}
