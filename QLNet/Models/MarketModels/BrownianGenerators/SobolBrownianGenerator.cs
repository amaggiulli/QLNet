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
using System.Linq;

namespace QLNet
{
   //! Sobol Brownian generator for market-model simulations
   /*! Incremental Brownian generator using a Sobol generator,
       inverse-cumulative Gaussian method, and Brownian bridging.
   */
   public class SobolBrownianGenerator : IBrownianGenerator
   {
      public enum Ordering {
         Factors,  /*!< The variates with the best quality will be
                        used for the evolution of the first factor. */
         Steps,    /*!< The variates with the best quality will be
                        used for the largest steps of all factors. */
         Diagonal  /*!< A diagonal schema will be used to assign
                        the variates with the best quality to the
                        most important factors and the largest
                        steps. */
      }
      public SobolBrownianGenerator( int factors, int steps,Ordering ordering,ulong seed = 0,
                                     SobolRsg.DirectionIntegers directionIntegers = SobolRsg.DirectionIntegers.Jaeckel)
      {
         factors_ = factors;
         steps_ = steps;
         ordering_ = ordering;
         generator_ = new InverseCumulativeRsg<SobolRsg, InverseCumulativeNormal>(
            new SobolRsg(factors*steps, seed, directionIntegers),new InverseCumulativeNormal());
         bridge_ = new BrownianBridge(steps);
         lastStep_ = 0;
         orderedIndices_ = new InitializedList<List<int>>(factors);
         bridgedVariates_ = new InitializedList<List<double>>(factors);
         for ( int i = 0 ; i < factors ; i++)
         {
            orderedIndices_[i] = new InitializedList<int>(steps);
            bridgedVariates_[i] = new InitializedList<double>( steps );
         }

         switch (ordering_)
         {
            case Ordering.Factors:
               fillByFactor(orderedIndices_, factors_, steps_);
               break;
            case Ordering.Steps:
               fillByStep(orderedIndices_, factors_, steps_);
               break;
            case Ordering.Diagonal:
               fillByDiagonal(orderedIndices_, factors_, steps_);
               break;
            default:
               Utils.QL_FAIL("unknown ordering");
               break;
         }
      }

      public double nextPath()
      {
         Sample<List<double>> sample = generator_.nextSequence();
         // Brownian-bridge the variates according to the ordered indices
         for (int i=0; i<factors_; ++i) 
         {
            List< double>  permList = new List<double>();
            foreach ( var index in orderedIndices_[i] )
            {
               permList.Add( sample.value[index]);
            }

            bridge_.transform( permList, bridgedVariates_[i] ); // TODO Check
            //bridge_.transform( sample.value, bridgedVariates_[i] ); // TODO Check
         //   bridge_.transform(boost::make_permutation_iterator(sample.value.begin(),orderedIndices_[i].begin()),
         //                     boost::make_permutation_iterator(sample.value.begin(),orderedIndices_[i].end()),
         //                     bridgedVariates_[i].begin());
         }
         lastStep_ = 0;
         return sample.weight;
      }
      public double nextStep(List<double> output)
      {
         #if QL_EXTRA_SAFETY_CHECKS
            Utils.QL_REQUIRE(output.Count == factors_,()=> "size mismatch");
            Utils.QL_REQUIRE(lastStep_<steps_,()=> "sequence exhausted");
         #endif
         for (int i=0; i<factors_; ++i)
            output[i] = bridgedVariates_[i][lastStep_];
         ++lastStep_;
         return 1.0;
      }

      public int numberOfFactors() { return factors_; }
      public int numberOfSteps() { return steps_; }
        
      // test interface
      public List<List<int> > orderedIndices() {return orderedIndices_;}
      public List<List<double> > transform(List<List<double> > variates)
      {
         Utils.QL_REQUIRE(   (variates.Count == factors_*steps_),()=> "inconsistent variate vector");

         int dim    = factors_*steps_;
         int nPaths = variates.First().Count;
        
         List<List<double>> retVal = new InitializedList<List<double>>(factors_,new InitializedList<double>(nPaths*steps_));
        
         for (int j=0; j < nPaths; ++j) 
         {
            List<double> sample = new InitializedList<double>(steps_*factors_);
            for (int k=0; k < dim; ++k) 
            {
               sample[k] = variates[k][j];
            }
            for (int i=0; i<factors_; ++i)
            {
               List<double> permList = new List<double>();
               foreach ( var index in orderedIndices_[i] )
               {
                  permList.Add( sample[index] );
               }
               List<double> temp = retVal[i].GetRange( j * steps_, retVal[i].Count - ( j * steps_ ) );

               bridge_.transform( permList, temp ); // TODO Check
               //bridge_.transform( sample, retVal[i + j * steps_] ); // TODO Check
               //bridge_.transform(boost::make_permutation_iterator(sample.begin(),orderedIndices_[i].begin()),
               //                  boost::make_permutation_iterator(sample.begin(),orderedIndices_[i].end()),
               //                  retVal[i].begin()+j*steps_);
            }
         }
         return retVal;
      }

      private void fillByFactor(List<List<int> > M,int factors, int steps) 
      {
         int counter = 0;
         for (int i=0; i<factors; ++i)
            for (int j=0; j<steps; ++j)
               M[i][j] = counter++;
      }

      private void fillByStep(List<List<int> > M,int factors, int steps) 
      {
         int counter = 0;
         for ( int j = 0; j < steps; ++j )
            for ( int i = 0; i < factors; ++i )
               M[i][j] = counter++;
      }

      // variate 2 is used for the second factor's full path
      private void fillByDiagonal(List<List<int> > M,int factors, int steps) 
      {
         // starting position of the current diagonal
         int i0 = 0, j0 = 0;
         // current position
         int i = 0, j = 0;
         int counter = 0;
         while (counter < factors*steps) 
         {
            M[i][j] = counter++;
            if (i == 0 || j == steps-1) 
            {
               // we completed a diagonal and have to start a new one
               if (i0 < factors-1) 
               {
                  // we start the path of the next factor
                  i0 = i0+1;
                  j0 = 0;
               } 
               else 
               {
                  // we move along the path of the last factor
                  i0 = factors-1;
                  j0 = j0+1;
               }
               i = i0;
               j = j0;
            } 
            else 
            {
               // we move along the diagonal
               i = i-1;
               j = j+1;
            }
         }
      }
      
      private int factors_, steps_;
      private Ordering ordering_;
      private InverseCumulativeRsg<SobolRsg,InverseCumulativeNormal> generator_;
      private BrownianBridge bridge_;
      // work variables
      private int lastStep_;
      private List<List<int> > orderedIndices_;
      private List<List<double> > bridgedVariates_;
   }

   public class SobolBrownianGeneratorFactory : IBrownianGeneratorFactory 
   {
      public SobolBrownianGeneratorFactory( SobolBrownianGenerator.Ordering ordering,ulong seed = 0,
         SobolRsg.DirectionIntegers integers = SobolRsg.DirectionIntegers.Jaeckel)
      {
         ordering_ = ordering;
         seed_ = seed;
         integers_ = integers;
      }
      
      public IBrownianGenerator create(int factors, int steps)
      {
         return new SobolBrownianGenerator(factors, steps, ordering_, seed_, integers_);
      }
      
      private SobolBrownianGenerator.Ordering ordering_;
      private ulong seed_;
      private SobolRsg.DirectionIntegers integers_;
    };
}
