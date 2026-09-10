using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class ShopManager : Panel
{
	private GameState _gameState;
	private VBoxContainer _shopItemsContainer;
	private Label _shopCashLabel;
	private Button _rerollButton;
	private Button _nextRoundButton;

	private List<IShopOffer> _currentShopStock = new List<IShopOffer>();
	private RandomNumberGenerator _rng = new RandomNumberGenerator();
	private int _currentRerollCost = 5;

	public override void _Ready()
	{
		_gameState = GetNode<GameState>("/root/GameState");
		_shopCashLabel = GetNodeOrNull<Label>("ShopCashLabel");
		_rerollButton = GetNodeOrNull<Button>("RerollButton");
		_nextRoundButton = GetNodeOrNull<Button>("NextRoundButton");

		MouseFilter = Control.MouseFilterEnum.Stop;
		_shopItemsContainer = GetNodeOrNull<VBoxContainer>("ShopItemsScroll/ShopItems") ?? GetNodeOrNull<VBoxContainer>("ShopItems");
		
		if (_shopItemsContainer != null)
		{
			_shopItemsContainer.CustomMinimumSize = new Vector2(920, 340);
			_shopItemsContainer.Position = new Vector2(180, 130);
		}
		if (_rerollButton != null)
		{
			_rerollButton.Pressed += OnRerollPressed;
			_rerollButton.CustomMinimumSize = new Vector2(200, 40);
			_rerollButton.Position = new Vector2(540, 550);
		}
		if (_nextRoundButton != null)
		{
			_nextRoundButton.CustomMinimumSize = new Vector2(200, 40);
			_nextRoundButton.Position = new Vector2(540, 600);
		}

		_rng.Randomize();
	}

	public void OpenShop()
	{
		int baseReroll = Mathf.Max(1, 5 - _gameState.RerollCostDiscount);
		_currentRerollCost = baseReroll;

		RefreshShopStock();
		RefreshShopUI();
	}

	private void RefreshShopStock()
	{
		_currentShopStock.Clear();

		var availableUpgrades = UpgradePool.All.ToList();
		for (int i = 0; i < 2 && availableUpgrades.Count > 0; i++)
		{
			int idx = _rng.RandiRange(0, availableUpgrades.Count - 1);
			_currentShopStock.Add(availableUpgrades[idx]);
			availableUpgrades.RemoveAt(idx);
		}

		var availableCharms = CharmPool.All
			.Where(c => !_gameState.OwnedCharms.Any(owned => owned.Id == c.Id))
			.ToList();

		for (int i = 0; i < 2 && availableCharms.Count > 0; i++)
		{
			int idx = _rng.RandiRange(0, availableCharms.Count - 1);
			_currentShopStock.Add(availableCharms[idx]);
			availableCharms.RemoveAt(idx);
		}

		if (ConsumablePool.All.Count > 0)
		{
			int idx = _rng.RandiRange(0, ConsumablePool.All.Count - 1);
			_currentShopStock.Add(ConsumablePool.All[idx]);
		}
	}

	public void RefreshShopUI()
	{
		if (_shopCashLabel != null) _shopCashLabel.Text = $"Available Cash: ${_gameState.Cash}";
		if (_rerollButton != null) _rerollButton.Text = $"Reroll Shop (${_currentRerollCost})";
		if (_shopItemsContainer == null) return;

		foreach (Node child in _shopItemsContainer.GetChildren()) child.QueueFree();

		var splitRoot = new HBoxContainer();
		splitRoot.AddThemeConstantOverride("separation", 20);
		splitRoot.SizeFlagsHorizontal = Control.SizeFlags.Expand | Control.SizeFlags.Fill;
		splitRoot.SizeFlagsVertical = Control.SizeFlags.Expand | Control.SizeFlags.Fill;

		// --- COLUMN 1: SHOP STOCK FOR SALE ---
		var leftCol = new VBoxContainer();
		leftCol.AddThemeConstantOverride("separation", 3);
		leftCol.SizeFlagsHorizontal = Control.SizeFlags.Expand | Control.SizeFlags.Fill;
		leftCol.AddChild(new Label { Text = "=== FOR SALE ===", HorizontalAlignment = HorizontalAlignment.Center });

		foreach (var offer in _currentShopStock)
		{
			int modifiedCost = offer is CharmDefinition charmDef ? _gameState.GetModifiedUpgradeCost(charmDef.BaseCost) : offer.BaseCost;

			string prefix = "[Upgrade]";
			if (offer is CharmDefinition) prefix = "[Charm]";
			else if (offer is ConsumableDefinition) prefix = "[Consumable]";
			else if (offer.Name.Contains("Tip Sheet")) prefix = "[Tip Sheet]";

			var itemBtn = new Button();
			itemBtn.Text = $"{prefix} {offer.Name} (${modifiedCost}) — {offer.Description}";
			itemBtn.AutowrapMode = TextServer.AutowrapMode.WordSmart;
			itemBtn.CustomMinimumSize = new Vector2(430, 44); // Compact height
			itemBtn.SizeFlagsHorizontal = Control.SizeFlags.Expand | Control.SizeFlags.Fill;
			itemBtn.MouseFilter = Control.MouseFilterEnum.Stop;

			itemBtn.Pressed += () =>
			{
				if (_gameState.Cash >= modifiedCost)
				{
					if (offer is UpgradeDefinition upgradeDef)
					{
						_gameState.Cash -= modifiedCost;
						_gameState.AddUpgrade(upgradeDef);
						_currentShopStock.Remove(offer);
						RefreshShopUI();
						GetNode<EventBus>("/root/EventBus").EmitSignal(EventBus.SignalName.ShopUpdated);
					}
					else if (offer is CharmDefinition charmDefToAdd)
					{
						if (_gameState.OwnedCharms.Count < _gameState.MaxCharmSlots)
						{
							_gameState.Cash -= modifiedCost;
							_gameState.OwnedCharms.Add(charmDefToAdd);
							_currentShopStock.Remove(offer);
							RefreshShopUI();
							GetNode<EventBus>("/root/EventBus").EmitSignal(EventBus.SignalName.ShopUpdated);
						}
					}
					else if (offer is ConsumableDefinition consumableDef)
					{
						if (_gameState.HeldConsumables.Count < _gameState.MaxConsumableSlots)
						{
							_gameState.Cash -= modifiedCost;
							_gameState.HeldConsumables.Add(consumableDef);
							_currentShopStock.Remove(offer);
							RefreshShopUI();
							GetNode<EventBus>("/root/EventBus").EmitSignal(EventBus.SignalName.ShopUpdated);
						}
					}
				}
			};
			leftCol.AddChild(itemBtn);
		}
		splitRoot.AddChild(leftCol);

		// --- COLUMN 2: OWNED ITEMS (Charms & Consumables) ---
		var rightCol = new VBoxContainer();
		rightCol.AddThemeConstantOverride("separation", 3);
		rightCol.SizeFlagsHorizontal = Control.SizeFlags.Expand | Control.SizeFlags.Fill;
		rightCol.AddChild(new Label { Text = "=== YOUR INVENTORY ===", HorizontalAlignment = HorizontalAlignment.Center });

		foreach (var charm in _gameState.OwnedCharms.ToArray())
		{
			int sellPrice = Mathf.Max(10, charm.BaseCost / 2);
			var charmBtn = new Button();
			charmBtn.Text = $"[Charm] {charm.Name} (Sell: ${sellPrice}) — {charm.Description}";
			charmBtn.AutowrapMode = TextServer.AutowrapMode.WordSmart;
			charmBtn.CustomMinimumSize = new Vector2(430, 44);
			charmBtn.SizeFlagsHorizontal = Control.SizeFlags.Expand | Control.SizeFlags.Fill;
			charmBtn.MouseFilter = Control.MouseFilterEnum.Stop;

			charmBtn.Pressed += () =>
			{
				_gameState.Cash += sellPrice;
				_gameState.OwnedCharms.Remove(charm);
				RefreshShopUI();
				GetNode<EventBus>("/root/EventBus").EmitSignal(EventBus.SignalName.ShopUpdated);
			};
			rightCol.AddChild(charmBtn);
		}

		foreach (var consumable in _gameState.HeldConsumables.ToArray())
		{
			int sellPrice = Mathf.Max(5, consumable.BaseCost / 2);
			var consBtn = new Button();
			consBtn.Text = $"[Consumable] {consumable.Name} (Sell: ${sellPrice}) — {consumable.Description}";
			consBtn.AutowrapMode = TextServer.AutowrapMode.WordSmart;
			consBtn.CustomMinimumSize = new Vector2(430, 44);
			consBtn.SizeFlagsHorizontal = Control.SizeFlags.Expand | Control.SizeFlags.Fill;
			consBtn.MouseFilter = Control.MouseFilterEnum.Stop;

			consBtn.Pressed += () =>
			{
				_gameState.Cash += sellPrice;
				_gameState.HeldConsumables.Remove(consumable);
				RefreshShopUI();
				GetNode<EventBus>("/root/EventBus").EmitSignal(EventBus.SignalName.ShopUpdated);
			};
			rightCol.AddChild(consBtn);
		}

		splitRoot.AddChild(rightCol);
		_shopItemsContainer.AddChild(splitRoot);
	}

	private void OnRerollPressed()
	{
		if (_gameState.Cash >= _currentRerollCost)
		{
			_gameState.Cash -= _currentRerollCost;
			_currentRerollCost += 2;
			RefreshShopStock();
			RefreshShopUI();
		}
	}
}
