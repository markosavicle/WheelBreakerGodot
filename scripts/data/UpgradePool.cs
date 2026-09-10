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
			
		new UpgradeDefinition("tip_sheet_straight", "Straight Tip Sheet", "+2.0x payout for Straight (single number) bets", 35,
			gs => gs.AddCategoryBonus(BetCategory.Straight, 2.0f)),

		new UpgradeDefinition("tip_sheet_color", "Color Tip Sheet", "+0.15x payout for Red/Black bets", 20,
			gs => gs.AddCategoryBonus(BetCategory.Color, 0.15f)),

		new UpgradeDefinition("tip_sheet_parity", "Parity Tip Sheet", "+0.15x payout for Odd/Even bets", 20,
			gs => gs.AddCategoryBonus(BetCategory.Parity, 0.15f)),

		new UpgradeDefinition("tip_sheet_highlow", "High/Low Tip Sheet", "+0.15x payout for 1-18/19-36 bets", 20,
			gs => gs.AddCategoryBonus(BetCategory.HighLow, 0.15f)),

		new UpgradeDefinition("tip_sheet_dozens", "Dozens Tip Sheet", "+0.3x payout for Dozen bets", 22,
			gs => gs.AddCategoryBonus(BetCategory.Dozen, 0.3f)),	
	};
}
