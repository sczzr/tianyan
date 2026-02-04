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

        // 构造函数
        public ChinaProvinceConfig(string name, string abbrev, string desc, ProvinceTerrainType type,
            float ocean, float shallowWater, float lowland, float hill, float mountain, float plateau,
            float rainfall, float temperature, int lakeCount, float lakeSize,
            float forest, float desert, float mountainRatio, float plainRatio)
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
                0.50f, 0.25f,  // 极低温度
                3, 5f,         // 少量湖泊（镜泊湖等）
                0.55f, 0.02f, 0.40f, 0.03f  // 大片森林，几乎无沙漠，极少平原
            ));

            AddProvince(new ChinaProvinceConfig(
                "吉林省", "吉",
                "东北平原与长白山脉过渡地带，松花江穿流而过",
                ProvinceTerrainType.Mixed,
                0.24f, 0.29f, 0.40f, 0.55f, 0.72f, 0.85f,
                0.55f, 0.35f,
                3, 5f,
                0.50f, 0.02f, 0.35f, 0.13f  // 森林为主
            ));

            AddProvince(new ChinaProvinceConfig(
                "辽宁省", "辽",
                "东北平原南端，濒临渤海与黄海，千山山脉纵贯",
                ProvinceTerrainType.Coastal,
                0.18f, 0.24f, 0.36f, 0.50f, 0.68f, 0.82f,
                0.60f, 0.45f,
                5, 6f,
                0.38f, 0.02f, 0.30f, 0.30f  // 沿海丘陵
            ));

            // 华北地区 - 平原与山地过渡
            AddProvince(new ChinaProvinceConfig(
                "河北省", "冀",
                "华北平原北缘，燕山山脉横亘北部，拥有坝上高原",
                ProvinceTerrainType.Mixed,
                0.22f, 0.28f, 0.38f, 0.52f, 0.72f, 0.85f,
                0.50f, 0.48f,
                2, 4f,
                0.28f, 0.10f, 0.35f, 0.27f  // 混合地形
            ));

            AddProvince(new ChinaProvinceConfig(
                "山西省", "晋",
                "黄土高原东部，表里山河，吕梁山脉与太行山脉夹峙",
                ProvinceTerrainType.Mountain,
                0.28f, 0.34f, 0.45f, 0.60f, 0.78f, 0.90f,
                0.40f, 0.45f,  // 干燥，黄土高原
                1, 2f,         // 极少湖泊
                0.15f, 0.20f, 0.50f, 0.15f  // 森林稀少，黄土与山地为主
            ));

            AddProvince(new ChinaProvinceConfig(
                "内蒙古自治区", "蒙",
                "辽阔的内蒙古高原，草原与沙漠并存，阴山横贯",
                ProvinceTerrainType.Plateau,
                0.30f, 0.38f, 0.48f, 0.60f, 0.75f, 0.88f,
                0.30f, 0.38f,  // 干燥寒冷，高原气候
                12, 12f,       // 众多湖泊（呼伦贝尔、乌兰察布等）
                0.10f, 0.25f, 0.15f, 0.50f  // 草原与沙漠交错
            ));

            AddProvince(new ChinaProvinceConfig(
                "北京市", "京",
                "华北平原北缘，西北有燕山余脉山地",
                ProvinceTerrainType.Mountain,
                0.24f, 0.30f, 0.40f, 0.55f, 0.72f, 0.85f,
                0.50f, 0.50f,
                2, 3f,
                0.25f, 0.08f, 0.40f, 0.27f  // 山地丘陵
            ));

            AddProvince(new ChinaProvinceConfig(
                "天津市", "津",
                "华北平原东端，濒临渤海",
                ProvinceTerrainType.Plain,
                0.16f, 0.22f, 0.32f, 0.45f, 0.62f, 0.78f,
                0.58f, 0.55f,
                4, 5f,
                0.18f, 0.02f, 0.12f, 0.68f  // 平原为主
            ));

            AddProvince(new ChinaProvinceConfig(
                "河南省", "豫",
                "中原腹地，黄淮海平原主体，华北平原最南端",
                ProvinceTerrainType.Plain,
                0.20f, 0.26f, 0.35f, 0.48f, 0.65f, 0.80f,
                0.62f, 0.60f,  // 温暖湿润
                5, 7f,
                0.22f, 0.05f, 0.10f, 0.63f  // 平原农业区
            ));

            // 西北地区 - 干旱沙漠与高原
            AddProvince(new ChinaProvinceConfig(
                "陕西省", "陕",
                "南北狭长，北部黄土高原，南部秦巴山地",
                ProvinceTerrainType.Mixed,
                0.26f, 0.32f, 0.42f, 0.55f, 0.72f, 0.85f,
                0.48f, 0.50f,
                2, 4f,
                0.30f, 0.15f, 0.35f, 0.20f  // 森林与沙漠交错
            ));

            AddProvince(new ChinaProvinceConfig(
                "甘肃省", "甘",
                "狭长走廊地形，河西走廊与陇南山地对比鲜明",
                ProvinceTerrainType.Mixed,
                0.32f, 0.40f, 0.50f, 0.62f, 0.78f, 0.90f,
                0.25f, 0.42f,  // 干旱
                5, 8f,         // 祁连山融雪湖泊
                0.08f, 0.35f, 0.35f, 0.22f  // 戈壁与沙漠为主
            ));

            AddProvince(new ChinaProvinceConfig(
                "青海省", "青",
                "青藏高原主体，世界屋脊，昆仑山脉、祁连山脉环绕",
                ProvinceTerrainType.Plateau,
                0.38f, 0.45f, 0.55f, 0.68f, 0.80f, 0.92f,
                0.30f, 0.25f,  // 高原严寒
                20, 15f,       // 众多高原湖泊
                0.05f, 0.12f, 0.50f, 0.33f  // 高原草甸与裸岩
            ));

            AddProvince(new ChinaProvinceConfig(
                "宁夏回族自治区", "宁",
                "贺兰山与六盘山夹峙的宁夏平原",
                ProvinceTerrainType.Plain,
                0.30f, 0.38f, 0.48f, 0.60f, 0.75f, 0.88f,
                0.28f, 0.45f,  // 干燥
                2, 4f,
                0.05f, 0.30f, 0.20f, 0.45f  // 半沙漠半平原
            ));

            AddProvince(new ChinaProvinceConfig(
                "新疆维吾尔自治区", "新",
                "三山夹两盆，塔克拉玛干与古尔班通古特沙漠环绕",
                ProvinceTerrainType.Plateau,
                0.35f, 0.42f, 0.52f, 0.65f, 0.78f, 0.90f,
                0.18f, 0.40f,  // 极度干燥
                25, 18f,       // 天山湖泊群
                0.03f, 0.55f, 0.35f, 0.07f  // 沙漠为主，绿洲极少
            ));

            // 华东地区 - 平原水乡与东南丘陵
            AddProvince(new ChinaProvinceConfig(
                "山东省", "鲁",
                "华北平原东端，濒临黄海与渤海，泰山雄踞中部",
                ProvinceTerrainType.Plain,
                0.18f, 0.24f, 0.34f, 0.48f, 0.65f, 0.80f,
                0.58f, 0.52f,
                4, 6f,
                0.22f, 0.03f, 0.18f, 0.57f  // 平原为主
            ));

            AddProvince(new ChinaProvinceConfig(
                "江苏省", "苏",
                "长江三角洲平原主体，河网密布",
                ProvinceTerrainType.Plain,
                0.15f, 0.22f, 0.30f, 0.42f, 0.58f, 0.75f,
                0.72f, 0.62f,  // 湿润
                10, 10f,
                0.18f, 0.0f, 0.08f, 0.74f  // 平原水乡
            ));

            AddProvince(new ChinaProvinceConfig(
                "安徽省", "皖",
                "皖中丘陵与皖北平原对比，黄山雄踞南部",
                ProvinceTerrainType.Mixed,
                0.20f, 0.26f, 0.36f, 0.50f, 0.68f, 0.82f,
                0.65f, 0.58f,
                4, 6f,
                0.32f, 0.02f, 0.30f, 0.36f  // 丘陵平原混合
            ));

            AddProvince(new ChinaProvinceConfig(
                "上海市", "沪",
                "长江三角洲冲积平原，海岸线绵长",
                ProvinceTerrainType.Plain,
                0.12f, 0.18f, 0.28f, 0.40f, 0.55f, 0.72f,
                0.75f, 0.65f,
                6, 8f,
                0.10f, 0.0f, 0.05f, 0.85f  // 几乎全为平原
            ));

            AddProvince(new ChinaProvinceConfig(
                "浙江省", "浙",
                "东南沿海，七山一水二分田，山地丘陵为主",
                ProvinceTerrainType.Mountain,
                0.16f, 0.24f, 0.36f, 0.52f, 0.70f, 0.85f,
                0.75f, 0.68f,  // 温暖湿润
                5, 7f,
                0.60f, 0.0f, 0.32f, 0.08f  // 森林覆盖率极高
            ));

            AddProvince(new ChinaProvinceConfig(
                "江西省", "赣",
                "鄱阳湖平原与赣南山地丘陵对比",
                ProvinceTerrainType.Mixed,
                0.18f, 0.26f, 0.38f, 0.52f, 0.70f, 0.84f,
                0.68f, 0.60f,
                10, 12f,       // 鄱阳湖等众多湖泊
                0.52f, 0.02f, 0.28f, 0.18f  // 森林与丘陵
            ));

            AddProvince(new ChinaProvinceConfig(
                "福建省", "闽",
                "武夷山脉东侧，沿海丘陵与山地交错",
                ProvinceTerrainType.Mountain,
                0.16f, 0.24f, 0.36f, 0.52f, 0.70f, 0.85f,
                0.72f, 0.70f,
                4, 6f,
                0.62f, 0.0f, 0.35f, 0.03f  // 山地森林
            ));

            // 华中地区 - 湖泊与丘陵
            AddProvince(new ChinaProvinceConfig(
                "湖北省", "鄂",
                "江汉平原与鄂西山地对比，长江横贯",
                ProvinceTerrainType.Mixed,
                0.18f, 0.26f, 0.36f, 0.50f, 0.68f, 0.82f,
                0.68f, 0.62f,
                12, 12f,       // 千湖之省
                0.42f, 0.02f, 0.22f, 0.34f  // 平原与丘陵
            ));

            AddProvince(new ChinaProvinceConfig(
                "湖南省", "湘",
                "湘江流域，洞庭湖平原与湘南丘陵对比",
                ProvinceTerrainType.Mixed,
                0.18f, 0.26f, 0.38f, 0.52f, 0.70f, 0.84f,
                0.70f, 0.65f,
                10, 10f,       // 洞庭湖等
                0.48f, 0.02f, 0.25f, 0.25f  // 丘陵与平原
            ));

            AddProvince(new ChinaProvinceConfig(
                "广东省", "粤",
                "岭南丘陵与珠三角平原，南海之滨",
                ProvinceTerrainType.Mountain,
                0.15f, 0.22f, 0.35f, 0.52f, 0.70f, 0.84f,
                0.80f, 0.82f,  // 炎热湿润
                5, 7f,
                0.55f, 0.0f, 0.30f, 0.15f  // 热带森林丘陵
            ));

            AddProvince(new ChinaProvinceConfig(
                "广西壮族自治区", "桂",
                "云贵高原东南边缘，喀斯特地貌与丘陵",
                ProvinceTerrainType.Mountain,
                0.20f, 0.28f, 0.40f, 0.56f, 0.74f, 0.88f,
                0.72f, 0.75f,  // 热带亚热带
                4, 6f,
                0.52f, 0.02f, 0.38f, 0.08f  // 喀斯特山区
            ));

            // 西南地区 - 高原与盆地对比
            AddProvince(new ChinaProvinceConfig(
                "重庆", "渝",
                "四川盆地东部，平行岭谷与丘陵",
                ProvinceTerrainType.Mountain,
                0.20f, 0.28f, 0.40f, 0.55f, 0.72f, 0.86f,
                0.62f, 0.62f,
                3, 5f,
                0.42f, 0.05f, 0.38f, 0.15f  // 山地丘陵
            ));

            AddProvince(new ChinaProvinceConfig(
                "四川省", "川",
                "四川盆地与川西高原雪山对比",
                ProvinceTerrainType.Mixed,
                0.24f, 0.32f, 0.44f, 0.58f, 0.74f, 0.88f,
                0.58f, 0.55f,
                10, 12f,       // 九寨沟、泸沽湖等
                0.38f, 0.05f, 0.42f, 0.15f  // 高原与山地交错
            ));

            AddProvince(new ChinaProvinceConfig(
                "贵州省", "黔",
                "云贵高原东部，喀斯特地貌王国",
                ProvinceTerrainType.Mountain,
                0.24f, 0.32f, 0.44f, 0.58f, 0.75f, 0.88f,
                0.68f, 0.58f,
                4, 6f,
                0.48f, 0.02f, 0.42f, 0.08f  // 喀斯特山地
            ));

            AddProvince(new ChinaProvinceConfig(
                "云南省", "滇",
                "云贵高原主体，从热带雨林到雪山冰川",
                ProvinceTerrainType.Plateau,
                0.30f, 0.38f, 0.50f, 0.64f, 0.78f, 0.90f,
                0.68f, 0.62f,
                12, 14f,       // 滇池、洱海、泸沽湖等
                0.52f, 0.03f, 0.38f, 0.07f  // 高原山地森林
            ));

            AddProvince(new ChinaProvinceConfig(
                "西藏自治区", "藏",
                "世界屋脊青藏高原核心，珠穆朗玛峰所在地",
                ProvinceTerrainType.Plateau,
                0.42f, 0.50f, 0.60f, 0.72f, 0.85f, 0.95f,
                0.32f, 0.20f,  // 高原严寒
                30, 20f,       // 纳木错、羊卓雍错等众多湖泊
                0.02f, 0.08f, 0.70f, 0.20f  // 雪山冰川与高原草甸
            ));

            // 特别行政区及台湾省
            AddProvince(new ChinaProvinceConfig(
                "香港特别行政区", "港",
                "珠江口东侧，丘陵海岸地貌",
                ProvinceTerrainType.Mountain,
                0.12f, 0.18f, 0.30f, 0.48f, 0.65f, 0.80f,
                0.80f, 0.82f,
                2, 3f,
                0.50f, 0.0f, 0.45f, 0.05f  // 山地森林海岸
            ));

            AddProvince(new ChinaProvinceConfig(
                "澳门特别行政区", "澳",
                "珠江口西侧，沿海低地",
                ProvinceTerrainType.Plain,
                0.10f, 0.16f, 0.26f, 0.38f, 0.55f, 0.72f,
                0.80f, 0.85f,
                2, 3f,
                0.15f, 0.0f, 0.08f, 0.77f  // 沿海平原
            ));

            AddProvince(new ChinaProvinceConfig(
                "台湾省", "台",
                "宝岛台湾，中央山脉纵贯，东部陡峭西部平缓",
                ProvinceTerrainType.Mountain,
                0.15f, 0.22f, 0.34f, 0.50f, 0.68f, 0.82f,
                0.78f, 0.78f,  // 热带亚热带
                6, 8f,
                0.62f, 0.0f, 0.35f, 0.03f  // 森林覆盖率极高
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
                0.35f, 0.05f, 0.30f, 0.30f
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
