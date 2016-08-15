/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2016  Andrea Maggiulli (a.maggiulli@gmail.com)
  
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
using System;
using System.Collections.Generic;

namespace QLNet
{
   // purely virtual base class for indexes
   // this class performs no check that the provided/requested fixings are for dates in the past,
   // i.e. for dates less than or equal to the evaluation date. It is up to the client code to take care of
   // possible inconsistencies due to "seeing in the future"
   public abstract class Index : IObservable
   {
      // Returns the name of the index.
      // This method is used for output and comparison between indexes.
      // It is not meant to be used for writing switch-on-type code.
      public abstract string name();
      
      // Returns the calendar defining valid fixing dates
      public abstract Calendar fixingCalendar();
      
      // Returns TRUE if the fixing date is a valid one
      public abstract bool isValidFixingDate( Date fixingDate );   
      
      // Returns the fixing at the given date
      // The date passed as arguments must be the actual calendar date of the fixing; no settlement days must be used.
      public abstract double fixing( Date fixingDate, bool forecastTodaysFixing = false);
      
      // Returns the fixing TimeSeries
      public ObservableValue<TimeSeries<double>> timeSeries() { return IndexManager.instance().getHistory( name() ); }
      
      // Check if index allows for native fixings.
      // If this returns false, calls to addFixing and similar methods will raise an exception.
      public virtual bool allowsNativeFixings() { return true; }
      
      // Stores the historical fixing at the given date
      // The date passed as arguments must be the actual calendar date of the fixing; no settlement days must be used.
      public virtual void addFixing( Date d, double v, bool forceOverwrite = false )
      {
         checkNativeFixingsAllowed();
         addFixings( new TimeSeries<double>() { { d, v } }, forceOverwrite );
      }
      
      // Stores historical fixings from a TimeSeries
      // The dates in the TimeSeries must be the actual calendar dates of the fixings; no settlement days must be used.
      public void addFixings( Dictionary<Date, double> source, bool forceOverwrite = false )
      {
         checkNativeFixingsAllowed();
         ObservableValue<TimeSeries<double>> target = IndexManager.instance().getHistory( name() );
         foreach ( Date d in source.Keys )
         {
            if ( isValidFixingDate( d ) )
               if ( !target.value().ContainsKey( d ) )
                  target.value().Add( d, source[d] );
               else
                  if ( forceOverwrite )
                     target.value()[d] = source[d];
                  else if ( Utils.close( target.value()[d], source[d] ) )
                     continue;
                  else
                     throw new ArgumentException( "Duplicated fixing provided: " + d + ", " + source[d] +
                                                 " while " + target.value()[d] + " value is already present" );
            else
               throw new ArgumentException( "Invalid fixing provided: " + d.DayOfWeek + " " + d + ", " + source[d] );
         }

         IndexManager.instance().setHistory( name(), target );
      }
      
      // Stores historical fixings at the given dates
      // The dates passed as arguments must be the actual calendar dates of the fixings; no settlement days must be used.
      public void addFixings( List<Date> d, List<double> v, bool forceOverwrite = false )
      {
         if ( ( d.Count != v.Count ) || d.Count == 0 )
            throw new ArgumentException( "Wrong collection dimensions when creating index fixings" );

         TimeSeries<double> t = new TimeSeries<double>();
         for ( int i = 0; i < d.Count; i++ )
            t.Add( d[i], v[i] );
         addFixings( t, forceOverwrite );
      }
      
      // Clears all stored historical fixings
      public void clearFixings()
      {
         checkNativeFixingsAllowed();
         IndexManager.instance().clearHistory( name() );
      }

      // Check if index allows for native fixings
      private void checkNativeFixingsAllowed()
      {
         Utils.QL_REQUIRE( allowsNativeFixings(), ()=>
            "native fixings not allowed for " + name() + "; refer to underlying indices instead" );
      }

        

      #region observable interface
      private readonly WeakEventSource eventSource = new WeakEventSource();
      public event Callback notifyObserversEvent
      {
         add { eventSource.Subscribe( value ); }
         remove { eventSource.Unsubscribe( value ); }
      }

      public void registerWith( Callback handler ) { notifyObserversEvent += handler; }
      public void unregisterWith( Callback handler ) { notifyObserversEvent -= handler; }
      protected void notifyObservers()
      {
         eventSource.Raise();
      }
      #endregion
   }
}