/*
 Copyright (C) 2008 Andrea Maggiulli
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
 
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
using System.Threading;

namespace QLNet
{
   /// <summary>
   /// exchange-rate repository
   /// test lookup of direct, triangulated, and derived exchange rates is tested
   /// </summary>
   public class ExchangeRateManager 
   {
      [ThreadStatic]
      private static ExchangeRateManager instance_ = null;
      public static ExchangeRateManager Instance
      {
          get
          {
              if (instance_ == null)
              {
                  instance_ =
                     new ExchangeRateManager();
              }
              return instance_;
          }
      }

      private ExchangeRateManager() {
          addKnownRates();
      }

      private System.Collections.Generic.Dictionary<int,List<Entry>> data_ = new Dictionary<int,List<Entry>>();

      public struct Entry
      {
         public Entry(ExchangeRate r, Date s, Date e)
         {
            rate = r;
            startDate = s;
            endDate = e;
         }
         public ExchangeRate rate;
         public Date startDate;
         public Date endDate;
      }

      private void addKnownRates()
      {
         // currencies obsoleted by Euro
          add(new ExchangeRate(new EURCurrency(), new ATSCurrency(), 13.7603), new Date(1, Month.January, 1999), Date.maxDate());
          add(new ExchangeRate(new EURCurrency(), new BEFCurrency(), 40.3399), new Date(1, Month.January, 1999), Date.maxDate());
          add(new ExchangeRate(new EURCurrency(), new DEMCurrency(), 1.95583), new Date(1, Month.January, 1999), Date.maxDate());
          add(new ExchangeRate(new EURCurrency(), new ESPCurrency(), 166.386), new Date(1, Month.January, 1999), Date.maxDate());
          add(new ExchangeRate(new EURCurrency(), new FIMCurrency(), 5.94573), new Date(1, Month.January, 1999), Date.maxDate());
          add(new ExchangeRate(new EURCurrency(), new FRFCurrency(), 6.55957), new Date(1, Month.January, 1999), Date.maxDate());
          add(new ExchangeRate(new EURCurrency(), new GRDCurrency(), 340.750), new Date(1, Month.January, 2001), Date.maxDate());
          add(new ExchangeRate(new EURCurrency(), new IEPCurrency(), 0.787564), new Date(1, Month.January, 1999), Date.maxDate());
          add(new ExchangeRate(new EURCurrency(), new ITLCurrency(), 1936.27), new Date(1, Month.January, 1999), Date.maxDate());
          add(new ExchangeRate(new EURCurrency(), new LUFCurrency(), 40.3399), new Date(1, Month.January, 1999), Date.maxDate());
          add(new ExchangeRate(new EURCurrency(), new NLGCurrency(), 2.20371), new Date(1, Month.January, 1999), Date.maxDate());
          add(new ExchangeRate(new EURCurrency(), new PTECurrency(), 200.482), new Date(1, Month.January, 1999), Date.maxDate());
         // other obsoleted currencies
          add(new ExchangeRate(new TRYCurrency(), new TRLCurrency(), 1000000.0), new Date(1, Month.January, 2005), Date.maxDate());
          add(new ExchangeRate(new RONCurrency(), new ROLCurrency(), 10000.0), new Date(1, Month.July, 2005), Date.maxDate());
          add(new ExchangeRate(new PENCurrency(), new PEICurrency(), 1000000.0), new Date(1, Month.July, 1991), Date.maxDate());
          add(new ExchangeRate(new PEICurrency(), new PEHCurrency(), 1000.0), new Date(1, Month.February, 1985), Date.maxDate());

      }

      public void add(ExchangeRate rate)
      {
          add(rate, Date.minDate(), Date.maxDate());
      }


      public void add(ExchangeRate rate, Date startDate)
      {
          add(rate, startDate, Date.maxDate());
      }
      /// <summary>
      /// Add an exchange rate.
      /// </summary>
      /// <param name="rate"></param>
      /// <param name="startDate"></param>
      /// <param name="endDate"></param>
      /// <remarks>
      /// The given rate is valid between the given dates.
      /// If two rates are given between the same currencies
      /// and with overlapping date ranges, the latest one
      /// added takes precedence during lookup.
      /// </remarks> 
      private void add(ExchangeRate rate, Date startDate, Date endDate)
      {
         int k = hash(rate.source, rate.target);
         if (data_.ContainsKey(k))
         {
            data_[k].Insert(0,new Entry(rate, startDate, endDate));
         }
         else
         {
            data_[k] = new List<Entry>() ;
            data_[k].Add(new Entry(rate, startDate, endDate));
         }
      }
      
      private int hash(Currency c1,Currency c2) 
      {
         return (int)(Math.Min(c1.numericCode,c2.numericCode))*1000
              + (int)(Math.Max(c1.numericCode,c2.numericCode));
      }

      private bool hashes(int k, Currency c)
      {
         if ( (c.numericCode == k % 1000) || 
              (c.numericCode == k / 1000) )
            return true;
         return false;
      }

      public ExchangeRate lookup(Currency source, Currency target)
      {
         return this.lookup(source, target, new Date(), ExchangeRate.Type.Derived);
      }

      public ExchangeRate lookup(Currency source, Currency target,Date date)
      {
         return this.lookup(source, target, date, ExchangeRate.Type.Derived);
      }

      /// <summary>
      /// Lookup the exchange rate between two currencies at a given
      /// date.  If the given type is Direct, only direct exchange
      /// rates will be returned if available; if Derived, direct
      /// rates are still preferred but derived rates are allowed.
      /// </summary>
      /// <remarks>
      /// if two or more exchange-rate chains are possible
      /// which allow to specify a requested rate, it is
      /// unspecified which one is returned.
      /// </remarks>
      /// <param name="source"></param>
      /// <param name="target"></param>
      /// <param name="date"></param>
      /// <param name="type"></param>
      /// <returns></returns>
      public ExchangeRate lookup(Currency source, Currency target, Date date, ExchangeRate.Type type)
      {

         if (source == target)
            return new ExchangeRate(source, target, 1.0);

         if (date == new Date())
            date = Settings.evaluationDate();

         if (type == ExchangeRate.Type.Direct)
         {
            return directLookup(source, target, date);
         }
         else if (!source.triangulationCurrency.empty())
         {
            Currency link = source.triangulationCurrency;
            if (link == target)
               return directLookup(source, link, date);
            else
               return ExchangeRate.chain(directLookup(source, link, date), lookup(link, target, date));
         }
         else if (!target.triangulationCurrency.empty())
         {
            Currency link = target.triangulationCurrency;
            if (source == link)
               return directLookup(link, target, date);
            else
               return ExchangeRate.chain(lookup(source, link, date), directLookup(link, target, date));
         }
         else
         {
            return smartLookup(source, target, date);
         }

      }
      
      private ExchangeRate directLookup(Currency source, Currency target, Date date)
      {
         ExchangeRate rate = fetch(source,target,date); 

         if (rate.rate !=  0)
            return rate;
        else
            throw new Exception("no direct conversion available from " + source.code + " to " + target.code + " for " + date);
      }
      private ExchangeRate smartLookup(Currency source, Currency target, Date date)
      {
         return smartLookup(source,target,date, new List<int>() );
      }      

      private ExchangeRate smartLookup(Currency source, Currency target, Date date, List<int> forbidden)
      {
        // direct exchange rates are preferred.
        ExchangeRate direct = fetch(source,target,date);
        if (direct.HasValue)
            return direct;

        // if none is found, turn to smart lookup. The source currency
        // is forbidden to subsequent lookups in order to avoid cycles.
        forbidden.Add(source.numericCode);

        foreach (KeyValuePair<int, List<Entry>> i in data_)
        {
            // we look for exchange-rate data which involve our source
            // currency...
            if (hashes(i.Key , source) && (i.Value.Count != 0))
            {
                // ...whose other currency is not forbidden...
               Entry e = i.Value[0];// front();
                Currency other = source == e.rate.source ? e.rate.target : e.rate.source;
                if (!forbidden.Contains (other.numericCode))
                {
                    // ...and which carries information for the requested date.
                    ExchangeRate head = fetch(source,other,date);
                    if (((Nullable<double>)head.rate).HasValue)
                    {
                        // if we can get to the target from here...
                        try
                        {
                            ExchangeRate tail = smartLookup(other, target, date, forbidden);
                            // ..we're done.
                            return ExchangeRate.chain(head,tail);
                        }
                        catch (Exception)
                        {
                            // otherwise, we just discard this rate.
                            ;
                        }
                    }
                }
            }
        }
        // if the loop completed, we have no way to return the requested rate.
        throw new Exception("no conversion available from " + source.code + " to " + target.code + " for " + date);
    }

      private ExchangeRate fetch(Currency source, Currency target, Date date)
      {
         if (data_.ContainsKey(hash(source, target)))
         {
            List<Entry> rates = data_[hash(source, target)];
            foreach (Entry e in rates)
            {
               if (date >= e.startDate && date <= e.endDate)
                  return e.rate;
            }
         }
         return new ExchangeRate();
      }
      /// <summary>
      /// remove the added exchange rates
      /// </summary>
      public void clear()
      {
         data_.Clear();
         addKnownRates();

      }
   }
}
