using Godot;

public partial class InventoryPanel : Control
{
	private GameState _gameState;
	private VBoxContainer _upgradesList;
	private VBoxContainer _charmsList;

	public override void _Ready()
	{
		_gameState = GetNode<GameState>("/root/GameState");
		
		_upgradesList = GetNode<VBoxContainer>("CenterContainer/PanelBackground/UpgradesScroll/UpgradesList");
		_charmsList = GetNode<VBoxContainer>("CenterContainer/PanelBackground/CharmsScroll/CharmsList");

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

		// Upgrades List (Stacked Map display)
		if (_gameState.OwnedUpgradesMap.Count == 0)
		{
			_upgradesList.AddChild(new Label { Text = "(none yet)" });
		}
		else
		{
			foreach (var kvp in _gameState.OwnedUpgradesMap)
			{
				string upgradeId = kvp.Key;
				int level = kvp.Value;

				if (_gameState.UpgradeDefinitionsMap.TryGetValue(upgradeId, out var def))
				{
					var lbl = new Label 
					{ 
						Text = $"{def.Name} (Lvl {level}) — {def.Description}", 
						AutowrapMode = TextServer.AutowrapMode.WordSmart,
						SizeFlagsHorizontal = Control.SizeFlags.Expand | Control.SizeFlags.Fill
					};
					lbl.CustomMinimumSize = new Vector2(400, 0);
					_upgradesList.AddChild(lbl);
				}
			}
		}

		// Charms List (Single Unified Clickable Sell Button per Charm)
		if (_gameState.OwnedCharms.Count == 0)
		{
			_charmsList.AddChild(new Label { Text = "(none yet)" });
		}
		else
		{
			foreach (var charm in _gameState.OwnedCharms.ToArray())
			{
				int sellPrice = Mathf.Max(10, charm.BaseCost / 2);

				var charmBtn = new Button();
				charmBtn.Text = $"{charm.Name}\n{charm.Description}\n[Click to Sell for ${sellPrice}]";
				charmBtn.AutowrapMode = TextServer.AutowrapMode.WordSmart;
				charmBtn.CustomMinimumSize = new Vector2(380, 75);
				charmBtn.SizeFlagsHorizontal = Control.SizeFlags.Expand | Control.SizeFlags.Fill;

				charmBtn.Pressed += () =>
				{
					_gameState.Cash += sellPrice;
					_gameState.OwnedCharms.Remove(charm);
					GD.Print($"[Inventory] Sold charm {charm.Name} for ${sellPrice}.");
					GetNode<EventBus>("/root/EventBus").EmitSignal(EventBus.SignalName.ShopUpdated);
					RefreshLists();
				};

				_charmsList.AddChild(charmBtn);
			}
		}
	}
}
