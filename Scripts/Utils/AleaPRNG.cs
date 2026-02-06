using System;
using Godot;
using System.Runtime.CompilerServices;

namespace FantasyMapGenerator.Scripts.Utils;

/// <summary>
/// Alea 伪随机数生成器
/// </summary>
public class AleaPRNG
{
	private readonly double[] _state = new double[4];
	private int _k;
	private double _floatState;

	public AleaPRNG(uint seed)
	{
		InitState((ulong)seed);
	}

	public AleaPRNG(string seed)
	{
		var hash = Fnv1aHash(seed);
		InitState(hash);
	}

	private void InitState(ulong seed)
	{
		_state[0] = seed & 0xFFFF;
		_state[1] = (seed >> 16) & 0xFFFF;
		_state[2] = seed & 0xFFFF;
		_state[3] = (seed >> 16) & 0xFFFF;

		for (int i = 0; i < 30; i++)
		{
			_ = NextDouble();
		}
	}

	private static ulong Fnv1aHash(string str)
	{
		ulong hash = 2166136261u;
		foreach (char c in str)
		{
			hash ^= (ulong)c;
			hash *= 16777619u;
		}
		return hash;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public double NextDouble()
	{
		var t = _state[0] += _state[3];
		ulong ut = (ulong)t;
		ut = (ut ^ (ut >> 5)) * 0xCD9E8D57U;
		ut = (ut ^ (ut >> 13)) * 0xFC87C538U;
		ut = (ut ^ (ut >> 6)) * 0xB5026F5AU;
		_floatState = (ut >> 12) * 2.2204460492503131e-16; // 2^-52
		return _floatState;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float NextFloat()
	{
		return (float)NextDouble();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int NextInt()
	{
		return (int)(NextDouble() * 2147483647.0);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int NextInt(int min, int max)
	{
		return min + (int)(NextDouble() * (max - min + 1));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public float NextRange(float min, float max)
	{
		return min + (float)NextDouble() * (max - min);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vector2 NextVector2(float minX, float maxX, float minY, float maxY)
	{
		return new Vector2(
			NextRange(minX, maxX),
			NextRange(minY, maxY)
		);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vector2 NextVector2InCircle(float radius)
	{
		double angle = NextDouble() * Math.PI * 2;
		double r = Math.Sqrt(NextDouble()) * radius;
		return new Vector2(
			(float)(Math.Cos(angle) * r),
			(float)(Math.Sin(angle) * r)
		);
	}
}
