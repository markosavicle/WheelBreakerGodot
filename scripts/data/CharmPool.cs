using Godot;
using System.Collections.Generic;

public static class CharmPool
{
	public static List<CharmDefinition> All = new List<CharmDefinition>
	{
		CreateLuckyHorseshoe(),
		CreateHighRoller(),
		CreateGreenZero(),
		CreateWeightedBall(),
		CreateVelvetFelt(),
		CreateDealerVisor(),
		CreateLoadedDice()
	};

	private static CharmDefinition CreateLuckyHorseshoe()
	{
		var j = new CharmDefinition("lucky_horseshoe", "Lucky Horseshoe",
			"Gain +25 score whenever the ball lands on an even number.", 30);
		j.OnSpinResolvedBonusScore = (gs, winningNumber) =>
			(winningNumber != 0 && winningNumber % 2 == 0) ? 25 : 0;
		return j;
	}

	private static CharmDefinition CreateHighRoller()
	{
		var j = new CharmDefinition("high_roller", "High Roller",
			"Doubles the cost of all shop upgrades, but multiplies end-of-round cash rewards by 2x.", 45);
		j.ModifyUpgradeCost = (gs, baseCost) => baseCost * 2;
		j.ModifyRoundCashReward = (gs, baseCash) => baseCash * 2;
		return j;
	}

	private static CharmDefinition CreateGreenZero()
	{
		var j = new CharmDefinition("green_zero", "Green Zero",
			"Hitting a straight bet on 0 yields a massive 50x payout bonus.", 50);
		j.ModifyBetScore = (gs, bet, winningNumber, baseScore) =>
		{
			if (bet.Type == BetType.Straight && winningNumber == 0 && bet.Numbers != null && bet.Numbers.Contains(0))
				return bet.ChipsWagered * 50;
			return baseScore;
		};
		return j;
	}

	private static CharmDefinition CreateWeightedBall()
	{
		var c = new CharmDefinition("weighted_ball", "Weighted Ball",
			"The roulette wheel is rigged: 35% chance per spin to force the ball to land on one of your active bets.", 45);

		c.ModifyWinningNumber = (gs, activeBets, rng, standardWinningNumber) =>
		{
			if (activeBets.Count == 0 || rng.Randf() >= 0.35f)
				return standardWinningNumber;

			var coveredNumbers = new List<int>();
			foreach (var bet in activeBets)
			{
				if (bet.Numbers != null)
				{
					foreach (int n in bet.Numbers)
						if (!coveredNumbers.Contains(n)) coveredNumbers.Add(n);
				}
				else
				{
					for (int i = 0; i <= 36; i++)
						if (WheelData.Hits(bet.Type, null, i) && !coveredNumbers.Contains(i))
							coveredNumbers.Add(i);
				}
			}

			if (coveredNumbers.Count == 0) return standardWinningNumber;

			int rigged = coveredNumbers[rng.RandiRange(0, coveredNumbers.Count - 1)];
			GD.Print($"[Charm] Weighted Ball triggered — rigged winning number to: {rigged}");
			return rigged;
		};

		return c;
	}

	private static CharmDefinition CreateVelvetFelt()
	{
		var j = new CharmDefinition("velvet_felt", "Velvet Felt",
			"Outside bets (Red, Black, Odd, Even, Doz) give +10 extra score per winning bet.", 25);
		j.ModifyBetScore = (gs, bet, winningNumber, baseScore) =>
		{
			if (bet.Type != BetType.Straight)
				return baseScore + 10;
			return baseScore;
		};
		return j;
	}

	private static CharmDefinition CreateDealerVisor()
	{
		var j = new CharmDefinition("dealer_visor", "Dealer's Visor",
			"Gain +$5 extra cash for every remaining spin left over when winning a round.", 30);
		j.ModifyRoundCashReward = (gs, baseCash) => baseCash + (gs.SpinsRemaining * 5);
		return j;
	}

	private static CharmDefinition CreateLoadedDice()
	{
		var j = new CharmDefinition("loaded_dice", "Loaded Dice",
			"Straight bets give 1.25x more score, but outside bets give 20% fewer points.", 40);
		j.ModifyBetScore = (gs, bet, winningNumber, baseScore) =>
		{
			if (bet.Type == BetType.Straight)
				return Mathf.RoundToInt(baseScore * 1.25f);
			else
				return Mathf.RoundToInt(baseScore * 0.8f);
		};
		return j;
	}
}
