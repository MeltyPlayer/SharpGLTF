using System.Collections.Generic;
using System.Runtime.CompilerServices;


namespace SharpGLTF.Collections
{
    internal static class ListExtensions
    {
        private class ListDummy<T>
        {
            internal T[] Items;
        }

        public static T[] GetInternalArray<T>(this List<T> list)
        {
            return Unsafe.As<ListDummy<T>>(list).Items;
        }
    }
}
