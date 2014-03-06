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
using System.Linq;
using System.Text;

namespace QLNet {
    // Base helper class for bootstrapping
    /* This class provides an abstraction for the instruments used to bootstrap a term structure.
       It is advised that a bootstrap helper for an instrument contains an instance of the actual instrument 
     * class to ensure consistancy between the algorithms used during bootstrapping
       and later instrument pricing. This is not yet fully enforced in the available rate helpers. */
    public class BootstrapHelper<TS> : IObservable, IObserver {
        protected Handle<Quote> quote_;
        protected TS termStructure_;
        protected Date earliestDate_, latestDate_;

        public BootstrapHelper() { } // required for generics

        public BootstrapHelper(Handle<Quote> quote) {
            quote_ = quote;
            quote_.registerWith(update);
        }
        public BootstrapHelper(double quote) { 
            quote_ = new Handle<Quote>(new SimpleQuote(quote)); 
        }


        //! BootstrapHelper interface
        public Handle<Quote> quote() { return quote_; }
        public double quoteError() { return quote_.link.value() - impliedQuote(); }
        public double quoteValue() { return quote_.link.value(); }
        public bool quoteIsValid() { return quote_.link.isValid(); }
        public virtual double impliedQuote() { throw new NotSupportedException(); }


        //! sets the term structure to be used for pricing
        /*! \warning Being a pointer and not a shared_ptr, the term
                             structure is not guaranteed to remain allocated
                             for the whole life of the rate helper. It is
                             responsibility of the programmer to ensure that
                             the pointer remains valid. It is advised that
                             this method is called only inside the term
                             structure being bootstrapped, setting the pointer
                             to <b>this</b>, i.e., the term structure itself.
        */
        public virtual void setTermStructure(TS ts) {
            if (ts == null) throw new ArgumentException("null term structure given");
            termStructure_ = ts;
        }

        // earliest relevant date
        // The earliest date at which discounts are needed by the helper in order to provide a quote.
        public virtual Date earliestDate() { return earliestDate_; }

        // latest relevant date
        /* The latest date at which discounts are needed by the helper in order to provide a quote.
         * It does not necessarily equal the maturity of the underlying instrument. */
        public virtual Date latestDate() { return latestDate_; }


        #region observer interface
        public event Callback notifyObserversEvent;
        public void registerWith(Callback handler) { notifyObserversEvent += handler; }
        public void unregisterWith(Callback handler) { notifyObserversEvent -= handler; }
        protected void notifyObservers() {
            Callback handler = notifyObserversEvent;
            if (handler != null) {
                handler();
            }
        }

        public virtual void update() { notifyObservers(); }
        #endregion
    }

    public class RateHelper : BootstrapHelper<YieldTermStructure>
    {
        public RateHelper() : base() { } // required for generics
        public RateHelper(Handle<Quote> quote) : base(quote) {}
        public RateHelper(double quote) : base (quote) {}
    }
}
