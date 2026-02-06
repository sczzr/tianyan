namespace TianYanShop.MapGeneration.Data
{
    /// <summary>
    /// 地理特征类型枚举
    /// </summary>
    public enum FeatureType
    {
        None = 0,
        Ocean = 1,
        Lake = 2,
        Island = 3,
        Peninsula = 4
    }

    /// <summary>
    /// 地形类型枚举
    /// </summary>
    public enum TerrainType
    {
        Ocean = 0,
        Land = 1,
        Coastline = -1,
        Shore = -2,
        LakeShore = -3
    }

    /// <summary>
    /// 生物群系类型枚举（基于 Whittaker 图）
    /// </summary>
    public enum BiomeType
    {
        Ocean = 0,
        Lake = 1,
        TropicalDesert = 2,
        TemperateDesert = 3,
        ColdDesert = 4,
        TropicalRainforest = 5,
        TropicalSeasonalForest = 6,
        TemperateSeasonalForest = 7,
        TemperateRainforest = 8,
        BorealForest = 9,
        Tundra = 10,
        Snow = 11,
        Mangrove = 12
    }

    /// <summary>
    /// 文化类型枚举
    /// </summary>
    public enum CultureType
    {
        Generic = 0,
        Naval = 1,
        River = 2,
        Nomadic = 3,
        Highland = 4,
        Hunting = 5,
        Lake = 6
    }

    /// <summary>
    /// 国家类型枚举
    /// </summary>
    public enum StateType
    {
        Generic = 0,
        Monarchy = 1,
        Republic = 2,
        Theocracy = 3,
        Anarchy = 4,
        Union = 5
    }

    /// <summary>
    /// 宗教类型枚举
    /// </summary>
    public enum ReligionType
    {
        None = 0,
        Organized = 1,
        Cult = 2,
        Pagan = 3
    }

    /// <summary>
    /// 城市类型枚举
    /// </summary>
    public enum BurgType
    {
        None = 0,
        Village = 1,
        Town = 2,
        City = 3,
        Capital = 4
    }

    /// <summary>
    /// 地图生成状态枚举
    /// </summary>
    public enum GenerationState
    {
        Idle = 0,
        GeneratingGraph = 1,
        GeneratingHeightmap = 2,
        GeneratingFeatures = 3,
        GeneratingBiomes = 4,
        GeneratingRivers = 5,
        GeneratingCultures = 6,
        GeneratingStates = 7,
        GeneratingBurgs = 8,
        GeneratingReligions = 9,
        GeneratingRoutes = 10,
        Complete = 11,
        Error = 12
    }

    /// <summary>
    /// 编辑器工具类型枚举
    /// </summary>
    public enum EditorToolType
    {
        Select = 0,
        HillBrush = 1,
        PitBrush = 2,
        RangeBrush = 3,
        TroughBrush = 4,
        StraitBrush = 5,
        SmoothBrush = 6,
        MaskBrush = 7,
        BurgAdd = 8,
        BurgRemove = 9
    }

    /// <summary>
    /// 显示图层枚举
    /// </summary>
    public enum MapLayer
    {
        Heightmap = 0,
        Biomes = 1,
        Coastline = 2,
        Rivers = 3,
        Lakes = 4,
        Cultures = 5,
        States = 6,
        Borders = 7,
        Burgs = 8,
        Labels = 9,
        Routes = 10
    }
}
