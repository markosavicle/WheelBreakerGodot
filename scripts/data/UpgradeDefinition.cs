using System;

public class UpgradeDefinition : IShopOffer
{
	public string Id { get; }
	public string Name { get; }
	public string Description { get; }
	public int BaseCost { get; }
	public Action<GameState> Apply { get; }

	public UpgradeDefinition(string id, string name, string description, int baseCost, Action<GameState> apply)
	{
		Id = id;
		Name = name;
		Description = description;
		BaseCost = baseCost;
		Apply = apply;
	}

	public void ApplyEffect(GameState gs)
	{
		Apply?.Invoke(gs);
	}
}
