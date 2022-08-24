using System.Collections.Generic;
using AutoBogus;

namespace QLNet.Tests.Fakers
{
   public  class YieldRequestFaker
   {
      public static YieldRequest[] createYieldRequest(int count = 1)
      {
         var list = new List<YieldRequest>();

         for (var i = 0; i < count; i++)
         {
            list.Add(new AutoFaker<YieldRequest>()
               .RuleFor(x => x.Id, i)
               .RuleFor(x => x.Bond, FixedRateBondFaker.createFixedRateBond)
               .RuleFor(x => x.CleanPrice, x => x.Random.Double(0, 100))
               .RuleFor(x => x.DayCounter, x => new Thirty360(Thirty360.Thirty360Convention.BondBasis))
               .RuleFor(x => x.Compounding, x => Compounding.Compounded)
               .RuleFor(x => x.Frequency, x => Frequency.Semiannual)
               .RuleFor(x => x.SettlementDate, x => null)
               .RuleFor(x => x.Accuracy, x => null)
               .RuleFor(x => x.MaxIterations, x => null)
               .RuleFor(x => x.Guess, x => null).Generate());
         }

         return list.ToArray();
      }
   }
}
