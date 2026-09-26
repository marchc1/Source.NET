using Source;
using Source.Common.GUI;
using Source.Common.Input;
using Source.Common.MaterialSystem;
using Source.GUI.Controls;

namespace Game.Client.GarrysMod;

public enum HtmlMouseButton
{
	Left = 0,
	Middle = 1,
	Right = 2
}

public enum HtmlKeyEventType
{
	RawKeyDown = 0,
	KeyUp = 1,
	Char = 2
}

[Flags]
public enum HtmlKeyModifiers
{
	None = 0,
	LeftShift = 1 << 0,
	LeftControl = 1 << 1,
	LeftAlt = 1 << 2,
	LeftMouse = 1 << 3,
	MiddleMouse = 1 << 4,
	RightMouse = 1 << 5,
	RightShift = 1 << 7,
	RightControl = 1 << 8,
	RightAlt = 1 << 9,
	KeyPad = 1 << 10
}

public class HtmlPanel : EditablePanel
{
	static HtmlPanel() => ChainToAnimationMap<HtmlPanel>();

	TextureID TextureID;
	int TextureWide;
	int TextureTall;
	bool Loading;
	int CursorX;
	int CursorY;
	IMaterial? Material;
	bool FinishedLoading;

	public HtmlPanel(Panel? parent, ReadOnlySpan<char> panelName) : base(parent, panelName) {
		TextureID = -1;
		TextureWide = 0;
		TextureTall = 0;
		CursorX = 0;
		CursorY = 0;
		Material = null;
		FinishedLoading = false;
		Loading = true;
		// todo: create cef
	}

	public override void Dispose() {
		// todo: release cef
		// if (TextureID != -1)
		// 	Surface.DestroyTextureID(TextureID); // todo, I thought we had this??
		Material?.DecrementReferenceCount();
		TextureID = -1;
		Material = null;
		base.Dispose();
	}

	public override void OnSizeChanged(int newWide, int newTall) {
		if (newWide != 0 && newTall != 0) {
			// todo: send to cef
		}
	}

	public override void OnSetFocus() {
		// todo: send to cef
		base.OnSetFocus();
	}

	public override void OnKillFocus(Panel? newPanel) {
		// todo: send to cef
		base.OnKillFocus(newPanel);
	}

	public override void OnCursorMoved(int x, int y) {
		CursorX = x;
		CursorY = y;
		HtmlKeyModifiers modifiers = GetKeyModifiers();
		// todo: send to cef
	}

	public override void OnMousePressed(ButtonCode code) {
		RequestFocus();
		if (!GetMouseButton(code, out HtmlMouseButton button))
			return;
		HtmlKeyModifiers modifiers = GetKeyModifiers();
		// todo: send to cef (up = false, clicks = 1)
	}

	public override void OnMouseDoublePressed(ButtonCode code) {
		RequestFocus();
		if (!GetMouseButton(code, out HtmlMouseButton button))
			return;
		HtmlKeyModifiers modifiers = GetKeyModifiers();
		// todo: send to cef (up = false, clicks = 2)
	}

	public override void OnMouseReleased(ButtonCode code) {
		if (!GetMouseButton(code, out HtmlMouseButton button))
			return;
		HtmlKeyModifiers modifiers = GetKeyModifiers();
		// todo: send to cef (up = true, clicks = 1)
	}

	public override void OnMouseWheeled(int delta) {
		int deltaX = 0, deltaY = 0;
		if (vguiInput.IsKeyDown(ButtonCode.KeyLShift) || vguiInput.IsKeyDown(ButtonCode.KeyRShift))
			deltaX = delta * 100;
		else
			deltaY = delta * 100;
		HtmlKeyModifiers modifiers = GetKeyModifiers();
		// todo: send to cef
	}

	public override void OnKeyCodePressed(ButtonCode code) { }

	public override void OnKeyCodeTyped(ButtonCode code) {
		if (code == ButtonCode.None)
			return;

		if (code >= ButtonCode.KeyF1 && code <= ButtonCode.KeyF12) {
			// todo: run key binding
			return;
		}

		HtmlKeyModifiers modifiers = GetKeyModifiers();
		if (code >= ButtonCode.KeyPad0 && code <= ButtonCode.KeyPadDecimal)
			modifiers |= HtmlKeyModifiers.KeyPad;
		// todo: send to cef (HtmlKeyEventType.RawKeyDown, KeyCodeToVirtualKey(code), KeyCodeToScanCode(code))
	}

	public override void OnKeyTyped(char unichar) {
		HtmlKeyModifiers modifiers = GetKeyModifiers();
		// todo: send to cef (HtmlKeyEventType.Char)
	}

	public override void OnKeyCodeReleased(ButtonCode code) {
		if (code == ButtonCode.None || (code >= ButtonCode.KeyF1 && code <= ButtonCode.KeyF12))
			return;

		HtmlKeyModifiers modifiers = GetKeyModifiers();
		if (code >= ButtonCode.KeyPad0 && code <= ButtonCode.KeyPadDecimal)
			modifiers |= HtmlKeyModifiers.KeyPad;
		// todo: send to cef (HtmlKeyEventType.KeyUp, KeyCodeToVirtualKey(code), KeyCodeToScanCode(code))
	}

	public override void Paint() {
		// todo: cef texture upload
		// if (TextureID == -1 || TextureWide != wide || TextureTall != tall) {
		// 	if (TextureID != -1)
		// 		Surface.DestroyTextureID(TextureID);
		// 	Material?.DecrementReferenceCount();
		// 	TextureID = -1;
		// 	Material = null;
		// 	TextureID = Surface.CreateNewTextureID(true);
		// 	Surface.DrawSetTextureRGBAEx(TextureID, rgba, wide, tall, ImageFormat.BGRA8888);
		// 	todo: Material from texture mat info, IncrementReferenceCount
		// 	TextureWide = wide;
		// 	TextureTall = tall;
		// }
		// else
		// 	Surface.DrawUpdateRegionTextureRGBA(TextureID, 0, 0, rgba, wide, tall, ImageFormat.BGRA8888);

		if (TextureID != -1) {
			Surface.DrawSetColor(255, 255, 255, 255);
			Surface.DrawSetTexture(TextureID);
			Surface.DrawTexturedRect(0, 0, TextureWide, TextureTall);
		}
	}

	public void OpenURL(ReadOnlySpan<char> url) {
		// todo: send to cef
	}

	public void SetHTML(ReadOnlySpan<char> html) {
		// todo: send to cef
	}

	public void Refresh() {
		// todo: send to cef
	}

	public void StopLoading() {
		// todo: send to cef
	}

	public void GoBack() {
		// todo: send to cef
	}

	public void GoForward() {
		// todo: send to cef
	}

	public void RunJavascript(ReadOnlySpan<char> script) {
		// todo: send to cef
	}

	public void NewObjectCallback(ReadOnlySpan<char> objName, ReadOnlySpan<char> funcName) {
		// todo: send to cef
	}

	public bool IsLoading() => Loading;

	public IMaterial? GetHTMLMaterial() => FinishedLoading ? Material : null;

	public void UpdateHTMLTexture() {
		// todo: cef texture upload
	}

	static bool GetMouseButton(ButtonCode code, out HtmlMouseButton button) {
		switch (code) {
			case ButtonCode.MouseLeft: button = HtmlMouseButton.Left; return true;
			case ButtonCode.MouseRight: button = HtmlMouseButton.Right; return true;
			case ButtonCode.MouseMiddle: button = HtmlMouseButton.Middle; return true;
		}
		button = default;
		return false;
	}

	static HtmlKeyModifiers GetKeyModifiers() {
		HtmlKeyModifiers modifiers = vguiInput.IsKeyDown(ButtonCode.KeyLShift) ? HtmlKeyModifiers.LeftShift : HtmlKeyModifiers.None;
		if (vguiInput.IsKeyDown(ButtonCode.KeyRShift))
			modifiers |= HtmlKeyModifiers.RightShift;
		if (vguiInput.IsKeyDown(ButtonCode.KeyLControl))
			modifiers |= HtmlKeyModifiers.LeftControl;
		if (vguiInput.IsKeyDown(ButtonCode.KeyRControl))
			modifiers |= HtmlKeyModifiers.RightControl;
		if (vguiInput.IsKeyDown(ButtonCode.KeyLAlt))
			modifiers |= HtmlKeyModifiers.LeftAlt;
		if (vguiInput.IsKeyDown(ButtonCode.KeyRAlt))
			modifiers |= HtmlKeyModifiers.RightAlt;
		if (vguiInput.IsMouseDown(ButtonCode.MouseLeft))
			modifiers |= HtmlKeyModifiers.LeftMouse;
		if (vguiInput.IsMouseDown(ButtonCode.MouseMiddle))
			modifiers |= HtmlKeyModifiers.MiddleMouse;
		if (vguiInput.IsMouseDown(ButtonCode.MouseRight))
			modifiers |= HtmlKeyModifiers.RightMouse;
		return modifiers;
	}

	static readonly int[] ScanCodes = [
		0x0000, 0x000b, 0x0002, 0x0003, 0x0004, 0x0005, 0x0006, 0x0007, 0x0008, 0x0009,
		0x000a, 0x001e, 0x0030, 0x002e, 0x0020, 0x0012, 0x0021, 0x0022, 0x0023, 0x0017,
		0x0024, 0x0025, 0x0026, 0x0032, 0x0031, 0x0018, 0x0019, 0x0010, 0x0013, 0x001f,
		0x0014, 0x0016, 0x002f, 0x0011, 0x002d, 0x0015, 0x002c, 0x0052, 0x004f, 0x0050,
		0x0051, 0x004b, 0x004c, 0x004d, 0x0047, 0x0048, 0x0049, 0x0000, 0x0000, 0x0000,
		0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
		0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
		0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
		0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0xe048, 0xe04b,
		0xe050, 0xe04d
	];

	static readonly int[] VirtualKeys = [
		0x00, 0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38,
		0x39, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49,
		0x4a, 0x4b, 0x4c, 0x4d, 0x4e, 0x4f, 0x50, 0x51, 0x52, 0x53,
		0x54, 0x55, 0x56, 0x57, 0x58, 0x59, 0x5a, 0x60, 0x61, 0x62,
		0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6f, 0x6a, 0x6d,
		0x6b, 0x0d, 0x6e, 0xdb, 0xdd, 0xba, 0xde, 0xc0, 0xbc, 0xbe,
		0xbf, 0xdc, 0x6d, 0xbb, 0x0d, 0x20, 0x08, 0x09, 0x14, 0x90,
		0x1b, 0x91, 0x2d, 0x2e, 0x24, 0x23, 0x21, 0x22, 0x13, 0xa0,
		0xa1, 0xa4, 0xa5, 0xa2, 0xa3, 0x5b, 0x5c, 0x5d, 0x26, 0x25,
		0x28, 0x27
	];

	static int KeyCodeToScanCode(ButtonCode code) => (uint)code < ScanCodes.Length ? ScanCodes[(int)code] : 0;
	static int KeyCodeToVirtualKey(ButtonCode code) => (uint)code < VirtualKeys.Length ? VirtualKeys[(int)code] : 0;

	// todo: all callbacks below, lua hook
	public void OnAddressChange(ReadOnlySpan<char> url) { }

	public void OnConsoleMessage(ReadOnlySpan<char> message, ReadOnlySpan<char> source, int line, int level) {
		ReadOnlySpan<char> levelName = "log";
		if (level == 1)
			levelName = "debug";
		else if (level == 3)
			levelName = "warn";
		else if (level == 4)
			levelName = "error";

		if (message.StartsWith("RUNLUA:")) {
			// todo: lua hook
		}
		// todo: lua hook

		ConColorMsg(new Color(255, 160, 255, 255), "[HTML] ");
		if (level == 1)
			ConColorMsg(new Color(160, 160, 160, 255), "[Debug] ");
		else if (level == 3)
			ConColorMsg(new Color(255, 255, 90, 255), "[Warn]  ");
		else if (level == 4)
			ConColorMsg(new Color(255, 90, 90, 255), "[Error] ");

		string sourceStr = new(source);
		if (sourceStr.Length > 64)
			sourceStr = $"{sourceStr[..32]}...{sourceStr[^32..]}";

		ConColorMsg(new Color(210, 210, 210, 255), "{0}:{1}: ", sourceStr, line);
		ConColorMsg(new Color(255, 255, 255, 255), "{0}\n", new string(message));
	}

	public void OnTitleChange(ReadOnlySpan<char> title) { }
	public void OnTargetUrlChange(ReadOnlySpan<char> url) { }

	public void OnCursorChange(int cursor) {
		CursorCode code = cursor switch {
			1 => CursorCode.Crosshair,
			2 => CursorCode.Hand,
			3 => CursorCode.IBeam,
			4 => CursorCode.Hourglass,
			5 => CursorCode.SizeWE,
			6 => CursorCode.SizeNS,
			7 => CursorCode.SizeNWSE,
			8 => CursorCode.SizeNESW,
			9 => CursorCode.SizeAll,
			10 => CursorCode.No,
			11 => CursorCode.None,
			_ => CursorCode.Arrow
		};
		SetCursor(code);
	}

	public void OnBeginLoading(ReadOnlySpan<char> url) {
		Loading = true;
	}

	public void OnFinishLoading(ReadOnlySpan<char> url) {
		Loading = false;
		FinishedLoading = true;
	}

	public void OnDocumentReady(ReadOnlySpan<char> url) { }
	public void OnChildViewCreated(ReadOnlySpan<char> sourceUrl, ReadOnlySpan<char> targetUrl, bool isPopup) { }
	// todo: OnJavaScriptCall, lua hook
}
