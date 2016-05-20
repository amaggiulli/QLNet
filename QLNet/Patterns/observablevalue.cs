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

namespace QLNet {
    //! %observable and assignable proxy to concrete value
    /*! Observers can be registered with instances of this class so
        that they are notified when a different value is assigned to
        such instances. Client code can copy the contained value or
        pass it to functions via implicit conversion.
        \note it is not possible to call non-const method on the
              returned value. This is by design, as this possibility
              would necessarily bypass the notification code; client
              code should modify the value via re-assignment instead.
    */
    public class ObservableValue<T> : IObservable where T : new() {
        private T value_;
        
        public ObservableValue() {
            value_ = new T();
        }

        public ObservableValue(T t) {
            value_ = t;
        }

        public ObservableValue(ObservableValue<T> t) {
            value_ = t.value_;
        }


        //! \name controlled assignment
        public ObservableValue<T> Assign(T t) {
            value_ = t;
            notifyObservers();
            return this;
        }

        public ObservableValue<T> Assign(ObservableValue<T> t) {
            value_ = t.value_;
            notifyObservers();
            return this;
        }

        //! explicit inspector
        public T value() { return value_; }


        // Subjects, i.e. observables, should define interface internally like follows.
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
