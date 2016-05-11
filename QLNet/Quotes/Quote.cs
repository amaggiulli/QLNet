/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
  
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

namespace QLNet
{
    //! purely virtual base class for market observables
    public class Quote : IObservable
    {
        // recheck this abstract implementations of methods which otherwise should throw "notimplemented"
        // such default implementation is needed for Handles

        //! returns the current value, 0 by default
        public virtual double value() { return 0; }
        //! returns true if the Quote holds a valid value, true by default
        public virtual bool isValid() { return true; }

        // observable interface
        private readonly WeakEventSource eventSource = new WeakEventSource();
        public event Callback notifyObserversEvent
        {
           add { eventSource.Subscribe(value); }
           remove { eventSource.Unsubscribe(value); }
        }

        public void registerWith(Callback handler) { notifyObserversEvent += handler; }
        public void unregisterWith(Callback handler) { notifyObserversEvent -= handler; }
        protected void notifyObservers()
        {
           eventSource.Raise();
        }
    }
}