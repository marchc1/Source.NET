using Source.Common.Bitmap;

using System.Runtime.CompilerServices;

namespace Source.Common.MaterialSystem;
[Flags]
public enum PixelWriterUsing
{
	Float = 0x01,
	Float16 = 0x02,
	SwapBytes = 0x04
}
public unsafe struct PixelWriterState
{
	public ImageFormat Format;
	public int Bits;
	public byte Size;
	public int BytesPerRow;
	public byte Flags;
	public short RShift;
	public short GShift;
	public short BShift;
	public short AShift;
	public uint RMask;
	public uint GMask;
	public uint BMask;
	public uint AMask;

	internal void SetFormat(ImageFormat format, uint stride) {
		Format = format;
		BytesPerRow = (int)stride;
		switch (format) {
			case ImageFormat.R32F:
				Size = 4;
				RShift = 0;
				GShift = 0;
				BShift = 0;
				AShift = 0;
				RMask = 0xFFFFFFFF;
				GMask = 0x0;
				BMask = 0x0;
				AMask = 0x0;
				Flags |= (int)PixelWriterUsing.Float;
				break;

			case ImageFormat.RGBA32323232F:
				Size = 16;
				RShift = 0;
				GShift = 32;
				BShift = 64;
				AShift = 96;
				RMask = 0xFFFFFFFF;
				GMask = 0xFFFFFFFF;
				BMask = 0xFFFFFFFF;
				AMask = 0xFFFFFFFF;
				Flags |= (int)PixelWriterUsing.Float;
				break;

			case ImageFormat.RGBA16161616F:
				Size = 8;
				RShift = 0;
				GShift = 16;
				BShift = 32;
				AShift = 48;
				RMask = 0xFFFF;
				GMask = 0xFFFF;
				BMask = 0xFFFF;
				AMask = 0xFFFF;
				Flags |= (int)PixelWriterUsing.Float | (int)PixelWriterUsing.Float16;
				break;

			case ImageFormat.RGBA8888:
				Size = 4;
				RShift = 0;
				GShift = 8;
				BShift = 16;
				AShift = 24;
				RMask = 0xFF;
				GMask = 0xFF;
				BMask = 0xFF;
				AMask = 0xFF;
				break;

			case ImageFormat.BGRA8888:
				Size = 4;
				RShift = 16;
				GShift = 8;
				BShift = 0;
				AShift = 24;
				RMask = 0xFF;
				GMask = 0xFF;
				BMask = 0xFF;
				AMask = 0xFF;
				break;

			case ImageFormat.BGRX8888:
				Size = 4;
				RShift = 16;
				GShift = 8;
				BShift = 0;
				AShift = 24;
				RMask = 0xFF;
				GMask = 0xFF;
				BMask = 0xFF;
				AMask = 0x00;
				break;

			case ImageFormat.BGRA4444:
				Size = 2;
				RShift = 4;
				GShift = 0;
				BShift = -4;
				AShift = 8;
				RMask = 0xF0;
				GMask = 0xF0;
				BMask = 0xF0;
				AMask = 0xF0;
				break;

			case ImageFormat.BGR888:
				Size = 3;
				RShift = 16;
				GShift = 8;
				BShift = 0;
				AShift = 0;
				RMask = 0xFF;
				GMask = 0xFF;
				BMask = 0xFF;
				AMask = 0x00;
				break;

			case ImageFormat.BGR565:
				Size = 2;
				RShift = 8;
				GShift = 3;
				BShift = -3;
				AShift = 0;
				RMask = 0xF8;
				GMask = 0xFC;
				BMask = 0xF8;
				AMask = 0x00;
				break;

			case ImageFormat.BGRA5551:
			case ImageFormat.BGRX5551:
				Size = 2;
				RShift = 7;
				GShift = 2;
				BShift = -3;
				AShift = 8;
				RMask = 0xF8;
				GMask = 0xF8;
				BMask = 0xF8;
				AMask = 0x80;
				break;

			case ImageFormat.A8:
				Size = 1;
				RShift = 0;
				GShift = 0;
				BShift = 0;
				AShift = 0;
				RMask = 0x00;
				GMask = 0x00;
				BMask = 0x00;
				AMask = 0xFF;
				break;

			case ImageFormat.UVWQ8888:
				AssertMsg(false, "What????");
				Size = 4;
				RShift = 0;
				GShift = 8;
				BShift = 16;
				AShift = 24;
				RMask = 0xFF;
				GMask = 0xFF;
				BMask = 0xFF;
				AMask = 0xFF;
				break;

			case ImageFormat.RGBA16161616:
				Size = 8;
				RShift = 0;
				GShift = 16;
				BShift = 32;
				AShift = 48;
				RMask = 0xFFFF;
				GMask = 0xFFFF;
				BMask = 0xFFFF;
				AMask = 0xFFFF;
				break;

			case ImageFormat.I8:
				// whatever goes into R is considered the intensity.
				Size = 1;
				RShift = 0;
				GShift = 0;
				BShift = 0;
				AShift = 0;
				RMask = 0xFF;
				GMask = 0x00;
				BMask = 0x00;
				AMask = 0x00;
				break;
			// FIXME: Add more color formats as need arises
			default: {
					if (!format_error_printed[(int)format]) {
						Assert(false);
						Msg($"PixelWriter.SetPixelMemory:  Unsupported image format {format}\n");
						format_error_printed[(int)format] = true;
					}
					Size = 0;
					RShift = 0;
					GShift = 0;
					BShift = 0;
					AShift = 0;
					RMask = 0x00;
					GMask = 0x00;
					BMask = 0x00;
					AMask = 0x00;
				}
				break;
		}
	}
	static bool[] format_error_printed = new bool[(int)ImageFormat.Count];
}

public ref struct PixelWriter
{
	Span<byte> Base;
	PixelWriterState State;

	/// <summary>
	/// Allows reuse of the internal state structure if we lose control of the pixel writer ref struct
	/// </summary>
	/// <param name="state"></param>
	public readonly void Export(out PixelWriterState state, out Span<byte> memory) {
		state = State;
		memory = Base;
	}


	/// <summary>
	/// Allows reuse of the internal state structure if we lose control of the pixel writer ref struct
	/// </summary>
	/// <param name="state"></param>
	public void Import(in PixelWriterState state, in Span<byte> memory) {
		State = state;
		Base = memory;
	}


	public void SetPixelMemory(ImageFormat format, in Span<byte> memory, int stride) {
		State.SetFormat(format, (uint)stride);
		Base = memory;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Seek(int x, int y) {
		State.Bits = y * State.BytesPerRow + x * State.Size;
	}

	public void Dispose() {
		Base = null;
		State = default;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void WritePixel(int r, int g, int b, int a) {
		WritePixelNoAdvance(r, g, b, a);
		State.Bits += State.Size;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe void WritePixelNoAdvance(int r, int g, int b, int a) {
		if (State.Size <= 0) return;

		uint rmask = State.RMask, gmask = State.GMask, bmask = State.BMask, amask = State.AMask;
		short rshift = State.RShift, gshift = State.GShift, bshift = State.BShift, ashift = State.AShift;

		fixed (byte* pBits = Base) {
			byte* bits = pBits + State.Bits;
			if (State.Size < 5) {
				uint val = (uint)((r & rmask) << rshift);
				val |= (uint)((g & gmask) << gshift);
				val |= (uint)((bshift > 0) ? ((b & bmask) << bshift) : ((b & bmask) >> -bshift));
				val |= (uint)((a & amask) << ashift);

				switch (State.Size) {
					default:
						Assert(false);
						return;
					case 1: {
							bits[0] = (byte)(val & 0xff);
							return;
						}
					case 2: {
							((ushort*)bits)[0] = (ushort)(val & 0xffff);
							return;
						}
					case 3: {
							((ushort*)bits)[0] = (ushort)(val & 0xffff);
							bits[2] = (byte)((val >> 16) & 0xff);
							return;
						}
					case 4: {
							((uint*)bits)[0] = val;
							return;
						}
				}
			}
			else {
				long val = (r & rmask) << rshift;
				val |= (g & gmask) << gshift;
				val |= (bshift > 0) ? ((b & bmask) << bshift) : ((b & bmask) >> -bshift);
				val |= (a & amask) << ashift;

				switch (State.Size) {
					case 6: {
							((uint*)bits)[0] = (uint)(val & 0xffffffff);
							((ushort*)bits)[2] = (ushort)((val >> 32) & 0xffff);

							return;
						}
					case 8: {
							((uint*)bits)[0] = (uint)(val & 0xffffffff);
							((uint*)bits)[1] = (uint)((val >> 32) & 0xffffffff);
							return;
						}
					default:
						Assert(false);
						return;
				}
			}
		}
	}

	public void WritePixel(in Color color) => WritePixel(color.R, color.G, color.B, color.A);
}