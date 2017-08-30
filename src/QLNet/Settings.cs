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

namespace QLNet
{
   public interface ISettings : IObservable
   { }

   // we need only one instance of the class
   // we can not derive it from IObservable because the class is static
   public class Settings : ISettings
   {
      private ObservableValue<Date> evaluationDate_;
      private bool includeReferenceDateEvents_;
      private bool enforcesTodaysHistoricFixings_;
      private bool? includeTodaysCashFlows_;

      public Settings()
      {}

      public Date evaluationDate()
      {
         if (evaluationDate_ == null)
         {
            evaluationDate_ = new ObservableValue<Date>();
            evaluationDate_.Assign(Date.Today);
         }
         return evaluationDate_.value();
      }

      public ObservableValue<Date> observableEvaluationDate()
      {
         if (evaluationDate_ == null)
         {
            evaluationDate_ = new ObservableValue<Date>();
            evaluationDate_.Assign(Date.Today);
         }
         return evaluationDate_;
      }

      public void setEvaluationDate(Date d)
      {
         evaluationDate_.Assign(d);
         this.notifyObservers();
      }

      public bool enforcesTodaysHistoricFixings
      {
         get { return enforcesTodaysHistoricFixings_; }
         set { enforcesTodaysHistoricFixings_ = value; }
      }

      public bool includeReferenceDateEvents
      {
         get { return includeReferenceDateEvents_; }
         set { includeReferenceDateEvents_ = value; }
      }

      public bool? includeTodaysCashFlows
      {
         get { return includeTodaysCashFlows_; }
         set { includeTodaysCashFlows_ = value; }
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
         evaluationDate_ = Singleton<Settings>.link.evaluationDate();
         enforcesTodaysHistoricFixings_ = Singleton<Settings>.link.enforcesTodaysHistoricFixings;
         includeReferenceDateEvents_ = Singleton<Settings>.link.includeReferenceDateEvents;
         includeTodaysCashFlows_ = Singleton<Settings>.link.includeTodaysCashFlows;
      }

      public void Dispose()
      {
         if (evaluationDate_ != Singleton<Settings>.link.evaluationDate())
            Singleton<Settings>.link.setEvaluationDate(evaluationDate_);
         Singleton<Settings>.link.enforcesTodaysHistoricFixings = enforcesTodaysHistoricFixings_;
         Singleton<Settings>.link.includeReferenceDateEvents = includeReferenceDateEvents_;
         Singleton<Settings>.link.includeTodaysCashFlows = includeTodaysCashFlows_;
      }
   }
}