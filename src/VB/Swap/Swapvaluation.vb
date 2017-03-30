Imports QLNet

Module Swapvaluation

    Sub Main()
        Dim timer As DateTime = DateTime.Now

        '********************
        '***  MARKET DATA  ***
        '*********************/

        Dim calendar As Calendar = New TARGET()

        Dim settlementDate As [Date] = New [Date](22, Month.September, 2004)
        ' must be a business day
        settlementDate = calendar.adjust(settlementDate)

        Dim fixingDays As Integer = 2
        Dim todaysDate As [Date] = calendar.advance(settlementDate, -fixingDays, TimeUnit.Days)

        ' nothing to do with Date::todaysDate

        Settings.setEvaluationDate(todaysDate)


        todaysDate = Settings.evaluationDate()
        Console.WriteLine("Today: {0}, {1}", todaysDate.DayOfWeek, todaysDate)
        Console.WriteLine("Settlement date: {0}, {1}", settlementDate.DayOfWeek, settlementDate)


        ' deposits
        Dim d1wQuote As Double = 0.0382
        Dim d1mQuote As Double = 0.0372
        Dim d3mQuote As Double = 0.0363
        Dim d6mQuote As Double = 0.0353
        Dim d9mQuote As Double = 0.0348
        Dim d1yQuote As Double = 0.0345
        ' FRAs
        Dim fra3x6Quote As Double = 0.037125
        Dim fra6x9Quote As Double = 0.037125
        Dim fra6x12Quote As Double = 0.037125
        ' futures
        Dim fut1Quote As Double = 96.2875
        Dim fut2Quote As Double = 96.7875
        Dim fut3Quote As Double = 96.9875
        Dim fut4Quote As Double = 96.6875
        Dim fut5Quote As Double = 96.4875
        Dim fut6Quote As Double = 96.3875
        Dim fut7Quote As Double = 96.2875
        Dim fut8Quote As Double = 96.0875
        ' swaps
        Dim s2yQuote As Double = 0.037125
        Dim s3yQuote As Double = 0.0398
        Dim s5yQuote As Double = 0.0443
        Dim s10yQuote As Double = 0.05165
        Dim s15yQuote As Double = 0.055175


        '********************
        '**    QUOTES    ***
        '*******************/

        ' SimpleQuote stores a value which can be manually changed;
        ' other Quote subclasses could read the value from a database
        ' or some kind of data feed.

        ' deposits
        Dim d1wRate As Quote = New SimpleQuote(d1wQuote)
        Dim d1mRate As Quote = New SimpleQuote(d1mQuote)
        Dim d3mRate As Quote = New SimpleQuote(d3mQuote)
        Dim d6mRate As Quote = New SimpleQuote(d6mQuote)
        Dim d9mRate As Quote = New SimpleQuote(d9mQuote)
        Dim d1yRate As Quote = New SimpleQuote(d1yQuote)
        ' FRAs
        Dim fra3x6Rate As Quote = New SimpleQuote(fra3x6Quote)
        Dim fra6x9Rate As Quote = New SimpleQuote(fra6x9Quote)
        Dim fra6x12Rate As Quote = New SimpleQuote(fra6x12Quote)
        ' futures
        Dim fut1Price As Quote = New SimpleQuote(fut1Quote)
        Dim fut2Price As Quote = New SimpleQuote(fut2Quote)
        Dim fut3Price As Quote = New SimpleQuote(fut3Quote)
        Dim fut4Price As Quote = New SimpleQuote(fut4Quote)
        Dim fut5Price As Quote = New SimpleQuote(fut5Quote)
        Dim fut6Price As Quote = New SimpleQuote(fut6Quote)
        Dim fut7Price As Quote = New SimpleQuote(fut7Quote)
        Dim fut8Price As Quote = New SimpleQuote(fut8Quote)
        ' swaps
        Dim s2yRate As Quote = New SimpleQuote(s2yQuote)
        Dim s3yRate As Quote = New SimpleQuote(s3yQuote)
        Dim s5yRate As Quote = New SimpleQuote(s5yQuote)
        Dim s10yRate As Quote = New SimpleQuote(s10yQuote)
        Dim s15yRate As Quote = New SimpleQuote(s15yQuote)


        '*********************
        '**  RATE HELPERS ***
        '********************/

        ' RateHelpers are built from the above quotes together with
        ' other instrument dependant infos.  Quotes are passed in
        ' relinkable handles which could be relinked to some other
        ' data source later.

        ' deposits
        Dim depositDayCounter As DayCounter = New Actual360()

      Dim d1w As RateHelper = New DepositRateHelper(New Handle(Of Quote)(d1wRate), New Period(1, TimeUnit.Weeks), fixingDays, calendar, BusinessDayConvention.ModifiedFollowing, True, depositDayCounter)
      Dim d1m As RateHelper = New DepositRateHelper(New Handle(Of Quote)(d1mRate), New Period(1, TimeUnit.Months), fixingDays, calendar, BusinessDayConvention.ModifiedFollowing, True, depositDayCounter)
      Dim d3m As RateHelper = New DepositRateHelper(New Handle(Of Quote)(d3mRate), New Period(3, TimeUnit.Months), fixingDays, calendar, BusinessDayConvention.ModifiedFollowing, True, depositDayCounter)
      Dim d6m As RateHelper = New DepositRateHelper(New Handle(Of Quote)(d6mRate), New Period(6, TimeUnit.Months), fixingDays, calendar, BusinessDayConvention.ModifiedFollowing, True, depositDayCounter)
      Dim d9m As RateHelper = New DepositRateHelper(New Handle(Of Quote)(d9mRate), New Period(9, TimeUnit.Months), fixingDays, calendar, BusinessDayConvention.ModifiedFollowing, True, depositDayCounter)
      Dim d1y As RateHelper = New DepositRateHelper(New Handle(Of Quote)(d1yRate), New Period(1, TimeUnit.Years), fixingDays, calendar, BusinessDayConvention.ModifiedFollowing, True, depositDayCounter)

        ' setup FRAs
      Dim fra3x6 As RateHelper = New FraRateHelper(New Handle(Of Quote)(fra3x6Rate), 3, 6, fixingDays, calendar, BusinessDayConvention.ModifiedFollowing, True, depositDayCounter)
      Dim fra6x9 As RateHelper = New FraRateHelper(New Handle(Of Quote)(fra6x9Rate), 6, 9, fixingDays, calendar, BusinessDayConvention.ModifiedFollowing, True, depositDayCounter)
      Dim fra6x12 As RateHelper = New FraRateHelper(New Handle(Of Quote)(fra6x12Rate), 6, 12, fixingDays, calendar, BusinessDayConvention.ModifiedFollowing, True, depositDayCounter)

        ' setup futures
        ' Handle<Quote> convexityAdjustment = new Handle<Quote>(new SimpleQuote(0.0));
        Dim futMonths As Integer = 3

        Dim imm As [Date] = QLNet.IMM.nextDate(settlementDate)
      Dim fut1 As RateHelper = New FuturesRateHelper(New Handle(Of Quote)(fut1Price), imm, futMonths, calendar, BusinessDayConvention.ModifiedFollowing, True, depositDayCounter)

        imm = QLNet.IMM.nextDate(imm + 1)
      Dim fut2 As RateHelper = New FuturesRateHelper(New Handle(Of Quote)(fut2Price), imm, futMonths, calendar, BusinessDayConvention.ModifiedFollowing, True, depositDayCounter)

        imm = QLNet.IMM.nextDate(imm + 1)
      Dim fut3 As RateHelper = New FuturesRateHelper(New Handle(Of Quote)(fut3Price), imm, futMonths, calendar, BusinessDayConvention.ModifiedFollowing, True, depositDayCounter)

        imm = QLNet.IMM.nextDate(imm + 1)
      Dim fut4 As RateHelper = New FuturesRateHelper(New Handle(Of Quote)(fut4Price), imm, futMonths, calendar, BusinessDayConvention.ModifiedFollowing, True, depositDayCounter)

        imm = QLNet.IMM.nextDate(imm + 1)
      Dim fut5 As RateHelper = New FuturesRateHelper(New Handle(Of Quote)(fut5Price), imm, futMonths, calendar, BusinessDayConvention.ModifiedFollowing, True, depositDayCounter)

        imm = QLNet.IMM.nextDate(imm + 1)
      Dim fut6 As RateHelper = New FuturesRateHelper(New Handle(Of Quote)(fut6Price), imm, futMonths, calendar, BusinessDayConvention.ModifiedFollowing, True, depositDayCounter)

        imm = QLNet.IMM.nextDate(imm + 1)
      Dim fut7 As RateHelper = New FuturesRateHelper(New Handle(Of Quote)(fut7Price), imm, futMonths, calendar, BusinessDayConvention.ModifiedFollowing, True, depositDayCounter)

        imm = QLNet.IMM.nextDate(imm + 1)
      Dim fut8 As RateHelper = New FuturesRateHelper(New Handle(Of Quote)(fut8Price), imm, futMonths, calendar, BusinessDayConvention.ModifiedFollowing, True, depositDayCounter)

        ' setup swaps
        Dim swFixedLegFrequency As Frequency = Frequency.Annual
        Dim swFixedLegConvention As BusinessDayConvention = BusinessDayConvention.Unadjusted
        Dim swFixedLegDayCounter As DayCounter = New Thirty360(Thirty360.Thirty360Convention.European)

        Dim swFloatingLegIndex As IborIndex = New Euribor6M()

      Dim s2y As RateHelper = New SwapRateHelper(New Handle(Of Quote)(s2yRate), New Period(2, TimeUnit.Years), calendar, swFixedLegFrequency, swFixedLegConvention, swFixedLegDayCounter, swFloatingLegIndex)
      Dim s3y As RateHelper = New SwapRateHelper(New Handle(Of Quote)(s3yRate), New Period(3, TimeUnit.Years), calendar, swFixedLegFrequency, swFixedLegConvention, swFixedLegDayCounter, swFloatingLegIndex)
      Dim s5y As RateHelper = New SwapRateHelper(New Handle(Of Quote)(s5yRate), New Period(5, TimeUnit.Years), calendar, swFixedLegFrequency, swFixedLegConvention, swFixedLegDayCounter, swFloatingLegIndex)
      Dim s10y As RateHelper = New SwapRateHelper(New Handle(Of Quote)(s10yRate), New Period(10, TimeUnit.Years), calendar, swFixedLegFrequency, swFixedLegConvention, swFixedLegDayCounter, swFloatingLegIndex)
      Dim s15y As RateHelper = New SwapRateHelper(New Handle(Of Quote)(s15yRate), New Period(15, TimeUnit.Years), calendar, swFixedLegFrequency, swFixedLegConvention, swFixedLegDayCounter, swFloatingLegIndex)

        '*********************
        '*  CURVE BUILDING **
        '********************/

        ' Any DayCounter would be fine.
        ' ActualActual::ISDA ensures that 30 years is 30.0
        Dim termStructureDayCounter As DayCounter = New ActualActual(ActualActual.Convention.ISDA)

        Dim tolerance As Double = 0.000000000000001

        ' A depo-swap curve
      Dim depoSwapInstruments As List(Of RateHelper) = New List(Of RateHelper)()
        depoSwapInstruments.Add(d1w)
        depoSwapInstruments.Add(d1m)
        depoSwapInstruments.Add(d3m)
        depoSwapInstruments.Add(d6m)
        depoSwapInstruments.Add(d9m)
        depoSwapInstruments.Add(d1y)
        depoSwapInstruments.Add(s2y)
        depoSwapInstruments.Add(s3y)
        depoSwapInstruments.Add(s5y)
        depoSwapInstruments.Add(s10y)
        depoSwapInstruments.Add(s15y)
        Dim depoSwapTermStructure As YieldTermStructure = New PiecewiseYieldCurve(Of Discount, LogLinear)( _
                    settlementDate, depoSwapInstruments, termStructureDayCounter, _
                    New List(Of Handle(Of Quote))(), New List(Of [Date])(), tolerance)

        ' A depo-futures-swap curve
      Dim depoFutSwapInstruments As List(Of RateHelper) = New List(Of RateHelper)()
        depoFutSwapInstruments.Add(d1w)
        depoFutSwapInstruments.Add(d1m)
        depoFutSwapInstruments.Add(fut1)
        depoFutSwapInstruments.Add(fut2)
        depoFutSwapInstruments.Add(fut3)
        depoFutSwapInstruments.Add(fut4)
        depoFutSwapInstruments.Add(fut5)
        depoFutSwapInstruments.Add(fut6)
        depoFutSwapInstruments.Add(fut7)
        depoFutSwapInstruments.Add(fut8)
        depoFutSwapInstruments.Add(s3y)
        depoFutSwapInstruments.Add(s5y)
        depoFutSwapInstruments.Add(s10y)
        depoFutSwapInstruments.Add(s15y)
        Dim depoFutSwapTermStructure As YieldTermStructure = New PiecewiseYieldCurve(Of Discount, LogLinear)( _
                settlementDate, depoFutSwapInstruments, termStructureDayCounter, New List(Of Handle(Of Quote))(), New List(Of [Date])(), tolerance)

        ' A depo-FRA-swap curve
      Dim depoFRASwapInstruments As List(Of RateHelper) = New List(Of RateHelper)()
        depoFRASwapInstruments.Add(d1w)
        depoFRASwapInstruments.Add(d1m)
        depoFRASwapInstruments.Add(d3m)
        depoFRASwapInstruments.Add(fra3x6)
        depoFRASwapInstruments.Add(fra6x9)
        depoFRASwapInstruments.Add(fra6x12)
        depoFRASwapInstruments.Add(s2y)
        depoFRASwapInstruments.Add(s3y)
        depoFRASwapInstruments.Add(s5y)
        depoFRASwapInstruments.Add(s10y)
        depoFRASwapInstruments.Add(s15y)
        Dim depoFRASwapTermStructure As YieldTermStructure = New PiecewiseYieldCurve(Of Discount, LogLinear)( _
                      settlementDate, depoFRASwapInstruments, termStructureDayCounter, New List(Of Handle(Of Quote))(), New List(Of [Date])(), tolerance)

        ' Term structures that will be used for pricing:
        ' the one used for discounting cash flows
        Dim discountingTermStructure As RelinkableHandle(Of YieldTermStructure) = New RelinkableHandle(Of YieldTermStructure)()
        ' the one used for forward rate forecasting
        Dim forecastingTermStructure As RelinkableHandle(Of YieldTermStructure) = New RelinkableHandle(Of YieldTermStructure)()

        '*********************
        ' SWAPS TO BE PRICED *
        '*********************/

        ' constant nominal 1,000,000 Euro
        Dim nominal As Double = 1000000.0
        ' fixed leg
        Dim fixedLegFrequency As Frequency = Frequency.Annual
        Dim fixedLegConvention As BusinessDayConvention = BusinessDayConvention.Unadjusted
        Dim floatingLegConvention As BusinessDayConvention = BusinessDayConvention.ModifiedFollowing
        Dim fixedLegDayCounter As DayCounter = New Thirty360(Thirty360.Thirty360Convention.European)
        Dim fixedRate As Double = 0.04
        Dim floatingLegDayCounter As DayCounter = New Actual360()

        ' floating leg
        Dim floatingLegFrequency As Frequency = Frequency.Semiannual
        Dim euriborIndex As IborIndex = New Euribor6M(forecastingTermStructure)
        Dim spread As Double = 0.0

        Dim lenghtInYears As Integer = 5
        Dim swapType As VanillaSwap.Type = VanillaSwap.Type.Payer

        Dim maturity As [Date] = settlementDate + New Period(lenghtInYears, TimeUnit.Years)
        Dim fixedSchedule As Schedule = New Schedule(settlementDate, maturity, New Period(fixedLegFrequency), _
                                 calendar, fixedLegConvention, fixedLegConvention, DateGeneration.Rule.Forward, False)
        Dim floatSchedule As Schedule = New Schedule(settlementDate, maturity, New Period(floatingLegFrequency), _
                                 calendar, floatingLegConvention, floatingLegConvention, DateGeneration.Rule.Forward, False)
        Dim spot5YearSwap As VanillaSwap = New VanillaSwap(swapType, nominal, fixedSchedule, fixedRate, fixedLegDayCounter, _
                                    floatSchedule, euriborIndex, spread, floatingLegDayCounter)

        Dim fwdStart As [Date] = calendar.advance(settlementDate, 1, TimeUnit.Years)
        Dim fwdMaturity As [Date] = fwdStart + New Period(lenghtInYears, TimeUnit.Years)
        Dim fwdFixedSchedule As Schedule = New Schedule(fwdStart, fwdMaturity, New Period(fixedLegFrequency), _
                                    calendar, fixedLegConvention, fixedLegConvention, DateGeneration.Rule.Forward, False)
        Dim fwdFloatSchedule As Schedule = New Schedule(fwdStart, fwdMaturity, New Period(floatingLegFrequency), _
                                    calendar, floatingLegConvention, floatingLegConvention, DateGeneration.Rule.Forward, False)
        Dim oneYearForward5YearSwap As VanillaSwap = New VanillaSwap(swapType, nominal, fwdFixedSchedule, fixedRate, fixedLegDayCounter, _
                                    fwdFloatSchedule, euriborIndex, spread, floatingLegDayCounter)

        '***************
        ' SWAP PRICING *
        '***************/

        ' utilities for reporting
        Dim headers As List(Of String) = New List(Of String)()
        headers.Add("term structure")
        headers.Add("net present value")
        headers.Add("fair spread")
        headers.Add("fair fixed rate")
        Dim separator As String = " | "
        Dim width As Integer = headers(0).Length + separator.Length + _
                               headers(1).Length + separator.Length + _
                               headers(2).Length + separator.Length + _
                               headers(3).Length + separator.Length - 1

        Dim rule As String = String.Format("").PadLeft(width, "-")
        Dim dblrule As String = String.Format("").PadLeft(width, "=")
        Dim tab As String = String.Format("").PadLeft(8, " ")

        ' calculations

        Console.WriteLine(dblrule)
        Console.WriteLine("5-year market swap-rate = {0:0.00%}", s5yRate.value())
        Console.WriteLine(dblrule)

        Console.WriteLine(tab + "5-years swap paying {0:0.00%}", fixedRate)
        Console.WriteLine(headers(0) & separator _
                  & headers(1) & separator _
                  & headers(2) & separator _
                  & headers(3) & separator)
        Console.WriteLine(rule)

        Dim NPV As Double
        Dim fairRate As Double
        Dim fairSpread As Double

		Dim swapEngine As IPricingEngine = New DiscountingSwapEngine(discountingTermStructure)

        spot5YearSwap.setPricingEngine(swapEngine)
        oneYearForward5YearSwap.setPricingEngine(swapEngine)

        ' Of course, you're not forced to really use different curves
        forecastingTermStructure.linkTo(depoSwapTermStructure)
        discountingTermStructure.linkTo(depoSwapTermStructure)

        NPV = spot5YearSwap.NPV()
        fairSpread = spot5YearSwap.fairSpread()
        fairRate = spot5YearSwap.fairRate()

        Console.Write("{0," & headers(0).Length & ":0.00}" & separator, "depo-swap")
        Console.Write("{0," & headers(1).Length & ":0.00}" & separator, NPV)
        Console.Write("{0," & headers(2).Length & ":0.00%}" & separator, fairSpread)
        Console.WriteLine("{0," & headers(3).Length & ":0.00%}" & separator, fairRate)

        ' let's check that the 5 years swap has been correctly re-priced
        If (Not (Math.Abs(fairRate - s5yQuote) < 0.00000001)) Then
            Throw New ApplicationException("5-years swap mispriced by " + Math.Abs(fairRate - s5yQuote))
        End If

        forecastingTermStructure.linkTo(depoFutSwapTermStructure)
        discountingTermStructure.linkTo(depoFutSwapTermStructure)

        NPV = spot5YearSwap.NPV()
        fairSpread = spot5YearSwap.fairSpread()
        fairRate = spot5YearSwap.fairRate()

        Console.Write("{0," & headers(0).Length & ":0.00}" & separator, "depo-fut-swap")
        Console.Write("{0," & headers(1).Length & ":0.00}" & separator, NPV)
        Console.Write("{0," & headers(2).Length & ":0.00%}" & separator, fairSpread)
        Console.WriteLine("{0," & headers(3).Length & ":0.00%}" & separator, fairRate)

        If (Not (Math.Abs(fairRate - s5yQuote) < 0.00000001)) Then
            Throw New ApplicationException("5-years swap mispriced by " + Math.Abs(fairRate - s5yQuote))
        End If


        forecastingTermStructure.linkTo(depoFRASwapTermStructure)
        discountingTermStructure.linkTo(depoFRASwapTermStructure)

        NPV = spot5YearSwap.NPV()
        fairSpread = spot5YearSwap.fairSpread()
        fairRate = spot5YearSwap.fairRate()

        Console.Write("{0," & headers(0).Length & ":0.00}" & separator, "depo-FRA-swap")
        Console.Write("{0," & headers(1).Length & ":0.00}" & separator, NPV)
        Console.Write("{0," & headers(2).Length & ":0.00%}" & separator, fairSpread)
        Console.WriteLine("{0," & headers(3).Length & ":0.00%}" & separator, fairRate)

        If (Not (Math.Abs(fairRate - s5yQuote) < 0.00000001)) Then
            Throw New ApplicationException("5-years swap mispriced by " + Math.Abs(fairRate - s5yQuote))
        End If

        Console.WriteLine(rule)

        ' now let's price the 1Y forward 5Y swap
        Console.WriteLine(tab & "5-years, 1-year forward swap paying {0:0.00%}", fixedRate)
        Console.WriteLine(headers(0) & separator _
                  & headers(1) & separator _
                  & headers(2) & separator _
                  & headers(3) & separator)
        Console.WriteLine(rule)

        forecastingTermStructure.linkTo(depoSwapTermStructure)
        discountingTermStructure.linkTo(depoSwapTermStructure)

        NPV = oneYearForward5YearSwap.NPV()
        fairSpread = oneYearForward5YearSwap.fairSpread()
        fairRate = oneYearForward5YearSwap.fairRate()

        Console.Write("{0," & headers(0).Length & ":0.00}" & separator, "depo-swap")
        Console.Write("{0," & headers(1).Length & ":0.00}" & separator, NPV)
        Console.Write("{0," & headers(2).Length & ":0.00%}" & separator, fairSpread)
        Console.WriteLine("{0," & headers(3).Length & ":0.00%}" & separator, fairRate)

        forecastingTermStructure.linkTo(depoFutSwapTermStructure)
        discountingTermStructure.linkTo(depoFutSwapTermStructure)

        NPV = oneYearForward5YearSwap.NPV()
        fairSpread = oneYearForward5YearSwap.fairSpread()
        fairRate = oneYearForward5YearSwap.fairRate()

        Console.Write("{0," & headers(0).Length & ":0.00}" & separator, "depo-fut-swap")
        Console.Write("{0," & headers(1).Length & ":0.00}" & separator, NPV)
        Console.Write("{0," & headers(2).Length & ":0.00%}" & separator, fairSpread)
        Console.WriteLine("{0," & headers(3).Length & ":0.00%}" & separator, fairRate)

        forecastingTermStructure.linkTo(depoFRASwapTermStructure)
        discountingTermStructure.linkTo(depoFRASwapTermStructure)

        NPV = oneYearForward5YearSwap.NPV()
        fairSpread = oneYearForward5YearSwap.fairSpread()
        fairRate = oneYearForward5YearSwap.fairRate()

        Console.Write("{0," & headers(0).Length & ":0.00}" & separator, "depo-FRA-swap")
        Console.Write("{0," & headers(1).Length & ":0.00}" & separator, NPV)
        Console.Write("{0," & headers(2).Length & ":0.00%}" & separator, fairSpread)
        Console.WriteLine("{0," & headers(3).Length & ":0.00%}" & separator, fairRate)

        ' now let's say that the 5-years swap rate goes up to 4.60%.
        ' A smarter market element--say, connected to a data source-- would
        ' notice the change itself. Since we're using SimpleQuotes,
        ' we'll have to change the value manually--which forces us to
        ' downcast the handle and use the SimpleQuote
        ' interface. In any case, the point here is that a change in the
        ' value contained in the Quote triggers a new bootstrapping
        ' of the curve and a repricing of the swap.

        Dim fiveYearsRate As SimpleQuote = s5yRate
        fiveYearsRate.setValue(0.046)

        Console.WriteLine(dblrule)
        Console.WriteLine("5-year market swap-rate = {0:0.00%}", s5yRate.value())
        Console.WriteLine(dblrule)

        Console.WriteLine(tab + "5-years swap paying {0:0.00%}", fixedRate)
        Console.WriteLine(headers(0) & separator _
                  & headers(1) & separator _
                  & headers(2) & separator _
                  & headers(3) & separator)
        Console.WriteLine(rule)

        ' now get the updated results
        forecastingTermStructure.linkTo(depoSwapTermStructure)
        discountingTermStructure.linkTo(depoSwapTermStructure)

        NPV = spot5YearSwap.NPV()
        fairSpread = spot5YearSwap.fairSpread()
        fairRate = spot5YearSwap.fairRate()

        Console.Write("{0," & headers(0).Length & ":0.00}" & separator, "depo-swap")
        Console.Write("{0," & headers(1).Length & ":0.00}" & separator, NPV)
        Console.Write("{0," & headers(2).Length & ":0.00%}" & separator, fairSpread)
        Console.WriteLine("{0," & headers(3).Length & ":0.00%}" & separator, fairRate)

        If (Not (Math.Abs(fairRate - s5yRate.value()) < 0.00000001)) Then
            Throw New ApplicationException("5-years swap mispriced by " + Math.Abs(fairRate - s5yRate.value()))
        End If

        forecastingTermStructure.linkTo(depoFutSwapTermStructure)
        discountingTermStructure.linkTo(depoFutSwapTermStructure)

        NPV = spot5YearSwap.NPV()
        fairSpread = spot5YearSwap.fairSpread()
        fairRate = spot5YearSwap.fairRate()

        Console.Write("{0," & headers(0).Length & ":0.00}" & separator, "depo-fut-swap")
        Console.Write("{0," & headers(1).Length & ":0.00}" & separator, NPV)
        Console.Write("{0," & headers(2).Length & ":0.00%}" & separator, fairSpread)
        Console.WriteLine("{0," & headers(3).Length & ":0.00%}" & separator, fairRate)

        If (Not (Math.Abs(fairRate - s5yRate.value()) < 0.00000001)) Then
            Throw New ApplicationException("5-years swap mispriced by " + Math.Abs(fairRate - s5yRate.value()))
        End If

        forecastingTermStructure.linkTo(depoFRASwapTermStructure)
        discountingTermStructure.linkTo(depoFRASwapTermStructure)

        NPV = spot5YearSwap.NPV()
        fairSpread = spot5YearSwap.fairSpread()
        fairRate = spot5YearSwap.fairRate()

        Console.Write("{0," & headers(0).Length & ":0.00}" & separator, "depo-FRA-swap")
        Console.Write("{0," & headers(1).Length & ":0.00}" & separator, NPV)
        Console.Write("{0," & headers(2).Length & ":0.00%}" & separator, fairSpread)
        Console.WriteLine("{0," & headers(3).Length & ":0.00%}" & separator, fairRate)

        If (Not (Math.Abs(fairRate - s5yRate.value()) < 0.00000001)) Then
            Throw New ApplicationException("5-years swap mispriced by " + Math.Abs(fairRate - s5yRate.value()))
        End If

        Console.WriteLine(rule)

        ' the 1Y forward 5Y swap changes as well

        Console.WriteLine(tab + "5-years, 1-year forward swap paying {0:0.00%}", fixedRate)
        Console.WriteLine(headers(0) & separator _
                  & headers(1) & separator _
                  & headers(2) & separator _
                  & headers(3) & separator)
        Console.WriteLine(rule)

        forecastingTermStructure.linkTo(depoSwapTermStructure)
        discountingTermStructure.linkTo(depoSwapTermStructure)

        NPV = oneYearForward5YearSwap.NPV()
        fairSpread = oneYearForward5YearSwap.fairSpread()
        fairRate = oneYearForward5YearSwap.fairRate()

        Console.Write("{0," & headers(0).Length & ":0.00}" & separator, "depo-swap")
        Console.Write("{0," & headers(1).Length & ":0.00}" & separator, NPV)
        Console.Write("{0," & headers(2).Length & ":0.00%}" & separator, fairSpread)
        Console.WriteLine("{0," & headers(3).Length & ":0.00%}" & separator, fairRate)

        forecastingTermStructure.linkTo(depoFutSwapTermStructure)
        discountingTermStructure.linkTo(depoFutSwapTermStructure)

        NPV = oneYearForward5YearSwap.NPV()
        fairSpread = oneYearForward5YearSwap.fairSpread()
        fairRate = oneYearForward5YearSwap.fairRate()

        Console.Write("{0," & headers(0).Length & ":0.00}" & separator, "depo-fut-swap")
        Console.Write("{0," & headers(1).Length & ":0.00}" & separator, NPV)
        Console.Write("{0," & headers(2).Length & ":0.00%}" & separator, fairSpread)
        Console.WriteLine("{0," & headers(3).Length & ":0.00%}" & separator, fairRate)

        forecastingTermStructure.linkTo(depoFRASwapTermStructure)
        discountingTermStructure.linkTo(depoFRASwapTermStructure)

        NPV = oneYearForward5YearSwap.NPV()
        fairSpread = oneYearForward5YearSwap.fairSpread()
        fairRate = oneYearForward5YearSwap.fairRate()

        Console.Write("{0," & headers(0).Length & ":0.00}" & separator, "depo-FRA-swap")
        Console.Write("{0," & headers(1).Length & ":0.00}" & separator, NPV)
        Console.Write("{0," & headers(2).Length & ":0.00%}" & separator, fairSpread)
        Console.WriteLine("{0," & headers(3).Length & ":0.00%}" & separator, fairRate)


        Console.WriteLine(" \nRun completed in {0}", DateTime.Now - timer)
        Console.WriteLine()

    End Sub

End Module
