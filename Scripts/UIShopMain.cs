using Godot;
using System;

namespace TianYanShop.Scripts
{
    /// <summary>
    /// 商铺主界面 - 显示商店主要功能和NPC访客
    /// </summary>
    public partial class UIShopMain : Control
    {
        [Export] public Label DayLabel;
        [Export] public Label SpiritStoneLabel;
        [Export] public Label ReputationLabel;
        [Export] public VBoxContainer NPCListContainer;
        [Export] public PackedScene NPCListItemPrefab;
        [Export] public Control BottomPanel;
        [Export] public Button CraftButton;
        [Export] public Button InventoryButton;
        [Export] public Button NextDayButton;

        public override void _Ready()
        {
            // 连接信号
            GameManager.Instance.SpiritStonesChanged += OnSpiritStonesChanged;
            GameManager.Instance.DayChanged += OnDayChanged;

            // 连接按钮信号
            CraftButton.Pressed += OnCraftButtonPressed;
            InventoryButton.Pressed += OnInventoryButtonPressed;
            NextDayButton.Pressed += OnNextDayButtonPressed;

            // 刷新显示
            RefreshUI();
            RefreshNPCList();

            GD.Print("[UIShopMain] 商铺主界面已初始化");
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
            DayLabel.Text = $"第 {GameManager.Instance.CurrentDay} 天";
            SpiritStoneLabel.Text = $"灵石: {GameManager.Instance.SpiritStones}";
            ReputationLabel.Text = $"声誉: {GameManager.Instance.Reputation}";
        }

        /// <summary>
        /// 刷新NPC列表
        /// </summary>
        private void RefreshNPCList()
        {
            // 清除现有列表
            foreach (Node child in NPCListContainer.GetChildren())
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
                NPCListContainer.AddChild(button);
            }
        }

        /// <summary>
        /// NPC被选中
        /// </summary>
        private void OnNPCSelected(NPCData npc)
        {
            GD.Print($"[UIShopMain] 选中NPC: {npc.Name}");
            GameManager.Instance.StartDialogue(npc);
        }

        /// <summary>
        /// 灵石数量变化
        /// </summary>
        private void OnSpiritStonesChanged(int newAmount)
        {
            SpiritStoneLabel.Text = $"灵石: {newAmount}";
        }

        /// <summary>
        /// 天数变化
        /// </summary>
        private void OnDayChanged(int newDay)
        {
            DayLabel.Text = $"第 {newDay} 天";
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
