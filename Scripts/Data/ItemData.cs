using Godot;
using System;

namespace TianYanShop.Data
{
    /// <summary>
    /// 物品数据类 - 定义游戏中的所有物品
    /// </summary>
    [GlobalClass]
    public partial class ItemData : Resource
    {
        [Export] public string Id { get; set; } = "";
        [Export] public string Name { get; set; } = "";
        [Export] public string Description { get; set; } = "";
        [Export] public ItemType Type { get; set; } = ItemType.Material;
        [Export] public ItemRarity Rarity { get; set; } = ItemRarity.Common;
        [Export] public int BasePrice { get; set; } = 10;
        [Export] public int StackSize { get; set; } = 99;
        [Export] public bool IsSellable { get; set; } = true;
        [Export] public bool IsCraftable { get; set; } = false;
        [Export] public string IconPath { get; set; } = "";

        // 特殊效果（针对丹药、符箓等）
        [Export] public string Effect { get; set; } = "";
        [Export] public int EffectValue { get; set; } = 0;

        /// <summary>
        /// 获取带品质颜色的名称
        /// </summary>
        public string GetColoredName()
        {
            string color = Rarity switch
            {
                ItemRarity.Common => "#CCCCCC",
                ItemRarity.Uncommon => "#00FF00",
                ItemRarity.Rare => "#0088FF",
                ItemRarity.Epic => "#AA00FF",
                ItemRarity.Legendary => "#FF8800",
                ItemRarity.Mythic => "#FF0000",
                _ => "#FFFFFF"
            };
            return $"[color={color}]{Name}[/color]";
        }

        /// <summary>
        /// 获取品质中文名
        /// </summary>
        public string GetRarityName()
        {
            return Rarity switch
            {
                ItemRarity.Common => "普通",
                ItemRarity.Uncommon => "精良",
                ItemRarity.Rare => "稀有",
                ItemRarity.Epic => "史诗",
                ItemRarity.Legendary => "传说",
                ItemRarity.Mythic => "神话",
                _ => "未知"
            };
        }
    }

    /// <summary>
    /// 物品类型枚举
    /// </summary>
    public enum ItemType
    {
        Material,    // 材料
        Pill,        // 丹药
        Talisman,    // 符箓
        Formation,   // 阵法
        Puppet,      // 傀儡
        Weapon,      // 武器
        Armor,       // 防具
        Accessory,   // 饰品
        SpiritStone, // 灵石
        Special      // 特殊
    }

    /// <summary>
    /// 物品品质枚举
    /// </summary>
    public enum ItemRarity
    {
        Common,     // 普通（灰色）
        Uncommon,   // 精良（绿色）
        Rare,       // 稀有（蓝色）
        Epic,       // 史诗（紫色）
        Legendary,  // 传说（橙色）
        Mythic      // 神话（红色）
    }
}
