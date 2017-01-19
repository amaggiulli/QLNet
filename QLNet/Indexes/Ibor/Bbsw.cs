//  Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)
//  
//  This file is part of QLNet Project https://github.com/amaggiulli/qlnet
//  QLNet is free software: you can redistribute it and/or modify it
//  under the terms of the QLNet license.  You should have received a
//  copy of the license along with this program; if not, license is  
//  available online at <http://qlnet.sourceforge.net/License.html>.
//   
//  QLNet is a based on QuantLib, a free-software/open-source library
//  for financial quantitative analysts and developers - http://quantlib.org/
//  The QuantLib license is available online at http://quantlib.org/license.shtml.
//  
//  This program is distributed in the hope that it will be useful, but WITHOUT
//  ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
//  FOR A PARTICULAR PURPOSE.  See the license for more details.

namespace QLNet
{
   //! %Bbsw index
   /*! Bbsw rate fixed by AFMA.

       See <http://www.afma.com.au/data/BBSW>.
   */
   public class Bbsw : IborIndex
   {
      public Bbsw( Period tenor, Handle<YieldTermStructure> h = null)
        : base("Bbsw", tenor,
                    0, // settlement days
                    new AUDCurrency(), new Australia(),
                    BusinessDayConvention.HalfMonthModifiedFollowing, true,
                    new Actual365Fixed(), h ?? new Handle<YieldTermStructure>()) 
      {
         Utils.QL_REQUIRE(this.tenor().units() != TimeUnit.Days,()=>
            "for daily tenors (" + this.tenor() + ") dedicated DailyTenor constructor must be used");
      }
   }

   //! 1-month %Bbsw index
   public class Bbsw1M : Bbsw 
   {
      public Bbsw1M( Handle<YieldTermStructure> h = null)
        : base(new Period(1, TimeUnit.Months), h ?? new Handle<YieldTermStructure>()) 
      {}
   }

   //! 2-month %Bbsw index
   public class Bbsw2M : Bbsw
   {
      public Bbsw2M( Handle<YieldTermStructure> h = null )
         : base( new Period( 2, TimeUnit.Months ), h ?? new Handle<YieldTermStructure>() )
      { }
   }

   //! 3-month %Bbsw index
   public class Bbsw3M : Bbsw
   {
      public Bbsw3M( Handle<YieldTermStructure> h = null )
         : base( new Period( 3, TimeUnit.Months ), h ?? new Handle<YieldTermStructure>() )
      { }
   }

   //! 4-month %Bbsw index
   public class Bbsw4M : Bbsw
   {
      public Bbsw4M( Handle<YieldTermStructure> h = null )
         : base( new Period( 4, TimeUnit.Months ), h ?? new Handle<YieldTermStructure>() )
      { }
   }

   //! 5-month %Bbsw index
   public class Bbsw5M : Bbsw
   {
      public Bbsw5M( Handle<YieldTermStructure> h = null )
         : base( new Period( 5, TimeUnit.Months ), h ?? new Handle<YieldTermStructure>() )
      { }
   }

   //! 6-month %Bbsw index
   public class Bbsw6M : Bbsw
   {
      public Bbsw6M( Handle<YieldTermStructure> h = null )
         : base( new Period( 6, TimeUnit.Months ), h ?? new Handle<YieldTermStructure>() )
      { }
   }
}
