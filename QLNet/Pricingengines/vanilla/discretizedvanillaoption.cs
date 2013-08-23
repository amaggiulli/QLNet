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
    public class DiscretizedVanillaOption : DiscretizedAsset {
        private VanillaOption.Arguments arguments_;
        private List<double> stoppingTimes_;

        public DiscretizedVanillaOption(VanillaOption.Arguments args, StochasticProcess process, TimeGrid grid) {
            arguments_ = args;

            stoppingTimes_ = new InitializedList<double>(args.exercise.dates().Count);
            for (int i=0; i<stoppingTimes_.Count; ++i) {
                stoppingTimes_[i] = process.time(args.exercise.date(i));
                if (!grid.empty()) {
                    // adjust to the given grid
                    stoppingTimes_[i] = grid.closestTime(stoppingTimes_[i]);
                }
            }
        }

        public override void reset(int size) {
            values_ = new Vector(size, 0.0);
            adjustValues();
        }

        public override List<double> mandatoryTimes() {
            return stoppingTimes_;
        }

        protected override void postAdjustValuesImpl() {
            double now = time();
            switch (arguments_.exercise.type()) {
                case Exercise.Type.American:
                    if (now <= stoppingTimes_[1] && now >= stoppingTimes_[0])
                        applySpecificCondition();
                    break;
                case Exercise.Type.European:
                    if (isOnTime(stoppingTimes_[0]))
                        applySpecificCondition();
                    break;
                case Exercise.Type.Bermudan:
                    for (int i=0; i<stoppingTimes_.Count; i++) {
                        if (isOnTime(stoppingTimes_[i]))
                            applySpecificCondition();
                    }
                    break;
                default:
                    throw new ApplicationException("invalid option type");
            }
        }

        private void applySpecificCondition() {
            Vector grid = method().grid(time());
            for (int j=0; j<values_.size(); j++) {
                values_[j] = Math.Max(values_[j], arguments_.payoff.value(grid[j]));
            }
        }
    }
}
