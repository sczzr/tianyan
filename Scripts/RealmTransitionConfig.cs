using Godot;
using System;
using System.Collections.Generic;

namespace TianYanShop
{
    /// <summary>
    /// 地形过渡配置数据库
    /// </summary>
    public static class RealmTransitionConfig
    {
        /// <summary>
        /// 基础地形数据（山川河海等普通地形）
        /// </summary>
        public static readonly Dictionary<TerrainType, TerrainData> Terrains = new()
        {
            { TerrainType.Ocean, new TerrainData(
                TerrainType.Ocean, "海洋",
                "res://Assets/Textures/World/terrains/Ocean.png",
                new Color(0.2f, 0.4f, 0.7f),
                0.0f, 0.2f, 0.3f, 0.8f, 0.8f, 1.0f) },

            { TerrainType.Beach, new TerrainData(
                TerrainType.Beach, "海滩",
                "res://Assets/Textures/World/terrains/Beach.png",
                new Color(0.76f, 0.70f, 0.50f),
                0.0f, 0.25f, 0.5f, 0.9f, 0.2f, 0.5f) },

            { TerrainType.Plain, new TerrainData(
                TerrainType.Plain, "平原",
                "res://Assets/Textures/World/terrains/Plain.png",
                new Color(0.35f, 0.60f, 0.25f),
                0.15f, 0.4f, 0.3f, 0.8f, 0.3f, 0.7f) },

            { TerrainType.Hill, new TerrainData(
                TerrainType.Hill, "丘陵",
                "res://Assets/Textures/World/terrains/Hill.png",
                new Color(0.40f, 0.50f, 0.30f),
                0.3f, 0.5f, 0.25f, 0.75f, 0.3f, 0.6f) },

            { TerrainType.Mountain, new TerrainData(
                TerrainType.Mountain, "山地",
                "res://Assets/Textures/World/terrains/Mountain.png",
                new Color(0.45f, 0.42f, 0.35f),
                0.5f, 0.9f, 0.1f, 0.6f, 0.2f, 0.6f) },

            { TerrainType.Forest, new TerrainData(
                TerrainType.Forest, "森林",
                "res://Assets/Textures/World/terrains/Forest.png",
                new Color(0.15f, 0.45f, 0.20f),
                0.25f, 0.6f, 0.35f, 0.75f, 0.4f, 0.8f) },

            { TerrainType.Desert, new TerrainData(
                TerrainType.Desert, "沙漠",
                "res://Assets/Textures/World/terrains/Desert.png",
                new Color(0.85f, 0.75f, 0.50f),
                0.2f, 0.5f, 0.6f, 1.0f, 0.0f, 0.2f) },

            { TerrainType.Swamp, new TerrainData(
                TerrainType.Swamp, "沼泽",
                "res://Assets/Textures/World/terrains/Swamp.png",
                new Color(0.25f, 0.35f, 0.25f),
                0.0f, 0.3f, 0.4f, 0.8f, 0.7f, 1.0f) },

            { TerrainType.River, new TerrainData(
                TerrainType.River, "河流",
                "res://Assets/Textures/World/terrains/River.png",
                new Color(0.25f, 0.45f, 0.65f),
                0.0f, 0.2f, 0.3f, 0.8f, 0.8f, 1.0f) },

            { TerrainType.Lake, new TerrainData(
                TerrainType.Lake, "湖泊",
                "res://Assets/Textures/World/terrains/Lake.png",
                new Color(0.20f, 0.40f, 0.60f),
                0.0f, 0.25f, 0.25f, 0.7f, 0.7f, 1.0f) },

            { TerrainType.Canyon, new TerrainData(
                TerrainType.Canyon, "峡谷",
                "res://Assets/Textures/World/terrains/Canyon.png",
                new Color(0.50f, 0.40f, 0.30f),
                0.4f, 0.8f, 0.3f, 0.8f, 0.1f, 0.4f) },

            { TerrainType.Plateau, new TerrainData(
                TerrainType.Plateau, "高原",
                "res://Assets/Textures/World/terrains/Plateau.png",
                new Color(0.55f, 0.50f, 0.35f),
                0.6f, 0.9f, 0.1f, 0.5f, 0.2f, 0.5f) },

            { TerrainType.Tundra, new TerrainData(
                TerrainType.Tundra, "苔原",
                "res://Assets/Textures/World/terrains/Tundra.png",
                new Color(0.60f, 0.65f, 0.65f),
                0.3f, 0.6f, 0.0f, 0.3f, 0.2f, 0.5f) },

            { TerrainType.Jungle, new TerrainData(
                TerrainType.Jungle, "丛林",
                "res://Assets/Textures/World/terrains/Jungle.png",
                new Color(0.10f, 0.35f, 0.15f),
                0.2f, 0.5f, 0.5f, 0.9f, 0.6f, 1.0f) }
        };

        /// <summary>
        /// 灵气等级数据
        /// </summary>
        public static readonly Dictionary<SpiritLevel, SpiritData> SpiritLevels = new()
        {
            { SpiritLevel.Desolate, new SpiritData(SpiritLevel.Desolate, "绝灵", 0.02f, new Color(0.50f, 0.45f, 0.40f)) },
            { SpiritLevel.Barren, new SpiritData(SpiritLevel.Barren, "贫瘠", 0.12f, new Color(0.55f, 0.50f, 0.45f)) },
            { SpiritLevel.Sparse, new SpiritData(SpiritLevel.Sparse, "稀薄", 0.25f, new Color(0.50f, 0.55f, 0.50f)) },
            { SpiritLevel.Normal, new SpiritData(SpiritLevel.Normal, "普通", 0.40f, new Color(0.50f, 0.60f, 0.50f)) },
            { SpiritLevel.Rich, new SpiritData(SpiritLevel.Rich, "充裕", 0.60f, new Color(0.45f, 0.65f, 0.55f)) },
            { SpiritLevel.Abundant, new SpiritData(SpiritLevel.Abundant, "浓郁", 0.80f, new Color(0.40f, 0.70f, 0.60f)) },
            { SpiritLevel.Extreme, new SpiritData(SpiritLevel.Extreme, "极致", 1.00f, new Color(0.35f, 0.75f, 0.70f)) }
        };

        /// <summary>
        /// 灵域渲染数据（用于TileMap）
        /// </summary>
        public static readonly Dictionary<RealmType, RealmData> Realms = new()
        {
            // 普通灵气等级
            { RealmType.Desolate, new RealmData(
                RealmType.Desolate, "绝灵荒漠",
                "res://Assets/Textures/World/realms/Desolate.png",
                0.02f, ElementTag.None, LawTag.None,
                new Color(0.76f, 0.60f, 0.42f)) },

            { RealmType.Barren, new RealmData(
                RealmType.Barren, "贫瘠荒原",
                "res://Assets/Textures/World/realms/Barren.png",
                0.12f, ElementTag.Earth, LawTag.None,
                new Color(0.55f, 0.45f, 0.35f)) },

            { RealmType.Sparse, new RealmData(
                RealmType.Sparse, "灵气稀疏",
                "res://Assets/Textures/World/realms/Sparse.png",
                0.25f, ElementTag.None, LawTag.None,
                new Color(0.50f, 0.55f, 0.45f)) },

            { RealmType.Normal, new RealmData(
                RealmType.Normal, "普通之地",
                "res://Assets/Textures/World/realms/Normal.png",
                0.40f, ElementTag.None, LawTag.None,
                new Color(0.40f, 0.60f, 0.35f)) },

            { RealmType.Rich, new RealmData(
                RealmType.Rich, "灵气充裕",
                "res://Assets/Textures/World/realms/Rich.png",
                0.60f, ElementTag.None, LawTag.None,
                new Color(0.35f, 0.65f, 0.45f)) },

            { RealmType.Abundant, new RealmData(
                RealmType.Abundant, "灵气浓郁",
                "res://Assets/Textures/World/realms/Abundant.png",
                0.80f, ElementTag.None, LawTag.None,
                new Color(0.30f, 0.70f, 0.55f)) },

            // 特殊元素地形
            { RealmType.MetalPeak, new RealmData(
                RealmType.MetalPeak, "庚金剑峰",
                "res://Assets/Textures/World/realms/MetalPeak.png",
                0.55f, ElementTag.Metal, LawTag.None,
                new Color(0.75f, 0.75f, 0.80f)) },

            { RealmType.WoodForest, new RealmData(
                RealmType.WoodForest, "乙木森林",
                "res://Assets/Textures/World/realms/WoodForest.png",
                0.65f, ElementTag.Wood, LawTag.LifeDeath,
                new Color(0.15f, 0.50f, 0.20f)) },

            { RealmType.WaterAbyss, new RealmData(
                RealmType.WaterAbyss, "壬水深渊",
                "res://Assets/Textures/World/realms/WaterAbyss.png",
                0.60f, ElementTag.Water, LawTag.Chaos,
                new Color(0.20f, 0.25f, 0.55f)) },

            { RealmType.FireLava, new RealmData(
                RealmType.FireLava, "丙火熔岩",
                "res://Assets/Textures/World/realms/FireLava.png",
                0.70f, ElementTag.Fire, LawTag.Chaos,
                new Color(0.80f, 0.30f, 0.10f)) },

            { RealmType.WindCanyon, new RealmData(
                RealmType.WindCanyon, "巽风峡谷",
                "res://Assets/Textures/World/realms/WindCanyon.png",
                0.50f, ElementTag.Wind, LawTag.Boundary,
                new Color(0.65f, 0.70f, 0.65f)) },

            { RealmType.ThunderRealm, new RealmData(
                RealmType.ThunderRealm, "癸雷泽国",
                "res://Assets/Textures/World/realms/ThunderRealm.png",
                0.65f, ElementTag.Thunder, LawTag.ForceField,
                new Color(0.50f, 0.40f, 0.70f)) },

            { RealmType.LightHoly, new RealmData(
                RealmType.LightHoly, "离光圣地",
                "res://Assets/Textures/World/realms/LightHoly.png",
                0.80f, ElementTag.Light, LawTag.Boundary,
                new Color(0.90f, 0.85f, 0.60f)) },

            { RealmType.DarkAbyss, new RealmData(
                RealmType.DarkAbyss, "坎幽冥域",
                "res://Assets/Textures/World/realms/DarkAbyss.png",
                0.40f, ElementTag.Dark, LawTag.Mental,
                new Color(0.15f, 0.15f, 0.25f)) },

            { RealmType.IcePlain, new RealmData(
                RealmType.IcePlain, "坤寒冰原",
                "res://Assets/Textures/World/realms/IcePlain.png",
                0.55f, ElementTag.Ice, LawTag.Time,
                new Color(0.70f, 0.85f, 0.95f)) },

            { RealmType.SoundValley, new RealmData(
                RealmType.SoundValley, "震音回谷",
                "res://Assets/Textures/World/realms/SoundValley.png",
                0.50f, ElementTag.Sound, LawTag.Illusion,
                new Color(0.60f, 0.55f, 0.50f)) },

            { RealmType.CrystalCave, new RealmData(
                RealmType.CrystalCave, "兑晶洞穴",
                "res://Assets/Textures/World/realms/CrystalCave.png",
                0.60f, ElementTag.Crystal, LawTag.Space,
                new Color(0.55f, 0.65f, 0.70f)) },

            { RealmType.SwampForest, new RealmData(
                RealmType.SwampForest, "艮泽瘴林",
                "res://Assets/Textures/World/realms/SwampForest.png",
                0.45f, ElementTag.Swamp, LawTag.Chaos,
                new Color(0.25f, 0.35f, 0.25f)) },

            // 法则显化区
            { RealmType.DaoRealm, new RealmData(
                RealmType.DaoRealm, "福地洞天",
                "res://Assets/Textures/World/realms/DaoRealm.png",
                0.90f, ElementTag.None, LawTag.Space,
                new Color(0.60f, 0.50f, 0.70f)) }
        };

        /// <summary>
        /// 元素颜色映射
        /// </summary>
        public static readonly Dictionary<ElementTag, Color> ElementColors = new()
        {
            { ElementTag.Metal, new Color(0.80f, 0.80f, 0.85f) },
            { ElementTag.Wood, new Color(0.20f, 0.55f, 0.25f) },
            { ElementTag.Water, new Color(0.25f, 0.35f, 0.65f) },
            { ElementTag.Fire, new Color(0.85f, 0.35f, 0.15f) },
            { ElementTag.Earth, new Color(0.60f, 0.50f, 0.30f) },
            { ElementTag.Wind, new Color(0.70f, 0.75f, 0.70f) },
            { ElementTag.Thunder, new Color(0.55f, 0.45f, 0.75f) },
            { ElementTag.Light, new Color(0.95f, 0.90f, 0.65f) },
            { ElementTag.Dark, new Color(0.15f, 0.15f, 0.30f) },
            { ElementTag.Ice, new Color(0.75f, 0.90f, 1.00f) },
            { ElementTag.Sound, new Color(0.65f, 0.60f, 0.55f) },
            { ElementTag.Crystal, new Color(0.60f, 0.70f, 0.75f) },
            { ElementTag.Swamp, new Color(0.30f, 0.40f, 0.30f) }
        };

        /// <summary>
        /// 法则颜色映射
        /// </summary>
        public static readonly Dictionary<LawTag, Color> LawColors = new()
        {
            { LawTag.Space, new Color(0.50f, 0.40f, 0.70f) },
            { LawTag.Time, new Color(0.70f, 0.50f, 0.80f) },
            { LawTag.LifeDeath, new Color(0.40f, 0.70f, 0.40f) },
            { LawTag.Fate, new Color(0.80f, 0.60f, 0.30f) },
            { LawTag.ForceField, new Color(0.60f, 0.30f, 0.70f) },
            { LawTag.Mental, new Color(0.50f, 0.30f, 0.60f) },
            { LawTag.Chaos, new Color(0.40f, 0.40f, 0.50f) },
            { LawTag.Illusion, new Color(0.65f, 0.50f, 0.70f) },
            { LawTag.Boundary, new Color(0.75f, 0.65f, 0.55f) }
        };

        /// <summary>
        /// 过渡类型默认配置
        /// </summary>
        public static readonly Dictionary<TransitionType, TransitionProfile> DefaultTransitions = new()
        {
            { TransitionType.Gradient, new TransitionProfile(TransitionType.Gradient, 15, 0.3f, 4.0f) },
            { TransitionType.Stepped, new TransitionProfile(TransitionType.Stepped, 7, 0.5f, 2.0f) },
            { TransitionType.Barrier, new TransitionProfile(TransitionType.Barrier, 2, 0.8f, 0.5f) }
        };

        /// <summary>
        /// 获取地形数据
        /// </summary>
        public static TerrainData GetTerrainData(TerrainType type)
        {
            if (Terrains.TryGetValue(type, out var data))
            {
                return data;
            }
            return new TerrainData(
                type, type.ToString(),
                "res://Assets/Textures/World/terrains/Default.png",
                new Color(0.5f, 0.5f, 0.5f),
                0.0f, 1.0f, 0.0f, 1.0f, 0.0f, 1.0f);
        }

        /// <summary>
        /// 获取灵气等级数据
        /// </summary>
        public static SpiritData GetSpiritData(SpiritLevel level)
        {
            if (SpiritLevels.TryGetValue(level, out var data))
            {
                return data;
            }
            return new SpiritData(level, level.ToString(), 0.5f, new Color(0.5f, 0.5f, 0.5f));
        }

        /// <summary>
        /// 获取灵域数据
        /// </summary>
        public static RealmData GetRealmData(RealmType type)
        {
            if (Realms.TryGetValue(type, out var data))
            {
                return data;
            }
            return new RealmData(
                type, type.ToString(),
                "res://Assets/Textures/World/realms/Default.png",
                0.5f, ElementTag.None, LawTag.None,
                new Color(0.5f, 0.5f, 0.5f));
        }

        /// <summary>
        /// 根据灵气等级获取对应的 RealmType
        /// </summary>
        public static RealmType GetRealmFromSpirit(SpiritLevel spirit)
        {
            return spirit switch
            {
                SpiritLevel.Desolate => RealmType.Desolate,
                SpiritLevel.Barren => RealmType.Barren,
                SpiritLevel.Sparse => RealmType.Sparse,
                SpiritLevel.Normal => RealmType.Normal,
                SpiritLevel.Rich => RealmType.Rich,
                SpiritLevel.Abundant => RealmType.Abundant,
                SpiritLevel.Extreme => RealmType.Abundant,
                _ => RealmType.Normal
            };
        }

        /// <summary>
        /// 计算组合颜色（地形颜色 + 灵气颜色）
        /// </summary>
        public static Color GetCombinedColor(TerrainType terrain, SpiritLevel spirit)
        {
            var terrainColor = GetTerrainData(terrain).MapColor;
            var spiritColor = GetSpiritData(spirit).MapColor;

            return new Color(
                (terrainColor.R + spiritColor.R) / 2f,
                (terrainColor.G + spiritColor.G) / 2f,
                (terrainColor.B + spiritColor.B) / 2f);
        }
    }
}
