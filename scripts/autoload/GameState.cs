using Godot;
using System.Collections.Generic;

public partial class GameState : Node
{
	// Round state
	public int SpinsRemaining;
	public int SpinsPerRound = 5;

	public int ChipsPerSpin = 50;
	public int ChipsRemainingThisSpin;

	public int Score;
	public int ScoreGoal;

	public int Cash;
	public int RoundNumber = 1;

	public List<Bet> ActiveBets = new List<Bet>();

	public override void _Ready()
	{
		StartNewRound();
	}

	public void StartNewRound()
	{
		SpinsRemaining = SpinsPerRound;
		Score = 0;
		ScoreGoal = CalculateScoreGoal(RoundNumber);
		StartNewSpin();
	}

	public void StartNewSpin()
	{
		ChipsRemainingThisSpin = ChipsPerSpin;
		ActiveBets.Clear();
	}

	private int CalculateScoreGoal(int round)
	{
		// Placeholder scaling curve — tune in Step 9 playtest.
		return Mathf.RoundToInt(100 * Mathf.Pow(1.5f, round - 1));
	}
	
	public void ResetRun()
{
	RoundNumber = 1;
	Cash = 0;
	StartNewRound();
	GD.Print("[GameState] Run reset completely.");
}

public void AdvanceToNextRound()
{
	RoundNumber++;
	StartNewRound();
	GD.Print($"[GameState] Advanced to Round {RoundNumber}. New Score Goal: {ScoreGoal}");
}
}
