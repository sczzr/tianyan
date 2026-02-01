using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TianYanShop.Scripts
{
    /// <summary>
    /// 背包管理器 - 管理玩家所有物品交互
    /// 提供基础的原子操作接口：查询、添加、移除物品
    /// </summary>
    public partial class InventoryManager : Node
    {
        public static InventoryManager Instance { get; private set; }

        // 可堆叠物品（如材料、丹药）- itemId -> 数量
        public Dictionary<string, int> StackableItems { get; private set; } = new();

        // 唯一实例物品（如带词条的法宝）- instanceId -> UniqueItem
        public Dictionary<string, UniqueItem> UniqueItems { get; private set; } = new();

        // 背包容量限制（可选）
        [Export] public int MaxStackableSlots { get; set; } = 99;
        [Export] public int MaxUniqueSlots { get; set; } = 50;

        // 金钱/货币
        public int SpiritStones { get; private set; } = 1000;

        // 信号
        [Signal]
        public delegate void ItemAddedEventHandler(string itemId, int quantity, int totalQuantity);

        [Signal]
        public delegate void ItemRemovedEventHandler(string itemId, int quantity, int remainingQuantity);

        [Signal]
        public delegate void UniqueItemAddedEventHandler(string instanceId, string itemBaseId);

        [Signal]
        public delegate void UniqueItemRemovedEventHandler(string instanceId);

        [Signal]
        public delegate void SpiritStonesChangedEventHandler(int newAmount, int delta);

        [Signal]
        public delegate void InventoryFullEventHandler();

        public override void _Ready()
        {
            if (Instance == null)
            {
                Instance = this;
                ProcessMode = ProcessModeEnum.Always;
                Initialize();
            }
            else
            {
                QueueFree();
            }
        }

        private void Initialize()
        {
            GD.Print("[InventoryManager] 初始化背包管理器...");

            // 添加初始物品
            AddStartingItems();

            GD.Print($"[InventoryManager] 背包初始化完成，拥有 {SpiritStones} 灵石");
        }

        /// <summary>
        /// 添加初始物品
        /// </summary>
        private void AddStartingItems()
        {
            // 等待 DataManager 就绪后再添加物品
            if (DataManager.Instance == null || DataManager.Instance.Items.Count == 0)
            {
                GD.Print("[InventoryManager] 等待 DataManager 就绪...");
                // 延迟1帧后重试
                CallDeferred(nameof(AddStartingItems));
                return;
            }

            GD.Print("[InventoryManager] 添加初始物品...");

            // 初始材料
            TryAddItem("spirit_grass", 20);
            TryAddItem("spirit_water", 5);
            TryAddItem("talisman_paper", 10);
            TryAddItem("cinnabar", 8);
            TryAddItem("moonflower", 2);
            TryAddItem("iron_ore", 15);

            GD.Print("[InventoryManager] 初始物品添加完成");
        }

        #region 物品查询接口

        /// <summary>
        /// 检查是否拥有指定物品
        /// </summary>
        public bool HasItem(string itemId, int quantity = 1)
        {
            if (StackableItems.TryGetValue(itemId, out var count))
            {
                return count >= quantity;
            }
            return false;
        }

        /// <summary>
        /// 获取物品数量
        /// </summary>
        public int GetItemCount(string itemId)
        {
            return StackableItems.TryGetValue(itemId, out var count) ? count : 0;
        }

        /// <summary>
        /// 检查是否有足够数量的多个物品
        /// </summary>
        public bool HasItems(Dictionary<string, int> items)
        {
            foreach (var item in items)
            {
                if (!HasItem(item.Key, item.Value))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 获取所有物品列表
        /// </summary>
        public Dictionary<string, int> GetAllItems()
        {
            return new Dictionary<string, int>(StackableItems);
        }

        #endregion

        #region 物品添加接口

        /// <summary>
        /// 尝试添加物品
        /// 返回实际添加的数量
        /// </summary>
        public int TryAddItem(string itemId, int quantity)
        {
            if (quantity <= 0) return 0;

            // 检查是否是有效物品
            var itemData = DataManager.Instance?.GetItem(itemId);
            if (itemData == null)
            {
                GD.PrintErr($"[InventoryManager] 尝试添加未知物品: {itemId}");
                return 0;
            }

            // 计算可添加数量（考虑堆叠上限）
            int currentCount = GetItemCount(itemId);
            int maxStack = itemData.StackSize;
            int canAdd = Math.Min(quantity, maxStack - currentCount);

            if (canAdd <= 0)
            {
                EmitSignal(SignalName.InventoryFull);
                return 0;
            }

            // 添加物品
            if (StackableItems.ContainsKey(itemId))
            {
                StackableItems[itemId] += canAdd;
            }
            else
            {
                StackableItems[itemId] = canAdd;
            }

            EmitSignal(SignalName.ItemAdded, itemId, canAdd, StackableItems[itemId]);

            return canAdd;
        }

        /// <summary>
        /// 尝试添加多个物品
        /// </summary>
        public bool TryAddItems(Dictionary<string, int> items)
        {
            // 先检查是否都能添加
            foreach (var item in items)
            {
                var itemData = DataManager.Instance?.GetItem(item.Key);
                if (itemData == null) return false;

                int currentCount = GetItemCount(item.Key);
                if (currentCount + item.Value > itemData.StackSize)
                    return false;
            }

            // 执行添加
            foreach (var item in items)
            {
                TryAddItem(item.Key, item.Value);
            }

            return true;
        }

        #endregion

        #region 物品移除接口

        /// <summary>
        /// 尝试移除物品
        /// 返回实际移除的数量
        /// </summary>
        public int TryRemoveItem(string itemId, int quantity)
        {
            if (quantity <= 0) return 0;

            int currentCount = GetItemCount(itemId);
            if (currentCount <= 0) return 0;

            int canRemove = Math.Min(quantity, currentCount);

            StackableItems[itemId] -= canRemove;

            if (StackableItems[itemId] <= 0)
            {
                StackableItems.Remove(itemId);
            }

            EmitSignal(SignalName.ItemRemoved, itemId, canRemove, GetItemCount(itemId));

            return canRemove;
        }

        /// <summary>
        /// 尝试移除多个物品
        /// </summary>
        public bool TryRemoveItems(Dictionary<string, int> items)
        {
            // 先检查是否都有足够数量
            if (!HasItems(items))
                return false;

            // 执行移除
            foreach (var item in items)
            {
                TryRemoveItem(item.Key, item.Value);
            }

            return true;
        }

        /// <summary>
        /// 移除所有指定物品
        /// </summary>
        public int RemoveAllItem(string itemId)
        {
            int count = GetItemCount(itemId);
            return TryRemoveItem(itemId, count);
        }

        #endregion

        #region 货币接口

        /// <summary>
        /// 添加灵石
        /// </summary>
        public void AddSpiritStones(int amount)
        {
            if (amount <= 0) return;

            int oldAmount = SpiritStones;
            SpiritStones += amount;

            EmitSignal(SignalName.SpiritStonesChanged, SpiritStones, amount);

            GD.Print($"[InventoryManager] 获得 {amount} 灵石，当前: {SpiritStones}");
        }

        /// <summary>
        /// 尝试消耗灵石
        /// </summary>
        public bool TrySpendSpiritStones(int amount)
        {
            if (amount <= 0) return true;
            if (SpiritStones < amount) return false;

            int oldAmount = SpiritStones;
            SpiritStones -= amount;

            EmitSignal(SignalName.SpiritStonesChanged, SpiritStones, -amount);

            GD.Print($"[InventoryManager] 消耗 {amount} 灵石，当前: {SpiritStones}");
            return true;
        }

        /// <summary>
        /// 检查是否有足够灵石
        /// </summary>
        public bool HasEnoughSpiritStones(int amount)
        {
            return SpiritStones >= amount;
        }

        #endregion

        #region 存档/读档

        /// <summary>
        /// 获取存档数据
        /// </summary>
        public InventorySaveData GetSaveData()
        {
            return new InventorySaveData
            {
                StackableItems = new Dictionary<string, int>(StackableItems),
                SpiritStones = SpiritStones,
                SaveTime = DateTime.Now
            };
        }

        /// <summary>
        /// 从存档数据加载
        /// </summary>
        public void LoadSaveData(InventorySaveData data)
        {
            if (data == null) return;

            StackableItems = new Dictionary<string, int>(data.StackableItems ?? new Dictionary<string, int>());
            SpiritStones = data.SpiritStones;

            // 处理离线时间
            if (data.SaveTime != default)
            {
                TimeSpan offlineDuration = DateTime.Now - data.SaveTime;
                if (offlineDuration.TotalHours > 0)
                {
                    ProcessOfflineTime(offlineDuration);
                }
            }

            GD.Print($"[InventoryManager] 从存档加载，拥有 {SpiritStones} 灵石，{StackableItems.Count} 种物品");
        }

        /// <summary>
        /// 处理离线时间（计算离线收益等）
        /// </summary>
        private void ProcessOfflineTime(TimeSpan offlineDuration)
        {
            GD.Print($"[InventoryManager] 处理离线时间: {offlineDuration.TotalHours:F1} 小时");

            // 这里可以计算离线期间的各种收益
            // 例如：洞天福地的产出、NPC的行为结果等
        }

        #endregion
    }

    /// <summary>
    /// 背包存档数据
    /// </summary>
    [Serializable]
    public class InventorySaveData
    {
        public Dictionary<string, int> StackableItems { get; set; }
        public int SpiritStones { get; set; }
        public DateTime SaveTime { get; set; }
    }

    /// <summary>
    /// 唯一实例物品（带独立属性的物品）
    /// </summary>
    [Serializable]
    public class UniqueItem
    {
        public string InstanceId { get; set; }
        public string BaseItemId { get; set; }
        public int Durability { get; set; }
        public int MaxDurability { get; set; }
        public List<ItemAffix> Affixes { get; set; } = new();
        public string CrafterName { get; set; }
        public DateTime CreatedTime { get; set; }
        public string CustomName { get; set; }
    }

    /// <summary>
    /// 物品词条
    /// </summary>
    [Serializable]
    public class ItemAffix
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public AffixType Type { get; set; }
        public string StatName { get; set; }
        public float StatValue { get; set; }
        public AffixTier Tier { get; set; }
    }

    public enum AffixType
    {
        Prefix, // 前缀
        Suffix  // 后缀
    }

    public enum AffixTier
    {
        Common,     // 普通
        Uncommon,   // 精良
        Rare,       // 稀有
        Epic,       // 史诗
        Legendary   // 传说
    }
}
