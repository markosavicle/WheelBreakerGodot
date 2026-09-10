public enum BetType
{
	Straight,   // single number
	Red, Black,
	Odd, Even,
	Low, High,      // 1-18 / 19-36
	Dozen1, Dozen2, Dozen3,
	// Split, Street, Corner intentionally deferred — add once Straight/outside bets are proven fun
}

public enum BetCategory
{
	Straight,
	Dozen,
	Color,
	Parity,
	HighLow
}
