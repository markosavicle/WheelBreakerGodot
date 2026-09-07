using System.Collections.Generic;

public class Bet
{
	public BetType Type;
	public List<int> Numbers;   // which pocket(s) this bet covers
	public int ChipsWagered;
	public float Payout;        // multiplier if this bet hits (see WheelData)

	public Bet(BetType type, List<int> numbers, int chips, float payout)
	{
		Type = type;
		Numbers = numbers;
		ChipsWagered = chips;
		Payout = payout;
	}
}
