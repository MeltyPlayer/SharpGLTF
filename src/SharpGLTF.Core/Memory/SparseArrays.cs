using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;


namespace SharpGLTF.Memory
{
    /// <summary>
    /// Special accessor to wrap over a base accessor and a sparse accessor
    /// </summary>
    /// <typeparam name="T">An unmanage structure type.</typeparam>
    [System.Diagnostics.DebuggerDisplay("Sparse {typeof(T).Name} Accessor {Count}")]
    public readonly struct SparseArray<T> : IAccessorList<T>
        where T : unmanaged
    {
        #region lifecycle

        public SparseArray(IList<T> bottom, IList<T> top, IntegerArray topMapping)
        {
            _BottomItems = bottom;
            _TopItems = top;
            _Mapping = new Dictionary<int, int>();

            for (int val = 0; val < topMapping.Count; ++val)
            {
                var key = (int)topMapping[val];
                _Mapping[key] = val;
            }
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly IList<T> _BottomItems;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly IList<T> _TopItems;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly Dictionary<int, int> _Mapping;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
        private T[] _DebugItems => this.ToArray();

        #endregion

        #region API

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public int Count => _BottomItems.Count;

        public bool IsReadOnly => true;

        public T this[int index]
        {
            get => _Mapping.TryGetValue(index, out int topIndex) ? _TopItems[topIndex] : _BottomItems[index];
            set => throw new NotSupportedException("Collection is read only.");
        }

        public IEnumerator<T> GetEnumerator() { return new EncodedArrayEnumerator<T>(this); }

        IEnumerator IEnumerable.GetEnumerator() { return new EncodedArrayEnumerator<T>(this); }

        public bool Contains(T item) { return IndexOf(item) >= 0; }

        public int IndexOf(T item) { return this._FirstIndexOf(item); }

        public void CopyTo(T[] array, int arrayIndex) { Guard.NotNull(array, nameof(array)); this._CopyTo(array, arrayIndex); }

        void IList<T>.Insert(int index, T item) { throw new NotSupportedException(); }

        void IList<T>.RemoveAt(int index) { throw new NotSupportedException(); }

        void ICollection<T>.Add(T item) { throw new NotSupportedException(); }

        void ICollection<T>.Clear() { throw new NotSupportedException(); }

        bool ICollection<T>.Remove(T item) { throw new NotSupportedException(); }

        public void ForEachSub<TAction>(TAction handler = default) where TAction : struct, IForEachSubAction {
            Span<T> elementSpan = stackalloc T[1];
            var subSpan = MemoryMarshal.Cast<T, float>(elementSpan);
            for (var rowI = 0; rowI < this.Count; ++rowI)
            {
                elementSpan[0] = this[rowI];
                for (var subI = 0; subI < this.Count; ++subI)
                {
                    handler.Handle(subI, subI, subSpan[subI]);
                }
            }
        }

        public void ForEach<TAction>(TAction handler = default) where TAction : struct, IForEachAction<T>
        {
            for (var rowI = 0; rowI < this.Count; ++rowI)
            {
                handler.Handle(rowI, this[rowI]);
            }
        }

        #endregion
    }
}
