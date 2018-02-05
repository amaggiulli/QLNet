/*
 Copyright (C) 2017 Jean-Camille Tournier (jean-camille.tournier@avivainvestors.com)

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
using System.Collections.Generic;
using System.Linq;

namespace QLNet
{
   public class FdmStepConditionComposite : IStepCondition<Vector>
   {
      public FdmStepConditionComposite()
      {
         conditions_ = new List<IStepCondition<Vector>>();
         stoppingTimes_ = new List<double>();
      }
      public FdmStepConditionComposite(List<List<double>> stoppingTimes, List<IStepCondition<Vector>> conditions)
      {
         conditions_ = conditions;

         List<double> allStoppingTimes = new List<double>();
         foreach (List<double> iter in stoppingTimes)
         {
            foreach (double t in iter)
               allStoppingTimes.Add(t);
         }
         stoppingTimes_ = allStoppingTimes.Distinct().OrderBy(x => x).ToList();
      }

      public void applyTo(object o, double t)
      {
         foreach (IStepCondition<Vector> iter in conditions_)
            iter.applyTo(o, t);
      }

      public static FdmStepConditionComposite joinConditions(FdmSnapshotCondition c1,
                                                             FdmStepConditionComposite c2)
      {
         List<List<double>> stoppingTimes = new List<List<double>>();
         stoppingTimes.Add(c2.stoppingTimes());
         stoppingTimes.Add(new InitializedList<double>(1, c1.getTime()));

         List<IStepCondition<Vector>> conditions = new List<IStepCondition<Vector>>();
         conditions.Add(c2);
         conditions.Add(c1);

         return new FdmStepConditionComposite(stoppingTimes, conditions);
      }

      public static FdmStepConditionComposite vanillaComposite(DividendSchedule cashFlow,
                                                               Exercise exercise,
                                                               FdmMesher mesher,
                                                               FdmInnerValueCalculator calculator,
                                                               Date refDate,
                                                               DayCounter dayCounter)
      {
         List<List<double>> stoppingTimes = new List<List<double>>();
         List<IStepCondition<Vector>> stepConditions = new List<IStepCondition<Vector>>();

         if (!cashFlow.empty())
         {
            FdmDividendHandler dividendCondition =
               new FdmDividendHandler(cashFlow, mesher,
                                      refDate, dayCounter, 0);

            stepConditions.Add(dividendCondition);
            stoppingTimes.Add(dividendCondition.dividendTimes());
         }

         Utils.QL_REQUIRE(exercise.type() == Exercise.Type.American
                          || exercise.type() == Exercise.Type.European
                          || exercise.type() == Exercise.Type.Bermudan,
                          () => "exercise type is not supported");
         if (exercise.type() == Exercise.Type.American)
         {
            stepConditions.Add(new FdmAmericanStepCondition(mesher, calculator));
         }
         else if (exercise.type() == Exercise.Type.Bermudan)
         {
            FdmBermudanStepCondition bermudanCondition =
               new FdmBermudanStepCondition(exercise.dates(),
                                            refDate, dayCounter,
                                            mesher, calculator);
            stepConditions.Add(bermudanCondition);
            stoppingTimes.Add(bermudanCondition.exerciseTimes());
         }

         return new FdmStepConditionComposite(stoppingTimes, stepConditions);
      }

      public List<double> stoppingTimes() { return stoppingTimes_; }
      public List<IStepCondition<Vector>> conditions() { return conditions_; }

      protected List<double> stoppingTimes_;
      protected List<IStepCondition<Vector>> conditions_;
   }
}
