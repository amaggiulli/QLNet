//  Copyright (C) 2008-2018 Andrea Maggiulli (a.maggiulli@gmail.com)
//
//  This file is part of QLNet Project https://github.com/amaggiulli/qlnet
//  QLNet is free software: you can redistribute it and/or modify it
//  under the terms of the QLNet license.  You should have received a
//  copy of the license along with this program; if not, license is
//  available at <https://github.com/amaggiulli/QLNet/blob/develop/LICENSE>.
//
//  QLNet is a based on QuantLib, a free-software/open-source library
//  for financial quantitative analysts and developers - http://quantlib.org/
//  The QuantLib license is available online at http://quantlib.org/license.shtml.
//
//  This program is distributed in the hope that it will be useful, but WITHOUT
//  ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
//  FOR A PARTICULAR PURPOSE.  See the license for more details.

using System.Collections.Generic;

namespace QLNet
{
   public abstract class CatSimulation
   {
      protected CatSimulation(Date start, Date end)
      {
         start_ = start;
         end_ = end;
      }

      public abstract bool nextPath(List<KeyValuePair<Date, double> > path) ;

      protected Date start_;
      protected Date end_;
   }

   public abstract class CatRisk
   {
      public abstract CatSimulation newSimulation(Date start, Date end);
   }

   public class EventSetSimulation : CatSimulation
   {
      public EventSetSimulation(List<KeyValuePair<Date, double> > events, Date eventsStart, Date eventsEnd, Date start, Date end)
         : base(start, end)
      {
         events_ = events;
         eventsStart_ = eventsStart;
         eventsEnd_ = eventsEnd;
         i_ = 0;

         years_ = end_.year() - start_.year();
         if (eventsStart_.month() < start_.month() ||
             (eventsStart_.month() == start_.month() && eventsStart_.Day <= start_.Day))
         {
            periodStart_ = new Date(start_.Day, start_.Month, eventsStart_.Year);
         }
         else
         {
            periodStart_ = new Date(start_.Day, start_.month(), eventsStart_.year() + 1);
         }
         periodEnd_ = new Date(end_.Day, end_.Month, periodStart_.Year + years_);
         while (i_ < events_.Count && (events_)[i_].Key < periodStart_)
            ++i_; //i points to the first element after the start of the relevant period.

      }

      public override bool nextPath(List<KeyValuePair<Date, double> > path)
      {
         path.Clear();
         if (periodEnd_ > eventsEnd_) //Ran out of event data
            return false;

         while (i_ < events_.Count && (events_)[i_].Key < periodStart_)
         {
            ++i_; //skip the elements between the previous period and this period
         }
         while (i_ < events_.Count  && (events_)[i_].Key <= periodEnd_)
         {
            KeyValuePair<Date, double> e = new KeyValuePair<Date, double>
            (events_[i_].Key + new Period((start_.year() - periodStart_.year()), TimeUnit.Years), events_[i_].Value);
            path.Add(e);
            ++i_; //i points to the first element after the start of the relevant period.
         }
         if (start_ + new Period(years_, TimeUnit.Years) < end_)
         {
            periodStart_ += new Period(years_ + 1, TimeUnit.Years);
            periodEnd_ += new Period(years_ + 1, TimeUnit.Years);
         }
         else
         {
            periodStart_ += new Period(years_, TimeUnit.Years);
            periodEnd_ += new Period(years_, TimeUnit.Years);
         }
         return true;
      }

      private List<KeyValuePair<Date, double>>  events_;
      private Date eventsStart_;
      private Date eventsEnd_;

      private int years_;
      private Date periodStart_;
      private Date periodEnd_;
      private int i_;
   }

   public class EventSet : CatRisk
   {
      public EventSet(List<KeyValuePair<Date, double> >  events, Date eventsStart, Date eventsEnd)
      {
         events_ = events;
         eventsStart_ = eventsStart;
         eventsEnd_ = eventsEnd;
      }

      public override CatSimulation newSimulation(Date start, Date end)
      {
         return new EventSetSimulation(events_, eventsStart_, eventsEnd_, start, end);
      }

      private List<KeyValuePair<Date, double> > events_;
      private Date eventsStart_;
      private Date eventsEnd_;
   }

}
