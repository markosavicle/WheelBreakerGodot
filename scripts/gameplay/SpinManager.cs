using Godot;
using System.Collections.Generic;
using System.Linq;

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

		int winningNumber;

		// Absolute Debug Override — takes precedence over Bosses and Charms
		if (_gameState.DebugForcedWinningNumber.HasValue)
		{
			winningNumber = _gameState.DebugForcedWinningNumber.Value;
			_gameState.DebugForcedWinningNumber = null; // consume it once
			GD.Print($"[SpinManager] 🛠 DEBUG FORCED winning number to: {winningNumber}");
		}
		else
		{
			var boss = _gameState.ActiveBoss;
			winningNumber = _rng.RandiRange(0, 36);

			// Boss: single-number rig.
			if (boss?.ModifyWinningNumber != null)
				winningNumber = boss.ModifyWinningNumber(_gameState, _gameState.ActiveBets, _rng, winningNumber);

			// Boss: multi-candidate rounds (e.g. "two balls, worse counts").
			List<int> candidates = new List<int> { winningNumber };
			if (boss?.GetCandidateWinningNumbers != null)
				candidates = boss.GetCandidateWinningNumbers(_gameState, _gameState.ActiveBets, _rng, winningNumber);

			if (candidates.Count > 1)
			{
				winningNumber = candidates.OrderBy(n => CalculateScoreForNumber(n, boss)).First();
			}

			// Charms get a say unless debug forced
			foreach (var charm in _gameState.OwnedCharms)
			{
				if (charm.ModifyWinningNumber != null)
					winningNumber = charm.ModifyWinningNumber(_gameState, _gameState.ActiveBets, _rng, winningNumber);
			}
		}

		int scoreGained = CalculateScoreForNumber(winningNumber, _gameState.ActiveBoss);

		if (scoreGained > _gameState.Stats.HighestScoringSpin)
			_gameState.Stats.HighestScoringSpin = scoreGained;

		_gameState.Score += scoreGained;
		_gameState.SpinsRemaining--;

		GD.Print($"[SpinManager] Landed on {winningNumber}. Gained {scoreGained}. Score: {_gameState.Score}/{_gameState.ScoreGoal}");

		var eventBus = GetNode<EventBus>("/root/EventBus");
		eventBus.EmitSignal(EventBus.SignalName.SpinResolved, winningNumber, scoreGained);

		if (_gameState.Score >= _gameState.ScoreGoal)
			eventBus.EmitSignal(EventBus.SignalName.RoundWon);
		else if (_gameState.SpinsRemaining <= 0)
			eventBus.EmitSignal(EventBus.SignalName.RoundLost);
		else
			_gameState.StartNewSpin();
	}

	// Pure — no state mutation — so it's safe to call repeatedly when comparing boss candidates.
	private int CalculateScoreForNumber(int winningNumber, BossDefinition boss)
	{
		int total = 0;

		foreach (var bet in _gameState.ActiveBets)
		{
			if (!WheelData.Hits(bet.Type, bet.Numbers, winningNumber)) continue;

			int gain = Mathf.RoundToInt(bet.ChipsWagered * bet.Payout * _gameState.GlobalPayoutMultiplier);

			if (boss?.ModifyBetScore != null)
				gain = boss.ModifyBetScore(_gameState, bet, winningNumber, gain);

			foreach (var charm in _gameState.OwnedCharms)
			{
				if (charm.ModifyBetScore != null)
					gain = charm.ModifyBetScore(_gameState, bet, winningNumber, gain);
			}

			total += gain;
		}

		foreach (var charm in _gameState.OwnedCharms)
		{
			if (charm.OnSpinResolvedBonusScore != null)
				total += charm.OnSpinResolvedBonusScore(_gameState, winningNumber);
		}

		return total;
	}
}
