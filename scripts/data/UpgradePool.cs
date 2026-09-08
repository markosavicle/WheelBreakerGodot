using System.Collections.Generic;

public static class UpgradePool
{
	public static List<UpgradeDefinition> All = new List<UpgradeDefinition>
	{
		new UpgradeDefinition("extra_spin", "Extra Spin", "+1 max spin per round", 18,
			gs => gs.BonusSpinsPerRound += 1),

		new UpgradeDefinition("chip_boost", "Chip Boost", "+15 chips per spin", 15,
			gs => gs.BonusChipsPerSpin += 15),

		new UpgradeDefinition("payout_boost", "Payout Boost", "+0.25x global payout multiplier", 35,
			gs => gs.GlobalPayoutMultiplier += 0.25f),

		new UpgradeDefinition("big_chip_boost", "Big Chip Stack", "+40 chips per spin", 45,
			gs => gs.BonusChipsPerSpin += 40),

		new UpgradeDefinition("cash_flow", "Cash Flow", "+5 cash per unused spin at round end", 20,
			gs => gs.CashPerRemainingSpin += 5),

		new UpgradeDefinition("bigger_purse", "Bigger Purse", "+20 flat cash reward per round win", 25,
			gs => gs.FlatRoundReward += 20),
			
		new UpgradeDefinition("pit_boss_bribe", "Pit Boss Bribe", "Lowers shop reroll costs by $1 (min $1)", 30,
			gs => gs.RerollCostDiscount += 1),
	};
}
