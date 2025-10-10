
using Microsoft.Extensions.DependencyInjection;

using SDL;

using Source;
using Source.Common.Input;
using Source.Common.Launcher;

namespace Nucleus.SDL3Window;

public class InputState
{
	public bool[] ButtonState;
	public int[] ButtonPressedTick;
	public int[] ButtonReleasedTick;
	public int[] AnalogDelta;
	public int[] AnalogValue;
	public Queue<InputEvent> Events;
	public bool Dirty;
	public InputState() {
		ButtonState = new bool[(int)ButtonCode.Count];
		ButtonPressedTick = new int[(int)ButtonCode.Count];
		ButtonReleasedTick = new int[(int)ButtonCode.Count];
		AnalogDelta = new int[(int)AnalogCode.Last];
		AnalogValue = new int[(int)AnalogCode.Last];
		Events = [];
		Dirty = false;
	}
}

public class SDL3_InputSystem(IServiceProvider services) : IInputSystem
{
	public InputState[] InputStates = [new(), new()];

	InputState InputState => IsPolling ? InputStates[1] : InputStates[0];
	InputState QueuedInputState => InputStates[(int)InputStateType.Queued];
	InputState CurrentInputState => InputStates[(int)InputStateType.Current];

	public IWindow? Window;
	public bool InputEnabled;
	public bool PumpEnabled;
	public bool IsPolling;

	int LastSampleTick;
	int LastPollTick;
	int PollCount;

	public unsafe void StartTextInput() {
		SDL_Window* window = (SDL_Window*)Window!.GetHandle();
		SDL3.SDL_StartTextInput(window);
	}

	public unsafe void StopTextInput() {
		SDL_Window* window = (SDL_Window*)Window!.GetHandle();
		SDL3.SDL_StopTextInput(window);
	}

	ILauncherManager launcherMgr;

	public void AttachToWindow(IWindow window) {
		Window = window;
		launcherMgr ??= services.GetRequiredService<ILauncherManager>();
		ClearInputState();
	}

	public int ButtonCodeToVirtualKey(ButtonCode code) {
		throw new NotImplementedException();
	}

	public void DetachFromWindow() {
		Window = null;
	}

	public void EnableInput(bool enabled) => InputEnabled = enabled;
	public void EnableMessagePump(bool enabled) => PumpEnabled = enabled;

	public int GetButtonPressedTick(ButtonCode code) {
		throw new NotImplementedException();
	}

	public int GetButtonReleasedTick(ButtonCode code) {
		throw new NotImplementedException();
	}

	public long GetEventCount() => CurrentInputState.Events.Count;
	public IEnumerable<InputEvent> GetEventData() => CurrentInputState.Events;

	public int GetPollCount() {
		return PollCount;
	}

	public int GetPollTick() {
		return LastPollTick;
	}

	public bool GetRawMouseAccumulators(out int accumX, out int accumY) {
		throw new NotImplementedException();
	}

	public bool IsButtonDown(ButtonCode code) {
		throw new NotImplementedException();
	}

	public void ClearInputState() {
		foreach (var state in InputStates) {
			state.Events.Clear();
			Array.Clear(state.ButtonState);
			Array.Clear(state.ButtonPressedTick);
			Array.Clear(state.ButtonReleasedTick);
			Array.Clear(state.AnalogValue);
			Array.Clear(state.AnalogDelta);
			state.Dirty = false;
		}
	}

	static ButtonCode[] scantokey;
	static SDL3_InputSystem() {
		scantokey = new ButtonCode[(int)SDL_Scancode.SDL_SCANCODE_COUNT];
		for (int i = (int)SDL_Scancode.SDL_SCANCODE_A; i <= (int)SDL_Scancode.SDL_SCANCODE_Z; i++)
			scantokey[i] = ButtonCode.KeyA + (i - (int)SDL_Scancode.SDL_SCANCODE_A);
		for (int i = (int)SDL_Scancode.SDL_SCANCODE_1; i <= (int)SDL_Scancode.SDL_SCANCODE_9; i++)
			scantokey[i] = ButtonCode.Key1 + (i - (int)SDL_Scancode.SDL_SCANCODE_1);
		for (int i = (int)SDL_Scancode.SDL_SCANCODE_F1; i <= (int)SDL_Scancode.SDL_SCANCODE_F12; i++)
			scantokey[i] = ButtonCode.KeyF1 + (i - (int)SDL_Scancode.SDL_SCANCODE_F1);
		for (int i = (int)SDL_Scancode.SDL_SCANCODE_KP_1; i <= (int)SDL_Scancode.SDL_SCANCODE_KP_9; i++)
			scantokey[i] = ButtonCode.KeyPad1 + (i - (int)SDL_Scancode.SDL_SCANCODE_KP_1);

		scantokey[(int)SDL_Scancode.SDL_SCANCODE_0] = ButtonCode.Key0;
		scantokey[(int)SDL_Scancode.SDL_SCANCODE_KP_0] = ButtonCode.KeyPad0;
		scantokey[(int)SDL_Scancode.SDL_SCANCODE_RETURN] = ButtonCode.KeyEnter;
		scantokey[(int)SDL_Scancode.SDL_SCANCODE_ESCAPE] = ButtonCode.KeyEscape;
		scantokey[(int)SDL_Scancode.SDL_SCANCODE_BACKSPACE] = ButtonCode.KeyBackspace;
		scantokey[(int)SDL_Scancode.SDL_SCANCODE_TAB] = ButtonCode.KeyTab;
		scantokey[(int)SDL_Scancode.SDL_SCANCODE_SPACE] = ButtonCode.KeySpace;
		scantokey[(int)SDL_Scancode.SDL_SCANCODE_MINUS] = ButtonCode.KeyMinus;
		scantokey[(int)SDL_Scancode.SDL_SCANCODE_EQUALS] = ButtonCode.KeyEqual;
		scantokey[(int)SDL_Scancode.SDL_SCANCODE_LEFTBRACKET] = ButtonCode.KeyLBracket;
		scantokey[(int)SDL_Scancode.SDL_SCANCODE_RIGHTBRACKET] = ButtonCode.KeyRBracket;
		scantokey[(int)SDL_Scancode.SDL_SCANCODE_BACKSLASH] = ButtonCode.KeyBackslash;
		scantokey[(int)SDL_Scancode.SDL_SCANCODE_SEMICOLON] = ButtonCode.KeySemicolon;
		scantokey[(int)SDL_Scancode.SDL_SCANCODE_APOSTROPHE] = ButtonCode.KeyApostrophe;
		scantokey[(int)SDL_Scancode.SDL_SCANCODE_GRAVE] = ButtonCode.KeyBackquote;
		scantokey[(int)SDL_Scancode.SDL_SCANCODE_COMMA] = ButtonCode.KeyComma;
		scantokey[(int)SDL_Scancode.SDL_SCANCODE_PERIOD] = ButtonCode.KeyPeriod;
		scantokey[(int)SDL_Scancode.SDL_SCANCODE_SLASH] = ButtonCode.KeySlash;
		scantokey[(int)SDL_Scancode.SDL_SCANCODE_CAPSLOCK] = ButtonCode.KeyCapsLock;
		scantokey[(int)SDL_Scancode.SDL_SCANCODE_SCROLLLOCK] = ButtonCode.KeyScrollLock;
		scantokey[(int)SDL_Scancode.SDL_SCANCODE_INSERT] = ButtonCode.KeyInsert;
		scantokey[(int)SDL_Scancode.SDL_SCANCODE_HOME] = ButtonCode.KeyHome;
		scantokey[(int)SDL_Scancode.SDL_SCANCODE_PAGEUP] = ButtonCode.KeyPageUp;
		scantokey[(int)SDL_Scancode.SDL_SCANCODE_DELETE] = ButtonCode.KeyDelete;
		scantokey[(int)SDL_Scancode.SDL_SCANCODE_END] = ButtonCode.KeyEnd;
		scantokey[(int)SDL_Scancode.SDL_SCANCODE_PAGEDOWN] = ButtonCode.KeyPageDown;
		scantokey[(int)SDL_Scancode.SDL_SCANCODE_RIGHT] = ButtonCode.KeyRight;
		scantokey[(int)SDL_Scancode.SDL_SCANCODE_LEFT] = ButtonCode.KeyLeft;
		scantokey[(int)SDL_Scancode.SDL_SCANCODE_DOWN] = ButtonCode.KeyDown;
		scantokey[(int)SDL_Scancode.SDL_SCANCODE_UP] = ButtonCode.KeyUp;
		scantokey[(int)SDL_Scancode.SDL_SCANCODE_NUMLOCKCLEAR] = ButtonCode.KeyNumLock;
		scantokey[(int)SDL_Scancode.SDL_SCANCODE_KP_DIVIDE] = ButtonCode.KeyPadDivide;
		scantokey[(int)SDL_Scancode.SDL_SCANCODE_KP_MULTIPLY] = ButtonCode.KeyPadMultiply;
		scantokey[(int)SDL_Scancode.SDL_SCANCODE_KP_MINUS] = ButtonCode.KeyPadMinus;
		scantokey[(int)SDL_Scancode.SDL_SCANCODE_KP_PLUS] = ButtonCode.KeyPadPlus;
		scantokey[(int)SDL_Scancode.SDL_SCANCODE_KP_ENTER] = ButtonCode.KeyEnter;
		scantokey[(int)SDL_Scancode.SDL_SCANCODE_KP_PERIOD] = ButtonCode.KeyPadDecimal;
		scantokey[(int)SDL_Scancode.SDL_SCANCODE_APPLICATION] = ButtonCode.KeyApp;
		scantokey[(int)SDL_Scancode.SDL_SCANCODE_LCTRL] = ButtonCode.KeyLControl;
		scantokey[(int)SDL_Scancode.SDL_SCANCODE_LSHIFT] = ButtonCode.KeyLShift;
		scantokey[(int)SDL_Scancode.SDL_SCANCODE_LALT] = ButtonCode.KeyLAlt;
		scantokey[(int)SDL_Scancode.SDL_SCANCODE_LGUI] = ButtonCode.KeyLWin;
		scantokey[(int)SDL_Scancode.SDL_SCANCODE_RCTRL] = ButtonCode.KeyRControl;
		scantokey[(int)SDL_Scancode.SDL_SCANCODE_RSHIFT] = ButtonCode.KeyRShift;
		scantokey[(int)SDL_Scancode.SDL_SCANCODE_RALT] = ButtonCode.KeyRAlt;
		scantokey[(int)SDL_Scancode.SDL_SCANCODE_RGUI] = ButtonCode.KeyRWin;
	}


	static bool MapVirtualKeyToButtonCode(int virtualKeyCode, out ButtonCode pOut) {
		if (virtualKeyCode < 0)
			pOut = (ButtonCode)(-1 * virtualKeyCode);
		else {
			virtualKeyCode &= 0x000000ff;
			pOut = (ButtonCode)scantokey[virtualKeyCode];
		}

		return true;
	}

	public void PostButtonPressedEvent(InputEventType type, int tick, ButtonCode scanCode, ButtonCode virtualCode) {
		InputState state = InputState;

		if (!state.ButtonState.IsBitSet((int)scanCode)) {
			state.ButtonState.Set((int)scanCode);
			state.ButtonPressedTick[(int)scanCode] = tick;

			PostEvent(type, tick, (int)scanCode, (int)virtualCode, 0);
		}
	}

	public void PostButtonReleasedEvent(InputEventType type, int tick, ButtonCode scanCode, ButtonCode virtualCode) {
		InputState state = InputState;

		if (state.ButtonState.IsBitSet((int)scanCode)) {
			state.ButtonState.Clear((int)scanCode);
			state.ButtonReleasedTick[(int)scanCode] = tick;

			PostEvent(type, tick, (int)scanCode, (int)virtualCode, 0);
		}
	}

	private enum InputStateType
	{
		Queued,
		Current,
		Count
	}

	WindowEvent[] eventBuffer = new WindowEvent[32];
	public void CopyInputState(InputState dest, InputState src, bool copyEvents) {
		dest.Events.Clear();
		dest.Dirty = false;
		if (src.Dirty) {
			dest.ButtonState = src.ButtonState;
			Array.ConstrainedCopy(src.ButtonPressedTick, 0, dest.ButtonPressedTick, 0, dest.ButtonPressedTick.Length);
			Array.ConstrainedCopy(src.ButtonReleasedTick, 0, dest.ButtonReleasedTick, 0, dest.ButtonReleasedTick.Length);
			Array.ConstrainedCopy(src.AnalogDelta, 0, dest.AnalogDelta, 0, dest.AnalogDelta.Length);
			Array.ConstrainedCopy(src.AnalogValue, 0, dest.AnalogValue, 0, dest.AnalogValue.Length);
			if (copyEvents) {
				if (src.Events.Count > 0) {
					dest.Events.Clear();
					dest.Events.EnsureCapacity(src.Events.Count);
					foreach (var queued in src.Events) dest.Events.Enqueue(queued);
				}
			}
		}
	}
	public void PollInputState() {
		IsPolling = true;
		PollCount++;

		InputState queuedState = QueuedInputState;
		CopyInputState(CurrentInputState, queuedState, true);
		SampleDevices();

		LastPollTick = LastSampleTick;

		PollInputState_Platform();
		CopyInputState(queuedState, CurrentInputState, false);
		IsPolling = false;
	}

	private void SampleDevices() {
		LastSampleTick = ComputeSampleTick();
	}

	private int ComputeSampleTick() {
		int sampleTick;

		uint nCurrentTick = Convert.ToUInt32(Platform.MSTime);
		sampleTick = (int)nCurrentTick;
		return sampleTick;
	}

	public void PollInputState_Platform() {
		InputState state = InputState;

		while (true) {
			int events = launcherMgr?.GetEvents(eventBuffer, eventBuffer.Length) ?? 0;
			if (events == 0) break;
			for (int i = 0; i < events; i++) {
				WindowEvent ev = eventBuffer[i];
				switch (ev.EventType) {
					case WindowEventType.AppActivate: {
							InputEvent newEv = new();
							newEv.Type = InputEventType.App_AppActivated;
							newEv.Data = ev.WasWindowFocused ? 1 : 0;

							PostUserEvent(in newEv);
							if (ev.ModifierKeyMask == 0) {
								ResetInputState();
							}
						}
						break;
					case WindowEventType.KeyDown: {
							if (MapVirtualKeyToButtonCode(ev.VirtualKeyCode, out ButtonCode virtualCode)) {
								ButtonCode scancode = virtualCode;
								if (scancode != ButtonCode.None)
									PostButtonPressedEvent(InputEventType.IE_ButtonPressed, LastSampleTick, scancode, virtualCode);

								InputEvent newEv = new() {
									Tick = GetPollTick(),
									Type = InputEventType.Gui_KeyCodeTyped,
									Data = (int)scancode
								};
								PostUserEvent(newEv);
							}

							if (0 == (ev.ModifierKeyMask & KeyModifier.Command) && ev.VirtualKeyCode >= 0 && ev.UTF8Key > 0) {
								InputEvent newEv = new() {
									Tick = GetPollTick(),
									Type = InputEventType.Gui_KeyTyped,
									Data = ev.UTF8Key
								};
								PostUserEvent(newEv);
							}
						}
						break;

					case WindowEventType.KeyUp: {
							if (MapVirtualKeyToButtonCode(ev.VirtualKeyCode, out ButtonCode virtualCode)) {
								ButtonCode scancode = virtualCode;
								if (scancode != ButtonCode.None)
									PostButtonReleasedEvent(InputEventType.IE_ButtonReleased, LastSampleTick, scancode, virtualCode);
							}
						}
						break;
					case WindowEventType.MouseButtonDown: {
							UpdateMouseButtonState(ev.MouseButtonFlags, ev.MouseClickCount > 1 ? ev.MouseButton switch {
								MouseButton.Right => ButtonCode.MouseRight,
								MouseButton.Middle => ButtonCode.MouseMiddle,
								MouseButton.Button4 => ButtonCode.Mouse4,
								MouseButton.Button5 => ButtonCode.Mouse5,
								MouseButton.Left or _ => ButtonCode.MouseLeft,
							} : ButtonCode.Invalid);
						}
						break;
					case WindowEventType.MouseButtonUp:
						UpdateMouseButtonState(ev.MouseButtonFlags);
						break;

					case WindowEventType.MouseMove:
						UpdateMousePositionState(InputState, (short)ev.MousePos[0], (short)ev.MousePos[1]);

						InputEvent newEvent = new();
						newEvent.Tick = GetPollTick();
						newEvent.Type = InputEventType.Gui_LocateMouseClick;
						newEvent.Data = (short)ev.MousePos[0];
						newEvent.Data2 = (short)ev.MousePos[1];
						PostUserEvent(newEvent);

						break;
					case WindowEventType.MouseScroll:
						ButtonCode code = (short)ev.MousePos[1] > 0 ? ButtonCode.MouseWheelUp : ButtonCode.MouseWheelDown;
						state.ButtonPressedTick[(int)code] = state.ButtonReleasedTick[(int)code] = LastSampleTick;
						PostEvent(InputEventType.IE_ButtonPressed, LastSampleTick, (int)code, (int)code);
						PostEvent(InputEventType.IE_ButtonReleased, LastSampleTick, (int)code, (int)code);

						state.AnalogDelta[(int)AnalogCode.MouseWheel] = (int)ev.MousePos[1];
						state.AnalogValue[(int)AnalogCode.MouseWheel] += state.AnalogDelta[(int)AnalogCode.MouseWheel];
						PostEvent(InputEventType.IE_AnalogValueChanged, LastSampleTick, (int)AnalogCode.MouseWheel, state.AnalogValue[(int)AnalogCode.MouseWheel], state.AnalogDelta[(int)AnalogCode.MouseWheel]);
						break;

					case WindowEventType.AppQuit:
						PostEvent(InputEventType.System_Quit, LastSampleTick, 0, 0, 0);
						break;
				}
			}
		}
	}

	private void ResetInputState() {
		ReleaseAllButtons();
		ZeroAnalogState(0, (int)AnalogCode.Last - 1);
	}

	private void ZeroAnalogState(int firstState, int lastState) {
		InputState state = InputState;
		memset(state.AnalogDelta.AsSpan()[firstState..lastState], 0);
		memset(state.AnalogValue.AsSpan()[firstState..lastState], 0);
	}

	private void ReleaseAllButtons(int firstButton = 0, int lastButton = (int)ButtonCode.Last - 1) {
		for (int i = firstButton; i <= lastButton; ++i) 
			PostButtonReleasedEvent(InputEventType.IE_ButtonReleased, LastSampleTick, (ButtonCode)i, (ButtonCode)i);
	}

	private void UpdateMousePositionState(InputState state, short x, short y) {
		int nOldX = state.AnalogValue[(int)AnalogCode.MouseX];
		int nOldY = state.AnalogValue[(int)AnalogCode.MouseY];

		state.AnalogValue[(int)AnalogCode.MouseX] = x;
		state.AnalogValue[(int)AnalogCode.MouseY] = y;

		int nDeltaX = x - nOldX;
		int nDeltaY = y - nOldY;

		state.AnalogDelta[(int)AnalogCode.MouseX] = nDeltaX;
		state.AnalogDelta[(int)AnalogCode.MouseY] = nDeltaY;

		if (nDeltaX != 0)
			PostEvent(InputEventType.IE_AnalogValueChanged, LastSampleTick, (int)AnalogCode.MouseX, x, nDeltaX);

		if (nDeltaY != 0)
			PostEvent(InputEventType.IE_AnalogValueChanged, LastSampleTick, (int)AnalogCode.MouseY, y, nDeltaY);

		if (nDeltaX != 0 || nDeltaY != 0)
			PostEvent(InputEventType.IE_AnalogValueChanged, LastSampleTick, (int)AnalogCode.MouseXY, x, y);
	}

	private void UpdateMouseButtonState(MouseButton buttonMask, ButtonCode dblClickCode = ButtonCode.Invalid) {
		for (int i = 0; i < 5; ++i) {
			ButtonCode code = ButtonCode.MouseFirst + i;
			bool down = ((int)buttonMask & (1 << i)) != 0;
			if (down) {
				InputEventType type = code != dblClickCode ? InputEventType.IE_ButtonPressed : InputEventType.IE_ButtonDoubleClicked;
				PostButtonPressedEvent(type, LastSampleTick, code, code);
			}
			else
				PostButtonReleasedEvent(InputEventType.IE_ButtonReleased, LastSampleTick, code, code);
		}
	}

	public void PostEvent(InputEventType type, int tick, int data = 0, int data2 = 0, int data3 = 0) {
		InputEvent ev = new();
		ev.Type = type;
		ev.Tick = tick;
		ev.Data = data;
		ev.Data2 = data2;
		ev.Data3 = data3;
		PostUserEvent(ev);
	}

	public void PostUserEvent(in InputEvent ev) {
		InputState state = InputState;
		state.Events.Enqueue(ev);
		state.Dirty = true;
	}

	public ButtonCode ScanCodeToButtonCode(int param) {
		throw new NotImplementedException();
	}

	public void SetConsoleTextMode(bool consoleTextMode) {
		throw new NotImplementedException();
	}

	public unsafe void GetCursorPosition(out int x, out int y) {
		float fx, fy;
		SDL3.SDL_GetMouseState(&fx, &fy);
		x = (int)fx;
		y = (int)fy;
	}

	public unsafe void SetCursorPosition(int x, int y) => SDL3.SDL_WarpMouseInWindow((SDL_Window*)Window!.GetHandle(), x, y);

	public ButtonCode VirtualKeyToButtonCode(int virtualKey) {
		throw new NotImplementedException();
	}

	static string[] ButtonCodeName ={
		"",				
		"0",			
		"1",			
		"2",			
		"3",			
		"4",			
		"5",			
		"6",			
		"7",			
		"8",			
		"9",			
		"a",			
		"b",			
		"c",			
		"d",			
		"e",			
		"f",			
		"g",			
		"h",			
		"i",			
		"j",			
		"k",			
		"l",			
		"m",			
		"n",			
		"o",			
		"p",			
		"q",			
		"r",			
		"s",			
		"t",			
		"u",			
		"v",			
		"w",			
		"x",			
		"y",			
		"z",			
		"KP_INS",		
		"KP_END",		
		"KP_DOWNARROW",	
		"KP_PGDN",		
		"KP_LEFTARROW",	
		"KP_5",			
		"KP_RIGHTARROW",
		"KP_HOME",		
		"KP_UPARROW",	
		"KP_PGUP",		
		"KP_SLASH",		
		"KP_MULTIPLY",	
		"KP_MINUS",		
		"KP_PLUS",		
		"KP_ENTER",		
		"KP_DEL",		
		"[",			
		"]",			
		"SEMICOLON",	
		"'",			
		"`",			
		",",			
		".",			
		"/",			
		"\\",			
		"-",			
		"=",			
		"ENTER",		
		"SPACE",		
		"BACKSPACE",	
		"TAB",			
		"CAPSLOCK",		
		"NUMLOCK",		
		"ESCAPE",		
		"SCROLLLOCK",	
		"INS",			
		"DEL",			
		"HOME",			
		"END",			
		"PGUP",			
		"PGDN",			
		"PAUSE",		
		"SHIFT",		
		"RSHIFT",		
		"ALT",			
		"RALT",			
		"CTRL",			
		"RCTRL",		
		"LWIN",			
		"RWIN",			
		"APP",			
		"UPARROW",		
		"LEFTARROW",	
		"DOWNARROW",	
		"RIGHTARROW",	
		"F1",			
		"F2",			
		"F3",			
		"F4",			
		"F5",			
		"F6",			
		"F7",			
		"F8",			
		"F9",			
		"F10",			
		"F11",			
		"F12",			

		"CAPSLOCKTOGGLE",	
		"NUMLOCKTOGGLE",	
		"SCROLLLOCKTOGGLE",

		"MOUSE1",		
		"MOUSE2",		
		"MOUSE3",		
		"MOUSE4",		
		"MOUSE5",		

		"MWHEELUP",		
		"MWHEELDOWN",	
	};

	public ButtonCode StringToButtonCode(ReadOnlySpan<char> str) {
		if (str.IsEmpty || str.Length <=0)
			return ButtonCode.Invalid;

		for (ButtonCode i = 0; i < ButtonCode.Last; ++i) 
			if (str.Equals(ButtonCodeName[(int)i], StringComparison.OrdinalIgnoreCase))
				return i;
		
		return ButtonCode.Invalid;
	}

	public ReadOnlySpan<char> ButtonCodeToString(ButtonCode code) {
		return ButtonCodeName[(int)code];
	}
}
