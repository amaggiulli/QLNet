using System.Diagnostics;
using System.Threading.Tasks;
using QLNet;
using QLNet.Tests.Fakers;
using Xunit;
using Xunit.Abstractions;

namespace TestSuite
{
   [Collection("QLNet CI Tests")]
   public class T_Batch
   {
      private readonly ITestOutputHelper output;

      public T_Batch(ITestOutputHelper output)
      {
         this.output = output;
      }

      [Fact]
      public async Task testBatchYieldCalculation()
      {
         var size = 1; // increase size to benchmark yield calculation
         var request = YieldRequestFaker.createYieldRequest(size);

         var stopwatch = Stopwatch.StartNew();
         var response = await BondFunctions.calculateYieldsAsync(request);
         output.WriteLine($"Calculated {size} yields in {stopwatch.ElapsedMilliseconds}ms");

         Assert.NotEmpty(response);
         Assert.Equal(size, response.Length);
         Assert.All(response, item => Assert.NotEqual(0,item.Yield));

      }
   }
}
