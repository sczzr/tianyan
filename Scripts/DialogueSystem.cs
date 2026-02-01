using Godot;
using System;
using System.Threading.Tasks;

namespace TianYanShop.Scripts
{
	/// <summary>
	/// 对话系统 - 处理与NPC的对话交互
	/// </summary>
	public partial class DialogueSystem : Control
	{
		// 节点引用
		private Label _npcNameLabel;
		private Label _npcInfoLabel;
		private LineEdit _playerInput;
		private Button _sendButton;
		private Button _tradeButton;
		private Button _closeButton;
		private VBoxContainer _historyContainer;

		private NPCData _currentNPC;
		private bool _isWaitingResponse = false;

		public override void _Ready()
		{
			// 获取节点引用
			_npcNameLabel = GetNode<Label>("DialoguePanel/NPCInfoPanel/NPCNameLabel");
			_npcInfoLabel = GetNode<Label>("DialoguePanel/NPCInfoPanel/NPCInfoLabel");
			_playerInput = GetNode<LineEdit>("DialoguePanel/InputPanel/HBoxContainer/PlayerInput");
			_sendButton = GetNode<Button>("DialoguePanel/InputPanel/HBoxContainer/SendButton");
			_tradeButton = GetNode<Button>("DialoguePanel/InputPanel/HBoxContainer/TradeButton");
			_closeButton = GetNode<Button>("DialoguePanel/InputPanel/HBoxContainer/CloseButton");
			_historyContainer = GetNode<VBoxContainer>("DialoguePanel/ScrollContainer/HistoryContainer");

			// 连接按钮信号
			_sendButton.Pressed += OnSendButtonPressed;
			_tradeButton.Pressed += OnTradeButtonPressed;
			_closeButton.Pressed += OnCloseButtonPressed;
			_playerInput.TextSubmitted += OnInputSubmitted;

			// 初始隐藏
			Hide();

			// 延迟连接信号，等待 GameManager 初始化完成
			CallDeferred(nameof(DeferredConnectSignals));

			// 延迟检查当前游戏状态，确保UI可见性正确
			CallDeferred(nameof(DeferredCheckVisibility));
		}

		/// <summary>
		/// 延迟连接信号 - 确保 GameManager 已初始化完成
		/// </summary>
		private void DeferredConnectSignals()
		{
			// 监听游戏状态变化
			if (GameManager.Instance != null)
			{
				GameManager.Instance.GameStateChanged += OnGameStateChanged;
				GD.Print("[DialogueSystem] 成功连接到 GameManager 信号");
			}
			else
			{
				GD.PrintErr("[DialogueSystem] GameManager.Instance 为 null，无法连接信号");
			}
		}

		public override void _ExitTree()
		{
			if (GameManager.Instance != null)
			{
				GameManager.Instance.GameStateChanged -= OnGameStateChanged;
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
				if (GameManager.Instance.CurrentState == GameManager.GameState.Dialogue)
				{
					_currentNPC = GameManager.Instance.CurrentNPC;
					if (_currentNPC != null)
					{
						ShowDialogue();
						GD.Print("[DialogueSystem] 初始化时检测到当前状态为 Dialogue，显示UI");
					}
				}
				else
				{
					Hide();
				}
			}
			else
			{
				GD.PrintErr("[DialogueSystem] GameManager.Instance 为 null，无法检查可见性");
			}
		}

		private void OnGameStateChanged(GameManager.GameState newState)
		{
			if (newState == GameManager.GameState.Dialogue)
			{
				_currentNPC = GameManager.Instance.CurrentNPC;
				if (_currentNPC != null)
				{
					ShowDialogue();
				}
			}
			else
			{
				Hide();
			}
		}

		/// <summary>
		/// 显示对话界面
		/// </summary>
		private void ShowDialogue()
		{
			Show();
			_npcNameLabel.Text = _currentNPC.Name;
			_npcInfoLabel.Text = $"{_currentNPC.GetCultivationDescription()} | {BackgroundToDescription(_currentNPC.Background)} | 关系: {_currentNPC.GetRelationshipLevel()}";

			// 清空历史显示
			foreach (Node child in _historyContainer.GetChildren())
			{
				child.QueueFree();
			}

			// 添加欢迎消息
			AddNPCMessage($"你好，我是{_currentNPC.Name}。");
			if (!string.IsNullOrEmpty(_currentNPC.CurrentGoal))
			{
				AddNPCMessage($"目前我正在寻求{_currentNPC.CurrentGoal}。");
			}

			_playerInput.GrabFocus();
		}

		/// <summary>
		/// 背景转描述
		/// </summary>
		private string BackgroundToDescription(string background)
		{
			return background switch
			{
				"家族弃子" => "家族弃子",
				"散修" => "散修",
				"小家族弟子" => "小家族弟子",
				"流浪修士" => "流浪修士",
				"宗门弃徒" => "宗门弃徒",
				"商人之子" => "商人之子",
				_ => background
			};
		}

		/// <summary>
		/// 玩家发送消息
		/// </summary>
		private async void OnSendButtonPressed()
		{
			string message = _playerInput.Text.Trim();
			if (string.IsNullOrEmpty(message) || _isWaitingResponse)
				return;

			// 添加玩家消息
			AddPlayerMessage(message);
			_currentNPC.AddToHistory("玩家", message);

			// 清空输入框
			_playerInput.Text = "";
			_isWaitingResponse = true;
			_sendButton.Disabled = true;

			// 模拟NPC响应（将来接入LLM）
			await SimulateNPCResponse(message);

			_isWaitingResponse = false;
			_sendButton.Disabled = false;
			_playerInput.GrabFocus();
		}

		private void OnInputSubmitted(string text)
		{
			OnSendButtonPressed();
		}

		/// <summary>
		/// 模拟NPC响应（临时方案，将来接入LLM）
		/// </summary>
		private async Task SimulateNPCResponse(string playerMessage)
		{
			// 模拟延迟
			await ToSignal(GetTree().CreateTimer(0.5f), "timeout");

			string response;
			string lowerMessage = playerMessage.ToLower();

			// 简单的关键词响应逻辑
			if (lowerMessage.Contains("筑基丹") || lowerMessage.Contains("突破"))
			{
				if (_currentNPC.StoryStage == 2)
				{
					response = "没错！我正是急需筑基丹来突破瓶颈。不知老板这里可有？若有的话，在下愿出高价购买！";
					_currentNPC.RelationshipWithPlayer += 5;
				}
				else
				{
					response = "筑基丹确实是珍贵之物。虽然我已不需要，但仍有许多道友在寻求。";
				}
			}
			else if (lowerMessage.Contains("价格") || lowerMessage.Contains("多少") || lowerMessage.Contains("钱"))
			{
				response = "老板的价格公道与否，在下还需比较一番。不过若质量上乘，价格稍高也无妨。";
			}
			else if (lowerMessage.Contains("材料") || lowerMessage.Contains("收购"))
			{
				response = "在下手中确实有一些材料。不知老板可收购灵草或矿石？价格合适的话，我愿意出售。";
			}
			else if (lowerMessage.Contains("你好") || lowerMessage.Contains("你好"))
			{
				response = $"老板客气了。在下{_currentNPC.Name}，初来天衍峰集市，还请多多关照。";
			}
			else
			{
				// 通用响应
				string[] genericResponses = {
					"有意思。老板此言，倒让我想起了一些往事。",
					"修仙之路漫漫，每个人都有自己的机缘。",
					"天衍峰的集市果然藏龙卧虎，老板也不是寻常之人。",
                    "我自有我的难处，不提也罢。老板若有何物售卖，不妨直说。"
				};
				response = genericResponses[GD.RandRange(0, genericResponses.Length - 1)];
			}

			AddNPCMessage(response);
			_currentNPC.AddToHistory(_currentNPC.Name, response);
		}

		/// <summary>
		/// 添加玩家消息气泡
		/// </summary>
		private void AddPlayerMessage(string message)
		{
			var label = new RichTextLabel
			{
				BbcodeEnabled = true,
				Text = $"[right][color=#88CCFF]你：{message}[/color][/right]",
				FitContent = true,
				AutowrapMode = TextServer.AutowrapMode.WordSmart,
				CustomMinimumSize = new Vector2(0, 30)
			};
			_historyContainer.AddChild(label);

			// 滚动到底部
			ScrollToBottom();
		}

		/// <summary>
		/// 添加NPC消息气泡
		/// </summary>
		private void AddNPCMessage(string message)
		{
			var label = new RichTextLabel
			{
				BbcodeEnabled = true,
				Text = $"[color=#FFCC88]{_currentNPC.Name}：{message}[/color]",
				FitContent = true,
				AutowrapMode = TextServer.AutowrapMode.WordSmart,
				CustomMinimumSize = new Vector2(0, 30)
			};
			_historyContainer.AddChild(label);

			ScrollToBottom();
		}

		/// <summary>
		/// 滚动到底部
		/// </summary>
		private void ScrollToBottom()
		{
			// 使用延迟确保布局更新后再滚动
			CallDeferred(nameof(DeferredScrollToBottom));
		}

		private void DeferredScrollToBottom()
		{
			var scrollContainer = GetNode<ScrollContainer>("DialoguePanel/ScrollContainer");
			if (scrollContainer != null)
			{
				scrollContainer.ScrollVertical = (int)scrollContainer.GetVScrollBar().MaxValue;
			}
		}

		/// <summary>
		/// 交易按钮被按下
		/// </summary>
		private void OnTradeButtonPressed()
		{
			GD.Print("[DialogueSystem] 打开交易界面");
			// TODO: 显示交易界面
		}

		/// <summary>
		/// 关闭按钮被按下
		/// </summary>
		private void OnCloseButtonPressed()
		{
			GD.Print("[DialogueSystem] 关闭对话");
			GameManager.Instance.EndDialogue();
		}
	}
}
