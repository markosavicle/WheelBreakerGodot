using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class BettingTable : Control
{
	private GameState _gameState;
	[Export] public int DefaultChipsPerClick = 5;

	private List<Bet> _lastRoundBets = new List<Bet>();

	private Dictionary<Button, Bet> _buttonBets = new Dictionary<Button, Bet>();
	private Dictionary<Button, BetType> _buttonBetType = new Dictionary<Button, BetType>();
	private Dictionary<Button, string> _buttonBaseLabel = new Dictionary<Button, string>();

	// Hold-to-add/remove state variables
	private Button _heldButton = null;
	private bool _isHoldingAdd = false;
	private float _holdTimer = 0f;
	private float _repeatInterval = 0.35f;

	public override void _Ready()
	{
		_gameState = GetNode<GameState>("/root/GameState");
		BuildNumberGrid();
		ConnectOutsideBetButtons();

		var eventBus = GetNode<EventBus>("/root/EventBus");
		eventBus.Connect(EventBus.SignalName.SpinResolved, new Callable(this, nameof(OnSpinResolved)));
		eventBus.Connect(EventBus.SignalName.RoundStarted, new Callable(this, nameof(OnRoundStarted)));

		ApplyBossRestrictions();
		RefreshTooltips();
	}

	public override void _Process(double delta)
	{
		if (_heldButton == null) return;

		_holdTimer += (float)delta;
		if (_holdTimer >= _repeatInterval)
		{
			_holdTimer = 0f;
			// Rapid acceleration for high chip counts (caps at 0.02s per tick)
			_repeatInterval = Mathf.Max(0.02f, _repeatInterval * 0.5f);

			BetType type = _buttonBetType[_heldButton];
			bool isStraight = type == BetType.Straight;
			int numVal = isStraight && int.TryParse(_buttonBaseLabel[_heldButton], out int n) ? n : -1;
			Color baseColor = GetOutsideButtonColor(_buttonBaseLabel[_heldButton]);

			if (_isHoldingAdd)
			{
				List<int> numbers = isStraight ? new List<int> { numVal } : null;
				PlaceBet(type, numbers, _heldButton, numVal, isStraight, baseColor);
			}
			else
			{
				RemoveBetForButton(_heldButton, numVal, isStraight, baseColor);
			}
		}
	}

	private void RegisterHoldEvents(Button btn, System.Action onInitialAdd, System.Action onInitialRemove)
	{
		btn.GuiInput += (inputEvent) =>
		{
			if (inputEvent is InputEventMouseButton mouseEvent)
			{
				if (mouseEvent.Pressed)
				{
					_heldButton = btn;
					_holdTimer = 0f;
					_repeatInterval = 0.35f;

					if (mouseEvent.ButtonIndex == MouseButton.Left)
					{
						_isHoldingAdd = true;
						onInitialAdd();
					}
					else if (mouseEvent.ButtonIndex == MouseButton.Right)
					{
						_isHoldingAdd = false;
						onInitialRemove();
					}
				}
				else
				{
					_heldButton = null;
				}
			}
		};
	}

	private void BuildNumberGrid()
	{
		var grid = GetNode<GridContainer>("TableLayout/NumberAndZeroBox/NumberButtons");
		if (grid == null) { GD.PrintErr("[BettingTable] ERROR: NumberButtons not found!"); return; }

		foreach (Node child in grid.GetChildren()) child.QueueFree();
		_buttonBets.Clear();
		_buttonBetType.Clear();
		_buttonBaseLabel.Clear();

		grid.Columns = 12;

		Button CreateNumButton(int number, int w = 45, int h = 45)
		{
			var btn = new Button();
			string label = number.ToString();
			btn.Text = label;
			btn.CustomMinimumSize = new Vector2(w, h);
			StyleButtonBaseColor(btn, number);

			_buttonBets[btn] = null;
			_buttonBetType[btn] = BetType.Straight;
			_buttonBaseLabel[btn] = label;

			RegisterHoldEvents(btn, 
				() => PlaceBet(BetType.Straight, new List<int> { number }, btn, number, true),
				() => RemoveBetForButton(btn, number, true));

			return btn;
		}

		int[] row3 = { 3, 6, 9, 12, 15, 18, 21, 24, 27, 30, 33, 36 };
		int[] row2 = { 2, 5, 8, 11, 14, 17, 20, 23, 26, 29, 32, 35 };
		int[] row1 = { 1, 4, 7, 10, 13, 16, 19, 22, 25, 28, 31, 34 };

		foreach (int n in row3) grid.AddChild(CreateNumButton(n));
		foreach (int n in row2) grid.AddChild(CreateNumButton(n));
		foreach (int n in row1) grid.AddChild(CreateNumButton(n));

		Button btnZero = GetNodeOrNull<Button>("TableLayout/NumberAndZeroBox/ZeroButton");
		if (btnZero != null)
		{
			btnZero.Text = "0";
			btnZero.CustomMinimumSize = new Vector2(45, 141);
			StyleButtonBaseColor(btnZero, 0);

			_buttonBets[btnZero] = null;
			_buttonBetType[btnZero] = BetType.Straight;
			_buttonBaseLabel[btnZero] = "0";

			RegisterHoldEvents(btnZero,
				() => PlaceBet(BetType.Straight, new List<int> { 0 }, btnZero, 0, true),
				() => RemoveBetForButton(btnZero, 0, true));
		}
	}

	private void StyleButtonBaseColor(Button btn, int number)
	{
		var styleBox = new StyleBoxFlat();
		styleBox.CornerRadiusTopLeft = styleBox.CornerRadiusTopRight = styleBox.CornerRadiusBottomLeft = styleBox.CornerRadiusBottomRight = 4;
		if (number == 0) styleBox.BgColor = new Color(0.1f, 0.6f, 0.2f);
		else if (WheelData.IsRed(number)) styleBox.BgColor = new Color(0.8f, 0.15f, 0.15f);
		else styleBox.BgColor = new Color(0.18f, 0.18f, 0.18f);

		btn.AddThemeStyleboxOverride("normal", styleBox);
		var hoverStyle = (StyleBoxFlat)styleBox.Duplicate();
		hoverStyle.BgColor = hoverStyle.BgColor.Lightened(0.2f);
		btn.AddThemeStyleboxOverride("hover", hoverStyle);
	}

	private void UpdateButtonVisualState(Button btn, int number)
	{
		var styleBox = new StyleBoxFlat();
		styleBox.CornerRadiusTopLeft = styleBox.CornerRadiusTopRight = styleBox.CornerRadiusBottomLeft = styleBox.CornerRadiusBottomRight = 4;
		if (number == 0) styleBox.BgColor = new Color(0.1f, 0.6f, 0.2f);
		else if (WheelData.IsRed(number)) styleBox.BgColor = new Color(0.8f, 0.15f, 0.15f);
		else styleBox.BgColor = new Color(0.18f, 0.18f, 0.18f);

		var hoverStyle = (StyleBoxFlat)styleBox.Duplicate();
		hoverStyle.BgColor = hoverStyle.BgColor.Lightened(0.2f);

		bool hasBet = _buttonBets.TryGetValue(btn, out var bet) && bet != null;
		if (hasBet)
		{
			styleBox.BorderWidthTop = styleBox.BorderWidthBottom = styleBox.BorderWidthLeft = styleBox.BorderWidthRight = 3;
			styleBox.BorderColor = new Color(1f, 0.84f, 0f);
			hoverStyle.BorderWidthTop = hoverStyle.BorderWidthBottom = hoverStyle.BorderWidthLeft = hoverStyle.BorderWidthRight = 3;
			hoverStyle.BorderColor = new Color(1f, 0.84f, 0f);
		}

		btn.AddThemeStyleboxOverride("normal", styleBox);
		btn.AddThemeStyleboxOverride("hover", hoverStyle);
		btn.Text = hasBet ? $"{_buttonBaseLabel[btn]}\n${bet.ChipsWagered}" : _buttonBaseLabel[btn];
	}

	private void ConnectOutsideBetButtons()
	{
		SetupOutsideButton("TableLayout/DozensRow/Dozen1Button", BetType.Dozen1, null, "1st 12", new Color(0.2f, 0.4f, 0.4f), 180);
		SetupOutsideButton("TableLayout/DozensRow/Dozen2Button", BetType.Dozen2, null, "2nd 12", new Color(0.2f, 0.4f, 0.4f), 180);
		SetupOutsideButton("TableLayout/DozensRow/Dozen3Button", BetType.Dozen3, null, "3rd 12", new Color(0.2f, 0.4f, 0.4f), 180);
		SetupOutsideButton("TableLayout/EvenOddRedBlackRow/LowButton", BetType.Low, null, "1-18", new Color(0.25f, 0.35f, 0.45f), 90);
		SetupOutsideButton("TableLayout/EvenOddRedBlackRow/EvenButton", BetType.Even, null, "EVEN", new Color(0.2f, 0.3f, 0.5f), 90);
		SetupOutsideButton("TableLayout/EvenOddRedBlackRow/RedButton", BetType.Red, null, "RED", new Color(0.8f, 0.15f, 0.15f), 90);
		SetupOutsideButton("TableLayout/EvenOddRedBlackRow/BlackButton", BetType.Black, null, "BLACK", new Color(0.18f, 0.18f, 0.18f), 90);
		SetupOutsideButton("TableLayout/EvenOddRedBlackRow/OddButton", BetType.Odd, null, "ODD", new Color(0.2f, 0.3f, 0.5f), 90);
		SetupOutsideButton("TableLayout/EvenOddRedBlackRow/HighButton", BetType.High, null, "19-36", new Color(0.25f, 0.35f, 0.45f), 90);
	}

	private void SetupOutsideButton(string path, BetType type, List<int> numbers, string labelText, Color baseColor, int customWidth)
	{
		var btn = GetNodeOrNull<Button>(path);
		if (btn == null) { GD.PrintErr($"[BettingTable] ERROR: '{path}' not found!"); return; }

		btn.Text = labelText;
		btn.CustomMinimumSize = new Vector2(customWidth, 35);
		btn.SizeFlagsHorizontal = Control.SizeFlags.Expand | Control.SizeFlags.Fill;
		StyleOutsideButtonBaseColor(btn, baseColor);

		_buttonBets[btn] = null;
		_buttonBetType[btn] = type;
		_buttonBaseLabel[btn] = labelText;

		RegisterHoldEvents(btn,
			() => PlaceBet(type, numbers, btn, -1, false, baseColor),
			() => RemoveBetForButton(btn, -1, false, baseColor));
	}

	private void StyleOutsideButtonBaseColor(Button btn, Color baseColor)
	{
		var styleBox = new StyleBoxFlat();
		styleBox.CornerRadiusTopLeft = styleBox.CornerRadiusTopRight = styleBox.CornerRadiusBottomLeft = styleBox.CornerRadiusBottomRight = 4;
		styleBox.BgColor = baseColor;
		btn.AddThemeStyleboxOverride("normal", styleBox);

		var hoverStyle = (StyleBoxFlat)styleBox.Duplicate();
		hoverStyle.BgColor = hoverStyle.BgColor.Lightened(0.2f);
		btn.AddThemeStyleboxOverride("hover", hoverStyle);
	}

	private void UpdateOutsideButtonVisualState(Button btn, Color baseColor)
	{
		var styleBox = new StyleBoxFlat();
		styleBox.CornerRadiusTopLeft = styleBox.CornerRadiusTopRight = styleBox.CornerRadiusBottomLeft = styleBox.CornerRadiusBottomRight = 4;
		styleBox.BgColor = baseColor;

		var hoverStyle = (StyleBoxFlat)styleBox.Duplicate();
		hoverStyle.BgColor = hoverStyle.BgColor.Lightened(0.2f);

		bool hasBet = _buttonBets.TryGetValue(btn, out var bet) && bet != null;
		if (hasBet)
		{
			styleBox.BorderWidthTop = styleBox.BorderWidthBottom = styleBox.BorderWidthLeft = styleBox.BorderWidthRight = 3;
			styleBox.BorderColor = new Color(1f, 0.84f, 0f);
			hoverStyle.BorderWidthTop = hoverStyle.BorderWidthBottom = hoverStyle.BorderWidthLeft = hoverStyle.BorderWidthRight = 3;
			hoverStyle.BorderColor = new Color(1f, 0.84f, 0f);
		}

		btn.AddThemeStyleboxOverride("normal", styleBox);
		btn.AddThemeStyleboxOverride("hover", hoverStyle);
		btn.Text = hasBet ? $"{_buttonBaseLabel[btn]}\n${bet.ChipsWagered}" : _buttonBaseLabel[btn];
	}

	private bool IsBetTypeBlockedByBoss(BetType type)
	{
		var boss = _gameState.ActiveBoss;
		return boss?.IsBetTypeBlocked != null && boss.IsBetTypeBlocked(type);
	}

	private void PlaceBet(BetType type, List<int> numbers, Button btn, int numberVal, bool isStraight, Color outsideBaseColor = default)
	{
		if (IsBetTypeBlockedByBoss(type)) return;
		if (_gameState.ChipsRemainingThisSpin < DefaultChipsPerClick) return;

		var boss = _gameState.ActiveBoss;
		if (boss?.MaxDistinctBetButtons != null)
		{
			int max = boss.MaxDistinctBetButtons();
			bool isNewSpot = !_buttonBets.TryGetValue(btn, out var existingCheck) || existingCheck == null;
			int distinctCount = _buttonBets.Count(kvp => kvp.Value != null);
			if (isNewSpot && distinctCount >= max) return;
		}

		if (_buttonBets.TryGetValue(btn, out Bet existingBet) && existingBet != null)
		{
			existingBet.ChipsWagered += DefaultChipsPerClick;
		}
		else
		{
			float payout = WheelData.GetBasePayout(type);
			var bet = new Bet(type, numbers, DefaultChipsPerClick, payout);
			_gameState.ActiveBets.Add(bet);
			_buttonBets[btn] = bet;
		}

		_gameState.ChipsRemainingThisSpin -= DefaultChipsPerClick;

		if (isStraight) UpdateButtonVisualState(btn, numberVal);
		else UpdateOutsideButtonVisualState(btn, outsideBaseColor);

		_gameState.Stats.TotalBetsPlaced++;
		GetNode<EventBus>("/root/EventBus").EmitSignal(EventBus.SignalName.BetPlaced);
	}

	private void RemoveBetForButton(Button btn, int numberVal, bool isStraight, Color outsideBaseColor = default)
	{
		if (!_buttonBets.TryGetValue(btn, out Bet bet) || bet == null) return;

		int refund = Mathf.Min(DefaultChipsPerClick, bet.ChipsWagered);
		bet.ChipsWagered -= refund;
		_gameState.ChipsRemainingThisSpin += refund;

		if (bet.ChipsWagered <= 0)
		{
			_gameState.ActiveBets.Remove(bet);
			_buttonBets[btn] = null;
		}

		if (isStraight) UpdateButtonVisualState(btn, numberVal);
		else UpdateOutsideButtonVisualState(btn, outsideBaseColor);

		GetNode<EventBus>("/root/EventBus").EmitSignal(EventBus.SignalName.BetPlaced);
	}

	private void OnSpinResolved(int winningNumber, int scoreGained)
	{
		_heldButton = null;
		foreach (var btn in _buttonBets.Keys.ToList())
		{
			_buttonBets[btn] = null;
			RestoreButtonBaseVisual(btn);
		}
	}

	private void RestoreButtonBaseVisual(Button btn)
	{
		btn.Text = _buttonBaseLabel[btn];

		if (int.TryParse(_buttonBaseLabel[btn], out int num))
			StyleButtonBaseColor(btn, num);
		else
			StyleOutsideButtonBaseColor(btn, GetOutsideButtonColor(_buttonBaseLabel[btn]));
	}

	private void OnRoundStarted() => ApplyBossRestrictions();

	private void ApplyBossRestrictions()
	{
		var boss = _gameState.ActiveBoss;
		foreach (var kvp in _buttonBetType)
		{
			Button btn = kvp.Key;
			bool blocked = boss?.IsBetTypeBlocked != null && boss.IsBetTypeBlocked(kvp.Value);
			btn.Disabled = blocked;
			btn.Modulate = blocked ? new Color(1, 1, 1, 0.35f) : Colors.White;
		}
		
		RefreshTooltips();
	}

	public void RepeatLastBets()
	{
		foreach (var bet in _gameState.ActiveBets)
			_gameState.ChipsRemainingThisSpin += bet.ChipsWagered;

		foreach (var btn in _buttonBets.Keys.ToList())
		{
			_buttonBets[btn] = null;
			RestoreButtonBaseVisual(btn);
		}
		_gameState.ActiveBets.Clear();

		foreach (var bet in _lastRoundBets)
		{
			if (IsBetTypeBlockedByBoss(bet.Type)) continue;
			if (_gameState.ChipsRemainingThisSpin < bet.ChipsWagered) break;

			var newBet = new Bet(bet.Type, bet.Numbers, bet.ChipsWagered, bet.Payout);
			_gameState.ActiveBets.Add(newBet);
			_gameState.ChipsRemainingThisSpin -= bet.ChipsWagered;
			_gameState.Stats.TotalBetsPlaced++;

			foreach (var btn in _buttonBetType.Keys)
			{
				bool matches = bet.Type == BetType.Straight
					? (bet.Numbers != null && bet.Numbers.Count > 0 && _buttonBaseLabel[btn] == bet.Numbers[0].ToString())
					: DoesButtonMatchOutsideType(btn, bet.Type);

				if (matches)
				{
					_buttonBets[btn] = newBet;
					if (bet.Type == BetType.Straight) UpdateButtonVisualState(btn, bet.Numbers[0]);
					else UpdateOutsideButtonVisualState(btn, GetOutsideButtonColor(_buttonBaseLabel[btn]));
					break;
				}
			}
		}

		GetNode<EventBus>("/root/EventBus").EmitSignal(EventBus.SignalName.BetPlaced);
	}

	private bool DoesButtonMatchOutsideType(Button btn, BetType type)
	{
		string label = _buttonBaseLabel[btn];
		return (type == BetType.Red && label == "RED") ||
			   (type == BetType.Black && label == "BLACK") ||
			   (type == BetType.Odd && label == "ODD") ||
			   (type == BetType.Even && label == "EVEN") ||
			   (type == BetType.Low && label == "1-18") ||
			   (type == BetType.High && label == "19-36") ||
			   (type == BetType.Dozen1 && label == "1st 12") ||
			   (type == BetType.Dozen2 && label == "2nd 12") ||
			   (type == BetType.Dozen3 && label == "3rd 12");
	}

	private Color GetOutsideButtonColor(string label)
	{
		return label switch
		{
			"RED" => new Color(0.8f, 0.15f, 0.15f),
			"BLACK" => new Color(0.18f, 0.18f, 0.18f),
			"ODD" or "EVEN" => new Color(0.2f, 0.3f, 0.5f),
			"1-18" or "19-36" => new Color(0.25f, 0.35f, 0.45f),
			_ => new Color(0.2f, 0.4f, 0.4f)
		};
	}

	public void CacheBetsForRepeat()
	{
		_lastRoundBets = new List<Bet>(_gameState.ActiveBets);
	}
	
	public void ClearAllBets()
	{
		foreach (var btn in _buttonBets.Keys.ToList())
		{
			if (_buttonBets[btn] != null)
			{
				_gameState.ChipsRemainingThisSpin += _buttonBets[btn].ChipsWagered;
				_buttonBets[btn] = null;
				RestoreButtonBaseVisual(btn);
			}
		}
		_gameState.ActiveBets.Clear();
		GetNode<EventBus>("/root/EventBus").EmitSignal(EventBus.SignalName.BetPlaced);
	}

	private void RefreshTooltips()
	{
		foreach (var kvp in _buttonBetType)
		{
			Button btn = kvp.Key;
			BetType type = kvp.Value;
			
			string tooltip = $"Type: {type}\nBase Payout: {WheelData.GetBasePayout(type)}x";

			if (IsBetTypeBlockedByBoss(type))
			{
				tooltip += $"\n\n[BLOCKED] by {_gameState.ActiveBoss?.Name}";
			}
			
			if (type != BetType.Straight && _gameState.OwnedCharms.Any(c => c.Id == "velvet_felt"))
				tooltip += "\n[BUFF] +15 Score (Velvet Felt)";
			if (type == BetType.Straight && _gameState.OwnedCharms.Any(c => c.Id == "loaded_dice"))
				tooltip += "\n[BUFF] 1.3x Score (Loaded Dice)";
			if (type != BetType.Straight && _gameState.OwnedCharms.Any(c => c.Id == "loaded_dice"))
				tooltip += "\n[DEBUFF] 0.8x Score (Loaded Dice)";

			btn.TooltipText = tooltip;
		}
	}
}
