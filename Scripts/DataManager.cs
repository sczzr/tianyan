using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace TianYanShop.Scripts
{
    /// <summary>
    /// 数据管理器 - 游戏数据的唯一来源
    /// 负责从JSON文件加载所有静态数据（物品、天篆、生物、材料等）
    /// </summary>
    public partial class DataManager : Node
    {
        public static DataManager Instance { get; private set; }

        // 数据存储字典
        public Dictionary<string, ItemData> Items { get; private set; } = new();
        public Dictionary<string, TianZhuanData> TianZhuans { get; private set; } = new();
        public Dictionary<string, CreatureData> Creatures { get; private set; } = new();
        public Dictionary<string, RawMaterialData> RawMaterials { get; private set; } = new();
        public Dictionary<string, LootTableData> LootTables { get; private set; } = new();
        public Dictionary<string, SpiritPlantData> SpiritPlants { get; private set; } = new();

        // 数据根路径
        private const string DataPath = "res://Data/";

        public override void _Ready()
        {
            if (Instance == null)
            {
                Instance = this;
                ProcessMode = ProcessModeEnum.Always;
                LoadAllData();
            }
            else
            {
                QueueFree();
            }
        }

        /// <summary>
        /// 加载所有数据
        /// </summary>
        private void LoadAllData()
        {
            GD.Print("[DataManager] 开始加载游戏数据...");

            LoadItems();
            LoadTianZhuans();
            LoadCreatures();
            LoadRawMaterials();
            LoadLootTables();
            LoadSpiritPlants();

            GD.Print($"[DataManager] 数据加载完成: {Items.Count} 物品, {TianZhuans.Count} 天篆, " +
                     $"{Creatures.Count} 生物, {RawMaterials.Count} 材料");
        }

        /// <summary>
        /// 从JSON文件加载数据
        /// </summary>
        private Dictionary<string, T> LoadFromJson<T>(string filePath) where T : class
        {
            var result = new Dictionary<string, T>();

            try
            {
                if (!Godot.FileAccess.FileExists(filePath))
                {
                    GD.PrintErr($"[DataManager] 文件不存在: {filePath}");
                    return result;
                }

                using var file = Godot.FileAccess.Open(filePath, Godot.FileAccess.ModeFlags.Read);
                string json = file.GetAsText();

                // 使用 System.Text.Json 进行反序列化
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true
                };

                var data = JsonSerializer.Deserialize<Dictionary<string, T>>(json, options);
                if (data != null)
                {
                    result = data;
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[DataManager] 加载JSON失败 {filePath}: {ex.Message}");
            }

            return result;
        }

        #region 数据加载方法

        private void LoadItems()
        {
            // 初始化一些基础物品数据（硬编码，后续改为JSON加载）
            InitializeDefaultItems();
        }

        private void LoadTianZhuans()
        {
            // TODO: 从天篆JSON文件加载
            // 当前留空，等待天篆数据文件
        }

        private void LoadCreatures()
        {
            // TODO: 从生物JSON文件加载
        }

        private void LoadRawMaterials()
        {
            // TODO: 从原材料JSON文件加载
        }

        private void LoadLootTables()
        {
            // TODO: 从掉落表JSON文件加载
        }

        private void LoadSpiritPlants()
        {
            // TODO: 从灵植JSON文件加载
        }

        #endregion

        /// <summary>
        /// 初始化默认物品数据
        /// </summary>
        private void InitializeDefaultItems()
        {
            // 材料
            Items["spirit_grass"] = new ItemData
            {
                Id = "spirit_grass",
                Name = "灵草",
                Description = "散发着淡淡灵气的草药，是炼制基础丹药的材料。",
                Type = ItemType.Material,
                Rarity = ItemRarity.Common,
                BasePrice = 10,
                StackSize = 99
            };

            Items["spirit_water"] = new ItemData
            {
                Id = "spirit_water",
                Name = "灵泉水",
                Description = "蕴含灵气的泉水，常用于炼制丹药和清洗药材。",
                Type = ItemType.Material,
                Rarity = ItemRarity.Common,
                BasePrice = 8,
                StackSize = 99
            };

            Items["talisman_paper"] = new ItemData
            {
                Id = "talisman_paper",
                Name = "符纸",
                Description = "特制的纸张，用于绘制符箓，能够承载灵气。",
                Type = ItemType.Material,
                Rarity = ItemRarity.Common,
                BasePrice = 5,
                StackSize = 99
            };

            Items["cinnabar"] = new ItemData
            {
                Id = "cinnabar",
                Name = "朱砂",
                Description = "绘制符箓的重要材料，含有特殊的灵气传导属性。",
                Type = ItemType.Material,
                Rarity = ItemRarity.Common,
                BasePrice = 12,
                StackSize = 99
            };

            Items["moonflower"] = new ItemData
            {
                Id = "moonflower",
                Name = "月见花",
                Description = "只在月光下绽放的奇花，是炼制高级丹药的珍贵材料。",
                Type = ItemType.Material,
                Rarity = ItemRarity.Uncommon,
                BasePrice = 50,
                StackSize = 50
            };

            Items["iron_ore"] = new ItemData
            {
                Id = "iron_ore",
                Name = "玄铁矿石",
                Description = "蕴含微弱灵气的矿石，可用于炼制基础法器。",
                Type = ItemType.Material,
                Rarity = ItemRarity.Common,
                BasePrice = 15,
                StackSize = 99
            };

            // 丹药
            Items["qi_gathering_pill"] = new ItemData
            {
                Id = "qi_gathering_pill",
                Name = "聚气丹",
                Description = "帮助练气期修士凝聚灵气的丹药，可略微提升修炼速度。",
                Type = ItemType.Pill,
                Rarity = ItemRarity.Common,
                BasePrice = 50,
                StackSize = 20,
                Effect = "增加修炼速度",
                EffectValue = 10
            };

            Items["foundation_pill"] = new ItemData
            {
                Id = "foundation_pill",
                Name = "筑基丹",
                Description = "珍贵的丹药，可帮助练气大圆满修士突破至筑基期。成功率与修士资质有关。",
                Type = ItemType.Pill,
                Rarity = ItemRarity.Rare,
                BasePrice = 1000,
                StackSize = 5,
                Effect = "突破境界",
                EffectValue = 30
            };

            // 符箓
            Items["fire_talisman"] = new ItemData
            {
                Id = "fire_talisman",
                Name = "火球符",
                Description = "基础的攻击符箓，激发后可释放火球攻击敌人。",
                Type = ItemType.Talisman,
                Rarity = ItemRarity.Common,
                BasePrice = 30,
                StackSize = 50
            };

            Items["protection_talisman"] = new ItemData
            {
                Id = "protection_talisman",
                Name = "护身符",
                Description = "防御型符箓，可抵挡一次不超过筑基期的攻击。",
                Type = ItemType.Talisman,
                Rarity = ItemRarity.Uncommon,
                BasePrice = 80,
                StackSize = 30
            };
        }

        /// <summary>
        /// 获取物品数据
        /// </summary>
        public ItemData GetItem(string itemId)
        {
            return Items.TryGetValue(itemId, out var item) ? item : null;
        }

        /// <summary>
        /// 获取天篆数据
        /// </summary>
        public TianZhuanData GetTianZhuan(string tianZhuanId)
        {
            return TianZhuans.TryGetValue(tianZhuanId, out var tz) ? tz : null;
        }

        /// <summary>
        /// 通过道枢获取天篆列表
        /// </summary>
        public List<TianZhuanData> GetTianZhuansByDaoHub(string daoHubName)
        {
            var result = new List<TianZhuanData>();
            foreach (var tz in TianZhuans.Values)
            {
                if (tz.AffiliatedDaoHub == daoHubName)
                {
                    result.Add(tz);
                }
            }
            return result;
        }
    }

    #region 数据类定义

    /// <summary>
    /// 天篆数据类
    /// </summary>
    public class TianZhuanData
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public TianZhuanRank Rank { get; set; }
        public string AffiliatedDaoHub { get; set; }
        public List<string> FunctionCategories { get; set; } = new();
        public string DiscoveryEra { get; set; }
        public string Discoverer { get; set; }
        public string XuanxueKoujue { get; set; }
        public TianZhuanForm FormDescription { get; set; }
        public LinXunAnnotation LinXunAnnotation { get; set; }
        public string DetailedFunction { get; set; }
        public List<DerivedForm> DerivedForms { get; set; } = new();
        public Dictionary<string, string> TechnicalParameters { get; set; } = new();
        public CultivationRestriction CultivationRestrictions { get; set; }
        public DaoHubRelationship DaoHubRelationships { get; set; }
        public string HistoricalSignificance { get; set; }
        public List<ApplicationExample> ApplicationExamples { get; set; } = new();
    }

    public class TianZhuanForm
    {
        public string ThreeDProjection { get; set; }
        public string TwoDTopology { get; set; }
    }

    public class LinXunAnnotation
    {
        public string PhysicalEssence { get; set; }
        public string Mechanism { get; set; }
        public string MathematicalExpression { get; set; }
        public string ApplicationPrinciple { get; set; }
    }

    public class DerivedForm
    {
        public string Context { get; set; }
        public string FormName { get; set; }
        public string LawInterpretation { get; set; }
        public string CombatApplication { get; set; }
    }

    public class CultivationRestriction
    {
        public string CultivationRealm { get; set; }
        public string DaoHubUnderstanding { get; set; }
        public string SoulRequirements { get; set; }
        public string UsageRestrictions { get; set; }
        public string Countermeasures { get; set; }
    }

    public class DaoHubRelationship
    {
        public string UpstreamDaoHub { get; set; }
        public List<string> CollaborativeDaoHubs { get; set; } = new();
        public List<string> CompatibleTianZhuans { get; set; } = new();
    }

    public class ApplicationExample
    {
        public string StandardApplication { get; set; }
        public string CombinationApplication { get; set; }
        public string LinXunModifiedApplication { get; set; }
    }

    public enum TianZhuanRank
    {
        Core,
        HighTier
    }

    /// <summary>
    /// 生物数据类
    /// </summary>
    public class CreatureData
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Race { get; set; }
        public int Level { get; set; }
        public Dictionary<string, int> Attributes { get; set; } = new();
        public List<string> Skills { get; set; } = new();
        public string LootTableID { get; set; }
    }

    /// <summary>
    /// 原材料数据类
    /// </summary>
    public class RawMaterialData
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public int Quality { get; set; }
        public string Source { get; set; }
        public Dictionary<string, float> Attributes { get; set; } = new();
    }

    /// <summary>
    /// 掉落表数据类
    /// </summary>
    public class LootTableData
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public List<LootEntry> Entries { get; set; } = new();
    }

    public class LootEntry
    {
        public string ItemID { get; set; }
        public int MinQuantity { get; set; }
        public int MaxQuantity { get; set; }
        public float DropChance { get; set; }
    }

    /// <summary>
    /// 灵植数据类
    /// </summary>
    public class SpiritPlantData
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int GrowthTime { get; set; }
        public int Yield { get; set; }
        public string YieldItemID { get; set; }
        public List<string> GrowthConditions { get; set; } = new();
    }

    #endregion
}
