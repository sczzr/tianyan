using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

using TianYanShop.Core;
using TianYanShop.Data;

namespace TianYanShop.Entity
{
	/// <summary>
	/// 天篆管理系统 - 游戏核心系统之一
	/// 负责天篆的加载、组合、效果计算与和谐度评估
	/// </summary>
	public partial class TianZhuanManager : Node
	{
		public static TianZhuanManager Instance { get; private set; }

		// 天篆数据存储
		public Dictionary<string, TianZhuanData> AllTianZhuans { get; private set; } = new();

		// 道纹音律谐振频率表 - 用于计算和谐度
		private Dictionary<(string, string), float> _daoPatternResonanceTable = new();

		// 信号
		[Signal]
		public delegate void TianZhuanActivatedEventHandler(string tianZhuanId, Variant target);

		[Signal]
		public delegate void TianZhuanCombinedEventHandler(Godot.Collections.Array<string> tianZhuanIds, float harmonyLevel);

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

		/// <summary>
		/// 初始化天篆系统
		/// </summary>
		private void Initialize()
		{
			GD.Print("[TianZhuanManager] 初始化天篆系统...");

			// 从 DataManager 加载天篆数据（如果可用）
			if (DataManager.Instance != null && DataManager.Instance.TianZhuans.Count > 0)
			{
				AllTianZhuans = DataManager.Instance.TianZhuans;
				GD.Print($"[TianZhuanManager] 已从 DataManager 加载 {AllTianZhuans.Count} 个天篆");
			}
			else
			{
				GD.Print("[TianZhuanManager] DataManager 暂无天篆数据，等待后续加载...");
			}

			// 初始化道纹音律谐振频率表
			InitializeResonanceTable();
		}

		/// <summary>
		/// 初始化谐振频率表
		/// </summary>
		private void InitializeResonanceTable()
		{
			// 同道枢内的天篆具有高谐振度
			// 协同道枢之间具有中等谐振度
			// 冲突道枢之间具有低/负谐振度

			// 示例：万象道枢（元素）内部的高谐振
			_daoPatternResonanceTable[("Fire", "Water")] = -0.5f; // 水火相克
			_daoPatternResonanceTable[("Fire", "Wood")] = 0.8f;  // 木生火
			_daoPatternResonanceTable[("Fire", "Earth")] = 0.6f; // 火生土
			_daoPatternResonanceTable[("Metal", "Water")] = 0.7f; // 金生水

			// 更多谐振关系可以在这里添加...
		}

		#region 天篆检索接口

		/// <summary>
		/// 通过ID获取天篆
		/// </summary>
		public TianZhuanData GetTianZhuan(string id)
		{
			return AllTianZhuans.TryGetValue(id, out var tz) ? tz : null;
		}

		/// <summary>
		/// 通过名称获取天篆
		/// </summary>
		public TianZhuanData GetTianZhuanByName(string name)
		{
			foreach (var tz in AllTianZhuans.Values)
			{
				if (tz.Name == name)
					return tz;
			}
			return null;
		}

		/// <summary>
		/// 通过道枢获取所有天篆
		/// </summary>
		public List<TianZhuanData> GetTianZhuansByDaoHub(string daoHubName)
		{
			return AllTianZhuans.Values
				.Where(tz => tz.AffiliatedDaoHub == daoHubName)
				.ToList();
		}

		/// <summary>
		/// 通过功能类别获取天篆
		/// </summary>
		public List<TianZhuanData> GetTianZhuansByCategory(string category)
		{
			return AllTianZhuans.Values
				.Where(tz => tz.FunctionCategories != null && tz.FunctionCategories.Contains(category))
				.ToList();
		}

		#endregion

		#region 天篆激活与组合

		/// <summary>
		/// 激活单个天篆
		/// </summary>
		public void ActivateTianZhuan(string tianZhuanId, Variant target, Variant caster = default)
		{
			var tz = GetTianZhuan(tianZhuanId);
			if (tz == null)
			{
				GD.PrintErr($"[TianZhuanManager] 未找到天篆: {tianZhuanId}");
				return;
			}

			GD.Print($"[TianZhuanManager] 激活天篆: {tz.Name} ({tianZhuanId})");

			// 解析天篆效果并应用
			ApplyTianZhuanEffect(tz, target, caster);

			EmitSignal(SignalName.TianZhuanActivated, tianZhuanId, target);
		}

		/// <summary>
		/// 组合激活多个天篆
		/// </summary>
		public void CombineTianZhuans(List<string> tianZhuanIds, Variant target, Variant caster = default, Dictionary<string, Variant> runtimeParams = null)
		{
			if (tianZhuanIds == null || tianZhuanIds.Count == 0)
			{
				GD.PrintErr("[TianZhuanManager] 组合天篆列表为空");
				return;
			}

			// 获取天篆数据
			var tianZhuans = new List<TianZhuanData>();
			foreach (var id in tianZhuanIds)
			{
				var tz = GetTianZhuan(id);
				if (tz != null)
					tianZhuans.Add(tz);
			}

			// 计算组合和谐度
			float harmonyLevel = CalculateHarmony(tianZhuans);
			GD.Print($"[TianZhuanManager] 组合 {tianZhuanIds.Count} 个天篆，和谐度: {harmonyLevel:P0}");

			// 应用组合效果
			ApplyCombinedEffect(tianZhuans, harmonyLevel, target, caster, runtimeParams);

			// 转换为Godot数组发射信号
			var godotArray = new Godot.Collections.Array<string>();
			foreach (var id in tianZhuanIds)
			{
				godotArray.Add(id);
			}
			EmitSignal(SignalName.TianZhuanCombined, godotArray, harmonyLevel);
		}

		/// <summary>
		/// 预测组合效果（用于"衍化创法"系统）
		/// </summary>
		public PredictedEffect PredictEffect(List<string> tianZhuanIds, Variant caster, Dictionary<string, Variant> potentialParams)
		{
			var tianZhuans = new List<TianZhuanData>();
			foreach (var id in tianZhuanIds)
			{
				var tz = GetTianZhuan(id);
				if (tz != null)
					tianZhuans.Add(tz);
			}

			float harmony = CalculateHarmony(tianZhuans);

			// 基于和谐度和天篆属性预测效果
			var prediction = new PredictedEffect
			{
				SuccessRate = harmony > 0 ? harmony * 0.8f + 0.2f : 0.1f,
				PredictedHarmony = harmony,
				EstimatedPower = CalculateEstimatedPower(tianZhuans, harmony),
				PotentialRisks = harmony < 0.3f ? new List<string> { "法则冲突", "灵气反噬" } : new List<string>()
			};

			return prediction;
		}

		#endregion

		#region 私有辅助方法

		/// <summary>
		/// 计算天篆组合的和谐度
		/// </summary>
		private float CalculateHarmony(List<TianZhuanData> tianZhuans)
		{
			if (tianZhuans.Count == 0) return 0f;
			if (tianZhuans.Count == 1) return 1f;

			float totalResonance = 0f;
			int pairCount = 0;

			// 计算所有天篆对的谐振值
			for (int i = 0; i < tianZhuans.Count; i++)
			{
				for (int j = i + 1; j < tianZhuans.Count; j++)
				{
					float resonance = CalculatePairResonance(tianZhuans[i], tianZhuans[j]);
					totalResonance += resonance;
					pairCount++;
				}
			}

			// 考虑组合顺序（有序天篆序列的和谐度）
			float sequenceBonus = 1f;
			if (tianZhuans.Count > 2)
			{
				// 检查是否有协同效应的连续组合
				for (int i = 0; i < tianZhuans.Count - 2; i++)
				{
					var combo = new List<TianZhuanData>
					{
						tianZhuans[i],
						tianZhuans[i + 1],
						tianZhuans[i + 2]
					};
					if (HasSynergyEffect(combo))
					{
						sequenceBonus += 0.1f;
					}
				}
			}

			float averageResonance = pairCount > 0 ? totalResonance / pairCount : 0f;
			float finalHarmony = Mathf.Clamp(averageResonance * sequenceBonus, -1f, 1f);

			return finalHarmony;
		}

		/// <summary>
		/// 计算两个天篆之间的谐振值
		/// </summary>
		private float CalculatePairResonance(TianZhuanData a, TianZhuanData b)
		{
			// 1. 检查显式谐振表
			var key = (a.ID, b.ID);
			var reverseKey = (b.ID, a.ID);

			if (_daoPatternResonanceTable.ContainsKey(key))
				return _daoPatternResonanceTable[key];
			if (_daoPatternResonanceTable.ContainsKey(reverseKey))
				return _daoPatternResonanceTable[reverseKey];

			// 2. 同道枢判断
			if (a.AffiliatedDaoHub == b.AffiliatedDaoHub)
			{
				return 0.8f; // 同道枢天篆具有高谐振
			}

			// 3. 检查道枢关系
			if (a.DaoHubRelationships?.CollaborativeDaoHubs?.Contains(b.AffiliatedDaoHub) == true ||
				b.DaoHubRelationships?.CollaborativeDaoHubs?.Contains(a.AffiliatedDaoHub) == true)
			{
				return 0.6f; // 协同道枢间的中等谐振
			}

			// 4. 功能类别相似度
			if (a.FunctionCategories != null && b.FunctionCategories != null)
			{
				var commonCategories = a.FunctionCategories.Intersect(b.FunctionCategories).ToList();
				if (commonCategories.Count > 0)
				{
					return 0.3f + (0.1f * commonCategories.Count);
				}
			}

			// 默认：无明显关系，略有干扰
			return 0.1f;
		}

		/// <summary>
		/// 检查一组天篆是否有协同效应
		/// </summary>
		private bool HasSynergyEffect(List<TianZhuanData> combo)
		{
			// 检查连续组合是否有特殊效果
			// 这里可以实现特定的三元素协同规则
			// 例如：火+木+风 = 燎原之势

			if (combo.Count == 3)
			{
				var ids = combo.Select(t => t.ID).ToList();
				// 示例：火+风+雷 = 天劫之雷
				if (ids.Contains("Fire") && ids.Contains("Wind") && ids.Contains("Thunder"))
					return true;
			}

			return false;
		}

		/// <summary>
		/// 应用单个天篆效果
		/// </summary>
		private void ApplyTianZhuanEffect(TianZhuanData tz, Variant target, Variant caster)
		{
			// 解析技术参数并应用效果
			// 这里需要根据具体的战斗/效果系统来实现

			GD.Print($"[TianZhuanManager] 应用天篆效果: {tz.Name}");

			// 示例：解析并应用效果
			if (tz.TechnicalParameters != null)
			{
				foreach (var param in tz.TechnicalParameters)
				{
					GD.Print($"  - {param.Key}: {param.Value}");
				}
			}
		}

		/// <summary>
		/// 应用组合天篆效果
		/// </summary>
		private void ApplyCombinedEffect(List<TianZhuanData> tianZhuans, float harmonyLevel, Variant target, Variant caster, Dictionary<string, Variant> runtimeParams)
		{
			// 根据和谐度调整效果
			float effectMultiplier = 1f + (harmonyLevel * 0.5f);

			if (harmonyLevel < 0)
			{
				// 负和谐度：法则冲突，产生反噬效果
				GD.Print($"[TianZhuanManager] 警告：天篆组合和谐度为负 ({harmonyLevel:P0})，可能发生法则冲突！");
				// 触发反噬效果...
			}

			// 应用组合效果
			foreach (var tz in tianZhuans)
			{
				ApplyTianZhuanEffect(tz, target, caster);
			}

			// 触发特殊组合效果（如果有）
			if (tianZhuans.Count >= 2 && harmonyLevel > 0.7f)
			{
				TriggerSynergyEffect(tianZhuans, harmonyLevel, target, caster);
			}
		}

		/// <summary>
		/// 触发协同效果
		/// </summary>
		private void TriggerSynergyEffect(List<TianZhuanData> tianZhuans, float harmonyLevel, Variant target, Variant caster)
		{
			GD.Print($"[TianZhuanManager] 触发天篆协同效果！和谐度: {harmonyLevel:P0}");
			// 实现具体的协同效果逻辑
		}

		/// <summary>
		/// 计算预计威力
		/// </summary>
		private float CalculateEstimatedPower(List<TianZhuanData> tianZhuans, float harmony)
		{
			float basePower = 0f;
			foreach (var tz in tianZhuans)
			{
				// 基于天篆等级计算基础威力
				float tzPower = tz.Rank switch
				{
					TianZhuanRank.Core => 10f,
					TianZhuanRank.HighTier => 20f,
					_ => 5f
				};
				basePower += tzPower;
			}

			// 和谐度影响最终威力
			float harmonyMultiplier = 1f + (harmony * 0.8f);
			return basePower * harmonyMultiplier;
		}
	}

	/// <summary>
	/// 预测效果类
	/// </summary>
	public class PredictedEffect
	{
		public float SuccessRate { get; set; }
		public float PredictedHarmony { get; set; }
		public float EstimatedPower { get; set; }
		public List<string> PotentialRisks { get; set; } = new();
		public string PredictedEffectDescription { get; set; }
	}

	#endregion
}
