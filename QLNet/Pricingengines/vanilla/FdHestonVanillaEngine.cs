//  Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)
//  
//  This file is part of QLNet Project https://github.com/amaggiulli/qlnet
//  QLNet is free software: you can redistribute it and/or modify it
//  under the terms of the QLNet license.  You should have received a
//  copy of the license along with this program; if not, license is  
//  available online at <http://qlnet.sourceforge.net/License.html>.
//   
//  QLNet is a based on QuantLib, a free-software/open-source library
//  for financial quantitative analysts and developers - http://quantlib.org/
//  The QuantLib license is available online at http://quantlib.org/license.shtml.
//  
//  This program is distributed in the hope that it will be useful, but WITHOUT
//  ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
//  FOR A PARTICULAR PURPOSE.  See the license for more details.

namespace QLNet
{
   //public struct FdmSchemeDesc
   //{
   //   public enum FdmSchemeType
   //   {
   //      HundsdorferType,
   //      DouglasType,
   //      CraigSneydType,
   //      ModifiedCraigSneydType,
   //      ImplicitEulerType,
   //      ExplicitEulerType
   //   };

   //   public FdmSchemeDesc( FdmSchemeType aType, double aTheta, double aMu )
   //   {
   //      type = aType;
   //      theta = aTheta;
   //      mu = aMu;
   //   }

   //   FdmSchemeType type;
   //   double theta, mu;

   //   // some default scheme descriptions
   //   public static FdmSchemeDesc Douglas() { return new FdmSchemeDesc( FdmSchemeType.DouglasType, 0.5, 0.0 ); }
   //   public static FdmSchemeDesc ImplicitEuler() { return new FdmSchemeDesc( FdmSchemeType.ImplicitEulerType, 0.0, 0.0 ); }
   //   public static FdmSchemeDesc ExplicitEuler() { return new FdmSchemeDesc( FdmSchemeType.ExplicitEulerType, 0.0, 0.0 ); }
   //   public static FdmSchemeDesc CraigSneyd() { return new FdmSchemeDesc( FdmSchemeType.CraigSneydType, 0.5, 0.5 ); }
   //   public static FdmSchemeDesc ModifiedCraigSneyd()
   //   {
   //      return new FdmSchemeDesc(FdmSchemeType.ModifiedCraigSneydType, 1.0/3.0, 1.0/3.0);
   //   }
   //   public static FdmSchemeDesc Hundsdorfer() { return new FdmSchemeDesc( FdmSchemeType.HundsdorferType, 0.5 + Math.Sqrt( 3.0 ) / 6, 0.5 ); }
   //   public static FdmSchemeDesc ModifiedHundsdorfer()
   //   {
   //      return new FdmSchemeDesc( FdmSchemeType.HundsdorferType,
   //         1.0 - Math.Sqrt( 2.0 ) / 2, 0.5 );
   //   }
   //}

   ////! Finite-Differences Heston Vanilla Option engine

   ///*! \ingroup vanillaengines

   //    \test the correctness of the returned value is tested by
   //          reproducing results available in web/literature
   //          and comparison with Black pricing.
   //*/
   //public class FdHestonVanillaEngine : GenericModelEngine<HestonModel,DividendVanillaOption.Arguments,
   //                                                        DividendVanillaOption.Results>
   //{
   //   // Constructor
   //   public FdHestonVanillaEngine( HestonModel model, int tGrid = 100, int xGrid = 100,
   //         int vGrid = 50, int dampingSteps = 0, FdmSchemeDesc schemeDesc = null )
   //   {
   //      //schemeDesc = FdmSchemeDesc.Hundsdorfer()
   //   }

   //   public void calculate() ;
        
   //   // multiple strikes caching engine
   //   public void update();
   //   public void enableMultipleStrikesCaching(List<double> strikes);
        
   //   // helper method for Heston like engines
   //   public FdmSolverDesc getSolverDesc(double equityScaleFactor) ;

   //   private int tGrid_, xGrid_, vGrid_, dampingSteps_;
   //   private FdmSchemeDesc schemeDesc_;
        
   //   List<double> strikes_;
   //   List<KeyValuePair<DividendVanillaOption.Arguments,DividendVanillaOption.Results> > cachedArgs2results_;

   //}
}
