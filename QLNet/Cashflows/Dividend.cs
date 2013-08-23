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

namespace QLNet
{
   //! Predetermined cash flow
   /*! This cash flow pays a predetermined amount at a given date. */
   public abstract class Dividend : CashFlow
   {
      protected Date date_;
      //! \name Event interface
      public override Date date() { return date_; }

      public Dividend(Date date)
      {
         date_ = date;
      }

      public abstract double amount(double underlying);
   }

   //! Predetermined cash flow
   /*! This cash flow pays a predetermined amount at a given date. */
   public class FixedDividend : Dividend
   {
      protected double amount_;
      public override double amount() { return amount_; }
      public override double amount(double d) { return amount_; }

      public FixedDividend(double amount, Date date)
         : base(date)
      {
         amount_ = amount;
      }
   }

   //! Predetermined cash flow
   /*! This cash flow pays a predetermined amount at a given date. */
   public class FractionalDividend : Dividend
   {
      protected double rate_;
      public double rate() { return rate_; }

      protected double? nominal_;
      public double? nominal() { return nominal_; }

      public FractionalDividend(double rate, Date date)
         : base(date)
      {
         rate_ = rate;
         nominal_ = null;
      }

      public FractionalDividend(double rate, double nominal, Date date)
         : base(date)
      {
         rate_ = rate;
         nominal_ = nominal;
      }

      //! \name Dividend interface
      public override double amount()
      {
         if (nominal_ == null) throw new ApplicationException("no nominal given");
         return rate_ * nominal_.GetValueOrDefault();
      }

      public override double amount(double underlying)
      {
         return rate_ * underlying;
      }
   }

   public static partial class Utils
   {
      //! helper function building a sequence of fixed dividends
      public static List<Dividend> DividendVector(List<Date> dividendDates, List<double> dividends)
      {

         if (dividendDates.Count != dividends.Count)
            throw new ApplicationException("size mismatch between dividend dates and amounts");

         List<Dividend> items = new List<Dividend>(dividendDates.Count);
         for (int i = 0; i < dividendDates.Count; i++)
            items.Add(new FixedDividend(dividends[i], dividendDates[i]));
         return items;
      }
   }
}
