using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;

namespace TianYanShop.MapGeneration.Core
{
    /// <summary>
    /// Alea PRNG 随机数管理器
    /// 基于 Johannes Baagøe 的 Alea 算法移植
    /// </summary>
    public class RandomManager
    {
        private double[] _state = new double[4];
        private double _s0, _s1, _s2, _c;
        private bool _initialized = false;

        public RandomManager()
        {
            Seed(Guid.NewGuid().ToString());
        }

        public RandomManager(string seed)
        {
            Seed(seed);
        }

        public void Seed(string seed)
        {
            if (string.IsNullOrEmpty(seed))
            {
                seed = Guid.NewGuid().ToString();
            }

            _s0 = 0.0;
            _s1 = 0.0;
            _s2 = 0.0;
            _c = 1.0;

            var mash = new Mash();
            mash.Init(seed);

            for (int i = 0; i < _state.Length; i++)
            {
                _state[i] = mash.Next();
            }

            _initialized = true;
        }

        public float NextFloat()
        {
            if (!_initialized) Seed(Guid.NewGuid().ToString());

            var t = 2091639.0 * _s0 + _c * 2.3283064365386963e-10;
            _c = (float)global::System.Math.Floor(t);
            _s0 = _s1;
            _s1 = _s2;
            _s2 = t - _c;
            return (float)_s2;
        }

        public int NextInt()
        {
            return (int)(NextFloat() * uint.MaxValue);
        }

        public int NextInt(int max)
        {
            return (int)(NextFloat() * max);
        }

        public int NextInt(int min, int max)
        {
            if (max <= min) return min;
            return min + NextInt(max - min);
        }

        public bool NextBool()
        {
            return NextFloat() >= 0.5f;
        }

        public bool NextBool(float probability)
        {
            if (probability >= 1.0f) return true;
            if (probability <= 0.0f) return false;
            return NextFloat() < probability;
        }

        public float NextGaussian(float mean = 0f, float deviation = 1f)
        {
            float u1 = 1.0f - NextFloat();
            float u2 = 1.0f - NextFloat();
            float randStdNormal = (float)(global::System.Math.Sqrt(-2.0 * global::System.Math.Log(u1)) * global::System.Math.Sin(2.0 * global::System.Math.PI * u2));
            return mean + deviation * randStdNormal;
        }

        public int NextChoice(object[] weights)
        {
            if (weights == null || weights.Length == 0) return -1;

            double total = 0;
            foreach (var w in weights)
            {
                total += Convert.ToDouble(w);
            }

            double random = NextFloat() * total;
            double sum = 0;

            for (int i = 0; i < weights.Length; i++)
            {
                sum += Convert.ToDouble(weights[i]);
                if (sum >= random) return i;
            }

            return weights.Length - 1;
        }

        public int NextBiased(int min, int max, float exponent)
        {
            if (max <= min) return min;
            float normalized = NextFloat();
            float biased = (float)global::System.Math.Pow(normalized, exponent);
            return min + (int)(biased * (max - min));
        }

        public float NextRange(float min, float max)
        {
            return min + NextFloat() * (max - min);
        }

        public T NextItem<T>(T[] array)
        {
            if (array == null || array.Length == 0) return default(T);
            return array[NextInt(array.Length)];
        }

        public T NextItem<T>(List<T> list)
        {
            if (list == null || list.Count == 0) return default(T);
            return list[NextInt(list.Count)];
        }

        public void Shuffle<T>(T[] array)
        {
            if (array == null || array.Length <= 1) return;

            for (int i = array.Length - 1; i > 0; i--)
            {
                int j = NextInt(i + 1);
                T temp = array[i];
                array[i] = array[j];
                array[j] = temp;
            }
        }

        public List<T> ShuffleCopy<T>(List<T> list)
        {
            if (list == null || list.Count <= 1) return new List<T>(list);

            var result = new List<T>(list);
            for (int i = result.Count - 1; i > 0; i--)
            {
                int j = NextInt(i + 1);
                T temp = result[i];
                result[i] = result[j];
                result[j] = temp;
            }
            return result;
        }

        private class Mash
        {
            private uint _n;

            public void Init(string seed)
            {
                _n = 0x811c9dc5;
                for (int i = 0; i < seed.Length; i++)
                {
                    _n ^= (uint)seed[i];
                    _n *= 0x01000193;
                }
            }

            public double Next()
            {
                _n ^= _n << 13;
                _n ^= _n >> 17;
                _n ^= _n << 5;
                return (_n + 1) / 4294967296.0;
            }
        }
    }
}
