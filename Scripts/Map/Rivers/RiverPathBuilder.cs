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
	/// 为河流添加曲折效果
	/// </summary>
	public void AddMeandering(River river, float meandering = DefaultMeandering)
	{
		river.MeanderedPoints.Clear();

		var points = GetRiverPoints(river);
		if (points.Count < 2) return;

		int step = _cells[river.Cells[0]].Height < 0.2f ? 1 : 10;

		for (int i = 0; i < points.Count; i++, step++)
		{
			int cellId = river.Cells[i];
			float flux = cellId >= 0 ? _cells[cellId].Flux : 0;

			var (x1, y1) = points[i];
			river.MeanderedPoints.Add(new Vector3(x1, y1, flux));

			if (i >= points.Count - 1) break;

			int nextCellId = river.Cells[i + 1];
			if (nextCellId < 0)
			{
				var (x2, y2) = points[i + 1];
				river.MeanderedPoints.Add(new Vector3(x2, y2, flux));
				break;
			}

			var (nx, ny) = points[i + 1];
			float dist2 = (nx - x1) * (nx - x1) + (ny - y1) * (ny - y1);

			// 如果距离太近且河流足够长，跳过
			if (dist2 <= 25 && river.Cells.Count >= 6) continue;

			// 计算曲折度
			float meander = meandering + 1f / step + MathF.Max(meandering - step / 100f, 0);
			float angle = MathF.Atan2(ny - y1, nx - x1);
			float sinMeander = MathF.Sin(angle) * meander;
			float cosMeander = MathF.Cos(angle) * meander;

			if (step < 20 && (dist2 > 64 || (dist2 > 36 && river.Cells.Count < 5)))
			{
				// 距离较大时添加两个控制点
				float p1x = (x1 * 2 + nx) / 3 - sinMeander;
				float p1y = (y1 * 2 + ny) / 3 + cosMeander;
				float p2x = (x1 + nx * 2) / 3 + sinMeander / 2;
				float p2y = (y1 + ny * 2) / 3 - cosMeander / 2;
				river.MeanderedPoints.Add(new Vector3(p1x, p1y, 0));
				river.MeanderedPoints.Add(new Vector3(p2x, p2y, 0));
			}
			else if (dist2 > 25 || river.Cells.Count < 6)
			{
				// 距离中等时添加一个控制点
				float px = (x1 + nx) / 2 - sinMeander;
				float py = (y1 + ny) / 2 + cosMeander;
				river.MeanderedPoints.Add(new Vector3(px, py, 0));
			}
		}
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
					var borderPoint = GetBorderPoint(river.Cells[i - 1]);
					points.Add(borderPoint);
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
