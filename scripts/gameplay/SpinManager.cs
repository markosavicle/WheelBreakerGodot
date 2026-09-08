using Godot;
using System.Collections.Generic;

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

		// Let any owned charm intervene on the winning number, in slot order.
		foreach (var charm in _gameState.OwnedCharms)
		{
			if (charm.ModifyWinningNumber != null)
				winningNumber = charm.ModifyWinningNumber(_gameState, _gameState.ActiveBets, _rng, winningNumber);
		}

		int scoreGained = 0;

		foreach (var bet in _gameState.ActiveBets)
		{
			if (WheelData.Hits(bet.Type, bet.Numbers, winningNumber))
			{
				int baseGain = Mathf.RoundToInt(bet.ChipsWagered * bet.Payout);
				int multipliedGain = Mathf.RoundToInt(baseGain * _gameState.GlobalPayoutMultiplier);

				foreach (var charm in _gameState.OwnedCharms)
				{
					if (charm.ModifyBetScore != null)
						multipliedGain = charm.ModifyBetScore(_gameState, bet, winningNumber, multipliedGain);
				}

				scoreGained += multipliedGain;
			}
		}

		foreach (var charm in _gameState.OwnedCharms)
		{
			if (charm.OnSpinResolvedBonusScore != null)
				scoreGained += charm.OnSpinResolvedBonusScore(_gameState, winningNumber);
		}
		
		if (scoreGained > _gameState.Stats.HighestScoringSpin)
		{
			_gameState.Stats.HighestScoringSpin = scoreGained;
		}

		_gameState.Score += scoreGained;
		_gameState.SpinsRemaining--;

		GD.Print($"[SpinManager] Spin resolved. Landed on {winningNumber}. Gained {scoreGained}. Total Score: {_gameState.Score}/{_gameState.ScoreGoal}");

		var eventBus = GetNode<EventBus>("/root/EventBus");
		eventBus.EmitSignal(EventBus.SignalName.SpinResolved, winningNumber, scoreGained);

		if (_gameState.Score >= _gameState.ScoreGoal)
			eventBus.EmitSignal(EventBus.SignalName.RoundWon);
		else if (_gameState.SpinsRemaining <= 0)
			eventBus.EmitSignal(EventBus.SignalName.RoundLost);
		else
			_gameState.StartNewSpin();
	}
}
