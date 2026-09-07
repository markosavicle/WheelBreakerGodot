using Godot;

public partial class EventBus : Node
{
	[Signal] public delegate void BetPlacedEventHandler();
	[Signal] public delegate void SpinResolvedEventHandler(int winningNumber, int scoreGained);
	[Signal] public delegate void RoundWonEventHandler();
	[Signal] public delegate void RoundLostEventHandler();
}
