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
	
	public int RerollCostDiscount = 0;
	
	public RunStats Stats = new RunStats();

	public List<Bet> ActiveBets = new List<Bet>();
	
	// Replace OwnedCharms / MaxCharmSlots with Charms
public List<CharmDefinition> OwnedCharms = new List<CharmDefinition>();
public int MaxCharmSlots = 5;
	public List<UpgradeDefinition> OwnedUpgrades = new List<UpgradeDefinition>();

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
		Stats = new RunStats();
		
		RoundNumber = 1;
		Cash = 0;

		BonusSpinsPerRound = 0;
		BonusChipsPerSpin = 0;
		GlobalPayoutMultiplier = 1.0f;
		CashPerRemainingSpin = 10;   // ← add: reset to base
		FlatRoundReward = 25;        // ← add: reset to base
		RerollCostDiscount = 0;
		
		OwnedCharms.Clear();
		OwnedUpgrades.Clear();
		

		StartNewRound();
		GD.Print("[GameState] Run reset completely — all cash and upgrades wiped.");
	}
	
		public int GrantRoundCashReward()
		{
			int bonusCash = SpinsRemaining * CashPerRemainingSpin;
			int totalEarned = FlatRoundReward + bonusCash;

			foreach (var charm in OwnedCharms)
			{
				if (charm.ModifyRoundCashReward != null)
					totalEarned = charm.ModifyRoundCashReward(this, totalEarned);
			}

			Stats.TotalCashEarned += totalEarned;
			
			Cash += totalEarned;
			return totalEarned;
		}
	
	public int GetModifiedUpgradeCost(int baseCost)
		{
			int cost = baseCost;
			foreach (var charm in OwnedCharms)
			{
				if (charm.ModifyUpgradeCost != null)
					cost = charm.ModifyUpgradeCost(this, cost);
			}
			return cost;
		}

	public void AdvanceToNextRound()
	{
		// Cash is now granted once, in GrantRoundCashReward() — called from HUD when the round is won.
		Stats.RoundsSurvived++;
		RoundNumber++;
		StartNewRound();
	}
	
}
