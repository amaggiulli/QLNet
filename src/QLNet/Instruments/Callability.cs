﻿/*
 Copyright (C) 2008, 2009 , 2010, 2011, 2012  Andrea Maggiulli (a.maggiulli@gmail.com)

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
using System.Collections.Generic;

namespace QLNet
{
   //! %instrument callability
   public class Callability : Event
   {

      //! type of the callability
      public enum Type { Call, Put }

      public Callability(Bond.Price price, Type type, Date date)
      {
         price_ = price;
         type_ = type;
         date_ = date;
      }
      public Bond.Price price()
      {
         Utils.QL_REQUIRE(price_ != null, () => "no price given");
         return price_;
      }
      public Type type() { return type_; }
      // Event interface
      public override Date date() { return date_; }

      private Bond.Price price_;
      private Type type_;
      private Date date_;

   }

   public class CallabilitySchedule : List<Callability> {}


}
