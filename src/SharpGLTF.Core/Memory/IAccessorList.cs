using System.Collections.Generic;

namespace SharpGLTF.Memory
{
    public interface IAccessorList<T> : IList<T>, IReadOnlyList<T> where T : struct
    {
        new T this[int index] { get; set; }
        new int Count { get; }

        void ForEachSub<TAction>(TAction handler = default) where TAction : struct, IForEachSubAction;
        void ForEach<TAction>(TAction handler = default) where TAction : struct, IForEachAction<T>;
    }
}
