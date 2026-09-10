using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class HUD : Control
{
	private GameState _gameState;
	private SpinManager _spinManager;
	private BettingTable _bettingTable;
	private ShopManager _shopManager;
	private Label _statsLabel;
	private Label _bossLabel;

	private Control _resultPopup;
	private Label _titleLabel;
	private Button _continueRestartButton;

	private Control _shopPanel;
	private Button _nextRoundButton;

	private bool _isRoundOver = false;

	// Admin Debug Overlay
	private PanelContainer _debugPanel;
	private LineEdit _debugNumberInput;

	public override void _Ready()
	{
		_gameState = GetNodeOrNull<GameState>("/root/GameState");
		_spinManager = GetNodeOrNull<SpinManager>("../SpinManager");
		_bettingTable = GetNodeOrNull<BettingTable>("../BettingTable");
		_shopManager = GetNodeOrNull<ShopManager>("../OverlayLayer/ShopPanel");

		var optionsButton = GetNodeOrNull<Button>("BottomLeftPanel/OptionsButton");
		if (optionsButton != null) optionsButton.Pressed += () => GD.Print("[HUD] Options button pressed");

		var clearBetsButton = GetParent().GetNodeOrNull<Button>("ClearBetsButton");
		if (clearBetsButton != null)
		{
			clearBetsButton.Pressed += () =>
			{
				if (_isRoundOver) return;
				_bettingTable?.ClearAllBets();
				RefreshLabels();
			};
		}

		_resultPopup = GetParent().GetNodeOrNull<Control>("OverlayLayer/ResultPopup");
		_titleLabel = GetParent().GetNodeOrNull<Label>("OverlayLayer/ResultPopup/ResultPopupTitle");
		_continueRestartButton = GetParent().GetNodeOrNull<Button>("OverlayLayer/ResultPopup/ContinueRestartButton");
		_statsLabel = GetParent().GetNodeOrNull<Label>("OverlayLayer/ResultPopup/StatsLabel");
		_bossLabel = GetNodeOrNull<Label>("BottomLeftPanel/BossLabel");
		
		if (_bossLabel != null)
		{
			_bossLabel.MouseFilter = Control.MouseFilterEnum.Stop;
		}

		if (_resultPopup != null)
		{
			_resultPopup.Visible = false;
			_resultPopup.MouseFilter = Control.MouseFilterEnum.Stop;
		}

		_shopPanel = GetParent().GetNodeOrNull<Control>("OverlayLayer/ShopPanel");
		_nextRoundButton = GetParent().GetNodeOrNull<Button>("OverlayLayer/ShopPanel/NextRoundButton");

		if (_shopPanel != null)
		{
			_shopPanel.Visible = false;
			_shopPanel.MouseFilter = Control.MouseFilterEnum.Stop;
		}

		var spinButton = GetParent().GetNodeOrNull<Button>("SpinButton");
		if (spinButton != null) spinButton.Pressed += OnSpinPressed;

		var repeatButton = GetParent().GetNodeOrNull<Button>("RepeatBetButton");
		if (repeatButton != null)
		{
			repeatButton.Pressed += () =>
			{
				if (_isRoundOver) return;
				_bettingTable?.RepeatLastBets();
				RefreshLabels();
			};
		}

		if (_continueRestartButton != null) _continueRestartButton.Pressed += OnPopupActionPressed;
		if (_nextRoundButton != null) _nextRoundButton.Pressed += OnShopFinished;

		var eventBus = GetNodeOrNull<EventBus>("/root/EventBus");
		if (eventBus != null)
		{
			eventBus.Connect(EventBus.SignalName.BetPlaced, new Callable(this, nameof(OnBetPlacedSignal)));
			eventBus.Connect(EventBus.SignalName.SpinResolved, new Callable(this, nameof(OnSpinResolved)));
			eventBus.Connect(EventBus.SignalName.RoundWon, new Callable(this, nameof(OnRoundWon)));
			eventBus.Connect(EventBus.SignalName.RoundLost, new Callable(this, nameof(OnRoundLost)));
			eventBus.Connect(EventBus.SignalName.ShopUpdated, new Callable(this, nameof(RefreshLabels)));
			eventBus.Connect(EventBus.SignalName.RoundStarted, new Callable(this, nameof(RefreshLabels)));
		}

		BuildAdminDebugPanel();
		RefreshLabels();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent && keyEvent.Pressed)
		{
			if (keyEvent.Keycode == Key.F12 || keyEvent.Keycode == Key.Quoteleft)
			{
				if (_debugPanel != null) _debugPanel.Visible = !_debugPanel.Visible;
			}
		}
	}

	private void BuildAdminDebugPanel()
	{
		_debugPanel = new PanelContainer();
		_debugPanel.Name = "AdminDebugPanel";
		_debugPanel.Visible = false;
		_debugPanel.CustomMinimumSize = new Vector2(280, 260);
		_debugPanel.SetPosition(new Vector2(15, 250));
		_debugPanel.ZIndex = 999;

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 6);

		vbox.AddChild(new Label { Text = "🛠 ADMIN DEBUG PANEL (F12)" });

		var addCashBtn = new Button { Text = "+$100 Cash" };
		addCashBtn.Pressed += () => { _gameState.Cash += 100; RefreshLabels(); };
		vbox.AddChild(addCashBtn);

		var addChipsBtn = new Button { Text = "+200 Chips This Spin" };
		addChipsBtn.Pressed += () => { _gameState.ChipsRemainingThisSpin += 200; RefreshLabels(); };
		vbox.AddChild(addChipsBtn);

		var winRoundBtn = new Button { Text = "Force Clear Round Goal" };
		winRoundBtn.Pressed += () => { _gameState.Score = _gameState.ScoreGoal; OnRoundWon(); };
		vbox.AddChild(winRoundBtn);

		var clearBossBtn = new Button { Text = "Remove Active Boss" };
		clearBossBtn.Pressed += () => { _gameState.ActiveBoss = null; RefreshLabels(); };
		vbox.AddChild(clearBossBtn);

		var rigBox = new HBoxContainer();
		rigBox.AddChild(new Label { Text = "Rigged Num (0-36):" });
		_debugNumberInput = new LineEdit { Text = "7", CustomMinimumSize = new Vector2(50, 30) };
		rigBox.AddChild(_debugNumberInput);
		vbox.AddChild(rigBox);

		var setRigBtn = new Button { Text = "Force Next Spin Outcome" };
		setRigBtn.Pressed += () =>
		{
			if (int.TryParse(_debugNumberInput.Text, out int targetNum) && targetNum >= 0 && targetNum <= 36)
			{
				_gameState.DebugForcedWinningNumber = targetNum;
				GD.Print($"[Debug] Next spin forced to land on: {targetNum}");
			}
		};
		vbox.AddChild(setRigBtn);

		_debugPanel.AddChild(vbox);
		AddChild(_debugPanel);
	}

	private string BuildStatsSummary()
	{
		var stats = _gameState.Stats;
		return $"Rounds Survived: {stats.RoundsSurvived}\n" +
			$"Total Cash Earned: ${stats.TotalCashEarned}\n" +
			$"Highest Scoring Spin: {stats.HighestScoringSpin}\n" +
			$"Total Bets Placed: {stats.TotalBetsPlaced}";
	}

	private void OnBetPlacedSignal()
	{
		if (_isRoundOver) return;
		RefreshLabels();
	}

	private void OnSpinPressed()
	{
		if (_isRoundOver) return;
		if (_gameState == null || _spinManager == null || _bettingTable == null) return;

		if (_gameState.ActiveBets.Count == 0)
		{
			GD.Print("[HUD] WARNING: Cannot spin - no active bets placed!");
			return;
		}

		_bettingTable.CacheBetsForRepeat();
		_spinManager.ResolveSpin();
		RefreshLabels();
	}

	private void OnSpinResolved(int winningNumber, int scoreGained)
	{
		var lastSpinLabel = GetNodeOrNull<Label>("BottomLeftPanel/LastSpinLabel");
		if (lastSpinLabel != null)
		{
			string colorDesc = winningNumber == 0 ? "Green" : (WheelData.IsRed(winningNumber) ? "Red" : "Black");
			lastSpinLabel.Text = $"Last Spin: {winningNumber} ({colorDesc}) | +{scoreGained}";
		}
		RefreshLabels();
	}

	private void OnRoundWon()
	{
		_isRoundOver = true;
		if (_resultPopup == null) return;

		_resultPopup.Visible = true;
		_resultPopup.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

		if (_gameState.IsFinalBossRound)
		{
			int totalEarned = _gameState.GrantRoundCashReward();
			if (_titleLabel != null) _titleLabel.Text = "YOU BEAT THE HOUSE!";
			if (_statsLabel != null) _statsLabel.Text = BuildStatsSummary();
			if (_continueRestartButton != null) _continueRestartButton.Text = "Start New Run";
		}
		else
		{
			bool wasBossRound = _gameState.RoundInStake == GameState.RoundsPerStake;
			if (_titleLabel != null) _titleLabel.Text = wasBossRound ? $"BOSS DEFEATED! (Stake {_gameState.Stake})" : $"ROUND {_gameState.RoundNumber} CLEARED!";
			if (_statsLabel != null) _statsLabel.Text = "";
			if (_continueRestartButton != null) _continueRestartButton.Text = "Open Shop";
		}
	}

	private void OnRoundLost()
	{
		_isRoundOver = true;
		if (_resultPopup == null) return;

		_resultPopup.Visible = true;
		_resultPopup.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

		if (_titleLabel != null) _titleLabel.Text = "GAME OVER";
		if (_statsLabel != null) _statsLabel.Text = BuildStatsSummary();
		if (_continueRestartButton != null) _continueRestartButton.Text = "Restart Run";
	}

	private void OnPopupActionPressed()
	{
		if (_gameState == null) return;
		bool won = _gameState.Score >= _gameState.ScoreGoal;

		if (won && _gameState.IsFinalBossRound)
		{
			_gameState.ResetRun();
			if (_resultPopup != null) _resultPopup.Visible = false;
			_isRoundOver = false;
			RefreshLabels();
		}
		else if (won)
		{
			int totalEarned = _gameState.GrantRoundCashReward();
			if (_resultPopup != null) _resultPopup.Visible = false;
			OpenShop();
		}
		else
		{
			_gameState.ResetRun();
			if (_resultPopup != null) _resultPopup.Visible = false;
			_isRoundOver = false;
			RefreshLabels();
		}
	}

	private void OpenShop()
	{
		if (_shopPanel != null)
		{
			_shopPanel.Visible = true;
			_shopPanel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		}
		_shopManager?.OpenShop();
		RefreshLabels();
	}

	private void OnShopFinished()
	{
		if (_shopPanel != null) _shopPanel.Visible = false;
		_gameState.AdvanceToNextRound();
		_isRoundOver = false;
		RefreshLabels();
	}
	
	private void RefreshCharmsRow()
	{
		var row = GetNodeOrNull<HBoxContainer>("CharmsRow");
		if (row == null) return;

		foreach (Node child in row.GetChildren()) child.QueueFree();

		if (_gameState == null || _gameState.OwnedCharms == null) return;

		foreach (var charm in _gameState.OwnedCharms)
		{
			var btn = new Button();
			btn.Text = charm.Name;
			btn.TooltipText = $"{charm.Name}\n{charm.Description}";
			btn.CustomMinimumSize = new Vector2(100, 56);
			btn.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
			btn.AutowrapMode = TextServer.AutowrapMode.WordSmart;
			btn.MouseFilter = Control.MouseFilterEnum.Stop;

			var localCharm = charm;
			btn.Pressed += () =>
			{
				int sellPrice = Mathf.Max(10, localCharm.BaseCost / 2);
				_gameState.Cash += sellPrice;
				_gameState.OwnedCharms.Remove(localCharm);
				GD.Print($"[HUD] Sold charm {localCharm.Name} for ${sellPrice}");
				GetNode<EventBus>("/root/EventBus")?.EmitSignal(EventBus.SignalName.ShopUpdated);
				RefreshLabels();
			};

			row.AddChild(btn);
		}
	}

	private void RefreshConsumablesRow()
	{
		var row = GetNodeOrNull<HBoxContainer>("ConsumablesRow");
		if (row == null) return;

		foreach (Node child in row.GetChildren()) child.QueueFree();

		if (_gameState == null || _gameState.HeldConsumables == null) return;

		foreach (var consumable in _gameState.HeldConsumables.ToArray())
		{
			var btn = new Button();
			btn.Text = $"{consumable.Name}\n[Use]";
			btn.MouseFilter = Control.MouseFilterEnum.Stop;
			btn.TooltipText = consumable.Description;
			btn.CustomMinimumSize = new Vector2(90, 56);
			btn.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
			btn.AutowrapMode = TextServer.AutowrapMode.WordSmart;

			btn.Pressed += () =>
			{
				if (_isRoundOver) return;
				
				consumable.Use?.Invoke(_gameState);
				_gameState.HeldConsumables.Remove(consumable);
				
				GD.Print($"[HUD] Successfully used consumable: {consumable.Name}");
				RefreshLabels();
				GetNode<EventBus>("/root/EventBus")?.EmitSignal(EventBus.SignalName.BetPlaced);
			};

			row.AddChild(btn);
		}
	}

	private void RefreshLabels()
	{
		if (_gameState == null) return;

		var chipsLabel = GetNodeOrNull<Label>("ChipsLabel");
		var spinsLabel = GetNodeOrNull<Label>("BottomLeftPanel/SpinsLabel");
		var scoreLabel = GetNodeOrNull<Label>("ScoreLabel");
		var roundLabel = GetNodeOrNull<Label>("BottomLeftPanel/RoundLabel");
		var cashLabel = GetNodeOrNull<Label>("CashLabel");
		var shopCashLabel = GetParent().GetNodeOrNull<Label>("OverlayLayer/ShopPanel/ShopCashLabel");
		var peekLabel = GetNodeOrNull<Label>("BottomLeftPanel/PeekedNumberLabel");

		if (chipsLabel != null) chipsLabel.Text = $"Chips: {_gameState.ChipsRemainingThisSpin}";
		if (spinsLabel != null) spinsLabel.Text = $"Spins Remaining: {_gameState.SpinsRemaining}";
		if (scoreLabel != null) scoreLabel.Text = $"Score: {_gameState.Score} / {_gameState.ScoreGoal}";
		if (cashLabel != null) cashLabel.Text = $"Money: ${_gameState.Cash}";
		if (shopCashLabel != null) shopCashLabel.Text = $"Available Cash: ${_gameState.Cash}";
		
		if (peekLabel != null)
		{
			peekLabel.Visible = _gameState.PeekedNextNumber.HasValue;
			if (_gameState.PeekedNextNumber.HasValue)
				peekLabel.Text = $"🔒 Peek: Next is {_gameState.PeekedNextNumber.Value}";
		}

		if (roundLabel != null)
		{
			string roundName = _gameState.RoundInStake == GameState.RoundsPerStake ? "BOSS ROUND" : $"Round {_gameState.RoundInStake}";
			roundLabel.Text = $"Round: {_gameState.RoundInStake} ({roundName})";
		}

		if (_bossLabel != null)
		{
			if (_gameState.ActiveBoss != null)
			{
				_bossLabel.Visible = true;
				_bossLabel.Text = $"⚠ Boss: {_gameState.ActiveBoss.Name}";
				_bossLabel.TooltipText = $"{_gameState.ActiveBoss.Name}\n{_gameState.ActiveBoss.Description}";
				_bossLabel.MouseFilter = Control.MouseFilterEnum.Stop;
			}
			else
			{
				_bossLabel.Visible = false;
			}
		}

		RefreshCharmsRow();
		RefreshConsumablesRow();
	}
}
