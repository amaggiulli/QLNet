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
    //! Euler discretization for stochastic processes
    public class EulerDiscretization : IDiscretization, IDiscretization1D {
        /*! Returns an approximation of the drift defined as
            \f$ \mu(t_0, \mathbf{x}_0) \Delta t \f$. */
        public Vector drift(StochasticProcess process, double t0, Vector x0, double dt) {
            return process.drift(t0, x0)*dt;
        }

        /*! Returns an approximation of the drift defined as
            \f$ \mu(t_0, x_0) \Delta t \f$. */
        public double drift(StochasticProcess1D process, double t0, double x0, double dt)  {
            return process.drift(t0, x0)*dt;
        }

        /*! Returns an approximation of the diffusion defined as
            \f$ \sigma(t_0, \mathbf{x}_0) \sqrt{\Delta t} \f$. */
        public Matrix diffusion(StochasticProcess process, double t0, Vector x0, double dt)  {
            return process.diffusion(t0, x0) * Math.Sqrt(dt);
        }

        /*! Returns an approximation of the diffusion defined as
            \f$ \sigma(t_0, x_0) \sqrt{\Delta t} \f$. */
        public double diffusion(StochasticProcess1D process, double t0, double x0, double dt) {
            return process.diffusion(t0, x0) * Math.Sqrt(dt);
        }

        /*! Returns an approximation of the covariance defined as
            \f$ \sigma(t_0, \mathbf{x}_0)^2 \Delta t \f$. */
        public Matrix covariance(StochasticProcess process, double t0, Vector x0, double dt) {
            Matrix sigma = process.diffusion(t0, x0);
            Matrix result = sigma * Matrix.transpose(sigma) * dt;
            return result;
        }

        /*! Returns an approximation of the variance defined as
            \f$ \sigma(t_0, x_0)^2 \Delta t \f$. */
        public double variance(StochasticProcess1D process, double t0, double x0, double dt) {
            double sigma = process.diffusion(t0, x0);
            return sigma*sigma*dt;
        }
    }
}
