using System;

// A held, player-triggered item — bought into a limited pocket, used on demand (not instant-apply).
public class ConsumableDefinition : IShopOffer
{
	public string Id { get; }
	public string Name { get; }
	public string Description { get; }
	public int BaseCost { get; }

	public Func<GameState, bool> CanUse;
	public Action<GameState> Use;

	public ConsumableDefinition(string id, string name, string description, int baseCost)
	{
		Id = id; Name = name; Description = description; BaseCost = baseCost;
	}
}
