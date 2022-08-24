using Bogus;

namespace QLNet.Tests.Fakers
{
   public class FixedRateBondFaker
   {
      public static FixedRateBond createFixedRateBond()
      {
         var faker = new Faker<FixedRateBond>()
            .CustomInstantiator(f => new FixedRateBond(0, 1000, ScheduleFaker.createSchedule(), new InitializedList<double>(1, f.Random.Double(0.1)),
               new Thirty360(Thirty360.Thirty360Convention.BondBasis),BusinessDayConvention.Unadjusted));

         return faker.Generate();
      }
   }
}
