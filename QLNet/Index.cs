/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008, 2009 , 2010  Andrea Maggiulli (a.maggiulli@gmail.com)
  
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

namespace QLNet {
    // purely virtual base class for indexes
    // this class performs no check that the provided/requested fixings are for dates in the past,
    // i.e. for dates less than or equal to the evaluation date. It is up to the client code to take care of
    // possible inconsistencies due to "seeing in the future"

    // dates passed as arguments must be the actual calendar date of the fixing
    // no settlement days must be used.
	public abstract class Index : IObservable {

		//! Returns the name of the index.
        //! \warning This method is used for output and comparison between indexes.
        // It is <b>not</b> meant to be used for writing switch-on-type code.
        public abstract string name();

        public abstract Calendar fixingCalendar();   //! returns the calendar defining valid fixing dates
        public abstract bool isValidFixingDate(Date fixingDate);   //! returns TRUE if the fixing date is a valid one

        //! returns the fixing at the given date
        /*! the date passed as arguments must be the actual calendar date of the fixing; no settlement days must be used. */
        public virtual double fixing(Date fixingDate) { return fixing(fixingDate, false); }
        public abstract double fixing(Date fixingDate, bool forecastTodaysFixing);

        //! returns the fixing TimeSeries
        public ObservableValue<TimeSeries<double>> timeSeries() { return IndexManager.instance().getHistory(name()); }

        //! clears all stored historical fixings
        public void clearFixings() { IndexManager.instance().clearHistory(name()); }

        // stores the historical fixing at the given date
        public virtual void addFixing(Date d, double v) { addFixing(d, v, false); }
        public virtual void addFixing(Date d, double v, bool forceOverwrite) {
            addFixings(new TimeSeries<double>() { { d, v } }, forceOverwrite);
	    }

        // stores historical fixings at the given dates
        public void addFixings(List<Date> d, List<double> v) { addFixings(d, v, false); }
        public void addFixings(List<Date> d, List<double> v, bool forceOverwrite) {
            if ((d.Count != v.Count) || d.Count == 0)
                throw new ArgumentException("Wrong collection dimensions when creating index fixings");

            TimeSeries<double> t = new TimeSeries<double>();
            for(int i=0; i<d.Count; i++)
                t.Add(d[i], v[i]);
            addFixings(t, forceOverwrite);
        }

        // stores historical fixings from a TimeSeries
        public void addFixings(Dictionary<Date, double> source) { addFixings(source, false); }
        public void addFixings(Dictionary<Date, double> source, bool forceOverwrite) {
            ObservableValue<TimeSeries<double>> target = IndexManager.instance().getHistory(name());
            foreach (Date d in source.Keys) {
               if (isValidFixingDate(d))
                  if (!target.value().ContainsKey(d))
                     target.value().Add(d, source[d]);
                  else
                     if (forceOverwrite)
                        target.value()[d] = source[d];
                     else if (Utils.close(target.value()[d], source[d]))
                        continue;
                     else
                        throw new ArgumentException("Duplicated fixing provided: " + d + ", " + source[d] +
                                                    " while " + target.value()[d] + " value is already present");
               else
                  throw new ArgumentException("Invalid fixing provided: " + d.DayOfWeek + " " + d + ", " + source[d]);
            }

            IndexManager.instance().setHistory(name(), target);
        }


        #region observable interface
        public event Callback notifyObserversEvent;
        public void registerWith(Callback handler) { notifyObserversEvent += handler; }
        public void unregisterWith(Callback handler) { notifyObserversEvent -= handler; }
        protected void notifyObservers() {
            Callback handler = notifyObserversEvent;
            if (handler != null) {
                handler();
            }
        }
        #endregion
    }
}