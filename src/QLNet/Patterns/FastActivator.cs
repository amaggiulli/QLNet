//  Copyright (C) 2008-2017 Andrea Maggiulli (a.maggiulli@gmail.com)
//
//  This file is part of QLNet Project https://github.com/amaggiulli/qlnet
//  QLNet is free software: you can redistribute it and/or modify it
//  under the terms of the QLNet license.  You should have received a
//  copy of the license along with this program; if not, license is
//  available at <https://github.com/amaggiulli/QLNet/blob/develop/LICENSE>.
//
//  QLNet is a based on QuantLib, a free-software/open-source library
//  for financial quantitative analysts and developers - http://quantlib.org/
//  The QuantLib license is available online at http://quantlib.org/license.shtml.
//
//  This program is distributed in the hope that it will be useful, but WITHOUT
//  ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
//  FOR A PARTICULAR PURPOSE.  See the license for more details.
using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace QLNet
{
   // Base on Sergey Teplyakov code
   // https://blogs.msdn.microsoft.com/seteplia/2017/02/01/dissecting-the-new-constraint-in-c-a-perfect-example-of-a-leaky-abstraction/
   //
   public static class FastActivator<T> where T : new ()
   {
      /// <summary>
      /// Extremely fast generic factory method that returns an instance
      /// of the type <typeparam name="T"/>.
      /// </summary>
      public static readonly Func<T> Create = DynamicModuleLambdaCompiler.GenerateFactory<T>();
   }

   public static class DynamicModuleLambdaCompiler
   {
      public static Func<T> GenerateFactory<T>() where T : new ()
      {
         Expression<Func<T>> expr = () => new T();
         NewExpression newExpr = (NewExpression)expr.Body;

#if NET452
         var method = new DynamicMethod(
            name: "lambda",
            returnType: newExpr.Type,
            parameterTypes: new Type[0],
            m: typeof(DynamicModuleLambdaCompiler).Module,
            skipVisibility: true);
#else
         var method = new DynamicMethod(
            name: "lambda",
            returnType: newExpr.Type,
            parameterTypes: new Type[0],
            m: typeof(DynamicModuleLambdaCompiler).GetTypeInfo().Module,
            skipVisibility: true);
#endif

         ILGenerator ilGen = method.GetILGenerator();
         // Constructor for value types could be null
         if (newExpr.Constructor != null)
         {
            ilGen.Emit(OpCodes.Newobj, newExpr.Constructor);
         }
         else
         {
            LocalBuilder temp = ilGen.DeclareLocal(newExpr.Type);
            ilGen.Emit(OpCodes.Ldloca, temp);
            ilGen.Emit(OpCodes.Initobj, newExpr.Type);
            ilGen.Emit(OpCodes.Ldloc, temp);
         }

         ilGen.Emit(OpCodes.Ret);

         return (Func<T>)method.CreateDelegate(typeof(Func<T>));
      }
   }

}
