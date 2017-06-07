/*
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

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace QLNet
{
   public delegate void Callback();

   public interface IObservable
   {
      //event Callback notifyObserversEvent;
      //void registerWith(Callback handler);
      //void unregisterWith(Callback handler);
   }

   public interface IObserver
   {
      //void update();
   }

   public static class IObservableCode
   {
      class State
      {
         public readonly WeakEventSource eventSource = new WeakEventSource();
         public event Callback notifyObserversEvent
         {
            add { eventSource.Subscribe(value); }
            remove { eventSource.Unsubscribe(value); }
         }
      }

      private static readonly ConditionalWeakTable<IObservable, State>
         _stateTable = new ConditionalWeakTable<IObservable, State>();

      public static WeakEventSource eventSource(this IObservable self)
      {
         return _stateTable.GetOrCreateValue(self).eventSource;
      }

      public static void registerWith(this IObservable self, Callback handler)
      {
         _stateTable.GetOrCreateValue(self).notifyObserversEvent += handler;
      }

      public static void unregisterWith(this IObservable self, Callback handler)
      {
         _stateTable.GetOrCreateValue(self).notifyObserversEvent -= handler;
      }

      public static void notifyObservers(this IObservable self)
      {
         _stateTable.GetOrCreateValue(self).eventSource.Raise();
      }

      //private static readonly ConditionalWeakTable<IObservable, WeakEventSource>
      //   _eventSourceTable = new ConditionalWeakTable<IObservable, WeakEventSource>();

      //public static WeakEventSource eventSource(this IObservable self)
      //{
      //   return _eventSourceTable.GetOrCreateValue(self);
      //}
   }

   public static class IObserverCode
   {
      public static void update(this IObserver self)
      {
         ((IObservable) self).notifyObservers();
      }
   }
}
