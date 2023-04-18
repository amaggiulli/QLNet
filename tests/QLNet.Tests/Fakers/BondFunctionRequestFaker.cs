using System;
using System.Collections.Generic;
using AutoBogus;
using QLNet.Requests;

namespace QLNet.Tests.Fakers
{
   internal class BondFunctionRequestFaker
   {
      public static BondFunctionsRequest[] CreateBondFunctions(int count = 1)
      {
         var list = new List<BondFunctionsRequest>();

         for (var i = 0; i < count; i++)
         {
            list.Add(new AutoFaker<BondFunctionsRequest>()
               .RuleFor(x => x.Id, i)
               .RuleFor(x => x.Bond, FixedRateBondFaker.CreateFixedRateBond)
               .RuleFor(x => x.SettlementDate, x => new Date(x.Date.Future()))
               .RuleFor(x => x.SinkAmounts, f => f.Make(10, () => f.Random.Double(0, 100)))
               .RuleFor(x => x.SinkDates, f => f.Make(10, () => f.Date.Between(DateTime.Today, DateTime.Today.AddYears(30))))
               .RuleFor(x => x.Price, x => x.Random.Double(0, 100))
               .RuleFor(x => x.DayCounter, x => new Thirty360(Thirty360.Thirty360Convention.BondBasis))
               .RuleFor(x => x.Comp, x => Compounding.Compounded)
               .RuleFor(x => x.Frequency, x => Frequency.Semiannual)
               .RuleFor(x => x.Accuracy, x => 1.0e-8)
               .Generate());
         }

         return list.ToArray();
      }
   }
}
