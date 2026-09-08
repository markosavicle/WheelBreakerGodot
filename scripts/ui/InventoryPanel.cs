using Godot;

public partial class InventoryPanel : Control
{
	private GameState _gameState;
	private VBoxContainer _upgradesList;
	private VBoxContainer _charmsList;

	public override void _Ready()
	{
		_gameState = GetNode<GameState>("/root/GameState");
		
		// Updated paths to include the new ScrollContainers!
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

		// Upgrades List (Non-Sellable)
		if (_gameState.OwnedUpgrades.Count == 0)
		{
			_upgradesList.AddChild(new Label { Text = "(none yet)" });
		}
		else
		{
			foreach (var upgrade in _gameState.OwnedUpgrades)
			{
				var lbl = new Label 
				{ 
					Text = $"{upgrade.Name} — {upgrade.Description}", 
					AutowrapMode = TextServer.AutowrapMode.WordSmart,
					SizeFlagsHorizontal = Control.SizeFlags.Expand | Control.SizeFlags.Fill
				};
				lbl.CustomMinimumSize = new Vector2(400, 0);
				_upgradesList.AddChild(lbl);
			}
		}

		// Charms List (Sellable)
		if (_gameState.OwnedCharms.Count == 0)
		{
			_charmsList.AddChild(new Label { Text = "(none yet)" });
		}
		else
		{
			foreach (var charm in _gameState.OwnedCharms.ToArray())
			{
				var row = new HBoxContainer();
				row.AddThemeConstantOverride("separation", 10);

				var lbl = new Label 
				{ 
					Text = $"{charm.Name} — {charm.Description}", 
					AutowrapMode = TextServer.AutowrapMode.WordSmart,
					SizeFlagsHorizontal = Control.SizeFlags.Expand | Control.SizeFlags.Fill
				};
				lbl.CustomMinimumSize = new Vector2(300, 0);
				row.AddChild(lbl);

				int sellPrice = Mathf.Max(10, charm.BaseCost / 2);
				var sellBtn = new Button { Text = $"Sell (${sellPrice})" };
				sellBtn.Pressed += () =>
				{
					_gameState.Cash += sellPrice;
					_gameState.OwnedCharms.Remove(charm);
					GD.Print($"[Inventory] Sold charm {charm.Name} for ${sellPrice}.");
					GetNode<EventBus>("/root/EventBus").EmitSignal(EventBus.SignalName.ShopUpdated);
					RefreshLists();
				};
				row.AddChild(sellBtn);

				_charmsList.AddChild(row);
			}
		}
	}
}
