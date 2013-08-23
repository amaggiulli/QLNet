/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
 * 
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
using System.Threading;

namespace QLNet {
    //! global repository for past index fixings
    public class IndexManager {
        private static Dictionary<string, ObservableValue<TimeSeries<double>>> data_ =
            new Dictionary<string, ObservableValue<TimeSeries<double>>>();

        // Index manager can store a callback for missing fixings
        public static Func<InterestRateIndex, DateTime, double> MissingPastFixingCallBack { get; set; }

        private static readonly IndexManager instance_ = new IndexManager();
        public static IndexManager instance() { return instance_; }
        private IndexManager() { }

        //! returns whether historical fixings were stored for the index
        public bool hasHistory(string name) {
            return data_.ContainsKey(name) && data_[name].value().Count > 0;
        }

        //! returns the (possibly empty) history of the index fixings
        public ObservableValue<TimeSeries<double>> getHistory(string name) {
            checkExists(name);
            return data_[name];
        }

        //! stores the historical fixings of the index
        public void setHistory(string name, ObservableValue<TimeSeries<double>> history) {
            checkExists(name);
            data_[name].Assign(history);
        }

        //! observer notifying of changes in the index fixings; in .NET it has the same logic as getHistory
        public ObservableValue<TimeSeries<double>> notifier(string name) { return getHistory(name); }

        //! returns all names of the indexes for which fixings were stored
        public List<string> histories() {
            List<string> t = new List<string>();
            foreach (string s in data_.Keys)
                t.Add(s);
            return t;
        }

        //! clears the historical fixings of the index
        public void clearHistory(string name) {
           data_.Remove(name.ToUpper());
        }

        //! clears all stored fixings
        public void clearHistories() {
           data_.Clear();
        }

        // checks whether index exists and adds it otherwise; for interal use only
        private void checkExists(string name) {
            if (!data_.ContainsKey(name))
                data_.Add(name, new ObservableValue<TimeSeries<double>>());
        }
    }
}