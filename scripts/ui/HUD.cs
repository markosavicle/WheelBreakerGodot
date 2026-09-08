using Godot;

public partial class HUD : Control
{
	private GameState _gameState;
	private SpinManager _spinManager;
	private BettingTable _bettingTable;
	private ShopManager _shopManager;
	private InventoryPanel _inventoryPanel;
	private Label _statsLabel;

	private Control _resultPopup;
	private Label _titleLabel;
	private Button _continueRestartButton;

	// Shop UI references
	private Control _shopPanel;
	private Button _nextRoundButton;

	private bool _isRoundOver = false;

	public override void _Ready()
	{
		_gameState = GetNodeOrNull<GameState>("/root/GameState");
		if (_gameState == null)
		{
			GD.PrintErr("[HUD] ERROR: GameState autoload not found at /root/GameState!");
		}

		_spinManager = GetNodeOrNull<SpinManager>("../SpinManager");
		if (_spinManager == null)
		{
			GD.PrintErr("[HUD] ERROR: SpinManager node not found at ../SpinManager!");
		}

		_bettingTable = GetNodeOrNull<BettingTable>("../BettingTable");
		if (_bettingTable == null)
		{
			GD.PrintErr("[HUD] ERROR: BettingTable node not found at ../BettingTable!");
		}
		
		_shopManager = GetNodeOrNull<ShopManager>("OverlayLayer/ShopPanel");
		if (_shopManager == null)
		{
			GD.PrintErr("[HUD] ERROR: _shopManager node not found at OverlayLayer/ShopPanel");
		}
		
		_inventoryPanel = GetNodeOrNull<InventoryPanel>("OverlayLayer/InventoryPanel");

		var inventoryButton = GetNodeOrNull<Button>("InventoryButton");
		if (inventoryButton != null)
		inventoryButton.Pressed += () => _inventoryPanel?.ToggleVisibility();

		// Result Popup nodes
		_resultPopup = GetNodeOrNull<Control>("OverlayLayer/ResultPopup");
		_titleLabel = GetNodeOrNull<Label>("OverlayLayer/ResultPopup/TitleLabel");
		_continueRestartButton = GetNodeOrNull<Button>("OverlayLayer/ResultPopup/ContinueRestartButton");
		_statsLabel = GetNodeOrNull<Label>("OverlayLayer/ResultPopup/StatsLabel");

		if (_resultPopup != null) 
		{
			_resultPopup.Visible = false;
			_resultPopup.MouseFilter = Control.MouseFilterEnum.Stop;
		}

		// Shop Panel nodes
		_shopPanel = GetNodeOrNull<Control>("OverlayLayer/ShopPanel");
		_nextRoundButton = GetNodeOrNull<Button>("OverlayLayer/ShopPanel/NextRoundButton");

		if (_shopPanel != null)
		{
			_shopPanel.Visible = false;
			_shopPanel.MouseFilter = Control.MouseFilterEnum.Stop;
		}

		GD.Print("[HUD] Initializing HUD and connecting signals...");

		var spinButton = GetNodeOrNull<Button>("SpinButton");
		if (spinButton != null)
		{
			spinButton.Pressed += OnSpinPressed;
		}

		var repeatButton = GetNodeOrNull<Button>("../RepeatBetButton");
		if (repeatButton != null)
		{
			repeatButton.Pressed += () => 
			{
				if (_isRoundOver) return;
				if (_bettingTable != null)
				{
					GD.Print("[HUD] Repeat Last Bet button pressed.");
					_bettingTable.RepeatLastBets();
					RefreshLabels();
				}
			};
		}

		if (_continueRestartButton != null)
		{
			_continueRestartButton.Pressed += OnPopupActionPressed;
		}

		if (_nextRoundButton != null)
		{
			_nextRoundButton.Pressed += OnShopFinished;
		}

		var eventBus = GetNodeOrNull<EventBus>("/root/EventBus");
		if (eventBus != null)
		{
			eventBus.Connect(EventBus.SignalName.BetPlaced, new Callable(this, nameof(OnBetPlacedSignal)));
			eventBus.Connect(EventBus.SignalName.SpinResolved, new Callable(this, nameof(OnSpinResolved)));
			eventBus.Connect(EventBus.SignalName.RoundWon, new Callable(this, nameof(OnRoundWon)));
			eventBus.Connect(EventBus.SignalName.RoundLost, new Callable(this, nameof(OnRoundLost)));
			eventBus.Connect(EventBus.SignalName.ShopUpdated, new Callable(this, nameof(RefreshLabels)));
		}

		RefreshLabels();
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

		GD.Print("[HUD] Spin button pressed.");
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
		GD.Print($"[HUD] Spin Resolved Event -> Ball landed on {winningNumber}. Score gained: {scoreGained}");
		
		var lastSpinLabel = GetNodeOrNull<Label>("LastSpinLabel");
		if (lastSpinLabel != null)
		{
			string colorDesc = winningNumber == 0 ? "Green" : (WheelData.IsRed(winningNumber) ? "Red" : "Black");
			lastSpinLabel.Text = $"Last Spin: {winningNumber} ({colorDesc}) | Gained: +{scoreGained}";
		}

		RefreshLabels();
	}

	private void OnRoundWon()
	{
		GD.Print("ROUND WON — displaying victory popup.");
		_isRoundOver = true;

		if (_resultPopup != null)
		{
			_resultPopup.Visible = true;
			_resultPopup.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
			
			if (_titleLabel != null)
			{
				_titleLabel.Text = $"ROUND {_gameState?.RoundNumber ?? 1} WON!";
			}

			if (_continueRestartButton != null)
			{
				_continueRestartButton.Text = "Open Shop";
			}
		}
	}

	private void OnRoundLost()
	{
		GD.Print("ROUND LOST — displaying game over popup.");
		_isRoundOver = true;

		if (_resultPopup != null)
		{
			_resultPopup.Visible = true;
			_resultPopup.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

			if (_titleLabel != null)
			{
				_titleLabel.Text = "GAME OVER";
			}

			if (_statsLabel != null)
			{
				_statsLabel.Text = BuildStatsSummary();   // ← add this block
			}

			if (_continueRestartButton != null)
			{
				_continueRestartButton.Text = "Restart Run";
			}
		}
	}

	private void OnPopupActionPressed()
	{
		if (_gameState == null) return;

		if (_gameState.Score >= _gameState.ScoreGoal)
		{
			int totalEarned = _gameState.GrantRoundCashReward();
			GD.Print($"[GameState] Round {_gameState.RoundNumber} Cleared! Earned {totalEarned} Cash. Total Cash: {_gameState.Cash}");

			if (_resultPopup != null) _resultPopup.Visible = false;
			OpenShop();
		}
		else
		{
			// LOSS -> Reset run
			_gameState.ResetRun();
			GD.Print("[HUD] Run restarted by user.");
			if (_resultPopup != null) _resultPopup.Visible = false;
			_isRoundOver = false;
			RefreshLabels();
		}
	}

	private void OpenShop()
	{
		GD.Print("[Shop] Opening Shop Phase...");
		if (_shopPanel != null)
		{
			_shopPanel.Visible = true;
			_shopPanel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		}
		_shopManager?.OpenShop();   // ← add this line
		RefreshLabels();
	}

	private void OnShopFinished()
	{
		GD.Print("[Shop] Exiting shop, starting next round...");
		if (_shopPanel != null)
		{
			_shopPanel.Visible = false;
		}

		_gameState.AdvanceToNextRound();
		_isRoundOver = false;
		RefreshLabels();
	}

	private void RefreshLabels()
	{
		if (_gameState == null) return;

		var chipsLabel = GetNodeOrNull<Label>("ChipsLabel");
		var spinsLabel = GetNodeOrNull<Label>("SpinsLabel");
		var scoreLabel = GetNodeOrNull<Label>("ScoreLabel");
		var roundLabel = GetNodeOrNull<Label>("RoundLabel");
		var cashLabel = GetNodeOrNull<Label>("CashLabel");
		
		// Shop cash label reference
		var shopCashLabel = GetNodeOrNull<Label>("OverlayLayer/ShopPanel/ShopCashLabel");

		if (chipsLabel != null) chipsLabel.Text = $"Chips: {_gameState.ChipsRemainingThisSpin}";
		if (spinsLabel != null) spinsLabel.Text = $"Spins: {_gameState.SpinsRemaining}";
		if (scoreLabel != null) scoreLabel.Text = $"Score: {_gameState.Score} / {_gameState.ScoreGoal}";
		if (roundLabel != null) roundLabel.Text = $"Round: {_gameState.RoundNumber}";
		if (cashLabel != null) cashLabel.Text = $"Cash: ${_gameState.Cash}";
		
		// Update shop cash display too
		if (shopCashLabel != null) shopCashLabel.Text = $"Available Cash: ${_gameState.Cash}";
		
		var charmSlotsLabel = GetNodeOrNull<Label>("CharmSlotsLabel");
		if (charmSlotsLabel != null) charmSlotsLabel.Text = $"Charms: {_gameState.OwnedCharms.Count}/{_gameState.MaxCharmSlots}";
		
		GD.Print($"[HUD] Labels refreshed -> Round: {_gameState.RoundNumber} | Cash: ${_gameState.Cash} | Chips: {_gameState.ChipsRemainingThisSpin} | Spins: {_gameState.SpinsRemaining} | Score: {_gameState.Score}/{_gameState.ScoreGoal}");
	}
}
