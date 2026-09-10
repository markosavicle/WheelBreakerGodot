using System.Collections.Generic;
using Godot;

public static class ConsumablePool
{
	public static List<ConsumableDefinition> All = new List<ConsumableDefinition>
	{
		CreateCompedDrink(),
		CreateMarkedCard(),
		CreatePitBossFavor(),
	};

	private static ConsumableDefinition CreateCompedDrink()
	{
		var c = new ConsumableDefinition("comped_drink", "Comped Drink", "Instantly gain $15 cash.", 12);
		c.CanUse = gs => true;
		c.Use = gs => gs.Cash += 15;
		return c;
	}

	private static ConsumableDefinition CreateMarkedCard()
	{
		var c = new ConsumableDefinition("marked_card", "Marked Card",
			"Reveals AND guarantees the outcome of your next spin.", 30);
		c.CanUse = gs => gs.PeekedNextNumber == null;
		c.Use = gs =>
		{
			var rng = new RandomNumberGenerator();
			rng.Randomize();
			gs.PeekedNextNumber = rng.RandiRange(0, 36);
		};
		return c;
	}

	private static ConsumableDefinition CreatePitBossFavor()
	{
		var c = new ConsumableDefinition("pit_boss_favor", "Pit Boss's Favor",
			"Refunds all chips wagered this spin — your bets stay active.", 20);
		c.CanUse = gs => gs.ActiveBets.Count > 0;
		c.Use = gs =>
		{
			int refund = 0;
			foreach (var bet in gs.ActiveBets) refund += bet.ChipsWagered;
			gs.ChipsRemainingThisSpin += refund;
		};
		return c;
	}
}
