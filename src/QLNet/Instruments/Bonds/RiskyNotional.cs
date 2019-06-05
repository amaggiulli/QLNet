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
using System;
using System.Collections.Generic;
using System.Linq;

namespace QLNet
{
   public abstract class EventPaymentOffset
   {
      public abstract Date paymentDate(Date eventDate) ;
   }

   public class NoOffset : EventPaymentOffset
   {
      public override Date paymentDate(Date eventDate) { return eventDate; }
   }

   public class NotionalPath
   {
      public NotionalPath()
      {
         double previous = 1.0;//full notional at the beginning
         notionalRate_ = new List<KeyValuePair<Date, double>> {new KeyValuePair<Date, double>(new Date(), previous)};
      }
      public double notionalRate(Date date)  //The fraction of the original notional left on a given date
      {
         int i = 0;
         for (; i < notionalRate_.Count && notionalRate_[i].Key <= date; ++i) //TODO do we take notional after reductions or before?
         {}
         return notionalRate_[i - 1].Value;

      }

      public void reset()
      {
         notionalRate_ = new InitializedList<KeyValuePair<Date, double>>(1, new KeyValuePair<Date, double>(new Date(), 1));
      }

      public void addReduction(Date date, double newRate)
      {
         notionalRate_.Add(new KeyValuePair<Date, double>(date, newRate));
      }

      public double loss()
      {
         return 1.0 - notionalRate_.Last().Value;
      }

      private List<KeyValuePair<Date, double> > notionalRate_;
   }

   public abstract class NotionalRisk
   {
      protected NotionalRisk(EventPaymentOffset paymentOffset)
      {
         paymentOffset_ = paymentOffset;
      }

      public abstract void updatePath(List<KeyValuePair<Date, double> >  events, NotionalPath path);

      protected EventPaymentOffset paymentOffset_;
   }

   public class DigitalNotionalRisk : NotionalRisk
   {
      public DigitalNotionalRisk(EventPaymentOffset paymentOffset, double threshold)
         : base(paymentOffset)
      {
         threshold_ = threshold;
      }

      public override void updatePath(List<KeyValuePair<Date, double> >  events,
                                      NotionalPath path)
      {
         path.reset();
         for (int i = 0; i < events.Count; ++i)
         {
            if (events[i].Value >= threshold_)
               path.addReduction(paymentOffset_.paymentDate(events[i].Key), 0.0);
         }
      }

      protected double threshold_;
   }

   public class ProportionalNotionalRisk : NotionalRisk
   {
      public ProportionalNotionalRisk(EventPaymentOffset paymentOffset, double attachement, double exhaustion)
         : base(paymentOffset)
      {
         attachement_ = attachement;
         exhaustion_ = exhaustion;

         Utils.QL_REQUIRE(attachement<exhaustion, () => "exhaustion level needs to be greater than attachement");
      }

      public override void updatePath(List<KeyValuePair<Date, double> >  events, NotionalPath path)
      {
         path.reset();
         double losses = 0;
         double previousNotional = 1;
         for (int i = 0; i < events.Count; ++i)
         {
            losses += events[i].Value;
            if (losses > attachement_ && previousNotional > 0)
            {
               previousNotional = Math.Max(0.0, (exhaustion_ - losses) / (exhaustion_ - attachement_));
               path.addReduction(paymentOffset_.paymentDate(events[i].Key), previousNotional);
            }
         }
      }

      protected double attachement_;
      protected double exhaustion_;
   }

}
