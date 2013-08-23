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
    public abstract class PdeSecondOrderParabolic {
        public abstract double diffusion(double t, double x);
        public abstract double drift(double t, double x);
        public abstract double discount(double t, double x);
        public abstract PdeSecondOrderParabolic factory(GeneralizedBlackScholesProcess process);

        public void generateOperator(double t, TransformedGrid tg, TridiagonalOperator L) {
            for (int i=1; i < tg.size() - 1; i++) {
                double sigma = diffusion(t, tg.grid(i));
                double nu = drift(t, tg.grid(i));
                double r = discount(t, tg.grid(i));
                double sigma2 = sigma * sigma;

                double pd = -(sigma2/tg.dxm(i)-nu)/ tg.dx(i);
                double pu = -(sigma2/tg.dxp(i)+nu)/ tg.dx(i);
                double pm = sigma2/(tg.dxm(i) * tg.dxp(i))+r;
                L.setMidRow(i, pd,pm,pu);
            }
        }
    }


    public class PdeConstantCoeff<PdeClass> : PdeSecondOrderParabolic where PdeClass : PdeSecondOrderParabolic, new() {
        private double diffusion_;
        private double drift_;
        private double discount_;

        public PdeConstantCoeff(GeneralizedBlackScholesProcess process, double t, double x) {
            PdeClass pde = (PdeClass)new PdeClass().factory(process);
            diffusion_ = pde.diffusion(t, x);
            drift_ = pde.drift(t, x);
            discount_ = pde.discount(t, x);
        }

        public override double diffusion(double x, double y) { return diffusion_; }
        public override double drift(double x, double y) { return drift_; }
        public override double discount(double x, double y) { return discount_; }
        public override PdeSecondOrderParabolic factory(GeneralizedBlackScholesProcess process) {
            throw new NotSupportedException();
        }
    }


    public class GenericTimeSetter<PdeClass> : TridiagonalOperator.TimeSetter where PdeClass : PdeSecondOrderParabolic, new() {
        // typedef LogGrid grid_type;
        private LogGrid grid_;
        private PdeClass pde_;

        public GenericTimeSetter(Vector grid, GeneralizedBlackScholesProcess process) {
            grid_ = new LogGrid(grid);
            pde_ = (PdeClass)new PdeClass().factory(process);
        }

        public override void setTime(double t, IOperator L) {
            pde_.generateOperator(t, grid_, (TridiagonalOperator)L);
        }
    }


    public class PdeOperator<PdeClass> : TridiagonalOperator where PdeClass : PdeSecondOrderParabolic, new() {
        public PdeOperator(Vector grid, GeneralizedBlackScholesProcess process) : this(grid, process, 0) { }
        public PdeOperator(Vector grid, GeneralizedBlackScholesProcess process, double residualTime)
            : base(grid.size()) {
            timeSetter_ = new GenericTimeSetter<PdeClass>(grid, process);
            setTime(residualTime);
        }
    }

}
