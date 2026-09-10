using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class GameState : Node
{
	public const int RoundsPerStake = 3;
	public const int TotalStakes = 8;

	public int RoundNumber = 1;
	public int Stake = 1;
	public int RoundInStake = 1;

	public BossDefinition ActiveBoss;
	public bool IsFinalBossRound => Stake >= TotalStakes && RoundInStake >= RoundsPerStake;

	public int SpinsRemaining;
	public int SpinsPerRound = 5;

	public int ChipsPerSpin = 50;
	public int ChipsRemainingThisSpin;

	public int Score;
	public int ScoreGoal;

	public int Cash = 0;

	public int FlatRoundReward = 25;
	public int CashPerRemainingSpin = 10;
	public int RerollCostDiscount = 0;

	public int BonusChipsPerSpin = 0;
	public int BonusSpinsPerRound = 0;
	public float GlobalPayoutMultiplier = 1.0f;

	// Admin Debug Support Property
	public int? DebugForcedWinningNumber = null;

	public RunStats Stats = new RunStats();

	public List<Bet> ActiveBets = new List<Bet>();

	public List<CharmDefinition> OwnedCharms = new List<CharmDefinition>();
	public int MaxCharmSlots = 5;
	public List<UpgradeDefinition> OwnedUpgrades = new List<UpgradeDefinition>();
	
	public Dictionary<BetCategory, float> CategoryPayoutBonus = new Dictionary<BetCategory, float>();
	
	public List<ConsumableDefinition> HeldConsumables = new List<ConsumableDefinition>();
	public int MaxConsumableSlots = 2;
	public int? PeekedNextNumber = null;


	private RandomNumberGenerator _rng = new RandomNumberGenerator();

	public override void _Ready()
	{
		_rng.Randomize();
		StartNewRound();
	}

	public void StartNewRound()
	{
		RoundInStake = ((RoundNumber - 1) % RoundsPerStake) + 1;
		Stake = ((RoundNumber - 1) / RoundsPerStake) + 1;

		ActiveBoss = (RoundInStake == RoundsPerStake) ? PickRandomBoss() : null;

		SpinsRemaining = SpinsPerRound + BonusSpinsPerRound;
		Score = 0;
		ScoreGoal = CalculateScoreGoal(Stake, RoundInStake);
		StartNewSpin();

		GetNodeOrNull<EventBus>("/root/EventBus")?.EmitSignal(EventBus.SignalName.RoundStarted);

		if (ActiveBoss != null)
			GD.Print($"[GameState] BOSS ROUND — {ActiveBoss.Name}: {ActiveBoss.Description}");
	}

	public void StartNewSpin()
	{
		int baseChips = ChipsPerSpin + BonusChipsPerSpin;

		foreach (var charm in OwnedCharms)
		{
			if (charm.ModifyChipsPerSpin != null)
				baseChips = charm.ModifyChipsPerSpin(this, baseChips);
		}

		if (ActiveBoss?.ModifyChipsPerSpin != null)
			baseChips = ActiveBoss.ModifyChipsPerSpin(this, baseChips);  // signature change — see boss section

		ChipsRemainingThisSpin = Mathf.Max(0, baseChips);
		ActiveBets.Clear();
	}

	private BossDefinition PickRandomBoss()
	{
		var candidates = BossPool.All.Where(b => ActiveBoss == null || b.Id != ActiveBoss.Id).ToList();
		return candidates[_rng.RandiRange(0, candidates.Count - 1)];
	}

	private int CalculateScoreGoal(int stake, int roundInStake)
	{
		float stakeBase = 100f * Mathf.Pow(1.6f, stake - 1);
		float roundMultiplier = roundInStake switch
		{
			1 => 1.0f,
			2 => 1.5f,
			3 => 2.2f,
			_ => 1.0f
		};
		return Mathf.RoundToInt(stakeBase * roundMultiplier);
	}

	public void ResetRun()
	{
		Stats = new RunStats();

		RoundNumber = 1;
		Cash = 0;

		BonusSpinsPerRound = 0;
		BonusChipsPerSpin = 0;
		GlobalPayoutMultiplier = 1.0f;
		CashPerRemainingSpin = 10;
		FlatRoundReward = 25;
		RerollCostDiscount = 0;

		OwnedCharms.Clear();
		OwnedUpgrades.Clear();
		CategoryPayoutBonus.Clear();
		
		HeldConsumables.Clear();
		PeekedNextNumber = null;

		StartNewRound();
		GD.Print("[GameState] Run reset completely.");
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
		Stats.RoundsSurvived++;
		RoundNumber++;
		StartNewRound();
	}
	
	public void AddCategoryBonus(BetCategory category, float amount)
	{
		if (!CategoryPayoutBonus.ContainsKey(category)) CategoryPayoutBonus[category] = 0f;
		CategoryPayoutBonus[category] += amount;
	}

	public float GetCategoryBonus(BetType type) =>
		CategoryPayoutBonus.TryGetValue(WheelData.GetCategory(type), out float bonus) ? bonus : 0f;
}
