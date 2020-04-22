using System;

namespace QLNet
{
   public class RootNotBracketException : Exception
   {
      public RootNotBracketException()
      {
      }

      public RootNotBracketException(string message)
         : base(message)
      {
      }

      public RootNotBracketException(string message, Exception inner)
         : base(message, inner)
      {
      }
   }
}
