using Godot;

public partial class InventoryPanel : Control
{
	private GameState _gameState;
	private VBoxContainer _upgradesList;
	private VBoxContainer _jokersList;

	public override void _Ready()
	{
		_gameState = GetNode<GameState>("/root/GameState");
		
		// Updated node paths matching the CenterContainer/PanelBackground layout in Main.tscn
		_upgradesList = GetNode<VBoxContainer>("CenterContainer/PanelBackground/UpgradesList");
		_jokersList = GetNode<VBoxContainer>("CenterContainer/PanelBackground/JokersList");

		var closeButton = GetNodeOrNull<Button>("CenterContainer/PanelBackground/CloseButton");
		if (closeButton != null) closeButton.Pressed += () => Visible = false;

		var eventBus = GetNodeOrNull<EventBus>("/root/EventBus");
		if (eventBus != null)
			eventBus.Connect(EventBus.SignalName.ShopUpdated, new Callable(this, nameof(RefreshLists)));

		Visible = false;
		MouseFilter = Control.MouseFilterEnum.Stop;
	}

	public void ToggleVisibility()
	{
		Visible = !Visible;
		if (Visible)
		{
			SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
			RefreshLists();
		}
	}

	private void RefreshLists()
	{
		if (_upgradesList == null || _jokersList == null) return;

		foreach (Node child in _upgradesList.GetChildren()) child.QueueFree();
		foreach (Node child in _jokersList.GetChildren()) child.QueueFree();

		if (_gameState.OwnedUpgrades.Count == 0)
			_upgradesList.AddChild(new Label { Text = "(none yet)" });
		foreach (var upgrade in _gameState.OwnedUpgrades)
			_upgradesList.AddChild(new Label { Text = $"{upgrade.Name} — {upgrade.Description}" });

		if (_gameState.OwnedJokers.Count == 0)
			_jokersList.AddChild(new Label { Text = "(none yet)" });
		foreach (var joker in _gameState.OwnedJokers)
			_jokersList.AddChild(new Label { Text = $"{joker.Name} — {joker.Description}" });
	}
}
