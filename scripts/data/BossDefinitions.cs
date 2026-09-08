using System;
using System.Collections.Generic;
using Godot;

// A round-scoped rule modifier — the "boss dealer" for that single round only.
// Hooks mirror CharmDefinition's shape for consistency; nothing here persists past the round.
public class BossDefinition
{
	public string Id;
	public string Name;
	public string Description;

	// Can the player place this bet type at all this round?
	public Func<BetType, bool> IsBetTypeBlocked;

	// (gameState, bet, winningNumber, baseScore) -> modified score.
	public Func<GameState, Bet, int, int, int> ModifyBetScore;

	// (baseChips) -> modified chips-per-spin for this round only.
	public Func<int, int> ModifyChipsPerSpin;

	// (gameState, activeBets, rng, standardWinningNumber) -> ONE candidate number (single-shift rigging).
	public Func<GameState, List<Bet>, RandomNumberGenerator, int, int> ModifyWinningNumber;

	// (gameState, activeBets, rng, standardWinningNumber) -> multiple candidates.
	// Default (null) = just [standardWinningNumber]. If provided, SpinManager scores every
	// candidate and keeps whichever is WORST for the player (e.g. "two balls" bosses).
	public Func<GameState, List<Bet>, RandomNumberGenerator, int, List<int>> GetCandidateWinningNumbers;

	// How many distinct bet spots can the player use this round? Null = unlimited.
	public Func<int> MaxDistinctBetButtons;

	public BossDefinition(string id, string name, string description)
	{
		Id = id; Name = name; Description = description;
	}
}
