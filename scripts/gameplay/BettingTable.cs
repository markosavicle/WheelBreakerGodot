using Godot;
using System.Collections.Generic;

public partial class BettingTable : Control
{
	private GameState _gameState;
	[Export] public int DefaultChipsPerClick = 5;

	private List<Bet> _lastRoundBets = new List<Bet>();
	private Dictionary<Button, List<Bet>> _buttonBets = new Dictionary<Button, List<Bet>>();

	public override void _Ready()
	{
		_gameState = GetNode<GameState>("/root/GameState");
		GD.Print("[BettingTable] Initializing BettingTable...");
		BuildNumberGrid();
		ConnectOutsideBetButtons();

		var eventBus = GetNode<EventBus>("/root/EventBus");
		eventBus.Connect(EventBus.SignalName.SpinResolved, new Callable(this, nameof(OnSpinResolved)));
	}

	private void BuildNumberGrid()
	{
		var grid = GetNode<GridContainer>("TableLayout/NumberAndZeroBox/NumberButtons");
		if (grid == null)
		{
			GD.PrintErr("[BettingTable] ERROR: TableLayout/NumberAndZeroBox/NumberButtons not found! Check your scene tree.");
			return;
		}
		
		foreach (Node child in grid.GetChildren())
		{
			child.QueueFree();
		}
		_buttonBets.Clear();

		grid.Columns = 12;

		Button CreateNumButton(int number, int customWidth = 45, int customHeight = 45)
		{
			var btn = new Button();
			btn.Text = number.ToString();
			btn.CustomMinimumSize = new Vector2(customWidth, customHeight);
			StyleButtonBaseColor(btn, number);
			_buttonBets[btn] = new List<Bet>();

			btn.GuiInput += (inputEvent) =>
			{
				if (inputEvent is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
				{
					if (mouseEvent.ButtonIndex == MouseButton.Left)
					{
						PlaceBet(BetType.Straight, new List<int> { number }, btn, number, true);
					}
					else if (mouseEvent.ButtonIndex == MouseButton.Right)
					{
						RemoveBetForButton(btn, BetType.Straight, new List<int> { number }, number, true);
					}
				}
			};
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
			_buttonBets[btnZero] = new List<Bet>();

			btnZero.GuiInput += (inputEvent) =>
			{
				if (inputEvent is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
				{
					if (mouseEvent.ButtonIndex == MouseButton.Left)
					{
						PlaceBet(BetType.Straight, new List<int> { 0 }, btnZero, 0, true);
					}
					else if (mouseEvent.ButtonIndex == MouseButton.Right)
					{
						RemoveBetForButton(btnZero, BetType.Straight, new List<int> { 0 }, 0, true);
					}
				}
			};
		}

		GD.Print("[BettingTable] Professional roulette layout built via updated scene hierarchy.");
	}


	private void StyleButtonBaseColor(Button btn, int number)
	{
		var styleBox = new StyleBoxFlat();
		styleBox.CornerRadiusTopLeft = styleBox.CornerRadiusTopRight = styleBox.CornerRadiusBottomLeft = styleBox.CornerRadiusBottomRight = 4;

		if (number == 0)
		{
			styleBox.BgColor = new Color(0.1f, 0.6f, 0.2f); // Casino Green for 0
		}
		else if (WheelData.IsRed(number))
		{
			styleBox.BgColor = new Color(0.8f, 0.15f, 0.15f); // Roulette Red
		}
		else
		{
			styleBox.BgColor = new Color(0.18f, 0.18f, 0.18f); // Roulette Black/Dark Gray
		}

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

		if (_buttonBets.ContainsKey(btn) && _buttonBets[btn].Count > 0)
		{
			styleBox.BorderWidthTop = styleBox.BorderWidthBottom = styleBox.BorderWidthLeft = styleBox.BorderWidthRight = 3;
			styleBox.BorderColor = new Color(1f, 0.84f, 0f);

			hoverStyle.BorderWidthTop = hoverStyle.BorderWidthBottom = hoverStyle.BorderWidthLeft = hoverStyle.BorderWidthRight = 3;
			hoverStyle.BorderColor = new Color(1f, 0.84f, 0f);
		}

		btn.AddThemeStyleboxOverride("normal", styleBox);
		btn.AddThemeStyleboxOverride("hover", hoverStyle);
	}

	private void ConnectOutsideBetButtons()
{
	// 3 Dozens spanning the top row
	SetupOutsideButton("TableLayout/DozensRow/Dozen1Button", BetType.Dozen1, null, "1st 12", new Color(0.2f, 0.4f, 0.4f), 180);
	SetupOutsideButton("TableLayout/DozensRow/Dozen2Button", BetType.Dozen2, null, "2nd 12", new Color(0.2f, 0.4f, 0.4f), 180);
	SetupOutsideButton("TableLayout/DozensRow/Dozen3Button", BetType.Dozen3, null, "3rd 12", new Color(0.2f, 0.4f, 0.4f), 180);
	
	// Bottom row: Changed 1-18 and 19-36 to a clear slate/steel blue so they don't blend with the background
	SetupOutsideButton("TableLayout/EvenOddRedBlackRow/LowButton", BetType.Low, null, "1-18", new Color(0.25f, 0.35f, 0.45f), 90);
	SetupOutsideButton("TableLayout/EvenOddRedBlackRow/EvenButton", BetType.Even, null, "EVEN", new Color(0.2f, 0.3f, 0.5f), 90);
	SetupOutsideButton("TableLayout/EvenOddRedBlackRow/RedButton", BetType.Red, null, "RED", new Color(0.8f, 0.15f, 0.15f), 90);
	SetupOutsideButton("TableLayout/EvenOddRedBlackRow/BlackButton", BetType.Black, null, "BLACK", new Color(0.18f, 0.18f, 0.18f), 90);
	SetupOutsideButton("TableLayout/EvenOddRedBlackRow/OddButton", BetType.Odd, null, "ODD", new Color(0.2f, 0.3f, 0.5f), 90);
	SetupOutsideButton("TableLayout/EvenOddRedBlackRow/HighButton", BetType.High, null, "19-36", new Color(0.25f, 0.35f, 0.45f), 90);
	
	GD.Print("[BettingTable] Outside bet buttons updated with high-contrast colors.");
}

private void SetupOutsideButton(string path, BetType type, List<int> numbers, string labelText, Color baseColor, int customWidth)
{
	var btn = GetNodeOrNull<Button>(path);
	if (btn == null)
	{
		GD.PrintErr($"[BettingTable] ERROR: Outside button at '{path}' not found! Check your scene tree.");
		return;
	}

	btn.Text = labelText;
	btn.CustomMinimumSize = new Vector2(customWidth, 35);
	
	// Force the button to expand horizontally to fill container distribution evenly
	btn.SizeFlagsHorizontal = Control.SizeFlags.Expand | Control.SizeFlags.Fill;
	
	StyleOutsideButtonBaseColor(btn, baseColor);

	_buttonBets[btn] = new List<Bet>();

	btn.GuiInput += (inputEvent) =>
	{
		if (inputEvent is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
		{
			if (mouseEvent.ButtonIndex == MouseButton.Left)
			{
				PlaceBet(type, numbers, btn, -1, false, baseColor);
			}
			else if (mouseEvent.ButtonIndex == MouseButton.Right)
			{
				RemoveBetForButton(btn, type, numbers, -1, false, baseColor);
			}
		}
	};
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

		if (_buttonBets.ContainsKey(btn) && _buttonBets[btn].Count > 0)
		{
			styleBox.BorderWidthTop = styleBox.BorderWidthBottom = styleBox.BorderWidthLeft = styleBox.BorderWidthRight = 3;
			styleBox.BorderColor = new Color(1f, 0.84f, 0f);

			hoverStyle.BorderWidthTop = hoverStyle.BorderWidthBottom = hoverStyle.BorderWidthLeft = hoverStyle.BorderWidthRight = 3;
			hoverStyle.BorderColor = new Color(1f, 0.84f, 0f);
		}

		btn.AddThemeStyleboxOverride("normal", styleBox);
		btn.AddThemeStyleboxOverride("hover", hoverStyle);
	}

	private void PlaceBet(BetType type, List<int> numbers, Button btn, int numberVal, bool isStraight, Color outsideBaseColor = default)
	{
		if (_gameState.ChipsRemainingThisSpin < DefaultChipsPerClick)
		{
			string targetDesc = numbers != null ? string.Join(",", numbers) : type.ToString();
			GD.Print($"[BettingTable] REJECTED: Not enough chips to place bet on [{targetDesc}]. Remaining: {_gameState.ChipsRemainingThisSpin}");
			return;
		}

		float payout = WheelData.GetBasePayout(type);
		var bet = new Bet(type, numbers, DefaultChipsPerClick, payout);
		
		_gameState.ActiveBets.Add(bet);
		_buttonBets[btn].Add(bet);
		_gameState.ChipsRemainingThisSpin -= DefaultChipsPerClick;

		if (isStraight)
		{
			UpdateButtonVisualState(btn, numberVal);
		}
		else
		{
			UpdateOutsideButtonVisualState(btn, outsideBaseColor);
		}

		string targetStr = numbers != null ? string.Join(", ", numbers) : type.ToString();
		GD.Print($"[BettingTable] BET PLACED -> Type: {type}, Target(s): [{targetStr}], Chips Wagered: {DefaultChipsPerClick}, Chips Remaining: {_gameState.ChipsRemainingThisSpin}");

		GetNode<EventBus>("/root/EventBus").EmitSignal(EventBus.SignalName.BetPlaced);
	}

	private void RemoveBetForButton(Button btn, BetType type, List<int> numbers, int numberVal, bool isStraight, Color outsideBaseColor = default)
	{
		if (!_buttonBets.ContainsKey(btn) || _buttonBets[btn].Count == 0)
		{
			GD.Print("[BettingTable] Cannot remove bet: No active bets on this button.");
			return;
		}

		var betToRemove = _buttonBets[btn][_buttonBets[btn].Count - 1];
		
		_buttonBets[btn].Remove(betToRemove);
		_gameState.ActiveBets.Remove(betToRemove);
		_gameState.ChipsRemainingThisSpin += betToRemove.ChipsWagered;

		if (isStraight)
		{
			UpdateButtonVisualState(btn, numberVal);
		}
		else
		{
			UpdateOutsideButtonVisualState(btn, outsideBaseColor);
		}

		string targetStr = numbers != null ? string.Join(", ", numbers) : type.ToString();
		GD.Print($"[BettingTable] BET REMOVED -> Type: {type}, Target(s): [{targetStr}], Refunded: {betToRemove.ChipsWagered}, Chips Remaining: {_gameState.ChipsRemainingThisSpin}");

		GetNode<EventBus>("/root/EventBus").EmitSignal(EventBus.SignalName.BetPlaced);
	}

	private void OnSpinResolved(int winningNumber, int scoreGained)
	{
		foreach (var kvp in _buttonBets)
		{
			kvp.Value.Clear();
			if (kvp.Key.Text == "0")
			{
				StyleButtonBaseColor(kvp.Key, 0);
			}
			else if (int.TryParse(kvp.Key.Text, out int num))
			{
				StyleButtonBaseColor(kvp.Key, num);
			}
			else
			{
				Color resetColor = kvp.Key.Text switch
				{
					"RED" => new Color(0.8f, 0.15f, 0.15f),
					"BLACK" => new Color(0.18f, 0.18f, 0.18f),
					"ODD" or "EVEN" => new Color(0.2f, 0.3f, 0.5f),
					"1-18" or "19-36" => new Color(0.3f, 0.3f, 0.3f),
					_ => new Color(0.2f, 0.4f, 0.4f)
				};
				StyleOutsideButtonBaseColor(kvp.Key, resetColor);
			}
		}
		GD.Print("[BettingTable] Spin resolved — cleared all board highlights and button bet tracking.");
	}

	public void RepeatLastBets()
	{
		GD.Print("[BettingTable] Repeating last round's bets...");
		
		// 1. REFUND existing active bets before clearing them out
		foreach (var bet in _gameState.ActiveBets)
		{
			_gameState.ChipsRemainingThisSpin += bet.ChipsWagered;
		}

		// 2. Clear visual buttons and active list
		foreach (var kvp in _buttonBets)
		{
			kvp.Value.Clear();
			if (kvp.Key.Text == "0")
			{
				StyleButtonBaseColor(kvp.Key, 0);
			}
			else if (int.TryParse(kvp.Key.Text, out int num))
			{
				StyleButtonBaseColor(kvp.Key, num);
			}
			else
			{
				Color resetColor = kvp.Key.Text switch
				{
					"RED" => new Color(0.8f, 0.15f, 0.15f),
					"BLACK" => new Color(0.18f, 0.18f, 0.18f),
					"ODD" or "EVEN" => new Color(0.2f, 0.3f, 0.5f),
					"1-18" or "19-36" => new Color(0.3f, 0.3f, 0.3f),
					_ => new Color(0.2f, 0.4f, 0.4f)
				};
				StyleOutsideButtonBaseColor(kvp.Key, resetColor);
			}
		}
		_gameState.ActiveBets.Clear();

		// 3. Apply cached last round bets
		foreach (var bet in _lastRoundBets)
		{
			if (_gameState.ChipsRemainingThisSpin < bet.ChipsWagered)
			{
				GD.Print("[BettingTable] Repeat stopped: Insufficient chips for remaining repeated bets.");
				break;
			}

			_gameState.ActiveBets.Add(new Bet(bet.Type, bet.Numbers, bet.ChipsWagered, bet.Payout));
			_gameState.ChipsRemainingThisSpin -= bet.ChipsWagered;

			foreach (var kvp in _buttonBets)
			{
				if (bet.Type == BetType.Straight && bet.Numbers != null && bet.Numbers.Count > 0 && kvp.Key.Text == bet.Numbers[0].ToString())
				{
					kvp.Value.Add(_gameState.ActiveBets[_gameState.ActiveBets.Count - 1]);
					if (bet.Numbers[0] == 0) UpdateButtonVisualState(kvp.Key, 0);
					else UpdateButtonVisualState(kvp.Key, bet.Numbers[0]);
				}
				else if (bet.Type != BetType.Straight)
				{
					bool match = (bet.Type == BetType.Red && kvp.Key.Text == "RED") ||
								 (bet.Type == BetType.Black && kvp.Key.Text == "BLACK") ||
								 (bet.Type == BetType.Odd && kvp.Key.Text == "ODD") ||
								 (bet.Type == BetType.Even && kvp.Key.Text == "EVEN") ||
								 (bet.Type == BetType.Low && kvp.Key.Text == "1-18") ||
								 (bet.Type == BetType.High && kvp.Key.Text == "19-36") ||
								 (bet.Type == BetType.Dozen1 && kvp.Key.Text == "1st 12") ||
								 (bet.Type == BetType.Dozen2 && kvp.Key.Text == "2nd 12") ||
								 (bet.Type == BetType.Dozen3 && kvp.Key.Text == "3rd 12");

					if (match)
					{
						kvp.Value.Add(_gameState.ActiveBets[_gameState.ActiveBets.Count - 1]);
						Color col = kvp.Key.Text switch
						{
							"RED" => new Color(0.8f, 0.15f, 0.15f),
							"BLACK" => new Color(0.18f, 0.18f, 0.18f),
							"ODD" or "EVEN" => new Color(0.2f, 0.3f, 0.5f),
							"1-18" or "19-36" => new Color(0.3f, 0.3f, 0.3f),
							_ => new Color(0.2f, 0.4f, 0.4f)
						};
						UpdateOutsideButtonVisualState(kvp.Key, col);
					}
				}
			}
		}

		GD.Print($"[BettingTable] Repeat complete. Active bets count: {_gameState.ActiveBets.Count}, Remaining Chips: {_gameState.ChipsRemainingThisSpin}");
		GetNode<EventBus>("/root/EventBus").EmitSignal(EventBus.SignalName.BetPlaced);
	}

	public void CacheBetsForRepeat()
	{
		_lastRoundBets = new List<Bet>(_gameState.ActiveBets);
		GD.Print($"[BettingTable] Cached {_lastRoundBets.Count} bets for repeat.");
	}
}
