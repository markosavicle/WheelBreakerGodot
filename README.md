# WheelBreaker

A **Balatro-inspired roguelike built around European roulette**, written in **C#** on **Godot 4 (.NET 8)**.

Instead of playing roulette for money, you place bets to hit a **score goal** within a limited number of spins. Clear the round, earn cash, and spend it in a shop on upgrades, charms and consumables that bend the rules of the wheel — then face a boss dealer who bends them back.

## Gameplay loop

1. **Bet** — each spin gives you a chip budget to spread across straight numbers, red/black, odd/even, low/high and dozens.
2. **Spin** — the winning number is resolved and scored using standard roulette payouts, modified by your build.
3. **Round result** — reach the score goal before you run out of spins to win the round; leftover spins turn into bonus cash.
4. **Shop** — buy stackable upgrades, passive charms and one-shot consumables, or reroll the offers.
5. **Progress** — 8 stakes × 3 rounds, with score goals scaling each stake. Every third round is a **boss round**.

### Content

- **10 charms** (passive, run-long modifiers) — e.g. *Weighted Ball* (35% chance to rig the ball onto one of your bets), *High Roller* (double upgrade costs, double cash rewards), *Green Zero*.
- **13 bosses** (round-scoped rule changes) — e.g. *The Colorblind Dealer* (red/black disabled), *The Skinflint* (wins score 30% less), *Double Down Dealer* (two balls, the worse one counts).
- **12 stackable upgrades** — extra spins, chip boosts, payout multipliers and per-category "tip sheets".
- **3 consumables** — e.g. *Marked Card*, which reveals and guarantees the next winning number.
- End-of-run statistics screen and a debug panel for forcing winning numbers during testing.

## Architecture

```
scripts/
├── autoload/      # Global singletons
│   ├── EventBus.cs      # Typed Godot signals (BetPlaced, SpinResolved, RoundWon, ...)
│   └── GameState.cs     # Run state: stake/round progression, score, cash, owned items
├── data/          # Plain C# definitions and content pools
│   ├── WheelData.cs     # Rules of a single-zero wheel: colors, hit tests, base payouts
│   ├── CharmDefinition.cs / CharmPool.cs
│   ├── BossDefinitions.cs / BossPool.cs
│   ├── UpgradeDefinition.cs / UpgradePool.cs
│   ├── ConsumableDefinition.cs / ConsumablePool.cs
│   └── IShopOffer.cs    # Common interface for everything the shop can sell
├── gameplay/
│   ├── SpinManager.cs   # Resolves a spin and calculates its score
│   ├── BettingTable.cs  # Bet placement, hold-to-bet, repeat/clear bets
│   └── ShopManager.cs   # Offer rolling, purchasing, rerolls
└── ui/
    ├── HUD.cs
    └── InventoryPanel.cs
```

Key design decisions:

- **Event-driven flow.** Gameplay systems and UI talk through a global `EventBus` (Godot signals) instead of holding references to each other. `SpinManager` emits `SpinResolved` / `RoundWon` / `RoundLost`; the HUD, shop and stats just subscribe.
- **Data-driven modifiers via optional hooks.** Charms and bosses are plain data objects that expose optional `Func<>` hooks (`ModifyWinningNumber`, `ModifyBetScore`, `ModifyChipsPerSpin`, `ModifyFinalSpinScore`, ...). A `null` hook is a no-op, so each item wires up only the moment it cares about, and new content is added in the pools without touching the core loop.
- **Clear resolution order.** Debug override → consumable guarantee → RNG → boss rigging → charm effects. This keeps interactions between stacked modifiers predictable.
- **Pure scoring function.** `CalculateScoreForNumber` has no side effects, so it can be evaluated multiple times — e.g. when a boss spins two balls and the game picks the worse result for the player.
- **Shared shop interface.** Upgrades, charms and consumables all implement `IShopOffer` (id, name, description, cost), so the shop keeps its stock in a single `List<IShopOffer>`. Each shop rolls 2 upgrades, 2 charms (excluding ones you already own) and 1 consumable.

## Running

Requirements: [Godot 4.x .NET edition](https://godotengine.org/download) and the .NET 8 SDK.

1. Clone the repo and open `project.godot` in Godot (.NET).
2. Build the C# solution (Godot does it automatically on first run, or `dotnet build`).
3. Press **F5** to run the main scene.

## History

The first prototype was built in Unity ([wheelbreaker](https://github.com/markosavicle/wheelbreaker)). I later rewrote it in Godot (still C#) and expanded it into a full roguelike; this repo is where development continues.

## Status

Personal project / work in progress. The core loop, shop, charms, bosses and consumables are playable; art and audio are placeholders.
