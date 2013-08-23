/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
  
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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QLNet;

namespace TestSuite
{
   [TestClass()]
   public class T_RNGTraits
   {
      [TestMethod()]
      public void testGaussian()
      {
         //("Testing Gaussian pseudo-random number generation...");

         var rsg = (InverseCumulativeRsg<RandomSequenceGenerator<MersenneTwisterUniformRng>, InverseCumulativeNormal>)
             new PseudoRandom().make_sequence_generator(100, 1234);

         List<double> values = rsg.nextSequence().value;
         double sum = 0.0;
         for (int i = 0; i < values.Count; i++)
            sum += values[i];

         double stored = 4.09916;
         double tolerance = 1.0e-5;
         if (Math.Abs(sum - stored) > tolerance)
            Assert.Fail("the sum of the samples does not match the stored value\n"
                       + "    calculated: " + sum + "\n"
                       + "    expected:   " + stored);
      }

      [TestMethod()]
      public void testDefaultPoisson()
      {

         //("Testing Poisson pseudo-random number generation...");

         PoissonPseudoRandom.icInstance = new InverseCumulativePoisson();
         IRNG rsg = (IRNG)new PoissonPseudoRandom().make_sequence_generator(100, 1234);

         List<double> values = rsg.nextSequence().value;
         double sum = 0.0;
         for (int i = 0; i < values.Count; i++)
            sum += values[i];

         double stored = 108.0;
         if (!Utils.close(sum, stored))
            Assert.Fail("the sum of the samples does not match the stored value\n"
                       + "    calculated: " + sum + "\n"
                       + "    expected:   " + stored);
      }

      [TestMethod()]
      public void testCustomPoisson()
      {

         //("Testing custom Poisson pseudo-random number generation...");

         PoissonPseudoRandom.icInstance = new InverseCumulativePoisson(4.0);
         IRNG rsg = (IRNG)new PoissonPseudoRandom().make_sequence_generator(100, 1234);

         List<double> values = rsg.nextSequence().value;
         double sum = 0.0;
         for (int i = 0; i < values.Count; i++)
            sum += values[i];

         double stored = 409.0;
         if (!Utils.close(sum, stored))
            Assert.Fail("the sum of the samples does not match the stored value\n"
                       + "    calculated: " + sum + "\n"
                       + "    expected:   " + stored);
      }
   }
}
