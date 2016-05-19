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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QLNet
{
   // Interface class to map the functionality of SobolBrownianGenerator
   // to the "conventional" sequence generator interface
   public class SobolBrownianBridgeRsg : IRNG
   {
      //typedef Sample<std::vector<Real> > sample_type;

      public SobolBrownianBridgeRsg(int factors, int steps,
                                    SobolBrownianGenerator.Ordering ordering = SobolBrownianGenerator.Ordering.Diagonal,
                                    ulong seed = 0,
                                    SobolRsg.DirectionIntegers directionIntegers = SobolRsg.DirectionIntegers.JoeKuoD7)
      {
         factors_ = factors;
         steps_ = steps;
         dim_ = factors*steps;
         seq_ = new Sample<List<double>>(new InitializedList<double>(factors*steps), 1.0) ;
         gen_ = new SobolBrownianGenerator(factors, steps, ordering, seed, directionIntegers);
      }

      public Sample<List<double>> nextSequence()
      {
         gen_.nextPath();
         List<double> output = new InitializedList<double>(factors_);
         for (int i=0; i < steps_; ++i) 
         {
            gen_.nextStep(output);
            for ( int j = 0; j < output.Count ; j++)
            {
               seq_.value[j + i*factors_] = output[j]; // std::copy(output.begin(), output.end(),seq_.value.begin()+i*factors_);
            }
        }

        return seq_;
      }
      public Sample<List<double>> lastSequence() {return seq_;}
      public IRNG factory(int dimensionality, ulong seed)
      {
         throw new NotImplementedException();
      }

      public int dimension() {return dim_;}

      private int factors_, steps_, dim_;
      private Sample<List<double>> seq_;
      private SobolBrownianGenerator gen_;
   }
}
