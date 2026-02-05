using Godot;
using System;
using System.Collections.Generic;

namespace TianYanShop.World.Sect
{
    /// <summary>
    /// 宗门名称生成器
    /// 生成符合修仙风格的宗门名字
    /// </summary>
    public static class SectNameGenerator
    {
        // ============ 通用前缀 ============
        private static readonly string[] CommonPrefixes = new[]
        {
            "天", "地", "玄", "黄", "宇", "宙", "洪", "荒",
            "太", "初", "混", "沌", "乾", "坤", "阴", "阳",
            "金", "木", "水", "火", "土", "风", "雷", "电",
            "东", "西", "南", "北", "中", "上", "下", "古"
        };

        // ============ 通用后缀 ============
        private static readonly string[] CommonSuffixes = new[]
        {
            "门", "宗", "派", "教", "院", "府", "宫", "殿",
            "阁", "楼", "堂", "社", "盟", "会", "山", "峰",
            "谷", "洞", "潭", "岛", "洲", "域", "境", "界"
        };

        // ============ 各类型专用前缀 ============
        private static readonly Dictionary<SectType, string[]> TypePrefixes = new()
        {
            { SectType.Sword, new[] { "剑", "天剑", "仙剑", "玄剑", "神剑", "万剑", "无双", "绝", "青锋", "紫电" } },
            { SectType.Spell, new[] { "法", "天法", "玄法", "万法", "神法", "大罗", "太虚", "无极", "混元", "乾坤" } },
            { SectType.Alchemy, new[] { "丹", "灵丹", "神丹", "万丹", "药", "灵药", "百草", "神农", "济世", "回春" } },
            { SectType.Artifact, new[] { "器", "宝", "神兵", "万宝", "炼", "锻", "铸", "精工", "鬼斧", "天工" } },
            { SectType.Beast, new[] { "兽", "灵兽", "万兽", "御兽", "妖", "灵", "驯", "百兽", "万灵", "神兽" } },
            { SectType.Formation, new[] { "阵", "玄阵", "万阵", "八卦", "九宫", "奇门", "遁甲", "封", "禁", "困" } },
            { SectType.Body, new[] { "体", "炼体", "金刚", "不坏", "霸", "武", "斗", "战神", "不灭", "永生" } },
            { SectType.Ghost, new[] { "鬼", "阴", "幽", "冥", "魂", "魄", "尸", "骨", "煞", "罗刹" } },
            { SectType.Puppet, new[] { "傀", "儡", "机关", "傀儡", "人偶", "偶", "丝", "线", "控", "操" } },
            { SectType.Music, new[] { "音", "乐", "琴", "瑟", "箫", "笛", "音律", "天籁", "仙音", "梵音" } }
        };

        // ============ 各类型专用后缀 ============
        private static readonly Dictionary<SectType, string[]> TypeSuffixes = new()
        {
            { SectType.Sword, new[] { "剑宗", "剑派", "剑阁", "剑门", "剑院", "剑府" } },
            { SectType.Spell, new[] { "法宗", "道宗", "玄门", "观", "院", "宫" } },
            { SectType.Alchemy, new[] { "丹宗", "药宗", "谷", "轩", "庐", "苑" } },
            { SectType.Artifact, new[] { "器宗", "宝宗", "坊", "阁", "楼", "斋" } },
            { SectType.Beast, new[] { "兽宗", "灵宗", "山", "岭", "峰", "崖" } },
            { SectType.Formation, new[] { "阵宗", "玄宗", "台", "坛", "窟", "洞" } },
            { SectType.Body, new[] { "体宗", "武宗", "寨", "堡", "营", "屯" } },
            { SectType.Ghost, new[] { "鬼宗", "幽宗", "陵", "冢", "穴", "渊" } },
            { SectType.Puppet, new[] { "傀宗", "儡宗", "榭", "舫", "榭", "台" } },
            { SectType.Music, new[] { "音宗", "乐宗", "亭", "榭", "馆", "舍" } }
        };

        // ============ 复合词前缀（双字） ============
        private static readonly string[] CompoundPrefixes = new[]
        {
            "太初", "混元", "无极", "太虚", "乾坤", "阴阳", "五行", "七星",
            "八卦", "九宫", "十方", "三界", "六道", "九霄", "天外", "云外",
            "蓬莱", "方丈", "瀛洲", "昆仑", "峨嵋", "武当", "终南", "峨眉"
        };

        // ============ 复合词后缀（双字） ============
        private static readonly string[] CompoundSuffixes = new[]
        {
            "圣地", "仙境", "洞天", "福地", "玄境", "秘境", "净土", "乐土",
            "神域", "仙域", "灵域", "圣域", "天域", "地域", "人域", "妖域"
        };

        /// <summary>
        /// 生成随机宗门名称
        /// </summary>
        /// <param name="random">随机数生成器</param>
        /// <param name="type">宗门类型</param>
        /// <param name="level">宗门等级</param>
        /// <returns>生成的宗门名称</returns>
        public static string Generate(Random random, SectType type, SectLevel level)
        {
            // 根据等级决定命名风格
            return level switch
            {
                SectLevel.Top => GenerateTopLevelName(random, type),
                SectLevel.Large => GenerateLargeLevelName(random, type),
                SectLevel.Small => GenerateSmallLevelName(random, type),
                _ => GenerateGenericName(random, type)
            };
        }

        /// <summary>
        /// 生成顶级宗门名称（霸气、古典、三字或四字）
        /// </summary>
        private static string GenerateTopLevelName(Random random, SectType type)
        {
            int style = random.Next(5);
            
            return style switch
            {
                0 => $"{GetCompoundPrefix(random)}{GetTypeSuffix(random, type)}",      // 太初剑宗
                1 => $"{GetTypePrefix(random, type)}{GetCompoundSuffix(random)}",        // 剑宗圣地
                2 => $"{GetCommonPrefix(random)}{GetTypePrefix(random, type)}{GetCommonSuffix(random)}", // 天仙剑门
                3 => $"{GetCompoundPrefix(random)}{GetCommonSuffix(random)}",          // 混元洞天
                _ => $"{GetTypePrefix(random, type)}{GetTypeSuffix(random, type)}"      // 仙剑剑宗
            };
        }

        /// <summary>
        /// 生成大型宗门名称（稳重、二字或三字）
        /// </summary>
        private static string GenerateLargeLevelName(Random random, SectType type)
        {
            int style = random.Next(4);
            
            return style switch
            {
                0 => $"{GetTypePrefix(random, type)}{GetCommonSuffix(random)}",      // 仙剑门
                1 => $"{GetCommonPrefix(random)}{GetTypeSuffix(random, type)}",     // 天剑宗
                2 => $"{GetTypePrefix(random, type)}{GetTypeSuffix(random, type)}", // 剑宗派
                _ => $"{GetCompoundPrefix(random)}{GetCommonSuffix(random).Substring(0, 1)}" // 太初门
            };
        }

        /// <summary>
        /// 生成小型宗门名称（朴素、多为二字）
        /// </summary>
        private static string GenerateSmallLevelName(Random random, SectType type)
        {
            int style = random.Next(3);
            
            return style switch
            {
                0 => $"{GetCommonPrefix(random)}{GetCommonSuffix(random).Substring(0, 1)}", // 天门
                1 => $"{GetTypePrefix(random, type)}{GetCommonSuffix(random).Substring(0, 1)}", // 剑门
                _ => $"{GetCommonPrefix(random)}家" // 天家（小型家族）
            };
        }

        /// <summary>
        /// 生成通用名称（备用方案）
        /// </summary>
        private static string GenerateGenericName(Random random, SectType type)
        {
            return $"{GetTypePrefix(random, type)}{GetCommonSuffix(random)}";
        }

        #region Helper Methods

        private static string GetCommonPrefix(Random random)
        {
            return CommonPrefixes[random.Next(CommonPrefixes.Length)];
        }

        private static string GetCommonSuffix(Random random)
        {
            return CommonSuffixes[random.Next(CommonSuffixes.Length)];
        }

        private static string GetCompoundPrefix(Random random)
        {
            return CompoundPrefixes[random.Next(CompoundPrefixes.Length)];
        }

        private static string GetCompoundSuffix(Random random)
        {
            return CompoundSuffixes[random.Next(CompoundSuffixes.Length)];
        }

        private static string GetTypePrefix(Random random, SectType type)
        {
            if (TypePrefixes.TryGetValue(type, out var prefixes))
            {
                return prefixes[random.Next(prefixes.Length)];
            }
            return GetCommonPrefix(random);
        }

        private static string GetTypeSuffix(Random random, SectType type)
        {
            if (TypeSuffixes.TryGetValue(type, out var suffixes))
            {
                return suffixes[random.Next(suffixes.Length)];
            }
            return GetCommonSuffix(random);
        }

        #endregion
    }
}
