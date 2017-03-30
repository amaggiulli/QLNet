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
using QLNet;

namespace Repo {
	public class Repo {
		static void Main(string[] args) {
			DateTime timer = DateTime.Now;

			Date repoSettlementDate = new Date(14,Month.February,2000);;
			Date repoDeliveryDate = new Date(15,Month.August,2000);
			double repoRate = 0.05;
			DayCounter repoDayCountConvention = new Actual360();
			int repoSettlementDays = 0;
			Compounding repoCompounding = Compounding.Simple;
			Frequency repoCompoundFreq = Frequency.Annual;

			// assume a ten year bond- this is irrelevant
			Date bondIssueDate = new Date(15,Month.September,1995);
			Date bondDatedDate = new Date(15,Month.September,1995);
			Date bondMaturityDate = new Date(15,Month.September,2005);
			double bondCoupon = 0.08;
			Frequency bondCouponFrequency = Frequency.Semiannual;
			// unknown what calendar fincad is using
			Calendar bondCalendar = new NullCalendar();
			DayCounter bondDayCountConvention = new Thirty360(Thirty360.Thirty360Convention.BondBasis);
			// unknown what fincad is using. this may affect accrued calculation
			int bondSettlementDays = 0;
			BusinessDayConvention bondBusinessDayConvention = BusinessDayConvention.Unadjusted;
			double bondCleanPrice = 89.97693786;
			double bondRedemption = 100.0;
			double faceAmount = 100.0;


			Settings.setEvaluationDate(repoSettlementDate);

			RelinkableHandle<YieldTermStructure> bondCurve = new RelinkableHandle<YieldTermStructure>();
			bondCurve.linkTo(new FlatForward(repoSettlementDate,
											   .01, // dummy rate
											   bondDayCountConvention,
											   Compounding.Compounded,
											   bondCouponFrequency));

			/*
			boost::shared_ptr<FixedRateBond> bond(
						   new FixedRateBond(faceAmount,
											 bondIssueDate,
											 bondDatedDate,
											 bondMaturityDate,
											 bondSettlementDays,
											 std::vector<Rate>(1,bondCoupon),
											 bondCouponFrequency,
											 bondCalendar,
											 bondDayCountConvention,
											 bondBusinessDayConvention,
											 bondBusinessDayConvention,
											 bondRedemption,
											 bondCurve));
			*/

			Schedule bondSchedule = new Schedule(bondDatedDate, bondMaturityDate,
								  new Period(bondCouponFrequency),
								  bondCalendar,bondBusinessDayConvention,
								  bondBusinessDayConvention,
								  DateGeneration.Rule.Backward,false);
			FixedRateBond bond = new FixedRateBond(bondSettlementDays,
											 faceAmount,
											 bondSchedule,
											 new List<double>() { bondCoupon },
											 bondDayCountConvention,
											 bondBusinessDayConvention,
											 bondRedemption,
											 bondIssueDate);
			bond.setPricingEngine(new DiscountingBondEngine(bondCurve));

			bondCurve.linkTo(new FlatForward(repoSettlementDate,
									   bond.yield(bondCleanPrice,
												   bondDayCountConvention,
												   Compounding.Compounded,
												   bondCouponFrequency),
									   bondDayCountConvention,
									   Compounding.Compounded,
									   bondCouponFrequency));

			Position.Type fwdType = Position.Type.Long;
			double dummyStrike = 91.5745;

			RelinkableHandle<YieldTermStructure> repoCurve = new RelinkableHandle<YieldTermStructure>();
			repoCurve.linkTo(new FlatForward(repoSettlementDate,
											   repoRate,
											   repoDayCountConvention,
											   repoCompounding,
											   repoCompoundFreq));


			FixedRateBondForward bondFwd = new FixedRateBondForward(repoSettlementDate,
										 repoDeliveryDate,
										 fwdType,
										 dummyStrike,
										 repoSettlementDays,
										 repoDayCountConvention,
										 bondCalendar,
										 bondBusinessDayConvention,
										 bond,
										 repoCurve,
										 repoCurve);


			Console.WriteLine("Underlying bond clean price: " + bond.cleanPrice());
			Console.WriteLine("Underlying bond dirty price: " + bond.dirtyPrice());
			Console.WriteLine("Underlying bond accrued at settlement: "
				 + bond.accruedAmount(repoSettlementDate));
			Console.WriteLine("Underlying bond accrued at delivery:   "
				 + bond.accruedAmount(repoDeliveryDate));
			Console.WriteLine("Underlying bond spot income: "
				 + bondFwd.spotIncome(repoCurve));
			Console.WriteLine("Underlying bond fwd income:  "
				 + bondFwd.spotIncome(repoCurve)/
					repoCurve.link.discount(repoDeliveryDate));
			Console.WriteLine("Repo strike: " + dummyStrike);
			Console.WriteLine("Repo NPV:    " + bondFwd.NPV());
			Console.WriteLine("Repo clean forward price: "
				 + bondFwd.cleanForwardPrice());
			Console.WriteLine("Repo dirty forward price: "
				 + bondFwd.forwardPrice());
			Console.WriteLine("Repo implied yield: "
				 + bondFwd.impliedYield(bond.dirtyPrice(),
										 dummyStrike,
										 repoSettlementDate,
										 repoCompounding,
										 repoDayCountConvention));
			Console.WriteLine("Market repo rate:   "
				 + repoCurve.link.zeroRate(repoDeliveryDate,
										repoDayCountConvention,
										repoCompounding,
										repoCompoundFreq));

			Console.WriteLine("\nCompare with example given at \n"
				 + "http://www.fincad.com/support/developerFunc/mathref/BFWD.htm");
			Console.WriteLine("Clean forward price = 88.2408");
			Console.WriteLine("\nIn that example, it is unknown what bond calendar they are\n"
				 + "using, as well as settlement Days. For that reason, I have\n"
				 + "made the simplest possible assumptions here: NullCalendar\n"
				 + "and 0 settlement days.\n");


			Console.WriteLine("nRun completed in {0}", DateTime.Now - timer);

      Console.Write("Press any key to continue ...");
      Console.ReadKey();
		}
	}
}
