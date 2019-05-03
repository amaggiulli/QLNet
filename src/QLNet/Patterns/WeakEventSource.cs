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

      public WeakEventSource()
      {
         _handlers = new List<WeakDelegate>();
      }

      public void Raise()
      {
         lock (_handlers)
         {
            _handlers.RemoveAll(h => !h.Invoke());
         }
      }

      public void Subscribe(Callback handler)
      {
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
         lock (_handlers)
         {
            int index = _handlers.FindIndex(h => h.IsMatch(handler));
            if (index >= 0)
               _handlers.RemoveAt(index);
         }
      }

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
