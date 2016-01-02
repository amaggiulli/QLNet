using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QLNet
{
   public interface IPrepayModel
   {
      double getCPR(Date valDate);
      double getSMM(Date valDate);
   }
}
