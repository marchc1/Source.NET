namespace Source;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using static Dbg;

using ColorType = byte;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ColorRGBExp32 {
	public byte R, G, B;
	public sbyte Exponent;
}
public struct Color
{
	public ColorType R, G, B, A;

	public Color() : this(0, 0, 0){}
	public Color(int r, int g, int b) : this(r, g, b, 255){}
	public Color(int r, int g, int b, int a) => SetColor(r, g, b, a);

	public void SetColor(int _r, int _g, int _b, int _a = 0) {
		Assert(_r >= 0 && _r <= 255 &&
				_g >= 0 && _g <= 255 &&
				_b >= 0 && _b <= 255 &&
				_a >= 0 && _a <= 255
		);

		R = (ColorType)(Math.Max(Math.Min(_r, 255), 0));
		G = (ColorType)(Math.Max(Math.Min(_g, 255), 0));
		B = (ColorType)(Math.Max(Math.Min(_b, 255), 0));
		A = (ColorType)(Math.Max(Math.Min(_a, 255), 0));
	}

	public readonly void GetColor(out int r, out int g, out int b, out int a) {
		r = this.R;
		g = this.G;
		b = this.B;
		a = this.A;
	}

	public unsafe void SetRawColor(int color32) {
		int* cmp = (int*)Unsafe.AsPointer(ref this);
		*cmp = color32;
	}

	public unsafe int GetRawColor() {
		int raw = *(int*)Unsafe.AsPointer(ref this);
		return raw;
	}

	public static implicit operator System.Drawing.Color(Color self) => System.Drawing.Color.FromArgb(self.A, self.R, self.G, self.B);
	public static implicit operator int(Color self) => self.GetRawColor();
	public static implicit operator Color(int raw) {
		Color color = new Color();
		color.SetRawColor(raw);
		return color;
	}

	public unsafe ColorType this[int index] {
		get {
			return *((ColorType*)Unsafe.AsPointer(ref this) + index);
		}
		set {
			*((ColorType*)Unsafe.AsPointer(ref this) + index) = value;
		}
	}

	public override readonly string ToString() {
		return $"Color({R}, {G}, {B}, {A})";
	}

	public static bool operator==(Color l, Color r) => l.GetRawColor() == r.GetRawColor();
	public static bool operator!=(Color l, Color r) => l.GetRawColor() != r.GetRawColor();

	public override readonly bool Equals([NotNullWhen(true)] object? obj) {
		return obj is Color color && this == color;
	}

	public override int GetHashCode() => HashCode.Combine(GetRawColor());
}
