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
   // Framework for calculation on demand and result caching.
   // Introduces Observer pattern
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
      /*! This method forces recalculation of any results which would otherwise be cached.
       * It needs to call the <i><b>LazyCalculationEvent</b></i> event.
         Explicit invocation of this method is <b>not</b> necessary if the object has registered itself as
         observer with the structures on which such results depend.  It is strongly advised to follow this
         policy when possible. */
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

      /*! This method constrains the object to return the presently cached results on successive invocations,
       * even if arguments upon which they depend should change. */
      public void freeze() { frozen_ = true; }

      // This method reverts the effect of the <i><b>freeze</b></i> method, thus re-enabling recalculations.
      public void unfreeze()
      {
         frozen_ = false;
         notifyObservers();              // send notification, just in case we lost any
      }

      /*! This method performs all needed calculations by calling the <i><b>performCalculations</b></i> method.
          Objects cache the results of the previous calculation. Such results will be returned upon
          later invocations of <i><b>calculate</b></i>. When the results depend
          on arguments which could change between invocations, the lazy object must register itself
          as observer of such objects for the calculations to be performed again when they change.
          Should this method be redefined in derived classes, LazyObject::calculate() should be called
          in the overriding method. */
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

      /* This method must implement any calculations which must be (re)done
       * in order to calculate the desired results. */
      protected virtual void performCalculations()
      {
         throw new NotSupportedException();
      }
      #endregion
   }
}
