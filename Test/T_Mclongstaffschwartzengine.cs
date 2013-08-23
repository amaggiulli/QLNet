using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QLNet;

namespace TestSuite {
    class T_Mclongstaffschwartzengine {

        class AmericanMaxPathPricer : IEarlyExercisePathPricer<MultiPath, Vector> {
            protected Payoff payoff_;

            public AmericanMaxPathPricer(Payoff payoff) {
                payoff_ = payoff;
            }

            public Vector state(MultiPath path, int t) {
                Vector tmp = new Vector(path.assetNumber());
                for (int i = 0; i < path.assetNumber(); ++i) {
                    tmp[i] = path[i][t];
                }
                return tmp;
            }

            public double value(MultiPath path, int t) {
                Vector tmp = (Vector)state(path, t);
                return payoff_.value(tmp.Max());
            }

            public List<Func<Vector, double>> basisSystem() {
                return LsmBasisSystem.multiPathBasisSystem(2, 2, LsmBasisSystem.PolynomType.Monomial);
            }
        }

        //class MCAmericanMaxEngine<RNG> : MCLongstaffSchwartzEngine<VanillaOption.Engine, MultiVariate, RNG>
        //    where RNG : IRSG, new() {

        //    //public MCAmericanMaxEngine(StochasticProcessArray processes, int timeSteps, int timeStepsPerYear,
        //    //                           bool brownianbridge, bool antitheticVariate, bool controlVariate,
        //    //                           int requiredSamples, double requiredTolerance, int maxSamples,
        //    //                           ulong seed, int nCalibrationSamples = Null<Size>())
        //    public MCAmericanMaxEngine(StochasticProcessArray processes, int timeSteps, int timeStepsPerYear,
        //                               bool brownianbridge, bool antitheticVariate, bool controlVariate,
        //                               int requiredSamples, double requiredTolerance, int maxSamples,
        //                               ulong seed, int nCalibrationSamples)
        //        : base(processes, timeSteps, timeStepsPerYear, brownianbridge, antitheticVariate, controlVariate,
        //               requiredSamples, requiredTolerance, maxSamples, seed, nCalibrationSamples)
        //    { }

        //    protected override LongstaffSchwartzPathPricer<IPath> lsmPathPricer() {
        //        StochasticProcessArray processArray = process_ as StochasticProcessArray;
        //        if (processArray == null || processArray.size() == 0)
        //            throw new ApplicationException("Stochastic process array required");

        //        GeneralizedBlackScholesProcess process = processArray.process(0) as GeneralizedBlackScholesProcess;
        //        if (process == null)
        //            throw new ApplicationException("generalized Black-Scholes proces required");

        //        AmericanMaxPathPricer earlyExercisePathPricer = new AmericanMaxPathPricer(arguments_.payoff);

        //        return new LongstaffSchwartzPathPricer<IPath>(timeGrid(), earlyExercisePathPricer,
        //                                                         process.riskFreeRate().currentLink());
        //    }
        //}
    }
}
