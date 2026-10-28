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
   /// discretization of a stochastic process over a given time interval
   /// </summary>
   public interface IDiscretization
   {
      Vector drift(StochasticProcess sp, double t0, Vector x0, double dt);
      Matrix diffusion(StochasticProcess sp, double t0, Vector x0, double dt);
      Matrix covariance(StochasticProcess sp, double t0, Vector x0, double dt);
   }

   /// <summary>
   /// discretization of a 1D stochastic process over a given time interval
   /// </summary>
   public interface IDiscretization1D
   {
      double drift(StochasticProcess1D sp, double t0, double x0, double dt);
      double diffusion(StochasticProcess1D sp, double t0, double x0, double dt);
      double variance(StochasticProcess1D sp, double t0, double x0, double dt);
   }

   /// <summary>
   /// multi-dimensional stochastic process class.
   /// </summary>
   /// <remarks>
   /// This class describes a stochastic process governed by
   /// \f[
   /// d\mathrm{x}_t = \mu(t, x_t)\mathrm{d}t
   /// + \sigma(t, \mathrm{x}_t) \cdot d\mathrm{W}_t.
   /// \f]
   /// </remarks>
   public abstract class StochasticProcess : IObservable, IObserver
   {
      protected IDiscretization discretization_;

      protected StochasticProcess() { }
      protected StochasticProcess(IDiscretization disc)
      {
         discretization_ = disc;
      }

      // Stochastic process interface
      /// <summary>
      /// Returns the number of dimensions of the stochastic process.
      /// </summary>
      public abstract int size();

      /// <summary>
      /// Returns the number of independent factors of the process.
      /// </summary>
      public virtual int factors() { return size(); }

      /// <summary>
      /// Returns the initial values of the state variables.
      /// </summary>
      public abstract Vector initialValues();

      /// <summary>
      /// Returns the drift term of the process.
      /// </summary>
      public abstract Vector drift(double t, Vector x);

      /// <summary>
      /// Returns the diffusion term of the process.
      /// </summary>
      public abstract Matrix diffusion(double t, Vector x);

      /// <summary>
      /// Returns the expected state after the given time increment.
      /// </summary>
      /// <remarks>
      /// Derived classes can override this method to hard-code a specific discretization.
      /// </remarks>
      public virtual Vector expectation(double t0, Vector x0, double dt)
      {
         return apply(x0, discretization_.drift(this, t0, x0, dt));
      }

      /// <summary>
      /// Returns the standard deviation over the given time increment.
      /// </summary>
      /// <remarks>
      /// Derived classes can override this method to hard-code a specific discretization.
      /// </remarks>
      public virtual Matrix stdDeviation(double t0, Vector x0, double dt)
      {
         return discretization_.diffusion(this, t0, x0, dt);
      }

      /// <summary>
      /// Returns the covariance over the given time increment.
      /// </summary>
      /// <remarks>
      /// Derived classes can override this method to hard-code a specific discretization.
      /// </remarks>
      public virtual Matrix covariance(double t0, Vector x0, double dt)
      {
         return discretization_.covariance(this, t0, x0, dt);
      }

      // returns the asset value after a time interval
      public virtual Vector evolve(double t0, Vector x0, double dt, Vector dw)
      {
         return apply(expectation(t0, x0, dt), stdDeviation(t0, x0, dt) * dw);
      }

      // applies a change to the asset value.
      public virtual Vector apply(Vector x0, Vector dx)
      {
         return x0 + dx;
      }

      // utilities
      /// <summary>
      /// Returns the time corresponding to the given date in the reference system of the process.
      /// </summary>
      /// <remarks>
      /// The default implementation throws because some processes do not support date-to-time conversion.
      /// </remarks>
      public virtual double time(Date d)
      {
         throw new NotSupportedException("date/time conversion not supported");
      }


      #region Observer & Observable
      // Subjects, i.e. observables, should define interface internally like follows.
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

      public virtual void update()
      {
         notifyObservers();
      }
      #endregion
   }

   /// <summary>
   /// 1-dimensional stochastic process
   /// </summary>
   public abstract class StochasticProcess1D : StochasticProcess
   {
      protected new IDiscretization1D discretization_;

      protected StochasticProcess1D() {}
      protected StochasticProcess1D(IDiscretization1D disc)
      {
         discretization_ = disc;
      }

      // 1-D stochastic process interface
      /// <summary>
      /// Returns the initial value of the state variable.
      /// </summary>
      public abstract double x0();

      /// <summary>
      /// Returns the drift term of the process.
      /// </summary>
      public abstract double drift(double t, double x);
      public override Vector drift(double t, Vector x)
      {
#if QL_EXTRA_SAFETY_CHECKS
         QL_REQUIRE(x.size() == 1, () => "1-D array required");
#endif
         Vector a = new Vector(1, drift(t, x[0]));
         return a;
      }

      /// <summary>
      /// Returns the diffusion term of the process.
      /// </summary>
      public abstract double diffusion(double t, double x);
      public override Matrix diffusion(double t, Vector x)
      {
#if QL_EXTRA_SAFETY_CHECKS
         QL_REQUIRE(x.size() == 1, () => "1-D array required");
#endif
         Matrix m = new Matrix(1, 1, diffusion(t, x[0]));
         return m;
      }

      /// <summary>
      /// Returns the expected state after the given time increment.
      /// </summary>
      /// <remarks>
      /// Derived classes can override this method to hard-code a specific discretization.
      /// </remarks>
      public virtual double expectation(double t0, double x0, double dt)
      {
         return apply(x0, discretization_.drift(this, t0, x0, dt));
      }
      public override Vector expectation(double t0, Vector x0, double dt)
      {
#if QL_EXTRA_SAFETY_CHECKS
         QL_REQUIRE(x0.size() == 1, () => "1-D array required");
#endif
         Vector a = new Vector(1, expectation(t0, x0[0], dt));
         return a;
      }

      /// <summary>
      /// Returns the standard deviation over the given time increment.
      /// </summary>
      /// <remarks>
      /// Derived classes can override this method to hard-code a specific discretization.
      /// </remarks>
      public virtual double stdDeviation(double t0, double x0, double dt)
      {
         return discretization_.diffusion(this, t0, x0, dt);
      }
      public override Matrix stdDeviation(double t0, Vector x0, double dt)
      {
#if QL_EXTRA_SAFETY_CHECKS
         QL_REQUIRE(x0.size() == 1, () => "1-D array required");
#endif
         Matrix m = new Matrix(1, 1, stdDeviation(t0, x0[0], dt));
         return m;
      }

      /// <summary>
      /// Returns the variance over the given time increment.
      /// </summary>
      /// <remarks>
      /// Derived classes can override this method to hard-code a specific discretization.
      /// </remarks>
      public virtual double variance(double t0, double x0, double dt)
      {
         return discretization_.variance(this, t0, x0, dt);
      }
      public virtual Matrix variance(double t0, Vector x0, double dt)
      {
#if QL_EXTRA_SAFETY_CHECKS
         QL_REQUIRE(x0.size() == 1, () => "1-D array required");
#endif
         Matrix m = new Matrix(1, 1, variance(t0, x0[0], dt));
         return m;
      }

      // returns the asset value after a time interval.
      public virtual double evolve(double t0, double x0, double dt, double dw)
      {
         return apply(expectation(t0, x0, dt), stdDeviation(t0, x0, dt) * dw);
      }
      public virtual Vector evolve(double t0, ref Vector x0, double dt, ref Vector dw)
      {
#if QL_EXTRA_SAFETY_CHECKS
         QL_REQUIRE(x0.size() == 1, () => "1-D array required");
         QL_REQUIRE(dw.size() == 1, () => "1-D array required");
#endif
         Vector a = new Vector(1, evolve(t0, x0[0], dt, dw[0]));
         return a;
      }

      // applies a change to the asset value.
      public virtual double apply(double x0, double dx) { return x0 + dx; }
      public virtual Vector apply(ref Vector x0, ref Vector dx)
      {
#if QL_EXTRA_SAFETY_CHECKS
         QL_REQUIRE(x0.size() == 1, () => "1-D array required");
         QL_REQUIRE(dx.size() == 1, () => "1-D array required");
#endif
         Vector a = new Vector(1, apply(x0[0], dx[0]));
         return a;
      }

      /// <summary>
      /// Returns the initial values of the state variables.
      /// </summary>
      public override Vector initialValues()
      {
         Vector a = new Vector(1, x0());
         return a;
      }
      public override int size() { return 1; }
   }
}
