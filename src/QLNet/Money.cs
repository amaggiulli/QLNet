/*
 Copyright (C) 2008 Andrea Maggiulli
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)

 This file is part of QLNet Project https://github.com/amaggiulli/qlnet

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

namespace QLNet
{
   /// <summary>
   /// Amount of cash
   /// Money arithmetic is tested with and without currency conversions.
   /// </summary>
   public class Money
   {
      #region Define

      public enum ConversionType
      {
         /// <summary>
         /// do not perform conversions
         /// </summary>
         NoConversion,

         /// <summary>
         /// convert both operands to the base currency before converting
         /// </summary>
         BaseCurrencyConversion,

         /// <summary>
         /// return the result in the currency of the first operand
         /// </summary>
         AutomatedConversion
      }

      #endregion Define

      #region Attributes

      [ThreadStatic]
      private static ConversionType conversionType_;

      [ThreadStatic]
      private static Currency baseCurrency_;

      public static ConversionType conversionType { get { return conversionType_; } set { conversionType_ = value; } }
      public static Currency baseCurrency { get { return baseCurrency_; } set { baseCurrency_ = value; } }

      private double value_;
      private Currency currency_;

      #endregion Attributes

      #region Constructor

      public Money()
      {
         value_ = 0.0;
      }

      public Money(Currency currency, double value)
      {
         value_ = value;
         currency_ = currency;
      }

      public Money(double value, Currency currency) : this(currency, value) { }

      #endregion Constructor

      #region Get/Set

      public Currency currency
      {
         get { return currency_; }
      }

      public double value
      {
         get { return value_; }
      }

      #endregion Get/Set



      #region Methods

      public static void convertTo(ref Money m, Currency target)
      {
         if (m.currency != target)
         {
            ExchangeRate rate = ExchangeRateManager.Instance.lookup(m.currency, target);
            m = rate.exchange(m).rounded();
         }
      }

      public static void convertToBase(ref Money m)
      {
         Utils.QL_REQUIRE(!baseCurrency.empty(), () => "no base currency set");
         convertTo(ref m, baseCurrency);
      }

      public Money rounded()
      {
         return new Money(currency_.rounding.Round(value_), currency_);
      }

      public override String ToString()
      {
         return this.rounded().value + "-" + this.currency.code + "-" + this.currency.symbol;
      }

      #endregion Methods

      #region Operators

      public static Money operator *(Money m, double x)
      {
         return new Money(m.value_ * x, m.currency);
      }

      public static Money operator *(double x, Money m)
      {
         return m * x;
      }

      public static Money operator /(Money m, double x)
      {
         return new Money(m.value_ / x, m.currency);
      }

      public static Money operator +(Money m1, Money m2)
      {
         Money m = new Money(m1.currency, m1.value);

         if (m1.currency_ == m2.currency_)
         {
            m.value_ += m2.value_;
         }
         else if (Money.conversionType == Money.ConversionType.BaseCurrencyConversion)
         {
            Money.convertToBase(ref m);
            Money tmp = m2;
            Money.convertToBase(ref tmp);
            m += tmp;
         }
         else if (Money.conversionType == Money.ConversionType.AutomatedConversion)
         {
            Money tmp = m2;
            Money.convertTo(ref tmp, m.currency_);
            m += tmp;
         }
         else
         {
            Utils.QL_FAIL("currency mismatch and no conversion specified");
         }

         return m;
      }

      public static Money operator -(Money m1, Money m2)
      {
         Money m = new Money(m1.currency, m1.value);

         if (m.currency_ == m2.currency_)
         {
            m.value_ -= m2.value_;
         }
         else if (Money.conversionType == Money.ConversionType.BaseCurrencyConversion)
         {
            convertToBase(ref m);
            Money tmp = m2;
            convertToBase(ref tmp);
            m -= tmp;
         }
         else if (Money.conversionType == Money.ConversionType.AutomatedConversion)
         {
            Money tmp = m2;
            convertTo(ref tmp, m.currency_);
            m -= tmp;
         }
         else
         {
            Utils.QL_FAIL("currency mismatch and no conversion specified");
         }

         return m;
      }

      public static bool operator ==(Money m1, Money m2)
      {
         if ((object)m1 == null && (object)m2 == null)
            return true;
         else if ((object)m1 == null || (object)m2 == null)
            return false;
         else if (m1.currency == m2.currency)
         {
            return m1.value.IsEqual(m2.value);
         }
         else if (Money.conversionType == Money.ConversionType.BaseCurrencyConversion)
         {
            Money tmp1 = m1;
            convertToBase(ref tmp1);
            Money tmp2 = m2;
            convertToBase(ref tmp2);
            return tmp1 == tmp2;
         }
         else if (Money.conversionType == Money.ConversionType.AutomatedConversion)
         {
            Money tmp = m2;
            convertTo(ref tmp, m1.currency);
            return m1 == tmp;
         }
         else
         {
            Utils.QL_FAIL("currency mismatch and no conversion specified");
            return false;
         }
      }

      public static bool operator !=(Money m1, Money m2)
      {
         return !(m1 == m2);
      }

      public override bool Equals(object o) { return (this == (Money)o); }

      public override int GetHashCode() { return 0; }

      #endregion Operators
   }
}
