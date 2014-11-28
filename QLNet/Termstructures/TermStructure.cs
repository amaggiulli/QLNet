/*
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
using QLNet;

namespace QLNet
{
   //! Basic term-structure functionality
   public class TermStructure : Extrapolator, IObserver, IObservable
   {
        
      #region Constructors

      // There are three ways in which a term structure can keep
      // track of its reference date.  The first is that such date
      // is fixed; the second is that it is determined by advancing
      // the current date of a given number of business days; and
      // the third is that it is based on the reference date of
      // some other structure.
      // 
      // In the first case, the constructor taking a date is to be
      // used; the default implementation of referenceDate() will
      // then return such date. In the second case, the constructor
      // taking a number of days and a calendar is to be used;
      // referenceDate() will return a date calculated based on the
      // current evaluation date, and the term structure and its
      // observers will be notified when the evaluation date
      // changes. In the last case, the referenceDate() method must
      // be overridden in derived classes so that it fetches and
      // return the appropriate date.
       

      //! default constructor
      /*! \warning term structures initialized by means of this
                   constructor must manage their own reference date
                   by overriding the referenceDate() method.
        */
        
      public TermStructure(DayCounter dc = null)
      {
         moving_ = false;
         updated_ = true;
         settlementDays_ = null;
         dayCounter_ = dc;
      }
        
      //! initialize with a fixed reference date
      public TermStructure(Date referenceDate,Calendar calendar = null,DayCounter dc = null)
      {
         moving_ = false;
         updated_ = true;
         calendar_ = calendar;
         referenceDate_ = referenceDate;
         settlementDays_= null;
         dayCounter_ = dc;
      }
        
      //! calculate the reference date based on the global evaluation date
      public TermStructure(int settlementDays, Calendar cal, DayCounter dc = null)
      {
         moving_ = true;
         updated_ = false;
         calendar_ = cal;
         settlementDays_ = settlementDays;
         dayCounter_ = dc;

         Settings.registerWith(update);
      }
      

      #endregion

      #region Dates and Time

      //! the day counter used for date/time conversion
      public virtual DayCounter dayCounter() {return dayCounter_;}
      //! date/time conversion
      public double timeFromReference( Date date) { return dayCounter().yearFraction(referenceDate(), date);}
      //! the latest date for which the curve can return values
      public virtual Date maxDate() { throw new NotSupportedException(); }
      //! the latest time for which the curve can return values
      public virtual double maxTime() {return timeFromReference(maxDate());}
      //! the date at which discount = 1.0 and/or variance = 0.0
      public virtual Date referenceDate()
      {
         if (!updated_) 
         {
            Date today = Settings.evaluationDate();
            referenceDate_ = calendar().advance(today, settlementDays(), TimeUnit.Days);
            updated_ = true;
         }
         return referenceDate_;
      }
      //! the calendar used for reference and/or option date calculation
      public virtual Calendar calendar() {return calendar_;}
      //! the settlementDays used for reference date calculation
      public virtual int settlementDays() 
      {
         Utils.QL_REQUIRE( settlementDays_ != null, () => "settlement days not provided for this instance" );
         return settlementDays_.Value;
      }
        
      #endregion

      #region observable & observer interface

      // observer interface
      public override void update()
      {
         if (moving_)
               updated_ = false;

         // recheck. this is in order to notify observers in the base method of LazyObject
         calculated_ = true;
         base.update();
         // otherwise the following code would be required
         //if (notifyObservers != null)
         //    notifyObservers();
         // the grand reason is that multiple inheritance is not allowed in c# and we need to notify observers in such way
      }
      #endregion

      //! date-range check
      protected virtual void checkRange(Date d, bool extrapolate)
      {
         Utils.QL_REQUIRE( d >= referenceDate(), () =>
                   "date (" + d + ") before reference date (" +
                   referenceDate() + ")");
         Utils.QL_REQUIRE( extrapolate || allowsExtrapolation() || d <= maxDate(), () =>
                    "date (" + d + ") is past max curve date ("
                             + maxDate() + ")");
      }

      //! time-range check
      protected void checkRange(double t, bool extrapolate)
      {
         Utils.QL_REQUIRE( t >= 0.0, () =>
                  "negative time (" + t + ") given");
         Utils.QL_REQUIRE(extrapolate || allowsExtrapolation()
                    || t <= maxTime() || Utils.close_enough( t, maxTime() ), () =>
                    "time (" + t + ") is past max curve time ("
                             + maxTime() + ")");
      }

      protected  bool moving_;
      protected  bool updated_;

       
      private  Calendar calendar_;
      private  Date referenceDate_;
      private  int? settlementDays_;
      private  DayCounter dayCounter_;
   }

}