using Game.Shared;

using Source.Common.Mathematics;
using Source.Engine;

using System.Numerics;

namespace Game.Client;

public static class LerpFunctions
{
	// Apparently, the C# JIT is smart enough to recognize both that the branches must be collapsed and that
	// the boxing is worthless, so this should compile fine without slow branches/object boxing, and is only
	// done for the sake of the compiler to build the IL. I do wish though that C# had a way to explicitly express
	// this behavior.

	// It really does suck that I have to copy all this too. I just wish it could be a template like C++. Using things like INumber
	// aren't applicable it seems because of things like Vector3 being outside of our control.

#pragma warning disable CS8605 // Unboxing a possibly null value.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.

	private static T TEMPLATE<T>(double fraction, in T start, in T end) {
		if (typeof(T) == typeof(float)) {
			float from = (float)(object)start;
			float to = (float)(object)end;
			throw new NotImplementedException();
		}
		else if (typeof(T) == typeof(double)) {
			double from = (double)(object)start;
			double to = (double)(object)end;
			throw new NotImplementedException();
		}
		else if (typeof(T) == typeof(Vector3)) {
			Vector3 from = (Vector3)(object)start;
			Vector3 to = (Vector3)(object)end;
			throw new NotImplementedException();
		}
		else if (typeof(T) == typeof(QAngle)) {
			QAngle from = (QAngle)(object)start;
			QAngle to = (QAngle)(object)end;
			throw new NotImplementedException();
		}
		else
			throw new NotImplementedException();
	}

	public static T Lerp<T>(double fraction, in T start, in T end) {
		if (typeof(T) == typeof(float))
			return (T)(object)float.Lerp((float)(object)start, (float)(object)end, (float)fraction);
		else if (typeof(T) == typeof(double))
			return (T)(object)double.Lerp((double)(object)start, (double)(object)end, fraction);
		else if (typeof(T) == typeof(Vector3))
			return (T)(object)Vector3.Lerp((Vector3)(object)start, (Vector3)(object)end, (float)fraction);
		else if (typeof(T) == typeof(QAngle))
			return (T)(object)QAngle.Lerp((QAngle)(object)start, (QAngle)(object)end, (float)fraction);
		else if (typeof(T) == typeof(AnimationLayer))
			return (T)(object)AnimationLayer.Lerp(fraction, (AnimationLayer)(object)start, (AnimationLayer)(object)end);
		else
			throw new NotImplementedException();
	}

	// I don't know what this does yet
	public static T Lerp_Clamp<T>(in T start) => start;

	public static T LoopingLerp<T>(double fraction, in T start, in T end) {
		if (typeof(T) == typeof(float)) {
			float from = (float)(object)start;
			float to = (float)(object)end;

			if (MathF.Abs(to - from) > 0.5f) {
				if (from < to)
					from += 1.0f;
				else
					to += 1.0f;
			}

			float s = to * (float)fraction + from * (1.0f - (float)fraction);
			s = s - (int)s;
			if (s < 0.0f)
				s += 1.0f;
			return (T)(object)s;
		}
		else if (typeof(T) == typeof(double)) {
			double from = (double)(object)start;
			double to = (double)(object)end;

			if (Math.Abs(to - from) > 0.5) {
				if (from < to)
					from += 1.0;
				else
					to += 1.0;
			}

			double s = to * fraction + from * (1.0 - fraction);
			s = s - (long)s;
			if (s < 0.0)
				s += 1.0;
			return (T)(object)s;
		}
		else if (typeof(T) == typeof(AnimationLayer))
			return (T)(object)AnimationLayer.LoopingLerp(fraction, (AnimationLayer)(object)start, (AnimationLayer)(object)end);
		else
			throw new NotImplementedException();
	}

	public static T LoopingLerp_Hermite<T>(double fraction, in T _p0, in T _p1, in T _p2) {
		if (typeof(T) == typeof(float)) {
			float p0 = (float)(object)_p0;
			float p1 = (float)(object)_p1;
			float p2 = (float)(object)_p2;

			if (MathF.Abs(p1 - p0) > 0.5f) {
				if (p0 < p1)
					p0 += 1.0f;
				else
					p1 += 1.0f;
			}

			if (MathF.Abs(p2 - p1) > 0.5f) {
				if (p1 < p2) {
					p1 += 1.0f;

					if (MathF.Abs(p1 - p0) > 0.5) {
						if (p0 < p1)
							p0 += 1.0f;
						else
							p1 += 1.0f;
					}
				}
				else {
					p2 += 1.0f;
				}
			}

			float s = Lerp_Hermite(fraction, in p0, in p1, in p2);

			s = s - (int)(s);
			if (s < 0.0f) {
				s = s + 1.0f;
			}

			return (T)(object)s;
		}
		else if (typeof(T) == typeof(AnimationLayer))
			return (T)(object)AnimationLayer.LoopingLerp_Hermite(fraction, (AnimationLayer)(object)_p0, (AnimationLayer)(object)_p1, (AnimationLayer)(object)_p2);
		else
			return Lerp_Hermite(fraction, in _p0, in _p1, in _p2);
	}
	public static T Lerp_Hermite<T>(double fraction, in T _p0, in T _p1, in T _p2) {
		if (typeof(T) == typeof(float)) {
			float p0 = (float)(object)_p0;
			float p1 = (float)(object)_p1;
			float p2 = (float)(object)_p2;
			float t = (float)fraction;

			float d1 = p1 - p0;
			float d2 = p2 - p1;

			float output;
			float tSqr = t * t;
			float tCube = t * tSqr;

			output = p1 * (2 * tCube - 3 * tSqr + 1);
			output += p2 * (-2 * tCube + 3 * tSqr);
			output += d1 * (tCube - 2 * tSqr + t);
			output += d2 * (tCube - tSqr);

			return (T)(object)output;
		}
		else if (typeof(T) == typeof(double)) {
			double p0 = (double)(object)_p0;
			double p1 = (double)(object)_p1;
			double p2 = (double)(object)_p2;
			double t = (double)fraction;

			double d1 = p1 - p0;
			double d2 = p2 - p1;

			double output;
			double tSqr = t * t;
			double tCube = t * tSqr;

			output = p1 * (2 * tCube - 3 * tSqr + 1);
			output += p2 * (-2 * tCube + 3 * tSqr);
			output += d1 * (tCube - 2 * tSqr + t);
			output += d2 * (tCube - tSqr);

			return (T)(object)output;
		}
		else if (typeof(T) == typeof(Vector3)) {
			Vector3 p0 = (Vector3)(object)_p0;
			Vector3 p1 = (Vector3)(object)_p1;
			Vector3 p2 = (Vector3)(object)_p2;
			float t = (float)fraction;

			Vector3 d1 = p1 - p0;
			Vector3 d2 = p2 - p1;

			Vector3 output;
			float tSqr = t * t;
			float tCube = t * tSqr;

			output = p1 * (2 * tCube - 3 * tSqr + 1);
			output += p2 * (-2 * tCube + 3 * tSqr);
			output += d1 * (tCube - 2 * tSqr + t);
			output += d2 * (tCube - tSqr);

			return (T)(object)output;
		}
		else if (typeof(T) == typeof(AnimationLayer))
			return (T)(object)AnimationLayer.Lerp_Hermite(fraction, (AnimationLayer)(object)_p0, (AnimationLayer)(object)_p1, (AnimationLayer)(object)_p2);
		else if (typeof(T) == typeof(QAngle)) {
			// Can't do hermite with QAngles, get discontinuities, just do a regular interpolation
			return Lerp(fraction, in _p1, in _p2);
		}
		else
			throw new NotImplementedException();
	}

	public static T Derivative_Hermite<T>(double fraction, in T _p0, in T _p1, in T _p2) {
		if (typeof(T) == typeof(float)) {
			float p0 = (float)(object)_p0;
			float p1 = (float)(object)_p1;
			float p2 = (float)(object)_p2;
			float t = (float)fraction;

			float d1 = p1 - p0;
			float d2 = p2 - p1;

			float output;
			float tSqr = t * t;

			output = p1 * (6 * tSqr - 6 * t);
			output += p2 * (-6 * tSqr + 6 * t);
			output += d1 * (3 * tSqr - 4 * t + 1);
			output += d2 * (3 * tSqr - 2 * t);

			return (T)(object)output;
		}
		else if (typeof(T) == typeof(double)) {
			double p0 = (double)(object)_p0;
			double p1 = (double)(object)_p1;
			double p2 = (double)(object)_p2;
			double t = (double)fraction;

			double d1 = p1 - p0;
			double d2 = p2 - p1;

			double output;
			double tSqr = t * t;

			output = p1 * (6 * tSqr - 6 * t);
			output += p2 * (-6 * tSqr + 6 * t);
			output += d1 * (3 * tSqr - 4 * t + 1);
			output += d2 * (3 * tSqr - 2 * t);

			return (T)(object)output;
		}
		else if (typeof(T) == typeof(Vector3)) {
			Vector3 p0 = (Vector3)(object)_p0;
			Vector3 p1 = (Vector3)(object)_p1;
			Vector3 p2 = (Vector3)(object)_p2;
			float t = (float)fraction;

			Vector3 d1 = p1 - p0;
			Vector3 d2 = p2 - p1;

			Vector3 output;
			float tSqr = t * t;

			output = p1 * (6 * tSqr - 6 * t);
			output += p2 * (-6 * tSqr + 6 * t);
			output += d1 * (3 * tSqr - 4 * t + 1);
			output += d2 * (3 * tSqr - 2 * t);

			return (T)(object)output;
		}
		else if (typeof(T) == typeof(QAngle)) {
			QAngle p0 = (QAngle)(object)_p0;
			QAngle p1 = (QAngle)(object)_p1;
			QAngle p2 = (QAngle)(object)_p2;
			float t = (float)fraction;

			QAngle d1 = p1 - p0;
			QAngle d2 = p2 - p1;

			QAngle output;
			float tSqr = t * t;

			output = p1 * (6 * tSqr - 6 * t);
			output += p2 * (-6 * tSqr + 6 * t);
			output += d1 * (3 * tSqr - 4 * t + 1);
			output += d2 * (3 * tSqr - 2 * t);

			return (T)(object)output;
		}
		else
			throw new NotImplementedException();
	}
}
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning restore CS8605 // Unboxing a possibly null value.

/*
	To elaborate on the initial comment, here is a test snippet and the LINQPad output:

	C#:
		Console.WriteLine(Test<float>.Lerp(0.2, 0f, 1f));

		public static class Test<T>{
			public static T Lerp(double fraction, in T start, in T end) {
				if (typeof(T) == typeof(float))
					return (T)(object)float.Lerp((float)(object)start, (float)(object)end, (float)fraction);
				else
					throw new NotImplementedException();
			}
		}

	MSIL (.NET 9.0)
		Main ()
			IL_0000	nop	
			IL_0001	ldc.r8	9A 99 99 99 99 99 C9 3F  // 0.2
			IL_000A	ldc.r4	00 00 00 00  // 0
			IL_000F	stloc.0	
			IL_0010	ldloca.s	00 
			IL_0012	ldc.r4	00 00 80 3F  // 1
			IL_0017	stloc.1	
			IL_0018	ldloca.s	01 
			IL_001A	call	Test <Single>.Lerp (Double, Single&, Single&)
			IL_001F	call	Console.WriteLine (Single)
			IL_0024	nop	
			IL_0025	ret	
		Test <Object>.Lerp (Double, Object&, Object&)
			IL_0000	nop	
			IL_0001	ldtoken	Object
			IL_0006	call	Type.GetTypeFromHandle (RuntimeTypeHandle)
			IL_000B	ldtoken	Single
			IL_0010	call	Type.GetTypeFromHandle (RuntimeTypeHandle)
			IL_0015	call	Type.op_Equality (Type, Type)
			IL_001A	stloc.0	
			IL_001B	ldloc.0	
			IL_001C	brfalse.s	IL_0052
			IL_001E	ldarg.1	
			IL_001F	ldobj	Object
			IL_0024	box	Object
			IL_0029	unbox.any	Single
			IL_002E	ldarg.2	
			IL_002F	ldobj	Object
			IL_0034	box	Object
			IL_0039	unbox.any	Single
			IL_003E	ldarg.0	
			IL_003F	conv.r4	
			IL_0040	call	Single.Lerp (Single, Single, Single)
			IL_0045	box	Single
			IL_004A	unbox.any	Object
			IL_004F	stloc.1	
			IL_0050	br.s	IL_0058
			IL_0052	newobj	NotImplementedException..ctor
			IL_0057	throw	
			IL_0058	ldloc.1	
			IL_0059	ret	

	X64 ASM
		Main ()
			L0000	push	rbp
			L0001	sub	rsp, 0x30
			L0005	lea	rbp, [rsp+0x30]
			L000a	xor	eax, eax
			L000c	mov	[rbp-4], eax
			L000f	mov	[rbp-8], eax
			L0012	mov	[rbp+0x10], rcx
			L0016	cmp	dword ptr [0x7ff953d5c258], 0
			L001d	je	short L0024
			L001f	call	0x00007ff9b329a200
			L0024	nop	
			L0025	xor	eax, eax
			L0027	mov	[rbp-4], eax
				// call	Test <Single>.Lerp (Double, Single&, Single&)
			L002a	mov	dword ptr [rbp-8], 0x3f800000
			L0031	lea	r8, [rbp-8]
			L0035	vmovsd	xmm0, [UserQuery.Main()]
			L003d	lea	rdx, [rbp-4]
			L0041	call	0x00007ff9540d0018
			L0046	vmovss	[rbp-0xc], xmm0
				// call	Console.WriteLine (Single)
			L004b	vmovss	xmm0, [rbp-0xc]
			L0050	call	qword ptr [0x7ff9540ac258]
			L0056	nop	
			L0057	nop	
			L0058	add	rsp, 0x30
			L005c	pop	rbp
			L005d	ret	
*/
