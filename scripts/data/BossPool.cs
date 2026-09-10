using Godot;
using System.Collections.Generic;

public static class BossPool
{
	public static List<BossDefinition> All = new List<BossDefinition>
	{
		CreateColorblindDealer(), CreateParityEnforcer(), CreateSkinflint(), CreatePickpocket(),
		CreateDoubleDownDealer(), CreateThePurist(), CreateTheGrinder(), CreateTheSpecialist(),
		CreateTheLoanShark(), CreateTheAuditor(), CreateCreatureOfHabit(), CreateTheLandlord(), CreateTheVoid(),
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
		b.ModifyChipsPerSpin = (gs, baseChips) => Mathf.Max(1, baseChips / 2); 
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
	
	// Chip budget pressure
	private static BossDefinition CreateTheLoanShark()
	{
		var b = new BossDefinition("the_loan_shark", "The Loan Shark",
			"Chips per spin shrink by 5 with every spin you take this round.");
		b.ModifyChipsPerSpin = (gs, baseChips) =>
		{
			int spinsUsed = gs.SpinsPerRound + gs.BonusSpinsPerRound - gs.SpinsRemaining;
			return Mathf.Max(1, baseChips - (spinsUsed * 5));
		};
		return b;
	}

	// Chip budget pressure — punishes holding back instead of chasing decay
	private static BossDefinition CreateTheAuditor()
	{
		var b = new BossDefinition("the_auditor", "The Auditor",
			"Any chips left unspent this spin are deducted directly from your score.");
		b.ModifyFinalSpinScore = (gs, bets, total) => total - gs.ChipsRemainingThisSpin;
		return b;
	}

	// Repeat-bet restriction
	private static BossDefinition CreateCreatureOfHabit()
	{
		var b = new BossDefinition("creature_of_habit", "Creature of Habit",
			"Repeat Last Bet is disabled this round.");
		b.BlocksRepeatBet = true;
		return b;
	}

	// Empty-spot penalty
	private static BossDefinition CreateTheLandlord()
	{
		var b = new BossDefinition("the_landlord", "The Landlord",
			"Score is halved if you bet on fewer than 3 spots this spin.");
		b.ModifyFinalSpinScore = (gs, bets, total) => bets.Count < 3 ? total / 2 : total;
		return b;
	}

	// Empty-spot penalty — sharper version
	private static BossDefinition CreateTheVoid()
	{
		var b = new BossDefinition("the_void", "The Void",
			"Betting on only a single spot scores nothing this round.");
		b.ModifyFinalSpinScore = (gs, bets, total) => bets.Count == 1 ? 0 : total;
		return b;
	}
}
