/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)

 This file is part of QLNet Project https://github.com/amaggiulli/qlnet

 QLNet is free software: you can redistribute it and/or modify it
 under the terms of the QLNet license.  You should have received a
 copy of the license along with this program; if not, license is
 available at <https://github.com/amaggiulli/QLNet/blob/develop/LICENSE>.

 QLNet is a based on QuantLib, a free-software/open-source library
 for financial quantitative analysts and developers - http://quantlib.org/
 The QuantLib license is available online at http://quantlib.org/license.shtml.

 This program is distributed in the hope that it will be useful, but WITHOUT
 ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
 FOR A PARTICULAR PURPOSE.  See the license for more details.
*/
using System;

namespace QLNet
{
   /// <summary>
   /// Framework for on-demand calculation and result caching.
   /// </summary>
   /// <remarks>
   /// This class combines lazy calculation with the observer pattern.
   /// </remarks>
   public abstract class LazyObject : IObservable, IObserver
   {
      protected bool calculated_;
      protected bool frozen_;

      #region Observer interface
      // Here we define this object as observable
      private readonly WeakEventSource eventSource = new WeakEventSource();
      public event Callback notifyObserversEvent
      {
         add
         {
            eventSource.Subscribe(value);
         }
         remove
         {
            eventSource.Unsubscribe(value);
         }
      }

      public void registerWith(Callback handler) { notifyObserversEvent += handler; }
      public void unregisterWith(Callback handler) { notifyObserversEvent -= handler; }
      protected void notifyObservers()
      {
         eventSource.Raise();
      }

      // This method is the observer interface
      // It must be implemented in derived classes and linked to the event of the required Observer
      public virtual void update()
      {
         // observers don't expect notifications from frozen objects
         // LazyObject forwards notifications only once until it has been recalculated
         if (!frozen_ && calculated_)
            notifyObservers();
         calculated_ = false;
      }
      #endregion

      #region Calculation methods
      /// <summary>
      /// Forces recalculation of any results which would otherwise be cached.
      /// </summary>
      /// <remarks>
      /// Explicit invocation of this method is not necessary if the object
      /// has registered itself as observer with the structures on which such
      /// results depend. Following that policy is strongly advised when possible.
      /// </remarks>
      public virtual void recalculate()
      {
         bool wasFrozen = frozen_;
         calculated_ = frozen_ = false;
         try
         {
            calculate();
         }
         catch
         {
            frozen_ = wasFrozen;
            notifyObservers();
            throw;
         }
         frozen_ = wasFrozen;
         notifyObservers();
      }

      /// <summary>
      /// Freezes the object so successive calls keep returning the currently cached results.
      /// </summary>
      public void freeze() { frozen_ = true; }

      // This method reverts the effect of the <i><b>freeze</b></i> method, thus re-enabling recalculations.
      public void unfreeze()
      {
         frozen_ = false;
         notifyObservers();              // send notification, just in case we lost any
      }

      /// <summary>
      /// Performs all needed calculations by calling <see cref="performCalculations"/>.
      /// </summary>
      /// <remarks>
      /// Objects cache the results of the previous calculation. Such results
      /// are returned by later invocations of this method. When the results
      /// depend on arguments that can change between invocations, the lazy object
      /// must register itself as observer of such objects so calculations are
      /// performed again when they change. If this method is redefined in a
      /// derived class, the overriding method should call the base implementation.
      /// </remarks>
      protected virtual void calculate()
      {
         if (!calculated_ && !frozen_)
         {
            calculated_ = true;   // prevent infinite recursion in case of bootstrapping
            try
            {
               performCalculations();
            }
            catch
            {
               calculated_ = false;
               throw;
            }
         }
      }

      /// <summary>
      /// Performs the calculations required to produce the desired results.
      /// </summary>
      protected virtual void performCalculations()
      {
         throw new NotSupportedException();
      }
      #endregion
   }
}
