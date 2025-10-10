using System.Runtime.CompilerServices;

namespace Source.Common.Input;

public enum ButtonCode
{
	Invalid = -1,
	None = 0,

	KeyFirst = 0,
	KeyNone = KeyFirst,
	Key0,
	Key1,
	Key2,
	Key3,
	Key4,
	Key5,
	Key6,
	Key7,
	Key8,
	Key9,
	KeyA,
	KeyB,
	KeyC,
	KeyD,
	KeyE,
	KeyF,
	KeyG,
	KeyH,
	KeyI,
	KeyJ,
	KeyK,
	KeyL,
	KeyM,
	KeyN,
	KeyO,
	KeyP,
	KeyQ,
	KeyR,
	KeyS,
	KeyT,
	KeyU,
	KeyV,
	KeyW,
	KeyX,
	KeyY,
	KeyZ,
	KeyPad0,
	KeyPad1,
	KeyPad2,
	KeyPad3,
	KeyPad4,
	KeyPad5,
	KeyPad6,
	KeyPad7,
	KeyPad8,
	KeyPad9,
	KeyPadDivide,
	KeyPadMultiply,
	KeyPadMinus,
	KeyPadPlus,
	KeyPadEnter,
	KeyPadDecimal,
	KeyLBracket,
	KeyRBracket,
	KeySemicolon,
	KeyApostrophe,
	KeyBackquote,
	KeyComma,
	KeyPeriod,
	KeySlash,
	KeyBackslash,
	KeyMinus,
	KeyEqual,
	KeyEnter,
	KeySpace,
	KeyBackspace,
	KeyTab,
	KeyCapsLock,
	KeyNumLock,
	KeyEscape,
	KeyScrollLock,
	KeyInsert,
	KeyDelete,
	KeyHome,
	KeyEnd,
	KeyPageUp,
	KeyPageDown,
	KeyBreak,
	KeyLShift,
	KeyRShift,
	KeyLAlt,
	KeyRAlt,
	KeyLControl,
	KeyRControl,
	KeyLWin,
	KeyRWin,
	KeyApp,
	KeyUp,
	KeyLeft,
	KeyDown,
	KeyRight,
	KeyF1,
	KeyF2,
	KeyF3,
	KeyF4,
	KeyF5,
	KeyF6,
	KeyF7,
	KeyF8,
	KeyF9,
	KeyF10,
	KeyF11,
	KeyF12,
	KeyCapsLockToggle,
	KeyNumLockToggle,
	KeyScrollLockToggle,

	KeyLast = KeyScrollLockToggle,
	KeyCount = KeyLast - KeyFirst + 1,

	MouseFirst = KeyLast + 1,

	MouseLeft = MouseFirst,
	MouseRight,
	MouseMiddle,
	Mouse4,
	Mouse5,
	MouseWheelUp,     // A fake button which is 'pressed' and 'released' when the wheel is moved up 
	MouseWheelDown,   // A fake button which is 'pressed' and 'released' when the wheel is moved down

	MouseLast = MouseWheelDown,
	MouseCount = MouseLast - MouseFirst + 1,

	Last = MouseLast + 1,
	Count
}
public static class ButtonCodeExts {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsAlpha(this ButtonCode code) {
		return (code >= ButtonCode.KeyA) && (code <= ButtonCode.KeyZ);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsAlphaNumeric(this ButtonCode code) {
		return (code >= ButtonCode.Key0) && (code <= ButtonCode.KeyZ);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsSpace(this ButtonCode code) {
		return (code == ButtonCode.KeyEnter) || (code == ButtonCode.KeyTab) || (code == ButtonCode.KeySpace);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsKeypad(this ButtonCode code) {
		return (code >= ButtonCode.KeyPad0) && (code <= ButtonCode.KeyPadDecimal);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ButtonCode GetBaseButtonCode(this ButtonCode code) {
		return code;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsPunctuation(this ButtonCode code) {
		return (code >= ButtonCode.Key0) && (code <= ButtonCode.KeySpace) && !IsAlphaNumeric(code) && !IsSpace(code) && !IsKeypad(code);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsKeyCode(this ButtonCode code) {
		return (code >= ButtonCode.KeyFirst) && (code <= ButtonCode.KeyLast);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsMouseCode(this ButtonCode code) {
		return (code >= ButtonCode.MouseFirst) && (code <= ButtonCode.MouseLast);
	}
}
