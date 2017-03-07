﻿/*
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
   //! Inverse cumulative random number generator
   /*! It uses a uniform deviate in (0, 1) as the source of cumulative
       distribution values.
       Then an inverse cumulative distribution is used to calculate
       the distribution deviate.

       The uniform deviate is supplied by RNG.
       The inverse cumulative distribution is supplied by IC.
   */

   public class InverseCumulativeRng<RNG, IC> where RNG : IRNGTraits where IC : IValue, new()
   {
      private RNG uniformGenerator_;
      private IC ICND_ = FastActivator<IC>.Create();

      public InverseCumulativeRng(RNG uniformGenerator)
      {
         uniformGenerator_ = uniformGenerator;
      }

      //! returns a sample from a Gaussian distribution
      public Sample<double> next()
      {
         Sample<double> sample = uniformGenerator_.next();
         return new Sample<double>(ICND_.value(sample.value), sample.weight);
      }
   }
}
