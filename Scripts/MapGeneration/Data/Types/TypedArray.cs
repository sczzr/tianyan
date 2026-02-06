using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TianYanShop.MapGeneration.Data.Types
{
    /// <summary>
    /// 类型化数组封装，模拟 JavaScript TypedArray 行为
    /// </summary>
    public class TypedArray<T> : IEnumerable<T>, IDisposable where T : struct
    {
        private T[] _array;
        private bool _disposed = false;

        public int Length => _array.Length;

        public T this[int index]
        {
            get => _array[index];
            set => _array[index] = value;
        }

        public TypedArray(int length)
        {
            _array = new T[length];
        }

        public TypedArray(T[] array)
        {
            _array = array;
        }

        public TypedArray(IEnumerable<T> collection)
        {
            _array = collection.ToArray();
        }

        public static TypedArray<byte> CreateUint8(int length) => new TypedArray<byte>(length);
        public static TypedArray<ushort> CreateUint16(int length) => new TypedArray<ushort>(length);
        public static TypedArray<uint> CreateUint32(int length) => new TypedArray<uint>(length);
        public static TypedArray<int> CreateInt32(int length) => new TypedArray<int>(length);
        public static TypedArray<float> CreateFloat32(int length) => new TypedArray<float>(length);

        public T[] ToArray() => (T[])_array.Clone();
        public List<T> ToList() => new List<T>(_array);

        public void Fill(T value)
        {
            for (int i = 0; i < _array.Length; i++)
            {
                _array[i] = value;
            }
        }

        public void Set(T[] array, int offset = 0)
        {
            Array.Copy(array, 0, _array, offset, global::System.Math.Min(array.Length, _array.Length - offset));
        }

        public TypedArray<T> SubArray(int start, int length)
        {
            T[] sub = new T[length];
            Array.Copy(_array, start, sub, 0, length);
            return new TypedArray<T>(sub);
        }

        public int FindIndex(Predicate<T> match)
        {
            return Array.FindIndex(_array, match);
        }

        public int FindLastIndex(Predicate<T> match)
        {
            return Array.FindLastIndex(_array, match);
        }

        public T? Find(Predicate<T> match)
        {
            int index = Array.FindIndex(_array, match);
            return index >= 0 ? _array[index] : null;
        }

        public List<int> FindAllIndices(Predicate<T> match)
        {
            var indices = new List<int>();
            for (int i = 0; i < _array.Length; i++)
            {
                if (match(_array[i])) indices.Add(i);
            }
            return indices;
        }

        public void Sort(IComparer<T> comparer = null)
        {
            if (comparer != null)
            {
                Array.Sort(_array, comparer);
            }
            else
            {
                Array.Sort(_array);
            }
        }

        public void Sort(Func<T, T, int> comparison)
        {
            Array.Sort(_array, (x, y) => comparison(x, y));
        }

        public TypedArray<T> Sorted(IComparer<T> comparer = null)
        {
            var copy = new TypedArray<T>((T[])_array.Clone());
            copy.Sort(comparer);
            return copy;
        }

        public int BinarySearch(T value, IComparer<T> comparer = null)
        {
            return Array.BinarySearch(_array, value, comparer);
        }

        public bool Exists(Predicate<T> match) => Array.Exists(_array, match);
        public bool TrueForAll(Predicate<T> match) => Array.TrueForAll(_array, match);

        public void ForEach(Action<T> action)
        {
            foreach (var item in _array)
            {
                action(item);
            }
        }

        public void ForEach(Action<T, int> action)
        {
            for (int i = 0; i < _array.Length; i++)
            {
                action(_array[i], i);
            }
        }

        public U Reduce<U>(U accumulator, Func<U, T, int, U> reducer)
        {
            for (int i = 0; i < _array.Length; i++)
            {
                accumulator = reducer(accumulator, _array[i], i);
            }
            return accumulator;
        }

        public T[] Map(Func<T, T> mapper)
        {
            var result = new T[_array.Length];
            for (int i = 0; i < _array.Length; i++)
            {
                result[i] = mapper(_array[i]);
            }
            return result;
        }

        public T[] Filter(Predicate<T> predicate)
        {
            return Array.FindAll(_array, predicate);
        }

        public IEnumerator<T> GetEnumerator()
        {
            foreach (var item in _array)
            {
                yield return item;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Dispose()
        {
            if (!_disposed)
            {
                _array = Array.Empty<T>();
                _disposed = true;
            }
        }

        public static implicit operator T[](TypedArray<T> typed) => typed._array;
        public static implicit operator TypedArray<T>(T[] array) => new TypedArray<T>(array);
    }

    /// <summary>
    /// Uint8Array 别名
    /// </summary>
    public class Uint8Array : TypedArray<byte>
    {
        public Uint8Array(int length) : base(length) { }
        public Uint8Array(byte[] array) : base(array) { }
        public new byte this[int index]
        {
            get => base[index];
            set => base[index] = (byte)global::System.Math.Clamp((byte)value, (byte)0, (byte)255);
        }
    }

    /// <summary>
    /// Uint16Array 别名
    /// </summary>
    public class Uint16Array : TypedArray<ushort>
    {
        public Uint16Array(int length) : base(length) { }
        public Uint16Array(ushort[] array) : base(array) { }
    }

    /// <summary>
    /// Uint32Array 别名
    /// </summary>
    public class Uint32Array : TypedArray<uint>
    {
        public Uint32Array(int length) : base(length) { }
        public Uint32Array(uint[] array) : base(array) { }
    }

    /// <summary>
    /// Float32Array 别名
    /// </summary>
    public class Float32Array : TypedArray<float>
    {
        public Float32Array(int length) : base(length) { }
        public Float32Array(float[] array) : base(array) { }
    }
}
