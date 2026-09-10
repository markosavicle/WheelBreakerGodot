using System.Collections.Generic;

// Static rules of a European wheel (single 0, 37 pockets) — the "physics" of the game.
public static class WheelData
{
	public static readonly HashSet<int> RedNumbers = new HashSet<int>
	{
		1,3,5,7,9,12,14,16,18,19,21,23,25,27,30,32,34,36
	};

	public static bool IsRed(int number) => RedNumbers.Contains(number);
	public static bool IsBlack(int number) => number != 0 && !RedNumbers.Contains(number);

	// Base payout multipliers — standard roulette odds, doubling as our starting "Score multiplier" balance.
	public static float GetBasePayout(BetType type)
	{
		return type switch
		{
			BetType.Straight => 35f,
			BetType.Red or BetType.Black or BetType.Odd or BetType.Even or BetType.Low or BetType.High => 1f,
			BetType.Dozen1 or BetType.Dozen2 or BetType.Dozen3 => 2f,
			_ => 0f
		};
	}

	// Does the winning number satisfy this bet type?
	public static bool Hits(BetType type, List<int> betNumbers, int winningNumber)
	{
		switch (type)
		{
			case BetType.Straight: return betNumbers.Contains(winningNumber);
			case BetType.Red: return IsRed(winningNumber);
			case BetType.Black: return IsBlack(winningNumber);
			case BetType.Odd: return winningNumber != 0 && winningNumber % 2 == 1;
			case BetType.Even: return winningNumber != 0 && winningNumber % 2 == 0;
			case BetType.Low: return winningNumber >= 1 && winningNumber <= 18;
			case BetType.High: return winningNumber >= 19 && winningNumber <= 36;
			case BetType.Dozen1: return winningNumber >= 1 && winningNumber <= 12;
			case BetType.Dozen2: return winningNumber >= 13 && winningNumber <= 24;
			case BetType.Dozen3: return winningNumber >= 25 && winningNumber <= 36;
			default: return false;
		}
	}
	
	public static BetCategory GetCategory(BetType type)
	{
		return type switch
		{
			BetType.Straight => BetCategory.Straight,
			BetType.Dozen1 or BetType.Dozen2 or BetType.Dozen3 => BetCategory.Dozen,
			BetType.Red or BetType.Black => BetCategory.Color,
			BetType.Odd or BetType.Even => BetCategory.Parity,
			BetType.Low or BetType.High => BetCategory.HighLow,
			_ => BetCategory.Straight
		};
	}
}
