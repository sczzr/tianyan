using Godot;
using System;
using System.Collections.Generic;

using TianYanShop.NPC;
using TianYanShop.Data;
using TianYanShop.Entity;

namespace TianYanShop.Core
{
    /// <summary>
    /// 游戏管理器 - 单例模式，管理游戏全局状态
    /// </summary>
    public partial class GameManager : Node
    {
        public static GameManager Instance { get; private set; }

        // 游戏状态
        public enum GameState
        {
            MainMenu,
            Playing,
            Dialogue,
            Crafting,
            Trading,
            Paused
        }

        [Signal]
        public delegate void GameStateChangedEventHandler(GameState newState);

        [Signal]
        public delegate void SpiritStonesChangedEventHandler(int newAmount);

        [Signal]
        public delegate void DayChangedEventHandler(int newDay);

        // 当前游戏状态
        public GameState CurrentState { get; private set; } = GameState.MainMenu;

        // 玩家数据
        public int SpiritStones { get; private set; } = 1000;
        public int Reputation { get; private set; } = 50;
        public int CurrentDay { get; private set; } = 1;
        public int PlayerRealm { get; private set; } = 0; // 0=练气, 1=筑基, 2=金丹...

        // NPC列表
        public Godot.Collections.Array<NPCData> NPCList { get; private set; } = new();

        // 当前对话的NPC
        public NPCData CurrentNPC { get; private set; }

        // 物品数据库
        public Godot.Collections.Dictionary<string, ItemData> ItemDatabase { get; private set; } = new();

        // 系统引用
        public DataManager DataMgr { get; private set; }
        public TimeManager TimeMgr { get; private set; }
        public InventoryManager InventoryMgr { get; private set; }
        public TianZhuanManager TianZhuanMgr { get; private set; }

        public override void _Ready()
        {
            if (Instance == null)
            {
                Instance = this;
                ProcessMode = ProcessModeEnum.Always;

                // 初始化系统引用
                InitializeSystemReferences();

                // 初始化游戏
                InitializeGame();
            }
            else
            {
                QueueFree();
            }
        }

        /// <summary>
        /// 初始化系统引用
        /// </summary>
        private void InitializeSystemReferences()
        {
            DataMgr = DataManager.Instance;
            TimeMgr = TimeManager.Instance;
            InventoryMgr = InventoryManager.Instance;
            TianZhuanMgr = TianZhuanManager.Instance;
        }

        private void InitializeGame()
        {
            GD.Print("[GameManager] 初始化游戏...");

            // 检查 DataManager 是否就绪
            if (DataMgr == null || DataMgr.Items.Count == 0)
            {
                GD.Print("[GameManager] 等待 DataManager 就绪，延迟初始化...");
                CallDeferred(nameof(InitializeGame));
                return;
            }

            // 初始化NPC
            InitializeNPCs();

            // 连接时间系统信号
            if (TimeMgr != null)
            {
                TimeMgr.GameDayPassed += OnGameDayPassed;
            }

            // 初始状态
            SetGameState(GameState.Playing);

            GD.Print($"[GameManager] 游戏初始化完成");
        }

        /// <summary>
        /// 游戏天数变化处理
        /// </summary>
        private void OnGameDayPassed(int day)
        {
            CurrentDay = day;
            EmitSignal(SignalName.DayChanged, day);
            GD.Print($"[GameManager] 第 {day} 天开始");

            // 触发每日更新逻辑
            // 例如：NPC每日行为、商店刷新等
        }

        private void InitializeItemDatabase()
        {
            // 材料
            ItemDatabase["spirit_grass"] = new ItemData
            {
                Id = "spirit_grass",
                Name = "灵草",
                Description = "散发着淡淡灵气的草药，是炼制基础丹药的材料。",
                Type = ItemType.Material,
                Rarity = ItemRarity.Common,
                BasePrice = 10
            };

            ItemDatabase["iron_ore"] = new ItemData
            {
                Id = "iron_ore",
                Name = "玄铁矿石",
                Description = "蕴含微弱灵气的矿石，可用于炼制基础法器。",
                Type = ItemType.Material,
                Rarity = ItemRarity.Common,
                BasePrice = 15
            };

            // 丹药
            ItemDatabase["qi_gathering_pill"] = new ItemData
            {
                Id = "qi_gathering_pill",
                Name = "聚气丹",
                Description = "帮助练气期修士凝聚灵气的丹药，可略微提升修炼速度。",
                Type = ItemType.Pill,
                Rarity = ItemRarity.Common,
                BasePrice = 50,
                Effect = "增加修炼速度",
                EffectValue = 10
            };

            ItemDatabase["foundation_pill"] = new ItemData
            {
                Id = "foundation_pill",
                Name = "筑基丹",
                Description = "珍贵的丹药，可帮助练气大圆满修士突破至筑基期。成功率与修士资质有关。",
                Type = ItemType.Pill,
                Rarity = ItemRarity.Rare,
                BasePrice = 1000,
                Effect = "突破境界",
                EffectValue = 30
            };

            // 符箓
            ItemDatabase["fire_talisman"] = new ItemData
            {
                Id = "fire_talisman",
                Name = "火球符",
                Description = "基础的攻击符箓，激发后可释放火球攻击敌人。",
                Type = ItemType.Talisman,
                Rarity = ItemRarity.Common,
                BasePrice = 30
            };

            ItemDatabase["protection_talisman"] = new ItemData
            {
                Id = "protection_talisman",
                Name = "护身符",
                Description = "防御型符箓，可抵挡一次不超过筑基期的攻击。",
                Type = ItemType.Talisman,
                Rarity = ItemRarity.Uncommon,
                BasePrice = 80
            };

            GD.Print($"[GameManager] 物品数据库初始化完成，共 {ItemDatabase.Count} 个物品");
        }

        private void InitializeNPCs()
        {
            // 萧云 - 主角NPC
            var xiaoYun = NPCData.CreateXiaoYun();
            xiaoYun.StoryStage = 2;
            xiaoYun.CurrentGoal = "获得筑基丹";
            NPCList.Add(xiaoYun);

            // 添加一些随机NPC
            string[] names = { "林婉儿", "张铁柱", "王富贵", "李逍遥", "陈师妹" };
            string[] backgrounds = { "散修", "小家族弟子", "流浪修士", "宗门弃徒", "商人之子" };
            string[] personalities = { "豪爽", "谨慎", "贪婪", "善良", "狡黠" };

            for (int i = 0; i < 5; i++)
            {
                var npc = new NPCData
                {
                    Id = $"npc_{i + 2:D3}",
                    Name = names[i],
                    Background = backgrounds[i],
                    Personality = personalities[i],
                    Realm = "练气期",
                    Level = new[] { "初期", "中期", "后期" }[GD.RandRange(0, 2)],
                    SpiritRoot = new[] { "五灵根", "四灵根", "三灵根", "双灵根" }[GD.RandRange(0, 3)],
                    SpiritStones = GD.RandRange(50, 500),
                    RelationshipWithPlayer = GD.RandRange(20, 60),
                    CurrentGoal = new[] { "寻找材料", "提升修为", "购买法器", "拜师学艺" }[GD.RandRange(0, 3)]
                };
                NPCList.Add(npc);
            }

            GD.Print($"[GameManager] NPC初始化完成，共 {NPCList.Count} 个NPC");
        }

        /// <summary>
        /// 设置游戏状态
        /// </summary>
        public void SetGameState(GameState newState)
        {
            CurrentState = newState;
            EmitSignal(SignalName.GameStateChanged, (int)newState);
            GD.Print($"[GameManager] 游戏状态变更为: {newState}");
        }

        /// <summary>
        /// 添加灵石
        /// </summary>
        public void AddSpiritStones(int amount)
        {
            if (InventoryMgr != null)
            {
                InventoryMgr.AddSpiritStones(amount);
            }
            else
            {
                SpiritStones += amount;
                EmitSignal(SignalName.SpiritStonesChanged, SpiritStones);
            }
        }

        /// <summary>
        /// 消耗灵石
        /// </summary>
        public bool SpendSpiritStones(int amount)
        {
            if (InventoryMgr != null)
            {
                return InventoryMgr.TrySpendSpiritStones(amount);
            }
            else
            {
                if (SpiritStones >= amount)
                {
                    SpiritStones -= amount;
                    EmitSignal(SignalName.SpiritStonesChanged, SpiritStones);
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// 检查是否有足够灵石
        /// </summary>
        public bool HasEnoughSpiritStones(int amount)
        {
            if (InventoryMgr != null)
            {
                return InventoryMgr.HasEnoughSpiritStones(amount);
            }
            return SpiritStones >= amount;
        }

        /// <summary>
        /// 进入下一天
        /// </summary>
        public void NextDay()
        {
            CurrentDay++;
            EmitSignal(SignalName.DayChanged, CurrentDay);
            GD.Print($"[GameManager] 进入第 {CurrentDay} 天");
        }

        /// <summary>
        /// 开始与NPC对话
        /// </summary>
        public void StartDialogue(NPCData npc)
        {
            CurrentNPC = npc;
            SetGameState(GameState.Dialogue);
        }

        /// <summary>
        /// 结束对话
        /// </summary>
        public void EndDialogue()
        {
            CurrentNPC = null;
            SetGameState(GameState.Playing);
        }

        /// <summary>
        /// 获取物品数据
        /// </summary>
        public ItemData GetItem(string itemId)
        {
            if (ItemDatabase.ContainsKey(itemId))
            {
                return ItemDatabase[itemId];
            }
            return null;
        }
    }
}
