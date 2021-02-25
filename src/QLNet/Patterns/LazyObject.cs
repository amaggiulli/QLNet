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
      protected volatile bool calculated_;
      protected volatile bool frozen_;

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

      public void registerWith(Callback handler) {} public void XXXregisterWith(Callback handler) { notifyObserversEvent += handler; }
      public void unregisterWith(Callback handler) {} public void XXXunregisterWith(Callback handler) { notifyObserversEvent -= handler; }
      protected void notifyObservers()
      {
         eventSource.Raise();
      }

      // This method is the observer interface
      // It must be implemented in derived classes and linked to the event of the required Observer
      public virtual void update()
      {
         using (new MayLock(this))
         {
            // observers don't expect notifications from frozen objects
            // LazyObject forwards notifications only once until it has been recalculated
            if (!frozen_ && calculated_)
               notifyObservers();
            calculated_ = false;
         }
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
      public void freeze() { lock (this) { frozen_ = true; } }

      // This method reverts the effect of the <i><b>freeze</b></i> method, thus re-enabling recalculations.
      public void unfreeze()
      {
         using (new MayLock(this))
         {
            frozen_ = false;
            notifyObservers();              // send notification, just in case we lost any
         }
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
         using (new MayLock(this))
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
      }
      /* This method must implement any calculations which must be (re)done
       * in order to calculate the desired results. */
      protected virtual void performCalculations()
      {
         throw new NotSupportedException();
      }
      #endregion

      /// <summary>
      /// Locking construct to detect deadlock scenarion.  Either locks the object or throws lock recursion error
      /// The use of MayLock highlights a design error in the locking order, that should be fixed directly; use MayLock where the code can not be refactored
      /// </summary>
      /// <example>
      ///   using (MayLock(target))
      ///   {
      ///      ..
      ///   }
      ///   as a replacement for
      ///   lock (target)
      ///   {
      ///      ..
      ///   }
      ///   to avoid deadlock
      /// </example>
      /// <remarks>
      /// scenario 1: the object is not locked -> ok to take it
      /// scenario 2: the object is locked, but the holding thread is not locked waiting for a lock held by this thread -> ok to wait
      /// scenario 3: the object is locked, but the locking thread is waiting for a lock that this thread currently holds -> would be deadlock
      /// scenario 4: the object is locked, but a thread is waiting to aquire that lock from another thread that is waiting to aquire a lock that this thread holds -> also deadlock
      /// scenario 5: deadlock is detected, but the holder of the lock causing deadlock is waiting to enter MAyLock::Dispose() to release the lock -> phanton deadlock could retry
      /// scenario 6: the thread already holds the lock and can reenter teh monitor
      ///
      /// This class uses aggresive dadlock detection, and not suitable for a scenarios where wait many or timeout could be used
      /// </remarks>
      internal class MayLock : IDisposable
      {
         protected object _Object;                    // object being locked
         protected System.Threading.Thread _Thread;   // thread locking
         protected volatile bool _Disposed = false;   // lazy leanup flag
         protected volatile bool _Held = false;       // Deos the thread hold the lock
         protected const double _pulse = 1000.0;      // time to pulse between lock test
         protected const int _lockPulse = 1000;       // time to enque lock

         private static System.Collections.Generic.List<MayLock> _locks;
         private static System.Timers.Timer _timer;

         #region deadlock detection
         static MayLock ()
         {
            _locks = new System.Collections.Generic.List<MayLock>();

            _timer = new System.Timers.Timer(_pulse);
            _timer.Elapsed += _timer_Elapsed;
            _timer.AutoReset = false;
            _timer.Enabled = true;
            _timer.Start();
         }

         private static void _timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
         {
            try
            {
               MayLock[] snap;
               // snapshot current locks  
               lock (_locks)
               {
                  snap = _locks.ToArray();
                  foreach (var ml in snap)
                     if (ml._Disposed)
                        _locks.Remove(ml);
               }

               foreach (var ml1 in snap)
               {
                  // blocked thread
                  if (!ml1._Disposed &&
                      !ml1._Held)
                  {
                     bool notHeld = true;
                     foreach(var ml2 in snap)
                     {
                        // threads also blocked with the same object
                        if (ml1 != ml2 &&
                           !ml2._Disposed &&
                            ml2._Held &&
                            ml1._Object == ml2._Object)
                        {
                           notHeld = false;
                           // find deadlock
                           if (Deadlock (snap, ml1, ml2,new MayLock[0]))
                           {
                              ml1._Disposed = true;      // trigger exception
                              return;
                           }
                        }
                     }
                     if (notHeld)
                     {
                        ml1._Disposed = true;      // trigger exception
                        return;
                     }
                  }
               }
            }
            finally
            {
               _timer.Enabled = true;                 // re-schedule dealock detection
            }
         }

         /// <summary>
         /// Find deadlock path back to target
         /// </summary>
         /// <param name="scope">snap</param>
         /// <param name="target">current blocked lock</param>
         /// <param name="self">lock being considered</param>
         /// <param name="path">locks already visited</param>
         /// <returns>true if deadlock found</returns>
         private static bool Deadlock (MayLock[] scope, MayLock target, MayLock self, MayLock[] path)
         {
            foreach(var ml1 in scope)
            {
               // filter scope to self
               if (ml1 == self && !ml1._Held && !ml1._Disposed)
               {
                  var newPath = new MayLock[path.Length + 1];
                  path.CopyTo(newPath, 1);

                  foreach (var ml2 in scope)
                  {
                     // find holder of the lock self is waiting on
                     if (!ml2._Disposed && ml1._Object == ml2._Object)
                     {
                        // we've recursed to the target is deadlock
                        if (ml2 == target)
                           return true;
                        else if (ml2._Held)
                        {
                           // filter rows already visited
                           bool notpath = true;
                           foreach (var p in path)
                           {
                              if (p == ml2)
                              {
                                 notpath = false;
                                 break;
                              }
                           }
                           // recuse to find lock
                           if (notpath)
                           {
                              newPath[0] = ml2;
                              if (Deadlock(scope, target, ml2, newPath))
                                 return true;
                           }
                        }
                     }
                  }
               }
            }
            return false;
         }
         #endregion

         public MayLock(object target)
         {
            _Object = target;
            _Thread = System.Threading.Thread.CurrentThread;
            lock (_locks)
            {
               _locks.Add(this);
            }
            while (!System.Threading.Monitor.TryEnter(target, _lockPulse))
            {
               if (_Disposed) throw new System.Threading.LockRecursionException();
            }
            _Held = true;
         }

         public void Dispose()
         {
            if (!_Disposed)
               System.Threading.Monitor.Exit(_Object);
            _Disposed = true;
         }
      }
   }
}

