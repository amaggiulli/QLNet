/*
 Copyright (C) 2016 Thomas Levesque // http://www.thomaslevesque.com/2015/08/16/weak-events-in-c-take-two
 Copyright (C) 2016 Francois Botha (igitur@gmail.com)
 Copyright (C) 2008-2026  Andrea Maggiulli (a.maggiulli@gmail.com)

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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace QLNet
{
   public class WeakEventSource
   {
      private readonly List<WeakDelegate> _handlers;

      // Per-thread opt-out for the Subscribe/Unsubscribe/Raise pipeline. Some callers construct large
      // numbers of short-lived, single-use IObservable/IObserver graphs (e.g. one-off bond pricing) where the
      // observer-notification machinery (reflection-based weak-delegate wrapping, list mutation under lock) is
      // pure overhead: nothing outlives the call, and nothing needs to be notified of anything. Wrapping such a
      // call in a using(WeakEventSource.SuppressNotifications()) scope makes Subscribe/Unsubscribe/Raise no-ops
      // on every WeakEventSource instance, for that thread only, for the scope's lifetime, with no effect on
      // other threads. Clear() is intentionally NOT suppressed: it discards handlers rather than notifying them,
      // so it remains active (and still affects the shared handler list for all threads) even inside the scope.
      [ThreadStatic]
      private static int _suppressionDepth;

      public static bool NotificationsSuppressed => _suppressionDepth > 0;

      public static SuppressionScope SuppressNotifications()
      {
         return new SuppressionScope(Environment.CurrentManagedThreadId);
      }

      // Public, non-allocating value-type scope: SuppressNotifications() is meant to be used in hot paths, so
      // it returns this concrete struct (rather than IDisposable) to let `using (WeakEventSource.
      // SuppressNotifications())` dispose it via the C# pattern-based using support with no heap allocation and
      // no boxing. It cannot be a `readonly struct` because Dispose() needs to mutate the _disposed field to
      // stay idempotent (safe to call more than once), per the usual IDisposable contract.
      public struct SuppressionScope : IDisposable
      {
         private readonly int _creatingThreadId;
         private bool _disposed;

         internal SuppressionScope(int creatingThreadId)
         {
            _creatingThreadId = creatingThreadId;
            _disposed = false;
            // Increment only once construction is actually underway, not before allocating this object: if
            // allocation/construction itself were to throw (e.g. OutOfMemoryException), the counter would
            // otherwise have already been bumped with no scope ever handed back to the caller to dispose it,
            // permanently (for this thread) suppressing notifications.
            _suppressionDepth++;
         }

         public void Dispose()
         {
            if (_disposed)
               return;

            // _suppressionDepth is [ThreadStatic]: disposing this scope on a thread other than the one that
            // created it (e.g. after an `await` resumes on a different thread pool thread, or the scope is
            // otherwise handed off across threads) would decrement the *wrong* thread's counter, silently
            // corrupting suppression state and potentially leaving the creating thread permanently suppressed.
            // Fail loudly instead so the misuse is caught immediately.
            if (Environment.CurrentManagedThreadId != _creatingThreadId)
               throw new InvalidOperationException(
                  "WeakEventSource.SuppressNotifications() scope must be disposed on the same thread that created it.");

            // Guard against underflow: if _suppressionDepth were ever corrupted (e.g. a misbalanced/duplicated
            // scope elsewhere), decrementing past 0 would silently leave suppression permanently disabled
            // (NotificationsSuppressed only checks > 0) instead of failing fast.
            if (_suppressionDepth <= 0)
               throw new InvalidOperationException(
                  "WeakEventSource.SuppressNotifications() scope disposed more times than it was entered on this thread.");

            _disposed = true;
            _suppressionDepth--;
         }
      }

      public WeakEventSource()
      {
         _handlers = new List<WeakDelegate>();
      }

      public void Raise()
      {
         if (NotificationsSuppressed)
            return;

         lock (_handlers)
         {
            _handlers.RemoveAll(h => !h.Invoke());
         }
      }

      public void Subscribe(Callback handler)
      {
         if (NotificationsSuppressed)
            return;

         var weakHandlers = handler
                            .GetInvocationList()
                            .Select(d => new WeakDelegate(d))
                            .ToList();

         lock (_handlers)
         {
            _handlers.AddRange(weakHandlers);
         }
      }

      public void Unsubscribe(Callback handler)
      {
         if (NotificationsSuppressed)
            return;

         lock (_handlers)
         {
            int index = _handlers.FindIndex(h => h.IsMatch(handler));
            if (index >= 0)
               _handlers.RemoveAt(index);
         }
      }

      // Clear() is intentionally left unaffected by SuppressNotifications(): it discards existing handlers
      // rather than notifying/adding/removing them, so suppressing it would not save any of the overhead the
      // scope targets and would instead risk silently keeping stale handlers alive.
      public void Clear()
      {
         lock (_handlers)
         {
            _handlers.Clear();
         }
      }

      private class WeakDelegate
      {
         #region Open handler generation and cache

         private delegate void OpenEventHandler(object target);

         // ReSharper disable once StaticMemberInGenericType (by design)
         private static readonly ConcurrentDictionary<MethodInfo, OpenEventHandler> _openHandlerCache =
            new ConcurrentDictionary<MethodInfo, OpenEventHandler>();

         private static OpenEventHandler CreateOpenHandler(MethodInfo method)
         {
            var target = Expression.Parameter(typeof(object), "target");

            if (method.IsStatic)
            {
               var expr = Expression.Lambda<OpenEventHandler>(
                             Expression.Call(
                                method),
                             target);
               return expr.Compile();
            }
            else
            {
               var expr = Expression.Lambda<OpenEventHandler>(
                             Expression.Call(
                                Expression.Convert(target, method.DeclaringType),
                                method),
                             target);
               return expr.Compile();
            }
         }

         #endregion Open handler generation and cache

         private readonly WeakReference _weakTarget;
         private readonly MethodInfo _method;
         private readonly OpenEventHandler _openHandler;

         public WeakDelegate(Delegate handler)
         {
            _weakTarget = handler.Target != null ? new WeakReference(handler.Target) : null;
#if NET452
            _method = handler.Method;
#else
            _method = handler.GetMethodInfo();
#endif

            _openHandler = _openHandlerCache.GetOrAdd(_method, CreateOpenHandler);
         }

         public bool Invoke()
         {
            object target = null;
            if (_weakTarget != null)
            {
               target = _weakTarget.Target;
               if (target == null)
                  return false;
            }
            _openHandler(target);
            return true;
         }

         public bool IsMatch(Callback handler)
         {
#if NET452
            return _weakTarget.Target != null && (ReferenceEquals(handler.Target, _weakTarget.Target)
                                                  && handler.Method.Equals(_method));
#else
            return _weakTarget.Target != null && (ReferenceEquals(handler.Target, _weakTarget.Target)
                                                  && handler.GetMethodInfo().Equals(_method));
#endif

         }
      }
   }
}
