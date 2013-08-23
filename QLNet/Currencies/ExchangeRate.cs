/*
 Copyright (C) 2008 Andrea Maggiulli
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
using System.Text;

namespace QLNet {
   /// <summary>
   /// Exchange rate between two currencies
   /// application of direct and derived exchange rate is
   /// tested against calculations.
   /// </summary>
   public class ExchangeRate {
      private Currency source_;
      private Currency target_;
      private Nullable<double> rate_;
      private Type type_;
      private KeyValuePair<ExchangeRate, ExchangeRate> rateChain_;

      /// <summary>
      /// the source currency.
      /// </summary>
      public Currency source
      {
         get { return source_ ; }
      }
       
      /// <summary>
      /// the target currency.
      /// </summary>
      public Currency target
      {
         get { return target_; }
      }

      /// <summary>
      /// the type
      /// </summary>
      /// <returns></returns>
      public ExchangeRate.Type type 
      {
         get { return type_;}
      }

      /// <summary>
      /// the exchange rate (when available)
      /// </summary>
      /// <returns></returns>
      public double rate
      {
         get { return (double)rate_.Value; }
      }

      public bool HasValue
      {
         get {return rate_.HasValue;}
      }

      /// <summary>
      /// given directly by the user 
      /// </summary>
      public enum Type : int 
      {
         /// <summary>
         /// given directly by the user
         /// </summary>
         Direct,
         /// <summary>
         /// Derived from exchange rates between other currencies 
         /// </summary>
         Derived
      }

      public ExchangeRate()
      {
         rate_ = null;
      }

      /// <summary>
      /// the rate r  is given with the convention that a
      /// unit of the source is worth r units of the target.
      /// </summary>
      /// <param name="source"></param>
      /// <param name="target"></param>
      /// <param name="rate"></param>
      public ExchangeRate(Currency source, Currency target, double rate)
      {
         source_ = source;
         target_ = target;
         rate_ = rate;
         type_ = Type.Direct;
      }

      /// <summary>
      /// Utility methods
      /// apply the exchange rate to a cash amount
      /// </summary>
      /// <param name="amount"></param>
      /// <returns></returns>
      public Money exchange(Money amount)
      {
         switch (type_)
         {
            case Type.Direct:
               if (amount.currency == source_)
                  return new Money(amount.value * rate_.Value, target_);
               else if (amount.currency == target_)
                  return new Money(amount.value / rate_.Value, source_);
               else
                  throw new Exception ("exchange rate not applicable");

            case Type.Derived:
               if (amount.currency == rateChain_.Key.source || amount.currency == rateChain_.Key.target)
                   return rateChain_.Value.exchange(rateChain_.Key.exchange(amount));
               else if (amount.currency == rateChain_.Value.source || amount.currency == rateChain_.Value.target)
                   return rateChain_.Key.exchange(rateChain_.Value.exchange(amount));
               else
                  throw new Exception("exchange rate not applicable");
            default:
               throw new Exception("unknown exchange-rate type");
         }
      }

      /// <summary>
      /// chain two exchange rates
      /// </summary>
      /// <param name="r1"></param>
      /// <param name="r2"></param>
      /// <returns></returns>
      public static ExchangeRate chain(ExchangeRate r1, ExchangeRate r2)
        {
            ExchangeRate result = new ExchangeRate();
            result.type_ = Type.Derived;
            result.rateChain_ = new KeyValuePair<ExchangeRate,ExchangeRate>(r1,r2);
            if (r1.source_ == r2.source_) 
            {
               result.source_ = r1.target_;
               result.target_ = r2.target_;
               result.rate_ = r2.rate_ / r1.rate_;
            } 
            else if (r1.source_ == r2.target_) 
            {
               result.source_ = r1.target_;
               result.target_ = r2.source_;
               result.rate_ = 1.0 / (r1.rate_ * r2.rate_);
            } 
            else if (r1.target_ == r2.source_) 
            {
               result.source_ = r1.source_;
               result.target_ = r2.target_;
               result.rate_ = r1.rate_ * r2.rate_;
            } 
            else if (r1.target_ == r2.target_) 
            {
               result.source_ = r1.source_;
               result.target_ = r2.source_;
               result.rate_ = r1.rate_ / r2.rate_;
            } 
            else 
            {
                throw new Exception ("exchange rates not chainable");
            }
            return result;
        }
   }
}
