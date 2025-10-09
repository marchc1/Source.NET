using Source.Common.Commands;
using Source.Common.Formats.Keyvalues;
using Source.Common.GUI;
using Source.Common.Input;

namespace Source.GUI.Controls;

public class CNonFocusableMenu : Menu
{
	private Panel? FocusPanel;

	public CNonFocusableMenu(Panel? parent, string? panelName) : base(parent, panelName) {
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
	public TabCatchingTextEntry(Panel? parent, string? name, Panel comp) : base(parent, name) {

	}
	public override void OnKeyCodeTyped(ButtonCode code) {
		if (code == ButtonCode.KeyTab)
			GetParent()!.OnKeyCodeTyped(code);
		else if(code != ButtonCode.KeyEnter)
			base.OnKeyCodeTyped(code);
	}
}
public class ConsolePanel : EditablePanel, IConsoleDisplayFunc
{
	readonly public ICvar Cvar = Singleton<ICvar>();

	internal RichText History;
	internal TextEntry Entry;
	internal Button Submit;
	internal CNonFocusableMenu CompletionList;

	protected Color PrintColor, DPrintColor;

	protected int NextCompletion;
	protected char[] PartialText = new char[256];
	protected char[] PreviousPartialText = new char[256];
	protected bool AutoCompleteMode;
	protected bool WasBackspacing;
	protected bool StatusVersion;

	public override void OnTextChanged(Panel p) {

	}

	public override void OnCommand(ReadOnlySpan<char> command) {
		if(command.Equals("submit", StringComparison.OrdinalIgnoreCase)) {
			Span<char> incoming = stackalloc char[256];
			int len = Entry.GetText(incoming);
			PostActionSignal(new KeyValues("CommandSubmitted", "command", incoming[..len]));

			Print("] ");
			Print(incoming[..len]);
			Print("\n");
			Entry.SetText("");

			OnTextChanged(Entry);

			// todo: History.GotoTextEnd();

			int extraPtr = command.IndexOf(' ');
			ReadOnlySpan<char> extra = null;
			if (extraPtr != -1) {
				extra = command[(extraPtr + 1)..];
				command = command[..extraPtr];
			}

			if (command.Length > 0) 
				AddToHistory(command, extra);
			
			 // CompletionList.SetVisible(false);
		}
		base.OnCommand(command);
	}

	private void AddToHistory(ReadOnlySpan<char> command, ReadOnlySpan<char> extra) {

	}

	public ConsolePanel(Panel? parent, string? panelName, bool statusVersion) : base(parent, panelName) {
		StatusVersion = statusVersion;

		SetKeyboardInputEnabled(true);

		if (!StatusVersion)
			SetMinimumSize(100, 100);

		History = new RichText(this, "ConsoleHistory");
		History.SetAllowKeyBindingChainToParent(false);
		History.MakeReadyForUse();

		 // History.SetVerticalScrollbar(!statusVersion);
		 // if (StatusVersion)
			// History.SetDrawOffsets(3, 3);

		// History.GotoTextEnd();

		Submit = new Button(this, "ConsoleSubmit", "#Console_Submit");
		Submit.SetCommand("submit");
		Submit.SetVisible(!StatusVersion);

		CompletionList = new CNonFocusableMenu(this, "CompletionList");
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
				
				// OnAutoComplete(reverse);
				Entry.RequestFocus();
			}
			else if (code == ButtonCode.KeyDown) {
				// OnAutoComplete(false);

				Entry.RequestFocus();
			}
			else if (code == ButtonCode.KeyUp) {
				// OnAutoComplete(true);
				Entry.RequestFocus();
			}
		}
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
			y = Entry.GetTall();
		else
			y = Entry.GetTall() + 4;

		LocalToScreen(ref x, ref y);
		CompletionList.SetPos(x, y);

		if (CompletionList.IsVisible()) {
			Entry.RequestFocus();
			MoveToFront();
			CompletionList.MoveToFront();
		}
	}

	public bool TextEntryHasFocus() => Input.GetFocus() == Entry;

	public void TextEntryRequestFocus() => Entry.RequestFocus();

	const int MAX_HISTORY_ITEMS = 500;
	class CompletionItem
	{
		public ReadOnlySpan<char> GetItemText() => null;
		public ReadOnlySpan<char> GetCommand() => null;
		public ReadOnlySpan<char> GetName() => null;
		public bool IsCommand;
		public ConCommandBase? Command;
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
