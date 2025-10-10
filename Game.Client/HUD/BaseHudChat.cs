using CommunityToolkit.HighPerformance;

using Game.Shared;

using Source;
using Source.Common;
using Source.Common.Bitbuffers;
using Source.Common.Client;
using Source.Common.Commands;
using Source.Common.Formats.Keyvalues;
using Source.Common.GUI;
using Source.Common.Input;
using Source.Engine;
using Source.GUI.Controls;

namespace Game.Client.HUD;

public class BaseHudChatLine : RichText
{
	protected IFont? Font;
	protected IFont? FontMarlett;
	Color TextColor;
	double ExpireTime;
	double StartTime;
	string? Text;
	int Count;
	protected List<TextRange> TextRanges = [];

	readonly ClientGlobalVariables gpGlobals = Singleton<ClientGlobalVariables>();

	public BaseHudChatLine(Panel? parent, string? name) : base(parent, name) {
		SetPaintBackgroundEnabled(true);
		SetVerticalScrollbar(true);
	}

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);

		Font = scheme.GetFont("Default");
		SetBgColor(new(0, 0, 0, 0));
		FontMarlett = scheme.GetFont("Marlett");
		TextColor = scheme.GetColor("FgColor", GetFgColor());
		SetFont(Font);
	}

	internal IFont? GetFont() {
		return Font;
	}

	public void SetExpireTime() {
		StartTime = gpGlobals.CurTime;
		ExpireTime = StartTime + BaseHudChat.hud_saytext_time.GetDouble();
		Count = BaseHudChat.LineCounter++;
	}

	int NameStart;
	int NameLength;
	Color NameColor;

	public int GetCount() => Count;
	readonly IEngineClient engine = Singleton<IEngineClient>();
	public bool IsReadyToExpire() {
		if (!engine.IsInGame() && !engine.IsConnected())
			return true;

		return gpGlobals.CurTime >= ExpireTime;
	}
	public double GetStartTime() => StartTime;
	public void Expire() {
		SetVisible(false);
	}

	internal void SetNameStart(int nameStart) => NameStart = nameStart;

	internal void SetNameLength(int nameLength) => NameLength = nameLength;

	internal void SetNameColor(Color clrNameColor) => NameColor = clrNameColor;
	internal unsafe void InsertAndColorizeText(ReadOnlySpan<char> buf, int playerIndex) {
		Text = null;
		TextRanges.Clear();

		Text = new(buf);

		BaseHudChat? chat = (BaseHudChat?)GetParent();

		if (chat == null)
			return;

		// TODO: Rewrite this to be safe!!!
		fixed (char* m_text = Text) {
			char* txt = m_text;
			int lineLen = Text.Length;
			Color colCustom = default;
			TextColor fcc = Text.Length > 0 ? (TextColor)Text[0] : 0;
			if (fcc == HUD.TextColor.PlayerName || fcc == HUD.TextColor.Location || fcc == HUD.TextColor.Normal || fcc == HUD.TextColor.Achievement || fcc == HUD.TextColor.Custom || fcc == HUD.TextColor.HexCode || fcc == HUD.TextColor.HexCodeAlpha) {
				while (txt != null && *txt != 0) {
					TextRange range = default;
					bool foundColorCode = false;
					bool done = false;
					int bytesIn = (int)(txt - m_text);

					switch ((TextColor)(*txt)) {
						case HUD.TextColor.Custom:
						case HUD.TextColor.PlayerName:
						case HUD.TextColor.Location:
						case HUD.TextColor.Achievement:
						case HUD.TextColor.Normal: {
								// save this start
								range.Start = bytesIn + 1;
								range.Color = chat.GetTextColorForClient((TextColor)(*txt), playerIndex);
								range.End = lineLen;
								foundColorCode = true;
							}
							++txt;
							break;
						case HUD.TextColor.HexCode:
						case HUD.TextColor.HexCodeAlpha: {
								bool readAlpha = ((TextColor)(*txt) == HUD.TextColor.HexCodeAlpha);
								int nCodeBytes = (readAlpha ? 8 : 6);
								range.Start = bytesIn + nCodeBytes + 1;
								range.End = lineLen;
								range.PreserveAlpha = readAlpha;
								++txt;

								if (range.End > range.Start) {
									int r = txt[0].Nibble() << 4 | txt[1].Nibble();
									int g = txt[2].Nibble() << 4 | txt[3].Nibble();
									int b = txt[4].Nibble() << 4 | txt[5].Nibble();
									int a = readAlpha ? txt[6].Nibble() << 4 | txt[7].Nibble() : 255;

									range.Color = new(r, g, b, a);
									foundColorCode = true;

									txt += nCodeBytes;
								}
								else
									done = true;
							}
							break;
						default:
							++txt;
							break;
					}

					if (done)
						break;

					if (foundColorCode) {
						int count = TextRanges.Count;
						if (count != 0) {
							Span<TextRange> textRanges = TextRanges.AsSpan();
							ref TextRange tr = ref textRanges[count - 1];
							tr.End = bytesIn;
						}
						TextRanges.Add(range);
					}
				}
			}

			if (TextRanges.Count == 0 && NameLength > 0 && m_text[0] == (int)HUD.TextColor.UseOldColors) {
				TextRange range = default;
				range.Start = 0;
				range.End = NameStart;
				range.Color = chat.GetTextColorForClient(HUD.TextColor.Normal, playerIndex);
				TextRanges.Add(range);

				range.Start = NameStart;
				range.End = NameStart + NameLength;
				range.Color = chat.GetTextColorForClient(HUD.TextColor.PlayerName, playerIndex);
				TextRanges.Add(range);

				range.Start = range.End;
				range.End = Text.Length;
				range.Color = chat.GetTextColorForClient(HUD.TextColor.Normal, playerIndex);
				TextRanges.Add(range);
			}

			if (TextRanges.Count == 0) {
				TextRange range = default;
				range.Start = 0;
				range.End = Text.Length;
				range.Color = chat.GetTextColorForClient(HUD.TextColor.Normal, playerIndex);
				TextRanges.Add(range);
			}

			for (int i = 0; i < TextRanges.Count; ++i) {
				char* start = m_text + TextRanges[i].Start;
				if (*start > 0 && *start < (int)HUD.TextColor.Max) {
					Assert(*start != (int)HUD.TextColor.HexCode && *start != (int)HUD.TextColor.HexCodeAlpha);
					Span<TextRange> textRanges2 = TextRanges.AsSpan();
					ref TextRange tr2 = ref textRanges2[i];
					tr2.Start += 1;
				}
			}

			Colorize();
		}
	}

	private void Colorize(int alpha = 255) {
		SetText("");

		BaseHudChat? chat = (BaseHudChat?)GetParent();

		if (chat != null && chat.GetChatHistory() != null)
			chat.GetChatHistory().InsertString("\n");

		Span<char> text = stackalloc char[4096];
		Color color = default;
		for (int i = 0; i < TextRanges.Count; ++i) {
			ReadOnlySpan<char> start = Text.AsSpan()[TextRanges[i].Start..];
			int len = TextRanges[i].End - TextRanges[i].Start + 1;
			if (len > 1 && len <= text.Length) {
				start.ClampedCopyTo(text);
				text[len - 1] = '\0';
				color = TextRanges[i].Color;
				if (!TextRanges[i].PreserveAlpha)
					color[3] = (byte)alpha;

				InsertColorChange(color);
				InsertString(text);

				if (chat != null && chat.GetChatHistory() != null) {
					chat.GetChatHistory().InsertColorChange(color);
					chat.GetChatHistory().InsertString(text);
					chat.GetChatHistory().InsertFade(BaseHudChat.hud_saytext_time.GetFloat(), BaseHudChat.CHAT_HISTORY_IDLE_FADE_TIME);

					if (i == TextRanges.Count - 1)
						chat.GetChatHistory().InsertFade(-1, -1);
				}
			}
		}

		InvalidateLayout(true);
	}
}

public class HudChatHistory : RichText
{
	readonly IEngineVGui enginevgui = Singleton<IEngineVGui>();
	public HudChatHistory(Panel? parent, string? name) : base(parent, "HudChatHistory") {
		IScheme scheme = SchemeManager.LoadSchemeFromFileEx(enginevgui.GetPanel(VGuiPanelType.ClientDll), "resource/ChatScheme.res", "ChatScheme")!;
		SetScheme(scheme);

		InsertFade(-1, -1);
	}


	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);
		SetFont(scheme.GetFont("ChatFont"));
		SetAlpha(255);
	}
}

public class BaseHudChatInputLine : Panel
{
	protected Label Prompt;
	protected BaseHudChatEntry Input;
	public BaseHudChatInputLine(Panel? parent, ReadOnlySpan<char> panelName) : base(parent, panelName) {
		SetMouseInputEnabled(false);
		Prompt = new Label(this, "ChatInputPrompt", "Enter text:");
		Input = new BaseHudChatEntry(this, "ChatInput", parent);
		Input.SetMaximumCharCount(127);
	}
	public Label GetPrompt() => Prompt;
	public Panel GetInputPanel() => Input;
	public void GetMessageText(Span<char> outBuffer) {
		Input.GetText(outBuffer);
	}
	public override IPanel? GetCurrentKeyFocus() => Input;

	internal void ClearEntry() {
		SetEntry("");
	}

	public override void PerformLayout() {
		base.PerformLayout();

		GetSize(out int wide, out int tall);

		Prompt.GetContentSize(out int w, out int h);
		Prompt.SetBounds(0, 0, w, tall);
		Input.SetBounds(w + 2, 0, wide - w - 2, tall);
	}

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);
		IFont? font = scheme.GetFont("ChatFont");

		Prompt.SetFont(font);
		Input.SetFont(font);

		Input.SetFgColor(scheme.GetColor("Chat.TypingText", scheme.GetColor("Panel.FgColor", new(255, 255, 255, 255))));

		SetPaintBackgroundEnabled(true);
		Prompt.SetPaintBackgroundEnabled(true);
		Prompt.SetContentAlignment(Alignment.West);
		Prompt.SetTextInset(2, 0);

		Input.SetMouseInputEnabled(true);

		SetBgColor(new(0, 0, 0, 0));
	}

	public override void ApplySettings(KeyValues resourceData) {
		base.ApplySettings(resourceData);
	}

	internal void SetEntry(ReadOnlySpan<char> entry) {
		Input.SetText(entry);
	}

	internal void SetPrompt(ReadOnlySpan<char> prompt) {
		Prompt.SetText(prompt);
		InvalidateLayout();
	}
}

public class HudChatFilterButton : Button
{
	public HudChatFilterButton(Panel parent, string name, string text) : base(parent, name, text) {
	}
}

public class HudChatFilterPanel : EditablePanel
{
	public HudChatFilterPanel(Panel? parent, string? panelName) : base(parent, panelName) {

	}

	public override void ApplySchemeSettings(IScheme scheme) {
		LoadControlSettings("resource/ui/ChatFilters.res");

		base.ApplySchemeSettings(scheme);

		Color color = scheme.GetColor("DullWhite", GetBgColor());
		SetBgColor(color with { A = (byte)BaseHudChat.CHAT_HISTORY_ALPHA });

		SetFgColor(scheme.GetColor("Blank", GetFgColor()));
	}
}

public class BaseHudChatEntry : TextEntry
{
	Panel? HudChat;
	public BaseHudChatEntry(Panel? parent, string? name, Panel chat) : base(parent, name) {
		SetCatchEnterKey(true);
		SetAllowNonAsciiCharacters(true);
		// set draw language id
		HudChat = chat;
	}

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);
		SetPaintBorderEnabled(false);
	}

	public override void OnKeyCodeTyped(ButtonCode code) {
		if (code == ButtonCode.KeyEnter || code == ButtonCode.KeyPadEnter || code == ButtonCode.KeyEscape) {
			if (code != ButtonCode.KeyEscape && HudChat != null)
				PostMessage(HudChat, new KeyValues("ChatEntrySend"));

			if (HudChat != null)
				PostMessage(HudChat, new KeyValues("ChatEntryStopMessageMode"));
		}
		else if (code == ButtonCode.KeyTab)
			return;
		else
			base.OnKeyCodeTyped(code);
	}
}

public enum ChatFilters
{
	None,
	JoinLeave = 0x000001,
	NameChange = 0x000002,
	PublicChat = 0x000004,
	ServerMsg = 0x000008,
	TeamChange = 0x000010,
	Achievement = 0x000020,
}

public enum TextColor
{
	Normal = 1,
	UseOldColors = 2,
	PlayerName = 3,
	Location = 4,
	Achievement = 5,
	Custom = 6,
	HexCode = 7,
	HexCodeAlpha = 8,
	Max
}

public struct TextRange
{
	public int Start;
	public int End;
	public Color Color;
	public bool PreserveAlpha;
}

public class BaseHudChat : EditableHudElement
{
	readonly IEngineClient engine = Singleton<IEngineClient>();
	public const int CHAT_INTERFACE_LINES = 6;
	public const int MAX_CHARS_PER_LINE = 128;

	public const float CHATLINE_NUM_FLASHES = 8;
	public const float CHATLINE_FLASH_TIME = 5;
	public const float CHATLINE_FADE_TIME = 1;

	public const float CHAT_HISTORY_FADE_TIME = 0.25f;
	public const float CHAT_HISTORY_IDLE_TIME = 15;
	public const float CHAT_HISTORY_IDLE_FADE_TIME = 2.5f;
	public const float CHAT_HISTORY_ALPHA = 127;

	internal static ConVar hud_saytext_time = new("12", 0);
	internal static ConVar cl_showtextmsg = new("1", 0, "Enable/disable text messages printing on the screen.");
	internal static ConVar cl_chatfilters = new("63", FCvar.ClientDLL | FCvar.Archive, "Stores the chat filter settings ");
	internal static ConVar cl_chatfilter_version = new("0", FCvar.ClientDLL | FCvar.Archive | FCvar.Hidden, "Stores the chat filter version");
	internal static ConVar cl_mute_all_comms = new("1", FCvar.Archive, "If 1, then all communications from a player will be blocked when that player is muted, including chat messages.");

	public static readonly Color ColorBlue = new(153, 204, 255, 255);
	public static readonly Color ColorRed = new(255, 63, 63, 255);
	public static readonly Color ColorGreen = new(153, 255, 153, 255);
	public static readonly Color ColorDarkGreen = new(64, 255, 64, 255);
	public static readonly Color ColorYellow = new(255, 178, 0, 255);
	public static readonly Color ColorGrey = new(204, 204, 204, 255);

	protected BaseHudChatLine? FindUnusedChatLine() {
		return ChatLine;
	}

	protected BaseHudChatInputLine ChatInput;
	protected BaseHudChatLine ChatLine;
	protected int FontHeight;
	protected HudChatHistory ChatHistory;
	protected HudChatFilterButton FiltersButton;
	protected HudChatFilterPanel FilterPanel;
	protected Color ColorCustom;
	public static int LineCounter;

	int ComputeBreakChar(int width, ReadOnlySpan<char> text) {
		BaseHudChatLine line = ChatLine;
		IFont? font = line.GetFont();

		int currentlen = 0;
		int lastbreak = text.Length;
		for (int i = 0; i < text.Length; i++) {
			char ch = text[i];

			if (ch <= 32)
				lastbreak = i;

			Surface.GetCharABCwide(font, ch, out int a, out int b, out int c);
			currentlen += a + b + c;

			if (currentlen >= width) {
				if (lastbreak == text.Length)
					lastbreak = Math.Max(0, i - 1);

				break;
			}
		}

		if (currentlen >= width)
			return lastbreak;

		return text.Length;
	}

	public override void ApplySchemeSettings(IScheme scheme) {
		LoadControlSettings("resource/ui/BaseChat.res");
		base.ApplySchemeSettings(scheme);

		SetPaintBackgroundType(PaintBackgroundType.Box);
		SetPaintBorderEnabled(true);
		SetPaintBackgroundEnabled(true);

		SetKeyboardInputEnabled(false);
		SetMouseInputEnabled(false);

		VisibleHeight = 0;

		Color dullWhite = scheme.GetColor("DullWhite", GetBgColor());
		SetBgColor(dullWhite with { A = (byte)CHAT_HISTORY_ALPHA });

		GetChatHistory().SetVerticalScrollbar(false);
	}

	MessageModeType MessageMode;
	int VisibleHeight;
	IFont? ChatFont;
	ChatFilters FilterFlags;
	bool EnteringVoice;
	double HistoryFadeTime;
	double HistoryIdleTime;

	readonly IEngineVGui EngineVGui = Singleton<IEngineVGui>();
	readonly ClientGlobalVariables gpGlobals = Singleton<ClientGlobalVariables>();

	public BaseHudChat(string? elementName) : base("HudChat", elementName) {
		Panel parent = clientMode.GetViewport();
		SetParent(parent);

		IScheme? scheme = SchemeManager.LoadSchemeFromFileEx(EngineVGui.GetPanel(VGuiPanelType.ClientDll), "resource/ChatScheme.res", "ChatScheme");
		SetScheme(scheme);

		Localize.AddFile("resource/chat_%language%.txt");

		MessageMode = 0;
		VGui.AddTickSignal(this);

		MakePopup();
		SetZPos(-30);

		HiddenBits = HideHudBits.Chat;

		FiltersButton = new HudChatFilterButton(this, "ChatFiltersButton", "Filters");

		FiltersButton.SetScheme(scheme);
		FiltersButton.SetVisible(true);
		FiltersButton.SetEnabled(true);
		FiltersButton.SetMouseInputEnabled(true);
		FiltersButton.SetKeyboardInputEnabled(false);

		ChatHistory = new HudChatHistory(this, "HudChatHistory");

		CreateChatLines();
		CreateChatInputLine();
		GetChatFilterPanel();

		FilterFlags = (ChatFilters)cl_chatfilters.GetInt();
	}

	public override void SetVisible(bool state) {
		base.SetVisible(state);
	}

	public void Reset() {
		VisibleHeight = 0;
		Clear();
	}

	public void Clear() {

	}

	public override void OnParentChanged(IPanel? oldParent, IPanel? newParent) {
		base.OnParentChanged(oldParent, newParent);
	}

	public override void OnTick() {
		VisibleHeight = 0;

		BaseHudChatLine? line = ChatLine;

		if (line != null) {
			IFont? font = line.GetFont();
			FontHeight = Surface.GetFontTall(font) + 2;
			ChatInput.GetBounds(out int iInputX, out int iInputY, out int iInputW, out int iInputH);
			GetBounds(out int iChatX, out int iChatY, out int iChatW, out int iChatH);
			ChatInput.SetBounds(iInputX, (int)(iChatH - (FontHeight * 1.75f)), iInputW, FontHeight);
			GetChatHistory().GetBounds(out int iChatHistoryX, out int iChatHistoryY, out int iChatHistoryW, out int iChatHistoryH);
			iChatHistoryH = (iChatH - (FontHeight * 9 / 4)) - iChatHistoryY;
			GetChatHistory().SetBounds(iChatHistoryX, iChatHistoryY, iChatHistoryW, iChatHistoryH);
		}

		FadeChatHistory();
	}

	private void FadeChatHistory() {
		float frac = (float)((HistoryFadeTime - gpGlobals.CurTime) / CHAT_HISTORY_FADE_TIME);

		int alpha = (int)(frac * CHAT_HISTORY_ALPHA);
		alpha = Math.Clamp(alpha, 0, (int)CHAT_HISTORY_ALPHA);

		if (alpha >= 0) {
			if (GetChatHistory() != null) {
				if (IsMouseInputEnabled()) {
					SetAlpha(255);
					GetChatHistory().SetBgColor(new(0, 0, 0, (int)(CHAT_HISTORY_ALPHA - alpha)));
					ChatInput.GetPrompt().SetAlpha((CHAT_HISTORY_ALPHA * 2) - alpha);
					ChatInput.GetInputPanel().SetAlpha((CHAT_HISTORY_ALPHA * 2) - alpha);
					SetBgColor(GetBgColor() with { A = (byte)(CHAT_HISTORY_ALPHA - alpha) });
					FiltersButton.SetAlpha((CHAT_HISTORY_ALPHA * 2) - alpha);
				}
				else {
					GetChatHistory().SetBgColor(new(0, 0, 0, alpha));
					SetBgColor(GetBgColor() with { A = (byte)alpha });
					ChatInput.GetPrompt().SetAlpha(alpha);
					ChatInput.GetInputPanel().SetAlpha(alpha);
					FiltersButton.SetAlpha(alpha);
				}
			}
		}
	}

	public override void Paint() {
		if (VisibleHeight == 0)
			return;
	}

	private void CreateChatLines() {
		ChatLine = new BaseHudChatLine(this, "ChatLine1");
		ChatLine.SetVisible(false);
	}

	private void CreateChatInputLine() {
		ChatInput = new BaseHudChatInputLine(this, "ChatInputLine");
		ChatInput.SetVisible(false);

		if (GetChatHistory() != null) {
			GetChatHistory().SetMaximumCharCount(127 * 100);
			GetChatHistory().SetVisible(true);
		}
	}

	private HudChatFilterPanel GetChatFilterPanel() {
		if (FilterPanel == null) {
			FilterPanel = new HudChatFilterPanel(this, "HudChatFilterPanel");

			IScheme? scheme = SchemeManager.LoadSchemeFromFileEx(EngineVGui.GetPanel(VGuiPanelType.ClientDll), "resource/ChatScheme.res", "ChatScheme");

			FilterPanel.SetScheme(scheme);
			FilterPanel.InvalidateLayout(true, true);
			FilterPanel.SetMouseInputEnabled(true);
			FilterPanel.SetPaintBackgroundType(PaintBackgroundType.Box);
			FilterPanel.SetPaintBorderEnabled(true);
			FilterPanel.SetVisible(false);
		}

		return FilterPanel;
	}

	protected void SayText(bf_read msg) {
		Span<char> str = stackalloc char[256];

		int client = msg.ReadByte();
		str = str[..msg.ReadString(str)];
		bool wantsToChat = msg.ReadByte() != 0;

		if (wantsToChat)
			ChatPrintf(client, ChatFilters.None, str);
		else
			Printf(ChatFilters.None, str);
	}

	public HudChatHistory GetChatHistory() => ChatHistory;

	private void Printf(ChatFilters filter, ReadOnlySpan<char> str) {

	}

	public ChatFilters GetFilterFlags() => FilterFlags;

	private void ChatPrintf(int playerIndex, ChatFilters filter, ReadOnlySpan<char> str) {
		ReadOnlySpan<char> trimmed = str;

		trimmed = trimmed.TrimEnd('\n');
		while (trimmed.Length > 0 && trimmed[0] != '\0' && (trimmed[0] == '\n' || (trimmed[0] > 0 && trimmed[0] < (int)HUD.TextColor.Max)))
			trimmed = trimmed[1..];
		trimmed.TrimStart('\n');

		BaseHudChatLine? line = FindUnusedChatLine();
		if (line == null)
			return;

		if (filter != ChatFilters.None)
			if ((filter & GetFilterFlags()) == 0) return;

		line.SetText("");

		PlayerInfo playerInfo = new();
		if (playerIndex == 0)
			strcpy(playerInfo.Name, "Console");
		else
			engine.GetPlayerInfo(playerIndex, out playerInfo);

		int nameStart = 0;
		int nameLength = 0;

		Color clrNameColor = GetClientColor(playerIndex);

		ReadOnlySpan<char> playerName = ((ReadOnlySpan<char>)playerInfo.Name).SliceNullTerminatedString();
		Span<char> buf = stackalloc char[playerName.Length + 2 + trimmed.Length + 1];
		int writePtr = 0;

		playerName.CopyTo(buf[writePtr..]);
		nameStart = writePtr;
		nameLength = playerName.Length;
		writePtr += playerName.Length;

		": ".CopyTo(buf[writePtr..]);
		writePtr += 2;

		trimmed.CopyTo(buf[writePtr..]);
		writePtr += trimmed.Length;

		"\n".CopyTo(buf[writePtr..]);
		writePtr += 1;

		line.SetExpireTime();

		line.SetVisible(false);
		line.SetNameStart(nameStart);
		line.SetNameLength(nameLength);
		line.SetNameColor(clrNameColor);
		// We only need the \n for Msg
		line.InsertAndColorizeText(buf[..(buf.Length - 1)], playerIndex);

		Msg(buf);
	}

	public virtual Color GetClientColor(int clientIndex) {
		if (clientIndex == 0)
			return ColorGreen;
		else
			return ColorYellow;
	}

	protected void SayText2(bf_read msg) {

	}

	protected void TextMsg(bf_read msg) {

	}

	internal void StartMessageMode(MessageModeType messageModeType) {
		MessageMode = messageModeType;
		ChatInput.ClearEntry();

		ReadOnlySpan<char> prompt = MessageMode == MessageModeType.Say ? Localize.Find("#chat_say") : Localize.Find("#chat_say_team");
		if (!prompt.IsEmpty)
			ChatInput.SetPrompt(prompt);
		else
			ChatInput.SetPrompt(MessageMode == MessageModeType.Say ? "Say : " : "Say (TEAM) :");

		if (GetChatHistory() != null) {
			GetChatHistory().SetMouseInputEnabled(true);
			GetChatHistory().SetKeyboardInputEnabled(false);
			GetChatHistory().SetVerticalScrollbar(true);
			GetChatHistory().ResetAllFades(true);
			GetChatHistory().SetPaintBorderEnabled(true);
			GetChatHistory().SetVisible(true);
		}

		MakeReadyForUse();
		SetKeyboardInputEnabled(true);
		SetMouseInputEnabled(true);
		ChatInput.SetVisible(true);
		Surface.CalculateMouseVisible();
		ChatInput.RequestFocus();
		ChatInput.SetPaintBorderEnabled(true);
		ChatInput.SetMouseInputEnabled(true);

		GetChatHistory().GetBounds(out int x, out int y, out int w, out int h);
		Input.SetCursorPos(x + (w / 2), y + (h / 2));

		HistoryFadeTime = gpGlobals.CurTime + CHAT_HISTORY_FADE_TIME;

		FilterPanel.SetVisible(false);

		engine.ClientCmd_Unrestricted("gameui_preventescapetoshow\n");
	}

	public void Send() {
		Span<char> text = stackalloc char[128];
		ChatInput.GetMessageText(text);
		if (text.Length > 0 && text[^1] == '\n')
			text[^1] = '\0';

		text = text.SliceNullTerminatedString();
		Span<char> buf = stackalloc char[144];
		int writePtr = 0;
		string cmd = (MessageMode == MessageModeType.Say ? "say " : "say_team ");
		cmd.CopyTo(buf[writePtr..]); writePtr += cmd.Length;
		text.CopyTo(buf[writePtr..]); writePtr += text.Length;
		engine.ClientCmd_Unrestricted(buf[..writePtr]);
	}

	public override void OnMessage(KeyValues message, IPanel? from) {
		switch (message.Name) {
			case "ChatEntryStopMessageMode": StopMessageMode(); break;
			case "ChatEntrySend": Send(); break;
			default: base.OnMessage(message, from); break;
		}
	}

	public void StopMessageMode() {
		engine.ClientCmd_Unrestricted("gameui_allowescapetoshow\n");

		SetKeyboardInputEnabled(false);
		SetMouseInputEnabled(false);

		if (GetChatHistory() != null) {
			GetChatHistory().SetPaintBorderEnabled(false);
			GetChatHistory().GotoTextEnd();
			GetChatHistory().SetMouseInputEnabled(false);
			GetChatHistory().SetVerticalScrollbar(false);
			GetChatHistory().ResetAllFades(false, true, CHAT_HISTORY_FADE_TIME);
			GetChatHistory().SelectNoText();
		}

		ChatInput.ClearEntry();
		FilterPanel.SetVisible(false);
		HistoryFadeTime = gpGlobals.CurTime + CHAT_HISTORY_FADE_TIME;
		MessageMode = MessageModeType.None;
	}

	public MessageModeType GetMessageMode() => MessageMode;

	public Color GetTextColorForClient(TextColor tcCur, int playerIndex) {
		Color c = default;
		switch (tcCur) {
			case TextColor.Custom:
				c = ColorCustom;
				break;

			case TextColor.PlayerName:
				c = GetClientColor(playerIndex);
				break;

			case TextColor.Location:
				c = ColorDarkGreen;
				break;

			case TextColor.Achievement:
				IScheme? sourceScheme = SchemeManager.GetScheme("SourceScheme");
				c = sourceScheme?.GetColor("SteamLightGreen", GetBgColor()) ?? GetDefaultTextColor();
				break;

			default:
				c = GetDefaultTextColor();
				break;
		}

		return c with { A = 255 };
	}

	private Color GetDefaultTextColor() {
		return ColorYellow;
	}
}

[DeclareHudElement(Name = "CHudChat")]
public class HudChat : BaseHudChat
{
	public HudChat(string? panelName) : base(panelName) {

	}
	public override void Init() {
		base.Init();

		IHudElement.HookMessage("SayText", SayText);
		IHudElement.HookMessage("SayText2", SayText2);
		IHudElement.HookMessage("TextMsg", TextMsg);
	}
	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);
	}
}
