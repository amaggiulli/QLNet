/*
 Copyright (C) 2016 Thomas Levesque // http://www.thomaslevesque.com/2015/08/16/weak-events-in-c-take-two
 Copyright (C) 2016 Francois Botha (igitur@gmail.com)

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
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace QLNet
{
   public class WeakEventSource
   {
      private readonly DelegateCollection _handlers;

      public WeakEventSource()
      {
         _handlers = new DelegateCollection();
      }

      public void Raise()
      {
         lock (_handlers)
         {
            var failedHandlers = new List<int>();
            int i = 0;
            foreach (var handler in _handlers.ToArray())
            {
               if (handler == null || !handler.Invoke())
               {
                  failedHandlers.Add(i);
               }

               i++;
            }

            foreach (var index in failedHandlers)
               _handlers.Invalidate(index);

            _handlers.CollectDeleted();
         }
      }

      public void Subscribe(Callback handler)
      {
         var singleHandlers = handler
                              .GetInvocationList()
                              .Cast<Callback>()
                              .ToList();

         lock (_handlers)
         {
            foreach (var h in singleHandlers)
               _handlers.Add(h);
         }
      }

      public void Unsubscribe(Callback handler)
      {
         var singleHandlers = handler
                              .GetInvocationList()
                              .Cast<Callback>();

         lock (_handlers)
         {
            foreach (var singleHandler in singleHandlers)
            {
               _handlers.Remove(singleHandler);
            }

            _handlers.CollectDeleted();
         }
      }

      private class WeakDelegate
      {
         #region Open handler generation and cache

         private delegate void OpenEventHandler(object target);

         // ReSharper disable once StaticMemberInGenericType (by design)
         private static readonly ConcurrentDictionary<MethodInfo, OpenEventHandler> OpenHandlerCache =
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
#if NET40
            _method = handler.Method;
#else
            _method = handler.GetMethodInfo();
#endif

            _openHandler = OpenHandlerCache.GetOrAdd(_method, CreateOpenHandler);
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
#if NET40
            return ReferenceEquals(handler.Target, _weakTarget?.Target)
                   && handler.Method.Equals(_method);
#else
            return ReferenceEquals(handler.Target, _weakTarget?.Target)
                   && handler.GetMethodInfo().Equals(_method);
#endif
         }

         public static int GetHashCode(Callback handler)
         {
            var hashCode = -335093136;
            hashCode = hashCode * -1521134295 + (handler?.Target?.GetHashCode()).GetValueOrDefault();
#if NET40
            hashCode = hashCode * -1521134295 + (handler?.Method?.GetHashCode()).GetValueOrDefault();
#else
            hashCode = hashCode * -1521134295 + (handler?.GetMethodInfo()?.GetHashCode()).GetValueOrDefault();
#endif
            return hashCode;
         }
      }

      private class DelegateCollection : IEnumerable<WeakDelegate>
      {
         private List<WeakDelegate> _delegates;

         private readonly Dictionary<long, List<int>> _index;

         private int _deletedCount;

         public DelegateCollection()
         {
            _delegates = new List<WeakDelegate>();
            _index = new Dictionary<long, List<int>>();
         }

         public void Add(Callback singleHandler)
         {
            _delegates.Add(new WeakDelegate(singleHandler));
            var index = _delegates.Count - 1;
            AddToIndex(singleHandler, index);
         }

         public void Invalidate(int index)
         {
            _delegates[index] = null;
            _deletedCount++;
         }

         internal void Remove(Callback singleHandler)
         {
            var hashCode = WeakDelegate.GetHashCode(singleHandler);

            if (!_index.ContainsKey(hashCode))
               return;

            var indices = _index[hashCode];
            for (int i = indices.Count - 1; i >= 0; i--)
            {
               int index = indices[i];
               if (_delegates[index] != null &&
                   _delegates[index].IsMatch(singleHandler))
               {
                  _delegates[index] = null;
                  _deletedCount++;
                  indices.Remove(i);
               }
            }

            if (indices.Count == 0)
               _index.Remove(hashCode);
         }

         public void CollectDeleted()
         {
            if (_deletedCount < _delegates.Count / 4)
               return;

            Dictionary<int, int> newIndices = new Dictionary<int, int>();
            var newDelegates = new List<WeakDelegate>();
            int oldIndex = 0;
            int newIndex = 0;
            foreach (var item in _delegates)
            {
               if (item != null)
               {
                  newDelegates.Add(item);
                  newIndices.Add(oldIndex, newIndex);
                  newIndex++;
               }

               oldIndex++;
            }

            _delegates = newDelegates;

            var hashCodes = _index.Keys.ToList();
            foreach (var hashCode in hashCodes)
            {
               _index[hashCode] = _index[hashCode]
                                  .Where(oi => newIndices.ContainsKey(oi))
                                  .Select(oi => newIndices[oi]).ToList();
            }

            _deletedCount = 0;
         }

         private void AddToIndex(Callback singleHandler, int index)
         {
            var hashCode = WeakDelegate.GetHashCode(singleHandler);
            if (_index.ContainsKey(hashCode))
               _index[hashCode].Add(index);
            else
               _index.Add(hashCode, new List<int> { index });
         }

         private WeakDelegate this[int index]
         {
            get
            {
               return _delegates[index];
            }
         }

         /// <summary>Returns an enumerator that iterates through the collection.</summary>
         /// <returns>A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.</returns>
         public IEnumerator<WeakDelegate> GetEnumerator()
         {
            return _delegates.GetEnumerator();
         }

         /// <summary>Returns an enumerator that iterates through a collection.</summary>
         /// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
         IEnumerator IEnumerable.GetEnumerator()
         {
            return GetEnumerator();
         }
      }
   }
}
