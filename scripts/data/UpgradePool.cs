using System.Collections.Generic;

// The full pool the shop rolls from. Add new upgrades here — nowhere else needs to change.
public static class UpgradePool
{
	public static List<UpgradeDefinition> All = new List<UpgradeDefinition>
	{
		new UpgradeDefinition("extra_spin", "Extra Spin", "+1 max spin per round", 15,
			gs => gs.BonusSpinsPerRound += 1),

		new UpgradeDefinition("chip_boost", "Chip Boost", "+10 chips per spin", 20,
			gs => gs.BonusChipsPerSpin += 10),

		new UpgradeDefinition("payout_boost", "Payout Boost", "+0.25x global payout multiplier", 30,
			gs => gs.GlobalPayoutMultiplier += 0.25f),

		new UpgradeDefinition("big_chip_boost", "Big Chip Stack", "+25 chips per spin", 40,
			gs => gs.BonusChipsPerSpin += 25),

		new UpgradeDefinition("cash_flow", "Cash Flow", "+5 cash per unused spin at round end", 25,
			gs => gs.CashPerRemainingSpin += 5),

		new UpgradeDefinition("bigger_purse", "Bigger Purse", "+15 flat cash reward per round win", 25,
			gs => gs.FlatRoundReward += 15),
	};
}
