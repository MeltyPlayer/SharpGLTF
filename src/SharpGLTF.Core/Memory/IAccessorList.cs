using System.Collections.Generic;

namespace SharpGLTF.Memory
{
    public interface IAccessorList<T> : IList<T>, IReadOnlyList<T> where T : unmanaged
    {
        new T this[int index] { get; set; }
        new int Count { get; }

        public delegate void ForEachHandler(int index, T element);

        void ForEach(ForEachHandler handler);
    }
}
