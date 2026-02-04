using Godot;
using System;
using System.Collections.Generic;

using TianYanShop.Core;

namespace TianYanShop.Entity
{
	/// <summary>
	/// 制作系统 - 炼制丹药、符箓等物品
	/// </summary>
	public partial class CraftingSystem : Control
	{
		// 节点引用
		private TabContainer _recipeTabs;
		private VBoxContainer _recipeListContainer;
		private Control _recipeDetailPanel;
		private RichTextLabel _recipeNameLabel;
		private RichTextLabel _recipeDescriptionLabel;
		private VBoxContainer _ingredientsContainer;
		private Button _craftButton;
		private Button _closeButton;

		// 当前选中的配方
		private RecipeData _selectedRecipe;

		// 配方数据库
		private Dictionary<string, RecipeData> _recipes = new();

		// 背包管理器引用
		private InventoryManager _inventory => InventoryManager.Instance;

		public override void _Ready()
		{
			// 获取节点引用
			_recipeTabs = GetNode<TabContainer>("MainPanel/HBoxContainer/LeftPanel/VBoxContainer/TabContainer");
			_recipeListContainer = GetNode<VBoxContainer>("MainPanel/HBoxContainer/LeftPanel/VBoxContainer/TabContainer/丹药/MarginContainer/RecipeList");
			_recipeDetailPanel = GetNode<Control>("MainPanel/HBoxContainer/RightPanel/VBoxContainer/RecipeDetailPanel");
			_recipeNameLabel = GetNode<RichTextLabel>("MainPanel/HBoxContainer/RightPanel/VBoxContainer/RecipeDetailPanel/VBoxContainer/RecipeNameLabel");
			_recipeDescriptionLabel = GetNode<RichTextLabel>("MainPanel/HBoxContainer/RightPanel/VBoxContainer/RecipeDetailPanel/VBoxContainer/RecipeDescriptionLabel");
			_ingredientsContainer = GetNode<VBoxContainer>("MainPanel/HBoxContainer/RightPanel/VBoxContainer/RecipeDetailPanel/VBoxContainer/IngredientsContainer");
			_craftButton = GetNode<Button>("MainPanel/HBoxContainer/RightPanel/VBoxContainer/CraftButton");
			_closeButton = GetNode<Button>("MainPanel/CloseButton");

			// 连接按钮信号
			_craftButton.Pressed += OnCraftButtonPressed;
			_closeButton.Pressed += OnCloseButtonPressed;

			// 连接标签切换信号
			_recipeTabs.TabChanged += OnTabChanged;

			// 初始化数据
			InitializeRecipes();

			// 初始隐藏
			Hide();

			// 延迟连接信号，等待 GameManager 初始化完成
			CallDeferred(nameof(DeferredConnectSignals));

			// 延迟检查当前游戏状态，确保UI可见性正确
			CallDeferred(nameof(DeferredCheckVisibility));

			GD.Print("[CraftingSystem] 制作系统已初始化");
		}

		/// <summary>
		/// 延迟连接信号 - 确保 GameManager 已初始化完成
		/// </summary>
		private void DeferredConnectSignals()
		{
			// 监听游戏状态
			if (GameManager.Instance != null)
			{
				GameManager.Instance.GameStateChanged += OnGameStateChanged;
				GD.Print("[CraftingSystem] 成功连接到 GameManager 信号");
			}
			else
			{
				GD.PrintErr("[CraftingSystem] GameManager.Instance 为 null，无法连接信号");
			}
		}

		/// <summary>
		/// 延迟检查可见性 - 确保在初始化时根据当前游戏状态正确显示/隐藏
		/// </summary>
		private void DeferredCheckVisibility()
		{
			if (GameManager.Instance != null)
			{
				// 根据当前游戏状态设置可见性
				if (GameManager.Instance.CurrentState == GameManager.GameState.Crafting)
				{
					Show();
					RefreshRecipeList();
					GD.Print("[CraftingSystem] 初始化时检测到当前状态为 Crafting，显示UI");
				}
				else
				{
					Hide();
				}
			}
			else
			{
				GD.PrintErr("[CraftingSystem] GameManager.Instance 为 null，无法检查可见性");
			}
		}

		public override void _ExitTree()
		{
			if (GameManager.Instance != null)
			{
				GameManager.Instance.GameStateChanged -= OnGameStateChanged;
			}
		}

		private void OnGameStateChanged(GameManager.GameState newState)
		{
			if (newState == GameManager.GameState.Crafting)
			{
				Show();
				RefreshRecipeList();
			}
			else
			{
				Hide();
			}
		}

		/// <summary>
		/// 初始化配方数据库
		/// </summary>
		private void InitializeRecipes()
		{
			// 丹药配方
			_recipes["qi_gathering_pill"] = new RecipeData
			{
				Id = "qi_gathering_pill",
				Name = "聚气丹",
				Description = "帮助练气期修士凝聚灵气的丹药，可略微提升修炼速度。",
				Category = RecipeCategory.Pill,
				RequiredRealm = 0, // 练气期
				Ingredients = new Dictionary<string, int>
				{
					{ "spirit_grass", 3 },
					{ "spirit_water", 1 }
				},
				BasePrice = 50,
				SuccessRate = 0.9f
			};

			_recipes["foundation_pill"] = new RecipeData
			{
				Id = "foundation_pill",
				Name = "筑基丹",
				Description = "珍贵的丹药，可帮助练气大圆满修士突破至筑基期。",
				Category = RecipeCategory.Pill,
				RequiredRealm = 1, // 筑基期炼制
				Ingredients = new Dictionary<string, int>
				{
					{ "spirit_grass", 10 },
					{ "moonflower", 3 },
					{ "beast_core", 1 }
				},
				BasePrice = 1000,
				SuccessRate = 0.6f
			};

			// 符箓配方
			_recipes["fire_talisman"] = new RecipeData
			{
				Id = "fire_talisman",
				Name = "火球符",
				Description = "基础的攻击符箓，激发后可释放火球攻击敌人。",
				Category = RecipeCategory.Talisman,
				RequiredRealm = 0,
				Ingredients = new Dictionary<string, int>
				{
					{ "talisman_paper", 1 },
					{ "cinnabar", 1 }
				},
				BasePrice = 30,
				SuccessRate = 0.95f
			};

			_recipes["protection_talisman"] = new RecipeData
			{
				Id = "protection_talisman",
				Name = "护身符",
				Description = "防御型符箓，可抵挡一次不超过筑基期的攻击。",
				Category = RecipeCategory.Talisman,
				RequiredRealm = 0,
				Ingredients = new Dictionary<string, int>
				{
					{ "talisman_paper", 2 },
					{ "cinnabar", 2 },
					{ "spirit_grass", 1 }
				},
				BasePrice = 80,
				SuccessRate = 0.85f
			};

			GD.Print($"[CraftingSystem] 配方初始化完成，共 {_recipes.Count} 个配方");
		}


		/// <summary>
		/// 标签页切换
		/// </summary>
		private void OnTabChanged(long tabIndex)
		{
			RefreshRecipeList();
		}

		/// <summary>
		/// 刷新配方列表
		/// </summary>
		private void RefreshRecipeList()
		{
			// 清除现有列表
			foreach (Node child in _recipeListContainer.GetChildren())
			{
				child.QueueFree();
			}

			// 获取当前选中的分类
			RecipeCategory selectedCategory = _recipeTabs.CurrentTab switch
			{
				0 => RecipeCategory.Pill,
				1 => RecipeCategory.Talisman,
				2 => RecipeCategory.Formation,
				3 => RecipeCategory.Puppet,
				_ => RecipeCategory.Pill
			};

			// 添加配方按钮
			foreach (var recipe in _recipes.Values)
			{
				if (recipe.Category == selectedCategory)
				{
					var button = new Button
					{
						Text = $"{recipe.Name} (成功率: {recipe.SuccessRate:P0})",
						SizeFlagsHorizontal = SizeFlags.ExpandFill,
						TooltipText = recipe.Description
					};
					button.Pressed += () => OnRecipeSelected(recipe);
					_recipeListContainer.AddChild(button);
				}
			}
		}

		/// <summary>
		/// 配方被选中
		/// </summary>
		private void OnRecipeSelected(RecipeData recipe)
		{
			_selectedRecipe = recipe;
			_recipeDetailPanel.Show();

			_recipeNameLabel.Text = $"[b]{recipe.Name}[/b]";
			_recipeDescriptionLabel.Text = recipe.Description;

			// 清空并重新填充材料列表
			foreach (Node child in _ingredientsContainer.GetChildren())
			{
				child.QueueFree();
			}

			bool hasAllMaterials = true;
			foreach (var ingredient in recipe.Ingredients)
			{
				string itemId = ingredient.Key;
				int requiredAmount = ingredient.Value;
				int currentAmount = _inventory?.GetItemCount(itemId) ?? 0;
				bool hasEnough = currentAmount >= requiredAmount;

				if (!hasEnough) hasAllMaterials = false;

				var label = new Label
				{
					Text = $"• {GetItemName(itemId)}: {currentAmount}/{requiredAmount}",
					Modulate = hasEnough ? new Color(1, 1, 1) : new Color(1, 0.5f, 0.5f)
				};
				_ingredientsContainer.AddChild(label);
			}

			_craftButton.Disabled = !hasAllMaterials;
		}

		/// <summary>
		/// 获取物品名称
		/// </summary>
		private string GetItemName(string itemId)
		{
			// 优先从DataManager查询
			var itemData = DataManager.Instance?.GetItem(itemId);
			if (itemData != null)
			{
				return itemData.Name;
			}

			// 后备映射表
			return itemId switch
			{
				"spirit_grass" => "灵草",
				"spirit_water" => "灵泉水",
				"talisman_paper" => "符纸",
				"cinnabar" => "朱砂",
				"moonflower" => "月见花",
				"beast_core" => "妖兽内丹",
				_ => itemId
			};
		}

		/// <summary>
		/// 制作按钮被按下
		/// </summary>
		private void OnCraftButtonPressed()
		{
			if (_selectedRecipe == null) return;

			// 检查材料
			if (_inventory == null)
			{
				GD.PrintErr("[CraftingSystem] InventoryManager 未初始化");
				return;
			}

			if (!_inventory.HasItems(_selectedRecipe.Ingredients))
			{
				GD.PrintErr("[CraftingSystem] 材料不足");
				return;
			}

			// 扣除材料
			_inventory.TryRemoveItems(_selectedRecipe.Ingredients);

			// 判定成功率
			float roll = GD.Randf();
			bool success = roll <= _selectedRecipe.SuccessRate;

			if (success)
			{
				// 制作成功
				GD.Print($"[CraftingSystem] 成功制作 {_selectedRecipe.Name}");
				ShowCraftResult(true, $"成功制作了 {_selectedRecipe.Name}！");

				// 添加到背包
				_inventory.TryAddItem(_selectedRecipe.Id, 1);
			}
			else
			{
				// 制作失败
				GD.Print($"[CraftingSystem] 制作 {_selectedRecipe.Name} 失败");
				ShowCraftResult(false, $"制作 {_selectedRecipe.Name} 失败了...材料已损失。");
			}

			// 刷新配方详情
			OnRecipeSelected(_selectedRecipe);
		}

		/// <summary>
		/// 显示制作结果
		/// </summary>
		private void ShowCraftResult(bool success, string message)
		{
			var dialog = new AcceptDialog
			{
				Title = success ? "制作成功" : "制作失败",
				DialogText = message
			};
			AddChild(dialog);
			dialog.PopupCentered();
		}

		/// <summary>
		/// 关闭按钮被按下
		/// </summary>
		private void OnCloseButtonPressed()
		{
			GameManager.Instance.SetGameState(GameManager.GameState.Playing);
		}
	}

	/// <summary>
	/// 配方数据类
	/// </summary>
	public class RecipeData
	{
		public string Id { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public RecipeCategory Category { get; set; }
		public int RequiredRealm { get; set; }
		public Dictionary<string, int> Ingredients { get; set; }
		public int BasePrice { get; set; }
		public float SuccessRate { get; set; }
	}

	public enum RecipeCategory
	{
		Pill,       // 丹药
		Talisman,   // 符箓
		Formation,  // 阵法
		Puppet,     // 傀儡
		Weapon,     // 武器
		Armor       // 防具
	}
}
