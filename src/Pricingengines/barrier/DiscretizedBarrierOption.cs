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

namespace QLNet
{
   public class DiscretizedBarrierOption : DiscretizedAsset
   {
      public DiscretizedBarrierOption( BarrierOption.Arguments args,StochasticProcess process,TimeGrid grid = null)
      {
         arguments_ = args; 
         vanilla_ = new DiscretizedVanillaOption(arguments_, process, grid);

         Utils.QL_REQUIRE( args.exercise.dates().Count >0 ,()=> "specify at least one stopping date" );

         stoppingTimes_ = new InitializedList<double>( args.exercise.dates().Count );
         for ( int i = 0; i < stoppingTimes_.Count; ++i )
         {
            stoppingTimes_[i] = process.time( args.exercise.date( i ) );
            if ( grid != null && !grid.empty() )
            {
               // adjust to the given grid
               stoppingTimes_[i] = grid.closestTime( stoppingTimes_[i] );
            }
         }
      }

      public override void reset(int size)
      {
         vanilla_.initialize( method(), time() );
         values_ = new Vector( size, 0.0 );
         adjustValues();
      }

      public Vector vanilla()  { return vanilla_.values(); }

      public BarrierOption.Arguments arguments() {return arguments_;}

      public override List<double> mandatoryTimes() {return stoppingTimes_;}

      public void checkBarrier(Vector optvalues, Vector grid)
      {
         double now = time();
         bool endTime = isOnTime(stoppingTimes_.Last());
         bool stoppingTime = false;         
         switch (arguments_.exercise.type()) 
         {
            case Exercise.Type.American:
               if (now <= stoppingTimes_[1] &&
                   now >= stoppingTimes_[0])
                  stoppingTime = true;
               break;
            case Exercise.Type.European:
               if (isOnTime(stoppingTimes_[0]))
                  stoppingTime = true;
               break;
            case Exercise.Type.Bermudan:
               for (int i=0; i<stoppingTimes_.Count; i++) 
               {
                  if (isOnTime(stoppingTimes_[i])) 
                  {
                     stoppingTime = true;
                     break;
                  }
               }
               break;
            default:
               Utils.QL_FAIL("invalid option type");
               break;
         }
         for (int j=0; j<optvalues.size(); j++) 
         {
            switch (arguments_.barrierType) 
            {
               case Barrier.Type.DownIn:
                  if (grid[j] <= arguments_.barrier) 
                  {
                     // knocked in
                     if (stoppingTime) 
                     {
                        optvalues[j] = Math.Max(vanilla_.values()[j],arguments_.payoff.value(grid[j]));
                     }
                     else
                         optvalues[j] = vanilla_.values()[j]; 
                  }
                  else if (endTime)
                      optvalues[j] = arguments_.rebate.GetValueOrDefault();
                  break;
               case Barrier.Type.DownOut:
                  if (grid[j] <= arguments_.barrier)
                      optvalues[j] = arguments_.rebate.GetValueOrDefault(); // knocked out
                  else if (stoppingTime) {
                     optvalues[j] = Math.Max( optvalues[j], arguments_.payoff.value( grid[j] ) );
                  }
                  break;
               case Barrier.Type.UpIn:
                  if (grid[j] >= arguments_.barrier) 
                  {
                     // knocked in
                     if (stoppingTime) 
                     {
                        optvalues[j] = Math.Max( vanilla_.values()[j], arguments_.payoff.value( grid[j] ) );
                     }
                     else
                         optvalues[j] = vanilla_.values()[j]; 
                  }
                  else if (endTime)
                      optvalues[j] = arguments_.rebate.GetValueOrDefault();
                  break;
               case Barrier.Type.UpOut:
                  if (grid[j] >= arguments_.barrier)
                     optvalues[j] = arguments_.rebate.GetValueOrDefault(); // knocked out
                  else if (stoppingTime)
                     optvalues[j] = Math.Max( optvalues[j], arguments_.payoff.value( grid[j] ) );
                  break;
               default:
                  Utils.QL_FAIL("invalid barrier type");
                  break;
            }
         }
      }
      
      protected override void postAdjustValuesImpl()
      {
         if (arguments_.barrierType==Barrier.Type.DownIn ||
             arguments_.barrierType==Barrier.Type.UpIn) 
         {
            vanilla_.rollback(time());
         }
         Vector grid = method().grid(time());
         checkBarrier(values_, grid);
      }
      
      private BarrierOption.Arguments arguments_;
      private List<double> stoppingTimes_;
      private DiscretizedVanillaOption vanilla_; 

   }

   public class DiscretizedDermanKaniBarrierOption : DiscretizedAsset 
   {
      public DiscretizedDermanKaniBarrierOption(BarrierOption.Arguments args,
         StochasticProcess process,TimeGrid grid = null)
      {
         unenhanced_ = new DiscretizedBarrierOption(args, process, grid );
      }

      public override void reset(int size)
      {
         unenhanced_.initialize( method(), time() );
         values_ = new Vector( size, 0.0 );
         adjustValues();
      }

      public override List<double> mandatoryTimes() {return unenhanced_.mandatoryTimes();}
      
      protected override void postAdjustValuesImpl()
      {
         unenhanced_.rollback( time() );

         Vector grid = method().grid( time() );
         adjustBarrier( values_, grid );
         unenhanced_.checkBarrier( values_, grid ); // compute payoffs
      }
      
      private void adjustBarrier(Vector optvalues, Vector grid)
      {
         double? barrier = unenhanced_.arguments().barrier;
         double? rebate = unenhanced_.arguments().rebate;
         switch (unenhanced_.arguments().barrierType) 
         {
            case Barrier.Type.DownIn:
               for (int j=0; j<optvalues.size()-1; ++j) 
               {
                  if (grid[j]<=barrier && grid[j+1] > barrier) 
                  {
                     double? ltob = (barrier-grid[j]);
                     double? htob = (grid[j+1]-barrier);
                     double htol = (grid[j+1]-grid[j]);
                     double u1 = unenhanced_.values()[j+1];
                     double t1 = unenhanced_.vanilla()[j+1];
                     optvalues[j+1] = Math.Max(0.0, (ltob.GetValueOrDefault()*t1+htob.GetValueOrDefault()*u1)/htol);
                  }
               }
               break;
            case Barrier.Type.DownOut:
               for (int j=0; j<optvalues.size()-1; ++j) 
               {
                  if (grid[j]<=barrier && grid[j+1] > barrier) 
                  {
                     double? a = (barrier-grid[j])*rebate;
                     double? b = (grid[j+1]-barrier)*unenhanced_.values()[j+1];
                     double c = (grid[j+1]-grid[j]);
                     optvalues[j+1] = Math.Max(0.0, (a.GetValueOrDefault()+b.GetValueOrDefault())/c);
                  }
               }
               break;
            case Barrier.Type.UpIn:
               for (int j=0; j<optvalues.size()-1; ++j) 
               {
                  if (grid[j] < barrier && grid[j+1] >= barrier) 
                  {
                      double? ltob = (barrier-grid[j]);
                      double? htob = (grid[j+1]-barrier);
                      double htol = (grid[j+1]-grid[j]);
                      double u = unenhanced_.values()[j];
                      double t = unenhanced_.vanilla()[j];
                      optvalues[j] = Math.Max(0.0, (ltob.GetValueOrDefault()*u+htob.GetValueOrDefault()*t)/htol); // derman std
                  }
               }
               break;
            case Barrier.Type.UpOut:
               for (int j=0; j<optvalues.size()-1; ++j) 
               {
                  if (grid[j] < barrier && grid[j+1] >= barrier) 
                  {
                      double? a = (barrier-grid[j])*unenhanced_.values()[j];
                      double? b = (grid[j+1]-barrier)*rebate;
                      double c = (grid[j+1]-grid[j]);
                      optvalues[j] = Math.Max(0.0, (a.GetValueOrDefault()+b.GetValueOrDefault())/c);
                  }
              }
              break;
        }
      }
      private DiscretizedBarrierOption unenhanced_;
   }
}
