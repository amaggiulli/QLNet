using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QLNet
{
   public static class ListExtension
   {
      //    list: List<T> to resize
      //    size: desired new size
      //    element: default value to insert

      public static void Resize<T>( this List<T> list, int size, T element = default(T) )
      {
         int count = list.Count;

         if ( size < count )
         {
            list.RemoveRange( size, count - size );
         }
         else if ( size > count )
         {
            if ( size > list.Capacity )   // Optimization
               list.Capacity = size;

            list.AddRange( Enumerable.Repeat( element, size - count ) );
         }
      }

      // erases the contents without changing the size
      public static void Erase<T>( this List<T> list )
      {
         for ( int i = 0; i < list.Count; i++ )
            list[i] = default( T );      
      }
   }
}
