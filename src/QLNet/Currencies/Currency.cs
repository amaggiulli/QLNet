/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)

 This file is part of QLNet Project https://github.com/amaggiulli/qlnet

 QLNet is free software: you can redistribute it and/or modify it
 under the terms of the QLNet license.  You should have received a
 copy of the license along with this program; if not, license is
 available at <https://github.com/amaggiulli/QLNet/blob/develop/LICENSE>.

 QLNet is a based on QuantLib, a free-software/open-source library
 for financial quantitative analysts and developers - http://quantlib.org/
 The QuantLib license is available online at http://quantlib.org/license.shtml.

 This program is distributed in the hope that it will be useful, but WITHOUT
 ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
 FOR A PARTICULAR PURPOSE.  See the license for more details.
*/

namespace QLNet
{
   //! %Currency specification
   public class Currency
   {
      protected string name_, code_;
      protected int numeric_;
      protected string symbol_, fractionSymbol_;
      protected int fractionsPerUnit_;
      protected Rounding rounding_;
      protected Currency triangulated_;
      protected string formatString_;

      // Inspectors
      public string name { get { return name_; } }            //! currency name, e.g, "U.S. Dollar"
      public string code { get { return code_; } }            //! ISO 4217 three-letter code, e.g, "USD"
      public int numericCode { get { return numeric_; } }     //! ISO 4217 numeric code, e.g, "840"
      public string symbol { get { return symbol_; } }        //! symbol, e.g, "$"
      public string fractionSymbol
      { get { return fractionSymbol_; } }                 //! fraction symbol, e.g, "¢"
      public int fractionsPerUnit
      { get { return fractionsPerUnit_; } }               //! number of fractionary parts in a unit, e.g, 100
      public Rounding rounding
      { get { return rounding_; } }                       //! rounding convention
      public Currency triangulationCurrency
      { get { return triangulated_; } }                   //! currency used for triangulated exchange when required
      // output format
      // The format will be fed three positional parameters, namely, value, code, and symbol, in this order.
      public string format { get { return formatString_; } }


      // default constructor
      // Instances built via this constructor have undefined behavior. Such instances can only act as placeholders
      // and must be reassigned to a valid currency before being used.
      public Currency() { }
      public Currency(string name, string code, int numericCode, string symbol, string fractionSymbol,
                      int fractionsPerUnit, Rounding rounding, string formatString) :
         this(name, code, numericCode, symbol, fractionSymbol, fractionsPerUnit, rounding, formatString,
              new Currency()) { }
      public Currency(string name, string code, int numericCode, string symbol, string fractionSymbol,
                      int fractionsPerUnit, Rounding rounding, string formatString,
                      Currency triangulationCurrency)
      {
         name_ = name;
         code_ = code;
         numeric_ = numericCode;
         symbol_ = symbol;
         fractionSymbol_ = fractionSymbol;
         fractionsPerUnit_ = fractionsPerUnit;
         rounding_ = rounding;
         triangulated_ = triangulationCurrency;
         formatString_ = formatString;
      }


      //! Other information
      //! is this a usable instance?
      public bool empty() { return (name_ == null); }

      public override string ToString() { return code; }

      /*! \relates Currency */
      public static bool operator ==(Currency c1, Currency c2)
      {
         if ((object)c1 == null && (object)c2 == null)
            return true;
         else if ((object)c1 == null || (object)c2 == null)
            return false;
         else
            return c1.name == c2.name;
      }
      public static bool operator !=(Currency c1, Currency c2) { return !(c1 == c2); }
      public static Money operator *(double value, Currency c)
      {
         return new Money(value, c);
      }


      public override bool Equals(object o) { return this == (Currency)o; }
      public override int GetHashCode() { return 0; }
   }
}
