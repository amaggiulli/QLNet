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
using System;
using System.Runtime.CompilerServices;

namespace QLNet
{
   // Framework for calculation on demand and result caching.
   // Introduces Observer pattern
   public interface ILazyObject : IObservable, IObserver
   {
      void performCalculations();
   }

   public static class ILazyObjectCode
   {
      private class State
      {
         public bool calculated_;
         public bool frozen_;
      }

      private static readonly ConditionalWeakTable<ILazyObject, State>
         _stateTable = new ConditionalWeakTable<ILazyObject, State>();

      public static bool calculated_(this ILazyObject self)
      {
         return _stateTable.GetOrCreateValue(self).calculated_;
      }

      public static void calculated_(this ILazyObject self, bool value)
      {
         _stateTable.GetOrCreateValue(self).calculated_ = value;
      }

      public static bool frozen_(this ILazyObject self)
      {
         return _stateTable.GetOrCreateValue(self).frozen_;
      }

      public static void frozen_(this ILazyObject self, bool value)
      {
         _stateTable.GetOrCreateValue(self).frozen_ = value;
      }

      #region Observer interface

      // This method is the observer interface
      // It must be implemented in derived classes and linked to the event of the required Observer
      public static void update(this ILazyObject self)
      {
         // observers don't expect notifications from frozen objects
         // LazyObject forwards notifications only once until it has been recalculated
         if (!_stateTable.GetOrCreateValue(self).frozen_ && _stateTable.GetOrCreateValue(self).calculated_)
            self.notifyObservers();
         _stateTable.GetOrCreateValue(self).calculated_= false;
      }

      #endregion

      #region Calculation methods

      /*! This method forces recalculation of any results which would otherwise be cached.
       * It needs to call the <i><b>LazyCalculationEvent</b></i> event.
         Explicit invocation of this method is <b>not</b> necessary if the object has registered itself as
         observer with the structures on which such results depend.  It is strongly advised to follow this
         policy when possible. */
      public static void recalculate(this ILazyObject self)
      {
         bool wasFrozen = _stateTable.GetOrCreateValue(self).frozen_;
         _stateTable.GetOrCreateValue(self).calculated_ = _stateTable.GetOrCreateValue(self).frozen_ = false;
         try
         {
            self.calculate();
         }
         catch
         {
            _stateTable.GetOrCreateValue(self).frozen_ = wasFrozen;
            self.notifyObservers();
            throw;
         }
         _stateTable.GetOrCreateValue(self).frozen_ = wasFrozen;
         self.notifyObservers();
      }

      /*! This method constrains the object to return the presently cached results on successive invocations,
       * even if arguments upon which they depend should change. */
      public static void freeze(this ILazyObject self)
      {
         _stateTable.GetOrCreateValue(self).frozen_ = true;
      }

      // This method reverts the effect of the <i><b>freeze</b></i> method, thus re-enabling recalculations.
      public static void unfreeze(this ILazyObject self)
      {
         _stateTable.GetOrCreateValue(self).frozen_ = false;
         self.notifyObservers(); // send notification, just in case we lost any
      }

      /*! This method performs all needed calculations by calling the <i><b>performCalculations</b></i> method.
          Objects cache the results of the previous calculation. Such results will be returned upon
          later invocations of <i><b>calculate</b></i>. When the results depend
          on arguments which could change between invocations, the lazy object must register itself
          as observer of such objects for the calculations to be performed again when they change.
          Should this method be redefined in derived classes, LazyObject::calculate() should be called
          in the overriding method. */
      public static void calculate(this ILazyObject self)
      {
         if (!_stateTable.GetOrCreateValue(self).calculated_ && !_stateTable.GetOrCreateValue(self).frozen_)
         {
            _stateTable.GetOrCreateValue(self).calculated_ = true; // prevent infinite recursion in case of bootstrapping
            try
            {
               self.performCalculations();
            }
            catch
            {
               _stateTable.GetOrCreateValue(self).calculated_ = false;
               throw;
            }
         }
      }

      /* This method must implement any calculations which must be (re)done 
       * in order to calculate the desired results. */
      //public static void performCalculations(this ILazyObject self)
      //{
      //   throw new NotSupportedException();
      //}

      #endregion
   }
}
