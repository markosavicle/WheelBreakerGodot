using Godot;

public partial class HUD : Control
{
	private GameState _gameState;
	private SpinManager _spinManager;
	private BettingTable _bettingTable;

	private Control _resultPopup;
	private Label _titleLabel;
	private Button _continueRestartButton;

	private bool _isRoundOver = false;

	public override void _Ready()
	{
		_gameState = GetNode<GameState>("/root/GameState");
		_spinManager = GetNode<SpinManager>("../SpinManager"); 
		_bettingTable = GetNode<BettingTable>("../BettingTable");

		// Fetch popup nodes based on your scene tree
		_resultPopup = GetNodeOrNull<Control>("ResultPopup");
		_titleLabel = GetNodeOrNull<Label>("ResultPopup/TitleLabel");
		_continueRestartButton = GetNodeOrNull<Button>("ResultPopup/ContinueRestartButton");

		if (_resultPopup != null) 
		{
			_resultPopup.Visible = false;
			// Ensure the panel blocks mouse inputs from hitting buttons underneath
			_resultPopup.MouseFilter = Control.MouseFilterEnum.Stop;
		}

		GD.Print("[HUD] Initializing HUD and connecting signals...");

		GetNode<Button>("SpinButton").Pressed += OnSpinPressed;
		GetNode<Button>("../RepeatBetButton").Pressed += () => 
		{
			if (_isRoundOver) return;
			GD.Print("[HUD] Repeat Last Bet button pressed.");
			_bettingTable.RepeatLastBets();
			RefreshLabels();
		};

		if (_continueRestartButton != null)
		{
			_continueRestartButton.Pressed += OnPopupActionPressed;
		}

		var eventBus = GetNode<EventBus>("/root/EventBus");
		eventBus.Connect(EventBus.SignalName.BetPlaced, new Callable(this, nameof(OnBetPlacedSignal)));
		eventBus.Connect(EventBus.SignalName.SpinResolved, new Callable(this, nameof(OnSpinResolved)));
		eventBus.Connect(EventBus.SignalName.RoundWon, new Callable(this, nameof(OnRoundWon)));
		eventBus.Connect(EventBus.SignalName.RoundLost, new Callable(this, nameof(OnRoundLost)));

		RefreshLabels();
	}

	private void OnBetPlacedSignal()
	{
		if (_isRoundOver) return;
		RefreshLabels();
	}

	private void OnSpinPressed()
	{
		if (_isRoundOver) return;

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
		RefreshLabels();
	}

	private void OnRoundWon()
	{
		GD.Print("ROUND WON — displaying victory popup.");
		_isRoundOver = true;

		if (_resultPopup != null)
		{
			_resultPopup.Visible = true;
			
			if (_titleLabel != null)
			{
				_titleLabel.Text = $"ROUND {_gameState.RoundNumber} WON!";
			}

			if (_continueRestartButton != null)
			{
				_continueRestartButton.Text = "Proceed to Next Round";
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
			
			if (_titleLabel != null)
			{
				_titleLabel.Text = "GAME OVER";
			}

			if (_continueRestartButton != null)
			{
				_continueRestartButton.Text = "Restart Run";
			}
		}
	}

	private void OnPopupActionPressed()
	{
		if (_gameState.Score >= _gameState.ScoreGoal)
		{
			_gameState.AdvanceToNextRound();
			GD.Print($"[HUD] Advanced to Round {_gameState.RoundNumber}");
		}
		else
		{
			_gameState.ResetRun();
			GD.Print("[HUD] Run restarted by user.");
		}

		if (_resultPopup != null) _resultPopup.Visible = false;
		_isRoundOver = false;
		RefreshLabels();
	}

	private void RefreshLabels()
	{
		GetNode<Label>("ChipsLabel").Text = $"Chips: {_gameState.ChipsRemainingThisSpin}";
		GetNode<Label>("SpinsLabel").Text = $"Spins: {_gameState.SpinsRemaining}";
		GetNode<Label>("ScoreLabel").Text = $"Score: {_gameState.Score} / {_gameState.ScoreGoal}";
		GD.Print($"[HUD] Labels refreshed -> Chips: {_gameState.ChipsRemainingThisSpin} | Spins: {_gameState.SpinsRemaining} | Score: {_gameState.Score}/{_gameState.ScoreGoal}");
	}
}
