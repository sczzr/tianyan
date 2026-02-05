using Godot;
using System;
using System.Collections.Generic;

namespace TianYanShop.World.Sect
{
    /// <summary>
    /// 宗门等级枚举
    /// </summary>
    public enum SectLevel
    {
        Top,    // 顶级宗门
        Large,  // 大型宗门
        Small   // 小型宗门
    }

    /// <summary>
    /// 宗门类型（修炼方向）枚举
    /// </summary>
    public enum SectType
    {
        Sword,      // 剑修
        Spell,      // 法修
        Alchemy,    // 丹修
        Artifact,   // 器修
        Beast,      // 兽修
        Formation,  // 阵修
        Body,       // 体修
        Ghost,      // 鬼修
        Puppet,     // 傀儡
        Music,      // 音修
        Mixed       // 杂修
    }

    /// <summary>
    /// 宗门数据结构体
    /// </summary>
    public struct SectData
    {
        public int Id;                      // 唯一ID
        public string Name;                 // 宗门名称
        public SectLevel Level;             // 宗门等级
        public SectType PrimaryType;        // 主修类型
        public SectType? SecondaryType;     // 副修类型（可为空）
        public Vector2I CenterPosition;     // 中心位置（地块坐标）
        public int InfluenceRadius;         // 势力范围半径
        public int EstablishedYear;         // 建立年份（游戏时间）
        public int Reputation;              // 声望值
        public int MemberCount;             // 弟子数量
        public int SpiritStoneIncome;       // 灵石收入
        public List<int> AlliedSects;       // 结盟宗门ID列表
        public List<int> HostileSects;      // 敌对宗门ID列表

        public SectData(int id, string name, SectLevel level)
        {
            Id = id;
            Name = name;
            Level = level;
            PrimaryType = SectType.Mixed;
            SecondaryType = null;
            CenterPosition = new Vector2I(0, 0);
            InfluenceRadius = 0;
            EstablishedYear = 0;
            Reputation = 0;
            MemberCount = 0;
            SpiritStoneIncome = 0;
            AlliedSects = new List<int>();
            HostileSects = new List<int>();
        }

        /// <summary>
        /// 获取宗门完整类型描述
        /// </summary>
        public string GetTypeDescription()
        {
            string primary = GetSectTypeName(PrimaryType);
            if (SecondaryType.HasValue)
            {
                return $"{primary}/{GetSectTypeName(SecondaryType.Value)}";
            }
            return primary;
        }

        /// <summary>
        /// 获取宗门等级名称
        /// </summary>
        public string GetLevelName()
        {
            return Level switch
            {
                SectLevel.Top => "顶级宗门",
                SectLevel.Large => "大型宗门",
                SectLevel.Small => "小型宗门",
                _ => "未知"
            };
        }

        private static string GetSectTypeName(SectType type)
        {
            return type switch
            {
                SectType.Sword => "剑修",
                SectType.Spell => "法修",
                SectType.Alchemy => "丹修",
                SectType.Artifact => "器修",
                SectType.Beast => "兽修",
                SectType.Formation => "阵修",
                SectType.Body => "体修",
                SectType.Ghost => "鬼修",
                SectType.Puppet => "傀儡",
                SectType.Music => "音修",
                SectType.Mixed => "杂修",
                _ => "未知"
            };
        }
    }

    /// <summary>
    /// 地块宗门归属信息
    /// </summary>
    public struct TileSectInfo
    {
        public int SectId;              // 所属宗门ID（-1表示无归属）
        public float Influence;         // 影响力值（0-1）
        public bool IsCore;             // 是否为核心区域
        public bool IsBorder;           // 是否为边界区域

        public TileSectInfo(int sectId = -1, float influence = 0f, bool isCore = false, bool isBorder = false)
        {
            SectId = sectId;
            Influence = influence;
            IsCore = isCore;
            IsBorder = isBorder;
        }

        public static TileSectInfo Empty => new TileSectInfo(-1, 0f, false, false);

        public bool HasSect => SectId >= 0;
    }
}
