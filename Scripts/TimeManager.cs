using Godot;
using System;
using System.Collections.Generic;

namespace TianYanShop.Scripts
{
	/// <summary>
	/// 时间管理系统 - 驱动世界演变的心跳
	/// 负责游戏时间的流逝、事件广播和离线计算
	/// </summary>
	public partial class TimeManager : Node
	{
		public static TimeManager Instance { get; private set; }

		// 核心时间数据
		public long TotalSecondsElapsed { get; private set; } = 0;
		public int CurrentDay { get; private set; } = 1;
		public int CurrentHour { get; private set; } = 6; // 从早上6点开始
		public int CurrentMinute { get; private set; } = 0;

		// 时间流速控制
		[Export] public float TimeScale { get; set; } = 60f; // 1秒现实时间 = 60秒游戏时间
		[Export] public bool IsPaused { get; set; } = false;

		// 内部计时器
		private double _accumulatedTime = 0.0;
		private int _lastHour = -1;
		private int _lastDay = -1;

		// 离线计算相关
		public DateTime LastSaveTime { get; private set; }

		// 信号
		[Signal]
		public delegate void GameHourPassedEventHandler(int hour);

		[Signal]
		public delegate void GameDayPassedEventHandler(int day);

		[Signal]
		public delegate void TimeScaleChangedEventHandler(float newTimeScale);

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
		/// 初始化时间系统
		/// </summary>
		private void Initialize()
		{
			GD.Print("[TimeManager] 初始化时间系统...");

			// 尝试加载上次保存的时间
			LoadTimeData();

			_lastHour = CurrentHour;
			_lastDay = CurrentDay;

			GD.Print($"[TimeManager] 当前时间: 第{CurrentDay}天 {CurrentHour:D2}:{CurrentMinute:D2}");
		}

		public override void _Process(double delta)
		{
			if (IsPaused) return;

			// 累积时间
			_accumulatedTime += delta * TimeScale;

			// 处理经过的时间
			while (_accumulatedTime >= 1.0)
			{
				AdvanceTime(1); // 推进1秒
				_accumulatedTime -= 1.0;
			}
		}

		/// <summary>
		/// 推进时间
		/// </summary>
		private void AdvanceTime(int seconds)
		{
			TotalSecondsElapsed += seconds;
			CurrentMinute += seconds / 60;

			// 处理分钟进位
			while (CurrentMinute >= 60)
			{
				CurrentMinute -= 60;
				CurrentHour++;

				// 触发小时事件
				if (CurrentHour != _lastHour)
				{
					_lastHour = CurrentHour;
					EmitSignal(SignalName.GameHourPassed, CurrentHour);
				}
			}

			// 处理小时进位
			while (CurrentHour >= 24)
			{
				CurrentHour -= 24;
				CurrentDay++;

				// 触发天数事件
				if (CurrentDay != _lastDay)
				{
					_lastDay = CurrentDay;
					OnDayPassed();
				}
			}
		}

		/// <summary>
		/// 当一天过去时调用
		/// </summary>
		private void OnDayPassed()
		{
			GD.Print($"[TimeManager] 第 {CurrentDay} 天开始");
			EmitSignal(SignalName.GameDayPassed, CurrentDay);

			// 通知GameManager更新天数
			if (GameManager.Instance != null)
			{
				// 这里可以调用GameManager的NextDay方法
			}
		}

		/// <summary>
		/// 快进时间（用于休息、冥想等）
		/// </summary>
		public void FastForward(int hours, int minutes = 0)
		{
			int totalSeconds = hours * 3600 + minutes * 60;

			// 直接推进时间而不逐秒处理
			TotalSecondsElapsed += totalSeconds;

			// 重新计算当前时间
			int totalMinutes = (int)(TotalSecondsElapsed / 60);
			CurrentMinute = totalMinutes % 60;
			CurrentHour = (totalMinutes / 60) % 24;
			CurrentDay = (totalMinutes / 60) / 24 + 1;

			GD.Print($"[TimeManager] 快进 {hours}小时{minutes}分钟，当前时间: 第{CurrentDay}天 {CurrentHour:D2}:{CurrentMinute:D2}");

			// 触发时间和天数事件
			EmitSignal(SignalName.GameHourPassed, CurrentHour);
			if (CurrentDay != _lastDay)
			{
				_lastDay = CurrentDay;
				OnDayPassed();
			}
		}

		/// <summary>
		/// 设置时间流速
		/// </summary>
		public void SetTimeScale(float scale)
		{
			TimeScale = Mathf.Max(0f, scale);
			EmitSignal(SignalName.TimeScaleChanged, TimeScale);
			GD.Print($"[TimeManager] 时间流速设置为: {TimeScale}x");
		}

		/// <summary>
		/// 暂停/恢复时间
		/// </summary>
		public void SetPaused(bool paused)
		{
			IsPaused = paused;
			GD.Print($"[TimeManager] 时间 {(paused ? "暂停" : "恢复")}");
		}

		/// <summary>
		/// 处理离线时间（Catch-up）
		/// </summary>
		public void ProcessOfflineTime(DateTime lastPlayTime)
		{
			TimeSpan offlineDuration = DateTime.Now - lastPlayTime;
			int offlineSeconds = (int)offlineDuration.TotalSeconds;

			if (offlineSeconds <= 0) return;

			// 计算离线期间的游戏时间流逝（通常离线时间流逝较慢）
			int offlineGameSeconds = (int)(offlineSeconds * 0.1f); // 离线时间流逝为正常的10%

			GD.Print($"[TimeManager] 离线 {offlineDuration.TotalHours:F1} 小时，游戏时间推进 {offlineGameSeconds / 3600.0f:F1} 小时");

			// 调用各系统的CatchUp方法
			if (GameManager.Instance != null)
			{
				// 这里可以通知各个管理器进行离线计算
				// 例如：洞天收益、NPC行为模拟、市场变化等
			}
		}

		/// <summary>
		/// 保存时间数据
		/// </summary>
		private void SaveTimeData()
		{
			// 实现时间数据的保存逻辑
			LastSaveTime = DateTime.Now;
		}

		/// <summary>
		/// 加载时间数据
		/// </summary>
		private void LoadTimeData()
		{
			// 实现时间数据的加载逻辑
			// 如果没有保存的数据，使用默认值
		}

		/// <summary>
		/// 获取格式化的当前时间字符串
		/// </summary>
		public string GetFormattedTime()
		{
			string period = CurrentHour switch
			{
				>= 5 and < 7 => "黎明",
				>= 7 and < 11 => "上午",
				>= 11 and < 13 => "正午",
				>= 13 and < 17 => "下午",
				>= 17 and < 19 => "黄昏",
				>= 19 and < 23 => "夜晚",
				_ => "深夜"
			};

			return $"第{CurrentDay}天 {period} {CurrentHour:D2}:{CurrentMinute:D2}";
		}
	}
}
