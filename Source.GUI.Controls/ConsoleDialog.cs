using Source.Common.Commands;
using Source.Common.Formats.Keyvalues;
using Source.Common.GUI;
using Source.Common.Input;

namespace Source.GUI.Controls;

public class NonFocusableMenu : Menu
{
	private Panel? FocusPanel;

	public NonFocusableMenu(Panel? parent, string? panelName) : base(parent, panelName) {
		FocusPanel = null;
	}

	public void SetFocusPanel(Panel? panel) {
		FocusPanel = panel;
	}

	public override IPanel? GetCurrentKeyFocus() {
		if (FocusPanel == null)
			return this;
		return FocusPanel;
	}
}

public class TabCatchingTextEntry : TextEntry
{
	private Panel CompletionList;

	public TabCatchingTextEntry(Panel? parent, string? name, Panel comp) : base(parent, name) {
		SetAllowNonAsciiCharacters(true);
		//SetDragEnabled(true);

		CompletionList = comp;
	}

	public override void OnKeyCodeTyped(ButtonCode code) {
		if (code == ButtonCode.KeyTab)
			GetParent()!.OnKeyCodeTyped(code);
		else if (code != ButtonCode.KeyEnter)
			base.OnKeyCodeTyped(code);
	}

	public override void OnKillFocus(Panel? newPanel) {
		if (newPanel != CompletionList)
			PostMessage(GetParent(), new KeyValues("CloseCompletionList"));
	}
}

class HistoryItem
{
	public string? Text;
	public string? ExtraText;
	public bool HasExtra;

	public HistoryItem() {
		Text = null;
		ExtraText = null;
		HasExtra = false;
	}

	public HistoryItem(string text, string? extra = null) {
		Assert(text != null);
		Text = null;
		ExtraText = null;
		HasExtra = false;
		SetText(text, extra);
	}

	public HistoryItem(HistoryItem src) {
		Text = null;
		ExtraText = null;
		HasExtra = false;
		SetText(src.GetText(), src.GetExtra());
	}

	public string GetText() {
		if (Text != null)
			return Text;
		return "";
	}

	public string GetExtra() {
		if (ExtraText != null)
			return ExtraText;
		return "";
	}

	public void SetText(string text, string? extra) {
		Text = text;
		if (extra != null) {
			ExtraText = extra;
			HasExtra = true;
		}
		else
			HasExtra = false;
	}
}

public class ConsolePanel : EditablePanel, IConsoleDisplayFunc
{
	readonly public ICvar Cvar = Singleton<ICvar>();

	internal RichText History;
	internal TextEntry Entry;
	internal Button Submit;
	internal NonFocusableMenu CompletionList;

	protected Color PrintColor, DPrintColor;

	protected int NextCompletion;
	protected char[] PartialText = new char[256];
	protected char[] PreviousPartialText = new char[256];
	protected bool AutoCompleteMode;
	protected bool WasBackspacing;
	protected bool StatusVersion;

	List<CompletionItem> CompletionItems = new();
	List<HistoryItem> CommandHistory = new();

	static readonly KeyValues KV_ClosedByHittingTilde = new("ClosedByHittingTilde");
	static readonly KeyValues KV_Close = new("Close");

	public override void OnTextChanged(Panel panel) {
		if (panel != Entry)
			return;

		Array.Copy(PartialText, PreviousPartialText, PartialText.Length);

		Entry.GetText(PartialText);
		int len = Array.IndexOf(PartialText, '\0');
		if (len == -1) len = PartialText.Length;

		bool hitTilde = len > 0 && (PartialText[len - 1] == '~' || PartialText[len - 1] == '`');
		bool altKeyDown = Input.IsKeyDown(ButtonCode.KeyLAlt) || Input.IsKeyDown(ButtonCode.KeyRAlt);
		bool ctrlKeyDown = Input.IsKeyDown(ButtonCode.KeyLControl) || Input.IsKeyDown(ButtonCode.KeyRControl);

		if (len > 0 && hitTilde) {
			PreviousPartialText[len - 1] = '\0';

			if (!altKeyDown && !ctrlKeyDown) {
				Entry.SetText("");
				PostMessage(this, KV_Close);
				PostActionSignal(KV_ClosedByHittingTilde);
			}
			else {
				Entry.SetText(PartialText);
			}
		}

		AutoCompleteMode = false;

		RebuildCompletionList(PartialText);

		if (CompletionItems.Count < 1)
			CompletionList.SetVisible(false);
		else {
			CompletionList.SetVisible(true);

			int MAX_MENU_ITEMS = 10;
			CompletionList.DeleteAllItems();

			for (int i = 0; i < CompletionItems.Count && i < MAX_MENU_ITEMS; i++) {
				ReadOnlySpan<char> text;

				if (i == MAX_MENU_ITEMS - 1)
					text = "...";
				else {
					Assert(CompletionItems[i] != null);
					text = CompletionItems[i]!.GetItemText();
				}

				KeyValues kv = new KeyValues("CompletionCommand");
				kv.SetString("command", text);
				CompletionList.AddMenuItem(text, kv, this);
			}

			UpdateCompletionListPosition();
		}

		RequestFocus();
		Entry.RequestFocus();
	}

	public override void OnCommand(ReadOnlySpan<char> command) {
		if (command.Equals("submit", StringComparison.OrdinalIgnoreCase)) {
			Span<char> incoming = stackalloc char[256];
			int len = Entry.GetText(incoming);
			PostActionSignal(new KeyValues("CommandSubmitted", "command", incoming[..len]));

			Print("] ");
			Print(incoming[..len]);
			Print("\n");
			Entry.SetText("");

			OnTextChanged(Entry);

			History.GotoTextEnd();

			string incomingText = incoming[..len].ToString();
			int extraPtr = incomingText.IndexOf(' ');
			string? extra = null;
			if (extraPtr != -1) {
				extra = incomingText[(extraPtr + 1)..];
				incomingText = incomingText[..extraPtr];
			}

			if (incomingText.Length > 0)
				AddToHistory(incomingText, extra);

			CompletionList.SetVisible(false);
		}
		base.OnCommand(command);
	}

	private void AddToHistory(ReadOnlySpan<char> commandText, ReadOnlySpan<char> extraText) {
		while (CommandHistory.Count > MAX_HISTORY_ITEMS)
			CommandHistory.RemoveAt(0);

		Span<char> command = stackalloc char[commandText.Length + 1];
		if (!commandText.IsEmpty) {
			command.Clear();
			commandText.CopyTo(command);
			int last = commandText.Length - 1;
			if (last >= 0 && command[last] == ' ')
				command[last] = '\0';
		}

		Span<char> extra = stackalloc char[extraText.Length + 1];
		if (!extraText.IsEmpty) {
			extra.Clear();
			extraText.CopyTo(extra);
			int last = extraText.Length - 1;
			while (last >= 0 && extra[last] == ' ') {
				extra[last] = '\0';
				last--;
			}
		}

		HistoryItem item;
		for (int i = 0; i < CommandHistory.Count; i++) {
			item = CommandHistory[i];
			if (item == null)
				continue;

			if (MemoryExtensions.Equals(command, item.GetText().AsSpan(), StringComparison.OrdinalIgnoreCase))
				continue;

			if (!extra.IsEmpty || item.GetExtra() != null) {
				if (extra.IsEmpty || item.GetExtra() == null)
					continue;

				if (MemoryExtensions.Equals(extra, item.GetExtra().AsSpan(), StringComparison.OrdinalIgnoreCase))
					continue;
			}

			CommandHistory.RemoveAt(i);
		}

		item = new HistoryItem();
		CommandHistory.Add(item);
		item.SetText(command.ToString(), extra.IsEmpty ? null : extra.ToString());

		NextCompletion = 0;
		RebuildCompletionList(command);
	}

	private void ClearCompletionList() {
		for (int i = 0; i < CompletionItems.Count; i++)
			CompletionItems[i] = null;

		CompletionItems = [];
	}

	ConCommand? FindAutoCompleteCommandFromPartial(ReadOnlySpan<char> text) {
		Span<char> command = stackalloc char[256];
		strcpy(command, text);

		ConCommand cmd = Cvar.FindCommand(command)!;
		if (cmd == null)
			return null;

		if (!cmd.CanAutoComplete())
			return null;

		return cmd;
	}

	private void RebuildCompletionList(ReadOnlySpan<char> text) // todo: this isnt a perfect 1:1 just yet, still needs some cleaning up
	{
		ClearCompletionList();

		int len = text.IndexOf('\0');
		if (len < 1) {
			for (int i = 0; i < CommandHistory.Count; i++) {
				HistoryItem item = CommandHistory[i];
				CompletionItem comp = new CompletionItem();
				CompletionItems.Add(comp);
				comp.IsCommand = true;
				comp.Command = null;
				comp.Text = new HistoryItem(item);
			}
			return;
		}

		bool NormalBuild = true;
		int space = text.IndexOf(' ');

		if (space != -1) {
			ConCommand? cmd = FindAutoCompleteCommandFromPartial(text);
			if (cmd == null)
				return;

			NormalBuild = false;

			IEnumerable<string> commands = cmd.AutoCompleteSuggest(text.ToString());
			int count = commands.Count();
			//Assert(count <= COMMAND_COMPLETION_MAXITEMS);
			Assert(count <= 64);

			for (int i = 0; i < count; i++) {
				CompletionItem item = new CompletionItem();
				CompletionItems.Add(item);
				item.IsCommand = false;
				item.Command = null;
				item.Text = new HistoryItem(commands.ElementAt(i));
			}
		}

		if (NormalBuild) {
			foreach (ConCommandBase cmd in Cvar.GetCommands()) {
				if (cmd.IsFlagSet(FCvar.DevelopmentOnly) || cmd.IsFlagSet(FCvar.Hidden))
					continue;

				if (text.Equals(cmd.GetName(), StringComparison.OrdinalIgnoreCase)) {
					CompletionItem item = new CompletionItem();
					CompletionItems.Add(item);
					item.Command = cmd;
					string tst = cmd.GetName();
					if (!cmd.IsCommand()) {
						item.IsCommand = false;
						ConVar? var = cmd as ConVar;
						ConVar_ServerBounded? pBounded = cmd as ConVar_ServerBounded;

						if (pBounded != null || var.IsFlagSet(FCvar.NeverAsString)) {
							string strValue;

							int intVal = pBounded != null ? pBounded.GetInt() : var.GetInt();
							float floatVal = pBounded != null ? pBounded.GetFloat() : var.GetFloat();

							if (floatVal == intVal)
								strValue = intVal.ToString();
							else
								strValue = floatVal.ToString("F");

							item.Text = new HistoryItem(strValue);
						}
						else {
							item.Text = new HistoryItem(var.GetName(), var.GetString());
						}
					}
					else {
						item.IsCommand = true;
						item.Text = new HistoryItem(tst);
					}
				}
			}
		}

		if (CompletionItems.Count >= 2) {
			for (int i = 0; i < CompletionItems.Count; i++) {
				for (int j = 0; j < CompletionItems.Count; j++) {
					CompletionItem item1 = CompletionItems[i];
					CompletionItem item2 = CompletionItems[j];

					if (item1.GetName().Equals(item2.GetName(), StringComparison.Ordinal)) {
						CompletionItem temp = CompletionItems[i];
						CompletionItems[i] = CompletionItems[j];
						CompletionItems[j] = temp;
					}
				}
			}
		}
	}

	public ConsolePanel(Panel? parent, string? panelName, bool statusVersion) : base(parent, panelName) {
		StatusVersion = statusVersion;

		SetKeyboardInputEnabled(true);

		if (!StatusVersion)
			SetMinimumSize(100, 100);

		History = new RichText(this, "ConsoleHistory");
		History.SetAllowKeyBindingChainToParent(false);
		History.MakeReadyForUse();
		History.SetVerticalScrollbar(!statusVersion);

		if (StatusVersion)
			History.SetDrawOffsets(3, 3);

		History.GotoTextEnd();

		Submit = new Button(this, "ConsoleSubmit", "#Console_Submit");
		Submit.SetCommand("submit");
		Submit.SetVisible(!StatusVersion);

		CompletionList = new NonFocusableMenu(this, "CompletionList");
		CompletionList.SetVisible(false);

		Entry = new TabCatchingTextEntry(this, "ConsoleEntry", CompletionList);
		Entry.AddActionSignalTarget(this);
		Entry.SendNewLine(true);
		CompletionList.SetFocusPanel(Entry);

		PrintColor = new(216, 222, 211, 255);
		DPrintColor = new(196, 181, 80, 255);

		Entry.SetTabPosition(1);

		AutoCompleteMode = false;

		Cvar!.InstallConsoleDisplayFunc(this);
	}


	public override void OnKeyCodeTyped(ButtonCode code) {
		base.OnKeyCodeTyped(code);

		if (TextEntryHasFocus()) {
			if (code == ButtonCode.KeyTab) {
				bool reverse = false;
				if (Input.IsKeyDown(ButtonCode.KeyLShift) || Input.IsKeyDown(ButtonCode.KeyRShift))
					reverse = true;

				OnAutoComplete(reverse);
				Entry.RequestFocus();
			}
			else if (code == ButtonCode.KeyDown) {
				OnAutoComplete(false);

				Entry.RequestFocus();
			}
			else if (code == ButtonCode.KeyUp) {
				OnAutoComplete(true);
				Entry.RequestFocus();
			}
		}
	}

	private void OnAutoComplete(bool reverse) {
		if (!AutoCompleteMode) {
			NextCompletion = 0;
			AutoCompleteMode = true;
		}

		if (reverse) {
			NextCompletion -= 2;
			if (NextCompletion < 0)
				NextCompletion = CompletionItems.Count - 1;
		}

		if (NextCompletion < 0 || NextCompletion >= CompletionItems.Count || CompletionItems[NextCompletion] == null)
			NextCompletion = 0;

		if (NextCompletion < 0 || NextCompletion >= CompletionItems.Count || CompletionItems[NextCompletion] == null)
			return;

		Span<char> CompletedText = stackalloc char[255];
		CompletionItem item = CompletionItems[NextCompletion];
		Assert(item != null);

		if (item.IsCommand && item.Command != null) {
			ReadOnlySpan<char> cmd = item.GetCommand();
			strcpy(CompletedText, cmd);
		}
		else {
			ReadOnlySpan<char> txt = item.GetItemText();
			strcpy(CompletedText, txt);
		}

		Entry.SetText(CompletedText);
		Entry.GotoTextEnd();
		Entry.SelectNone();

		NextCompletion++;
	}

	public void Clear() {
		History.SetText("");
		History.GotoTextEnd();
	}
	public void ColorPrint(in Color clr, ReadOnlySpan<char> message) {
		if (StatusVersion)
			Clear();
		History.InsertColorChange(in clr);
		History.InsertString(message);
	}

	public void DPrint(ReadOnlySpan<char> message) {
		ColorPrint(DPrintColor, message);
	}

	public void Print(ReadOnlySpan<char> message) {
		ColorPrint(PrintColor, message);
	}

	public override void ApplySchemeSettings(IScheme scheme) {
		base.ApplySchemeSettings(scheme);

		PrintColor = GetSchemeColor("Console.TextColor", scheme);
		DPrintColor = GetSchemeColor("Console.DevTextColor", scheme);
		History.SetFont(scheme.GetFont("ConsoleText", IsProportional()));
		CompletionList.SetFont(scheme.GetFont("DefaultSmall", IsProportional()));

		InvalidateLayout();
	}

	public override void PerformLayout() {
		base.PerformLayout();

		GetFocusNavGroup().SetDefaultButton(Submit);

		IScheme scheme = GetScheme()!;
		Entry.SetBorder(scheme.GetBorder("DepressedButtonBorder"));
		History.SetBorder(scheme.GetBorder("DepressedButtonBorder"));

		GetSize(out int wide, out int tall);

		if (!StatusVersion) {
			const int inset = 8;
			const int entryHeight = 24;
			const int topHeight = 4;
			const int entryInset = 4;
			const int submitWide = 64;
			const int submitInset = 7;

			History.SetPos(inset, inset + topHeight);
			History.SetSize(wide - (inset * 2), tall - (entryInset * 2 + inset * 2 + topHeight + entryHeight));
			History.InvalidateLayout();

			int nSubmitXPos = wide - (inset + submitWide + submitInset);
			Submit.SetPos(nSubmitXPos, tall - (entryInset * 2 + entryHeight));
			Submit.SetSize(submitWide, entryHeight);

			Entry.SetPos(inset, tall - (entryInset * 2 + entryHeight));
			Entry.SetSize(nSubmitXPos - entryInset - 2 * inset, entryHeight);
		}
		else {
			const int inset = 2;

			int entryWidth = wide / 2;
			if (wide > 400) {
				entryWidth = 200;
			}

			Entry.SetBounds(inset, inset, entryWidth, tall - 2 * inset);

			History.SetBounds(inset + entryWidth + inset, inset, (wide - entryWidth) - inset, tall - 2 * inset);
		}

		UpdateCompletionListPosition();
	}

	private void UpdateCompletionListPosition() {
		Entry.GetPos(out int x, out int y);

		if (!StatusVersion)
			y += Entry.GetTall();
		else
			y -= Entry.GetTall() + 4;

		LocalToScreen(ref x, ref y);
		CompletionList.SetPos(x, y);

		if (CompletionList.IsVisible()) {
			Entry.RequestFocus();
			MoveToFront();
			CompletionList.MoveToFront();
		}
	}

	internal void OnCloseCompletionList() {
		CompletionList.SetVisible(false);
	}

	public override void OnMessage(KeyValues message, IPanel? from) {
		switch (message.Name) {
			case "CloseCompletionList":
				OnCloseCompletionList();
				return;
		}

		base.OnMessage(message, from);
	}

	public bool TextEntryHasFocus() => Input.GetFocus() == Entry;

	public void TextEntryRequestFocus() => Entry.RequestFocus();

	const int MAX_HISTORY_ITEMS = 500;
	class CompletionItem // todo: currently not 1:1
	{
		public bool IsCommand;
		public ConCommandBase? Command;
		public HistoryItem? Text;

		public CompletionItem() {
			IsCommand = false;
			Command = null;
			Text = null;
		}

		public CompletionItem(CompletionItem src) {
			IsCommand = src.IsCommand;
			Command = src.Command;
			Text = src.Text == null ? null : new HistoryItem(src.Text.Text, src.Text.ExtraText);
		}

		public ReadOnlySpan<char> GetName() {
			if (IsCommand)
				return Command!.GetName();
			return Command != null ? Command.GetName() : GetCommand();
		}

		public ReadOnlySpan<char> GetItemText() {
			string text = "";
			if (Text != null) {
				if (Text.HasExtra)
					text = Text.GetText() + " " + Text.GetExtra();
				else
					text = Text.GetText();
			}

			return text;
		}

		public ReadOnlySpan<char> GetCommand() {
			char[] text = new char[256];
			text[0] = '\0';
			if (Text != null)
				strcpy(text, Text.Text);
			return text;
		}
	}
}

public class ConsoleDialog : Frame
{
	protected ConsolePanel ConsolePanel;

	public ConsoleDialog(Panel? parent, string? name, bool statusVersion) : base(parent, name) {
		SetVisible(false);
		SetTitle("#Console_Title", true);

		ConsolePanel = new ConsolePanel(this, "ConsolePage", statusVersion);
		ConsolePanel.AddActionSignalTarget(this);
	}

	public override void OnMessage(KeyValues message, IPanel? from) {
		switch (message.Name) {
			case "CommandSubmitted":
				OnCommandSubmitted(message.GetString("command"));
				return;
		}

		base.OnMessage(message, from);
	}

	protected virtual void OnCommandSubmitted(ReadOnlySpan<char> command) {
		PostActionSignal(new KeyValues("CommandSubmitted", "command", command));
	}

	public override void PerformLayout() {
		base.PerformLayout();

		GetClientArea(out int x, out int y, out int w, out int h);
		ConsolePanel.SetBounds(x, y, w, h);
	}

	public override void Activate() {
		base.Activate();
		ConsolePanel.Entry.RequestFocus();
	}

	public void Clear() {
		ConsolePanel.Clear();
	}
	public void Hide() { }

	public void Print(ReadOnlySpan<char> msg) { }
	public void DPrint(ReadOnlySpan<char> msg) { }
	public void ColorPrint(in Color clr, ReadOnlySpan<char> msg) { }
	public void DumpConsoleTextToFile() { }

	public override void OnKeyCodePressed(ButtonCode code) {
		base.OnKeyCodePressed(code);
	}
}
