using System;
using System.Collections.Generic;
using Godot;
using FantasyMapGenerator.Scripts.Data;
using FantasyMapGenerator.Scripts.Utils;

namespace FantasyMapGenerator.Scripts.Map.Rivers;

/// <summary>
/// 河流路径构建器，生成曲折的河流渲染路径
/// </summary>
public class RiverPathBuilder
{
	private readonly Cell[] _cells;
	private readonly AleaPRNG _prng;
	private readonly int _mapWidth;
	private readonly int _mapHeight;

	private const float DefaultMeandering = 0.5f;

	public RiverPathBuilder(Cell[] cells, AleaPRNG prng, int mapWidth, int mapHeight)
	{
		_cells = cells;
		_prng = prng;
		_mapWidth = mapWidth;
		_mapHeight = mapHeight;
	}

	/// <summary>
	/// 为河流添加曲折效果 (使用 Chaikin 平滑算法)
	/// </summary>
	public void AddMeandering(River river, float meandering = DefaultMeandering)
	{
		river.MeanderedPoints.Clear();

		var rawPoints = GetRiverPoints(river);
		if (rawPoints.Count < 2) return;

		// 1. 构建基础控制点并添加随机扰动
		var controlPoints = new List<Vector3>();
		for (int i = 0; i < rawPoints.Count; i++)
		{
			int cellId = river.Cells[Math.Min(i, river.Cells.Count - 1)];
			float flux = cellId >= 0 ? _cells[cellId].Flux : 0;
			controlPoints.Add(new Vector3(rawPoints[i].x, rawPoints[i].y, flux));

			// 在点之间添加扰动点
			if (i < rawPoints.Count - 1)
			{
				var p1 = rawPoints[i];
				var p2 = rawPoints[i + 1];
				float dist = Mathf.Sqrt((p2.x - p1.x) * (p2.x - p1.x) + (p2.y - p1.y) * (p2.y - p1.y));
				
				// 只有足够长的段才添加扰动
				if (dist > 10) 
				{
					// 中点
					float midX = (p1.x + p2.x) * 0.5f;
					float midY = (p1.y + p2.y) * 0.5f;

					// 垂直向量
					float dx = p2.x - p1.x;
					float dy = p2.y - p1.y;
					float nx = -dy;
					float ny = dx;
					
					// 归一化
					float len = Mathf.Sqrt(nx * nx + ny * ny);
					if (len > 0.001f)
					{
						nx /= len;
						ny /= len;
					}

					// 随机偏移 (基于 meandering 参数)
					float offsetStr = dist * meandering * 0.3f;
					float offset = _prng.NextRange(-offsetStr, offsetStr);

					controlPoints.Add(new Vector3(midX + nx * offset, midY + ny * offset, flux));
				}
			}
		}

		// 2. 应用 Chaikin 平滑
		var smoothedPoints = ChaikinSmooth(controlPoints, 3); // 3次迭代

		river.MeanderedPoints.AddRange(smoothedPoints);
	}

	private List<Vector3> ChaikinSmooth(List<Vector3> points, int iterations)
	{
		if (iterations <= 0 || points.Count < 3) return points;

		var newPoints = new List<Vector3>();
		
		// 保留第一个点
		newPoints.Add(points[0]);

		for (int i = 0; i < points.Count - 1; i++)
		{
			var p0 = points[i];
			var p1 = points[i + 1];

			// Q: 0.75 * p0 + 0.25 * p1
			newPoints.Add(p0 * 0.75f + p1 * 0.25f);
			
			// R: 0.25 * p0 + 0.75 * p1
			newPoints.Add(p0 * 0.25f + p1 * 0.75f);
		}

		// 保留最后一个点
		newPoints.Add(points[points.Count - 1]);

		return ChaikinSmooth(newPoints, iterations - 1);
	}

	/// <summary>
	/// 获取河流点坐标
	/// </summary>
	private List<(float x, float y)> GetRiverPoints(River river)
	{
		var points = new List<(float, float)>();

		for (int i = 0; i < river.Cells.Count; i++)
		{
			int cellId = river.Cells[i];
			if (cellId < 0)
			{
				// 边界点
				if (i > 0)
				{
					int prevCellId = river.Cells[i - 1];
					// 确保 prevCellId 有效
					if (prevCellId >= 0) {
						var borderPoint = GetBorderPoint(prevCellId);
						points.Add(borderPoint);
					}
				}
			}
			else
			{
				points.Add((_cells[cellId].Position.X, _cells[cellId].Position.Y));
			}
		}

		return points;
	}

	/// <summary>
	/// 获取边界点（水流出地图的位置）
	/// </summary>
	private (float x, float y) GetBorderPoint(int cellId)
	{
		float x = _cells[cellId].Position.X;
		float y = _cells[cellId].Position.Y;

		float minDist = MathF.Min(
			MathF.Min(y, _mapHeight - y),
			MathF.Min(x, _mapWidth - x)
		);

		if (minDist == y) return (x, 0);
		if (minDist == _mapHeight - y) return (x, _mapHeight);
		if (minDist == x) return (0, y);
		return (_mapWidth, y);
	}

	/// <summary>
	/// 构建河流多边形路径（用于渲染）
	/// </summary>
	public List<Vector2> BuildPolygon(River river)
	{
		if (river.MeanderedPoints.Count < 2)
			return new List<Vector2>();

		var leftPoints = new List<Vector2>();
		var rightPoints = new List<Vector2>();
		float flux = 0;

		for (int i = 0; i < river.MeanderedPoints.Count; i++)
		{
			var prev = i > 0 ? river.MeanderedPoints[i - 1] : river.MeanderedPoints[i];
			var curr = river.MeanderedPoints[i];
			var next = i < river.MeanderedPoints.Count - 1 ? river.MeanderedPoints[i + 1] : river.MeanderedPoints[i];

			if (curr.Z > flux) flux = curr.Z;

			float offset = GetOffset(flux, i, river.WidthFactor, river.SourceWidth);
			float angle = MathF.Atan2(prev.Y - next.Y, prev.X - next.X);
			float sinOffset = MathF.Sin(angle) * offset;
			float cosOffset = MathF.Cos(angle) * offset;

			leftPoints.Add(new Vector2(curr.X - sinOffset, curr.Y + cosOffset));
			rightPoints.Add(new Vector2(curr.X + sinOffset, curr.Y - cosOffset));
		}

		// 合并左右点形成多边形
		rightPoints.Reverse();
		var polygon = new List<Vector2>();
		polygon.AddRange(leftPoints);
		polygon.AddRange(rightPoints);

		return polygon;
	}

	/// <summary>
	/// 获取宽度偏移
	/// </summary>
	private float GetOffset(float flux, int pointIndex, float widthFactor, float startingWidth)
	{
		if (pointIndex == 0) return startingWidth;

		float fluxWidth = MathF.Min(MathF.Pow(flux, 0.7f) / 500f, 1f);
		float lengthWidth = pointIndex / 200f;

		return widthFactor * (lengthWidth + fluxWidth) + startingWidth;
	}

	/// <summary>
	/// 为所有河流构建渲染路径
	/// </summary>
	public void BuildAllRiverPaths(List<River> rivers)
	{
		foreach (var river in rivers)
		{
			AddMeandering(river);
		}
	}
}
