/*
 Copyright (C) 2017 Jean-Camille Tournier (jean-camille.tournier@avivainvestors.com)

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
using System.Collections.Generic;
using System.Linq;

namespace QLNet
{
   public class FdmMesherComposite : FdmMesher
   {
      public FdmMesherComposite(FdmLinearOpLayout layout, List<Fdm1dMesher> mesher)
         : base(layout)
      {
         mesher_ = mesher;
         for (int i = 0; i < mesher.Count; ++i)
         {
            Utils.QL_REQUIRE(mesher[i].size() == layout.dim()[i],
                             () => "size of 1d mesher " + i + " does not fit to layout");
         }
      }

      public FdmMesherComposite(List<Fdm1dMesher> mesher)
         : base(getLayoutFromMeshers(mesher))
      {
         mesher_ = mesher;
      }

      public FdmMesherComposite(Fdm1dMesher mesher)
         : base(getLayoutFromMeshers(new List<Fdm1dMesher>() { mesher }))
      {
         mesher_ = new List<Fdm1dMesher>() { mesher };
      }

      public FdmMesherComposite(Fdm1dMesher m1, Fdm1dMesher m2)
         : base(getLayoutFromMeshers(new List<Fdm1dMesher>() { m1, m2 }))
      {
         mesher_ = new List<Fdm1dMesher>() { m1, m2 };
      }

      public FdmMesherComposite(Fdm1dMesher m1, Fdm1dMesher m2, Fdm1dMesher m3)
         : base(getLayoutFromMeshers(new List<Fdm1dMesher>() { m1, m2, m3 }))
      {
         mesher_ = new List<Fdm1dMesher>() { m1, m2, m3 };
      }

      public FdmMesherComposite(Fdm1dMesher m1, Fdm1dMesher m2, Fdm1dMesher m3, Fdm1dMesher m4)
         : base(getLayoutFromMeshers(new List<Fdm1dMesher>() { m1, m2, m3, m4 }))
      {
         mesher_ = new List<Fdm1dMesher>() { m1, m2, m3, m4 };
      }

      public override double? dplus(FdmLinearOpIterator iter, int direction)
      {
         return mesher_[direction].dplus(iter.coordinates()[direction]);
      }

      public override double? dminus(FdmLinearOpIterator iter, int direction)
      {
         return mesher_[direction].dminus(iter.coordinates()[direction]);
      }

      public override double location(FdmLinearOpIterator iter,
                                      int direction)
      {
         return mesher_[direction].location(iter.coordinates()[direction]);
      }

      public override Vector locations(int direction)
      {
         Vector retVal = new Vector(layout_.size());

         FdmLinearOpIterator endIter = layout_.end();
         for (FdmLinearOpIterator iter = layout_.begin();
              iter != endIter; ++iter)
         {
            retVal[iter.index()] =
               mesher_[direction].locations()[iter.coordinates()[direction]];
         }

         return retVal;
      }

      public List<Fdm1dMesher> getFdm1dMeshers()
      {
         return mesher_;
      }

      protected static FdmLinearOpLayout getLayoutFromMeshers(List<Fdm1dMesher> meshers)
      {
         List<int> dim = new InitializedList<int>(meshers.Count);
         for (int i = 0; i < dim.Count; ++i)
         {
            dim[i] = meshers[i].size();
         }
         return new FdmLinearOpLayout(dim);
      }

      protected Vector dx_;
      protected List<List<double>> locations_;
      protected List<Fdm1dMesher> mesher_;
   }
}
