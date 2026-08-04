/*
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
using System.Threading;
using Xunit;
using QLNet;

namespace TestSuite
{
   [Collection("QLNet CI Tests")]
   public class T_WeakEventSourceSuppression
   {
      // Kept as an instance (non-static) handler target so WeakEventSource wraps it in a real
      // WeakReference; the local variable in each test keeps it alive for the test's duration.
      private class Counter
      {
         public int Count;
         public void Increment() { Count++; }
      }

      [Fact]
      public void SuppressNotifications_SuppressesSubscribeRaiseAndUnsubscribe()
      {
         var source = new WeakEventSource();
         var counter = new Counter();

         using (WeakEventSource.SuppressNotifications())
         {
            // Subscribe should be a no-op inside the scope.
            source.Subscribe(counter.Increment);
            // Raise should be a no-op inside the scope, even if a handler were registered.
            source.Raise();
         }

         Assert.Equal(0, counter.Count);

         // Outside the scope, Raise now works normally, but since Subscribe was suppressed while
         // the scope was active, the handler was never actually added: nothing should fire.
         source.Raise();
         Assert.Equal(0, counter.Count);

         // Subscribe for real, outside any suppression scope.
         source.Subscribe(counter.Increment);
         source.Raise();
         Assert.Equal(1, counter.Count);

         using (WeakEventSource.SuppressNotifications())
         {
            // Unsubscribe should be a no-op inside the scope: the handler must remain registered.
            source.Unsubscribe(counter.Increment);
         }

         source.Raise();
         Assert.Equal(2, counter.Count);
      }

      [Fact]
      public void SuppressNotifications_NestedScopesComposeViaDepthCounter()
      {
         Assert.False(WeakEventSource.NotificationsSuppressed);

         var outer = WeakEventSource.SuppressNotifications();
         Assert.True(WeakEventSource.NotificationsSuppressed);

         var inner = WeakEventSource.SuppressNotifications();
         Assert.True(WeakEventSource.NotificationsSuppressed);

         inner.Dispose();
         // The outer scope is still active: notifications must remain suppressed until it, too,
         // is disposed (depth-counter semantics, not a simple bool).
         Assert.True(WeakEventSource.NotificationsSuppressed);

         outer.Dispose();
         Assert.False(WeakEventSource.NotificationsSuppressed);
      }

      [Fact]
      public void SuppressNotifications_DisposingTwiceIsIdempotentAndDoesNotUnderflow()
      {
         var scope = WeakEventSource.SuppressNotifications();
         Assert.True(WeakEventSource.NotificationsSuppressed);

         scope.Dispose();
         Assert.False(WeakEventSource.NotificationsSuppressed);

         // A second Dispose() call must be a safe no-op: this API is explicitly designed to be
         // idempotent (IDisposable itself doesn't require this, it's just a common convention), and
         // must not decrement _suppressionDepth a second time (which would trigger the underflow
         // guard on a subsequent, unrelated SuppressNotifications()/Dispose() pair on this thread).
         scope.Dispose();
         Assert.False(WeakEventSource.NotificationsSuppressed);

         using (WeakEventSource.SuppressNotifications())
         {
            Assert.True(WeakEventSource.NotificationsSuppressed);
         }

         Assert.False(WeakEventSource.NotificationsSuppressed);
      }

      // Bounded so that a real deadlock/logic bug in the suppression scope fails the test quickly
      // with a clear assertion instead of hanging the whole suite indefinitely.
      private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(10);

      [Fact]
      public void SuppressNotifications_IsThreadLocal_OtherThreadsAreUnaffected()
      {
         var source = new WeakEventSource();
         var counter = new Counter();
         source.Subscribe(counter.Increment);

         using var suppressionEntered = new ManualResetEventSlim(false);
         using var releaseOtherThread = new ManualResetEventSlim(false);
         Exception otherThreadException = null;

         var otherThread = new Thread(() =>
         {
            try
            {
               // Wait until the main thread has entered its suppression scope before raising, so
               // this genuinely exercises "another thread while suppression is active elsewhere".
               Assert.True(suppressionEntered.Wait(WaitTimeout));
               Assert.False(WeakEventSource.NotificationsSuppressed);
               source.Raise();
            }
            catch (Exception ex)
            {
               otherThreadException = ex;
            }
            finally
            {
               releaseOtherThread.Set();
            }
         });
         otherThread.IsBackground = true;
         otherThread.Start();

         using (WeakEventSource.SuppressNotifications())
         {
            Assert.True(WeakEventSource.NotificationsSuppressed);
            suppressionEntered.Set();
            Assert.True(releaseOtherThread.Wait(WaitTimeout));
         }

         Assert.True(otherThread.Join(WaitTimeout));

         Assert.Null(otherThreadException);
         // The other thread was never suppressed, so its Raise() must have invoked the handler.
         Assert.Equal(1, counter.Count);
      }

      // There is deliberately no "dispose on a different thread" test: SuppressionScope is a `ref
      // struct`, so the compiler itself forbids capturing it in a lambda/closure (e.g. the body of
      // `new Thread(() => scope.Dispose())` below would not compile), storing it in a field, or
      // using it across an `await`. Cross-thread misuse of the scope is therefore a compile-time
      // error, not something that can be exercised or asserted on at runtime.
   }
}
