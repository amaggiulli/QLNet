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
   //! Monte Carlo Heston-model engine for European options
   /*! \ingroup vanillaengines

       \test the correctness of the returned value is tested by
             reproducing results available in web/literature
   */
   public class MCEuropeanHestonEngine<RNG, S> : MCVanillaEngine<MultiVariate, RNG, S>
      where RNG : IRSG, new()
      where S : IGeneralStatistics, new()
   {
      // typedef typename MCVanillaEngine<MultiVariate,RNG,S>::path_pricer_type path_pricer_type;

      public MCEuropeanHestonEngine( HestonProcess process,
                                     int? timeSteps,
                                     int? timeStepsPerYear,
                                     bool antitheticVariate,
                                     int? requiredSamples,
                                     double? requiredTolerance,
                                     int? maxSamples,
                                     ulong seed)
         :base( process, timeSteps, timeStepsPerYear,false, antitheticVariate, false,requiredSamples, requiredTolerance,
                maxSamples, seed )
      {}

      protected override PathPricer<IPath> pathPricer()
      {
         PlainVanillaPayoff payoff= this.arguments_.payoff as PlainVanillaPayoff;
         Utils.QL_REQUIRE(payoff!=null,()=> "non-plain payoff given");

         HestonProcess process = this.process_ as HestonProcess;
         Utils.QL_REQUIRE(process!= null,()=> "Heston process required");

         return new EuropeanHestonPathPricer( payoff.optionType(),
                                              payoff.strike(),
                                              process.riskFreeRate().link.discount(this.timeGrid().Last()));
      }

   }

   //! Monte Carlo Heston European engine factory
    public class MakeMCEuropeanHestonEngine<RNG , S> 
        where RNG : IRSG, new()
        where S : IGeneralStatistics, new()
    {
       public MakeMCEuropeanHestonEngine( HestonProcess process )
       {
          process_ = process; 
          antithetic_ = false;
          steps_ = null; 
          stepsPerYear_ = null;
          samples_ = null; 
          maxSamples_ = null;
          tolerance_ = null;
          seed_ = 0;


       }
       // named parameters
       public MakeMCEuropeanHestonEngine<RNG, S> withSteps( int steps )
       {
          Utils.QL_REQUIRE( stepsPerYear_ == null,()=> "number of steps per year already set" );
          steps_ = steps;
          return this;
       }

       public MakeMCEuropeanHestonEngine<RNG, S> withStepsPerYear( int steps )
       {
          Utils.QL_REQUIRE( steps_ == null,()=> "number of steps already set" );
          stepsPerYear_ = steps;
          return this;   
       }

       public MakeMCEuropeanHestonEngine<RNG, S> withSamples( int samples )
       {
          Utils.QL_REQUIRE( tolerance_ == null,()=> "tolerance already set" );
          samples_ = samples;
          return this;          
       }

       public MakeMCEuropeanHestonEngine<RNG, S> withAbsoluteTolerance( double tolerance )
       {
          Utils.QL_REQUIRE(samples_ == null,()=> "number of samples already set");
          Utils.QL_REQUIRE( new RNG().allowsErrorEstimate != 0, () => "chosen random generator policy does not allow an error estimate" );
          tolerance_ = tolerance;
          return this;   
       }

       public MakeMCEuropeanHestonEngine<RNG, S> withMaxSamples( int samples )
       {
          maxSamples_ = samples;
          return this;
       }

       public MakeMCEuropeanHestonEngine<RNG, S> withSeed( ulong seed )
       {
          seed_ = seed;
          return this;
       }

       public MakeMCEuropeanHestonEngine<RNG, S> withAntitheticVariate( bool b = true )
       {
          antithetic_ = b;
          return this;
       }

       // conversion to pricing engine
       public IPricingEngine getAsPricingEngine()
       {
          Utils.QL_REQUIRE(steps_ != null || stepsPerYear_ != null,()=> "number of steps not given");
          return new MCEuropeanHestonEngine<RNG,S>(process_,
                                                   steps_,
                                                   stepsPerYear_,
                                                   antithetic_,
                                                   samples_, tolerance_,
                                                   maxSamples_,
                                                   seed_);
       }


       private HestonProcess process_;
       private bool antithetic_;
       private int? steps_, stepsPerYear_, samples_, maxSamples_;
       private double? tolerance_;
       private ulong seed_;
    };


   public class EuropeanHestonPathPricer : PathPricer<IPath> 
   {
      public EuropeanHestonPathPricer(Option.Type type, double strike,double discount)
      {
         payoff_ = new PlainVanillaPayoff(type, strike);
         discount_ = discount;

         Utils.QL_REQUIRE( strike >= 0.0,()=> "strike less than zero not allowed" );
      }
      
      public double value(IPath multiPath)
      {
         MultiPath m = multiPath as MultiPath;
         Utils.QL_REQUIRE( m != null , () => "the path is invalid" );
         Path path = m[0];
         int n = m.pathSize();
         Utils.QL_REQUIRE(n>0,()=> "the path cannot be empty");

        return payoff_.value(path.back()) * discount_;   
      }
      
      private PlainVanillaPayoff payoff_;
      private double discount_;
   }
}
