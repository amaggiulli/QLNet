/*
 Copyright (C) 2008 Andrea Maggiulli
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)

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

namespace QLNet
{
   /// <summary>
   /// Argentinian peso.
   /// </summary>
   /// <remarks>
   /// The ISO three-letter code is ARS; the numeric code is 32.
   /// It is divided into 100 centavos.
   /// </remarks>
   public class ARSCurrency : Currency
   {
      public ARSCurrency() : base("Argentinian peso", "ARS", 32, "", "", 100, new Rounding(), "%2% %1$.2f") { }
   }

   /// <summary>
   /// Brazilian real.
   /// </summary>
   /// <remarks>
   /// The ISO three-letter code is BRL; the numeric code is 986.
   /// It is divided into 100 centavos.
   /// </remarks>
   public class BRLCurrency : Currency
   {
      public BRLCurrency() : base("Brazilian real", "BRL", 986, "R$", "", 100, new Rounding(), "%3% %1$.2f") { }
   }

   /// <summary>
   /// Canadian dollar.
   /// </summary>
   /// <remarks>
   /// The ISO three-letter code is CAD; the numeric code is 124.
   /// It is divided into 100 cents.
   /// </remarks>
   public class CADCurrency : Currency
   {
      public CADCurrency() : base("Canadian dollar", "CAD", 124, "Can$", "", 100, new Rounding(), "%3% %1$.2f") { }
   }

   /// <summary>
   /// Chilean peso.
   /// </summary>
   /// <remarks>
   /// The ISO three-letter code is CLP; the numeric code is 152.
   /// It is divided into 100 centavos.
   /// </remarks>
   public class CLPCurrency : Currency
   {
      public CLPCurrency() : base("Chilean peso", "CLP", 152, "Ch$", "", 100, new Rounding(), "%3% %1$.0f") { }
   }

   /// <summary>
   /// Colombian peso.
   /// </summary>
   /// <remarks>
   /// The ISO three-letter code is COP; the numeric code is 170.
   /// It is divided into 100 centavos.
   /// </remarks>
   public class COPCurrency : Currency
   {
      public COPCurrency() : base("Colombian peso", "COP", 170, "Col$", "", 100, new Rounding(), "%3% %1$.2f") { }
   }

   /// <summary>
   /// Mexican peso.
   /// </summary>
   /// <remarks>
   /// The ISO three-letter code is MXN; the numeric code is 484.
   /// It is divided into 100 centavos.
   /// </remarks>
   public class MXNCurrency : Currency
   {
      public MXNCurrency() : base("Mexican peso", "MXN", 484, "Mex$", "", 100, new Rounding(), "%3% %1$.2f") { }
   }

   /// <summary>
   /// Peruvian nuevo sol.
   /// </summary>
   /// <remarks>
   /// The ISO three-letter code is PEN; the numeric code is 604.
   /// It is divided into 100 centimos.
   /// </remarks>
   public class PENCurrency : Currency
   {
      public PENCurrency() : base("Peruvian nuevo sol", "PEN", 604, "S/.", "", 100, new Rounding(), "%3% %1$.2f") { }
   }

   /// <summary>
   /// Peruvian inti.
   /// </summary>
   /// <remarks>
   /// The ISO three-letter code was PEI.
   /// It was divided into 100 centimos. A numeric code is not available
   /// as per ISO 3166-1, so 998 is assigned as a user-defined code.
   ///
   /// Obsoleted by the nuevo sol since July 1991.
   /// </remarks>
   public class PEICurrency : Currency
   {
      public PEICurrency() : base("Peruvian inti", "PEI", 998, "I/.", "", 100, new Rounding(), "%3% %1$.2f") { }
   }

   /// <summary>
   /// Peruvian sol.
   /// </summary>
   /// <remarks>
   /// The ISO three-letter code was PEH. A numeric code is not available
   /// as per ISO 3166-1, so 999 is assigned as a user-defined code.
   /// It was divided into 100 centavos.
   ///
   /// Obsoleted by the inti since February 1985.
   /// </remarks>
   public class PEHCurrency : Currency
   {
      public PEHCurrency() : base("Peruvian sol", "PEH", 999, "S./", "", 100, new Rounding(), "%3% %1$.2f") { }
   }

   /// <summary>
   /// Trinidad &amp; Tobago dollar.
   /// </summary>
   /// <remarks>
   /// The ISO three-letter code is TTD; the numeric code is 780.
   /// It is divided into 100 cents.
   /// </remarks>
   public class TTDCurrency : Currency
   {
      public TTDCurrency() : base("Trinidad & Tobago dollar", "TTD", 780, "TT$", "", 100, new Rounding(), "%3% %1$.2f") { }
   }

   /// <summary>
   /// U.S. dollar.
   /// </summary>
   /// <remarks>
   /// The ISO three-letter code is USD; the numeric code is 840.
   /// It is divided into 100 cents.
   /// </remarks>
   public class USDCurrency : Currency
   {
      public USDCurrency() : base("U.S. dollar", "USD", 840, "$", "\xA2", 100, new Rounding(), "%3% %1$.2f") { }
   }

   /// <summary>
   /// Venezuelan bolivar.
   /// </summary>
   /// <remarks>
   /// The ISO three-letter code is VEB; the numeric code is 862.
   /// It is divided into 100 centimos.
   /// </remarks>
   public class VEBCurrency : Currency
   {
      public VEBCurrency() : base("Venezuelan bolivar", "VEB", 862, "Bs", "", 100, new Rounding(), "%3% %1$.2f") { }
   }

}
