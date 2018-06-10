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
   // Pricing Engine interfaces
   // these interfaces replace the abstract PricingEngine class below
   public interface IPricingEngine : IObservable
   {
      IPricingEngineArguments getArguments();
      IPricingEngineResults getResults();
      void reset();
      void calculate();
   }

   public interface IPricingEngineArguments
   {
      void validate();
   }

   public interface IPricingEngineResults
   {
      void reset();
   }

   public interface IGenericEngine : IPricingEngine, IObserver
   {}

   // template base class for option pricing engines
   // Derived engines only need to implement the <tt>calculate()</tt> method.
   public abstract class GenericEngine<ArgumentsType, ResultsType> : IGenericEngine
      where ArgumentsType : IPricingEngineArguments, new ()
      where ResultsType : IPricingEngineResults, new ()
   {
      protected ArgumentsType arguments_ = FastActivator<ArgumentsType>.Create();
      protected ResultsType results_ = FastActivator<ResultsType>.Create();

      public IPricingEngineArguments getArguments()
      {
         return arguments_;
      }

      public IPricingEngineResults getResults()
      {
         return results_;
      }

      public void reset()
      {
         results_.reset();
      }

      public virtual void calculate()
      {
         throw new NotSupportedException();
      }

      #region Observer & Observable

      // observable interface
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

      public void registerWith(Callback handler)
      {
         notifyObserversEvent += handler;
      }

      public void unregisterWith(Callback handler)
      {
         notifyObserversEvent -= handler;
      }

      protected void notifyObservers()
      {
         eventSource.Raise();
      }

      public virtual void update()
      {
         notifyObservers();
      }

      #endregion
   }
}
