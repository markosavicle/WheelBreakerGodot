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
				scoreGained += Mathf.RoundToInt(bet.ChipsWagered * bet.Payout);
			}
		}

		_gameState.Score += scoreGained;
		_gameState.SpinsRemaining--;

		GetNode<EventBus>("/root/EventBus").EmitSignal(
			EventBus.SignalName.SpinResolved, winningNumber, scoreGained);

		CheckRoundEnd();

		if (_gameState.SpinsRemaining > 0 && _gameState.Score < _gameState.ScoreGoal)
		{
			_gameState.StartNewSpin();
		}
	}

	private void CheckRoundEnd()
	{
		var eventBus = GetNode<EventBus>("/root/EventBus");

		if (_gameState.Score >= _gameState.ScoreGoal)
		{
			eventBus.EmitSignal(EventBus.SignalName.RoundWon);
		}
		else if (_gameState.SpinsRemaining <= 0)
		{
			eventBus.EmitSignal(EventBus.SignalName.RoundLost);
		}
	}
}
