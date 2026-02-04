using Godot;
using System;
using System.Collections.Generic;

namespace TianYanShop.NPC
{
    /// <summary>
    /// NPC数据类 - 存储NPC的所有信息
    /// </summary>
    [GlobalClass]
    public partial class NPCData : Resource
    {
        [Export] public string Id { get; set; } = "";
        [Export] public string Name { get; set; } = "";
        [Export] public string Background { get; set; } = "";
        [Export] public string Personality { get; set; } = "";

        // 修为境界
        [Export] public string Realm { get; set; } = "练气期";
        [Export] public string Level { get; set; } = "初期";
        [Export] public string SpiritRoot { get; set; } = "四灵根";

        // 关系与情感
        [Export] public int RelationshipWithPlayer { get; set; } = 30;
        [Export] public int Trust { get; set; } = 30;
        [Export] public int Fear { get; set; } = 0;
        [Export] public int Gratitude { get; set; } = 0;
        [Export] public int Suspicion { get; set; } = 20;

        // 当前状态
        [Export] public string CurrentGoal { get; set; } = "";
        [Export] public int StoryStage { get; set; } = 1;

        // 物品栏
        [Export] public int SpiritStones { get; set; } = 100;

        // 对话历史（运行时生成）
        [Export] public Godot.Collections.Array<string> ConversationHistory { get; set; } = new();

        /// <summary>
        /// 获取修为描述
        /// </summary>
        public string GetCultivationDescription()
        {
            return $"{Realm} {Level} ({SpiritRoot})";
        }

        /// <summary>
        /// 获取关系等级描述
        /// </summary>
        public string GetRelationshipLevel()
        {
            return RelationshipWithPlayer switch
            {
                >= 80 => "生死之交",
                >= 60 => "莫逆之交",
                >= 40 => "点头之交",
                >= 20 => "泛泛之交",
                >= 0 => "陌生",
                >= -20 => "略有嫌隙",
                >= -40 => "不合",
                _ => "敌对"
            };
        }

        /// <summary>
        /// 添加对话历史
        /// </summary>
        public void AddToHistory(string speaker, string message)
        {
            ConversationHistory.Add($"{speaker}: {message}");
            // 限制历史长度
            if (ConversationHistory.Count > 20)
            {
                ConversationHistory.RemoveAt(0);
            }
        }

        /// <summary>
        /// 获取格式化的对话历史
        /// </summary>
        public string GetFormattedHistory()
        {
            return string.Join("\n", ConversationHistory);
        }

        /// <summary>
        /// 更新关系值
        /// </summary>
        public void ModifyRelationship(int delta)
        {
            RelationshipWithPlayer = Mathf.Clamp(RelationshipWithPlayer + delta, -100, 100);
        }

        /// <summary>
        /// 创建示例NPC数据（萧云）
        /// </summary>
        public static NPCData CreateXiaoYun()
        {
            return new NPCData
            {
                Id = "npc_001",
                Name = "萧云",
                Background = "家族弃子",
                Personality = "谨慎",
                Realm = "练气期",
                Level = "后期",
                SpiritRoot = "四灵根",
                CurrentGoal = "获得筑基丹",
                StoryStage = 2,
                SpiritStones = 500,
                RelationshipWithPlayer = 40
            };
        }
    }
}
