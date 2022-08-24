using System;
using Bogus;

namespace QLNet.Tests.Fakers
{
   public class ScheduleFaker
   {
      public static Schedule createSchedule()
      {
         var faker = new Faker<Schedule>()
            .CustomInstantiator(f => new Schedule(DateTime.Now, DateTime.Now.AddYears(f.Random.Int(1,30)),
               new Period(Frequency.Semiannual), new TARGET(),
               BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted, DateGeneration.Rule.Backward,
               false));


         return faker.Generate();
      }
   }
}
