using Godot;
using System;

using TianYanShop.Core;
using TianYanShop.NPC;

namespace TianYanShop.UI
{
	/// <summary>
	/// 商铺主界面 - 显示商店主要功能和NPC访客
	/// </summary>
	public partial class UIShopMain : Control
	{
		// 节点引用
		private Label _dayLabel;
		private Label _spiritStoneLabel;
		private Label _reputationLabel;
		private VBoxContainer _npcListContainer;
		private Button _craftButton;
		private Button _inventoryButton;
		private Button _nextDayButton;

		public override void _Ready()
		{
			// 获取节点引用
			_dayLabel = GetNode<Label>("TopBar/DayLabel");
			_spiritStoneLabel = GetNode<Label>("TopBar/SpiritStoneLabel");
			_reputationLabel = GetNode<Label>("MainContent/RightPanel/VBoxContainer/ShopReputationLabel");
			_npcListContainer = GetNode<VBoxContainer>("MainContent/LeftPanel/VBoxContainer/NPCListContainer");
			_craftButton = GetNode<Button>("BottomBar/HBoxContainer/CraftButton");
			_inventoryButton = GetNode<Button>("BottomBar/HBoxContainer/InventoryButton");
			_nextDayButton = GetNode<Button>("BottomBar/HBoxContainer/NextDayButton");

			// 连接按钮信号
			_craftButton.Pressed += OnCraftButtonPressed;
			_inventoryButton.Pressed += OnInventoryButtonPressed;
			_nextDayButton.Pressed += OnNextDayButtonPressed;

			// 延迟初始化，等待 GameManager 完成初始化
			CallDeferred(nameof(DeferredInitialize));

			GD.Print("[UIShopMain] 商铺主界面已初始化");
		}

		/// <summary>
		/// 延迟初始化 - 确保 GameManager 已初始化完成
		/// </summary>
		private void DeferredInitialize()
		{
			// 连接信号
			if (GameManager.Instance != null)
			{
				GameManager.Instance.SpiritStonesChanged += OnSpiritStonesChanged;
				GameManager.Instance.DayChanged += OnDayChanged;
				GD.Print("[UIShopMain] 成功连接到 GameManager 信号");
			}
			else
			{
				GD.PrintErr("[UIShopMain] GameManager.Instance 为 null，无法连接信号");
			}

			// 刷新显示
			RefreshUI();
			RefreshNPCList();
		}

		public override void _ExitTree()
		{
			if (GameManager.Instance != null)
			{
				GameManager.Instance.SpiritStonesChanged -= OnSpiritStonesChanged;
				GameManager.Instance.DayChanged -= OnDayChanged;
			}
		}

		/// <summary>
		/// 刷新UI显示
		/// </summary>
		private void RefreshUI()
		{
			if (GameManager.Instance == null)
			{
				GD.PrintErr("[UIShopMain] GameManager.Instance 为 null，无法刷新UI");
				return;
			}
			_dayLabel.Text = $"第 {GameManager.Instance.CurrentDay} 天";
			_spiritStoneLabel.Text = $"灵石: {GameManager.Instance.SpiritStones}";
			_reputationLabel.Text = $"声誉: {GameManager.Instance.Reputation}";
		}

		/// <summary>
		/// 刷新NPC列表
		/// </summary>
		private void RefreshNPCList()
		{
			// 检查 GameManager.Instance 是否为 null
			if (GameManager.Instance == null)
			{
				GD.PrintErr("[UIShopMain] GameManager.Instance 为 null，无法刷新NPC列表");
				return;
			}

			// 清除现有列表
			foreach (Node child in _npcListContainer.GetChildren())
			{
				child.QueueFree();
			}

			// 添加NPC按钮
			foreach (var npc in GameManager.Instance.NPCList)
			{
				var button = new Button
				{
					Text = $"{npc.Name} - {npc.GetCultivationDescription()} (关系: {npc.RelationshipWithPlayer})",
					SizeFlagsHorizontal = SizeFlags.ExpandFill
				};
				button.Pressed += () => OnNPCSelected(npc);
				_npcListContainer.AddChild(button);
			}
		}

		/// <summary>
		/// NPC被选中
		/// </summary>
		private void OnNPCSelected(NPC.NPCData npc)
		{
			GD.Print($"[UIShopMain] 选中NPC: {npc.Name}");
			GameManager.Instance.StartDialogue(npc);
		}

		/// <summary>
		/// 灵石数量变化
		/// </summary>
		private void OnSpiritStonesChanged(int newAmount)
		{
			_spiritStoneLabel.Text = $"灵石: {newAmount}";
		}

		/// <summary>
		/// 天数变化
		/// </summary>
		private void OnDayChanged(int newDay)
		{
			_dayLabel.Text = $"第 {newDay} 天";
		}

		/// <summary>
		/// 制作按钮被按下
		/// </summary>
		private void OnCraftButtonPressed()
		{
			GD.Print("[UIShopMain] 打开制作界面");
			GameManager.Instance.SetGameState(GameManager.GameState.Crafting);
		}

		/// <summary>
		/// 物品栏按钮被按下
		/// </summary>
		private void OnInventoryButtonPressed()
		{
			GD.Print("[UIShopMain] 打开物品栏");
			// TODO: 显示物品栏界面
		}

		/// <summary>
		/// 下一天按钮被按下
		/// </summary>
		private void OnNextDayButtonPressed()
		{
			GD.Print("[UIShopMain] 进入下一天");
			GameManager.Instance.NextDay();
			RefreshNPCList(); // 刷新NPC列表，可能会有新NPC
		}
	}
}
