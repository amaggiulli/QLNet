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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QLNet {
    //! Intermediate class for put/call payoffs
    public class TypePayoff : Payoff {
        protected Option.Type type_;
        public Option.Type optionType() { return type_; }

        public TypePayoff(Option.Type type) {
            type_ = type;
        }
        
        //! \name Payoff interface
        public override string description() { return name() + " " + optionType(); }
    }

    //! %Payoff based on a floating strike
    public class FloatingTypePayoff : TypePayoff {
        public FloatingTypePayoff(Option.Type type) : base(type) {}

        //! \name Payoff interface
        public override string name() { return "FloatingType";}

        public override double value(double k) { throw new NotSupportedException("floating payoff not handled"); }
    }

    //! Intermediate class for payoffs based on a fixed strike
    public class StrikedTypePayoff : TypePayoff {
        protected double strike_;

        public StrikedTypePayoff(Option.Type type, double strike) : base(type) {
            strike_ = strike;
        }
        
        //! \name Payoff interface
        public override string description() {
            return base.description() + ", " + strike() + " strike";
        }

        public double strike() { return strike_; }
    }

    //! Plain-vanilla payoff
    public class PlainVanillaPayoff : StrikedTypePayoff {
        public PlainVanillaPayoff(Option.Type type, double strike) : base(type, strike) {}

        //! \name Payoff interface
        public override string name() { return "Vanilla";}
        public override double value(double price) {
            switch (type_) {
                case Option.Type.Call:
                    return Math.Max(price-strike_,0.0);
                case Option.Type.Put:
                    return Math.Max(strike_ - price, 0.0);
                default:
                    throw new ArgumentException("unknown/illegal option type");
            }
        }
    }

    //! %Payoff with strike expressed as percentage
    public class PercentageStrikePayoff : StrikedTypePayoff {
        public PercentageStrikePayoff(Option.Type type, double moneyness) : base(type, moneyness) {}

        //! \name Payoff interface
        public override string name() { return "PercentageStrike";}
        public override double value(double price) {
            switch (type_) {
                case Option.Type.Call:
                    return price * Math.Max(1.0 - strike_, 0.0);
                case Option.Type.Put:
                    return price * Math.Max(strike_ - 1.0, 0.0);
                default:
                    throw new ArgumentException("unknown/illegal option type");
            }
        }
    }

    /*! Definitions of Binary path-independent payoffs used below,
        can be found in M. Rubinstein, E. Reiner:"Unscrambling The Binary Code", Risk, Vol.4 no.9,1991.
        (see: http://www.in-the-money.com/artandpap/Binary%20Options.doc)
    */
    //! Binary asset-or-nothing payoff
    public class AssetOrNothingPayoff : StrikedTypePayoff {
        public AssetOrNothingPayoff(Option.Type type, double strike) : base(type, strike) {}

        //! \name Payoff interface
        public override string name() { return "AssetOrNothing";}
        public override double value(double price) {
            switch (type_) {
                case Option.Type.Call:
                    return (price-strike_ > 0.0 ? price : 0.0);
                case Option.Type.Put:
                    return (strike_-price > 0.0 ? price : 0.0);
                default:
                    throw new ArgumentException("unknown/illegal option type");
            }
        }
    }

    //! Binary cash-or-nothing payoff
    public class CashOrNothingPayoff : StrikedTypePayoff {
        protected double cashPayoff_;
        public double cashPayoff() { return cashPayoff_;}

        public CashOrNothingPayoff(Option.Type type, double strike, double cashPayoff) : base(type, strike) {
            cashPayoff_ = cashPayoff;
        }
        //! \name Payoff interface
        public override string name() { return "CashOrNothing";}
        public override string description() {
            return base.description() + ", " + cashPayoff() + " cash payoff";
        }
        public override double value(double price) {
            switch (type_) {
                case Option.Type.Call:
                    return (price-strike_ > 0.0 ? cashPayoff_ : 0.0);
                case Option.Type.Put:
                    return (strike_-price > 0.0 ? cashPayoff_ : 0.0);
                default:
                    throw new ArgumentException("unknown/illegal option type");
            }
        }
    }

    //! Binary gap payoff
    /*! This payoff is equivalent to being a) long a PlainVanillaPayoff at
        the first strike (same Call/Put type) and b) short a
        CashOrNothingPayoff at the first strike (same Call/Put type) with
        cash payoff equal to the difference between the second and the first
        strike.
        \warning this payoff can be negative depending on the strikes
    */
    public class GapPayoff : StrikedTypePayoff {
        protected double secondStrike_;    
        public double secondStrike() { return secondStrike_;}
        
        public GapPayoff(Option.Type type, double strike, double secondStrike) // a.k.a. payoff strike
            : base(type, strike) {
            secondStrike_ = secondStrike;
        }

        //! \name Payoff interface
        public override string name() { return "Gap";}
        public override string description() {
            return base.description() + ", " + secondStrike() + " strike payoff";
        }
        public override double value(double price) {
            switch (type_) {
                case Option.Type.Call:
                    return (price-strike_ >= 0.0 ? price-secondStrike_ : 0.0);
                case Option.Type.Put:
                    return (strike_-price >= 0.0 ? secondStrike_-price : 0.0);
                default:
                    throw new ArgumentException("unknown/illegal option type");
            }
        }
    }

    //! Binary supershare and superfund payoffs

    //! Binary superfund payoff
    /*! Superfund sometimes also called "supershare", which can lead to ambiguity; within QuantLib
        the terms supershare and superfund are used consistently according to the definitions in
        Bloomberg OVX function's help pages.
    */
    /*! This payoff is equivalent to being (1/lowerstrike) a) long (short) an AssetOrNothing
        Call (Put) at the lower strike and b) short (long) an AssetOrNothing
        Call (Put) at the higher strike
    */
    public class SuperFundPayoff : StrikedTypePayoff {
        protected double secondStrike_;
        public double secondStrike() { return secondStrike_;}

        public SuperFundPayoff(double strike, double secondStrike) : base(Option.Type.Call, strike) {
            secondStrike_ = secondStrike;

            if (!(strike>0.0))
                throw new ApplicationException("strike (" +  strike + ") must be positive");
            if (!(secondStrike>strike))
                throw new ApplicationException("second strike (" +  secondStrike + 
                    ") must be higher than first strike (" + strike + ")");
        }

        //! \name Payoff interface
        public override string name() { return "SuperFund";}
        public override double value(double price) {
            return (price >= strike_ && price < secondStrike_) ? price / strike_ : 0.0;
        }
    }

    //! Binary supershare payoff
    public class SuperSharePayoff : StrikedTypePayoff {
        protected double secondStrike_;
        public double secondStrike() { return secondStrike_; }

        protected double cashPayoff_;
        public double cashPayoff() { return cashPayoff_; }

        public SuperSharePayoff(double strike, double secondStrike, double cashPayoff)
            : base(Option.Type.Call, strike) {
            secondStrike_ = secondStrike;
            cashPayoff_ = cashPayoff;

            if (!(secondStrike>strike))
                throw new ApplicationException("second strike (" +  secondStrike +
                    ") must be higher than first strike (" + strike + ")");
        }

        //! \name Payoff interface
        public override string name() { return "SuperShare";}
        public override string description() {
            return base.description() + ", " + secondStrike() + " second strike" + ", " + cashPayoff() + " amount";;
        }
        public override double value(double price) {
            return (price>=strike_ && price<secondStrike_) ? cashPayoff_ : 0.0;
        }
    }
}
