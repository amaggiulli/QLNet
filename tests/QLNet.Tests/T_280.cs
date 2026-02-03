using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TestSuite;
using Xunit.Abstractions;

namespace QLNet.Tests
{
   public class T_280
   {
      private readonly ITestOutputHelper testOutputHelper;

      public T_280(ITestOutputHelper testOutputHelper)
      {
         this.testOutputHelper = testOutputHelper;
      }

      private const double ACCURACY = 1.0e-06;

      // Run a calculation based on BondBasic object
      [Theory]
      [MemberData(nameof(GetSecurityInfo))]
      public void testBondBasic(BondBasics basics)
      {
         var adapter = new QLNetAdapter(new NullLogger<QLNetAdapter>());
         var res = adapter.Calculate(basics, new DateTime(2023, 07, 12));
         var ytw = res.YieldToWorst.Yield;

      }

      public static IEnumerable<object[]> GetSecurityInfo()
      {
         yield return new object[]
         {
            new BondBasics
            {
               PriceType = PriceTypes.Percentage,
               Price = 99.5190m,
               Coupon = 4.026m,
               CouponType = CouponType.FixedRate,
               CouponTypeMethod = CouponTypeMethod.FixedToFloat,
               MaturityDate = new DateTime(2030,01,22),
               AccrualDate = new DateTime(2023,07,18),
               FirstCouponDate = new DateTime(2024,07,15),
               NextCouponChangeDate = null,
               IssueDate = new DateTime(2023,07,18),
               Call = new Redemption[]
               {
                  new Redemption(){Date = new DateTime(2030,07,15),Price = 100}
               },
               Put = null,
               Refund = null,
               EffectiveCall = null,
               IsMandatoryPut = false,
               FaceAmount = 1000.0m,
               Redemption = 100.0m,
               SettlementDays = 0,
               OriginalPrice = 99.519m,
               OriginalYield = 4.03m,
               AccrualConvention = BusinessDayConvention.Unadjusted,
               PaymentConvention = BusinessDayConvention.Unadjusted,
               Compounding = Compounding.Compounded,
               Frequency = Frequency.Semiannual,
               BondDayCount = new Thirty360(Thirty360.Thirty360Convention.BondBasis),
               BondCalendar = new TARGET(),
               SinkSchedule = null,
               TradingStatusType = TradingStatusType.Unknown,
               ConversionSchedule = null,
               AssetType = AssetType.UsMunicipalBond
            }
         };
      }

      public static IEnumerable<object[]> Get467502JE1()
      {
         // Cusip : 467502JE1
         yield return new object[]
         {
           new BondBasics
           {
               PriceType = PriceTypes.Percentage,
               Price = 101.143m,
               Coupon = 6m,
               CouponType =  CouponType.FixedRate,
               CouponTypeMethod = CouponTypeMethod.Unknown,
               MaturityDate = new DateTime(2025,03,01),
               AccrualDate = new DateTime(2024,07,25),
               FirstCouponDate = new DateTime(2025,03,01),
               NextCouponChangeDate = null,
               IssueDate = new DateTime(2024,07,25),
               Put = null,
               Refund = null,
               EffectiveCall = null,
               IsMandatoryPut = false,
               FaceAmount = 1000.0m,
               Redemption =100.0m,
               SettlementDays =0,
               OriginalPrice = 101.575m,
               OriginalYield = 3.32m,
               AccrualConvention =BusinessDayConvention.Unadjusted,
               PaymentConvention =BusinessDayConvention.Unadjusted,
               Compounding =Compounding.Compounded,
               Frequency = Frequency.Semiannual,
               BondDayCount = new Thirty360(Thirty360.Thirty360Convention.BondBasis),
               BondCalendar = new TARGET(),
               SinkSchedule = null,
               TradingStatusType = TradingStatusType.Unknown,
               ConversionSchedule = null,
               AssetType = AssetType.UsMunicipalBond
           }
         };
      }

      [Fact]
      public void testStepped()
      {
         Date accrualDate = new Date(23, 09, 2022);
         Date maturityDate = new Date(23, 06, 2024);
         decimal price = 99.70m;
         Frequency frequency = Frequency.Semiannual;
         Calendar calendar = new TARGET();
         Date settlementDate = new Date(23, 09, 2022);
         BusinessDayConvention accrualConvention = BusinessDayConvention.Unadjusted;
         decimal coupon = 4.0m;
         CouponConversion[] conversionSchedule =
         {
            new CouponConversion(new DateTime(2022, 09, 23), 4),
            new CouponConversion(new DateTime(2024, 03, 23), 5),
            new CouponConversion(new DateTime(2024, 09, 23), 6),
            new CouponConversion(new DateTime(2025, 03, 23), 8),
         };
         DayCounter dayCounter = new Thirty360(Thirty360.Thirty360Convention.BondBasis);

         var bond = CreateNonCallableBond(accrualDate, maturityDate, 100, frequency, calendar, settlementDate,
            accrualConvention, coupon, conversionSchedule, dayCounter);

         var yield = bond.yield((double)price, dayCounter, Compounding.Compounded, frequency, settlementDate, ACCURACY);
      }

      [Fact]
      public void testAccrual()
      {
         Date accrualDate = new Date(25, 07, 2024);
         Date issueDate = new Date(25, 07, 2024);
         Date maturityDate = new Date(01, 03, 2025);
         Calendar calendar = new TARGET();
         Date settlementDate = new Date(04, 10, 2024);
         BusinessDayConvention accrualConvention = BusinessDayConvention.Unadjusted;
         decimal coupon = 6.0m;
         DayCounter dayCounter = new Thirty360(Thirty360.Thirty360Convention.BondBasis);


         var sch = new Schedule(accrualDate, maturityDate, new Period(Frequency.Once),
           calendar, accrualConvention, accrualConvention, DateGeneration.Rule.Backward, false, null);
         var coupons = CreateCoupons(sch, coupon, null);

         var bond = new CallableFixedRateBond(0, 1000, sch, coupons, dayCounter, accrualConvention,
            100, issueDate, null);

         var (accruedDays, accruedAmount) = CashFlows.accruedDaysAndAmount(bond.cashflows(), false, settlementDate);
      }


      [Fact]
      public void testAccrualLongFirstCouponDate()
      {
         var accrualDate = new Date(01, 07, 2023);
         var issueDate = new Date(01, 07, 2023);
         var firstCouponDate = new Date(01, 07, 2025);
         var maturityDate = new Date(01, 07, 2026);
         Calendar calendar = new TARGET();
         var settlementDate = new Date(07, 02, 2024);
         var accrualConvention = BusinessDayConvention.Unadjusted;
         var coupon = 4.0m;
         DayCounter dayCounter = new Thirty360(Thirty360.Thirty360Convention.BondBasis);
         var price = 102.396;
         var frequency = Frequency.Semiannual;

         var sch = new Schedule(accrualDate, maturityDate, new Period(frequency),
            calendar, accrualConvention, accrualConvention, DateGeneration.Rule.Forward, false, firstCouponDate);
         var coupons = CreateCoupons(sch, coupon, null);

         var bond = new CallableFixedRateBond(0, 1000, sch, coupons, dayCounter, accrualConvention,
            100, issueDate, null);
         var bond1 = new FixedRateBond(0, 1000, sch, coupons, dayCounter, accrualConvention,
            100, issueDate, null);

         var (accruedDays, accruedAmount) = CashFlows.accruedDaysAndAmount(bond.cashflows(), false, settlementDate);
         var yield = bond.yield((double)price, dayCounter, Compounding.Compounded, frequency, settlementDate, ACCURACY);
         var yield1 = bond.yield((double)price, dayCounter, Compounding.Simple, frequency, settlementDate, ACCURACY);
      }

      [Fact]
      public void testAccrualLongFirstCouponDate2()
      {
         var accrualDate = new Date(21, 11, 2024);
         var issueDate = new Date(21, 11, 2024);
         var firstCouponDate = new Date(31, 07, 2025);
         var maturityDate = new Date(31, 01, 2030);
         Calendar calendar = new TARGET();
         var settlementDate = new Date(03, 02, 2025);
         var accrualConvention = BusinessDayConvention.Unadjusted;
         var coupon = 5.0m;
         DayCounter dayCounter = new Actual360();
         var price = 99.874;
         var frequency = Frequency.Semiannual;

         var sch = new Schedule(accrualDate, maturityDate, new Period(frequency),
            calendar, accrualConvention, accrualConvention, DateGeneration.Rule.Forward, true, firstCouponDate);
         var coupons = CreateCoupons(sch, coupon, null);

         var bond = new CallableFixedRateBond(0, 1000, sch, coupons, dayCounter, accrualConvention,
            100, issueDate, null);
         var bond1 = new FixedRateBond(0, 1000, sch, coupons, dayCounter, accrualConvention,
            100, issueDate, null);

         var (accruedDays, accruedAmount) = CashFlows.accruedDaysAndAmount(bond.cashflows(), false, settlementDate);
         var yield = bond.yield((double)price, dayCounter, Compounding.Compounded, frequency, settlementDate, ACCURACY);
         var yield1 = bond.yield((double)price, dayCounter, Compounding.Simple, frequency, settlementDate, ACCURACY);
      }

      [Fact]
      public void testAccrualOnFloater()
      {
         var accrualDate = new Date(01, 07, 2023);
         var issueDate = new Date(01, 07, 2023);
         var firstCouponDate = new Date(01, 07, 2025);
         var maturityDate = new Date(01, 07, 2026);
         Calendar calendar = new TARGET();
         var settlementDate = new Date(07, 02, 2024);
         var accrualConvention = BusinessDayConvention.Unadjusted;
         var coupon = 4.0m;
         DayCounter dayCounter = new Thirty360(Thirty360.Thirty360Convention.BondBasis);
         var price = 102.396;
         var frequency = Frequency.Semiannual;

         var sch = new Schedule(accrualDate, maturityDate, new Period(frequency),
            calendar, accrualConvention, accrualConvention, DateGeneration.Rule.Forward, false, firstCouponDate);
         var coupons = CreateCoupons(sch, coupon, null);

         var discountCurve = new Handle<YieldTermStructure>(Utilities.flatRate(settlementDate, 0.03, new Actual360()));
         var sofr = new Sofr(discountCurve);

         var bond = new FloatingRateBond(0, 1000, sch, sofr, dayCounter);

         //var bond = new CallableFixedRateBond(0, 1000, sch, coupons, dayCounter, accrualConvention,
         //   100, issueDate, null);
         //var bond1 = new FixedRateBond(0, 1000, sch, coupons, dayCounter, accrualConvention,
         //   100, issueDate, null);

         var (accruedDays, accruedAmount) = CashFlows.accruedDaysAndAmount(bond.cashflows(), false, settlementDate);
         var yield = bond.yield((double)price, dayCounter, Compounding.Compounded, frequency, settlementDate, ACCURACY);
         var yield1 = bond.yield((double)price, dayCounter, Compounding.Simple, frequency, settlementDate, ACCURACY);
      }
      /// <summary>
      /// Create a FixedRate bond to the given date at the given price
      /// </summary>
      /// <returns>Bond</returns>
      private Bond CreateNonCallableBond(Date accrualDate, Date date, decimal? price, Frequency frequency, Calendar calendar, Date settlementDate,
      BusinessDayConvention accrualConvention, decimal coupon, CouponConversion[] conversionSchedule, DayCounter dayCounter)
      {
         if (date != null && date > settlementDate && price.HasValue)
         {
            var sch = new Schedule(accrualDate, date, new Period(frequency), calendar,
               accrualConvention, accrualConvention, DateGeneration.Rule.Backward, false);

            //var dates1 = sch.dates();
            //var reg = sch.isRegular().ToList();

            var dates = conversionSchedule.Select(x => x.Date).ToArray();
            sch.addIrregularDates(dates);
            //mergeDates(dates1, dates, reg);

            //var sch2 = new Schedule(dates1, calendar, accrualConvention, accrualConvention, new Period(frequency),null, null, reg );

            var coupons = CreateCoupons(sch, coupon, conversionSchedule);

            return new FixedRateBond(0, (double)1000, sch, coupons, dayCounter, accrualConvention, (double)price.Value, sch[0]);
         }

         return null;
      }

      private List<double> CreateCoupons(Schedule sch, decimal coupon, CouponConversion[] conversionSchedule)
      {
         // Conversion for stepped coupon check
         List<double> coupons;
         if (conversionSchedule != null &&
             conversionSchedule.Length > 0)
         {
            var steppedCouponList = new CouponConversionSchedule();
            foreach (var couponConversion in conversionSchedule)
            {
               steppedCouponList.Add(new QLNet.CouponConversion(couponConversion.Date, (double)(couponConversion.Rate / 100m)));
            }
            coupons = CreateCouponSchedule(sch, steppedCouponList);
         }
         else
         {
            coupons = new InitializedList<double>(1, (double)(coupon / 100m));
         }
         return coupons;
      }

      public static List<double> CreateCouponSchedule(Schedule schedule,
         CouponConversionSchedule couponConversionSchedule)
      {
         List<double> ret = new InitializedList<double>(schedule.Count);
         for (int i = 0; i < couponConversionSchedule.Count; i++)
            for (int j = 0; j < schedule.Count; j++)
               if (schedule[j] >= (Date)couponConversionSchedule[i].Date)
                  ret[j] = couponConversionSchedule[i].Rate;

         return ret;
      }

      public class CouponConversion
      {
         public DateTime Date { get; set; }
         public decimal Rate { get; set; }
         public override string ToString() => ($"Conversion Date : {Date}\nConversion Rate : {Rate}");

         public CouponConversion(DateTime date, decimal rate)
         {
            Date = date;
            Rate = rate;
         }

      }

      private void MergeDates(List<Date> scheduleDates, DateTime[] conversiondates, List<bool> isRegular)
      {
         foreach (var date1 in conversiondates)
         {
            if (!scheduleDates.Exists(x => x == (Date)date1) &&
                date1 < (DateTime)scheduleDates.Max(x => x))
            {
               var index = scheduleDates.FindIndex((x => x > (Date)date1));
               scheduleDates.Insert(index, date1);
               isRegular.Insert(index, false);
            }
         }
      }

      [Fact]
      public void TestWeightedAverageLife()
      {
         // Based on https://280cap.atlassian.net/secure/attachment/10581/Sinking%20Fund%20-%20weighted%20avg%20life.xlsx
         DateTime today = new Date(5, Month.Jun, 2018);
         var amounts = new List<double> { 5080, 35255, 8335 };
         var schedule = new List<DateTime> { new Date(1, 8, 2035), new Date(1, 8, 2036), new Date(1, 8, 2037) };

         var weightedAverageLife = BondFunctions.WeightedAverageLife(today, amounts, schedule);
         Assert.True(weightedAverageLife == new DateTime(2036, 08, 25));

         // Test with past dates
         today = new Date(19, Month.Sep, 2019);
         amounts = new List<double> { 1180000, 1250000, 1320000, 1395000 };
         schedule = new List<DateTime> { new Date(1, 10, 2016), new Date(1, 10, 2017), new Date(1, 10, 2018), new Date(1, 10, 2019) };

         weightedAverageLife = BondFunctions.WeightedAverageLife(today, amounts, schedule);
         Assert.True(weightedAverageLife == new DateTime(2019, 10, 1));
      }

      [Fact]
      public void TestWeightedAverageLife2()
      {
         DateTime today = new Date(1, Month.Mar, 2024);
         var amounts = new List<double> { 21560000, 22515000, 23510000, 24555000, 18825000 };
         var schedule = new List<DateTime> { new Date(1, 4, 2024), new Date(1, 4, 2025), new Date(1, 4, 2026), new Date(1, 4, 2027), new Date(1, 4, 2028) };

         var weightedAverageLife = BondFunctions.WeightedAverageLife(today, amounts, schedule);
         Assert.True(weightedAverageLife == new DateTime(2026, 03, 06));

      }

      [Fact]
      public void TestScheduleUntil()
      {
         var today = new Date(10, Month.Apr, 2023);

         var backup = new SavedSettings();
         Settings.setEvaluationDate(today);

         var sch = new Schedule(new DateTime(2013, 07, 26), new DateTime(2023, 07, 26), new Period(Frequency.Semiannual), new TARGET(),
            BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted, DateGeneration.Rule.Backward, false);

         var newSch = sch.until(new DateTime(2023, 04, 27));

         var fixedBond = new FixedRateBond(0, (double)100, newSch, new InitializedList<double>(newSch.size() - 1, 0.04625),
            new Thirty360(Thirty360.Thirty360Convention.USA), BusinessDayConvention.Unadjusted, 100, new DateTime(2013, 07, 26));

         var yield = fixedBond.yield(100.1, new Thirty360(Thirty360.Thirty360Convention.USA), Compounding.Simple, Frequency.Semiannual);
      }

      //[Fact]
      //public async Task TestThreadSpecificSettings()
      //{
      //   // Arrange
      //   const int threadCount = 15;

      //   // Act
      //   var tasks = Enumerable.Range(0, threadCount)
      //      .Select(i => Task.Run(async () =>
      //      {
      //         var settings = ThreadSafeSettings.Instance;
      //         var d = Date.Today + i;
      //         //settings.setEvaluationDate(d);
      //         settings.SetTestString($"Thread {i}");

      //         await Task.Delay(100); // Simulate some work
      //         testOutputHelper.WriteLine($"{Task.CurrentId}: Thread {i} String {settings.TestString()}");
      //         //Assert.Equal(d, settings.evaluationDate());
      //         Assert.Equal($"Thread {i}", settings.TestString());
      //      }));

      //   await Task.WhenAll(tasks);
      //}
      // 280 stuff
      #region 280 stuff  
      public class QLNetAdapter
      {
         private const int DECIMAL_PLACES = 6;
         private const double ACCURACY = 1.0e-06;
         private const decimal YTCONV_FIXED_PRICE = 100m;
         private const decimal YIELD_DIV = 100m;
         private const double YIELD_DIV_DOUBLE = 100;

         public static decimal QLRound(decimal value)
         {
            return Math.Round(value, DECIMAL_PLACES);
         }

         public static decimal QLRound(double value)
         {
            return (decimal)Math.Round(value, DECIMAL_PLACES);
         }

         private readonly ILogger<QLNetAdapter> _logger;

         public QLNetAdapter(ILogger<QLNetAdapter> logger)
         {
            _logger = logger;
         }

         /// <summary>
         /// Calculate price/yield
         /// </summary>
         /// <param name="bondBasics"></param>
         /// <param name="settlementDate"></param>
         /// <param name="commission"></param>
         /// <returns>CalcResults struct with ytm,ytc and ytw filled</returns>
         public CalcResults Calculate(BondBasics bondBasics, DateTime settlementDate, decimal commission = 0)
         {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
               _logger.Log(LogLevel.Debug,
                   "Calculate called with PriceType " + bondBasics.PriceType + Environment.NewLine +
                   " Price : " + bondBasics.Price + Environment.NewLine +
                   "BondBasic : " + bondBasics);
            }

            var res = new CalcResults
            {
               IsMandatoryPut = bondBasics.IsMandatoryPut,
               SettlementDate = settlementDate,
               SubjectToDeMinimis = null
            };

            try
            {
               Date today = settlementDate;
               Settings.setEvaluationDate(today);
               Bond bondConversion = null;

               // Create the main schedule
               var sch = CreateSchedule(bondBasics);

               var bond = CreateMaturityBond(bondBasics, sch);
               var bondCalled = bondBasics.Call?.Select(redemption => CreateNonCallableBond(redemption.Date, redemption.Price, bondBasics, settlementDate, sch)).Where(x => x != null).ToArray();
               var bondPutted = CreateNonCallableBond(bondBasics.Put?.Date, bondBasics.Put?.Price, bondBasics, settlementDate, sch);
               var bondRefund = CreateNonCallableBond(bondBasics.Refund?.Date, bondBasics.Refund?.Price, bondBasics, settlementDate, sch);
               var bondEffCall = CreateNonCallableBond(bondBasics.EffectiveCall?.Date, bondBasics.EffectiveCall?.Price, bondBasics, settlementDate, sch);
               if (bondBasics.CouponTypeMethod == CouponTypeMethod.FixedToFloat)
                  bondConversion = CreateNonCallableBond(bondBasics.NextCouponChangeDate?.Date, YTCONV_FIXED_PRICE, bondBasics, settlementDate, sch);

               // Accrued Interest & Days
               if (bondBasics.TradingStatusType != TradingStatusType.TradingFlat)
               {
                  var (accruedDays, accruedAmount) = CashFlows.accruedDaysAndAmount(bond.cashflows(), true,
                      bondBasics.AccrualDate > settlementDate ? bondBasics.AccrualDate : settlementDate);
                  res.AccruedInterest = QLRound(accruedAmount);
                  res.AccruedDays = accruedDays;
               }

               // In the last period compound must be simple if it is a fixedrate
               var comp = GetSecurityCompounding(bond, bondBasics.CouponType, settlementDate);

               // calculate Price
               if (bondBasics.PriceType == PriceTypes.Percentage) // Given Price
               {
                  res.Price = bondBasics.Price;
               }
               else if (bondBasics.PriceType == PriceTypes.YieldToMaturity) // Given YTM Calculate Price
               {
                  res.Price = QLRound(bond.cleanPrice((double)(bondBasics.Price / YIELD_DIV), bondBasics.BondDayCount, comp, bondBasics.Frequency, settlementDate));
               }
               else if (bondBasics.PriceType == PriceTypes.YieldToWorst) // Given YTW Calculate Price
               {
                  // The lowest price is the ytw
                  var priceToMaturity = bond.cleanPrice((double)(bondBasics.Price / YIELD_DIV), bondBasics.BondDayCount, comp, bondBasics.Frequency, settlementDate);
                  if (bondConversion != null)
                  {
                     priceToMaturity = bondConversion.cleanPrice((double)(bondBasics.Price / YIELD_DIV), bondBasics.BondDayCount, comp, bondBasics.Frequency, settlementDate);
                  }
                  var priceToRefund = bondRefund?.cleanPrice((double)(bondBasics.Price / YIELD_DIV), bondBasics.BondDayCount, comp, bondBasics.Frequency, settlementDate);
                  var priceToEffCall = bondEffCall?.cleanPrice((double)(bondBasics.Price / YIELD_DIV), bondBasics.BondDayCount, comp, bondBasics.Frequency, settlementDate);
                  double? priceToPutted = null;
                  if (bondBasics.IsMandatoryPut)
                     priceToPutted = bondPutted?.cleanPrice((double)(bondBasics.Price / YIELD_DIV), bondBasics.BondDayCount, comp, bondBasics.Frequency, settlementDate);

                  var list = new List<double?> { priceToMaturity, priceToRefund, priceToPutted, priceToEffCall };
                  if (bondCalled is { Length: > 0 })
                  {
                     var priceToCall = bondCalled.Select(bondCall => bondCall?.cleanPrice((double)(bondBasics.Price / YIELD_DIV), bondBasics.BondDayCount, comp, bondBasics.Frequency, settlementDate)).ToArray();
                     list.AddRange(priceToCall.Select(d => d));
                  }
                  var price = list.Where(x => x != null).Min();
                  res.Price = QLRound(price.GetValueOrDefault());
               }

               // Adding commission 
               res.Price += QLRound(commission);

               // We have Price and AccruedInterest we can calculate DirtyPrice
               res.DirtyPrice = QLRound(res.Price + (res.AccruedInterest / 10.0m));

               // Check for a special case :
               //
               // - Settlement Date 31
               // - Maturity date one day later
               // - DayCounter Thirty360
               //
               // In that case we return all yields at 0.00%.
               // Verified with Bloomberg.
               if (settlementDate.Day == 31 &&
                   bondBasics.BondDayCount is Thirty360 &&
                   bondBasics.BondDayCount.dayCount(settlementDate, bondBasics.MaturityDate) == 1)
               {
                  res.Yields.Add(new YieldCalc
                  {
                     Type = YieldType.YieldToMaturity,
                     Date = bondBasics.MaturityDate,
                     Yield = 0.00m,
                     Redemption = bondBasics.Redemption
                  });
                  return res;
               }

               // If is a Fixed to Floating Rate calculate Yield to Conversion
               if (bondConversion != null)
               {
                  // In the last period compound must be simple if it is a fixedrate
                  var compCalled = GetSecurityCompounding(bondConversion, bondBasics.CouponType, settlementDate);

                  // BON-6983 use 100 as price to calculte YieldToConversion 
                  var yieldToConversion = CalculateYield(bondConversion, res.Price, bondBasics.BondDayCount,
                      compCalled, bondBasics.Frequency, settlementDate, "yieldToConversion", bondBasics);

                  if (yieldToConversion.HasValue)
                  {
                     res.Yields.Add(new YieldCalc
                     {
                        Type = YieldType.YieldToConversion,
                        Date = bondBasics.NextCouponChangeDate.Value,
                        Yield = QLRound(yieldToConversion.Value * YIELD_DIV_DOUBLE),
                        Redemption = YTCONV_FIXED_PRICE
                     });

                     // Calculate duration
                     res.ModifiedDuration = CalculateModifiedDuration(bondConversion, yieldToConversion.Value, compCalled
                         , bondBasics.Frequency, bondBasics.BondDayCount, settlementDate);
                  }
               }
               else
               {
                  var yieldToMaturity = CalculateYield(bond, res.Price, bondBasics.BondDayCount, comp,
                      bondBasics.Frequency, settlementDate, "yieldToMaturity", bondBasics);

                  // For T-Bills return the BEY
                  if (bondBasics.AssetType == AssetType.Treasury && bondBasics.CouponType is CouponType.ShortTermDiscount)
                  {
                     yieldToMaturity = CalculateBondEquivaletYield(res.Price, bondBasics.BondDayCount,
                         bondBasics.IssueDate, settlementDate, bondBasics.MaturityDate);
                     // New library calculation, let's compare yields before update
                     var newYieldToMaturity = bond.bondEquivalentYield(Bond.BondEquivalentYearType.IssueFwdOneYear, settlementDate,
                         res.Price, bondBasics.BondDayCount, new UnitedStates(UnitedStates.Market.GovernmentBond), bondBasics.Frequency);
                     if (yieldToMaturity != newYieldToMaturity)
                     {
                        _logger.LogError("Bond Equivalent Yield error: original : {originalYield}, new : {updatedYield}. BondBasic : {bondBasic}",
                            yieldToMaturity, newYieldToMaturity, bondBasics);
                     }
                  }

                  if (yieldToMaturity.HasValue)
                  {
                     res.Yields.Add(new YieldCalc
                     {
                        Type = YieldType.YieldToMaturity,
                        Date = bondBasics.MaturityDate,
                        Yield = QLRound(yieldToMaturity.Value * YIELD_DIV_DOUBLE),
                        Redemption = bondBasics.Redemption
                     });

                     // Calculate duration
                     res.ModifiedDuration = CalculateModifiedDuration(bond, yieldToMaturity.Value, comp, bondBasics.Frequency,
                         bondBasics.BondDayCount, settlementDate);
                  }
               }

               //
               // res.EffectiveDuration = TBD
               // we need a yield term structure here 
               //
               // effectiveDuration( double oas,
               //                    Handle < YieldTermStructure > engineTS,
               //                    DayCounter dayCounter,
               //                    Compounding compounding,
               //                    Frequency frequency,
               //                    double bump = 2e-4)
               var callIndex = 0;
               foreach (var bondCall in bondCalled ?? Array.Empty<Bond>())
               {
                  // In the last period compound must be simple if it is a fixedrate
                  var compCalled = GetSecurityCompounding(bondCall, bondBasics.CouponType, settlementDate);
                  var yieldToCall = CalculateYield(bondCall, res.Price, bondBasics.BondDayCount,
                      compCalled, bondBasics.Frequency, settlementDate, "yieldToCall", bondBasics);

                  if (yieldToCall.HasValue)
                  {
                     res.Yields.Add(new YieldCalc
                     {
                        Type = YieldType.YieldToCall,
                        Date = bondBasics.Call[callIndex].Date.Value,
                        Yield = QLRound(yieldToCall.Value * YIELD_DIV_DOUBLE),
                        Redemption = bondBasics.Call[callIndex].Price.Value
                     });
                  }
                  callIndex++;
               }

               if (bondPutted != null)
               {
                  // In the last period compound must be simple if it is a fixedrate
                  var compPutted = GetSecurityCompounding(bondPutted, bondBasics.CouponType, settlementDate);
                  var yieldToPut = CalculateYield(bondPutted, res.Price, bondBasics.BondDayCount,
                      compPutted, bondBasics.Frequency, settlementDate, "yieldToPut", bondBasics);

                  if (yieldToPut.HasValue)
                  {
                     res.Yields.Add(new YieldCalc
                     {
                        Type = YieldType.YieldToPut,
                        Date = bondBasics.Put.Date.Value,
                        Yield = QLRound(yieldToPut.Value * YIELD_DIV_DOUBLE),
                        Redemption = bondBasics.Put.Price.Value
                     });
                  }
               }

               if (bondRefund != null)
               {
                  // In the last period compound must be simple if it is a fixedrate
                  var compRefund = GetSecurityCompounding(bondRefund, bondBasics.CouponType, settlementDate);
                  var yieldToRefund = CalculateYield(bondRefund, res.Price, bondBasics.BondDayCount,
                      compRefund, bondBasics.Frequency, settlementDate, "yieldToRefund", bondBasics);

                  if (yieldToRefund.HasValue)
                  {
                     res.Yields.Add(new YieldCalc
                     {
                        Type = YieldType.YieldToRefunding,
                        Date = bondBasics.Refund.Date.Value,
                        Yield = QLRound(yieldToRefund.Value * YIELD_DIV_DOUBLE),
                        Redemption = bondBasics.Refund.Price.Value
                     });
                  }
               }

               if (bondEffCall != null)
               {
                  // In the last period compound must be simple if it is a fixedrate
                  var compRefund = GetSecurityCompounding(bondEffCall, bondBasics.CouponType, settlementDate);
                  var yieldToEffCall = CalculateYield(bondEffCall, res.Price, bondBasics.BondDayCount,
                      compRefund, bondBasics.Frequency, settlementDate, "yieldToEffCall", bondBasics);

                  if (yieldToEffCall.HasValue)
                  {
                     res.Yields.Add(new YieldCalc
                     {
                        Type = YieldType.YieldToEffCall,
                        Date = bondBasics.EffectiveCall.Date.Value,
                        Yield = QLRound(yieldToEffCall.Value * YIELD_DIV_DOUBLE),
                        Redemption = bondBasics.EffectiveCall.Price.Value
                     });
                  }
               }

               // Check if Weighted Average Life fields are present
               if (bondBasics.AccrualDate != null && bondBasics.SinkSchedule is { Length: > 0 })
               {
                  // Calculate Weighted Average Life
                  res.WeightedAverageLife = BondFunctions.WeightedAverageLife(settlementDate,
                      bondBasics.SinkSchedule.Select(sink => (double)sink.Amount).ToList(),
                      bondBasics.SinkSchedule.Select(sink => sink.Date).ToList());

                  // Calculate YieldToAverageLife
                  // For now we skip ZeroCoupons
                  if (res.WeightedAverageLife != null && bondBasics.CouponType != CouponType.ZeroCoupon)
                  {
                     var bondWal = CreateNonCallableBond(res.WeightedAverageLife.Value, 100, bondBasics, settlementDate, sch);
                     // In the last period compound must be simple if it is a fixedrate
                     var compWal = GetSecurityCompounding(bondWal, bondBasics.CouponType, settlementDate);
                     var yieldToAverageLife = CalculateYield(bondWal, res.Price, bondBasics.BondDayCount,
                         compWal, bondBasics.Frequency, settlementDate, "yieldToAverageLife", bondBasics);

                     if (yieldToAverageLife.HasValue)
                     {
                        res.Yields.Add(new YieldCalc
                        {
                           Type = YieldType.YieldToAverageLife,
                           Date = res.WeightedAverageLife.Value,
                           Yield = QLRound(yieldToAverageLife.Value * YIELD_DIV_DOUBLE),
                           Redemption = bondBasics.Redemption
                        });
                     }
                  }
               }

               // Check if is subject to De Minims tax rule
               if (bondBasics.AssetType.HasValue &&
                   bondBasics.AssetType != AssetType.CorporateBond)
               {
                  res.SubjectToDeMinimis = IsSubjectToDeMinimis(bond, res.Price, bondBasics, settlementDate);
               }

               return res;
            }
            catch (Exception e)
            {
               _logger.LogDebug("Calculate called with SettlementDate " + settlementDate + Environment.NewLine +
                  "BondBasic : " + bondBasics + Environment.NewLine +
                  "Exception : " + e.Message);
               throw;
            }

         }

         /// <summary>
         /// Calculate a bond yield and trap common exceptions
         /// </summary>
         /// <param name="bond"></param>
         /// <param name="price"></param>
         /// <param name="bondDayCount"></param>
         /// <param name="comp"></param>
         /// <param name="freq"></param>
         /// <param name="settlementDate"></param>
         /// <param name="typeOfYield"></param>
         /// <returns></returns>
         private double? CalculateYield(Bond bond, decimal price, DayCounter bondDayCount, Compounding comp,
             Frequency freq, DateTime settlementDate, string typeOfYield, BondBasics bondBasics)
         {
            double var;
            try
            {
               var = bond.yield((double)price, bondDayCount, comp, freq, settlementDate, ACCURACY);
            }
            catch (RootNotBracketException e)
            {
               _logger.LogWarning("Unable to calculate " + typeOfYield + " with Price " + price + Environment.NewLine +
                                  "BondBasics : " + bondBasics +
                                  "Exception : " + e.Message);
               return null;
            }
            catch (MaxNumberFuncEvalExceeded e)
            {
               _logger.LogWarning("Unable to calculate " + typeOfYield + " with Price " + price + Environment.NewLine +
                                  "Exception : " + e.Message);
               return null;
            }
            catch (NotTradableException)
            {
               return 0.0;
            }
            catch (InvalidPriceSignException e)
            {
               _logger.LogWarning("Unable to calculate " + typeOfYield + " with Price " + price + Environment.NewLine +
                                  "Exception : " + e.Message);
               return null;
            }

            return var;
         }

         /// <summary>
         /// Calculate Bond Equivalent Yield
         /// </summary>
         /// <param name="price">Current Price</param>
         /// <param name="dc">Day Counter</param>
         /// <param name="issueDate"></param>
         /// <param name="settlementDate">Settlement Date</param>
         /// <param name="maturityDate">Maturity Date</param>
         /// <returns></returns>
         private static double? CalculateBondEquivaletYield(decimal price, DayCounter dc, DateTime? issueDate, DateTime settlementDate, DateTime maturityDate)
         {
            // Leap years are not handled for now
            // to handle uncomment the following line
            // double yearDays = DateTime.IsLeapYear(maturityDate.Year) ? 366 : 365;
            double yearDays = 365;
            if (issueDate != null)
            {
               var calendar = new UnitedStates(UnitedStates.Market.GovernmentBond);
               var eom = calendar.isEndOfMonth(issueDate);
               var endPeriod = calendar.advance(issueDate, new Period(1, TimeUnit.Years), BusinessDayConvention.Unadjusted, eom);
               yearDays = dc.dayCount(issueDate, endPeriod);
            }
            double daysToMaturity = dc.dayCount(settlementDate, maturityDate);
            if (daysToMaturity <= 182)
            {
               return (double?)((YIELD_DIV - price) / price * ((decimal)yearDays / dc.dayCount(settlementDate, maturityDate)));
            }
            var numerator = ((-2 * daysToMaturity) / yearDays) + 2 * Math.Pow(
                Math.Pow(daysToMaturity / yearDays, 2) - ((2 * daysToMaturity / yearDays - 1) * (1 - 100 / (double)price)), 0.5);
            var denominator = 2 * daysToMaturity / yearDays - 1;
            return numerator / denominator;
         }

         /// <summary>
         /// Calculate Bond Equivalent Yield
         /// </summary>
         /// <param name="bond"></param>
         /// <param name="price">Current Price</param>
         /// <param name="dc">Day Counter</param>
         /// <param name="settlementDate">Settlement Date</param>
         /// <returns></returns>
         private static double? CalculateBondEquivaletYield(Bond bond, decimal price, DayCounter dc, DateTime settlementDate, Frequency freq)
         {
            return bond.bondEquivalentYield(Bond.BondEquivalentYearType.IssueFwdOneYear, settlementDate, price, dc, bond.calendar(), freq);
         }
         /// <summary>
         /// Calculate Discount Yield
         /// </summary>
         /// <param name="price">Current Price</param>
         /// <param name="dc">Day Counter</param>
         /// <param name="settlementDate">Settlement Date</param>
         /// <param name="maturityDate">Maturity Date</param>
         /// <returns></returns>
         private static double? CalculateDiscountYield(decimal price, DayCounter dc, DateTime settlementDate, DateTime maturityDate)
         {
            return (double?)((YIELD_DIV - price) / YIELD_DIV * (360m / dc.dayCount(settlementDate, maturityDate)));
         }

         /// <summary>
         /// Calculate Modified Duration
         /// </summary>
         /// <param name="bond"></param>
         /// <param name="yield"></param>
         /// <param name="comp"></param>
         /// <param name="frequency"></param>
         /// <param name="dayCounter"></param>
         /// <param name="settlementDate"></param>
         /// <returns></returns>
         private static decimal CalculateModifiedDuration(Bond bond, double yield, Compounding comp, Frequency frequency
             , DayCounter dayCounter, DateTime settlementDate)
         {
            try
            {
               return QLRound((decimal)BondFunctions.duration(bond, yield, dayCounter,
                   comp, frequency, Duration.Type.Modified, settlementDate));
            }
            catch (Exception)
            {
               return 0;
            }
         }

         #region Bond Setups

         /// <summary>
         /// Create a callable bond to maturity
         /// </summary>
         /// <returns>Bond</returns>
         private static Bond CreateMaturityBond(BondBasics bondBasics, Schedule sch)
         {
            // Callability 
            var callSchedule = new CallabilitySchedule();
            if (bondBasics.Call?[0].Price != null && bondBasics.Call[0].Date != null)
            {
               var myPrice = new Bond.Price((double)bondBasics.Call[0].Price.Value, Bond.Price.Type.Clean);
               callSchedule.Add(new Callability(myPrice, Callability.Type.Call, bondBasics.Call[0].Date));
            }

            var coupons = CreateCoupons(sch, bondBasics.Coupon, bondBasics.ConversionSchedule);

            switch (bondBasics.CouponType)
            {
               case CouponType.AdjRate:
               case CouponType.OIS:
               case CouponType.FixedRate:
               case CouponType.DeferredInterest:
                  return new CallableFixedRateBond(bondBasics.SettlementDays, (double)bondBasics.FaceAmount, sch,
                      coupons,
                      bondBasics.BondDayCount, bondBasics.PaymentConvention,
                      (double)bondBasics.Redemption, bondBasics.IssueDate, callSchedule);
               case CouponType.ZeroCoupon:
               case CouponType.ShortTermDiscount:
                  return new CallableZeroCouponBond(bondBasics.SettlementDays, (double)bondBasics.FaceAmount, bondBasics.BondCalendar, bondBasics.MaturityDate,
                      bondBasics.BondDayCount, bondBasics.PaymentConvention, (double)bondBasics.Redemption, bondBasics.IssueDate, callSchedule);
               default:
                  return null;
            }
         }

         private static Schedule CreateSchedule(BondBasics bondBasics)
         {
            // Check if BondBasics.FirstCouponDate is in period 
            DateTime? firstCouponDate = null;
            //if (bondBasics.FirstCouponDate > bondBasics.AccrualDate &&
            //    bondBasics.FirstCouponDate < bondBasics.MaturityDate)
            //    firstCouponDate = bondBasics.FirstCouponDate;

            // If we have a valid firstCouponDate set the Date generation rule to Forward
            // this will give us more accurate results for any bond with irregural period/maturity date
            var dateGenerationRule = DateGeneration.Rule.Backward;
            var endOfMonth = Date.isEndOfMonth(bondBasics.MaturityDate);

            if (firstCouponDate != null)
            {
               dateGenerationRule = DateGeneration.Rule.Forward;
               endOfMonth = Date.isEndOfMonth(firstCouponDate);
            }

            if (bondBasics.Frequency == Frequency.Once)
            {
               endOfMonth = false;
            }

            // If CouponType is DeferredInterest the FirstCouponDate is the first date with coupon > 0
            // This patch is needed because we noticed several wrong FirstCouponDate from providers 
            if (bondBasics.CouponType == CouponType.DeferredInterest &&
                bondBasics.ConversionSchedule?.Length > 0)
            {
               var firstValidCoupon = bondBasics.ConversionSchedule.FirstOrDefault(x => x.Rate > 0);
               firstCouponDate = firstValidCoupon?.Date;
               if (firstCouponDate != null && firstCouponDate < bondBasics.AccrualDate)
                  firstCouponDate = bondBasics.AccrualDate;
            }

            var sch = new Schedule(bondBasics.AccrualDate, bondBasics.MaturityDate, new Period(bondBasics.Frequency),
                bondBasics.BondCalendar,
                bondBasics.AccrualConvention, bondBasics.AccrualConvention, dateGenerationRule, endOfMonth, firstCouponDate);
            return sch;
         }

         /// <summary>
         /// Create a coupon rate list
         /// It return a list with one coupon rate or a stepped list
         /// </summary>
         /// <param name="sch"></param>
         /// <param name="coupon"></param>
         /// <param name="conversionSchedule"></param>
         /// <returns></returns>
         private static List<double> CreateCoupons(Schedule sch, decimal coupon, CouponConversion[] conversionSchedule)
         {
            // Conversion for stepped coupon check
            List<double> coupons;
            if (conversionSchedule != null &&
                conversionSchedule.Length > 0)
            {
               var steppedCouponList = new CouponConversionSchedule();
               foreach (var couponConversion in conversionSchedule)
               {
                  steppedCouponList.Add(new QLNet.CouponConversion(couponConversion.Date, (double)(couponConversion.Rate / YIELD_DIV)));
               }
               coupons = CreateCouponSchedule(sch, steppedCouponList);
            }
            else
            {
               coupons = new InitializedList<double>(1, (double)(coupon / YIELD_DIV));
            }
            return coupons;
         }

         public static List<double> CreateCouponSchedule(Schedule schedule,
             CouponConversionSchedule couponConversionSchedule)
         {
            List<double> ret = new InitializedList<double>(schedule.Count);
            for (int i = 0; i < couponConversionSchedule.Count; i++)
               for (int j = 0; j < schedule.Count; j++)
                  if (schedule[j] >= (Date)couponConversionSchedule[i].Date)
                     ret[j] = couponConversionSchedule[i].Rate;

            return ret;
         }

         /// <summary>
         /// Create a FixedRate bond to the given date at the given price
         /// </summary>
         /// <returns>Bond</returns>
         private static Bond CreateNonCallableBond(Date date, decimal? price, BondBasics bondBasics, DateTime settlementDate, Schedule mainSchedule)
         {
            if (date != null &&
                date > settlementDate.AsDate() &&
                price.HasValue)
            {
               switch (bondBasics.CouponType)
               {
                  case CouponType.AdjRate:
                  case CouponType.OIS:
                  case CouponType.FixedRate:
                  case CouponType.DeferredInterest:
                     var sch = mainSchedule.until(date);
                     if (bondBasics.ConversionSchedule is { Length: > 0 })
                     {
                        var dates = bondBasics.ConversionSchedule.Select(x => x.Date).ToArray();
                        sch.addIrregularDates(dates);
                     }
                     var coupons = CreateCoupons(sch, bondBasics.Coupon, bondBasics.ConversionSchedule);
                     return new FixedRateBond(bondBasics.SettlementDays, (double)bondBasics.FaceAmount, sch,
                         coupons, bondBasics.BondDayCount, bondBasics.PaymentConvention,
                         (double)price.Value, bondBasics.IssueDate);
                  case CouponType.ZeroCoupon:
                  case CouponType.ShortTermDiscount:
                     return new ZeroCouponBond(bondBasics.SettlementDays, bondBasics.BondCalendar, (double)bondBasics.FaceAmount, date,
                         bondBasics.AccrualConvention, (double)price.Value, bondBasics.IssueDate);
               }
            }

            return null;
         }

         /// <summary>
         /// Check if the bond is subject to
         /// De Minimis Tax Rule
         /// </summary>
         /// <returns></returns>
         private static bool IsSubjectToDeMinimis(Bond bond, decimal price, BondBasics bondBasics, DateTime settlementDate)
         {
            var yearToMaturity = (decimal)bondBasics.BondDayCount.yearFraction(settlementDate, bond.maturityDate());

            if (bondBasics.CouponType == CouponType.ZeroCoupon &&
                 bondBasics.OriginalPrice.HasValue)
            {
               return bondBasics.OriginalPrice - (yearToMaturity * 0.25m) > price;
            }

            return bondBasics.FaceAmount - (yearToMaturity * 0.25m) > (price / 100 * bondBasics.FaceAmount);
         }

         /// <summary>
         /// Return Compounding type based on bond date
         /// </summary>
         /// <param name="bond"></param>
         /// <param name="couponType"></param>
         /// <param name="settlementDate"></param>
         /// <returns></returns>
         private static Compounding GetSecurityCompounding(Bond bond, CouponType couponType, DateTime settlementDate)
         {
            if (bond.nextCashFlowDate(settlementDate) == bond.maturityDate())
            {
               if (couponType is not CouponType.ZeroCoupon) // and not CouponType.ShortTermDiscount
                  return Compounding.Simple;

               if ((Date)settlementDate + new Period(Frequency.Semiannual) >= bond.maturityDate())
                  return Compounding.Simple;
            }
            return Compounding.Compounded;
         }

         #endregion

      }
      public enum PriceTypes
      {
         Percentage = 1,
         YieldToMaturity = 2,
         YieldToWorst = 3,
         Spread = 4,
      }
      public enum CouponTypeMethod
      {
         [Display(Name = "N/A")]
         Unknown,
         [Display(Name = "Fixed to Float")]
         FixedToFloat,
         [Display(Name = "Float to Fixed")]
         FloatToFixed,
         [Display(Name = "Fixed to Float to Fixed")]
         FixedToFloatToFixed,
         [Display(Name = "Float to Fixed to Float")]
         FloatToFixedToFloat
      }
      public class Sink
      {
         public DateTime Date { get; set; }
         public decimal Price { get; set; }
         public decimal Amount { get; set; }
         public override string ToString() => ($"Sink Date : {Date}\nSink Amount : {Amount}\nSink Price : {Price}");
      }
      public enum AssetType
      {
         [Display(Name = "N/A", ShortName = "N/A", Description = "N/A")]
         Other = 0,
         [Display(Name = "Corporate Bond", ShortName = "Corp", Description = "Corporate Bond")]
         CorporateBond = 1,
         [Display(Name = "Government/Agency Bond", ShortName = "Government/Agency Bond", Description = "Government/Agency Bond")]
         GovernmentAgencyBond = 2,
         [Display(Name = "US Municipal Bond", ShortName = "Muni", Description = "US Municipal Bond")]
         UsMunicipalBond = 3,
         [Display(Name = "Collateralized Mortgage Obligation/Asset-Backed Security", ShortName = "", Description = "")]
         CollateralizedMortgageObligationAssetBackedSecurity = 4,
         [Display(Name = "Mortgage-Backed Security", ShortName = "", Description = "")]
         MortgageBackedSecurity = 5,
         [Display(Name = "Money Market", ShortName = "Money Market", Description = "Money Market")]
         MoneyMarket = 6,
         [Display(Name = "Sample.Common Equity", ShortName = "Sample.Common Equity", Description = "Sample.Common Equity")]
         CommonEquity = 7,
         [Display(Name = "Preferred Equity", ShortName = "Preferred Equity", Description = "Preferred Equity")]
         PreferredEquity = 8,
         [Display(Name = "Right", ShortName = "Right", Description = "Right")]
         Right = 9,
         [Display(Name = "Warrant", ShortName = "Warrant", Description = "Warrant")]
         Warrant = 10,
         [Display(Name = "Option", ShortName = "Option", Description = "Option")]
         Option = 11,
         [Display(Name = "Future", ShortName = "Future", Description = "Future")]
         Future = 12,
         [Display(Name = "Swap", ShortName = "Swap", Description = "Swap")]
         Swap = 13,
         [Display(Name = "Currency", ShortName = "Currency", Description = "Currency")]
         Currency = 14,
         [Display(Name = "Commodity", ShortName = "Commodity", Description = "Commodity")]
         Commodity = 0xF,
         [Display(Name = "Index", ShortName = "Index", Description = "Index")]
         Index = 0x10,
         [Display(Name = "Mutual Fund/Unit Investment Trust", ShortName = "Mutual Fund/Unit Investment Trust", Description = "Mutual Fund/Unit Investment Trust")]
         MutualFundUnitInvestmentTrust = 17,
         [Display(Name = "Money Market Fund", ShortName = "Money Market Fund", Description = "Money Market Fund")]
         MoneyMarketFund = 18,
         [Display(Name = "Exchange Traded Fund", ShortName = "Exchange Traded Fund", Description = "Exchange Traded Fund")]
         ExchangeTradedFund = 19,
         [Display(Name = "Hybrid", ShortName = "Hybrid", Description = "Hybrid")]
         Hybrid = 20,
         [Display(Name = "Non-US Mortgage-Backed Security", ShortName = "Non-US Mortgage-Backed Security", Description = "Non-US Mortgage-Backed Security")]
         NonUsMortgageBackedSecurity = 21,
         [Display(Name = "Composite Unit", ShortName = "Composite Unit", Description = "Composite Unit")]
         CompositeUnit = 22,
         [Display(Name = "Treasury", ShortName = "Treasury", Description = "Treasury")]
         Treasury = 90
      }
      public enum TradingStatusType
      {
         [Display(Name = "N/A")]
         Unknown,
         [Display(Name = "Coupon Will Change")]
         CouponWillChange,
         [Display(Name = "Trading Flat")]
         TradingFlat,
         [Display(Name = "Ex-interest")]
         ExInterest,
         [Display(Name = "Normal")]
         Normal,
         [Display(Name = "Ex-stock")]
         ExStock,
         [Display(Name = "Variable interest")]
         VariableInterest,
         [Display(Name = "Ex-warrant")]
         ExWarrant
      }
      public class BondBasics
      {
         public PriceTypes PriceType { get; set; }
         public decimal Price { get; set; }
         public decimal Coupon { get; set; }
         public CouponType CouponType { get; set; }
         public CouponTypeMethod CouponTypeMethod { get; set; }
         public DateTime MaturityDate { get; set; }
         public DateTime? AccrualDate { get; set; }
         public DateTime? FirstCouponDate { get; set; }
         public DateTime? NextCouponChangeDate { get; set; }
         public DateTime? IssueDate { get; set; }

         public Redemption[] Call { get; set; }
         public Redemption Put { get; set; }
         public Redemption Refund { get; set; }
         public Redemption EffectiveCall { get; set; }

         public bool IsMandatoryPut { get; set; }

         // We have defaults for these for the moment
         public decimal FaceAmount { get; set; } = 1000.0m;
         public decimal Redemption { get; set; } = 100.0m;
         public int SettlementDays { get; set; }
         public decimal? OriginalPrice { get; set; }
         public decimal? OriginalYield { get; set; }

         public BusinessDayConvention AccrualConvention { get; set; } = BusinessDayConvention.Unadjusted;
         public BusinessDayConvention PaymentConvention { get; set; } = BusinessDayConvention.Unadjusted;
         public Compounding Compounding { get; set; } = Compounding.Compounded;
         public Frequency Frequency { get; set; } = Frequency.Semiannual;
         public DayCounter BondDayCount { get; set; } = new Thirty360(Thirty360.Thirty360Convention.BondBasis);
         public QLNet.Calendar BondCalendar { get; set; } = new TARGET();

         // Weighted Average Life fields
         public Sink[] SinkSchedule { get; set; }

         // Conversion Events
         public CouponConversion[] ConversionSchedule { get; set; }

         public AssetType? AssetType { get; set; }
         public TradingStatusType? TradingStatusType { get; set; }

         public override string ToString()
         {
            var sb = new StringBuilder();
            sb.AppendLine(CultureInfo.InvariantCulture, $"PriceType : {PriceType}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"Price : {Price}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"Coupon : {Coupon}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"CouponType : {CouponType}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"CouponTypeMehod : {CouponTypeMethod}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"MaturityDate : {MaturityDate}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"AccrualDate : {AccrualDate}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"FirstCouponDate : {FirstCouponDate}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"NextCouponChangeDate : {NextCouponChangeDate}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"IssueDate : {IssueDate}");

            if (Call != null)
            {
               foreach (var call in Call)
               {
                  sb.AppendLine(CultureInfo.InvariantCulture, $"CallSchedule call : {call}");
               }
            }
            sb.AppendLine(CultureInfo.InvariantCulture, $"Put : {Put}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"Refund : {Refund}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"EffectiveCall : {EffectiveCall}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"IsMandatoryPut : {IsMandatoryPut}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"FaceAmount : {FaceAmount}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"Redemption : {Redemption}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"SettlementDays : {SettlementDays}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"Original Price : {OriginalPrice}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"Original Yield : {OriginalYield}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"AccrualConvention : {AccrualConvention}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"PaymentConvention : {PaymentConvention}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"Compounding : {Compounding}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"Frequency : {Frequency}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"BondDayCount : {BondDayCount}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"BondCalendar : {BondCalendar}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"SinkSchedule : {SinkSchedule}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"TradingStatusType : {TradingStatusType}");
            if (SinkSchedule != null)
            {
               foreach (var sink in SinkSchedule)
               {
                  sb.AppendLine(CultureInfo.InvariantCulture, $"SinkSchedule sink : {sink}");
               }
            }
            sb.AppendLine(CultureInfo.InvariantCulture, $"ConversionSchedule : {ConversionSchedule}");
            if (ConversionSchedule != null)
            {
               foreach (var couponConversion in ConversionSchedule)
               {
                  sb.AppendLine(CultureInfo.InvariantCulture, $"ConversionSchedule coupon : {couponConversion}");
               }
            }
            sb.AppendLine(CultureInfo.InvariantCulture, $"AssetType : {AssetType}");
            return sb.ToString();
         }

         public BondBasics ShallowCopy()
         {
            return (BondBasics)MemberwiseClone();
         }
      }
      public class CalcResults
      {
         public decimal Price { get; set; }
         public decimal DirtyPrice { get; set; }
         public decimal AccruedInterest { get; set; }
         public int AccruedDays { get; set; }
         public DateTime SettlementDate { get; set; }

         public bool IsMandatoryPut { get; set; }
         public bool? SubjectToDeMinimis { get; set; }

         public List<YieldCalc> Yields { get; set; } = new List<YieldCalc>();
         public List<SpreadCalc> Spreads { get; set; } = new List<SpreadCalc>();



         public YieldCalc YieldToCall => Yields.FirstOrDefault(y => y.Type == YieldType.YieldToCall);
         public YieldCalc YieldToMaturity => Yields.FirstOrDefault(y => y.Type == YieldType.YieldToMaturity);
         public YieldCalc YieldToPut => Yields.FirstOrDefault(y => y.Type == YieldType.YieldToPut);
         public YieldCalc YieldToRefunding => Yields.FirstOrDefault(y => y.Type == YieldType.YieldToRefunding);
         public YieldCalc YieldToConversion => Yields.FirstOrDefault(y => y.Type == YieldType.YieldToConversion);
         public YieldCalc YieldToAverageLife => Yields.FirstOrDefault(y => y.Type == YieldType.YieldToAverageLife);
         public YieldCalc YieldToEffCall => Yields.FirstOrDefault(y => y.Type == YieldType.YieldToEffCall);


         // Spreads
         public SpreadCalc SpreadToBenchmark => Spreads?.FirstOrDefault(x => x.Type == SpreadType.Benchmark);
         public SpreadCalc SpreadToEval => Spreads?.FirstOrDefault(x => x.Type == SpreadType.Eval);

         // OrderBy orders by yield ascending, so the first one has the lowest yield.
         // If the put is mandatory than we want to include that yield in YTW,
         // if it's not mandatory we want to skip it
         // we always skip YieldToAverageLife
         public YieldCalc YieldToWorst => IsMandatoryPut
             ? Yields.Where(y => y.Type != YieldType.YieldToAverageLife).MinBy(y => y.Yield)
             : Yields.Where(y => y.Type != YieldType.YieldToPut && y.Type != YieldType.YieldToAverageLife).MinBy(y => y.Yield);

         public DateTime? WeightedAverageLife { get; set; }
         public decimal ModifiedDuration { get; set; }
         public decimal EffectiveDuration { get; set; }
      }
      public class YieldCalc
      {
         public YieldType Type { get; set; }
         public decimal Yield { get; set; }
         public DateTime Date { get; set; }
         public decimal Redemption { get; set; }
      }
      public class SpreadCalc
      {
         public SpreadType Type { get; set; }
         public decimal Bid { get; set; }
         public decimal? Mid { get; set; }
         public decimal Offer { get; set; }
      }
      public class Redemption
      {
         public DateTime? Date { get; set; }
         public decimal? Price { get; set; }
         public override string ToString() => ($"Redemption Date : {Date}\nRedemption Price : {Price}");
      }
      public enum YieldType
      {
         YieldToMaturity,
         YieldToCall,
         YieldToPut,
         YieldToRefunding,
         YieldToConversion,
         YieldToAverageLife,
         YieldToEffCall
      }
      public enum CouponType
      {
         FixedRate,
         AdjRate,
         OIS,
         ZeroCoupon,
         DeferredInterest,
         ShortTermDiscount
      }
      public enum SpreadType
      {
         Unknown,
         Benchmark,
         Eval,
      }
      #endregion

   }

   public static class Extensions
   {
      public static Date AsDate(this DateTime? date)
      {
         return date;
      }

      public static Date AsDate(this DateTime date)
      {
         return date;
      }
   }
}
