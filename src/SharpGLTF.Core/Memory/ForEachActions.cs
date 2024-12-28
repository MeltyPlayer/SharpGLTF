namespace SharpGLTF.Memory
{
    public enum ParallelType {
        ROW_AND_SUB,
        SUB_ONLY,
    }

    public interface IForEachSubAction
    {
        void Handle(int rowI, int subI, float value);
    }

    public interface IForEachAction<in T> where T : struct
    {
        void Handle(int rowI, T value);
    }
}
