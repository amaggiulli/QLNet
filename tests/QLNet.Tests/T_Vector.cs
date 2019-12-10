//  Copyright (C) 2008-2017 Andrea Maggiulli (a.maggiulli@gmail.com)
//
//  This file is part of QLNet Project https://github.com/amaggiulli/qlnet
//  QLNet is free software: you can redistribute it and/or modify it
//  under the terms of the QLNet license.  You should have received a
//  copy of the license along with this program; if not, license is
//  available at <https://github.com/amaggiulli/QLNet/blob/develop/LICENSE>.
//
//  QLNet is a based on QuantLib, a free-software/open-source library
//  for financial quantitative analysts and developers - http://quantlib.org/
//  The QuantLib license is available online at http://quantlib.org/license.shtml.
//
//  This program is distributed in the hope that it will be useful, but WITHOUT
//  ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
//  FOR A PARTICULAR PURPOSE.  See the license for more details.
using System;
using System.Collections.Generic;
#if NET452
using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
using Xunit;
#endif
using QLNet;

namespace TestSuite
{
#if NET452
   [TestClass()]
#endif
   public class T_Vector
   {
      /// <summary>
      /// Sample values.
      /// </summary>
      protected readonly List<double> Data = new List<double>() { 1, 2, 3, 4, 5 };

      /// <summary>
      /// Test vector clone
      /// </summary>
#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testClone()
      {
         Vector vector = new Vector(Data);
         Vector clone = vector.Clone();

         QAssert.AreNotSame(vector, clone);
         QAssert.AreEqual(vector.Count, clone.Count);
         QAssert.CollectionAreEqual(vector, clone);
         vector[0] = 100;
         QAssert.CollectionAreNotEqual(vector, clone);

      }

      /// <summary>
      /// Test clone a vector using <c>IClonable</c> interface method.
      /// </summary>
#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testCloneICloneable()
      {
         Vector vector = new Vector(Data);
         Vector clone = (Vector)((QLNet.ICloneable)vector).Clone();

         QAssert.AreNotSame(vector, clone);
         QAssert.AreEqual(vector.Count, clone.Count);
         QAssert.CollectionAreEqual(vector, clone);
         vector[0] = 100;
         QAssert.CollectionAreNotEqual(vector, clone);
      }

      /// <summary>
      /// Test vectors equality.
      /// </summary>
#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testEquals()
      {
         Vector vector1 = new Vector(Data);
         Vector vector2 = new Vector(Data);
         Vector vector3 = new Vector(4);
         QAssert.IsTrue(vector1.Equals(vector1));
         QAssert.IsTrue(vector1.Equals(vector2));
         QAssert.IsFalse(vector1.Equals(vector3));
         QAssert.IsFalse(vector1.Equals(null));
         QAssert.IsFalse(vector1.Equals(2));
      }

      /// <summary>
      /// Test Vector hash code.
      /// </summary>
#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testHashCode()
      {
         Vector vector = new Vector(Data);
         QAssert.AreEqual(vector.GetHashCode(), vector.GetHashCode());
         QAssert.AreEqual(vector.GetHashCode(),
         new Vector(new List<double>() { 1, 2, 3, 4, 5  }).GetHashCode());
         QAssert.AreNotEqual(vector.GetHashCode(), new Vector(new List<double>() { 1 }).GetHashCode());
      }
   }
}
