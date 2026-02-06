using System;
using Godot;
using System.Runtime.CompilerServices;
using FantasyMapGenerator.Scripts.Data;
using FantasyMapGenerator.Scripts.Utils;

namespace FantasyMapGenerator.Scripts.Map.Heightmap;

/// <summary>
/// 高度图处理器，负责将高度数据转换为地图特征和颜色
/// </summary>
public class HeightmapProcessor
{
	private readonly AleaPRNG _prng;
	private readonly PerlinNoise _noise;
	public float WaterLevel { get; set; } = 0.35f;

	public HeightmapProcessor(AleaPRNG prng)
	{
		_prng = prng;
		_noise = new PerlinNoise(prng);
	}

	public float[] GenerateHeightmap(int width, int height, int seedOffset = 0)
	{
		float[] heightmap = new float[width * height];

		float scale = 0.015f;
		int octaves = 5;

		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				float nx = x / (float)width;
				float ny = y / (float)height;

				float value = _noise.GetFractalNoise(nx, ny, octaves, 0.5f, scale);

				float distFromCenter = DistanceFromCenter(nx, ny);
				value *= distFromCenter;

				heightmap[y * width + x] = Mathf.Clamp(value, -1f, 1f);
			}
		}

		Normalize(heightmap, width, height);

		return heightmap;
	}

	/// <summary>
	/// 使用模板生成高度图（直接在Cell上操作）
	/// </summary>
	public void GenerateFromTemplate(Cell[] cells, int width, int height, HeightmapTemplateType templateType)
	{
		// 初始化所有Cell的高度为0
		foreach (var cell in cells)
		{
			cell.Height = 0;
		}

		string template = HeightmapTemplates.GetTemplate(templateType);
		ApplyTemplate(cells, width, height, template);

		// 应用水位线
		foreach (var cell in cells)
		{
			cell.IsLand = cell.Height > WaterLevel;
		}
	}

	/// <summary>
	/// 使用随机模板生成高度图
	/// </summary>
	public void GenerateFromRandomTemplate(Cell[] cells, int width, int height)
	{
		var random = new Random(_prng.NextInt());
		var templateType = HeightmapTemplates.GetRandomTemplate(random);
		GD.Print($"Using heightmap template: {HeightmapTemplates.GetTemplateName(templateType)}");
		GenerateFromTemplate(cells, width, height, templateType);
	}

	/// <summary>
	/// 应用模板字符串
	/// </summary>
	public void ApplyTemplate(Cell[] cells, int width, int height, string template)
	{
		var tools = new HeightmapTools(_prng, cells, width, height);
		var lines = template.Split('\n', StringSplitOptions.RemoveEmptyEntries);

		foreach (var line in lines)
		{
			var trimmed = line.Trim();
			if (string.IsNullOrEmpty(trimmed)) continue;

			var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length < 2) continue;

			string command = parts[0];
			string arg1 = parts.Length > 1 ? parts[1] : "0";
			string arg2 = parts.Length > 2 ? parts[2] : "0";
			string arg3 = parts.Length > 3 ? parts[3] : "0";
			string arg4 = parts.Length > 4 ? parts[4] : "0";

			switch (command)
			{
				case "Hill":
					tools.AddHill(arg1, arg2, arg3, arg4);
					break;
				case "Pit":
					tools.AddPit(arg1, arg2, arg3, arg4);
					break;
				case "Range":
					tools.AddRange(arg1, arg2, arg3, arg4);
					break;
				case "Trough":
					tools.AddTrough(arg1, arg2, arg3, arg4);
					break;
				case "Strait":
					tools.AddStrait(arg1, arg2);
					break;
				case "Mask":
					tools.Mask(arg1);
					break;
				case "Smooth":
					tools.Smooth(arg1);
					break;
				case "Add":
					if (float.TryParse(arg1, out float addValue))
						tools.Modify(arg2, addValue, 1);
					break;
				case "Multiply":
					if (float.TryParse(arg1, out float multValue))
						tools.Modify(arg2, 0, multValue);
					break;
				case "Invert":
					tools.Invert(arg1, arg2);
					break;
			}
		}

		// 规范化高度到 [0, 1] 范围
		NormalizeCellHeights(cells);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static float DistanceFromCenter(float x, float y)
	{
		float dx = x - 0.5f;
		float dy = y - 0.5f;
		float dist = Mathf.Sqrt(dx * dx + dy * dy);
		float maxDist = Mathf.Sqrt(0.5f * 0.5f + 0.5f * 0.5f);
		return Mathf.Clamp(1f - dist / (maxDist * 0.8f), 0f, 1f);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void Normalize(float[] heightmap, int width, int height)
	{
		float min = float.MaxValue;
		float max = float.MinValue;

		for (int i = 0; i < heightmap.Length; i++)
		{
			min = Mathf.Min(min, heightmap[i]);
			max = Mathf.Max(max, heightmap[i]);
		}

		float range = max - min;
		if (range < 0.0001f) range = 1f;

		for (int i = 0; i < heightmap.Length; i++)
		{
			heightmap[i] = (heightmap[i] - min) / range;
		}
	}

	/// <summary>
	/// 规范化Cell高度到 [0, 1] 范围
	/// </summary>
	private static void NormalizeCellHeights(Cell[] cells)
	{
		if (cells.Length == 0) return;

		float min = float.MaxValue;
		float max = float.MinValue;

		foreach (var cell in cells)
		{
			min = Mathf.Min(min, cell.Height);
			max = Mathf.Max(max, cell.Height);
		}

		float range = max - min;
		if (range < 0.0001f) range = 1f;

		foreach (var cell in cells)
		{
			cell.Height = (cell.Height - min) / range;
		}
	}

	public void ApplyToCells(Cell[] cells, float[] heightmap, int width, int height)
	{
		for (int i = 0; i < cells.Length; i++)
		{
			var cell = cells[i];
			int mapX = (int)Mathf.Clamp(cell.Position.X, 0, width - 1);
			int mapY = (int)Mathf.Clamp(cell.Position.Y, 0, height - 1);

			int heightIndex = mapY * width + mapX;
			if (heightIndex >= 0 && heightIndex < heightmap.Length)
			{
				cell.Height = heightmap[heightIndex];
				cell.IsLand = cell.Height > WaterLevel;
			}
		}
	}

	public Color GetColorForHeight(float height, bool isLand)
	{
		if (!isLand)
		{
			return new Color(0.2f, 0.4f, 0.8f, 1f);
		}

		if (height < 0.4f)
		{
			return new Color(0.3f, 0.6f, 0.3f, 1f);
		}
		else if (height < 0.6f)
		{
			return new Color(0.2f, 0.5f, 0.2f, 1f);
		}
		else if (height < 0.8f)
		{
			return new Color(0.3f, 0.35f, 0.3f, 1f);
		}
		else
		{
			return new Color(0.9f, 0.9f, 0.95f, 1f);
		}
	}

	public void AssignColors(Cell[] cells)
	{
		foreach (var cell in cells)
		{
			cell.RenderColor = GetColorForHeight(cell.Height, cell.IsLand);
		}
	}
}
