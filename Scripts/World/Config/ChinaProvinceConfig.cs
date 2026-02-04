using Godot;
using System;
using System.Collections.Generic;

namespace TianYanShop.World.Config
{
    /// <summary>
    /// 中国省份地形参数配置
    /// 每个省份都有独特的地形特征
    /// </summary>
    public class ChinaProvinceConfig
    {
        // 省份名称
        public string Name { get; private set; }

        // 简称
        public string Abbreviation { get; private set; }

        // 地形特征描述
        public string Description { get; private set; }

        // 省份地形类型
        public ProvinceTerrainType TerrainType { get; private set; }

        // 海洋阈值（低于此值为水域）
        public float OceanThreshold { get; private set; }

        // 浅海阈值
        public float ShallowWaterThreshold { get; private set; }

        // 低地阈值
        public float LowlandThreshold { get; private set; }

        // 丘陵阈值
        public float HillThreshold { get; private set; }

        // 山地阈值
        public float MountainThreshold { get; private set; }

        // 高原阈值
        public float PlateauThreshold { get; private set; }

        // 基础降雨量（影响沙漠vs森林）
        public float BaseRainfall { get; private set; }

        // 基础温度（影响寒冷地区分布）
        public float BaseTemperature { get; private set; }

        // 湖泊数量
        public int LakeCount { get; private set; }

        // 湖泊平均大小
        public float LakeSize { get; private set; }

        // 森林覆盖率
        public float ForestRatio { get; private set; }

        // 沙漠覆盖率
        public float DesertRatio { get; private set; }

        // 山地覆盖率
        public float MountainRatio { get; private set; }

        // 平原覆盖率
        public float PlainRatio { get; private set; }

        // 灵气浓郁度 (0-1)
        public float SpiritDensity { get; private set; }

        // 特殊区域类型
        public SpecialRegionType SpecialRegionType { get; private set; }

        // 是否有洞天福地
        public bool HasCaveParadise { get; private set; }

        // 是否有上古遗迹
        public bool HasAncientRuins { get; private set; }

        // 是否有灵脉
        public bool HasSpiritVeins { get; private set; }

        // 是否有妖兽出没
        public bool HasMonsterActivity { get; private set; }

        // 灵气特征描述
        public string SpiritDescription { get; private set; }

        // 特殊区域描述
        public string SpecialRegionDescription { get; private set; }

        // 构造函数
        public ChinaProvinceConfig(string name, string abbrev, string desc, ProvinceTerrainType type,
            float ocean, float shallowWater, float lowland, float hill, float mountain, float plateau,
            float rainfall, float temperature, int lakeCount, float lakeSize,
            float forest, float desert, float mountainRatio, float plainRatio,
            float spiritDensity = 0.5f, SpecialRegionType specialType = SpecialRegionType.None,
            bool hasCave = false, bool hasRuins = false, bool hasVeins = false, bool hasMonster = false,
            string spiritDesc = "", string specialDesc = "")
        {
            Name = name;
            Abbreviation = abbrev;
            Description = desc;
            TerrainType = type;
            OceanThreshold = ocean;
            ShallowWaterThreshold = shallowWater;
            LowlandThreshold = lowland;
            HillThreshold = hill;
            MountainThreshold = mountain;
            PlateauThreshold = plateau;
            BaseRainfall = rainfall;
            BaseTemperature = temperature;
            LakeCount = lakeCount;
            LakeSize = lakeSize;
            ForestRatio = forest;
            DesertRatio = desert;
            MountainRatio = mountainRatio;
            PlainRatio = plainRatio;
            SpiritDensity = spiritDensity;
            SpecialRegionType = specialType;
            HasCaveParadise = hasCave;
            HasAncientRuins = hasRuins;
            HasSpiritVeins = hasVeins;
            HasMonsterActivity = hasMonster;
            SpiritDescription = spiritDesc;
            SpecialRegionDescription = specialDesc;
        }

        // 获取修改后的降雨量（带噪声）
        public float GetModifiedRainfall(float noise, int x, int y)
        {
            float modification = noise * 0.25f;
            float latFactor = Mathf.Abs((float)y / 1000f - 0.5f) * 0.5f;
            return Mathf.Clamp(BaseRainfall + modification - latFactor, 0f, 1f);
        }

        // 获取修改后的温度（带噪声）
        public float GetModifiedTemperature(float noise, int x, int y)
        {
            float modification = noise * 0.15f;
            float latFactor = Mathf.Abs((float)y / 1000f - 0.5f) * 0.8f;
            return Mathf.Clamp(BaseTemperature - latFactor + modification, 0f, 1f);
        }

        // 获取修改后的高程（带省份特征）
        public float GetModifiedElevation(float noise, int x, int y)
        {
            float modification = noise * 0.25f;

            // 根据省份类型调整 - 放大效果
            float typeMod = 0f;
            switch (TerrainType)
            {
                case ProvinceTerrainType.Plateau:
                    typeMod = 0.25f;
                    break;
                case ProvinceTerrainType.Mountain:
                    typeMod = 0.20f;
                    break;
                case ProvinceTerrainType.Plain:
                    typeMod = -0.20f;
                    break;
                case ProvinceTerrainType.Coastal:
                    typeMod = -0.12f;
                    break;
                case ProvinceTerrainType.Basin:
                    typeMod = 0.15f;
                    break;
                case ProvinceTerrainType.Mixed:
                    typeMod = 0f;
                    break;
            }

            return Mathf.Clamp(noise + typeMod + modification, 0f, 1f);
        }
    }

    /// <summary>
    /// 省份地形类型分类
    /// </summary>
    public enum ProvinceTerrainType
    {
        Plain,           // 平原为主
        Mountain,        // 山地为主
        Plateau,         // 高原为主
        Coastal,         // 沿海地区
        Basin,           // 盆地
        Mixed            // 混合地形
    }

    /// <summary>
    /// 特殊区域类型
    /// </summary>
    public enum SpecialRegionType
    {
        None,
        AncientBattlefield,    // 古战场
        SacredMountain,        // 圣山
        ForbiddenLand,         // 禁地
        SpiritValley,          // 灵谷
        DragonLair,            // 龙穴
        FairyResidence,        // 仙境
        DemonicRealm,          // 魔域
        AncientTomb,           // 古墓
        SpiritForest,          // 灵林
        FloatingIsland         // 浮空岛
    }

    /// <summary>
    /// 省份配置管理器
    /// </summary>
    public static class ProvinceConfigManager
    {
        private static Dictionary<string, ChinaProvinceConfig> _provinces;

        public static void Initialize()
        {
            _provinces = new Dictionary<string, ChinaProvinceConfig>();

            // 东北地区 - 寒带森林为主
            AddProvince(new ChinaProvinceConfig(
                "黑龙江省", "黑",
                "东北极寒之地，拥有大兴安岭、小兴安岭，冬季漫长",
                ProvinceTerrainType.Mountain,
                0.25f, 0.30f, 0.40f, 0.55f, 0.75f, 0.88f,
                0.50f, 0.25f,
                3, 5f,
                0.55f, 0.02f, 0.40f, 0.03f,
                0.45f, SpecialRegionType.SacredMountain, true, false, true, true,
                "极寒灵气凝聚，适合冰系功法修炼", "长白山传为上古神山，有元婴修士遗迹"
            ));

            AddProvince(new ChinaProvinceConfig(
                "吉林省", "吉",
                "东北平原与长白山脉过渡地带，松花江穿流而过",
                ProvinceTerrainType.Mixed,
                0.24f, 0.29f, 0.40f, 0.55f, 0.72f, 0.85f,
                0.55f, 0.35f,
                3, 5f,
                0.50f, 0.02f, 0.35f, 0.13f,
                0.40f, SpecialRegionType.SpiritValley, true, true, true, false,
                "灵气分布均衡，山水相依", "松花江底有上古龙宫遗迹"
            ));

            AddProvince(new ChinaProvinceConfig(
                "辽宁省", "辽",
                "东北平原南端，濒临渤海与黄海，千山山脉纵贯",
                ProvinceTerrainType.Coastal,
                0.18f, 0.24f, 0.36f, 0.50f, 0.68f, 0.82f,
                0.60f, 0.45f,
                5, 6f,
                0.38f, 0.02f, 0.30f, 0.30f,
                0.50f, SpecialRegionType.AncientBattlefield, false, true, true, false,
                "沿海灵气活跃，海风携带着丰富灵气", "辽东古战场，残留无数兵修执念"
            ));

            // 华北地区 - 平原与山地过渡
            AddProvince(new ChinaProvinceConfig(
                "河北省", "冀",
                "华北平原北缘，燕山山脉横亘北部，拥有坝上高原",
                ProvinceTerrainType.Mixed,
                0.22f, 0.28f, 0.38f, 0.52f, 0.72f, 0.85f,
                0.50f, 0.48f,
                2, 4f,
                0.28f, 0.10f, 0.35f, 0.27f,
                0.42f, SpecialRegionType.SacredMountain, true, true, false, false,
                "燕山山脉蕴含丰富灵矿", "清东陵有上古阵法守护"
            ));

            AddProvince(new ChinaProvinceConfig(
                "山西省", "晋",
                "黄土高原东部，表里山河，吕梁山脉与太行山脉夹峙",
                ProvinceTerrainType.Mountain,
                0.28f, 0.34f, 0.45f, 0.60f, 0.78f, 0.90f,
                0.40f, 0.45f,
                1, 2f,
                0.15f, 0.20f, 0.50f, 0.15f,
                0.35f, SpecialRegionType.AncientTomb, false, true, true, true,
                "黄土之下蕴含远古灵气", "晋商古墓群中有修士洞府"
            ));

            AddProvince(new ChinaProvinceConfig(
                "内蒙古自治区", "蒙",
                "辽阔的内蒙古高原，草原与沙漠并存，阴山横贯",
                ProvinceTerrainType.Plateau,
                0.30f, 0.38f, 0.48f, 0.60f, 0.75f, 0.88f,
                0.30f, 0.38f,
                12, 12f,
                0.10f, 0.25f, 0.15f, 0.50f,
                0.30f, SpecialRegionType.AncientBattlefield, false, true, true, true,
                "草原灵气纯正，适合体修", "阴山古战场遗留魔道气息"
            ));

            AddProvince(new ChinaProvinceConfig(
                "北京市", "京",
                "华北平原北缘，西北有燕山余脉山地",
                ProvinceTerrainType.Mountain,
                0.24f, 0.30f, 0.40f, 0.55f, 0.72f, 0.85f,
                0.50f, 0.50f,
                2, 3f,
                0.25f, 0.08f, 0.40f, 0.27f,
                0.48f, SpecialRegionType.AncientBattlefield, false, true, false, false,
                "帝都之气凝聚，灵气混杂人皇气运", "八达岭有古修士布下的守护大阵"
            ));

            AddProvince(new ChinaProvinceConfig(
                "天津市", "津",
                "华北平原东端，濒临渤海",
                ProvinceTerrainType.Plain,
                0.16f, 0.22f, 0.32f, 0.45f, 0.62f, 0.78f,
                0.58f, 0.55f,
                4, 5f,
                0.18f, 0.02f, 0.12f, 0.68f,
                0.45f, SpecialRegionType.DragonLair, false, false, true, false,
                "海陆交汇处灵气活跃", "渤海深处传有龙宫入口"
            ));

            AddProvince(new ChinaProvinceConfig(
                "河南省", "豫",
                "中原腹地，黄淮海平原主体，华北平原最南端",
                ProvinceTerrainType.Plain,
                0.20f, 0.26f, 0.35f, 0.48f, 0.65f, 0.80f,
                0.62f, 0.60f,
                5, 7f,
                0.22f, 0.05f, 0.10f, 0.63f,
                0.52f, SpecialRegionType.AncientTomb, true, true, true, false,
                "中原腹地，灵气底蕴深厚", "嵩山少林有上古传承，龙门石窟封印着远古大妖"
            ));

            // 西北地区 - 干旱沙漠与高原
            AddProvince(new ChinaProvinceConfig(
                "陕西省", "陕",
                "南北狭长，北部黄土高原，南部秦巴山地",
                ProvinceTerrainType.Mixed,
                0.26f, 0.32f, 0.42f, 0.55f, 0.72f, 0.85f,
                0.48f, 0.50f,
                2, 4f,
                0.30f, 0.15f, 0.35f, 0.20f,
                0.45f, SpecialRegionType.AncientBattlefield, true, true, false, false,
                "秦岭阻挡南北灵气交汇", "秦陵地宫有始皇帝封印"
            ));

            AddProvince(new ChinaProvinceConfig(
                "甘肃省", "甘",
                "狭长走廊地形，河西走廊与陇南山地对比鲜明",
                ProvinceTerrainType.Mixed,
                0.32f, 0.40f, 0.50f, 0.62f, 0.78f, 0.90f,
                0.25f, 0.42f,
                5, 8f,
                0.08f, 0.35f, 0.35f, 0.22f,
                0.28f, SpecialRegionType.SacredMountain, true, true, true, false,
                "祁连山雪水蕴含纯净灵气", "敦煌莫高窟封印着西域佛法"
            ));

            AddProvince(new ChinaProvinceConfig(
                "青海省", "青",
                "青藏高原主体，世界屋脊，昆仑山脉、祁连山脉环绕",
                ProvinceTerrainType.Plateau,
                0.38f, 0.45f, 0.55f, 0.68f, 0.80f, 0.92f,
                0.30f, 0.25f,
                20, 15f,
                0.05f, 0.12f, 0.50f, 0.33f,
                0.55f, SpecialRegionType.SacredMountain, true, true, true, false,
                "世界屋脊，灵气稀薄但极为精纯", "昆仑山传为万山之祖，有上古仙境入口"
            ));

            AddProvince(new ChinaProvinceConfig(
                "宁夏回族自治区", "宁",
                "贺兰山与六盘山夹峙的宁夏平原",
                ProvinceTerrainType.Plain,
                0.30f, 0.38f, 0.48f, 0.60f, 0.75f, 0.88f,
                0.28f, 0.45f,
                2, 4f,
                0.05f, 0.30f, 0.20f, 0.45f,
                0.32f, SpecialRegionType.ForbiddenLand, false, true, false, true,
                "西夏旧地，灵气混杂着古老诅咒", "贺兰山岩画封印着远古凶兽"
            ));

            AddProvince(new ChinaProvinceConfig(
                "新疆维吾尔自治区", "新",
                "三山夹两盆，塔克拉玛干与古尔班通古特沙漠环绕",
                ProvinceTerrainType.Plateau,
                0.35f, 0.42f, 0.52f, 0.65f, 0.78f, 0.90f,
                0.18f, 0.40f,
                25, 18f,
                0.03f, 0.55f, 0.35f, 0.07f,
                0.25f, SpecialRegionType.ForbiddenLand, true, true, true, true,
                "极度干燥，灵气稀薄", "塔克拉玛干深处有上古魔域遗迹"
            ));

            // 华东地区 - 平原水乡与东南丘陵
            AddProvince(new ChinaProvinceConfig(
                "山东省", "鲁",
                "华北平原东端，濒临黄海与渤海，泰山雄踞中部",
                ProvinceTerrainType.Plain,
                0.18f, 0.24f, 0.34f, 0.48f, 0.65f, 0.80f,
                0.58f, 0.52f,
                4, 6f,
                0.22f, 0.03f, 0.18f, 0.57f,
                0.50f, SpecialRegionType.SacredMountain, true, true, true, false,
                "儒家文气与灵气交融", "泰山封禅台蕴含人皇气运"
            ));

            AddProvince(new ChinaProvinceConfig(
                "江苏省", "苏",
                "长江三角洲平原主体，河网密布",
                ProvinceTerrainType.Plain,
                0.15f, 0.22f, 0.30f, 0.42f, 0.58f, 0.75f,
                0.72f, 0.62f,
                10, 10f,
                0.18f, 0.0f, 0.08f, 0.74f,
                0.55f, SpecialRegionType.SpiritValley, false, true, true, false,
                "水系发达，水属性灵气充沛", "茅山为道教圣地，有上古传承"
            ));

            AddProvince(new ChinaProvinceConfig(
                "安徽省", "皖",
                "皖中丘陵与皖北平原对比，黄山雄踞南部",
                ProvinceTerrainType.Mixed,
                0.20f, 0.26f, 0.36f, 0.50f, 0.68f, 0.82f,
                0.65f, 0.58f,
                4, 6f,
                0.32f, 0.02f, 0.30f, 0.36f,
                0.52f, SpecialRegionType.SacredMountain, true, true, true, false,
                "黄山云海蕴含天地灵气", "九华山地藏王菩萨道场"
            ));

            AddProvince(new ChinaProvinceConfig(
                "上海市", "沪",
                "长江三角洲冲积平原，海岸线绵长",
                ProvinceTerrainType.Plain,
                0.12f, 0.18f, 0.28f, 0.40f, 0.55f, 0.72f,
                0.75f, 0.65f,
                6, 8f,
                0.10f, 0.0f, 0.05f, 0.85f,
                0.48f, SpecialRegionType.DragonLair, false, false, false, false,
                "海陆交汇，灵气混杂", "东海有龙族分支栖息"
            ));

            AddProvince(new ChinaProvinceConfig(
                "浙江省", "浙",
                "东南沿海，七山一水二分田，山地丘陵为主",
                ProvinceTerrainType.Mountain,
                0.16f, 0.24f, 0.36f, 0.52f, 0.70f, 0.85f,
                0.75f, 0.68f,
                5, 7f,
                0.60f, 0.0f, 0.32f, 0.08f,
                0.62f, SpecialRegionType.FairyResidence, true, true, true, false,
                "山林茂密，木属性灵气极为充沛", "西湖有白娘子传说，实为水妖一族"
            ));

            AddProvince(new ChinaProvinceConfig(
                "江西省", "赣",
                "鄱阳湖平原与赣南山地丘陵对比",
                ProvinceTerrainType.Mixed,
                0.18f, 0.26f, 0.38f, 0.52f, 0.70f, 0.84f,
                0.68f, 0.60f,
                10, 12f,
                0.52f, 0.02f, 0.28f, 0.18f,
                0.50f, SpecialRegionType.SpiritValley, true, true, true, false,
                "山水相依，灵气分布均衡", "龙虎山为道教祖庭之一"
            ));

            AddProvince(new ChinaProvinceConfig(
                "福建省", "闽",
                "武夷山脉东侧，沿海丘陵与山地交错",
                ProvinceTerrainType.Mountain,
                0.16f, 0.24f, 0.36f, 0.52f, 0.70f, 0.85f,
                0.72f, 0.70f,
                4, 6f,
                0.62f, 0.0f, 0.35f, 0.03f,
                0.58f, SpecialRegionType.AncientBattlefield, false, true, true, false,
                "海风带来充沛灵气", "闽南古战场遗留无数法宝"
            ));

            // 华中地区 - 湖泊与丘陵
            AddProvince(new ChinaProvinceConfig(
                "湖北省", "鄂",
                "江汉平原与鄂西山地对比，长江横贯",
                ProvinceTerrainType.Mixed,
                0.18f, 0.26f, 0.36f, 0.50f, 0.68f, 0.82f,
                0.68f, 0.62f,
                12, 12f,
                0.42f, 0.02f, 0.22f, 0.34f,
                0.52f, SpecialRegionType.AncientTomb, true, true, true, false,
                "千湖之省，水属性灵气充沛", "荆州古城有上古阵法，神农架传有药王遗迹"
            ));

            AddProvince(new ChinaProvinceConfig(
                "湖南省", "湘",
                "湘江流域，洞庭湖平原与湘南丘陵对比",
                ProvinceTerrainType.Mixed,
                0.18f, 0.26f, 0.38f, 0.52f, 0.70f, 0.84f,
                0.70f, 0.65f,
                10, 10f,
                0.48f, 0.02f, 0.25f, 0.25f,
                0.50f, SpecialRegionType.SpiritValley, true, true, false, false,
                "湘楚之地，灵气带有诗意", "张家界有上古仙境遗迹，洞庭湖底有龙宫"
            ));

            AddProvince(new ChinaProvinceConfig(
                "广东省", "粤",
                "岭南丘陵与珠三角平原，南海之滨",
                ProvinceTerrainType.Mountain,
                0.15f, 0.22f, 0.35f, 0.52f, 0.70f, 0.84f,
                0.80f, 0.82f,
                5, 7f,
                0.55f, 0.0f, 0.30f, 0.15f,
                0.60f, SpecialRegionType.DemonicRealm, false, true, true, true,
                "岭南炎热，火雷灵气活跃", "罗浮山有上古修士洞府，南海有鲛人出没"
            ));

            AddProvince(new ChinaProvinceConfig(
                "广西壮族自治区", "桂",
                "云贵高原东南边缘，喀斯特地貌与丘陵",
                ProvinceTerrainType.Mountain,
                0.20f, 0.28f, 0.40f, 0.56f, 0.74f, 0.88f,
                0.72f, 0.75f,
                4, 6f,
                0.52f, 0.02f, 0.38f, 0.08f,
                0.55f, SpecialRegionType.ForbiddenLand, true, true, true, true,
                "喀斯特地貌蕴含神秘力量", "桂林山水有上古封印，十万大山深处有凶兽"
            ));

            // 西南地区 - 高原与盆地对比
            AddProvince(new ChinaProvinceConfig(
                "重庆", "渝",
                "四川盆地东部，平行岭谷与丘陵",
                ProvinceTerrainType.Mountain,
                0.20f, 0.28f, 0.40f, 0.55f, 0.72f, 0.86f,
                0.62f, 0.62f,
                3, 5f,
                0.42f, 0.05f, 0.38f, 0.15f,
                0.48f, SpecialRegionType.AncientBattlefield, false, true, true, false,
                "山城雾气蕴含独特灵气", "丰都传为鬼界入口"
            ));

            AddProvince(new ChinaProvinceConfig(
                "四川省", "川",
                "四川盆地与川西高原雪山对比",
                ProvinceTerrainType.Mixed,
                0.24f, 0.32f, 0.44f, 0.58f, 0.74f, 0.88f,
                0.58f, 0.55f,
                10, 12f,
                0.38f, 0.05f, 0.42f, 0.15f,
                0.58f, SpecialRegionType.SacredMountain, true, true, true, false,
                "蜀山剑派传说之地，剑修圣地", "青城山为道教祖庭，峨眉山有佛道传承"
            ));

            AddProvince(new ChinaProvinceConfig(
                "贵州省", "黔",
                "云贵高原东部，喀斯特地貌王国",
                ProvinceTerrainType.Mountain,
                0.24f, 0.32f, 0.44f, 0.58f, 0.75f, 0.88f,
                0.68f, 0.58f,
                4, 6f,
                0.48f, 0.02f, 0.42f, 0.08f,
                0.50f, SpecialRegionType.ForbiddenLand, true, true, true, true,
                "苗疆蛊毒与灵气交织", "梵净山有上古封印，黔东南有巫族传承"
            ));

            AddProvince(new ChinaProvinceConfig(
                "云南省", "滇",
                "云贵高原主体，从热带雨林到雪山冰川",
                ProvinceTerrainType.Plateau,
                0.30f, 0.38f, 0.50f, 0.64f, 0.78f, 0.90f,
                0.68f, 0.62f,
                12, 14f,
                0.52f, 0.03f, 0.38f, 0.07f,
                0.60f, SpecialRegionType.FairyResidence, true, true, true, false,
                "动植物种类繁多，木灵气充沛", "大理有段氏皇族传承，玉龙雪山传有仙境"
            ));

            AddProvince(new ChinaProvinceConfig(
                "西藏自治区", "藏",
                "世界屋脊青藏高原核心，珠穆朗玛峰所在地",
                ProvinceTerrainType.Plateau,
                0.42f, 0.50f, 0.60f, 0.72f, 0.85f, 0.95f,
                0.32f, 0.20f,
                30, 20f,
                0.02f, 0.08f, 0.70f, 0.20f,
                0.70f, SpecialRegionType.SacredMountain, true, true, true, false,
                "世界屋脊，灵气稀薄但最为精纯", "布达拉宫有上古封印，珠峰传为天界入口"
            ));

            // 特别行政区及台湾省
            AddProvince(new ChinaProvinceConfig(
                "香港特别行政区", "港",
                "珠江口东侧，丘陵海岸地貌",
                ProvinceTerrainType.Mountain,
                0.12f, 0.18f, 0.30f, 0.48f, 0.65f, 0.80f,
                0.80f, 0.82f,
                2, 3f,
                0.50f, 0.0f, 0.45f, 0.05f,
                0.55f, SpecialRegionType.DragonLair, false, false, false, false,
                "海陆交汇，商业繁荣带来混杂灵气", "维多利亚港有龙族分支守护"
            ));

            AddProvince(new ChinaProvinceConfig(
                "澳门特别行政区", "澳",
                "珠江口西侧，沿海低地",
                ProvinceTerrainType.Plain,
                0.10f, 0.16f, 0.26f, 0.38f, 0.55f, 0.72f,
                0.80f, 0.85f,
                2, 3f,
                0.15f, 0.0f, 0.08f, 0.77f,
                0.45f, SpecialRegionType.AncientBattlefield, false, true, false, false,
                "赌场繁荣，灵气混杂人欲", "大三巴有传教士留下的阵法"
            ));

            AddProvince(new ChinaProvinceConfig(
                "台湾省", "台",
                "宝岛台湾，中央山脉纵贯，东部陡峭西部平缓",
                ProvinceTerrainType.Mountain,
                0.15f, 0.22f, 0.34f, 0.50f, 0.68f, 0.82f,
                0.78f, 0.78f,
                6, 8f,
                0.62f, 0.0f, 0.35f, 0.03f,
                0.65f, SpecialRegionType.FairyResidence, true, true, true, false,
                "岛屿气候，温润灵气充沛", "阿里山有原住民传承的灵术"
            ));
        }

        private static void AddProvince(ChinaProvinceConfig province)
        {
            _provinces[province.Abbreviation] = province;
            _provinces[province.Name] = province;
        }

        public static ChinaProvinceConfig GetProvince(string nameOrAbbrev)
        {
            if (_provinces == null)
                Initialize();

            if (_provinces.TryGetValue(nameOrAbbrev, out var province))
                return province;

            // 默认返回混合地形配置
            return _provinces["混合地形"] ?? new ChinaProvinceConfig(
                "默认地区", "默认",
                "混合地形特征",
                ProvinceTerrainType.Mixed,
                0.25f, 0.32f, 0.42f, 0.55f, 0.72f, 0.85f,
                0.55f, 0.55f,
                5, 6f,
                0.35f, 0.05f, 0.30f, 0.30f,
                0.50f, SpecialRegionType.None, false, false, false, false,
                "灵气分布均衡", ""
            );
        }

        public static List<ChinaProvinceConfig> GetAllProvinces()
        {
            if (_provinces == null)
                Initialize();

            var provinces = new List<ChinaProvinceConfig>();
            foreach (var province in _provinces.Values)
            {
                if (!provinces.Contains(province))
                    provinces.Add(province);
            }
            return provinces;
        }

        public static List<string> GetAllProvinceNames()
        {
            var names = new List<string>();
            foreach (var kvp in _provinces)
            {
                if (!names.Contains(kvp.Value.Name))
                    names.Add(kvp.Value.Name);
            }
            return names;
        }
    }
}
