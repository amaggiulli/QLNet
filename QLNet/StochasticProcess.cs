/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
  
 This file is part of QLNet Project http://qlnet.sourceforge.net/

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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QLNet {
    //! discretization of a stochastic process over a given time interval
    public interface IDiscretization {
        Vector drift(StochasticProcess sp, double t0, Vector x0, double dt);
        Matrix diffusion(StochasticProcess sp, double t0, Vector x0, double dt);
        Matrix covariance(StochasticProcess sp, double t0, Vector x0, double dt);
    }

    //! discretization of a 1D stochastic process over a given time interval
    public interface IDiscretization1D {
        double drift(StochasticProcess1D sp, double t0, double x0, double dt);
        double diffusion(StochasticProcess1D sp, double t0, double x0, double dt);
        double variance(StochasticProcess1D sp, double t0, double x0, double dt);
    }

    //! multi-dimensional stochastic process class.
    /*! This class describes a stochastic process governed by
        \f[
        d\mathrm{x}_t = \mu(t, x_t)\mathrm{d}t
                      + \sigma(t, \mathrm{x}_t) \cdot d\mathrm{W}_t.
        \f]
    */
    public abstract class StochasticProcess : IObservable, IObserver {
        protected IDiscretization discretization_;

        protected StochasticProcess() { }
        protected StochasticProcess(IDiscretization disc) {
            discretization_ = disc;
        }
   
        //! \name Stochastic process interface
        //! returns the number of dimensions of the stochastic process
        public abstract int size();

        //! returns the number of independent factors of the process
        public virtual int factors() { return size(); }

        //! returns the initial values of the state variables
        public abstract Vector initialValues();

        /*! \brief returns the drift part of the equation, i.e.,
                   \f$ \mu(t, \mathrm{x}_t) \f$ */
        public abstract Vector drift(double t, Vector x);

        /*! \brief returns the diffusion part of the equation, i.e.
                   \f$ \sigma(t, \mathrm{x}_t) \f$ */
        public abstract Matrix diffusion(double t, Vector x);

        /*! returns the expectation
            \f$ E(\mathrm{x}_{t_0 + \Delta t}
                | \mathrm{x}_{t_0} = \mathrm{x}_0) \f$
            of the process after a time interval \f$ \Delta t \f$
            according to the given discretization. This method can be
            overridden in derived classes which want to hard-code a
            particular discretization.
        */
        public virtual Vector expectation(double t0, Vector x0, double dt) {
            return apply(x0, discretization_.drift(this, t0, x0, dt));
        }

        /*! returns the standard deviation
            \f$ S(\mathrm{x}_{t_0 + \Delta t}
                | \mathrm{x}_{t_0} = \mathrm{x}_0) \f$
            of the process after a time interval \f$ \Delta t \f$
            according to the given discretization. This method can be
            overridden in derived classes which want to hard-code a
            particular discretization.
        */
        public virtual Matrix stdDeviation(double t0, Vector x0, double dt) {
            return discretization_.diffusion(this, t0, x0, dt);
        }

        /*! returns the covariance
            \f$ V(\mathrm{x}_{t_0 + \Delta t}
                | \mathrm{x}_{t_0} = \mathrm{x}_0) \f$
            of the process after a time interval \f$ \Delta t \f$
            according to the given discretization. This method can be
            overridden in derived classes which want to hard-code a
            particular discretization.
        */
        public virtual Matrix covariance(double t0, Vector x0, double dt) {
            return discretization_.covariance(this, t0, x0, dt);
        }

        /*! returns the asset value after a time interval \f$ \Delta t
            \f$ according to the given discretization. By default, it
            returns
            \f[
            E(\mathrm{x}_0,t_0,\Delta t) +
            S(\mathrm{x}_0,t_0,\Delta t) \cdot \Delta \mathrm{w}
            \f]
            where \f$ E \f$ is the expectation and \f$ S \f$ the
            standard deviation.
        */
        public virtual Vector evolve(double t0, Vector x0, double dt, Vector dw) {
            return apply(expectation(t0, x0, dt), stdDeviation(t0, x0, dt) * dw);
        }

        /*! applies a change to the asset value. By default, it
            returns \f$ \mathrm{x} + \Delta \mathrm{x} \f$.
        */
        public virtual Vector apply(Vector x0, Vector dx) {
            return x0 + dx;
        }

        //! \name utilities
        //@{
        /*! returns the time value corresponding to the given date
            in the reference system of the stochastic process.

            \note As a number of processes might not need this
                  functionality, a default implementation is given
                  which raises an exception.
        */
        public virtual double time(Date d) {
            throw new NotSupportedException("date/time conversion not supported");
        }


        #region Observer & Observable
		// Subjects, i.e. observables, should define interface internally like follows.
        public event Callback notifyObserversEvent;
        // this method is required for calling from derived classes
        protected void notifyObservers() {
            Callback handler = notifyObserversEvent;
            if (handler != null) {
                handler();
            }
        }
        public void registerWith(Callback handler) { notifyObserversEvent += handler; }
        public void unregisterWith(Callback handler) { notifyObserversEvent -= handler; }

        public virtual void update() {
            notifyObservers();
        }
	    #endregion   
    }

    //! 1-dimensional stochastic process
    /*! This class describes a stochastic process governed by
        \f[
            dx_t = \mu(t, x_t)dt + \sigma(t, x_t)dW_t.
        \f]
    */
    public abstract class StochasticProcess1D : StochasticProcess {
        new protected IDiscretization1D discretization_;

        protected StochasticProcess1D() {}
        protected StochasticProcess1D(IDiscretization1D disc) {
            discretization_ = disc;
        }

        //! \name 1-D stochastic process interface
        //@{
        //! returns the initial value of the state variable
        public abstract double x0();

        //! returns the drift part of the equation, i.e. \f$ \mu(t, x_t) \f$
        public abstract double drift(double t, double x);
        public override Vector drift(double t, Vector x) {
            #if QL_EXTRA_SAFETY_CHECKS
            QL_REQUIRE(x.size() == 1, () => "1-D array required");
            #endif
            Vector a = new Vector(1, drift(t, x[0]));
            return a;
        }

        /*! \brief returns the diffusion part of the equation, i.e.
            \f$ \sigma(t, x_t) \f$
        */
        public abstract double diffusion(double t, double x);
        public override Matrix diffusion(double t, Vector x) {
            #if QL_EXTRA_SAFETY_CHECKS
            QL_REQUIRE(x.size() == 1, () => "1-D array required");
            #endif
            Matrix m = new Matrix(1, 1, diffusion(t, x[0]));
            return m;
        }

        /*! returns the expectation
            \f$ E(x_{t_0 + \Delta t} | x_{t_0} = x_0) \f$
            of the process after a time interval \f$ \Delta t \f$
            according to the given discretization. This method can be
            overridden in derived classes which want to hard-code a
            particular discretization.
        */
        public virtual double expectation(double t0, double x0, double dt) {
            return apply(x0, discretization_.drift(this, t0, x0, dt));
        }
        public override Vector expectation(double t0, Vector x0, double dt) {
            #if QL_EXTRA_SAFETY_CHECKS
            QL_REQUIRE(x0.size() == 1, () => "1-D array required");
            #endif
            Vector a = new Vector(1, expectation(t0, x0[0], dt));
            return a;
        }

        /*! returns the standard deviation
            \f$ S(x_{t_0 + \Delta t} | x_{t_0} = x_0) \f$
            of the process after a time interval \f$ \Delta t \f$
            according to the given discretization. This method can be
            overridden in derived classes which want to hard-code a
            particular discretization.
        */
        public virtual double stdDeviation(double t0, double x0, double dt) {
            return discretization_.diffusion(this, t0, x0, dt);
        }
        public override Matrix stdDeviation(double t0, Vector x0, double dt) {
            #if QL_EXTRA_SAFETY_CHECKS
            QL_REQUIRE(x0.size() == 1, () => "1-D array required");
            #endif
            Matrix m = new Matrix(1, 1, stdDeviation(t0, x0[0], dt));
            return m;
        }

        /*! returns the variance
            \f$ V(x_{t_0 + \Delta t} | x_{t_0} = x_0) \f$
            of the process after a time interval \f$ \Delta t \f$
            according to the given discretization. This method can be
            overridden in derived classes which want to hard-code a
            particular discretization.
        */
        public virtual double variance(double t0, double x0, double dt) {
            return discretization_.variance(this, t0, x0, dt);
        }
        public virtual Matrix variance(double t0, Vector x0, double dt) {
            #if QL_EXTRA_SAFETY_CHECKS
            QL_REQUIRE(x0.size() == 1, () => "1-D array required");
            #endif
            Matrix m = new Matrix(1, 1, variance(t0, x0[0], dt));
            return m;
        }

        /*! returns the asset value after a time interval \f$ \Delta t
            \f$ according to the given discretization. By default, it
            returns
            \f[
            E(x_0,t_0,\Delta t) + S(x_0,t_0,\Delta t) \cdot \Delta w
            \f]
            where \f$ E \f$ is the expectation and \f$ S \f$ the
            standard deviation.
        */
        public virtual double evolve(double t0, double x0, double dt, double dw) {
            return apply(expectation(t0, x0, dt), stdDeviation(t0, x0, dt) * dw);
        }
        public virtual Vector evolve(double t0, ref Vector x0, double dt, ref Vector dw) {
            #if QL_EXTRA_SAFETY_CHECKS
            QL_REQUIRE(x0.size() == 1, () => "1-D array required");
            QL_REQUIRE(dw.size() == 1, () => "1-D array required");
            #endif
            Vector a = new Vector(1, evolve(t0, x0[0], dt, dw[0]));
            return a;
        }

        /*! applies a change to the asset value. By default, it
            returns \f$ x + \Delta x \f$.
        */
        public virtual double apply(double x0, double dx) { return x0 + dx; }
        public virtual Vector apply(ref Vector x0, ref Vector dx) {
            #if QL_EXTRA_SAFETY_CHECKS
            QL_REQUIRE(x0.size() == 1, () => "1-D array required");
            QL_REQUIRE(dx.size() == 1, () => "1-D array required");
            #endif
            Vector a = new Vector(1, apply(x0[0], dx[0]));
            return a;
        }

        //! returns the initial values of the state variables
        public override Vector initialValues() {
            Vector a = new Vector(1, x0());
            return a;
        }
        public override int size() { return 1; }
    }
}
