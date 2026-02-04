using Godot;
using System;
using System.Collections.Generic;

namespace TianYanShop.Data
{
    /// <summary>
    /// 基础地形类型（普通山川河海）
    /// </summary>
    public enum TerrainType
    {
        Ocean = 0,
        Beach = 1,
        Plain = 2,
        Hill = 3,
        Mountain = 4,
        Forest = 5,
        Desert = 6,
        Swamp = 7,
        River = 8,
        Lake = 9,
        Canyon = 10,
        Plateau = 11,
        Tundra = 12,
        Jungle = 13
    }

    /// <summary>
    /// 灵气浓度等级
    /// </summary>
    public enum SpiritLevel
    {
        Desolate = 0,
        Barren = 1,
        Sparse = 2,
        Normal = 3,
        Rich = 4,
        Abundant = 5,
        Extreme = 6
    }

    /// <summary>
    /// 元素属性
    /// </summary>
    [Flags]
    public enum ElementTag
    {
        None = 0,
        Metal = 1 << 0,
        Wood = 1 << 1,
        Water = 1 << 2,
        Fire = 1 << 3,
        Earth = 1 << 4,
        Wind = 1 << 5,
        Thunder = 1 << 6,
        Light = 1 << 7,
        Dark = 1 << 8,
        Ice = 1 << 9,
        Sound = 1 << 10,
        Crystal = 1 << 11,
        Swamp = 1 << 12
    }

    /// <summary>
    /// 法则属性
    /// </summary>
    public enum LawTag
    {
        None = 0,
        Space,
        Time,
        LifeDeath,
        Fate,
        ForceField,
        Mental,
        Chaos,
        Illusion,
        Boundary
    }

    /// <summary>
    /// 过渡类型
    /// </summary>
    public enum TransitionType
    {
        Gradient,
        Stepped,
        Barrier
    }

    /// <summary>
    /// 过渡配置
    /// </summary>
    public struct TransitionProfile
    {
        public TransitionType Type;
        public int Width;
        public float ChaosFactor;
        public float BlurRadius;

        public TransitionProfile(TransitionType type, int width, float chaosFactor, float blurRadius)
        {
            Type = type;
            Width = width;
            ChaosFactor = chaosFactor;
            BlurRadius = blurRadius;
        }
    }

    /// <summary>
    /// 地图瓦片数据
    /// </summary>
    public struct RealmTile
    {
        public TerrainType Terrain;
        public SpiritLevel Spirit;
        public RealmType Realm;
        public ElementTag PrimaryElement;
        public LawTag Law;
        public float Elevation;
        public float Temperature;
        public float Rainfall;
        public float SpiritValue;

        public float BlendFactor;
        public TerrainType SecondaryTerrain;
        public SpiritLevel SecondarySpirit;
        public RealmType SecondaryRealm;
        public TransitionType TransitionType;

        public RealmTile(TerrainType terrain, SpiritLevel spirit, float elevation, float temperature, float rainfall)
        {
            Terrain = terrain;
            Spirit = spirit;
            Elevation = elevation;
            Temperature = temperature;
            Rainfall = rainfall;
            SpiritValue = (float)spirit / 6f;
            Realm = RealmType.Normal;
            PrimaryElement = ElementTag.None;
            Law = LawTag.None;
            BlendFactor = 0f;
            SecondaryTerrain = terrain;
            SecondarySpirit = spirit;
            SecondaryRealm = RealmType.Normal;
            TransitionType = TransitionType.Stepped;
        }
    }

    /// <summary>
    /// 灵域类型（渲染用）
    /// </summary>
    public enum RealmType
    {
        Desolate = 0,
        Barren = 1,
        Sparse = 2,
        Normal = 3,
        Rich = 4,
        Abundant = 5,

        MetalPeak = 100,
        WoodForest = 101,
        WaterAbyss = 102,
        FireLava = 103,
        WindCanyon = 104,
        ThunderRealm = 105,
        LightHoly = 106,
        DarkAbyss = 107,
        IcePlain = 108,
        SoundValley = 109,
        CrystalCave = 110,
        SwampForest = 111,

        DaoRealm = 200
    }

    /// <summary>
    /// 基础地形数据
    /// </summary>
    public struct TerrainData
    {
        public TerrainType Type;
        public string Name;
        public string TexturePath;
        public Color MapColor;
        public float MinElevation;
        public float MaxElevation;
        public float MinTemperature;
        public float MaxTemperature;
        public float MinRainfall;
        public float MaxRainfall;

        public TerrainData(TerrainType type, string name, string texturePath, Color color,
            float minElev, float maxElev, float minTemp, float maxTemp, float minRain, float maxRain)
        {
            Type = type;
            Name = name;
            TexturePath = texturePath;
            MapColor = color;
            MinElevation = minElev;
            MaxElevation = maxElev;
            MinTemperature = minTemp;
            MaxTemperature = maxTemp;
            MinRainfall = minRain;
            MaxRainfall = maxRain;
        }
    }

    /// <summary>
    /// 灵气等级数据
    /// </summary>
    public struct SpiritData
    {
        public SpiritLevel Level;
        public string Name;
        public float SpiritValue;
        public Color MapColor;

        public SpiritData(SpiritLevel level, string name, float value, Color color)
        {
            Level = level;
            Name = name;
            SpiritValue = value;
            MapColor = color;
        }
    }

    /// <summary>
    /// 灵域数据（用于渲染）
    /// </summary>
    public struct RealmData
    {
        public RealmType Type;
        public string Name;
        public string TexturePath;
        public float SpiritLevel;
        public ElementTag Element;
        public LawTag Law;
        public Color MapColor;

        public RealmData(RealmType type, string name, string texturePath,
            float spiritLevel, ElementTag element, LawTag law, Color color)
        {
            Type = type;
            Name = name;
            TexturePath = texturePath;
            SpiritLevel = spiritLevel;
            Element = element;
            Law = law;
            MapColor = color;
        }
    }
}
