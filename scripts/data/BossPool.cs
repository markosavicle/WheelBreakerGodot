using Godot;
using System.Collections.Generic;

public static class BossPool
{
	public static List<BossDefinition> All = new List<BossDefinition>
	{
		CreateColorblindDealer(),
		CreateParityEnforcer(),
		CreateSkinflint(),
		CreatePickpocket(),
		CreateDoubleDownDealer(),
		CreateThePurist(),
		CreateTheGrinder(),
		CreateTheSpecialist(),
	};

	private static BossDefinition CreateColorblindDealer()
	{
		var b = new BossDefinition("colorblind_dealer", "The Colorblind Dealer",
			"Red and Black bets are disabled this round.");
		b.IsBetTypeBlocked = type => type == BetType.Red || type == BetType.Black;
		return b;
	}

	private static BossDefinition CreateParityEnforcer()
	{
		var b = new BossDefinition("parity_enforcer", "The Parity Enforcer",
			"Odd and Even bets are disabled this round.");
		b.IsBetTypeBlocked = type => type == BetType.Odd || type == BetType.Even;
		return b;
	}

	private static BossDefinition CreateSkinflint()
	{
		var b = new BossDefinition("skinflint", "The Skinflint",
			"All winning bets score 30% less this round.");
		b.ModifyBetScore = (gs, bet, winningNumber, baseScore) => Mathf.RoundToInt(baseScore * 0.7f);
		return b;
	}

	private static BossDefinition CreatePickpocket()
	{
		var b = new BossDefinition("pickpocket", "The Pickpocket",
			"Chips per spin are halved this round.");
		b.ModifyChipsPerSpin = (baseChips) => Mathf.Max(1, baseChips / 2);
		return b;
	}

	private static BossDefinition CreateDoubleDownDealer()
	{
		var b = new BossDefinition("double_down_dealer", "Double Down Dealer",
			"Two balls are spun each round — the WORSE result counts.");
		b.GetCandidateWinningNumbers = (gs, bets, rng, standardNumber) =>
			new List<int> { standardNumber, rng.RandiRange(0, 36) };
		return b;
	}

	private static BossDefinition CreateThePurist()
	{
		var b = new BossDefinition("the_purist", "The Purist",
			"Only straight-up (single number) bets are allowed this round.");
		b.IsBetTypeBlocked = type => type != BetType.Straight;
		return b;
	}

	private static BossDefinition CreateTheGrinder()
	{
		var b = new BossDefinition("the_grinder", "The Grinder",
			"Straight-up bets pay nothing this round — only outside bets score.");
		b.ModifyBetScore = (gs, bet, winningNumber, baseScore) => bet.Type == BetType.Straight ? 0 : baseScore;
		return b;
	}

	private static BossDefinition CreateTheSpecialist()
	{
		var b = new BossDefinition("the_specialist", "The Specialist",
			"You may only place up to 5 bets this round, and they must be Straight-up (single number) bets.");
		
		// Restrict to single numbers only
		b.IsBetTypeBlocked = type => type != BetType.Straight;
		
		// Allow up to 5 distinct bets to match max charm slots
		b.MaxDistinctBetButtons = () => 5;
		
		return b;
	}
}
