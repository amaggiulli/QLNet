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
using System.Reflection;

namespace QLNet {
    //! Shared handle to an observable
    /*! All copies of an instance of this class refer to the same observable by means of a relinkable smart pointer. When such
        pointer is relinked to another observable, the change will be propagated to all the copies.
        <tt>registerAsObserver</tt> is not needed since C# does automatic garbage collection */
    public class Handle<T> where T : IObservable, new() {
        protected Link link_;

        public Handle() : this(new T()) { }
        public Handle(T h) : this(h, true) { }
        public Handle(T h, bool registerAsObserver) {
            link_ = new Link(h, registerAsObserver);
        }

        //! dereferencing
        public T currentLink() { return link; }
        // this one is instead of c++ -> and () operators overload
        public static implicit operator T(Handle<T> ImpliedObject) { return ImpliedObject.link; }
        public T link {     
            get {
                if (empty())
                    throw new ApplicationException("empty Handle cannot be dereferenced");
                return link_.currentLink();
            }
        }

        // dereferencing of the observable to the Link
        public void registerWith(Callback handler) { link_.registerWith(handler); }
        public void unregisterWith(Callback handler) { link_.unregisterWith(handler); }


        //! checks if the contained shared pointer points to anything
        public bool empty() { return link_.empty(); }
        
        #region operator overload
        public static bool operator ==(Handle<T> here, Handle<T> there) {
			  if ( System.Object.ReferenceEquals( here, there ) ) return true; 
			  else if ( (object)here == null || (object)there == null ) return false;
			  else return here.Equals(there);
        }
        public static bool operator !=(Handle<T> here, Handle<T> there) {
			  return !( here == there );
        }
        public override bool Equals(object o) {
            return this.link_ == ((Handle<T>)o).link_;
        }
        public override int GetHashCode() { return this.ToString().GetHashCode(); } 
        #endregion

        ////! strict weak ordering
        //bool operator<(Handle<U>& other) {
        //    return link_ < other.link_;


        protected class Link : IObservable, IObserver {
            private T h_;
            private bool isObserver_;

            public Link(T h, bool registerAsObserver) {
                linkTo(h, registerAsObserver);
            }

            public void linkTo(T h, bool registerAsObserver) {
                if (!h.Equals(h_) || (isObserver_ != registerAsObserver)) {

                    if (h_ != null && isObserver_) {
                        h_.unregisterWith(update);
                    }
                    
                    h_ = h;
                    isObserver_ = registerAsObserver;

                    if (h_ != null && isObserver_) {
                        h_.registerWith(update);
                    }
                    
                    // finally, notify observers of this of the change in the underlying object
                    notifyObservers();
                }
            }

            public bool empty() { return h_ == null; }
            public T currentLink() { return h_; }

            public void update() { notifyObservers(); }
            
            // Observable
            public event Callback notifyObserversEvent;
            public void registerWith(Callback handler) { notifyObserversEvent += handler; }
            public void unregisterWith(Callback handler) { notifyObserversEvent -= handler; }
            protected void notifyObservers() {
                Callback handler = notifyObserversEvent;
                if (handler != null) {
                    handler();
                }
            }
        }
    }

    //! Relinkable handle to an observable
    /*! An instance of this class can be relinked so that it points to another observable. The change will be propagated to all
        handles that were created as copies of such instance. */
    public class RelinkableHandle<T> : Handle<T>  where T : IObservable, new() {
        public RelinkableHandle() : base(new T(), true) { }
        public RelinkableHandle(T h) : base(h, true) { }
        public RelinkableHandle(T h, bool registerAsObserver) : base(h, registerAsObserver) { }
        public void linkTo(T h) { linkTo(h, true); }
        public void linkTo(T h, bool registerAsObserver) {
            this.link_.linkTo(h, registerAsObserver);
        }
    }

}
