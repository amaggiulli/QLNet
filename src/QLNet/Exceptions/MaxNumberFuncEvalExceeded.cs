using System;

namespace QLNet
{
   public class MaxNumberFuncEvalExceeded : Exception
   {
      public MaxNumberFuncEvalExceeded()
      {
      }

      public MaxNumberFuncEvalExceeded(string message)
         : base(message)
      {
      }

      public MaxNumberFuncEvalExceeded(string message, Exception inner)
         : base(message, inner)
      {
      }
   }
}
