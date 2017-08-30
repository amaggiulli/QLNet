//  Copyright (C) 2017 Jean-Camille Tournier (jean-camille.tournier@avivainvestors.com)
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

using System;

namespace QLNet
{ 
   // code taken from MSDN documentation : https://msdn.microsoft.com/en-us/library/ff650316.aspx
   public class Singleton<T> where T : new()
   {
      [ThreadStatic]
      private static Singleton<T> instance_;
      private static T link_;

      protected Singleton() { }

      public static Singleton<T> instance
      {
         get
         {
            if (instance_ == null)
            {
               instance_ = new Singleton<T>();
            }
            return instance_;
         }
      }

      public static T link
      {
         get
         {
            if (link_ == null)
            {
               link_ = new T();
            }
            return link_;
         }
      }
   }
}
