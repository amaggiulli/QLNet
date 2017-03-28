/*
 Copyright (C) 2008 Andrea Maggiulli
  
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
   //! Bangladesh taka
   /*! The ISO three-letter code is BDT; the numeric code is 50.
       It is divided in 100 paisa.
          \ingroup currencies
   */
   public class BDTCurrency : Currency 
   {
      public BDTCurrency() : base("Bangladesh taka", "BDT", 50,"Bt", "", 100,new Rounding(), "%3% %1$.2f"){}
   }

   //! Chinese yuan
   /*! The ISO three-letter code is CNY; the numeric code is 156.
      It is divided in 100 fen.

      \ingroup currencies
   */
   public class CNYCurrency : Currency 
   {
     public CNYCurrency() :base("Chinese yuan", "CNY", 156, "Y", "", 100, new Rounding(), "%3% %1$.2f"){}
   }

   //! Hong Kong dollar
   /*! The ISO three-letter code is HKD; the numeric code is 344.
      It is divided in 100 cents.

      \ingroup currencies
   */
   public class HKDCurrency : Currency 
   {
      public HKDCurrency() : base( "Hong Kong dollar", "HKD", 344, "HK$", "", 100, new Rounding(), "%3% %1$.2f" ) { }
   }

   //! Indonesian Rupiah
    /*! The ISO three-letter code is IDR; the numeric code is 360.
        It is divided in 100 sen.

        \ingroup currencies
   */
   public class IDRCurrency : Currency 
   {
      public IDRCurrency():base("Indonesian Rupiah", "IDR", 360,"Rp", "", 100,new Rounding(),"%3% %1$.2f") { }
   }

   //! Israeli shekel
   /*! The ISO three-letter code is ILS; the numeric code is 376.
      It is divided in 100 agorot.

      \ingroup currencies
   */
   public class ILSCurrency : Currency 
   {
      public ILSCurrency() : base( "Israeli shekel", "ILS", 376, "NIS", "", 100, new Rounding(), "%1$.2f %3%" ) { }
   }

   //! Indian rupee
   /*! The ISO three-letter code is INR; the numeric code is 356.
      It is divided in 100 paise.

      \ingroup currencies
   */
   public class INRCurrency : Currency 
   {
      public INRCurrency()
         : base( "Indian rupee", "INR", 356, "Rs", "", 100,new Rounding(), "%3% %1$.2f" ) { }
   }

   //! Iraqi dinar
   /*! The ISO three-letter code is IQD; the numeric code is 368.
      It is divided in 1000 fils.

      \ingroup currencies
   */
   public class IQDCurrency : Currency 
   {
      public IQDCurrency()
         : base( "Iraqi dinar", "IQD", 368,"ID", "", 1000,new Rounding(), "%2% %1$.3f" ) { }
   }

   //! Iranian rial
   /*! The ISO three-letter code is IRR; the numeric code is 364.
      It has no subdivisions.

      \ingroup currencies
   */
   public class IRRCurrency : Currency 
   {
      public IRRCurrency() : base( "Iranian rial", "IRR", 364, "Rls", "", 1,new Rounding(), "%3% %1$.2f" ) { }
   }

   /// <summary>
   /// Japanese yen
   /// The ISO three-letter code is JPY; the numeric code is 392.
   /// It is divided into 100 sen.
   /// </summary>
   public class JPYCurrency : Currency
   {
      public JPYCurrency() : base("Japanese yen", "JPY", 392, "\xA5", "", 100, new Rounding(), "%3% %1$.0f") { }
   }

   //! South-Korean won
   /*! The ISO three-letter code is KRW; the numeric code is 410.
      It is divided in 100 chon.

      \ingroup currencies
   */
   public class KRWCurrency : Currency 
   {
      public KRWCurrency() : base( "South-Korean won", "KRW", 410, "W", "", 100,new Rounding(), "%3% %1$.0f" ) { }
   }

   //! Kuwaiti dinar
   /*! The ISO three-letter code is KWD; the numeric code is 414.
      It is divided in 1000 fils.

      \ingroup currencies
   */
   public class KWDCurrency : Currency 
   {
      public KWDCurrency() : base("Kuwaiti dinar", "KWD", 414,"KD", "", 1000,new Rounding(),"%3% %1$.3f"){}
   }

   //! Malaysian Ringgit
    /*! The ISO three-letter code is MYR; the numeric code is 458.
        It is divided in 100 sen.

        \ingroup currencies
   */
   public class MYRCurrency : Currency 
   {
      public MYRCurrency() : base("Malaysian Ringgit","MYR", 458,"RM", "", 100,new Rounding(),"%3% %1$.2f"){}
   }

   //! Nepal rupee
   /*! The ISO three-letter code is NPR; the numeric code is 524.
      It is divided in 100 paise.

      \ingroup currencies
   */
   public class NPRCurrency : Currency 
   {
      public NPRCurrency() : base("Nepal rupee", "NPR", 524, "NRs", "", 100, new Rounding(),"%3% %1$.2f"){}
   }

   //! Pakistani rupee
   /*! The ISO three-letter code is PKR; the numeric code is 586.
      It is divided in 100 paisa.

      \ingroup currencies
   */
   public class PKRCurrency : Currency 
   {
      public PKRCurrency() : base("Pakistani rupee", "PKR", 586, "Rs", "", 100,new Rounding(),"%3% %1$.2f"){}
   }

   //! Saudi riyal
   /*! The ISO three-letter code is SAR; the numeric code is 682.
      It is divided in 100 halalat.

      \ingroup currencies
   */
   public class SARCurrency : Currency 
   {
      public SARCurrency() : base("Saudi riyal", "SAR", 682, "SRls", "", 100,new Rounding(),"%3% %1$.2f"){}
   }

   //! %Singapore dollar
   /*! The ISO three-letter code is SGD; the numeric code is 702.
      It is divided in 100 cents.

      \ingroup currencies
   */
   public class SGDCurrency : Currency 
   {
      public SGDCurrency() : base("Singapore dollar", "SGD", 702, "S$", "", 100, new Rounding(),"%3% %1$.2f"){}
   }

   //! Thai baht
   /*! The ISO three-letter code is THB; the numeric code is 764.
      It is divided in 100 stang.

      \ingroup currencies
   */
   public class THBCurrency : Currency 
   {
      public THBCurrency() : base("Thai baht", "THB", 764, "Bht", "", 100,new Rounding(), "%1$.2f %3%"){}
   }

   //! %Taiwan dollar
   /*! The ISO three-letter code is TWD; the numeric code is 901.
      It is divided in 100 cents.

      \ingroup currencies
   */
   public class TWDCurrency : Currency 
   {
      public TWDCurrency() : base( "Taiwan dollar", "TWD", 901, "NT$", "", 100, new Rounding(), "%3% %1$.2f" ) { }
   }

   //! Vietnamese Dong
   /*! The ISO three-letter code is VND; the numeric code is 704.
       It was divided in 100 xu.

       \ingroup currencies
   */
   public class VNDCurrency : Currency 
   {
      public VNDCurrency() : base("Vietnamese Dong", "VND", 704,"", "", 100,new Rounding(),"%1$.0f %3%") {}
   }
}
