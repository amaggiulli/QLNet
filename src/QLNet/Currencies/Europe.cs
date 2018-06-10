/*
 Copyright (C) 2008 Andrea Maggiulli
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

namespace QLNet
{
   /// <summary>
   /// Bulgarian lev
   /// The ISO three-letter code is BGL; the numeric code is 100.
   /// It is divided in 100 stotinki.
   /// </summary>
   public class BGLCurrency : Currency
   {
      public BGLCurrency() : base("Bulgarian lev", "BGL", 100, "lv", "", 100, new Rounding(), "%1$.2f %3%") { }
   }

   /// <summary>
   /// Belarussian ruble
   /// The ISO three-letter code is BYR; the numeric code is 974.
   /// It has no subdivisions.
   /// </summary>
   public class BYRCurrency : Currency
   {
      public BYRCurrency() : base("Belarussian ruble", "BYR", 974, "BR", "", 1, new Rounding(), "%2% %1$.0f") { }
   }

   /// <summary>
   /// Swiss franc
   /// The ISO three-letter code is CHF; the numeric code is 756.
   /// It is divided into 100 cents.
   /// </summary>
   public class CHFCurrency : Currency
   {
      public CHFCurrency() : base("Swiss franc", "CHF", 756, "SwF", "", 100, new Rounding(), "%3% %1$.2f") { }
   }

   /// <summary>
   /// Cyprus pound
   /// The ISO three-letter code is CYP; the numeric code is 196.
   /// It is divided in 100 cents.
   /// </summary>
   public class CYPCurrency : Currency
   {
      public CYPCurrency() : base("Cyprus pound", "CYP", 196, "\xA3C", "", 100, new Rounding(), "%3% %1$.2f") { }
   }

   /// <summary>
   /// Czech koruna
   /// The ISO three-letter code is CZK; the numeric code is 203.
   /// It is divided in 100 haleru.
   /// </summary>
   public class CZKCurrency : Currency
   {
      public CZKCurrency() : base("Czech koruna", "CZK", 203, "Kc", "", 100, new Rounding(), "%1$.2f %3%") { }
   }

   /// <summary>
   /// Danish krone
   /// The ISO three-letter code is DKK; the numeric code is 208.
   /// It is divided in 100 шre.
   /// </summary>
   public class DKKCurrency : Currency
   {
      public DKKCurrency() : base("Danish krone", "DKK", 208, "Dkr", "", 100, new Rounding(), "%3% %1$.2f") { }
   }

   /// <summary>
   /// Estonian kroon
   /// The ISO three-letter code is EEK; the numeric code is 233.
   /// It is divided in 100 senti.
   /// </summary>
   public class EEKCurrency : Currency
   {
      public EEKCurrency() : base("Estonian kroon", "EEK", 233, "KR", "", 100, new Rounding(), "%1$.2f %2%") { }
   }

   /// <summary>
   /// European Euro
   /// The ISO three-letter code is EUR; the numeric code is 978.
   /// It is divided into 100 cents.
   /// </summary>
   public class EURCurrency : Currency
   {
      public EURCurrency() : base("European Euro", "EUR", 978, "", "", 100, new Rounding(2, Rounding.Type.Closest), "%2% %1$.2f") { }
   }

   /// <summary>
   /// British pound sterling
   /// The ISO three-letter code is GBP; the numeric code is 826.
   /// It is divided into 100 pence.
   /// </summary>
   public class GBPCurrency : Currency
   {
      public GBPCurrency() : base("British pound sterling", "GBP", 826, "\xA3", "p", 100, new Rounding(), "%3% %1$.2f") { }
   }

   /// <summary>
   /// Hungarian forint
   /// The ISO three-letter code is HUF; the numeric code is 348.
   /// It has no subdivisions.
   /// </summary>
   public class HUFCurrency : Currency
   {
      public HUFCurrency() : base("Hungarian forint", "HUF", 348, "Ft", "", 1, new Rounding(), "%1$.0f %3%") { }
   }

   /// <summary>
   /// Icelandic krona
   /// The ISO three-letter code is ISK; the numeric code is 352.
   /// It is divided in 100 aurar.
   /// </summary>
   public class ISKCurrency : Currency
   {
      public ISKCurrency() : base("Iceland krona", "ISK", 352, "IKr", "", 100, new Rounding(), "%1$.2f %3%") { }
   }

   /// <summary>
   /// Lithuanian litas
   /// The ISO three-letter code is LTL; the numeric code is 440.
   /// It is divided in 100 centu.
   /// </summary>
   public class LTLCurrency : Currency
   {
      public LTLCurrency() : base("Lithuanian litas", "LTL", 440, "Lt", "", 100, new Rounding(), "%1$.2f %3%") { }
   }

   /// <summary>
   /// Latvian lat
   /// The ISO three-letter code is LVL; the numeric code is 428.
   /// It is divided in 100 santims.
   /// </summary>
   public class LVLCurrency : Currency
   {
      public LVLCurrency() : base("Latvian lat", "LVL", 428, "Ls", "", 100, new Rounding(), "%3% %1$.2f") { }
   }

   /// <summary>
   /// Maltese lira
   /// The ISO three-letter code is MTL; the numeric code is 470.
   /// It is divided in 100 cents.
   /// </summary>
   public class MTLCurrency : Currency
   {
      public MTLCurrency() : base("Maltese lira", "MTL", 470, "Lm", "", 100, new Rounding(), "%3% %1$.2f") { }
   }

   /// Norwegian krone
   /// The ISO three-letter code is NOK; the numeric code is 578.
   /// It is divided in 100 шre.
   public class NOKCurrency : Currency
   {
      public NOKCurrency() : base("Norwegian krone", "NOK", 578, "NKr", "", 100, new Rounding(), "%3% %1$.2f") { }
   }

   /// Polish zloty
   /// The ISO three-letter code is PLN; the numeric code is 985.
   /// It is divided in 100 groszy.
   public class PLNCurrency : Currency
   {
      public PLNCurrency() : base("Polish zloty", "PLN", 985, "zl", "", 100, new Rounding(), "%1$.2f %3%") { }
   }

   /// Romanian leu
   /// The ISO three-letter code was ROL; the numeric code was 642.
   /// It was divided in 100 bani.
   public class ROLCurrency : Currency
   {
      public ROLCurrency() : base("Romanian leu", "ROL", 642, "L", "", 100, new Rounding(), "%1$.2f %3%") { }
   }

   /// Romanian new leu
   /// The ISO three-letter code is RON; the numeric code is 946.
   /// It is divided in 100 bani.
   public class RONCurrency : Currency
   {
      public RONCurrency() : base("Romanian new leu", "RON", 946, "L", "", 100, new Rounding(), "%1$.2f %3%") { }
   }

   //! Russian ruble
   /*! The ISO three-letter code is RUB; the numeric code is 643.
       It is divided in 100 kopeyki.

       \ingroup currencies
   */
   public class RUBCurrency : Currency
   {
      public RUBCurrency(): base("Russian ruble", "RUB", 643, "", "", 100, new Rounding(), "%1$.2f %2%") { }
   }

   /// Swedish krona
   /// The ISO three-letter code is SEK; the numeric code is 752.
   /// It is divided in 100 цre.
   public class SEKCurrency : Currency
   {
      public SEKCurrency() : base("Swedish krona", "SEK", 752, "kr", "", 100, new Rounding(), "%1$.2f %3%") { }
   }

   /// Slovenian tolar
   /// The ISO three-letter code is SIT; the numeric code is 705.
   /// It is divided in 100 stotinov.
   public class SITCurrency : Currency
   {
      public SITCurrency() : base("Slovenian tolar", "SIT", 705, "SlT", "", 100, new Rounding(), "%1$.2f %3%") { }
   }

   /// Slovak koruna
   /// The ISO three-letter code is SKK; the numeric code is 703.
   /// It is divided in 100 halierov.
   public class SKKCurrency : Currency
   {
      public SKKCurrency() : base("Slovak koruna", "SKK", 703, "Sk", "", 100, new Rounding(), "%1$.2f %3%") { }
   }

   /// Turkish lira
   /// The ISO three-letter code was TRL; the numeric code was 792.
   /// It was divided in 100 kurus.
   /// Obsoleted by the new Turkish lira since 2005.
   public class TRLCurrency : Currency
   {
      public TRLCurrency() : base("Turkish lira", "TRL", 792, "TL", "", 100, new Rounding(), "%1$.0f %3%") { }
   }

   /// New Turkish lira
   /// The ISO three-letter code is TRY; the numeric code is 949.
   ///  It is divided in 100 new kurus.
   public class TRYCurrency : Currency
   {
      public TRYCurrency() : base("New Turkish lira", "TRY", 949, "YTL", "", 100, new Rounding(), "%1$.2f %3%") { }
   }

   // currencies obsoleted by Euro

   /// Austrian shilling
   /// The ISO three-letter code was ATS; the numeric code was 40.
   /// It was divided in 100 groschen.
   /// Obsoleted by the Euro since 1999.
   public class ATSCurrency : Currency
   {
      public ATSCurrency() : base("Austrian shilling", "ATS", 40, "", "", 100, new Rounding(), "%2% %1$.2f", new EURCurrency()) { }
   }

   /// Belgian franc
   /// The ISO three-letter code was BEF; the numeric code was 56.
   /// It had no subdivisions.
   /// Obsoleted by the Euro since 1999.
   public class BEFCurrency : Currency
   {
      public BEFCurrency() : base("Belgian franc", "BEF", 56, "", "", 1, new Rounding(), "%2% %1$.0f", new EURCurrency()) { }
   }

   /// Deutsche mark
   /// The ISO three-letter code was DEM; the numeric code was 276.
   /// It was divided into 100 pfennig.
   /// Obsoleted by the Euro since 1999.
   public class DEMCurrency : Currency
   {
      public DEMCurrency() : base("Deutsche mark", "DEM", 276, "DM", "", 100, new Rounding(), "%1$.2f %3%", new EURCurrency()) { }
   }

   /// Spanish peseta
   /// The ISO three-letter code was ESP; the numeric code was 724.
   /// It was divided in 100 centimos.
   /// Obsoleted by the Euro since 1999.
   public class ESPCurrency : Currency
   {
      public ESPCurrency() : base("Spanish peseta", "ESP", 724, "Pta", "", 100, new Rounding(), "%1$.0f %3%", new EURCurrency()) { }
   }

   /// Finnish markka
   /// The ISO three-letter code was FIM; the numeric code was 246.
   /// It was divided in 100 penniд.
   /// Obsoleted by the Euro since 1999.
   public class FIMCurrency : Currency
   {
      public FIMCurrency() : base("Finnish markka", "FIM", 246, "mk", "", 100, new Rounding(), "%1$.2f %3%", new EURCurrency()) { }
   }

   /// French franc
   /// The ISO three-letter code was FRF; the numeric code was 250.
   /// It was divided in 100 centimes.
   /// Obsoleted by the Euro since 1999.
   public class FRFCurrency : Currency
   {
      public FRFCurrency() : base("French franc", "FRF", 250, "", "", 100, new Rounding(), "%1$.2f %2%", new EURCurrency()) { }
   }

   /// Greek drachma
   /// The ISO three-letter code was GRD; the numeric code was 300.
   /// It was divided in 100 lepta.
   /// Obsoleted by the Euro since 1999.
   public class GRDCurrency : Currency
   {
      public GRDCurrency() : base("Greek drachma", "GRD", 300, "", "", 100, new Rounding(), "%1$.2f %2%", new EURCurrency()) { }
   }

   /// Irish punt
   /// The ISO three-letter code was IEP; the numeric code was 372.
   /// It was divided in 100 pence.
   /// Obsoleted by the Euro since 1999.
   public class IEPCurrency : Currency
   {
      public IEPCurrency() : base("Irish punt", "IEP", 372, "", "", 100, new Rounding(), "%2% %1$.2f", new EURCurrency()) { }
   }

   /// Italian lira
   /// The ISO three-letter code was ITL; the numeric code was 380.
   /// It had no subdivisions.
   /// Obsoleted by the Euro since 1999.
   public class ITLCurrency : Currency
   {
      public ITLCurrency() : base("Italian lira", "ITL", 380, "L", "", 1, new Rounding(), "%3% %1$.0f", new EURCurrency()) { }
   }

   /// Luxembourg franc
   /// The ISO three-letter code was LUF; the numeric code was 442.
   /// It was divided in 100 centimes.
   /// Obsoleted by the Euro since 1999.
   public class LUFCurrency : Currency
   {
      public LUFCurrency() : base("Luxembourg franc", "LUF", 442, "F", "", 100, new Rounding(), "%1$.0f %3%", new EURCurrency()) { }
   }

   /// Dutch guilder
   /// The ISO three-letter code was NLG; the numeric code was 528.
   /// It was divided in 100 cents.
   /// Obsoleted by the Euro since 1999.
   public class NLGCurrency : Currency
   {
      public NLGCurrency() : base("Dutch guilder", "NLG", 528, "f", "", 100, new Rounding(), "%3% %1$.2f", new EURCurrency()) { }
   }

   /// Portuguese escudo
   /// The ISO three-letter code was PTE; the numeric code was 620.
   /// It was divided in 100 centavos.
   /// Obsoleted by the Euro since 1999.
   public class PTECurrency : Currency
   {
      public PTECurrency() : base("Portuguese escudo", "PTE", 620, "Esc", "", 100, new Rounding(), "%1$.0f %3%", new EURCurrency()) { }
   }

   //! Ukrainian hryvnia
   /*! The ISO three-letter code is UAH; the numeric code is 980.
      It is divided in 100 kopiykas.

      \ingroup currencies
   */
   public class UAHCurrency : Currency
   {
      public UAHCurrency() : base("Ukrainian hryvnia", "UAH", 980, "hrn", "", 100, new Rounding(), "%1$.2f %3%") { }
   }
}
