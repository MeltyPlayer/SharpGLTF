using System;

namespace SharpGLTF.Memory
{
    public interface IForEachAction
    {
        void Handle(int index, ReadOnlySpan<float> element);
    }

    public interface IForEachAction<in T> where T : struct
    {
        void Handle(int index, T element);
    }
}
