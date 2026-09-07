using Godot;

public partial class SpinManager : Node
{
	private GameState _gameState;
	private RandomNumberGenerator _rng = new RandomNumberGenerator();

	public override void _Ready()
	{
		_gameState = GetNode<GameState>("/root/GameState");
		_rng.Randomize();
	}

	public void ResolveSpin()
	{
		if (_gameState.SpinsRemaining <= 0) return;

		int winningNumber = _rng.RandiRange(0, 36);
		int scoreGained = 0;

		foreach (var bet in _gameState.ActiveBets)
		{
			if (WheelData.Hits(bet.Type, bet.Numbers, winningNumber))
			{
				// Apply base payout calculation
				int baseGain = Mathf.RoundToInt(bet.ChipsWagered * bet.Payout);
				
				// Apply permanent shop multiplier upgrade
				int multipliedGain = Mathf.RoundToInt(baseGain * _gameState.GlobalPayoutMultiplier);
				
				scoreGained += multipliedGain;
			}
		}

		_gameState.Score += scoreGained;
		_gameState.SpinsRemaining--;

		GD.Print($"[SpinManager] Spin resolved. Landed on {winningNumber}. Gained {scoreGained}. Total Score: {_gameState.Score}/{_gameState.ScoreGoal}");

		// Emit spin resolved event
		var eventBus = GetNode<EventBus>("/root/EventBus");
		eventBus.EmitSignal(EventBus.SignalName.SpinResolved, winningNumber, scoreGained);

		// Check win/loss immediately after updating score and spins
		if (_gameState.Score >= _gameState.ScoreGoal)
		{
			GD.Print("[SpinManager] Score goal reached! Triggering RoundWon.");
			eventBus.EmitSignal(EventBus.SignalName.RoundWon);
		}
		else if (_gameState.SpinsRemaining <= 0)
		{
			GD.Print("[SpinManager] Out of spins! Triggering RoundLost.");
			eventBus.EmitSignal(EventBus.SignalName.RoundLost);
		}
		else
		{
			_gameState.StartNewSpin();
		}
	}
}
