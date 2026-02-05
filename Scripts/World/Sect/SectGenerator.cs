using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TianYanShop.World.Sect
{
    /// <summary>
    /// 宗门生成器核心类
    /// 负责在地图上生成和放置宗门
    /// </summary>
    public partial class SectGenerator : RefCounted
    {
        #region Constants

        private SectConfig _config;

        #endregion

        #region Fields

        private Random _random;
        private int _mapWidth;
        private int _mapHeight;
        private float[,] _spiritPowerMap;  // 灵力值地图
        private TileSectInfo[,] _sectTerritoryMap;  // 宗门势力范围地图

        // 宗门数据存储
        private Dictionary<int, SectData> _sects;
        private List<SectData> _topSects;
        private List<SectData> _largeSects;
        private List<SectData> _smallSects;

        private int _nextSectId;

        #endregion

        #region Properties

        public int MapWidth => _mapWidth;
        public int MapHeight => _mapHeight;
        public IReadOnlyDictionary<int, SectData> Sects => _sects;
        public IReadOnlyList<SectData> TopSects => _topSects;
        public IReadOnlyList<SectData> LargeSects => _largeSects;
        public IReadOnlyList<SectData> SmallSects => _smallSects;

        #endregion

        #region Constructor

        public SectGenerator(int seed = -1, SectConfig? config = null)
        {
            _random = seed == -1 ? new Random() : new Random(seed);
            _config = config ?? SectConfig.Default;
            _sects = new Dictionary<int, SectData>();
            _topSects = new List<SectData>();
            _largeSects = new List<SectData>();
            _smallSects = new List<SectData>();
            _nextSectId = 1;
        }

        #endregion

        #region Initialization

        /// <summary>
        /// 初始化生成器，设置地图参数
        /// </summary>
        public void Initialize(int width, int height, float[,] spiritPowerMap)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentException("地图尺寸必须大于0");
            
            if (spiritPowerMap == null)
                throw new ArgumentNullException(nameof(spiritPowerMap));
            
            if (spiritPowerMap.GetLength(0) != width || spiritPowerMap.GetLength(1) != height)
                throw new ArgumentException("灵力地图尺寸与指定尺寸不匹配");

            _mapWidth = width;
            _mapHeight = height;
            _spiritPowerMap = spiritPowerMap;
            _sectTerritoryMap = new TileSectInfo[width, height];

            // 初始化势力范围地图
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    _sectTerritoryMap[x, y] = TileSectInfo.Empty;
                }
            }
        }

        #endregion

        #region Generation

        /// <summary>
        /// 生成所有宗门
        /// </summary>
        public void GenerateAllSects()
        {
            if (_spiritPowerMap == null)
                throw new InvalidOperationException("生成器未初始化，请先调用Initialize");

            GD.Print($"[SectGenerator] 开始生成宗门，地图大小: {_mapWidth}x{_mapHeight}");

            // 计算各级宗门数量
            int mapSize = _mapWidth * _mapHeight;
            int topCount = Math.Max(1, mapSize / _config.TopSectRatio);
            int largeCount = Math.Max(1, mapSize / _config.LargeSectRatio);
            int smallCount = Math.Max(1, mapSize / _config.SmallSectRatio);

            GD.Print($"[SectGenerator] 计划生成: 顶级{topCount}个, 大型{largeCount}个, 小型{smallCount}个");

            // 1. 生成顶级宗门
            GenerateTopSects(topCount);

            // 2. 生成大型宗门
            GenerateLargeSects(largeCount);

            // 3. 生成小型宗门
            GenerateSmallSects(smallCount);

            // 4. 计算势力范围
            CalculateTerritories();

            GD.Print($"[SectGenerator] 宗门生成完成，总计: {_sects.Count}个");
        }

        /// <summary>
        /// 生成顶级宗门
        /// </summary>
        private void GenerateTopSects(int count)
        {
            // 收集灵力值高于阈值的地块
            var candidates = new List<(Vector2I pos, float spirit, int radius)>();

            for (int x = 0; x < _mapWidth; x++)
            {
                for (int y = 0; y < _mapHeight; y++)
                {
                    float spirit = _spiritPowerMap[x, y];
                    if (spirit >= _config.TopSectSpiritThreshold)
                    {
                        if (!IsWithinBounds(new Vector2I(x, y), SectLevel.Top))
                            continue;
                        candidates.Add((new Vector2I(x, y), spirit, 0));
                    }
                }
            }

            // 按灵力值降序排序
            candidates.Sort((a, b) => b.spirit.CompareTo(a.spirit));

            GD.Print($"[SectGenerator] 顶级宗门候选地块: {candidates.Count}个");

            // 放置宗门
            int placed = 0;
            int skipped = 0;
            foreach (var candidate in candidates)
            {
                if (placed >= count) break;

                // 检查是否与现有顶级宗门太接近
                bool tooClose = false;
                foreach (var existing in _topSects)
                {
                    float dist = candidate.pos.DistanceTo(existing.CenterPosition);
                    if (dist < _config.MinDistanceTopToTop)
                    {
                        tooClose = true;
                        skipped++;
                        break;
                    }
                }

                if (tooClose) continue;

                // 创建宗门（先不计算势力范围，等所有宗门放置完成后再计算）
                var sect = CreateSect(candidate.pos, SectLevel.Top);
                _topSects.Add(sect);
                _sects[sect.Id] = sect;
                placed++;
            }

            GD.Print($"[SectGenerator] 成功放置 {placed}/{count} 个顶级宗门，跳过 {skipped} 个（距离太近）");
        }

        /// <summary>
        /// 生成大型宗门
        /// </summary>
        private void GenerateLargeSects(int count)
        {
            // 收集灵力值高于阈值的地块
            var candidates = new List<(Vector2I pos, float spirit, int radius)>();

            for (int x = 0; x < _mapWidth; x++)
            {
                for (int y = 0; y < _mapHeight; y++)
                {
                    float spirit = _spiritPowerMap[x, y];
                    if (spirit >= _config.LargeSectSpiritThreshold)
                    {
                        if (!IsWithinBounds(new Vector2I(x, y), SectLevel.Large))
                            continue;
                        candidates.Add((new Vector2I(x, y), spirit, 0));
                    }
                }
            }

            // 按灵力值降序排序
            candidates.Sort((a, b) => b.spirit.CompareTo(a.spirit));

            GD.Print($"[SectGenerator] 大型宗门候选地块: {candidates.Count}个");

            // 放置宗门
            int placed = 0;
            foreach (var candidate in candidates)
            {
                if (placed >= count) break;

                // 检查是否与现有大型宗门太接近
                bool tooCloseToLarge = false;
                foreach (var existing in _largeSects)
                {
                    float dist = candidate.pos.DistanceTo(existing.CenterPosition);
                    if (dist < _config.MinDistanceLargeToLarge)
                    {
                        tooCloseToLarge = true;
                        break;
                    }
                }

                if (tooCloseToLarge) continue;

                // 检查与顶级宗门的距离
                bool tooCloseToTop = false;
                foreach (var topSect in _topSects)
                {
                    float dist = candidate.pos.DistanceTo(topSect.CenterPosition);
                    if (dist < _config.MinDistanceLargeToTop)
                    {
                        tooCloseToTop = true;
                        break;
                    }
                }

                if (tooCloseToTop) continue;

                // 创建宗门（允许在顶级宗门势力范围内，但势力范围计算时顶级优先）
                var sect = CreateSect(candidate.pos, SectLevel.Large);
                _largeSects.Add(sect);
                _sects[sect.Id] = sect;
                placed++;
            }

            GD.Print($"[SectGenerator] 成功放置 {placed}/{count} 个大型宗门");
        }

        /// <summary>
        /// 生成小型宗门
        /// </summary>
        private void GenerateSmallSects(int count)
        {
            // 收集灵力值高于阈值的地块
            var candidates = new List<(Vector2I pos, float spirit, int radius)>();

            for (int x = 0; x < _mapWidth; x++)
            {
                for (int y = 0; y < _mapHeight; y++)
                {
                    float spirit = _spiritPowerMap[x, y];
                    if (spirit >= _config.SmallSectSpiritThreshold)
                    {
                        if (!IsWithinBounds(new Vector2I(x, y), SectLevel.Small))
                            continue;
                        candidates.Add((new Vector2I(x, y), spirit, 0));
                    }
                }
            }

            // 按灵力值降序排序
            candidates.Sort((a, b) => b.spirit.CompareTo(a.spirit));

            GD.Print($"[SectGenerator] 小型宗门候选地块: {candidates.Count}个");

            // 放置宗门
            int placed = 0;
            foreach (var candidate in candidates)
            {
                if (placed >= count) break;

                // 检查是否与现有小型宗门太接近
                bool tooCloseToSmall = false;
                foreach (var existing in _smallSects)
                {
                    float dist = candidate.pos.DistanceTo(existing.CenterPosition);
                    if (dist < _config.MinDistanceSmallToAdvanced)
                    {
                        tooCloseToSmall = true;
                        break;
                    }
                }

                if (tooCloseToSmall) continue;

                // 检查与顶级宗门的距离
                bool tooCloseToTop = false;
                foreach (var topSect in _topSects)
                {
                    float dist = candidate.pos.DistanceTo(topSect.CenterPosition);
                    if (dist < _config.MinDistanceSmallToAdvanced)
                    {
                        tooCloseToTop = true;
                        break;
                    }
                }

                if (tooCloseToTop) continue;

                // 检查与大型宗门的距离
                bool tooCloseToLarge = false;
                foreach (var largeSect in _largeSects)
                {
                    float dist = candidate.pos.DistanceTo(largeSect.CenterPosition);
                    if (dist < _config.MinDistanceSmallToAdvanced)
                    {
                        tooCloseToLarge = true;
                        break;
                    }
                }

                if (tooCloseToLarge) continue;

                // 创建宗门
                var sect = CreateSect(candidate.pos, SectLevel.Small);
                _smallSects.Add(sect);
                _sects[sect.Id] = sect;
                placed++;
            }

            GD.Print($"[SectGenerator] 成功放置 {placed}/{count} 个小型宗门");
        }

        /// <summary>
        /// 创建宗门数据（不立即标记势力范围，统一在CalculateTerritories中处理）
        /// </summary>
        private SectData CreateSect(Vector2I position, SectLevel level)
        {
            int id = _nextSectId++;

            // 分配类型
            (SectType primary, SectType? secondary) = AssignSectTypes(level);

            // 生成名称
            string name = SectNameGenerator.Generate(_random, primary, level);

            var sect = new SectData(id, name, level)
            {
                PrimaryType = primary,
                SecondaryType = secondary,
                CenterPosition = position,
                InfluenceRadius = CalculateInfluenceRadius(level),
                EstablishedYear = _random.Next(100, 5000),  // 随机建立年份
                Reputation = CalculateBaseReputation(level),
                MemberCount = CalculateMemberCount(level),
                SpiritStoneIncome = CalculateSpiritStoneIncome(level)
            };

            // 注意：不在这里标记中心地块，所有势力范围统一在CalculateTerritories中计算
            // 这样可以确保按正确的顺序和规则处理冲突

            return sect;
        }

        /// <summary>
        /// 分配宗门类型
        /// </summary>
        private (SectType primary, SectType? secondary) AssignSectTypes(SectLevel level)
        {
            switch (level)
            {
                case SectLevel.Top:
                    // 50%单专精，50%双专精
                    if (_random.NextDouble() < 0.5)
                    {
                        return (GetRandomSectType(), null);
                    }
                    else
                    {
                        SectType primary = GetRandomSectType();
                        SectType secondary;
                        do
                        {
                            secondary = GetRandomSectType();
                        } while (secondary == primary);
                        return (primary, secondary);
                    }

                case SectLevel.Large:
                    // 80%单专精，20%双专精
                    if (_random.NextDouble() < 0.8)
                    {
                        return (GetRandomSectType(), null);
                    }
                    else
                    {
                        SectType primary = GetRandomSectType();
                        SectType secondary;
                        do
                        {
                            secondary = GetRandomSectType();
                        } while (secondary == primary);
                        return (primary, secondary);
                    }

                case SectLevel.Small:
                default:
                    // 100%杂修
                    return (SectType.Mixed, null);
            }
        }

        /// <summary>
        /// 获取随机宗门类型（不包括Mixed）
        /// </summary>
        private SectType GetRandomSectType()
        {
            // 不包括Mixed（最后一个）
            Array values = Enum.GetValues(typeof(SectType));
            return (SectType)values.GetValue(_random.Next(values.Length - 1))!;
        }

        /// <summary>
        /// 计算势力范围半径
        /// </summary>
        private int CalculateInfluenceRadius(SectLevel level)
        {
            return level switch
            {
                SectLevel.Top => _random.Next(_config.TopInfluenceRange.min, _config.TopInfluenceRange.max + 1),
                SectLevel.Large => _random.Next(_config.LargeInfluenceRange.min, _config.LargeInfluenceRange.max + 1),
                SectLevel.Small => _random.Next(_config.SmallInfluenceRange.min, _config.SmallInfluenceRange.max + 1),
                _ => 5
            };
        }

        /// <summary>
        /// 计算基础声望
        /// </summary>
        private int CalculateBaseReputation(SectLevel level)
        {
            return level switch
            {
                SectLevel.Top => _random.Next(_config.TopReputationRange.min, _config.TopReputationRange.max + 1),
                SectLevel.Large => _random.Next(_config.LargeReputationRange.min, _config.LargeReputationRange.max + 1),
                SectLevel.Small => _random.Next(_config.SmallReputationRange.min, _config.SmallReputationRange.max + 1),
                _ => 30
            };
        }

        /// <summary>
        /// 计算弟子数量
        /// </summary>
        private int CalculateMemberCount(SectLevel level)
        {
            return level switch
            {
                SectLevel.Top => _random.Next(_config.TopMemberRange.min, _config.TopMemberRange.max + 1),
                SectLevel.Large => _random.Next(_config.LargeMemberRange.min, _config.LargeMemberRange.max + 1),
                SectLevel.Small => _random.Next(_config.SmallMemberRange.min, _config.SmallMemberRange.max + 1),
                _ => 50
            };
        }

        /// <summary>
        /// 计算灵石收入
        /// </summary>
        private int CalculateSpiritStoneIncome(SectLevel level)
        {
            return level switch
            {
                SectLevel.Top => _random.Next(_config.TopSpiritStoneIncomeRange.min, _config.TopSpiritStoneIncomeRange.max + 1),
                SectLevel.Large => _random.Next(_config.LargeSpiritStoneIncomeRange.min, _config.LargeSpiritStoneIncomeRange.max + 1),
                SectLevel.Small => _random.Next(_config.SmallSpiritStoneIncomeRange.min, _config.SmallSpiritStoneIncomeRange.max + 1),
                _ => 500
            };
        }

        #endregion

        #region Territory Overlap Check

        /// <summary>
        /// 检查两个势力的范围是否会重叠
        /// </summary>
        /// <param name="pos1">位置1</param>
        /// <param name="radius1">半径1</</param>
        /// <param name="pos2">位置2</param>
        /// <param name="radius2">半径2</param>
        /// <returns>是否重叠</returns>
        private bool WillTerritoriesOverlap(Vector2I pos1, int radius1, Vector2I pos2, int radius2)
        {
            float distance = pos1.DistanceTo(pos2);
            float minDistance = radius1 + radius2;
            return distance < minDistance * 0.95f;
        }

        /// <summary>
        /// 检查两个宗门中心点是否过于接近
        /// </summary>
        private bool IsTooClose(Vector2I pos1, SectLevel level1, Vector2I pos2, SectLevel level2)
        {
            float distance = pos1.DistanceTo(pos2);

            return (level1, level2) switch
            {
                (SectLevel.Top, SectLevel.Top) => distance < _config.MinDistanceTopToTop,
                (SectLevel.Large, SectLevel.Large) => distance < _config.MinDistanceLargeToLarge,
                (SectLevel.Large, SectLevel.Top) => distance < _config.MinDistanceLargeToTop,
                (SectLevel.Top, SectLevel.Large) => distance < _config.MinDistanceLargeToTop,
                (_, SectLevel.Small) => distance < _config.MinDistanceSmallToAdvanced,
                (SectLevel.Small, _) => distance < _config.MinDistanceSmallToAdvanced,
                _ => false
            };
        }

        /// <summary>
        /// 检查位置加上最大势力半径后是否在地图边界内
        /// </summary>
        private bool IsWithinBounds(Vector2I pos, SectLevel level)
        {
            int maxRadius = level switch
            {
                SectLevel.Top => _config.TopInfluenceRange.max,
                SectLevel.Large => _config.LargeInfluenceRange.max,
                SectLevel.Small => _config.SmallInfluenceRange.max,
                _ => 5
            };
            return pos.X - maxRadius >= 0 &&
                   pos.X + maxRadius < _mapWidth &&
                   pos.Y - maxRadius >= 0 &&
                   pos.Y + maxRadius < _mapHeight;
        }

        /// <summary>
        /// 检查位置是否在顶级宗门的势力范围内
        /// </summary>
        private bool IsInsideAnyTopTerritory(Vector2I pos)
        {
            foreach (var topSect in _topSects)
            {
                float dist = topSect.CenterPosition.DistanceTo(pos);
                if (dist <= topSect.InfluenceRadius)
                {
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region Territory Calculation

        /// <summary>
        /// 计算所有宗门的势力范围（按等级顺序）
        /// </summary>
        private void CalculateTerritories()
        {
            GD.Print("[SectGenerator] 开始计算势力范围...");

            // 先计算顶级宗门（最高优先级，不会被覆盖）
            foreach (var sect in _topSects)
            {
                CalculateSectTerritory(sect);
            }

            // 再计算大型宗门（可能被顶级宗门覆盖）
            foreach (var sect in _largeSects)
            {
                CalculateSectTerritory(sect);
            }

            // 最后计算小型宗门（可能被所有高级宗门覆盖）
            foreach (var sect in _smallSects)
            {
                CalculateSectTerritory(sect);
            }

            GD.Print("[SectGenerator] 势力范围计算完成");
        }

        /// <summary>
        /// 计算单个宗门的势力范围
        /// 根据宗门等级实现不同的冲突处理规则
        /// </summary>
        private void CalculateSectTerritory(SectData sect)
        {
            int radius = sect.InfluenceRadius;
            Vector2I center = sect.CenterPosition;

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    int x = center.X + dx;
                    int y = center.Y + dy;

                    if (!IsValidPosition(x, y))
                        continue;

                    // 计算距离
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    if (distance > radius)
                        continue;

                    // 计算影响力（中心最强，边缘减弱）
                    float influence = 1.0f - (distance / radius);

                    // 核心区域强化
                    bool isCore = distance <= radius * 0.3f;
                    bool isBorder = distance >= radius * 0.8f;

                    // 检查冲突
                    var conflictResult = CheckTerritoryConflict(x, y, sect, influence);
                    if (conflictResult.ShouldSkip)
                    {
                        continue;
                    }

                    // 如果可以覆盖现有宗门
                    if (conflictResult.CanOverride)
                    {
                        _sectTerritoryMap[x, y] = new TileSectInfo(
                            sect.Id, influence, isCore, isBorder);
                    }
                }
            }
        }

        /// <summary>
        /// 检查势力范围冲突
        /// 返回 (ShouldSkip: 是否跳过, CanOverride: 是否可以覆盖)
        /// </summary>
        private (bool ShouldSkip, bool CanOverride) CheckTerritoryConflict(int x, int y, SectData sect, float influence)
        {
            var existingInfo = _sectTerritoryMap[x, y];

            if (!existingInfo.HasSect)
            {
                // 没有归属，直接占领
                return (false, true);
            }

            // 获取现有宗门
            if (!_sects.TryGetValue(existingInfo.SectId, out var existingSect))
            {
                return (false, true);
            }

            // 规则1: 顶级宗门之间不重叠
            if (sect.Level == SectLevel.Top && existingSect.Level == SectLevel.Top)
            {
                // 按影响力决定（同级之间不覆盖）
                if (existingInfo.Influence >= influence)
                {
                    return (true, false); // 跳过，不覆盖
                }
                else
                {
                    return (false, true); // 覆盖
                }
            }

            // 规则2: 大型宗门之间不重叠
            if (sect.Level == SectLevel.Large && existingSect.Level == SectLevel.Large)
            {
                if (existingInfo.Influence >= influence)
                {
                    return (true, false);
                }
                else
                {
                    return (false, true);
                }
            }

            // 规则3: 顶级宗门可以覆盖大型宗门
            if (sect.Level == SectLevel.Top && existingSect.Level == SectLevel.Large)
            {
                return (false, true); // 顶级覆盖大型，允许
            }

            // 规则4: 大型宗门不能覆盖顶级宗门
            if (sect.Level == SectLevel.Large && existingSect.Level == SectLevel.Top)
            {
                return (true, false); // 大型被顶级覆盖，跳过
            }

            // 规则5: 小型宗门可以被任何高级宗门覆盖
            if (sect.Level == SectLevel.Small && existingSect.Level != SectLevel.Small)
            {
                return (true, false); // 跳过，被高级覆盖
            }

            // 规则6: 任何宗门都可以覆盖小型宗门
            if (sect.Level != SectLevel.Small && existingSect.Level == SectLevel.Small)
            {
                return (false, true); // 覆盖小型
            }

            // 默认规则：按影响力比较
            if (existingInfo.Influence >= influence)
            {
                return (true, false);
            }

            return (false, true);
        }

        #endregion

        #region Public API

        /// <summary>
        /// 获取指定位置的宗门
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <returns>宗门数据，若无归属则返回null</returns>
        public SectData? GetSectAtPosition(int x, int y)
        {
            if (!IsValidPosition(x, y))
                return null;

            var info = _sectTerritoryMap[x, y];
            if (!info.HasSect)
                return null;

            return _sects.TryGetValue(info.SectId, out var sect) ? sect : null;
        }

        /// <summary>
        /// 获取指定位置的宗门（Vector2I版本）
        /// </summary>
        public SectData? GetSectAtPosition(Vector2I position)
        {
            return GetSectAtPosition(position.X, position.Y);
        }

        /// <summary>
        /// 按等级获取宗门列表
        /// </summary>
        /// <param name="level">宗门等级</param>
        /// <returns>该等级的宗门列表</returns>
        public IReadOnlyList<SectData> GetSectsByLevel(SectLevel level)
        {
            return level switch
            {
                SectLevel.Top => _topSects,
                SectLevel.Large => _largeSects,
                SectLevel.Small => _smallSects,
                _ => new List<SectData>()
            };
        }

        /// <summary>
        /// 检查位置是否在指定宗门的势力范围内
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <param name="sectId">宗门ID</param>
        /// <returns>是否在势力范围内</returns>
        public bool IsInSectTerritory(int x, int y, int sectId)
        {
            if (!IsValidPosition(x, y))
                return false;

            var info = _sectTerritoryMap[x, y];
            return info.SectId == sectId;
        }

        /// <summary>
        /// 获取指定位置的势力信息
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <returns>势力信息</returns>
        public TileSectInfo GetTerritoryInfo(int x, int y)
        {
            if (!IsValidPosition(x, y))
                return TileSectInfo.Empty;

            return _sectTerritoryMap[x, y];
        }

        /// <summary>
        /// 获取指定宗门的势力范围地块列表
        /// </summary>
        /// <param name="sectId">宗门ID</param>
        /// <returns>势力范围内的所有地块坐标</returns>
        public List<Vector2I> GetSectTerritoryTiles(int sectId)
        {
            var tiles = new List<Vector2I>();

            for (int x = 0; x < _mapWidth; x++)
            {
                for (int y = 0; y < _mapHeight; y++)
                {
                    if (_sectTerritoryMap[x, y].SectId == sectId)
                    {
                        tiles.Add(new Vector2I(x, y));
                    }
                }
            }

            return tiles;
        }

        /// <summary>
        /// 获取最近的宗门
        /// </summary>
        /// <param name="position">查询位置</param>
        /// <param name="maxDistance">最大搜索距离（-1表示无限制）</param>
        /// <returns>最近的宗门数据，若无可用的则返回null</returns>
        public SectData? GetNearestSect(Vector2I position, float maxDistance = -1)
        {
            SectData? nearest = null;
            float minDist = float.MaxValue;

            foreach (var sect in _sects.Values)
            {
                float dist = sect.CenterPosition.DistanceTo(position);
                
                if (maxDistance > 0 && dist > maxDistance)
                    continue;

                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = sect;
                }
            }

            return nearest;
        }

        /// <summary>
        /// 获取指定范围内的所有宗门
        /// </summary>
        /// <param name="center">中心位置</param>
        /// <param name="radius">搜索半径</param>
        /// <returns>范围内的宗门列表</returns>
        public List<SectData> GetSectsInRange(Vector2I center, float radius)
        {
            var result = new List<SectData>();
            float radiusSq = radius * radius;

            foreach (var sect in _sects.Values)
            {
                float distSq = center.DistanceSquaredTo(sect.CenterPosition);
                if (distSq <= radiusSq)
                {
                    result.Add(sect);
                }
            }

            return result;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// 检查坐标是否在地图范围内
        /// </summary>
        private bool IsValidPosition(int x, int y)
        {
            return x >= 0 && x < _mapWidth && y >= 0 && y < _mapHeight;
        }

        #endregion

        #region Debug Methods

        /// <summary>
        /// 打印生成统计信息
        /// </summary>
        public void PrintStatistics()
        {
            GD.Print("========== 宗门生成统计 ==========");
            GD.Print($"地图大小: {_mapWidth}x{_mapHeight}");
            GD.Print($"顶级宗门: {_topSects.Count}个");
            GD.Print($"大型宗门: {_largeSects.Count}个");
            GD.Print($"小型宗门: {_smallSects.Count}个");
            GD.Print($"总计: {_sects.Count}个");
            GD.Print("==================================");
        }

        /// <summary>
        /// 导出宗门列表（用于调试）
        /// </summary>
        public List<string> ExportSectList()
        {
            var list = new List<string>();
            
            list.Add("=== 顶级宗门 ===");
            foreach (var sect in _topSects.OrderBy(s => s.Id))
            {
                list.Add($"[{sect.Id}] {sect.Name} - {sect.GetTypeDescription()} - 位置:({sect.CenterPosition.X},{sect.CenterPosition.Y})");
            }

            list.Add("\n=== 大型宗门 ===");
            foreach (var sect in _largeSects.OrderBy(s => s.Id))
            {
                list.Add($"[{sect.Id}] {sect.Name} - {sect.GetTypeDescription()} - 位置:({sect.CenterPosition.X},{sect.CenterPosition.Y})");
            }

            list.Add("\n=== 小型宗门 ===");
            foreach (var sect in _smallSects.OrderBy(s => s.Id))
            {
                list.Add($"[{sect.Id}] {sect.Name} - {sect.GetTypeDescription()} - 位置:({sect.CenterPosition.X},{sect.CenterPosition.Y})");
            }

            return list;
        }

        #endregion
    }
}
