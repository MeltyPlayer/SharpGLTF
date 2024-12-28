﻿using System;
using System.Collections.Generic;
using System.Numerics;
using System.Collections;
using System.Linq;
using System.Runtime.InteropServices;

using CommunityToolkit.HighPerformance.Helpers;

using BYTES = System.Memory<byte>;

using ENCODING = SharpGLTF.Schema2.EncodingType;
using static SharpGLTF.Memory.FloatingAccessor;

namespace SharpGLTF.Memory
{
    /// <summary>
    /// Wraps an encoded <see cref="BYTES"/> and exposes it as an array of strided <see cref="Single"/> values.
    /// </summary>
    readonly struct FloatingAccessor
    {
        private const string ERR_UNSUPPORTEDENCODING = "Unsupported encoding.";

        #region constructors

        public FloatingAccessor(BYTES source, int byteOffset, int itemsCount, int byteStride, int dimensions, ENCODING encoding, Boolean normalized)
        {
            var enclen = encoding.ByteLength();

            this._Data = source.Slice(byteOffset);
            this._Getter = null;
            this._Setter = null;
            this._Encoding = encoding;
            this._Normalized = normalized;
            this._ByteStride = Math.Max(byteStride, enclen * dimensions);
            this._EncodedLen = enclen;
            this._ItemCount = this._Data.Length / this._ByteStride;

            // strided buffers require 4 byte word padding.
            if ((_Data.Length % _ByteStride) >= enclen * dimensions) ++_ItemCount;

            _ItemCount = Math.Min(itemsCount, _ItemCount);

            if (encoding == ENCODING.FLOAT)
            {
                this._Setter = this._SetValue<Single>;
                this._Getter = this._GetValue<Single>;
                return;
            }

            if (normalized)
            {
                switch (encoding)
                {
                    case ENCODING.BYTE:
                        {
                            this._Setter = this._SetNormalizedS8;
                            this._Getter = this._GetNormalizedS8;
                            break;
                        }

                    case ENCODING.UNSIGNED_BYTE:
                        {
                            this._Setter = this._SetNormalizedU8;
                            this._Getter = this._GetNormalizedU8;
                            break;
                        }

                    case ENCODING.SHORT:
                        {
                            this._Setter = this._SetNormalizedS16;
                            this._Getter = this._GetNormalizedS16;
                            break;
                        }

                    case ENCODING.UNSIGNED_SHORT:
                        {
                            this._Setter = this._SetNormalizedU16;
                            this._Getter = this._GetNormalizedU16;
                            break;
                        }

                    default: throw new ArgumentException(ERR_UNSUPPORTEDENCODING, nameof(encoding));
                }
            }
            else
            {
                switch (encoding)
                {
                    case ENCODING.BYTE:
                        {
                            this._Setter = this._SetValueS8;
                            this._Getter = this._GetValueS8;
                            break;
                        }

                    case ENCODING.UNSIGNED_BYTE:
                        {
                            this._Setter = this._SetValueU8;
                            this._Getter = this._GetValueU8;
                            break;
                        }

                    case ENCODING.SHORT:
                        {
                            this._Setter = this._SetValueS16;
                            this._Getter = this._GetValueS16;
                            break;
                        }

                    case ENCODING.UNSIGNED_SHORT:
                        {
                            this._Setter = this._SetValueU16;
                            this._Getter = this._GetValueU16;
                            break;
                        }

                    case ENCODING.UNSIGNED_INT:
                        {
                            this._Setter = this._SetValueU32;
                            this._Getter = this._GetValueU32;
                            break;
                        }

                    case ENCODING.FLOAT:
                        break;

                    default: throw new ArgumentException("Unsupported encoding.", nameof(encoding));
                }
            }
        }

        #endregion

        #region encoding / decoding

        private Single _GetValueU8(int byteOffset) { return _GetValue<Byte>(byteOffset); }
        private void _SetValueU8(int byteOffset, Single value) { _SetValue<Byte>(byteOffset, (Byte)value); }

        private Single _GetValueS8(int byteOffset) { return _GetValue<SByte>(byteOffset); }
        private void _SetValueS8(int byteOffset, Single value) { _SetValue<SByte>(byteOffset, (SByte)value); }

        private Single _GetValueU16(int byteOffset) { return _GetValue<UInt16>(byteOffset); }
        private void _SetValueU16(int byteOffset, Single value) { _SetValue<UInt16>(byteOffset, (UInt16)value); }

        private Single _GetValueS16(int byteOffset) { return _GetValue<Int16>(byteOffset); }
        private void _SetValueS16(int byteOffset, Single value) { _SetValue<Int16>(byteOffset, (Int16)value); }

        private Single _GetValueU32(int byteOffset) { return _GetValue<UInt32>(byteOffset); }
        private void _SetValueU32(int byteOffset, Single value) { _SetValue<UInt32>(byteOffset, (UInt32)value); }

        private Single _GetNormalizedU8(int byteOffset) { return _GetValueU8(byteOffset) / 255.0f; }
        private void _SetNormalizedU8(int byteOffset, Single value) { _SetValueU8(byteOffset, value * 255.0f); }

        private Single _GetNormalizedS8(int byteOffset) { return Math.Max(_GetValueS8(byteOffset) / 127.0f, -1); }
        private void _SetNormalizedS8(int byteOffset, Single value) { _SetValueS8(byteOffset, (Single)Math.Round(value * 127.0f)); }

        private Single _GetNormalizedU16(int byteOffset) { return _GetValueU16(byteOffset) / 65535.0f; }
        private void _SetNormalizedU16(int byteOffset, Single value) { _SetValueU16(byteOffset, value * 65535.0f); }

        private Single _GetNormalizedS16(int byteOffset) { return Math.Max(_GetValueS16(byteOffset) / 32767.0f, -1); }
        private void _SetNormalizedS16(int byteOffset, Single value) { _SetValueS16(byteOffset, (Single)Math.Round(value * 32767.0f)); }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private T _GetValue<T>(int byteOffset)
            where T : unmanaged
        {
            return System.Runtime.InteropServices.MemoryMarshal.Read<T>(_Data.Span.Slice(byteOffset));
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private void _SetValue<T>(int byteOffset, T value)
            where T : unmanaged
        {
            var dst = _Data.Span.Slice(byteOffset);

            #if NET8_0_OR_GREATER
            System.Runtime.InteropServices.MemoryMarshal.Write<T>(dst, value);
            #else
            System.Runtime.InteropServices.MemoryMarshal.Write<T>(dst, ref value);
            #endif
        }

        #endregion

        #region data

        delegate Single _GetterCallback(int byteOffset);

        delegate void _SetterCallback(int byteOffset, Single value);

        private readonly BYTES _Data;

        private readonly ENCODING _Encoding;
        private readonly bool _Normalized;

        private readonly int _ByteStride;
        private readonly int _EncodedLen;

        private readonly int _ItemCount;

        private readonly _GetterCallback _Getter;
        private readonly _SetterCallback _Setter;

        #endregion

        #region API

        public int ByteLength => _Data.Length;

        public int Count => _ItemCount;

        public Single this[int index]
        {
            get => _Getter(index * _ByteStride);
            set
            {
                if (!value._IsFinite()) throw new NotFiniteNumberException(nameof(value), value);
                _Setter(index * _ByteStride, value);
            }
        }

        public Single this[int rowIndex, int subIndex]
        {
            get => _Getter((rowIndex * _ByteStride) + (subIndex * _EncodedLen));
            set
            {
                if (!value._IsFinite()) throw new NotFiniteNumberException(nameof(value), value);
                _Setter((rowIndex * _ByteStride) + (subIndex * _EncodedLen), value);
            }
        }
        
        public unsafe void CopyTo(Span<float> dst, int subCount) {
            using var pin = this._Data.Pin();
            var rowCount = Math.Min(dst.Length / subCount, this._ItemCount);

            if (_Normalized)
            {
                switch (_Encoding)
                {
                    case ENCODING.BYTE:
                        {
                            var scan0 = (sbyte*) pin.Pointer;
                            for (var rowI = 0; rowI < rowCount; ++rowI)
                            {
                                var basePtr = scan0 + rowI * _ByteStride;
                                for (var subI = 0; subI < subCount; ++subI)
                                {
                                    dst[subCount * rowI + subI] = Math.Max(basePtr[subI] / 127.0f, -1);
                                }
                            }
                            break;
                        }
                    case ENCODING.UNSIGNED_BYTE:
                        {
                            var scan0 = (byte*) pin.Pointer;
                            for (var rowI = 0; rowI < rowCount; ++rowI)
                            {
                                var basePtr = scan0 + rowI * _ByteStride;
                                for (var subI = 0; subI < subCount; ++subI)
                                {
                                    dst[subCount * rowI + subI] = basePtr[subI] / 255.0f;
                                }
                            }
                            break;
                        }
                    case ENCODING.SHORT:
                        {
                            var scan0 = (byte*) pin.Pointer;
                            for (var rowI = 0; rowI < rowCount; ++rowI)
                            {
                                var basePtr = (short*) (scan0 + rowI * _ByteStride);
                                for (var subI = 0; subI < subCount; ++subI)
                                {
                                    dst[subCount * rowI + subI] = Math.Max(basePtr[subI] / 32767.0f, -1);
                                }
                            }
                            break;
                        }
                    case ENCODING.UNSIGNED_SHORT:
                        {
                            var scan0 = (byte*) pin.Pointer;
                            for (var rowI = 0; rowI < rowCount; ++rowI)
                            {
                                var basePtr = (ushort*) (scan0 + rowI * _ByteStride);
                                for (var subI = 0; subI < subCount; ++subI)
                                {
                                    dst[subCount * rowI + subI] = basePtr[subI] / 65535.0f;
                                }
                            }
                            break;
                        }
                    default: throw new ArgumentOutOfRangeException();
                }
                return;
            }

            switch (_Encoding)
            {
                case ENCODING.BYTE:
                    {
                        var scan0 = (sbyte*) pin.Pointer;
                        for (var rowI = 0; rowI < rowCount; ++rowI)
                        {
                            var basePtr = scan0 + rowI * _ByteStride;
                            for (var subI = 0; subI < subCount; ++subI)
                            {
                                dst[subCount * rowI + subI] = basePtr[subI];
                            }
                        }
                        break;
                    }
                case ENCODING.UNSIGNED_BYTE:
                    {
                        var scan0 = (byte*) pin.Pointer;
                        for (var rowI = 0; rowI < rowCount; ++rowI)
                        {
                            var basePtr = scan0 + rowI * _ByteStride;
                            for (var subI = 0; subI < subCount; ++subI)
                            {
                                dst[subCount * rowI + subI] = basePtr[subI];
                            }
                        }
                        break;
                    }
                case ENCODING.SHORT:
                    {
                        var scan0 = (byte*) pin.Pointer;
                        for (var rowI = 0; rowI < rowCount; ++rowI)
                        {
                            var basePtr = (short*) (scan0 + rowI * _ByteStride);
                            for (var subI = 0; subI < subCount; ++subI)
                            {
                                dst[subCount * rowI + subI] = basePtr[subI];
                            }
                        }
                        break;
                    }
                case ENCODING.UNSIGNED_SHORT:
                    {
                        var scan0 = (byte*) pin.Pointer;
                        for (var rowI = 0; rowI < rowCount; ++rowI)
                        {
                            var basePtr = (ushort*) (scan0 + rowI * _ByteStride);
                            for (var subI = 0; subI < subCount; ++subI)
                            {
                                dst[subCount * rowI + subI] = basePtr[subI];
                            }
                        }
                        break;
                    }
                case ENCODING.UNSIGNED_INT:
                    {
                        var scan0 = (byte*) pin.Pointer;
                        for (var rowI = 0; rowI < rowCount; ++rowI)
                        {
                            var basePtr = (uint*) (scan0 + rowI * _ByteStride);
                            for (var subI = 0; subI < subCount; ++subI)
                            {
                                dst[subCount * rowI + subI] = basePtr[subI];
                            }
                        }
                        break;
                    }
                case ENCODING.FLOAT:
                    {
                        var scan0 = (byte*) pin.Pointer;
                        for (var rowI = 0; rowI < rowCount; ++rowI)
                        {
                            var basePtr = (float*) (scan0 + rowI * _ByteStride);
                            for (var subI = 0; subI < subCount; ++subI)
                            {
                                dst[subCount * rowI + subI] = basePtr[subI];
                            }
                        }
                        break;
                    }
                default: throw new ArgumentOutOfRangeException();
            }
        }

        public unsafe void Fill(ReadOnlySpan<float> src, int subCount, int offset = 0)
        {
            using var pin = this._Data.Pin();
            var rowCount = Math.Min(src.Length / subCount, this._ItemCount);

            if (_Normalized)
            {
                switch (_Encoding)
                {
                    case ENCODING.BYTE:
                        {
                            var scan0 = (sbyte*) pin.Pointer;
                            for (var rowI = 0; rowI < rowCount; ++rowI)
                            {
                                var basePtr = scan0 + rowI * _ByteStride;
                                for (var subI = 0; subI < subCount; ++subI)
                                {
                                    basePtr[subI] = (sbyte) Math.Round(src[subCount * rowI + subI] * 127.0f);
                                }
                            }
                            break;
                        }
                    case ENCODING.UNSIGNED_BYTE:
                        {
                            var scan0 = (byte*) pin.Pointer;
                            for (var rowI = 0; rowI < rowCount; ++rowI)
                            {
                                var basePtr = scan0 + rowI * _ByteStride;
                                for (var subI = 0; subI < subCount; ++subI)
                                {
                                    basePtr[subI] = (byte) (src[subCount * rowI + subI] * 255.0f);
                                }
                            }
                            break;
                        }
                    case ENCODING.SHORT:
                        {
                            var scan0 = (byte*) pin.Pointer;
                            for (var rowI = 0; rowI < rowCount; ++rowI)
                            {
                                var basePtr = (short*) (scan0 + rowI * _ByteStride);
                                for (var subI = 0; subI < subCount; ++subI)
                                {
                                    basePtr[subI] = (short) Math.Round(src[subCount * rowI + subI] * 32767.0f);
                                }
                            }
                            break;
                        }
                    case ENCODING.UNSIGNED_SHORT:
                        {
                            var scan0 = (byte*) pin.Pointer;
                            for (var rowI = 0; rowI < rowCount; ++rowI)
                            {
                                var basePtr = (ushort*) (scan0 + rowI * _ByteStride);
                                for (var subI = 0; subI < subCount; ++subI)
                                {
                                    basePtr[subI] = (ushort) (src[subCount * rowI + subI] * 65535.0f);
                                }
                            }
                            break;
                        }
                    default: throw new ArgumentOutOfRangeException();
                }
                return;
            }

            switch (_Encoding)
            {
                case ENCODING.BYTE:
                    {
                        var scan0 = (sbyte*) pin.Pointer;
                        for (var rowI = 0; rowI < rowCount; ++rowI)
                        {
                            var basePtr = scan0 + rowI * _ByteStride;
                            for (var subI = 0; subI < subCount; ++subI)
                            {
                                basePtr[subI] = (sbyte) src[subCount * rowI + subI];
                            }
                        }
                        break;
                    }
                case ENCODING.UNSIGNED_BYTE:
                    {
                        var scan0 = (byte*) pin.Pointer;
                        for (var rowI = 0; rowI < rowCount; ++rowI)
                        {
                            var basePtr = scan0 + rowI * _ByteStride;
                            for (var subI = 0; subI < subCount; ++subI)
                            {
                                basePtr[subI] = (byte) src[subCount * rowI + subI];
                            }
                        }
                        break;
                    }
                case ENCODING.SHORT:
                    {
                        var scan0 = (byte*) pin.Pointer;
                        for (var rowI = 0; rowI < rowCount; ++rowI)
                        {
                            var basePtr = (short*) (scan0 + rowI * _ByteStride);
                            for (var subI = 0; subI < subCount; ++subI)
                            {
                                basePtr[subI] = (short) src[subCount * rowI + subI];
                            }
                        }
                        break;
                    }
                case ENCODING.UNSIGNED_SHORT:
                    {
                        var scan0 = (byte*) pin.Pointer;
                        for (var rowI = 0; rowI < rowCount; ++rowI)
                        {
                            var basePtr = (ushort*) (scan0 + rowI * _ByteStride);
                            for (var subI = 0; subI < subCount; ++subI)
                            {
                                basePtr[subI] = (ushort) src[subCount * rowI + subI];
                            }
                        }
                        break;
                    }
                case ENCODING.UNSIGNED_INT:
                    {
                        var scan0 = (byte*) pin.Pointer;
                        for (var rowI = 0; rowI < rowCount; ++rowI)
                        {
                            var basePtr = (uint*) (scan0 + rowI * _ByteStride);
                            for (var subI = 0; subI < subCount; ++subI)
                            {
                                basePtr[subI] = (uint) src[subCount * rowI + subI];
                            }
                        }
                        break;
                    }
                case ENCODING.FLOAT:
                    {
                        var scan0 = (byte*) pin.Pointer;
                        for (var rowI = 0; rowI < rowCount; ++rowI)
                        {
                            var basePtr = (float*) (scan0 + rowI * _ByteStride);
                            for (var subI = 0; subI < subCount; ++subI)
                            {
                                basePtr[subI] = src[subCount * rowI + subI];
                            }
                        }
                        break;
                    }
                default: throw new ArgumentOutOfRangeException();
            }
        }

        internal unsafe void _ForEachSub<TAction>(int subCount, TAction handler = default, ParallelType parallelType = ParallelType.ROW_AND_SUB) where TAction : struct, IForEachSubAction
        {
            using var pin = this._Data.Pin();
            var rowCount = this._ItemCount;

            if (_Normalized)
            {
                switch (_Encoding)
                {
                    case ENCODING.BYTE:
                        {
                            var scan0 = (sbyte*) pin.Pointer;
                            for (var rowI = 0; rowI < rowCount; ++rowI)
                            {
                                var basePtr = scan0 + rowI * _ByteStride;
                                for (var subI = 0; subI < subCount; ++subI)
                                {
                                    handler.Handle(rowI, subI, Math.Max(*(basePtr + subI) / 127.0f, -1));
                                }
                            }
                            break;
                        }
                    case ENCODING.UNSIGNED_BYTE:
                        {
                            var scan0 = (byte*) pin.Pointer;
                            for (var rowI = 0; rowI < rowCount; ++rowI)
                            {
                                var basePtr = scan0 + rowI * _ByteStride;
                                for (var subI = 0; subI < subCount; ++subI)
                                {
                                    handler.Handle(rowI, subI, *(basePtr + subI) / 255.0f);
                                }
                            }
                            break;
                        }
                    case ENCODING.SHORT:
                        {
                            var scan0 = (byte*) pin.Pointer;
                            for (var rowI = 0; rowI < rowCount; ++rowI)
                            {
                                var basePtr = (short*) (scan0 + rowI * _ByteStride);
                                for (var subI = 0; subI < subCount; ++subI)
                                {
                                    handler.Handle(rowI, subI, Math.Max(*(basePtr + subI) / 32767.0f, -1));
                                }
                            }
                            break;
                        }
                    case ENCODING.UNSIGNED_SHORT:
                        {
                            var scan0 = (byte*) pin.Pointer;
                            for (var rowI = 0; rowI < rowCount; ++rowI)
                            {
                                var basePtr = (ushort*) (scan0 + rowI * _ByteStride);
                                for (var subI = 0; subI < subCount; ++subI)
                                {
                                    handler.Handle(rowI, subI, *(basePtr + subI) / 65535.0f);
                                }
                            }
                            break;
                        }
                    default: throw new ArgumentOutOfRangeException();
                }
                return;
            }

            switch (_Encoding)
            {
                case ENCODING.BYTE:
                    {
                        var scan0 = (sbyte*) pin.Pointer;
                        for (var rowI = 0; rowI < rowCount; ++rowI)
                        {
                            var basePtr = scan0 + rowI * _ByteStride;
                            for (var subI = 0; subI < subCount; ++subI)
                            {
                                handler.Handle(rowI, subI, *(basePtr + subI));
                            }
                        }
                        break;
                    }
                case ENCODING.UNSIGNED_BYTE:
                    {
                        var scan0 = (byte*) pin.Pointer;
                        switch (parallelType)
                        {
                            case ParallelType.ROW_AND_SUB:
                                {
                                    ParallelHelper.For2D(0, rowCount, 0, subCount, new UnnormalizedByteRowAndSubForEachAction<TAction>(scan0, _ByteStride, handler));
                                    break;
                                }
                            default:
                                {
                                    for (var rowI = 0; rowI < rowCount; ++rowI) {
                                        var basePtr = scan0 + rowI * _ByteStride;
                                        for (var subI = 0; subI < subCount; ++subI) {
                                            handler.Handle(rowI, subI, *(basePtr + subI));
                                        }
                                    }
                                    break;
                                }
                        }
                        break;
                    }
                case ENCODING.SHORT:
                    {
                        var scan0 = (byte*) pin.Pointer;
                        for (var rowI = 0; rowI < rowCount; ++rowI)
                        {
                            var basePtr = (short*) (scan0 + rowI * _ByteStride);
                            for (var subI = 0; subI < subCount; ++subI)
                            {
                                handler.Handle(rowI, subI, *(basePtr + subI));
                            }
                        }
                        break;
                    }
                case ENCODING.UNSIGNED_SHORT:
                    {
                        var scan0 = (byte*) pin.Pointer;
                        for (var rowI = 0; rowI < rowCount; ++rowI)
                        {
                            var basePtr = (ushort*) (scan0 + rowI * _ByteStride);
                            for (var subI = 0; subI < subCount; ++subI)
                            {
                                handler.Handle(rowI, subI, *(basePtr + subI));
                            }
                        }
                        break;
                    }
                case ENCODING.UNSIGNED_INT:
                    {
                        var scan0 = (byte*) pin.Pointer;
                        for (var rowI = 0; rowI < rowCount; ++rowI)
                        {
                            var basePtr = (uint*) (scan0 + rowI * _ByteStride);
                            for (var subI = 0; subI < subCount; ++subI)
                            {
                                handler.Handle(rowI, subI, *(basePtr + subI));
                            }
                        }
                        break;
                    }
                case ENCODING.FLOAT:
                    {
                        switch (parallelType)
                        {
                            case ParallelType.ROW_AND_SUB:
                                {
                                    ParallelHelper.For2D(0, rowCount, 0, subCount, new UnnormalizedFloatRowAndSubForEachAction<TAction>((byte*)pin.Pointer, _ByteStride, handler));
                                    break;
                                }
                            case ParallelType.SUB_ONLY:
                                {
                                    ParallelHelper.For(0, subCount, new UnnormalizedFloatSubForEachAction<TAction>((byte*)pin.Pointer, _ByteStride, rowCount, handler));
                                    break;
                                }
                            default: throw new ArgumentOutOfRangeException(nameof(parallelType), parallelType, null);
                        }
                        break;
                    }
                default: throw new ArgumentOutOfRangeException();
            }
        }

        private readonly unsafe struct UnnormalizedByteRowAndSubForEachAction<TAction> : IAction2D where TAction : struct, IForEachSubAction {
            private readonly byte* _scan0;
            private readonly int _byteStride;
            private readonly TAction _handler;

            public UnnormalizedByteRowAndSubForEachAction(byte* scan0, int byteStride, TAction handler) {
                this._scan0 = scan0;
                this._byteStride = byteStride;
                this._handler = handler;
            }

            public void Invoke(int rowI, int subI) {
                var basePtr = _scan0 + rowI * _byteStride;
                _handler.Handle(rowI, subI, basePtr[subI]);
            }
        }

        private readonly unsafe struct UnnormalizedFloatRowAndSubForEachAction<TAction> : IAction2D where TAction : struct, IForEachSubAction
        {
            private readonly byte* _scan0;
            private readonly int _byteStride;
            private readonly TAction _handler;

            public UnnormalizedFloatRowAndSubForEachAction(byte* scan0, int byteStride, TAction handler)
            {
                this._scan0 = scan0;
                this._byteStride = byteStride;
                this._handler = handler;
            }

            public void Invoke(int rowI, int subI)
            {
                var basePtr = (float*)(_scan0 + rowI * _byteStride);
                _handler.Handle(rowI, subI, basePtr[subI]);
            }
        }

        private readonly unsafe struct UnnormalizedFloatSubForEachAction<TAction> : IAction where TAction : struct, IForEachSubAction
        {
            private readonly byte* _scan0;
            private readonly int _byteStride;
            private readonly int _rowCount;
            private readonly TAction _handler;

            public UnnormalizedFloatSubForEachAction(byte* scan0, int byteStride, int rowCount, TAction handler)
            {
                this._scan0 = scan0;
                this._byteStride = byteStride;
                this._rowCount = rowCount;
                this._handler = handler;
            }

            public void Invoke(int subI)
            {
                for (var rowI = 0; rowI < _rowCount; rowI++)
                {
                    var basePtr = (float*) (_scan0 + rowI * _byteStride);
                    _handler.Handle(rowI, subI, basePtr[subI]);
                }
            }
        }

        internal unsafe void _ForEach<T, TAction>(TAction handler = default) 
            where T : unmanaged
            where TAction : struct, IForEachAction<T>
        {
            using var pin = this._Data.Pin();

            var tSize = sizeof(T);
            Guard.IsTrue(tSize % 4 == 0, nameof(tSize), "Size of T must be divisible by 4");

            var rowCount = this._ItemCount;
            var subCount = sizeof(T) >> 2;

            Span<T> elementSpan = stackalloc T[1];
            Span<float> subSpan = MemoryMarshal.Cast<T, float>(elementSpan);

            if (_Normalized) {
                switch (_Encoding) {
                    case ENCODING.BYTE:
                        {
                            var scan0 = (sbyte*) pin.Pointer;
                            for (var rowI = 0; rowI < rowCount; ++rowI)
                            {
                                var basePtr = scan0 + rowI * _ByteStride;
                                for (var subI = 0; subI < subCount; ++subI)
                                {
                                    subSpan[subI] = Math.Max(*(basePtr + subI) / 127.0f, -1);
                                }
                                handler.Handle(rowI, elementSpan[0]);
                            }
                            break;
                        }
                    case ENCODING.UNSIGNED_BYTE:
                        {
                            var scan0 = (byte*) pin.Pointer;
                            for (var rowI = 0; rowI < rowCount; ++rowI)
                            {
                                var basePtr = scan0 + rowI * _ByteStride;
                                for (var subI = 0; subI < subCount; ++subI)
                                {
                                    subSpan[subI] = *(basePtr + subI) / 255.0f;
                                }
                                handler.Handle(rowI, elementSpan[0]);
                            }
                            break;
                        }
                    case ENCODING.SHORT:
                        {
                            var scan0 = (byte*) pin.Pointer;
                            for (var rowI = 0; rowI < rowCount; ++rowI)
                            {
                                var basePtr = (short*) (scan0 + rowI * _ByteStride);
                                for (var subI = 0; subI < subCount; ++subI)
                                {
                                    subSpan[subI] = Math.Max(*(basePtr + subI) / 32767.0f, -1);
                                }
                                handler.Handle(rowI, elementSpan[0]);
                            }
                            break;
                        }
                    case ENCODING.UNSIGNED_SHORT:
                        {
                            var scan0 = (byte*) pin.Pointer;
                            for (var rowI = 0; rowI < rowCount; ++rowI)
                            {
                                var basePtr = (ushort*) (scan0 + rowI * _ByteStride);
                                for (var subI = 0; subI < subCount; ++subI)
                                {
                                    subSpan[subI] = *(basePtr + subI) / 65535.0f;
                                }
                                handler.Handle(rowI, elementSpan[0]);
                            }
                            break;
                        }
                    default: throw new ArgumentOutOfRangeException();
                }
                return;
            }

            switch (_Encoding) {
                case ENCODING.BYTE:
                    {
                        var scan0 = (sbyte*) pin.Pointer;
                        for (var rowI = 0; rowI < rowCount; ++rowI)
                        {
                            var basePtr = scan0 + rowI * _ByteStride;
                            for (var subI = 0; subI < subCount; ++subI)
                            {
                                subSpan[subI] = basePtr[subI];
                            }
                            handler.Handle(rowI, elementSpan[0]);
                        }
                        break;
                    }
                case ENCODING.UNSIGNED_BYTE:
                    {
                        var scan0 = (byte*) pin.Pointer;
                        for (var rowI = 0; rowI < rowCount; ++rowI)
                        {
                            var basePtr = scan0 + rowI * _ByteStride;
                            for (var subI = 0; subI < subCount; ++subI)
                            {
                                subSpan[subI] = basePtr[subI];
                            }
                            handler.Handle(rowI, elementSpan[0]);
                        }
                        break;
                    }
                case ENCODING.SHORT:
                    {
                        var scan0 = (byte*) pin.Pointer;
                        for (var rowI = 0; rowI < rowCount; ++rowI)
                        {
                            var basePtr = (short*) (scan0 + rowI * _ByteStride);
                            for (var subI = 0; subI < subCount; ++subI) {
                                subSpan[subI] = basePtr[subI];
                            }
                            handler.Handle(rowI, elementSpan[0]);
                        }
                        break;
                    }
                case ENCODING.UNSIGNED_SHORT:
                    {
                        var scan0 = (byte*) pin.Pointer;
                        for (var rowI = 0; rowI < rowCount; ++rowI)
                        {
                            var basePtr = (ushort*) (scan0 + rowI * _ByteStride);
                            for (var subI = 0; subI < subCount; ++subI) {
                                subSpan[subI] = basePtr[subI];
                            }
                            handler.Handle(rowI, elementSpan[0]);
                        }
                        break;
                    }
                case ENCODING.UNSIGNED_INT:
                    {
                        var scan0 = (byte*) pin.Pointer;
                        for (var rowI = 0; rowI < rowCount; ++rowI)
                        {
                            var basePtr = (uint*) (scan0 + rowI * _ByteStride);
                            for (var subI = 0; subI < subCount; ++subI) {
                                subSpan[subI] = basePtr[subI];
                            }
                            handler.Handle(rowI, elementSpan[0]);
                        }
                        break;
                    }
                case ENCODING.FLOAT:
                    {
                        ParallelHelper.For(0, rowCount, new TForEachAction<T, TAction>((byte*) pin.Pointer, _ByteStride, handler));
                        break;
                    }
                default: throw new ArgumentOutOfRangeException();
            }
        }

        private readonly unsafe struct TForEachAction<T, TAction> : IAction
                where T : unmanaged
                where TAction : struct, IForEachAction<T>
        {
            private readonly byte* _scan0;
            private readonly int _byteStride;
            private readonly TAction _handler;

            public TForEachAction(byte* scan0, int byteStride, TAction handler) {
                this._scan0 = scan0;
                this._byteStride = byteStride;
                this._handler = handler;
            }

            public void Invoke(int rowI) {
                _handler.Handle(rowI, *(T*) (_scan0 + rowI * _byteStride));
            }
        }

        #endregion
    }

    /// <summary>
    /// Wraps an encoded <see cref="BYTES"/> and exposes it as an <see cref="IList{single}"/>.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("Float[{Count}]")]
    public readonly struct ScalarArray : IAccessorList<Single>
    {
        #region constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ScalarArray"/> struct.
        /// </summary>
        /// <param name="source">The array range to wrap.</param>
        /// <param name="byteStride">
        /// The byte stride between elements.
        /// If the value is zero, the size of the item is used instead.
        /// </param>
        /// <param name="encoding">A value of <see cref="ENCODING"/>.</param>
        /// <param name="normalized">True if values are normalized.</param>
        public ScalarArray(BYTES source, int byteStride = 0, ENCODING encoding = ENCODING.FLOAT, Boolean normalized = false)
            : this(source, 0, int.MaxValue, byteStride, encoding, normalized) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScalarArray"/> struct.
        /// </summary>
        /// <param name="source">The array range to wrap.</param>
        /// <param name="byteOffset">The zero-based index of the first <see cref="Byte"/> in <paramref name="source"/>.</param>
        /// <param name="itemsCount">The number of <see cref="Single"/> items in <paramref name="source"/>.</param>
        /// <param name="byteStride">
        /// The byte stride between elements.
        /// If the value is zero, the size of the item is used instead.
        /// </param>
        /// <param name="encoding">A value of <see cref="ENCODING"/>.</param>
        /// <param name="normalized">True if values are normalized.</param>
        public ScalarArray(BYTES source, int byteOffset, int itemsCount, int byteStride, ENCODING encoding = ENCODING.FLOAT, Boolean normalized = false)
        {
            _Accessor = new FloatingAccessor(source, byteOffset, itemsCount, byteStride, 1, encoding, normalized);
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly FloatingAccessor _Accessor;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
        private Single[] _DebugItems => this.ToArray();

        #endregion

        #region API

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public int Count => _Accessor.Count;

        bool ICollection<Single>.IsReadOnly => false;

        public Single this[int index]
        {
            get => _Accessor[index, 0];
            set => _Accessor[index, 0] = value;
        }

        public IEnumerator<Single> GetEnumerator() { return new EncodedArrayEnumerator<Single>(this); }

        IEnumerator IEnumerable.GetEnumerator() { return new EncodedArrayEnumerator<Single>(this); }

        public bool Contains(Single item) { return IndexOf(item) >= 0; }

        public int IndexOf(Single item) { return this._FirstIndexOf(item); }

        public void CopyTo(Single[] array, int arrayIndex)
        {
            Guard.NotNull(array, nameof(array));
            this._CopyTo(array, arrayIndex);
        }

        public void Fill(IEnumerable<Single> values, int dstStart = 0)
        {
            Guard.NotNull(values, nameof(values));
            values._CopyTo(this, dstStart);
        }

        public void FillSpan(ReadOnlySpan<Single> values, int dstStart = 0)
        {
            _Accessor.Fill(values, 1, dstStart);
        }

        public void ForEachSub<TAction>(TAction handler = default) where TAction : struct, IForEachSubAction
        {
            _Accessor._ForEachSub(1, handler);
        }

        public void ForEach<TAction>(TAction handler = default) where TAction : struct, IForEachAction<Single>
        {
            _Accessor._ForEach<float, TAction>(handler);
        }

        void IList<Single>.Insert(int index, Single item) { throw new NotSupportedException(); }

        void IList<Single>.RemoveAt(int index) { throw new NotSupportedException(); }

        void ICollection<Single>.Add(Single item) { throw new NotSupportedException(); }

        void ICollection<Single>.Clear() { throw new NotSupportedException(); }

        bool ICollection<Single>.Remove(Single item) { throw new NotSupportedException(); }

        #endregion
    }

    /// <summary>
    /// Wraps an encoded <see cref="BYTES"/> and exposes it as an <see cref="IList{Vector2}"/>.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("Vector2[{Count}]")]
    public readonly struct Vector2Array : IAccessorList<Vector2>
    {
        #region constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Vector2Array"/> struct.
        /// </summary>
        /// <param name="source">The array range to wrap.</param>
        /// <param name="byteStride">
        /// The byte stride between elements.
        /// If the value is zero, the size of the item is used instead.
        /// </param>
        /// <param name="encoding">A value of <see cref="ENCODING"/>.</param>
        /// <param name="normalized">True if values are normalized.</param>
        public Vector2Array(BYTES source, int byteStride = 0, ENCODING encoding = ENCODING.FLOAT, Boolean normalized = false)
            : this(source, 0, int.MaxValue, byteStride, encoding, normalized) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Vector2Array"/> struct.
        /// </summary>
        /// <param name="source">The array range to wrap.</param>
        /// <param name="byteOffset">The zero-based index of the first <see cref="Byte"/> in <paramref name="source"/>.</param>
        /// <param name="itemsCount">>The number of <see cref="Vector2"/> items in <paramref name="source"/>.</param>
        /// <param name="byteStride">
        /// The byte stride between elements.
        /// If the value is zero, the size of the item is used instead.
        /// </param>
        /// <param name="encoding">A value of <see cref="ENCODING"/>.</param>
        /// <param name="normalized">True if values are normalized.</param>
        public Vector2Array(BYTES source, int byteOffset, int itemsCount, int byteStride, ENCODING encoding = ENCODING.FLOAT, Boolean normalized = false)
        {
            _Accessor = new FloatingAccessor(source, byteOffset, itemsCount, byteStride, 2, encoding, normalized);
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly FloatingAccessor _Accessor;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
        private Vector2[] _DebugItems => this.ToArray();

        #endregion

        #region API

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public int Count => _Accessor.Count;

        bool ICollection<Vector2>.IsReadOnly => false;

        public Vector2 this[int index]
        {
            get
            {
                return new Vector2(_Accessor[index, 0], _Accessor[index, 1]);
            }

            set
            {
                _Accessor[index, 0] = value.X;
                _Accessor[index, 1] = value.Y;
            }
        }

        public IEnumerator<Vector2> GetEnumerator() { return new EncodedArrayEnumerator<Vector2>(this); }

        IEnumerator IEnumerable.GetEnumerator() { return new EncodedArrayEnumerator<Vector2>(this); }

        public bool Contains(Vector2 item) { return IndexOf(item) >= 0; }

        public int IndexOf(Vector2 item) { return this._FirstIndexOf(item); }

        public void CopyTo(Vector2[] array, int arrayIndex)
        {
            Guard.NotNull(array, nameof(array));
            _Accessor.CopyTo(MemoryMarshal.Cast<Vector2, float>(array.Slice(arrayIndex)), 2);
        }

        public void Fill(IEnumerable<Vector2> values, int dstStart = 0)
        {
            Guard.NotNull(values, nameof(values));
            values._CopyTo(this, dstStart);
        }

        public void FillSpan(ReadOnlySpan<Vector2> values, int dstStart = 0)
        {
            _Accessor.Fill(MemoryMarshal.Cast<Vector2, float>(values), 2, dstStart);
        }

        public void ForEachSub<TAction>(TAction handler = default) where TAction : struct, IForEachSubAction
        {
            _Accessor._ForEachSub(2, handler);
        }

        public void ForEach<TAction>(TAction handler = default) where TAction : struct, IForEachAction<Vector2>
        {
            _Accessor._ForEach<Vector2, TAction>(handler);
        }

        void IList<Vector2>.Insert(int index, Vector2 item) { throw new NotSupportedException(); }

        void IList<Vector2>.RemoveAt(int index) { throw new NotSupportedException(); }

        void ICollection<Vector2>.Add(Vector2 item) { throw new NotSupportedException(); }

        void ICollection<Vector2>.Clear() { throw new NotSupportedException(); }

        bool ICollection<Vector2>.Remove(Vector2 item) { throw new NotSupportedException(); }

        #endregion
    }

    /// <summary>
    /// Wraps an encoded <see cref="BYTES"/> and exposes it as an <see cref="IList{Vector3}"/>.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("Vector3[{Count}]")]
    public readonly struct Vector3Array : IAccessorList<Vector3>
    {
        #region constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Vector3Array"/> struct.
        /// </summary>
        /// <param name="source">The array range to wrap.</param>
        /// <param name="byteStride">
        /// The byte stride between elements.
        /// If the value is zero, the size of the item is used instead.
        /// </param>
        /// <param name="encoding">A value of <see cref="ENCODING"/>.</param>
        /// <param name="normalized">True if values are normalized.</param>
        public Vector3Array(BYTES source, int byteStride = 0, ENCODING encoding = ENCODING.FLOAT, Boolean normalized = false)
            : this(source, 0, int.MaxValue, byteStride, encoding, normalized) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Vector3Array"/> struct.
        /// </summary>
        /// <param name="source">The array range to wrap.</param>
        /// <param name="byteOffset">The zero-based index of the first <see cref="Byte"/> in <paramref name="source"/>.</param>
        /// <param name="itemsCount">The number of <see cref="Vector3"/> items in <paramref name="source"/>.</param>
        /// <param name="byteStride">
        /// The byte stride between elements.
        /// If the value is zero, the size of the item is used instead.
        /// </param>
        /// <param name="encoding">A value of <see cref="ENCODING"/>.</param>
        /// <param name="normalized">True if values are normalized.</param>
        public Vector3Array(BYTES source, int byteOffset, int itemsCount, int byteStride, ENCODING encoding = ENCODING.FLOAT, Boolean normalized = false)
        {
            _Accessor = new FloatingAccessor(source, byteOffset, itemsCount, byteStride, 3, encoding, normalized);
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly FloatingAccessor _Accessor;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
        private Vector3[] _DebugItems => this.ToArray();

        #endregion

        #region API

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public int Count => _Accessor.Count;

        bool ICollection<Vector3>.IsReadOnly => false;

        public Vector3 this[int index]
        {
            get
            {
                return new Vector3(_Accessor[index, 0], _Accessor[index, 1], _Accessor[index, 2]);
            }

            set
            {
                _Accessor[index, 0] = value.X;
                _Accessor[index, 1] = value.Y;
                _Accessor[index, 2] = value.Z;
            }
        }

        public IEnumerator<Vector3> GetEnumerator() { return new EncodedArrayEnumerator<Vector3>(this); }

        IEnumerator IEnumerable.GetEnumerator() { return new EncodedArrayEnumerator<Vector3>(this); }

        public bool Contains(Vector3 item) { return IndexOf(item) >= 0; }

        public int IndexOf(Vector3 item) { return this._FirstIndexOf(item); }

        public void CopyTo(Vector3[] array, int arrayIndex)
        {
            Guard.NotNull(array, nameof(array));
            _Accessor.CopyTo(MemoryMarshal.Cast<Vector3, float>(array.Slice(arrayIndex)), 3);
        }

        public void Fill(IEnumerable<Vector3> values, int dstStart = 0)
        {
            Guard.NotNull(values, nameof(values));
            values._CopyTo(this, dstStart);
        }

        public void FillSpan(ReadOnlySpan<Vector3> values, int dstStart = 0)
        {
            _Accessor.Fill(MemoryMarshal.Cast<Vector3, float>(values), 3, dstStart);
        }
        
        public void ForEachSub<TAction>(TAction handler = default) where TAction : struct, IForEachSubAction
        {
            _Accessor._ForEachSub(3, handler);
        }

        public void ForEach<TAction>(TAction handler = default) where TAction : struct, IForEachAction<Vector3>
        {
            _Accessor._ForEach<Vector3, TAction>(handler);
        }

        void IList<Vector3>.Insert(int index, Vector3 item) { throw new NotSupportedException(); }

        void IList<Vector3>.RemoveAt(int index) { throw new NotSupportedException(); }

        void ICollection<Vector3>.Add(Vector3 item) { throw new NotSupportedException(); }

        void ICollection<Vector3>.Clear() { throw new NotSupportedException(); }

        bool ICollection<Vector3>.Remove(Vector3 item) { throw new NotSupportedException(); }

        #endregion
    }

    /// <summary>
    /// Wraps an encoded <see cref="BYTES"/> and exposes it as an <see cref="IList{Vector4}"/>.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("Vector4[{Count}]")]
    public readonly struct Vector4Array : IAccessorList<Vector4>
    {
        #region constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Vector4Array"/> struct.
        /// </summary>
        /// <param name="source">The array range to wrap.</param>
        /// <param name="byteStride">
        /// The byte stride between elements.
        /// If the value is zero, the size of the item is used instead.
        /// </param>
        /// <param name="encoding">A value of <see cref="ENCODING"/>.</param>
        /// <param name="normalized">True if values are normalized.</param>
        public Vector4Array(BYTES source, int byteStride = 0, ENCODING encoding = ENCODING.FLOAT, Boolean normalized = false)
            : this(source, 0, int.MaxValue, byteStride, encoding, normalized) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Vector4Array"/> struct.
        /// </summary>
        /// <param name="source">The array range to wrap.</param>
        /// <param name="byteOffset">The zero-based index of the first <see cref="Byte"/> in <paramref name="source"/>.</param>
        /// <param name="itemsCount">The number of <see cref="Vector3"/> items in <paramref name="source"/>.</param>
        /// <param name="byteStride">
        /// The byte stride between elements.
        /// If the value is zero, the size of the item is used instead.
        /// </param>
        /// <param name="encoding">A value of <see cref="ENCODING"/>.</param>
        /// <param name="normalized">True if values are normalized.</param>
        public Vector4Array(BYTES source, int byteOffset, int itemsCount, int byteStride, ENCODING encoding = ENCODING.FLOAT, Boolean normalized = false)
        {
            _Accessor = new FloatingAccessor(source, byteOffset, itemsCount, byteStride, 4, encoding, normalized);
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly FloatingAccessor _Accessor;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
        private Vector4[] _DebugItems => this.ToArray();

        #endregion

        #region API

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public int Count => _Accessor.Count;

        bool ICollection<Vector4>.IsReadOnly => false;

        public Vector4 this[int index]
        {
            get
            {
                return new Vector4(_Accessor[index, 0], _Accessor[index, 1], _Accessor[index, 2], _Accessor[index, 3]);
            }

            set
            {
                _Accessor[index, 0] = value.X;
                _Accessor[index, 1] = value.Y;
                _Accessor[index, 2] = value.Z;
                _Accessor[index, 3] = value.W;
            }
        }

        public IEnumerator<Vector4> GetEnumerator() { return new EncodedArrayEnumerator<Vector4>(this); }

        IEnumerator IEnumerable.GetEnumerator() { return new EncodedArrayEnumerator<Vector4>(this); }

        public bool Contains(Vector4 item) { return IndexOf(item) >= 0; }

        public int IndexOf(Vector4 item) { return this._FirstIndexOf(item); }

        public void CopyTo(Vector4[] array, int arrayIndex)
        {
            Guard.NotNull(array, nameof(array));
            _Accessor.CopyTo(MemoryMarshal.Cast<Vector4, float>(array.Slice(arrayIndex)), 3);
        }

        public void Fill(IEnumerable<Vector4> values, int dstStart = 0)
        {
            Guard.NotNull(values, nameof(values));
            values._CopyTo(this, dstStart);
        }

        public void FillSpan(ReadOnlySpan<Vector4> values, int dstStart = 0)
        {
            _Accessor.Fill(MemoryMarshal.Cast<Vector4, float>(values), 4, dstStart);
        }

        public void ForEachSub<TAction>(TAction handler = default) where TAction : struct, IForEachSubAction
        {
            _Accessor._ForEachSub(4, handler);
        }

        public void ForEach<TAction>(TAction handler = default) where TAction : struct, IForEachAction<Vector4>
        {
            _Accessor._ForEach<Vector4, TAction>(handler);
        }

        void IList<Vector4>.Insert(int index, Vector4 item) { throw new NotSupportedException(); }

        void IList<Vector4>.RemoveAt(int index) { throw new NotSupportedException(); }

        void ICollection<Vector4>.Add(Vector4 item) { throw new NotSupportedException(); }

        void ICollection<Vector4>.Clear() { throw new NotSupportedException(); }

        bool ICollection<Vector4>.Remove(Vector4 item) { throw new NotSupportedException(); }

        #endregion
    }

    /// <summary>
    /// Wraps an encoded <see cref="BYTES"/> and exposes it as an <see cref="IList{Quaternion}"/>.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("Quaternion[{Count}]")]
    public readonly struct QuaternionArray : IAccessorList<Quaternion>
    {
        #region constructors

        public QuaternionArray(BYTES source, int byteStride = 0, ENCODING encoding = ENCODING.FLOAT, Boolean normalized = false)
            : this(source, 0, int.MaxValue, byteStride, encoding, normalized) { }

        public QuaternionArray(BYTES source, int byteOffset, int itemsCount, int byteStride, ENCODING encoding, Boolean normalized)
        {
            _Accessor = new FloatingAccessor(source, byteOffset, itemsCount, byteStride, 4, encoding, normalized);
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly FloatingAccessor _Accessor;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
        private Quaternion[] _DebugItems => this.ToArray();

        #endregion

        #region API

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public int Count => _Accessor.Count;

        bool ICollection<Quaternion>.IsReadOnly => false;

        public Quaternion this[int index]
        {
            get
            {
                return new Quaternion(_Accessor[index, 0], _Accessor[index, 1], _Accessor[index, 2], _Accessor[index, 3]);
            }

            set
            {
                _Accessor[index, 0] = value.X;
                _Accessor[index, 1] = value.Y;
                _Accessor[index, 2] = value.Z;
                _Accessor[index, 3] = value.W;
            }
        }

        public IEnumerator<Quaternion> GetEnumerator() { return new EncodedArrayEnumerator<Quaternion>(this); }

        IEnumerator IEnumerable.GetEnumerator() { return new EncodedArrayEnumerator<Quaternion>(this); }

        public bool Contains(Quaternion item) { return IndexOf(item) >= 0; }

        public int IndexOf(Quaternion item) { return this._FirstIndexOf(item); }

        public void CopyTo(Quaternion[] array, int arrayIndex)
        {
            Guard.NotNull(array, nameof(array));
            _Accessor.CopyTo(MemoryMarshal.Cast<Quaternion, float>(array.Slice(arrayIndex)), 4);
        }

        public void Fill(IEnumerable<Quaternion> values, int dstStart = 0)
        {
            Guard.NotNull(values, nameof(values));
            values._CopyTo(this, dstStart);
        }

        public void FillSpan(ReadOnlySpan<Quaternion> values, int dstStart = 0)
        {
            _Accessor.Fill(MemoryMarshal.Cast<Quaternion, float>(values), 3, dstStart);
        }

        public void ForEachSub<TAction>(TAction handler = default) where TAction : struct, IForEachSubAction {
            _Accessor._ForEachSub(4, handler);
        }

        public void ForEach<TAction>(TAction handler = default) where TAction : struct, IForEachAction<Quaternion> {
            _Accessor._ForEach<Quaternion, TAction>(handler);
        }

        void IList<Quaternion>.Insert(int index, Quaternion item) { throw new NotSupportedException(); }

        void IList<Quaternion>.RemoveAt(int index) { throw new NotSupportedException(); }

        void ICollection<Quaternion>.Add(Quaternion item) { throw new NotSupportedException(); }

        void ICollection<Quaternion>.Clear() { throw new NotSupportedException(); }

        bool ICollection<Quaternion>.Remove(Quaternion item) { throw new NotSupportedException(); }

        #endregion
    }

    /// <summary>
    /// Wraps an encoded <see cref="BYTES"/> and exposes it as an <see cref="IList{Matrix3x2}"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="Vector"/> namespace doesn't support a 2x2 matrix, so the array is<br/>
    /// decoded as a Matrix2x2 matrix internally, but exposed as a <see cref="Matrix3x2"/>.
    /// </remarks>
    [System.Diagnostics.DebuggerDisplay("Matrix2x2[{Count}]")]
    public readonly struct Matrix2x2Array : IList<Matrix3x2>, IReadOnlyList<Matrix3x2>
    {
        #region constructors

        public Matrix2x2Array(BYTES source, int byteStride = 0, ENCODING encoding = ENCODING.FLOAT, Boolean normalized = false)
            : this(source, 0, int.MaxValue, byteStride, encoding, normalized) { }

        public Matrix2x2Array(BYTES source, int byteOffset, int itemsCount, int byteStride, ENCODING encoding, Boolean normalized)
        {
            _Accessor = new FloatingAccessor(source, byteOffset, itemsCount, byteStride, 4, encoding, normalized);
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly FloatingAccessor _Accessor;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
        private Matrix3x2[] _DebugItems => this.ToArray();

        #endregion

        #region API

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public int Count => _Accessor.Count;

        bool ICollection<Matrix3x2>.IsReadOnly => false;

        public Matrix3x2 this[int index]
        {
            get
            {
                return new Matrix3x2
                    (
                    _Accessor[index, 0], _Accessor[index, 1],
                    _Accessor[index, 2], _Accessor[index, 3],
                    0, 0
                    );
            }

            set
            {
                _Accessor[index, 0] = value.M11;
                _Accessor[index, 1] = value.M12;
                _Accessor[index, 2] = value.M21;
                _Accessor[index, 3] = value.M22;
            }
        }

        public IEnumerator<Matrix3x2> GetEnumerator() { return new EncodedArrayEnumerator<Matrix3x2>(this); }

        IEnumerator IEnumerable.GetEnumerator() { return new EncodedArrayEnumerator<Matrix3x2>(this); }

        public bool Contains(Matrix3x2 item) { return IndexOf(item) >= 0; }

        public int IndexOf(Matrix3x2 item) { return this._FirstIndexOf(item); }

        public void CopyTo(Matrix3x2[] array, int arrayIndex)
        {
            Guard.NotNull(array, nameof(array));
            this._CopyTo(array, arrayIndex);
        }

        public void Fill(IEnumerable<Matrix3x2> values, int dstStart = 0)
        {
            Guard.NotNull(values, nameof(values));
            values._CopyTo(this, dstStart);
        }

        void IList<Matrix3x2>.Insert(int index, Matrix3x2 item) { throw new NotSupportedException(); }

        void IList<Matrix3x2>.RemoveAt(int index) { throw new NotSupportedException(); }

        void ICollection<Matrix3x2>.Add(Matrix3x2 item) { throw new NotSupportedException(); }

        void ICollection<Matrix3x2>.Clear() { throw new NotSupportedException(); }

        bool ICollection<Matrix3x2>.Remove(Matrix3x2 item) { throw new NotSupportedException(); }

        #endregion
    }

    /// <summary>
    /// Wraps an encoded <see cref="BYTES"/> and exposes it as an <see cref="IList{Matrix3x2}"/>.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("Matrix3x2[{Count}]")]
    public readonly struct Matrix3x2Array : IList<Matrix3x2>, IReadOnlyList<Matrix3x2>
    {
        #region constructors

        public Matrix3x2Array(BYTES source, int byteStride = 0, ENCODING encoding = ENCODING.FLOAT, Boolean normalized = false)
            : this(source, 0, int.MaxValue, byteStride, encoding, normalized) { }

        public Matrix3x2Array(BYTES source, int byteOffset, int itemsCount, int byteStride, ENCODING encoding, Boolean normalized)
        {
            _Accessor = new FloatingAccessor(source, byteOffset, itemsCount, byteStride, 6, encoding, normalized);
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly FloatingAccessor _Accessor;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
        private Matrix3x2[] _DebugItems => this.ToArray();

        #endregion

        #region API

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public int Count => _Accessor.Count;

        bool ICollection<Matrix3x2>.IsReadOnly => false;

        public Matrix3x2 this[int index]
        {
            get
            {
                return new Matrix3x2
                    (
                    _Accessor[index, 0], _Accessor[index, 1],
                    _Accessor[index, 2], _Accessor[index, 3],
                    _Accessor[index, 4], _Accessor[index, 5]
                    );
            }

            set
            {
                _Accessor[index, 0] = value.M11;
                _Accessor[index, 1] = value.M12;
                _Accessor[index, 2] = value.M21;
                _Accessor[index, 3] = value.M22;
                _Accessor[index, 4] = value.M31;
                _Accessor[index, 5] = value.M32;
            }
        }

        public IEnumerator<Matrix3x2> GetEnumerator() { return new EncodedArrayEnumerator<Matrix3x2>(this); }

        IEnumerator IEnumerable.GetEnumerator() { return new EncodedArrayEnumerator<Matrix3x2>(this); }

        public bool Contains(Matrix3x2 item) { return IndexOf(item) >= 0; }

        public int IndexOf(Matrix3x2 item) { return this._FirstIndexOf(item); }

        public void CopyTo(Matrix3x2[] array, int arrayIndex)
        {
            Guard.NotNull(array, nameof(array));
            this._CopyTo(array, arrayIndex);
        }

        public void Fill(IEnumerable<Matrix3x2> values, int dstStart = 0)
        {
            Guard.NotNull(values, nameof(values));
            values._CopyTo(this, dstStart);
        }

        void IList<Matrix3x2>.Insert(int index, Matrix3x2 item) { throw new NotSupportedException(); }

        void IList<Matrix3x2>.RemoveAt(int index) { throw new NotSupportedException(); }

        void ICollection<Matrix3x2>.Add(Matrix3x2 item) { throw new NotSupportedException(); }

        void ICollection<Matrix3x2>.Clear() { throw new NotSupportedException(); }

        bool ICollection<Matrix3x2>.Remove(Matrix3x2 item) { throw new NotSupportedException(); }

        #endregion
    }

    /// <summary>
    /// Wraps an encoded <see cref="BYTES"/> and exposes it as an <see cref="IList{Matrix4x4}"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="Vector"/> namespace doesn't support a 3x3 matrix, so the array is<br/>
    /// decoded as a Matrix3x3 matrix internally, but exposed as a <see cref="Matrix4x4"/>.
    /// </remarks>
    [System.Diagnostics.DebuggerDisplay("Matrix3x3[{Count}]")]
    public readonly struct Matrix3x3Array : IList<Matrix4x4>, IReadOnlyList<Matrix4x4>
    {
        #region constructors

        public Matrix3x3Array(BYTES source, int byteStride = 0, ENCODING encoding = ENCODING.FLOAT, Boolean normalized = false)
            : this(source, 0, int.MaxValue, byteStride, encoding, normalized) { }

        public Matrix3x3Array(BYTES source, int byteOffset, int itemsCount, int byteStride, ENCODING encoding, Boolean normalized)
        {
            _Accessor = new FloatingAccessor(source, byteOffset, itemsCount, byteStride, 9, encoding, normalized);
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly FloatingAccessor _Accessor;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
        private Matrix4x4[] _DebugItems => this.ToArray();

        #endregion

        #region API

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public int Count => _Accessor.Count;

        bool ICollection<Matrix4x4>.IsReadOnly => false;

        public Matrix4x4 this[int index]
        {
            get
            {
                return new Matrix4x4
                    (
                    _Accessor[index, 0], _Accessor[index, 1], _Accessor[index, 2], 0,
                    _Accessor[index, 3], _Accessor[index, 4], _Accessor[index, 5], 0,
                    _Accessor[index, 6], _Accessor[index, 7], _Accessor[index, 8], 0,
                    0, 0, 0, 1
                    );
            }

            set
            {
                _Accessor[index, 0] = value.M11;
                _Accessor[index, 1] = value.M12;
                _Accessor[index, 2] = value.M13;
                _Accessor[index, 3] = value.M21;
                _Accessor[index, 4] = value.M22;
                _Accessor[index, 5] = value.M23;
                _Accessor[index, 6] = value.M31;
                _Accessor[index, 7] = value.M32;
                _Accessor[index, 8] = value.M33;
            }
        }

        public IEnumerator<Matrix4x4> GetEnumerator() { return new EncodedArrayEnumerator<Matrix4x4>(this); }

        IEnumerator IEnumerable.GetEnumerator() { return new EncodedArrayEnumerator<Matrix4x4>(this); }

        public bool Contains(Matrix4x4 item) { return IndexOf(item) >= 0; }

        public int IndexOf(Matrix4x4 item) { return this._FirstIndexOf(item); }

        public void CopyTo(Matrix4x4[] array, int arrayIndex)
        {
            Guard.NotNull(array, nameof(array));
            this._CopyTo(array, arrayIndex);
        }

        public void Fill(IEnumerable<Matrix4x4> values, int dstStart = 0)
        {
            Guard.NotNull(values, nameof(values));
            values._CopyTo(this, dstStart);
        }

        void IList<Matrix4x4>.Insert(int index, Matrix4x4 item) { throw new NotSupportedException(); }

        void IList<Matrix4x4>.RemoveAt(int index) { throw new NotSupportedException(); }

        void ICollection<Matrix4x4>.Add(Matrix4x4 item) { throw new NotSupportedException(); }

        void ICollection<Matrix4x4>.Clear() { throw new NotSupportedException(); }

        bool ICollection<Matrix4x4>.Remove(Matrix4x4 item) { throw new NotSupportedException(); }

        #endregion
    }

    /// <summary>
    /// Wraps an encoded <see cref="BYTES"/> and exposes it as an <see cref="IList{Matrix4x4}"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="Vector"/> namespace doesn't support a 4x3 matrix, so the array is<br/>
    /// decoded as a Matrix4x3 matrix internally, but exposed as a <see cref="Matrix4x4"/>.
    /// </remarks>
    [System.Diagnostics.DebuggerDisplay("Matrix4x3[{Count}]")]
    public readonly struct Matrix4x3Array : IList<Matrix4x4>, IReadOnlyList<Matrix4x4>
    {
        #region constructors

        public Matrix4x3Array(BYTES source, int byteStride = 0, ENCODING encoding = ENCODING.FLOAT, Boolean normalized = false)
            : this(source, 0, int.MaxValue, byteStride, encoding, normalized) { }

        public Matrix4x3Array(BYTES source, int byteOffset, int itemsCount, int byteStride, ENCODING encoding, Boolean normalized)
        {
            _Accessor = new FloatingAccessor(source, byteOffset, itemsCount, byteStride, 12, encoding, normalized);
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly FloatingAccessor _Accessor;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
        private Matrix4x4[] _DebugItems => this.ToArray();

        #endregion

        #region API

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public int Count => _Accessor.Count;

        bool ICollection<Matrix4x4>.IsReadOnly => false;

        public Matrix4x4 this[int index]
        {
            get
            {
                return new Matrix4x4
                    (
                    _Accessor[index, 0], _Accessor[index, 1], _Accessor[index, 2], 0,
                    _Accessor[index, 3], _Accessor[index, 4], _Accessor[index, 5], 0,
                    _Accessor[index, 6], _Accessor[index, 7], _Accessor[index, 8], 0,
                    _Accessor[index, 9], _Accessor[index, 10], _Accessor[index, 11], 1
                    );
            }

            set
            {
                _Accessor[index, 0] = value.M11;
                _Accessor[index, 1] = value.M12;
                _Accessor[index, 2] = value.M13;
                _Accessor[index, 3] = value.M21;
                _Accessor[index, 4] = value.M22;
                _Accessor[index, 5] = value.M23;
                _Accessor[index, 6] = value.M31;
                _Accessor[index, 7] = value.M32;
                _Accessor[index, 8] = value.M33;
                _Accessor[index, 9] = value.M41;
                _Accessor[index, 10] = value.M42;
                _Accessor[index, 11] = value.M43;
            }
        }

        public IEnumerator<Matrix4x4> GetEnumerator() { return new EncodedArrayEnumerator<Matrix4x4>(this); }

        IEnumerator IEnumerable.GetEnumerator() { return new EncodedArrayEnumerator<Matrix4x4>(this); }

        public bool Contains(Matrix4x4 item) { return IndexOf(item) >= 0; }

        public int IndexOf(Matrix4x4 item) { return this._FirstIndexOf(item); }

        public void CopyTo(Matrix4x4[] array, int arrayIndex)
        {
            Guard.NotNull(array, nameof(array));
            this._CopyTo(array, arrayIndex);
        }

        public void Fill(IEnumerable<Matrix4x4> values, int dstStart = 0)
        {
            Guard.NotNull(values, nameof(values));
            values._CopyTo(this, dstStart);
        }

        void IList<Matrix4x4>.Insert(int index, Matrix4x4 item) { throw new NotSupportedException(); }

        void IList<Matrix4x4>.RemoveAt(int index) { throw new NotSupportedException(); }

        void ICollection<Matrix4x4>.Add(Matrix4x4 item) { throw new NotSupportedException(); }

        void ICollection<Matrix4x4>.Clear() { throw new NotSupportedException(); }

        bool ICollection<Matrix4x4>.Remove(Matrix4x4 item) { throw new NotSupportedException(); }

        #endregion
    }

    /// <summary>
    /// Wraps an encoded <see cref="BYTES"/> and exposes it as an <see cref="IList{Matrix4x4}"/>.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("Matrix4x4[{Count}]")]
    public readonly struct Matrix4x4Array : IList<Matrix4x4>, IReadOnlyList<Matrix4x4>
    {
        #region constructors

        public Matrix4x4Array(BYTES source, int byteStride = 0, ENCODING encoding = ENCODING.FLOAT, Boolean normalized = false)
            : this(source, 0, int.MaxValue, byteStride, encoding, normalized) { }

        public Matrix4x4Array(BYTES source, int byteOffset, int itemsCount, int byteStride, ENCODING encoding, Boolean normalized)
        {
            _Accessor = new FloatingAccessor(source, byteOffset, itemsCount, byteStride, 16, encoding, normalized);
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly FloatingAccessor _Accessor;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
        private Matrix4x4[] _DebugItems => this.ToArray();

        #endregion

        #region API

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public int Count => _Accessor.Count;

        bool ICollection<Matrix4x4>.IsReadOnly => false;

        public Matrix4x4 this[int index]
        {
            get
            {
                return new Matrix4x4
                    (
                    _Accessor[index, 0], _Accessor[index, 1], _Accessor[index, 2], _Accessor[index, 3],
                    _Accessor[index, 4], _Accessor[index, 5], _Accessor[index, 6], _Accessor[index, 7],
                    _Accessor[index, 8], _Accessor[index, 9], _Accessor[index, 10], _Accessor[index, 11],
                    _Accessor[index, 12], _Accessor[index, 13], _Accessor[index, 14], _Accessor[index, 15]
                    );
            }

            set
            {
                _Accessor[index, 0] = value.M11;
                _Accessor[index, 1] = value.M12;
                _Accessor[index, 2] = value.M13;
                _Accessor[index, 3] = value.M14;
                _Accessor[index, 4] = value.M21;
                _Accessor[index, 5] = value.M22;
                _Accessor[index, 6] = value.M23;
                _Accessor[index, 7] = value.M24;
                _Accessor[index, 8] = value.M31;
                _Accessor[index, 9] = value.M32;
                _Accessor[index, 10] = value.M33;
                _Accessor[index, 11] = value.M34;
                _Accessor[index, 12] = value.M41;
                _Accessor[index, 13] = value.M42;
                _Accessor[index, 14] = value.M43;
                _Accessor[index, 15] = value.M44;
            }
        }

        public IEnumerator<Matrix4x4> GetEnumerator() { return new EncodedArrayEnumerator<Matrix4x4>(this); }

        IEnumerator IEnumerable.GetEnumerator() { return new EncodedArrayEnumerator<Matrix4x4>(this); }

        public bool Contains(Matrix4x4 item) { return IndexOf(item) >= 0; }

        public int IndexOf(Matrix4x4 item) { return this._FirstIndexOf(item); }

        public void CopyTo(Matrix4x4[] array, int arrayIndex)
        {
            Guard.NotNull(array, nameof(array));
            this._CopyTo(array, arrayIndex);
        }

        public void Fill(IEnumerable<Matrix4x4> values, int dstStart = 0)
        {
            Guard.NotNull(values, nameof(values));
            values._CopyTo(this, dstStart);
        }

        void IList<Matrix4x4>.Insert(int index, Matrix4x4 item) { throw new NotSupportedException(); }

        void IList<Matrix4x4>.RemoveAt(int index) { throw new NotSupportedException(); }

        void ICollection<Matrix4x4>.Add(Matrix4x4 item) { throw new NotSupportedException(); }

        void ICollection<Matrix4x4>.Clear() { throw new NotSupportedException(); }

        bool ICollection<Matrix4x4>.Remove(Matrix4x4 item) { throw new NotSupportedException(); }

        #endregion
    }

    /// <summary>
    /// Wraps an encoded <see cref="BYTES"/> and exposes it as an IList{Single[]}/>.
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("Float[][{Count}]")]
    public readonly struct MultiArray : IList<Single[]>, IReadOnlyList<Single[]>
    {
        #region constructors
        public MultiArray(BYTES source, int byteOffset, int itemsCount, int byteStride, int dimensions, ENCODING encoding, Boolean normalized)
        {
            _Dimensions = dimensions;
            _Accessor = new FloatingAccessor(source, byteOffset, itemsCount, byteStride, dimensions, encoding, normalized);
        }

        #endregion

        #region data

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly int _Dimensions;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        private readonly FloatingAccessor _Accessor;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
        private Single[][] _DebugItems => this.ToArray();

        #endregion

        #region API

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public int Count => _Accessor.Count;

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public int Dimensions => _Dimensions;

        bool ICollection<Single[]>.IsReadOnly => false;

#pragma warning disable CA1819 // Properties should not return arrays
        public Single[] this[int index]
#pragma warning restore CA1819 // Properties should not return arrays
        {
            get
            {
                var val = new Single[_Dimensions];
                CopyItemTo(index, val);
                return val;
            }

            set
            {
                Guard.NotNull(value, nameof(value));
                Guard.IsTrue(value.Length == _Dimensions, nameof(value));

                for (int i = 0; i < _Dimensions; ++i)
                {
                    _Accessor[index, i] = value[i];
                }
            }
        }

        public void CopyItemTo(int index, Single[] dstItem)
        {
            Guard.NotNull(dstItem, nameof(dstItem));

            var count = _Dimensions;

            for (int i = 0; i < count; ++i) dstItem[i] = _Accessor[index, i];
        }

        public void ForEachSub<TAction>(int subCount, TAction handler = default, ParallelType parallelType = ParallelType.ROW_AND_SUB) where TAction : struct, IForEachSubAction
        {
            _Accessor._ForEachSub(subCount, handler, parallelType);
        }

        public IEnumerator<Single[]> GetEnumerator() { return new EncodedArrayEnumerator<Single[]>(this); }

        IEnumerator IEnumerable.GetEnumerator() { return new EncodedArrayEnumerator<Single[]>(this); }

        public bool Contains(Single[] item) { return IndexOf(item) >= 0; }

        public int IndexOf(Single[] item) { return this._FirstIndexOf(item); }

        public void CopyTo(Single[][] array, int arrayIndex)
        {
            Guard.NotNull(array, nameof(array));
            this._CopyTo(array, arrayIndex);
        }

        public void Fill(IEnumerable<Single[]> values, int dstStart = 0)
        {
            Guard.NotNull(values, nameof(values));
            values._CopyTo(this, dstStart);
        }

        void IList<Single[]>.Insert(int index, Single[] item) { throw new NotSupportedException(); }

        void IList<Single[]>.RemoveAt(int index) { throw new NotSupportedException(); }

        void ICollection<Single[]>.Add(Single[] item) { throw new NotSupportedException(); }

        void ICollection<Single[]>.Clear() { throw new NotSupportedException(); }

        bool ICollection<Single[]>.Remove(Single[] item) { throw new NotSupportedException(); }

        #endregion
    }
}
