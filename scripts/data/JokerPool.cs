using Godot;
using System.Collections.Generic;

public static class JokerPool
{
	public static List<JokerDefinition> All = new List<JokerDefinition>
	{
		CreateLuckyHorseshoe(),
		CreateHighRoller(),
		CreateGreenZero(),
		CreateWeightedBall(),
		CreateVelvetFelt(),
	};

	private static JokerDefinition CreateLuckyHorseshoe()
	{
		var j = new JokerDefinition("lucky_horseshoe", "Lucky Horseshoe",
			"Gain +20 score whenever the ball lands on an even number.", 35);
		j.OnSpinResolvedBonusScore = (gs, winningNumber) =>
			(winningNumber != 0 && winningNumber % 2 == 0) ? 20 : 0;
		return j;
	}

	private static JokerDefinition CreateHighRoller()
	{
		var j = new JokerDefinition("high_roller", "High Roller",
			"Doubles the cost of all upgrades, but multiplies round cash rewards by 1.5x.", 40);
		j.ModifyUpgradeCost = (gs, baseCost) => baseCost * 2;
		j.ModifyRoundCashReward = (gs, baseCash) => Mathf.RoundToInt(baseCash * 1.5f);
		return j;
	}

	private static JokerDefinition CreateGreenZero()
	{
		var j = new JokerDefinition("green_zero", "Green Zero",
			"Hitting a straight bet on 0 pays a massive bonus instead of the standard payout.", 50);
		j.ModifyBetScore = (gs, bet, winningNumber, baseScore) =>
		{
			if (bet.Type == BetType.Straight && winningNumber == 0 && bet.Numbers != null && bet.Numbers.Contains(0))
				return bet.ChipsWagered * 60; // deliberately above the standard 35x straight payout
			return baseScore;
		};
		return j;
	}

	private static JokerDefinition CreateWeightedBall()
	{
		var j = new JokerDefinition("weighted_ball", "Weighted Ball",
			"Red bets pay out an extra 0.5x multiplier.", 30);
		j.ModifyBetScore = (gs, bet, winningNumber, baseScore) =>
		{
			if (bet.Type == BetType.Red && WheelData.IsRed(winningNumber))
				return Mathf.RoundToInt(baseScore * 1.5f);
			return baseScore;
		};
		return j;
	}

	private static JokerDefinition CreateVelvetFelt()
	{
		var j = new JokerDefinition("velvet_felt", "Velvet Felt",
			"+10 cash at the end of every round, regardless of performance.", 25);
		j.ModifyRoundCashReward = (gs, baseCash) => baseCash + 10;
		return j;
	}
}
