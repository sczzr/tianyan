using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;
using FantasyMapGenerator.Scripts.Data;
using FantasyMapGenerator.Scripts.Utils;

namespace FantasyMapGenerator.Scripts.Map.Heightmap;

/// <summary>
/// 高度图地形工具，用于创建各种地形特征
/// </summary>
public class HeightmapTools
{
	private readonly AleaPRNG _prng;
	private readonly Cell[] _cells;
	private readonly int _width;
	private readonly int _height;
	private float _blobPower;
	private float _linePower;

	public HeightmapTools(AleaPRNG prng, Cell[] cells, int width, int height)
	{
		_prng = prng;
		_cells = cells;
		_width = width;
		_height = height;
		_blobPower = GetBlobPower(cells.Length);
		_linePower = GetLinePower(cells.Length);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static float GetBlobPower(int cellCount)
	{
		return cellCount switch
		{
			<= 1000 => 0.93f,
			<= 2000 => 0.95f,
			<= 5000 => 0.97f,
			<= 10000 => 0.98f,
			<= 20000 => 0.99f,
			<= 30000 => 0.991f,
			<= 40000 => 0.993f,
			<= 50000 => 0.994f,
			<= 60000 => 0.995f,
			<= 70000 => 0.9955f,
			<= 80000 => 0.996f,
			<= 90000 => 0.9964f,
			_ => 0.9973f
		};
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static float GetLinePower(int cellCount)
	{
		return cellCount switch
		{
			<= 1000 => 0.75f,
			<= 2000 => 0.77f,
			<= 5000 => 0.79f,
			<= 10000 => 0.81f,
			<= 20000 => 0.82f,
			<= 30000 => 0.83f,
			<= 40000 => 0.84f,
			<= 50000 => 0.86f,
			<= 60000 => 0.87f,
			<= 70000 => 0.88f,
			<= 80000 => 0.91f,
			<= 90000 => 0.92f,
			_ => 0.93f
		};
	}

	/// <summary>
	/// 解析范围字符串，返回实际值
	/// </summary>
	private float GetValueInRange(string range)
	{
		if (string.IsNullOrEmpty(range)) return 0;

		if (range.Contains('-'))
		{
			var parts = range.Split('-');
			if (parts.Length == 2 &&
				float.TryParse(parts[0], out float min) &&
				float.TryParse(parts[1], out float max))
			{
				return _prng.NextRange(min, max);
			}
		}

		if (float.TryParse(range, out float value))
			return value;

		return 0;
	}

	/// <summary>
	/// 获取范围内的点位置
	/// </summary>
	private float GetPointInRange(string range, float length)
	{
		if (string.IsNullOrEmpty(range)) return length / 2;

		float min, max;
		if (range.Contains('-'))
		{
			var parts = range.Split('-');
			if (parts.Length == 2 &&
				float.TryParse(parts[0], out float minPct) &&
				float.TryParse(parts[1], out float maxPct))
			{
				min = minPct / 100f;
				max = maxPct / 100f;
			}
			else
			{
				min = max = 0.5f;
			}
		}
		else if (float.TryParse(range, out float pct))
		{
			min = max = pct / 100f;
		}
		else
		{
			min = max = 0.5f;
		}

		return _prng.NextRange(min * length, max * length);
	}

	/// <summary>
	/// 查找最近的Cell索引
	/// </summary>
	private int FindNearestCell(float x, float y)
	{
		int bestIndex = 0;
		float bestDist = float.MaxValue;

		for (int i = 0; i < _cells.Length; i++)
		{
			float dx = _cells[i].Position.X - x;
			float dy = _cells[i].Position.Y - y;
			float dist = dx * dx + dy * dy;
			if (dist < bestDist)
			{
				bestDist = dist;
				bestIndex = i;
			}
		}

		return bestIndex;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static float Clamp(float value, float min = 0, float max = 100)
	{
		return Math.Max(min, Math.Min(max, value));
	}

	/// <summary>
	/// 添加丘陵/山峰
	/// </summary>
	public void AddHill(string countRange, string heightRange, string rangeX, string rangeY)
	{
		int count = (int)GetValueInRange(countRange);
		if (count < 1) count = 1;

		for (int i = 0; i < count; i++)
		{
			AddOneHill(heightRange, rangeX, rangeY);
		}
	}

	private void AddOneHill(string heightRange, string rangeX, string rangeY)
	{
		var change = new float[_cells.Length];
		float h = Clamp(GetValueInRange(heightRange));

		int start = -1;
		int limit = 0;
		do
		{
			float x = GetPointInRange(rangeX, _width);
			float y = GetPointInRange(rangeY, _height);
			start = FindNearestCell(x, y);
			limit++;
		} while (_cells[start].Height * 100 + h > 90 && limit < 50);

		if (start < 0) return;

		change[start] = h;
		var queue = new Queue<int>();
		queue.Enqueue(start);

		while (queue.Count > 0)
		{
			int q = queue.Dequeue();

			foreach (int c in _cells[q].NeighborIds)
			{
				if (change[c] > 0) continue;
				change[c] = MathF.Pow(change[q], _blobPower) * (_prng.NextRange(0.9f, 1.1f));
				if (change[c] > 1)
					queue.Enqueue(c);
			}
		}

		for (int i = 0; i < _cells.Length; i++)
		{
			_cells[i].Height = Clamp(_cells[i].Height * 100 + change[i]) / 100f;
		}
	}

	/// <summary>
	/// 添加坑洼/凹陷
	/// </summary>
	public void AddPit(string countRange, string heightRange, string rangeX, string rangeY)
	{
		int count = (int)GetValueInRange(countRange);
		if (count < 1) count = 1;

		for (int i = 0; i < count; i++)
		{
			AddOnePit(heightRange, rangeX, rangeY);
		}
	}

	private void AddOnePit(string heightRange, string rangeX, string rangeY)
	{
		var used = new bool[_cells.Length];
		float h = Clamp(GetValueInRange(heightRange));

		int start = -1;
		int limit = 0;
		do
		{
			float x = GetPointInRange(rangeX, _width);
			float y = GetPointInRange(rangeY, _height);
			start = FindNearestCell(x, y);
			limit++;
		} while (_cells[start].Height * 100 < 20 && limit < 50);

		if (start < 0) return;

		var queue = new Queue<int>();
		queue.Enqueue(start);

		while (queue.Count > 0)
		{
			int q = queue.Dequeue();
			h = MathF.Pow(h, _blobPower) * (_prng.NextRange(0.9f, 1.1f));
			if (h < 1) return;

			foreach (int c in _cells[q].NeighborIds)
			{
				if (used[c]) continue;
				_cells[c].Height = Clamp(_cells[c].Height * 100 - h * _prng.NextRange(0.9f, 1.1f)) / 100f;
				used[c] = true;
				queue.Enqueue(c);
			}
		}
	}

	/// <summary>
	/// 添加山脉
	/// </summary>
	public void AddRange(string countRange, string heightRange, string rangeX, string rangeY)
	{
		int count = (int)GetValueInRange(countRange);
		if (count < 1) count = 1;

		for (int i = 0; i < count; i++)
		{
			AddOneRange(heightRange, rangeX, rangeY);
		}
	}

	private void AddOneRange(string heightRange, string rangeX, string rangeY)
	{
		var used = new bool[_cells.Length];
		float h = Clamp(GetValueInRange(heightRange));

		float startX = GetPointInRange(rangeX, _width);
		float startY = GetPointInRange(rangeY, _height);

		float dist = 0;
		int limit = 0;
		float endX, endY;
		do
		{
			endX = _prng.NextRange(_width * 0.1f, _width * 0.9f);
			endY = _prng.NextRange(_height * 0.15f, _height * 0.85f);
			dist = MathF.Abs(endY - startY) + MathF.Abs(endX - startX);
			limit++;
		} while ((dist < _width / 8f || dist > _width / 3f) && limit < 50);

		int startCell = FindNearestCell(startX, startY);
		int endCell = FindNearestCell(endX, endY);

		var range = GetRangePath(startCell, endCell, used);
		if (range.Count == 0) return;

		var queue = new List<int>(range);
		int iteration = 0;

		while (queue.Count > 0)
		{
			var frontier = new List<int>(queue);
			queue.Clear();
			iteration++;

			foreach (int i in frontier)
			{
				_cells[i].Height = Clamp(_cells[i].Height * 100 + h * _prng.NextRange(0.85f, 1.15f)) / 100f;
			}

			h = MathF.Pow(h, _linePower) - 1;
			if (h < 2) break;

			foreach (int f in frontier)
			{
				foreach (int c in _cells[f].NeighborIds)
				{
					if (!used[c])
					{
						queue.Add(c);
						used[c] = true;
					}
				}
			}
		}

		// 生成山脊线上的隆起
		for (int d = 0; d < range.Count; d += 6)
		{
			int cur = range[d];
			for (int l = 0; l < iteration; l++)
			{
				int minIndex = -1;
				float minHeight = float.MaxValue;
				foreach (int c in _cells[cur].NeighborIds)
				{
					if (_cells[c].Height < minHeight)
					{
						minHeight = _cells[c].Height;
						minIndex = c;
					}
				}
				if (minIndex < 0) break;
				_cells[minIndex].Height = (_cells[cur].Height * 2 + _cells[minIndex].Height) / 3f;
				cur = minIndex;
			}
		}
	}

	private List<int> GetRangePath(int start, int end, bool[] used)
	{
		var range = new List<int> { start };
		used[start] = true;
		int cur = start;

		while (cur != end)
		{
			float minDist = float.MaxValue;
			int next = -1;

			foreach (int c in _cells[cur].NeighborIds)
			{
				if (used[c]) continue;
				float dx = _cells[end].Position.X - _cells[c].Position.X;
				float dy = _cells[end].Position.Y - _cells[c].Position.Y;
				float dist = dx * dx + dy * dy;
				if (_prng.NextDouble() > 0.85) dist /= 2;
				if (dist < minDist)
				{
					minDist = dist;
					next = c;
				}
			}

			if (next < 0) break;
			range.Add(next);
			used[next] = true;
			cur = next;
		}

		return range;
	}

	/// <summary>
	/// 添加峡谷/槽谷
	/// </summary>
	public void AddTrough(string countRange, string heightRange, string rangeX, string rangeY)
	{
		int count = (int)GetValueInRange(countRange);
		if (count < 1) count = 1;

		for (int i = 0; i < count; i++)
		{
			AddOneTrough(heightRange, rangeX, rangeY);
		}
	}

	private void AddOneTrough(string heightRange, string rangeX, string rangeY)
	{
		var used = new bool[_cells.Length];
		float h = Clamp(GetValueInRange(heightRange));

		int startCell = -1;
		int limit = 0;
		float startX, startY;
		do
		{
			startX = GetPointInRange(rangeX, _width);
			startY = GetPointInRange(rangeY, _height);
			startCell = FindNearestCell(startX, startY);
			limit++;
		} while (_cells[startCell].Height * 100 < 20 && limit < 50);

		float dist = 0;
		limit = 0;
		float endX, endY;
		do
		{
			endX = _prng.NextRange(_width * 0.1f, _width * 0.9f);
			endY = _prng.NextRange(_height * 0.15f, _height * 0.85f);
			dist = MathF.Abs(endY - startY) + MathF.Abs(endX - startX);
			limit++;
		} while ((dist < _width / 8f || dist > _width / 2f) && limit < 50);

		int endCell = FindNearestCell(endX, endY);

		var range = GetRangePath(startCell, endCell, used);
		if (range.Count == 0) return;

		var queue = new List<int>(range);
		int iteration = 0;

		while (queue.Count > 0)
		{
			var frontier = new List<int>(queue);
			queue.Clear();
			iteration++;

			foreach (int i in frontier)
			{
				_cells[i].Height = Clamp(_cells[i].Height * 100 - h * _prng.NextRange(0.85f, 1.15f)) / 100f;
			}

			h = MathF.Pow(h, _linePower) - 1;
			if (h < 2) break;

			foreach (int f in frontier)
			{
				foreach (int c in _cells[f].NeighborIds)
				{
					if (!used[c])
					{
						queue.Add(c);
						used[c] = true;
					}
				}
			}
		}
	}

	/// <summary>
	/// 添加海峡
	/// </summary>
	public void AddStrait(string widthRange, string direction)
	{
		float desiredWidth = GetValueInRange(widthRange);
		if (desiredWidth < 1) return;

		bool vertical = direction != "horizontal";
		var used = new bool[_cells.Length];

		float startX = vertical ? _prng.NextRange(_width * 0.3f, _width * 0.7f) : 5;
		float startY = vertical ? 5 : _prng.NextRange(_height * 0.3f, _height * 0.7f);
		float endX = vertical ? _prng.NextRange(_width * 0.3f, _width * 0.7f) : _width - 5;
		float endY = vertical ? _height - 5 : _prng.NextRange(_height * 0.3f, _height * 0.7f);

		int start = FindNearestCell(startX, startY);
		int end = FindNearestCell(endX, endY);

		var range = GetStraitPath(start, end);
		var query = new List<int>();

		float step = 0.1f / desiredWidth;

		for (int i = 0; i < desiredWidth; i++)
		{
			float exp = 0.9f - step * desiredWidth;
			foreach (int r in range)
			{
				foreach (int c in _cells[r].NeighborIds)
				{
					if (used[c]) continue;
					used[c] = true;
					query.Add(c);
					float newHeight = MathF.Pow(_cells[c].Height * 100, exp);
					if (newHeight > 100) newHeight = 5;
					_cells[c].Height = newHeight / 100f;
				}
			}
			range = new List<int>(query);
			query.Clear();
		}
	}

	private List<int> GetStraitPath(int start, int end)
	{
		var range = new List<int>();
		int cur = start;

		while (cur != end)
		{
			float minDist = float.MaxValue;
			int next = -1;

			foreach (int c in _cells[cur].NeighborIds)
			{
				float dx = _cells[end].Position.X - _cells[c].Position.X;
				float dy = _cells[end].Position.Y - _cells[c].Position.Y;
				float dist = dx * dx + dy * dy;
				if (_prng.NextDouble() > 0.8) dist /= 2;
				if (dist < minDist)
				{
					minDist = dist;
					next = c;
				}
			}

			if (next < 0) break;
			range.Add(next);
			cur = next;
		}

		return range;
	}

	/// <summary>
	/// 应用边缘遮罩
	/// </summary>
	public void Mask(string powerRange)
	{
		float power = GetValueInRange(powerRange);
		if (MathF.Abs(power) < 0.01f) power = 1;

		float fr = MathF.Abs(power);

		for (int i = 0; i < _cells.Length; i++)
		{
			float x = _cells[i].Position.X;
			float y = _cells[i].Position.Y;

			float nx = 2 * x / _width - 1;  // [-1, 1]
			float ny = 2 * y / _height - 1; // [-1, 1]

			float distance = (1 - nx * nx) * (1 - ny * ny); // 1 at center, 0 at edge
			if (power < 0) distance = 1 - distance;

			float h = _cells[i].Height * 100;
			float masked = h * distance;
			_cells[i].Height = Clamp((h * (fr - 1) + masked) / fr) / 100f;
		}
	}

	/// <summary>
	/// 平滑高度图
	/// </summary>
	public void Smooth(string fractionRange)
	{
		int fr = (int)GetValueInRange(fractionRange);
		if (fr < 1) fr = 2;

		var newHeights = new float[_cells.Length];

		for (int i = 0; i < _cells.Length; i++)
		{
			float sum = _cells[i].Height;
			int count = 1;

			foreach (int c in _cells[i].NeighborIds)
			{
				sum += _cells[c].Height;
				count++;
			}

			float avg = sum / count;
			if (fr == 1)
			{
				newHeights[i] = avg;
			}
			else
			{
				newHeights[i] = (_cells[i].Height * (fr - 1) + avg) / fr;
			}
		}

		for (int i = 0; i < _cells.Length; i++)
		{
			_cells[i].Height = Clamp(newHeights[i] * 100) / 100f;
		}
	}

	/// <summary>
	/// 添加/乘法修改高度
	/// </summary>
	public void Modify(string range, float add, float mult)
	{
		float min, max;
		if (range == "land")
		{
			min = 0.20f;
			max = 1.0f;
		}
		else if (range == "all")
		{
			min = 0;
			max = 1.0f;
		}
		else if (range.Contains('-'))
		{
			var parts = range.Split('-');
			min = float.TryParse(parts[0], out float v1) ? v1 / 100f : 0;
			max = float.TryParse(parts[1], out float v2) ? v2 / 100f : 1;
		}
		else
		{
			min = 0;
			max = 1.0f;
		}

		bool isLand = min >= 0.20f;

		for (int i = 0; i < _cells.Length; i++)
		{
			float h = _cells[i].Height;
			if (h < min || h > max) continue;

			if (add != 0)
			{
				h = isLand ? MathF.Max(h + add / 100f, 0.20f) : h + add / 100f;
			}
			if (MathF.Abs(mult - 1) > 0.001f)
			{
				h = isLand ? (h - 0.20f) * mult + 0.20f : h * mult;
			}

			_cells[i].Height = Clamp(h * 100) / 100f;
		}
	}

	/// <summary>
	/// 反转高度图
	/// </summary>
	public void Invert(string probabilityRange, string axes)
	{
		float prob = GetValueInRange(probabilityRange);
		if (_prng.NextDouble() > prob) return;

		bool invertX = axes != "y";
		bool invertY = axes != "x";

		var newHeights = new float[_cells.Length];

		for (int i = 0; i < _cells.Length; i++)
		{
			float x = _cells[i].Position.X;
			float y = _cells[i].Position.Y;

			float nx = invertX ? _width - x : x;
			float ny = invertY ? _height - y : y;

			int nearestInverted = FindNearestCell(nx, ny);
			newHeights[i] = _cells[nearestInverted].Height;
		}

		for (int i = 0; i < _cells.Length; i++)
		{
			_cells[i].Height = newHeights[i];
		}
	}
}
