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

	public int Cash = 0;
	public int RoundNumber = 1;

	// Economy tuning
	public int FlatRoundReward = 25;
	public int CashPerRemainingSpin = 10;
	
	// Permanent Run Upgrades
	public int BonusChipsPerSpin = 0;
	public int BonusSpinsPerRound = 0;
	public float GlobalPayoutMultiplier = 1.0f;

	public List<Bet> ActiveBets = new List<Bet>();

	public override void _Ready()
	{
		StartNewRound();
	}

	public void StartNewRound()
	{
		SpinsRemaining = SpinsPerRound + BonusSpinsPerRound;
		Score = 0;
		ScoreGoal = CalculateScoreGoal(RoundNumber);
		StartNewSpin();
	}

	public void StartNewSpin()
	{
		ChipsRemainingThisSpin = ChipsPerSpin + BonusChipsPerSpin;
		ActiveBets.Clear();
	}

	private int CalculateScoreGoal(int round)
	{
		return Mathf.RoundToInt(100 * Mathf.Pow(1.5f, round - 1));
	}
	
	public void ResetRun()
	{
		RoundNumber = 1;
		Cash = 0;
		
		// Reset all shop upgrades back to default starting values
		BonusSpinsPerRound = 0;
		BonusChipsPerSpin = 0;
		GlobalPayoutMultiplier = 1.0f;

		StartNewRound();
		GD.Print("[GameState] Run reset completely — all cash and upgrades wiped.");
	}

	public void AdvanceToNextRound()
	{
		// Calculate cash reward: Flat amount + bonus for remaining spins
		int bonusCash = SpinsRemaining * CashPerRemainingSpin;
		int totalEarned = FlatRoundReward + bonusCash;
		Cash += totalEarned;

		GD.Print($"[GameState] Round {RoundNumber} Cleared! Earned {totalEarned} Cash ({FlatRoundReward} base + {bonusCash} unspent spin bonus). Total Cash: {Cash}");

		RoundNumber++;
		StartNewRound();
	}
	
	public bool TryBuyUpgrade(string upgradeName, int cost)
	{
		if (Cash < cost)
		{
			GD.Print($"[GameState] Not enough cash to buy {upgradeName}. Cost: ${cost}, Have: ${Cash}");
			return false;
		}

		Cash -= cost;

		switch (upgradeName)
		{
			case "ExtraSpin":
				BonusSpinsPerRound += 1;
				GD.Print("[GameState] Upgraded: +1 Max Spin per round!");
				break;
			case "ChipBoost":
				BonusChipsPerSpin += 10;
				GD.Print("[GameState] Upgraded: +10 Chips per spin!");
				break;
			case "PayoutBoost":
				GlobalPayoutMultiplier += 0.25f;
				GD.Print("[GameState] Upgraded: +0.25x Payout Multiplier!");
				break;
		}
		return true;
	}
}
