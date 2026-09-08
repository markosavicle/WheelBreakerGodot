using System;
using System.Collections.Generic;
using Godot;

// A passive, permanent-for-the-run modifier. Hooks are optional (null = no-op),
// so each Charm only wires up the specific moment it cares about.
public class CharmDefinition : IShopOffer
{
	public string Id { get; }
	public string Name { get; }
	public string Description { get; }
	public int BaseCost { get; }
	
	// (gameState, activeBets, rng, standardWinningNumber) -> final winning number.
	// Return standardWinningNumber unchanged if this charm doesn't want to intervene this spin.
	public Func<GameState, List<Bet>, RandomNumberGenerator, int, int> ModifyWinningNumber;

	// (gameState, bet, winningNumber, baseScore) -> modified score. Called per winning bet.
	public Func<GameState, Bet, int, int, int> ModifyBetScore;

	// (gameState, winningNumber) -> flat bonus score. Called once per spin, regardless of bets.
	public Func<GameState, int, int> OnSpinResolvedBonusScore;

	// (gameState, baseCost) -> modified cost. Applied to upgrade purchases only.
	public Func<GameState, int, int> ModifyUpgradeCost;

	// (gameState, baseCash) -> modified cash. Applied once at round-clear.
	public Func<GameState, int, int> ModifyRoundCashReward;

	public CharmDefinition(string id, string name, string description, int baseCost)
	{
		Id = id;
		Name = name;
		Description = description;
		BaseCost = baseCost;
	}
}
