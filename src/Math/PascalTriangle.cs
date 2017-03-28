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

using System.Collections.Generic;

namespace QLNet
{
   //! Pascal triangle coefficients calculator
   public class PascalTriangle
   {
      //! Get and store one vector of coefficients after another.
      public static List<ulong> get(int order)
      {
         if (coefficients_.empty()) 
         {
            // order zero mandatory for bootstrap
            coefficients_.Add(new InitializedList<ulong>(1, 1));

            coefficients_.Add(new InitializedList<ulong>(2, 1));
            coefficients_.Add(new InitializedList<ulong>(3, 1));
            coefficients_[2][1] = 2;
            coefficients_.Add(new InitializedList<ulong>(4, 1));
            coefficients_[3][1] = coefficients_[3][2] = 3;
        }
        while (coefficients_.Count<=order)
            nextOrder();
        return coefficients_[order];
      }
      
      private PascalTriangle() {}
      private static void nextOrder()
      {
         int order = coefficients_.Count;
         coefficients_.Add(new InitializedList<ulong>(order+1));
         coefficients_[order][0] = coefficients_[order][order] = 1;
         for (int i=1; i<order/2+1; ++i) 
         {
            coefficients_[order][i] = coefficients_[order][order-i] = coefficients_[order-1][i-1] + coefficients_[order-1][i];
         }
      }

      private static List<List<ulong> > coefficients_ = new List<List<ulong>>();
   }
}
