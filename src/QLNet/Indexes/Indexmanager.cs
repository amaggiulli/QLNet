/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
 Copyright (C) 2008-2017  Andrea Maggiulli (a.maggiulli@gmail.com)

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

using System;
using System.Collections.Generic;
using history_map = System.Collections.Generic.Dictionary < string, QLNet.ObservableValue < QLNet.TimeSeries < double? >>>;

namespace QLNet
{
   /// <summary>
   /// global repository for past index fixings
   /// <remarks>
   /// Index names are case insensitive
   /// </remarks>
   /// </summary>
   public class IndexManager
   {

      // Index manager can store a callback for missing fixings
      public static Func<InterestRateIndex, DateTime, double> MissingPastFixingCallBack { get; set; }

      private static readonly IndexManager instance_ = new IndexManager();

      public static IndexManager instance()
      {
         return instance_;
      }

      private IndexManager()
      { }

      /// <summary>
      /// returns whether historical fixings were stored for the index
      /// </summary>
      /// <param name="name"></param>
      /// <returns></returns>
      public bool hasHistory(string name)
      {
         return data_.ContainsKey(name.ToUpper()) && data_[name.ToUpper()].value().Count > 0;
      }

      /// <summary>
      /// returns the (possibly empty) history of the index fixings
      /// </summary>
      /// <param name="name"></param>
      /// <returns></returns>
      public TimeSeries < double? > getHistory(string name)
      {
         checkExists(name);
         return data_[name.ToUpper()].value();
      }

      /// <summary>
      /// Returns the history of the index fixings if the index exists
      /// A return value indicates whether the index exists.
      /// </summary>
      /// <param name="name">The index name</param>
      /// <param name="history">The history of the index. Populated with null if the index does not exist</param>
      /// <returns>true if the index exists; otherwise, false.</returns>
      public bool tryGetHistory(string name, out TimeSeries < double? > history)
      {
         if (hasHistory(name))
         {
            history = getHistory(name);
            return true;
         }
         else
         {
            history = null;
            return false;
         }
      }

      /// <summary>
      /// stores the historical fixings of the index
      /// </summary>
      /// <param name="name"></param>
      /// <param name="history"></param>
      public void setHistory(string name, TimeSeries < double? > history)
      {
         checkExists(name);
         data_[name.ToUpper()].Assign(history);
      }

      /// <summary>
      /// observer notifying of changes in the index fixings;
      /// </summary>
      /// <param name="name"></param>
      /// <returns></returns>
      public ObservableValue < TimeSeries < double? >> notifier(string name)
      {
         checkExists(name);
         return data_[name.ToUpper()];
      }

      /// <summary>
      /// returns all names of the indexes for which fixings were stored
      /// </summary>
      /// <returns></returns>
      public List<string> histories()
      {
         List<string> t = new List<string>();
         foreach (string s in data_.Keys)
            t.Add(s);
         return t;
      }

      /// <summary>
      /// clears the historical fixings of the index
      /// </summary>
      /// <param name="name"></param>
      public void clearHistory(string name)
      {
         data_.Remove(name.ToUpper());
      }

      /// <summary>
      /// clears ALL stored fixings
      /// </summary>
      public void clearHistories()
      {
         data_.Clear();
      }

      /// <summary>
      /// checks whether index exists and adds it otherwise; for interal use only
      /// </summary>
      /// <param name="name"></param>
      private void checkExists(string name)
      {
         if (!data_.ContainsKey(name.ToUpper()))
            data_.Add(name.ToUpper(), new ObservableValue < TimeSeries < double? >> ());
      }

      private static history_map data_ = new Dictionary < string, ObservableValue < TimeSeries < double? >>> ();

   }

}
