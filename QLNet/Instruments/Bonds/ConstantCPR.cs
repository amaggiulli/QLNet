using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QLNet
{
   public class ConstantCPR : IPrepayModel
   {
      public ConstantCPR(double cpr)
      {
         _cpr = cpr;
      }
      public double getCPR(Date valDate)
      {
         return _cpr;
      }
      public double getSMM(Date valDate)
      {
         return 1 - Math.Pow((1 - getCPR(valDate)), (1 / 12d));
      }

      private double _cpr;
   }
}
