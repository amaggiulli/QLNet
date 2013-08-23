/*
 Copyright (C) 2010 Philippe Real (ph_real@hotmail.com)
  
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

    //! Two-dimensional tree-based lattice.
    /*! This lattice is based on two trinomial trees and primarily used
        for the G2 short-rate model.

        \ingroup lattices
    */
    public class TreeLattice2D<T, Tl> : TreeLattice<T> 
        where T : IGenericLattice 
        where Tl  : TrinomialTree
    {
        Matrix m_;
        double rho_;
        
        protected Tl tree1_; 
        protected Tl tree2_;
        //public enum Branches { branches = 3 };
        public enum Branches { branches = 3 };
        //// smelly
        
        public override Vector grid(double t) {  throw new NotImplementedException("not implemented"); }

        // this is a workaround for CuriouslyRecurringTemplate of TreeLattice
        // recheck it
    /*  public override TreeLattice2D impl() { return this; }
    */
        public TreeLattice2D(TrinomialTree tree1,TrinomialTree tree2,double correlation)
            : base(tree1.timeGrid(), (int)Branches.branches * (int)Branches.branches)
        {
            tree1_ = (Tl) tree1; //le cast à voir!!
            tree2_ = (Tl)tree2; //le cast à voir!!
            m_ = new Matrix((int)Branches.branches, (int)Branches.branches);
            rho_ = Math.Abs(correlation);

            // what happens here?
            if (correlation < 0.0 && (int)Branches.branches == 3)
            {
                m_[0, 0] = -1.0;
                m_[0, 1] = -4.0;
                m_[0, 2] = 5.0;
                m_[1, 0] = -4.0;
                m_[1, 1] = 8.0;
                m_[1, 2] = -4.0;
                m_[2, 0] = 5.0;
                m_[2, 1] = -4.0;
                m_[2, 2] = -1.0;
            }
            else
            {
                m_[0, 0] = 5.0;
                m_[0, 1] = -4.0;
                m_[0, 2] = -1.0;
                m_[1, 0] = -4.0;
                m_[1, 1] = 8.0;
                m_[1, 2] = -4.0;
                m_[2, 0] = -1.0;
                m_[2, 1] = -4.0;
                m_[2, 2] = 5.0;
            }
        }

        public int size(int i) {return (tree1_.size(i)*tree2_.size(i));}

        public int descendant(int i, int index, int branch)
        {
                int modulo = tree1_.size(i);

         int index1 = index % modulo;
         int index2 = index / modulo;
         int branch1 = branch % (int)Branches.branches;
         int branch2 = branch / (int)Branches.branches;

        modulo = tree1_.size(i+1);
        return tree1_.descendant(i, index1, branch1) +
            tree2_.descendant(i, index2, branch2)*modulo;
        }

        public double probability(int i, int index, int branch)
        {
            int modulo = tree1_.size(i);
            int index1 = index % modulo;
            int index2 = index / modulo;
            int branch1 = branch % (int)Branches.branches;
            int branch2 = branch / (int)Branches.branches;

            double prob1 = tree1_.probability(i, index1, branch1);
            double prob2 = tree2_.probability(i, index2, branch2);
            // does the 36 below depend on T::branches?
            return prob1*prob2 + rho_*(m_[branch1,branch2])/36.0;
        }       

    }

}


