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
    //! Correlated multiple asset paths
    /*! MultiPath contains the list of paths for each asset, i.e.,
        multipath[j] is the path followed by the j-th asset.

        \ingroup mcarlo
    */
    public class MultiPath : IPath {
        private List<Path> multiPath_;
        
        public MultiPath() {}
        public MultiPath(int nAsset, TimeGrid timeGrid) {
            multiPath_ = new List<Path>(nAsset);
            for (int i = 0; i < nAsset; i++)
                multiPath_.Add(new Path(timeGrid));
            if (!(nAsset > 0)) throw new ApplicationException("number of asset must be positive");
        }

        public MultiPath(List<Path> multiPath) {
            multiPath_ = multiPath;
        }

        //! \name inspectors
        public int assetNumber() { return multiPath_.Count; }
        public int length() { return pathSize(); }
        public int pathSize() { return multiPath_[0].length(); }

        //! \name read/write access to components
        public Path this[int j] { get { return multiPath_[j]; } set { multiPath_[j] = value; } }

        // ICloneable interface
        public object Clone() {
            MultiPath temp = (MultiPath)this.MemberwiseClone();
            temp.multiPath_ = new List<Path>(this.multiPath_);
            return temp;
        }
    }
}
