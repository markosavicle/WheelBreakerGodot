using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class ShopManager : Control
{
	private GameState _gameState;
	private VBoxContainer _shopItemsContainer;
	private Button _rerollButton;

	public int OffersPerVisit = 3;
	public int BaseRerollCost = 5;
	public int RerollCostStep = 5;

	private int _rerollCount = 0;
	private List<IShopOffer> _currentOffers = new List<IShopOffer>();
	private RandomNumberGenerator _rng = new RandomNumberGenerator();

	private int CurrentRerollCost => Mathf.Max(1, BaseRerollCost + (_rerollCount * RerollCostStep) - _gameState.RerollCostDiscount);

	public override void _Ready()
	{
		_gameState = GetNode<GameState>("/root/GameState");
		_shopItemsContainer = GetNode<VBoxContainer>("ShopItems");
		_rng.Randomize();

		_rerollButton = GetNodeOrNull<Button>("RerollButton");
		if (_rerollButton != null) _rerollButton.Pressed += OnRerollPressed;
	}

	public void OpenShop()
	{
		_rerollCount = 0;
		RollNewOffers();
	}

	private List<IShopOffer> BuildAvailablePool()
	{
		var ownedCharmIds = _gameState.OwnedCharms.Select(j => j.Id).ToHashSet();
		bool charmSlotsFull = _gameState.OwnedCharms.Count >= _gameState.MaxCharmSlots;

		var pool = new List<IShopOffer>();
		pool.AddRange(UpgradePool.All);

		if (!charmSlotsFull)
			pool.AddRange(CharmPool.All.Where(j => !ownedCharmIds.Contains(j.Id)));

		return pool;
	}

	private void RollNewOffers()
	{
		_currentOffers = BuildAvailablePool()
			.OrderBy(_ => _rng.Randi())
			.Take(OffersPerVisit)
			.ToList();

		RefreshShopUI();
		GD.Print($"[Shop] Rolled {_currentOffers.Count} offers: {string.Join(", ", _currentOffers.Select(o => o.Name))}");
	}

	private void OnRerollPressed()
	{
		int cost = CurrentRerollCost;
		if (_gameState.Cash < cost)
		{
			GD.Print($"[Shop] Not enough cash to reroll. Cost: ${cost}, Have: ${_gameState.Cash}");
			return;
		}

		_gameState.Cash -= cost;
		_rerollCount++;
		RollNewOffers();
		GetNode<EventBus>("/root/EventBus").EmitSignal(EventBus.SignalName.ShopUpdated);
	}

	private void OnBuyPressed(IShopOffer offer, Button btn)
	{
		int cost = offer is UpgradeDefinition
			? _gameState.GetModifiedUpgradeCost(offer.BaseCost)
			: offer.BaseCost;

		if (_gameState.Cash < cost)
		{
			GD.Print($"[Shop] Not enough cash for {offer.Name}. Cost: ${cost}, Have: ${_gameState.Cash}");
			return;
		}

		if (offer is CharmDefinition && _gameState.OwnedCharms.Count >= _gameState.MaxCharmSlots)
		{
			GD.Print("[Shop] Charm slots full — cannot purchase.");
			return;
		}

		_gameState.Cash -= cost;

		switch (offer)
		{
			case UpgradeDefinition upgrade:
				upgrade.Apply(_gameState);
				_gameState.OwnedUpgrades.Add(upgrade);
				break;
			case CharmDefinition charmDef:
				_gameState.OwnedCharms.Add(charmDef);
				break;
		}

		GD.Print($"[Shop] Purchased {offer.Name} for ${cost}. Remaining cash: ${_gameState.Cash}");
		_currentOffers.Remove(offer);
		btn.QueueFree();
		GetNode<EventBus>("/root/EventBus").EmitSignal(EventBus.SignalName.ShopUpdated);
	}

	private void RefreshShopUI()
	{
		foreach (Node child in _shopItemsContainer.GetChildren()) child.QueueFree();

		foreach (var offer in _currentOffers)
		{
			int displayCost = offer is UpgradeDefinition
				? _gameState.GetModifiedUpgradeCost(offer.BaseCost)
				: offer.BaseCost;
			string tag = offer is CharmDefinition ? "[CHARM] " : "";

			var btn = new Button();
			btn.Text = $"{tag}{offer.Name}\n{offer.Description}\n${displayCost}";
			btn.CustomMinimumSize = new Vector2(0, 60);
			btn.Pressed += () => OnBuyPressed(offer, btn);
			_shopItemsContainer.AddChild(btn);
		}

		if (_rerollButton != null) _rerollButton.Text = $"Reroll (${CurrentRerollCost})";
	}
}
