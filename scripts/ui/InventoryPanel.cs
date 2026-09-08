using Godot;

public partial class InventoryPanel : Control
{
	private GameState _gameState;
	private VBoxContainer _upgradesList;
	private VBoxContainer _charmsList;

	public override void _Ready()
	{
		_gameState = GetNode<GameState>("/root/GameState");
		
		_upgradesList = GetNode<VBoxContainer>("CenterContainer/PanelBackground/UpgradesList");
		_charmsList = GetNode<VBoxContainer>("CenterContainer/PanelBackground/CharmsList");

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
		if (_upgradesList == null || _charmsList == null) return;

		foreach (Node child in _upgradesList.GetChildren()) child.QueueFree();
		foreach (Node child in _charmsList.GetChildren()) child.QueueFree();

		if (_gameState.OwnedUpgrades.Count == 0)
			_upgradesList.AddChild(new Label { Text = "(none yet)" });
		foreach (var upgrade in _gameState.OwnedUpgrades)
		{
			var lbl = new Label { Text = $"{upgrade.Name} — {upgrade.Description}", AutowrapMode = TextServer.AutowrapMode.WordSmart };
			lbl.CustomMinimumSize = new Vector2(410, 0);
			_upgradesList.AddChild(lbl);
		}

		if (_gameState.OwnedCharms.Count == 0)
			_charmsList.AddChild(new Label { Text = "(none yet)" });
		foreach (var charm in _gameState.OwnedCharms)
		{
			var lbl = new Label { Text = $"{charm.Name} — {charm.Description}", AutowrapMode = TextServer.AutowrapMode.WordSmart };
			lbl.CustomMinimumSize = new Vector2(410, 0);
			_charmsList.AddChild(lbl);
		}
	}
}
