using Godot;
using System.Collections.Generic;
using TianYanShop.World.Map;

namespace TianYanShop
{
	/// <summary>
	/// 生物群系混合规则管理器
	/// 定义不同生物群系之间的混合强度和策略
	/// </summary>
	public static class BiomeBlendRules
	{
		/// <summary>
		/// 混合强度映射表
		/// 0.0 = 不混合（清晰分界）
		/// 1.0 = 完全混合（模糊边界）
		/// </summary>
		private static readonly Dictionary<(BiomeType, BiomeType), float> BlendStrengthMap = new()
		{
			// === 海洋与陆地边界 ===
			{ (BiomeType.Ocean, BiomeType.IceSheet), 0.3f },
			{ (BiomeType.Ocean, BiomeType.Tundra), 0.3f },
			{ (BiomeType.Ocean, BiomeType.Desert), 0.2f },
			{ (BiomeType.Ocean, BiomeType.TropicalRainforest), 0.25f },
			{ (BiomeType.Ocean, BiomeType.AridShrubland), 0.25f },
			{ (BiomeType.Ocean, BiomeType.BorealForest), 0.3f },
			{ (BiomeType.Ocean, BiomeType.TemperateForest), 0.3f },
			{ (BiomeType.IceSheetOcean, BiomeType.IceSheet), 0.6f },
			{ (BiomeType.IceSheetOcean, BiomeType.Ocean), 0.5f },
			
			// === 冰雪系统 ===
			{ (BiomeType.IceSheet, BiomeType.Tundra), 0.85f },
			{ (BiomeType.IceSheet, BiomeType.ColdBog), 0.7f },
			{ (BiomeType.Tundra, BiomeType.ColdBog), 0.8f },
			{ (BiomeType.Tundra, BiomeType.BorealForest), 0.9f },
			
			// === 森林系统 ===
			{ (BiomeType.BorealForest, BiomeType.TemperateForest), 0.95f },
			{ (BiomeType.TemperateForest, BiomeType.TropicalRainforest), 0.9f },
			{ (BiomeType.BorealForest, BiomeType.ColdBog), 0.85f },
			{ (BiomeType.TemperateForest, BiomeType.TemperateSwamp), 0.9f },
			{ (BiomeType.TemperateForest, BiomeType.AridShrubland), 0.7f },
			
			// === 干旱系统 ===
			{ (BiomeType.AridShrubland, BiomeType.Desert), 0.95f },
			{ (BiomeType.Desert, BiomeType.ExtremeDesert), 0.9f },
			{ (BiomeType.ExtremeDesert, BiomeType.AridShrubland), 0.85f },
			
			// === 沼泽系统 ===
			{ (BiomeType.ColdBog, BiomeType.TemperateSwamp), 0.8f },
			{ (BiomeType.TemperateSwamp, BiomeType.TropicalSwamp), 0.85f },
			{ (BiomeType.TropicalSwamp, BiomeType.TropicalRainforest), 0.95f },
			
			// === 热带系统 ===
			{ (BiomeType.TropicalRainforest, BiomeType.TropicalSwamp), 0.95f },
			{ (BiomeType.TropicalRainforest, BiomeType.AridShrubland), 0.6f },
			
			// === 极端对比（保持清晰边界）===
			{ (BiomeType.Desert, BiomeType.BorealForest), 0.2f },
			{ (BiomeType.Desert, BiomeType.TropicalRainforest), 0.15f },
			{ (BiomeType.Desert, BiomeType.TemperateSwamp), 0.2f },
			{ (BiomeType.Desert, BiomeType.TropicalSwamp), 0.15f },
			{ (BiomeType.IceSheet, BiomeType.Desert), 0.1f },
			{ (BiomeType.IceSheet, BiomeType.TropicalRainforest), 0.1f },
			{ (BiomeType.IceSheet, BiomeType.ExtremeDesert), 0.05f },
		};
		
		/// <summary>
		/// 获取两个生物群系之间的混合强度
		/// </summary>
		public static float GetBlendStrength(BiomeType biome1, BiomeType biome2)
		{
			if (biome1 == biome2)
				return 0f;
			
			if (BlendStrengthMap.TryGetValue((biome1, biome2), out float strength))
				return strength;
			
			if (BlendStrengthMap.TryGetValue((biome2, biome1), out strength))
				return strength;
			
			return 0.7f;
		}
		
		/// <summary>
		/// 判断是否应该进行混合
		/// </summary>
		public static bool ShouldBlend(BiomeType biome1, BiomeType biome2)
		{
			if (biome1 == biome2)
				return false;
			
			float strength = GetBlendStrength(biome1, biome2);
			return strength > 0.05f;
		}
	}
}
