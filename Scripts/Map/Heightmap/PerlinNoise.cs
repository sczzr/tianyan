using System;
using Godot;
using System.Runtime.CompilerServices;
using FantasyMapGenerator.Scripts.Utils;

namespace FantasyMapGenerator.Scripts.Map.Heightmap;

/// <summary>
/// Perlin 噪声生成器
/// </summary>
public class PerlinNoise
{
    private readonly int[] _permutation;
    private readonly Vector2[] _gradients;
    private readonly AleaPRNG _prng;
    private const int GridSize = 256;

    public PerlinNoise(AleaPRNG prng)
    {
        _prng = prng;
        _permutation = new int[GridSize * 2];
        _gradients = new Vector2[GridSize];

        InitPermutation();
        InitGradients();
    }

    private void InitPermutation()
    {
        for (int i = 0; i < GridSize; i++)
        {
            _permutation[i] = i;
        }

        for (int i = GridSize - 1; i > 0; i--)
        {
            int j = _prng.NextInt(0, i);  // [0, i)，范围安全
            if (j < 0) j = 0;
            if (j >= i) j = i - 1;
            // 安全交换
            int temp = _permutation[i];
            _permutation[i] = _permutation[j];
            _permutation[j] = temp;
        }

        for (int i = 0; i < GridSize; i++)
        {
            _permutation[GridSize + i] = _permutation[i];
        }
    }

    private void InitGradients()
    {
        for (int i = 0; i < GridSize; i++)
        {
            double angle = _prng.NextDouble() * Math.PI * 2;
            _gradients[i] = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetHash(int x, int y)
    {
        return _permutation[(x & 0xFF) + _permutation[(y & 0xFF)]];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float Lerp(float a, float b, float t)
    {
        return a + t * (b - a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float Fade(float t)
    {
        return t * t * t * (t * (t * 6 - 15) + 10);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float DotGridGradient(int ix, int iy, float x, float y)
    {
        float gradientX = _gradients[GetHash(ix, iy)].X;
        float gradientY = _gradients[GetHash(ix, iy)].Y;

        float dx = x - ix;
        float dy = y - iy;

        return gradientX * dx + gradientY * dy;
    }

    public float GetNoise(float x, float y, float scale = 1f)
    {
        x *= scale;
        y *= scale;

        int x0 = (int)Math.Floor(x);
        int y0 = (int)Math.Floor(y);
        int x1 = x0 + 1;
        int y1 = y0 + 1;

        float sx = x - x0;
        float sy = y - y0;

        sx = Fade(sx);
        sy = Fade(sy);

        float n00 = DotGridGradient(x0, y0, x, y);
        float n10 = DotGridGradient(x1, y0, x, y);
        float n01 = DotGridGradient(x0, y1, x, y);
        float n11 = DotGridGradient(x1, y1, x, y);

        float ix0 = Lerp(n00, n10, sx);
        float ix1 = Lerp(n01, n11, sx);

        return Lerp(ix0, ix1, sy);
    }

    public float GetFractalNoise(float x, float y, int octaves = 4, float persistence = 0.5f, float scale = 1f)
    {
        float total = 0;
        float frequency = scale;
        float amplitude = 1;
        float maxValue = 0;

        for (int i = 0; i < octaves; i++)
        {
            total += GetNoise(x * frequency, y * frequency) * amplitude;
            maxValue += amplitude;
            amplitude *= persistence;
            frequency *= 2;
        }

        return total / maxValue;
    }

    public float[] GenerateHeightmap(int width, int height, float scale = 0.02f, int octaves = 4)
    {
        float[] heightmap = new float[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                heightmap[y * width + x] = GetFractalNoise(x, y, octaves, 0.5f, scale);
            }
        }

        return heightmap;
    }
}
