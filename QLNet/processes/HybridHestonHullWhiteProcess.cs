//  Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)
//  
//  This file is part of QLNet Project https://github.com/amaggiulli/qlnet
//  QLNet is free software: you can redistribute it and/or modify it
//  under the terms of the QLNet license.  You should have received a
//  copy of the license along with this program; if not, license is  
//  available online at <http://qlnet.sourceforge.net/License.html>.
//   
//  QLNet is a based on QuantLib, a free-software/open-source library
//  for financial quantitative analysts and developers - http://quantlib.org/
//  The QuantLib license is available online at http://quantlib.org/license.shtml.
//  
//  This program is distributed in the hope that it will be useful, but WITHOUT
//  ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
//  FOR A PARTICULAR PURPOSE.  See the license for more details.
using System;

namespace QLNet
{
   //! Hybrid Heston Hull-White stochastic process
   /*! This class implements a three factor Heston Hull-White model

       \bug This class was not tested enough to guarantee
            its functionality... work in progress

       \ingroup processes
   */
   public class HybridHestonHullWhiteProcess : StochasticProcess
   {
      public enum Discretization { Euler, BSMHullWhite };

      public HybridHestonHullWhiteProcess( HestonProcess hestonProcess,
                                           HullWhiteForwardProcess hullWhiteProcess,
                                           double corrEquityShortRate,
                                           Discretization discretization = Discretization.BSMHullWhite)
      {
         hestonProcess_ = hestonProcess;
         hullWhiteProcess_ = hullWhiteProcess;
         hullWhiteModel_ = new HullWhite(hestonProcess.riskFreeRate(),
                                         hullWhiteProcess.a(),
                                         hullWhiteProcess.sigma());
         corrEquityShortRate_ = corrEquityShortRate;
         disc_ = discretization;
         maxRho_ = Math.Sqrt(1 - hestonProcess.rho()*hestonProcess.rho())
                 - Math.Sqrt(Const.QL_EPSILON) /* reserve for rounding errors */;

         T_ = hullWhiteProcess.getForwardMeasureTime();
         endDiscount_ = hestonProcess.riskFreeRate().link.discount(T_);

         Utils.QL_REQUIRE( corrEquityShortRate * corrEquityShortRate
                    + hestonProcess.rho() * hestonProcess.rho() <= 1.0,()=>
                    "correlation matrix is not positive definite" );

         Utils.QL_REQUIRE( hullWhiteProcess.sigma() > 0.0,()=>
                    "positive vol of Hull White process is required" );
      }

      public override int size() { return 3; }
      public override Vector initialValues()
      {
         Vector retVal = new Vector(3);
         retVal[0] = hestonProcess_.s0().link.value();
         retVal[1] = hestonProcess_.v0();
         retVal[2] = hullWhiteProcess_.x0();
         
         return retVal;
      }
      public override Vector drift(double t, Vector x)
      {
         Vector retVal = new Vector(3), x0 = new Vector(2);
        
         x0[0] = x[0]; x0[1] = x[1];
         Vector y0 = hestonProcess_.drift(t, x0);
        
         retVal[0] = y0[0]; retVal[1] = y0[1];
         retVal[2] = hullWhiteProcess_.drift(t, x[2]);
        
         return retVal;
      }
      public override Matrix diffusion(double t, Vector x)
      {
         Matrix retVal = new Matrix(3,3);

         Vector xt = new Vector(2); 
         xt[0] = x[0]; 
         xt[1] = x[1];
         Matrix m = hestonProcess_.diffusion(t, xt);
         retVal[0,0] = m[0,0]; retVal[0,1] = 0.0;    retVal[0,2] = 0.0;
         retVal[1,0] = m[1,0]; retVal[1,1] = m[1,1]; retVal[1,2] = 0.0;
        
         double sigma = hullWhiteProcess_.sigma();
         retVal[2,0] = corrEquityShortRate_ * sigma;
         retVal[2,1] = - retVal[2,0]*retVal[1,0] / retVal[1,1];
         retVal[2,2] = Math.Sqrt( sigma*sigma - retVal[2,1]*retVal[2,1] 
                                              - retVal[2,0]*retVal[2,0] );
        
         return retVal;
      }
      public override Vector apply(Vector x0, Vector dx)
      {
         Vector retVal = new Vector(3), xt= new Vector(2), dxt = new Vector(2);
        
         xt[0]  = x0[0]; xt[1]  = x0[1];
         dxt[0] = dx[0]; dxt[1] = dx[1];

         Vector yt = hestonProcess_.apply( xt, dxt );
        
         retVal[0] = yt[0]; retVal[1] = yt[1];
         retVal[2] = hullWhiteProcess_.apply(x0[2], dx[2]);
        
         return retVal;
      }

      public override Vector evolve(double t0, Vector x0,double dt, Vector dw)
      {
         double r = x0[2];
         double a = hullWhiteProcess_.a();
         double sigma = hullWhiteProcess_.sigma();
         double rho = corrEquityShortRate_;
         double xi = hestonProcess_.rho();
         double eta = (x0[1] > 0.0) ? Math.Sqrt(x0[1]) : 0.0;
         double s = t0;
         double t = t0 + dt;
         double T = T_;
         double dy = hestonProcess_.dividendYield().link.forwardRate(s, t, Compounding.Continuous,Frequency.NoFrequency).value();

         double df = Math.Log(hestonProcess_.riskFreeRate().link.discount(t)
                             /hestonProcess_.riskFreeRate().link.discount(s));

         double eaT = Math.Exp(-a*T);
         double eat = Math.Exp(-a*t);
         double eas = Math.Exp(-a*s);
         double iat = 1.0/eat;
         double ias = 1.0/eas;

         double m1 = -(dy + 0.5*eta*eta)*dt - df;

         double m2 = -rho*sigma*eta/a*(dt - 1/a*eaT*(iat - ias));

         double m3 = (r - hullWhiteProcess_.alpha(s))
                         *hullWhiteProcess_.B(s, t);

         double m4 = sigma*sigma/(2*a*a) *(dt + 2/a*(eat - eas) - 1/(2*a)*(eat*eat - eas*eas));

         double m5 = -sigma*sigma/(a*a) *(dt - 1/a*(1 - eat*ias) - 1/(2*a)*eaT*(iat - 2*ias + eat*ias*ias));

         double mu = m1 + m2 + m3 + m4 + m5;

         Vector retVal= new Vector(3);

         double eta2 = hestonProcess_.sigma()*eta;
         double nu = hestonProcess_.kappa()*(hestonProcess_.theta() - eta*eta);

         retVal[1] = x0[1] + nu*dt + eta2*Math.Sqrt(dt)
                     *(xi*dw[0] + Math.Sqrt(1 - xi*xi)*dw[1]);

         if (disc_ == Discretization.BSMHullWhite)
         {
            double v1 = eta*eta*dt + sigma*sigma/(a*a)*(dt - 2/a*(1 - eat*ias) 
                                   + 1/(2*a)*(1 - eat*eat*ias*ias))
                                   + 2*sigma*eta/a*rho*(dt - 1/a*(1 - eat*ias));
            double v2 = hullWhiteProcess_.variance(t0, r, dt);
            double v12 = (1 - eat*ias)*(sigma*eta/a*rho + sigma*sigma/(a*a))
                             - sigma*sigma/(2*a*a)*(1 - eat*eat*ias*ias);

            Utils.QL_REQUIRE(v1 > 0.0 && v2 > 0.0,()=> "zero or negative variance given");

            // terminal rho must be between -maxRho and +maxRho
            double rhoT = Math.Min(maxRho_, Math.Max(-maxRho_, v12/Math.Sqrt(v1*v2)));
            Utils.QL_REQUIRE(rhoT <= 1.0 && rhoT >= -1.0
                       && 1 - rhoT*rhoT/(1 - xi*xi) >= 0.0,()=> "invalid terminal correlation");

            double dw_0 = dw[0];
            double dw_2 = rhoT*dw[0] - rhoT*xi/Math.Sqrt(1 - xi*xi)*dw[1]
                              + Math.Sqrt(1 - rhoT*rhoT/(1 - xi*xi))*dw[2];

            retVal[2] = hullWhiteProcess_.evolve(t0, r, dt, dw_2);

            double vol = Math.Sqrt(v1)*dw_0;
            retVal[0] = x0[0]*Math.Exp(mu + vol);
         }
         else if (disc_ == Discretization.Euler)
         {
            double dw_2 = rho*dw[0] - rho*xi/Math.Sqrt(1 - xi*xi)*dw[1]
                              + Math.Sqrt(1 - rho*rho/(1 - xi*xi))*dw[2];

            retVal[2] = hullWhiteProcess_.evolve(t0, r, dt, dw_2);

            double vol = eta*Math.Sqrt(dt)*dw[0];
            retVal[0] = x0[0]*Math.Exp(mu + vol);
         }
         else
            Utils.QL_FAIL("unknown discretization scheme");

         return retVal;
      }

      public double numeraire(double t, Vector x)
      {
         return hullWhiteModel_.discountBond( t, T_, x[2] ) / endDiscount_;
      }

      public HestonProcess hestonProcess() { return hestonProcess_; }
      public HullWhiteForwardProcess hullWhiteProcess() { return hullWhiteProcess_; }

      public double eta() { return corrEquityShortRate_; }
      public override double time( Date date ) { return hestonProcess_.time( date ); }
      public Discretization discretization() { return disc_; }
      public override void update() { endDiscount_ = hestonProcess_.riskFreeRate().link.discount( T_ ); }

      protected HestonProcess hestonProcess_;
      protected HullWhiteForwardProcess hullWhiteProcess_;
        
      //model is used to calculate P(t,T)
      protected HullWhite hullWhiteModel_;

      protected double corrEquityShortRate_;
      protected Discretization disc_;
      protected double maxRho_;
      protected double T_;
      protected double endDiscount_;

   }
}
