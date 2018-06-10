/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)

 This file is part of QLNet Project https://github.com/amaggiulli/qlnet

 QLNet is free software: you can redistribute it and/or modify it
 under the terms of the QLNet license.  You should have received a
 copy of the license along with this program; if not, license is
 available at <https://github.com/amaggiulli/QLNet/blob/develop/LICENSE>.

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
   //! rounding methods
   /*! The rounding methods follow the OMG specification available
       at ftp://ftp.omg.org/pub/docs/formal/00-06-29.pdf
       \warning the names of the Floor and Ceiling methods might be misleading. Check the provided reference. */

   /// <summary>
   /// Basic rounding class
   /// </summary>
   public class Rounding
   {
      private int precision_;
      private Type type_;
      private int digit_;

      public enum Type
      {
         /// <summary>
         /// do not round: return the number unmodified
         /// </summary>
         None,
         /// <summary>
         /// the first decimal place past the precision will be
         /// rounded up. This differs from the OMG rule which
         /// rounds up only if the decimal to be rounded is
         /// greater than or equal to the rounding digit
         /// </summary>
         Up,
         /// <summary>
         /// all decimal places past the precision will be
         /// truncated
         /// </summary>
         Down,
         /// <summary>
         /// the first decimal place past the precision
         /// will be rounded up if greater than or equal
         /// to the rounding digit; this corresponds to
         /// the OMG round-up rule.  When the rounding
         /// digit is 5, the result will be the one
         /// closest to the original number, hence the
         /// name.
         /// </summary>
         Closest,
         /// <summary>
         /// positive numbers will be rounded up and negative
         /// numbers will be rounded down using the OMG round up
         /// and round down rules
         /// </summary>
         Floor,
         /// <summary>
         /// positive numbers will be rounded down and negative
         /// numbers will be rounded up using the OMG round up
         /// and round down rules
         /// </summary>
         Ceiling
      }

      /// <summary>
      /// default constructor
      /// Instances built through this constructor don't perform
      /// any rounding.
      /// </summary>
      public Rounding()
      {
         type_ = Type.None;
      }
      public Rounding(int precision, Type type)
         : this(precision, type, 5)
      {
      }
      public Rounding(int precision)
         : this(precision, Type.Closest, 5)
      {
      }
      public Rounding(int precision, Type type, int digit)
      {
         precision_ = precision;
         type_ = type;
         digit_ = digit;
      }

      public int Precision
      {
         get
         {
            return precision_;
         }
      }

      public Type getType
      {
         get
         {
            return type_;
         }
      }

      public int Digit
      {
         get
         {
            return digit_;
         }
      }

      /// <summary>
      /// Up-rounding
      /// </summary>
      /// <param name="value"></param>
      /// <returns></returns>
      public double Round(double value)
      {
         if (type_ == Type.None)
            return value;

         double mult = Math.Pow(10.0, precision_);
         bool neg = (value < 0.0);
         double lvalue = Math.Abs(value) * mult;
         double integral = 0.0;
         double modVal = lvalue - (integral = Math.Floor(lvalue));

         lvalue -= modVal;
         switch (type_)
         {
            case Type.Down:
               break;
            case Type.Up:
               lvalue += 1.0;
               break;
            case Type.Closest:
               if (modVal >= (digit_ / 10.0))
                  lvalue += 1.0;
               break;
            case Type.Floor:
               if (!neg)
               {
                  if (modVal >= (digit_ / 10.0))
                     lvalue += 1.0;
               }
               break;
            case Type.Ceiling:
               if (neg)
               {
                  if (modVal >= (digit_ / 10.0))
                     lvalue += 1.0;
               }
               break;
            default:
               Utils.QL_FAIL("unknown rounding method");
               break;
         }
         return (neg) ? -(lvalue / mult) : lvalue / mult;
      }

   }

   /// <summary>
   /// Up-rounding
   /// </summary>
   public class UpRounding : Rounding
   {
      public UpRounding(int precision) : base(precision, Type.Up, 5) { }
      public UpRounding(int precision, int digit) : base(precision, Type.Up, digit) { }
   }

   /// <summary>
   /// Down-rounding.
   /// </summary>
   public class DownRounding : Rounding
   {
      public DownRounding(int precision) : base(precision, Type.Down, 5) { }
      public DownRounding(int precision, int digit) : base(precision, Type.Down, digit) { }
   }

   /// <summary>
   /// Closest rounding.
   /// </summary>
   public class ClosestRounding : Rounding
   {
      public ClosestRounding(int precision) : base(precision, Type.Closest, 5) { }
      public ClosestRounding(int precision, int digit) : base(precision, Type.Closest, digit) { }
   }

   //!
   /// <summary>
   /// Ceiling truncation.
   /// </summary>
   public class CeilingTruncation : Rounding
   {
      public CeilingTruncation(int precision) : base(precision, Type.Ceiling, 5) { }
      public CeilingTruncation(int precision, int digit) : base(precision, Type.Ceiling, digit) { }
   }

   /// <summary>
   /// Floor truncation.
   /// </summary>
   public class FloorTruncation : Rounding
   {
      public FloorTruncation(int precision) : base(precision, Type.Floor, 5) { }
      public FloorTruncation(int precision, int digit) : base(precision, Type.Floor, digit) { }
   }

}
