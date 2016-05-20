/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
 Copyright (C) 2008-2013 Andrea Maggiulli (a.maggiulli@gmail.com)
 
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

namespace QLNet {
    // we need only one instance of the class
    // we can not derive it from IObservable because the class is static
    public static class Settings {

        [ThreadStatic]
        private static Date evaluationDate_;

        [ThreadStatic]
        private static bool includeReferenceDateEvents_;

        [ThreadStatic]
        private static bool enforcesTodaysHistoricFixings_;

        [ThreadStatic]
        private static bool? includeTodaysCashFlows_;

        public static Date evaluationDate() 
        {
            if (evaluationDate_ == null)
                evaluationDate_ = Date.Today;
            return evaluationDate_; 
        }


        public static void setEvaluationDate(Date d) {
            evaluationDate_ = d;
            notifyObservers();
        }

        public static bool enforcesTodaysHistoricFixings
        {
           get { return enforcesTodaysHistoricFixings_; }
           set { enforcesTodaysHistoricFixings_ = value; }
        }

        public static bool includeReferenceDateEvents {
            get { return includeReferenceDateEvents_; }
            set { includeReferenceDateEvents_ = value; }
        }

        public static bool? includeTodaysCashFlows
        {
           get { return includeTodaysCashFlows_; }
            set { includeTodaysCashFlows_ = value; }
        }

        ////////////////////////////////////////////////////
        // Observable interface
        private static readonly WeakEventSource eventSource = new WeakEventSource();
        public static event Callback notifyObserversEvent
        {
           add { eventSource.Subscribe(value); }
           remove { eventSource.Unsubscribe(value); }
        }

        public static void registerWith(Callback handler) { notifyObserversEvent += handler; }
        public static void unregisterWith(Callback handler) { notifyObserversEvent -= handler; }
        private static void notifyObservers()
        {
           eventSource.Raise();
        }
    }

   // helper class to temporarily and safely change the settings
   public class SavedSettings : IDisposable
   {
      private Date evaluationDate_;
      private bool enforcesTodaysHistoricFixings_;
      private bool includeReferenceDateEvents_;
      private bool? includeTodaysCashFlows_;

      public SavedSettings()
      {
         evaluationDate_ = Settings.evaluationDate();
         enforcesTodaysHistoricFixings_ = Settings.enforcesTodaysHistoricFixings;
         includeReferenceDateEvents_ = Settings.includeReferenceDateEvents;
         includeTodaysCashFlows_ = Settings.includeTodaysCashFlows;
      }

      public void Dispose()
      {
         if (evaluationDate_ != Settings.evaluationDate())
            Settings.setEvaluationDate(evaluationDate_);
         Settings.enforcesTodaysHistoricFixings = enforcesTodaysHistoricFixings_;
         Settings.includeReferenceDateEvents = includeReferenceDateEvents_;
         Settings.includeTodaysCashFlows = includeTodaysCashFlows_;
      }
   }
}