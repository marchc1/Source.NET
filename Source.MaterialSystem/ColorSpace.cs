using Source.Common.Mathematics;

using System.Numerics;

namespace Source.MaterialSystem;

public static class ColorSpace
{
	static float[] textureToLinear = new float[256];  // texture (0..255) to linear (0..1)
	static int[] linearToTexture = new int[1024];   // linear (0..1) to texture (0..255)
	static int[] linearToScreen = new int[1024];    // linear (0..1) to gamma corrected vertex light (0..255)
	static float[] g_LinearToVertex = new float[4096];   // linear (0..4) to screen corrected vertex space (0..1?)
	static int[] linearToLightmap = new int[4096];  // linear (0..4) to screen corrected texture value (0..255)

	public static void LinearToLightmap(Span<byte> pDstRGB, ReadOnlySpan<float> pSrcRGB) {
		Vector3 tmpVect = default;
		int i, j;
		for (j = 0; j < 3; j++) {
			i = MathLib.RoundFloatToInt(pSrcRGB[j] * 1024); // assume 0..4 range
			if (i < 0) {
				i = 0;
			}
			if (i > 4091)
				i = 4091;
			tmpVect[j] = g_LinearToVertex[i];
		}

		MathLib.ColorClamp(ref tmpVect);

		pDstRGB[0] = MathLib.RoundFloatToByte(tmpVect[0] * 255.0f);
		pDstRGB[1] = MathLib.RoundFloatToByte(tmpVect[1] * 255.0f);
		pDstRGB[2] = MathLib.RoundFloatToByte(tmpVect[2] * 255.0f);
	}

	internal static void SetGamma(float screenGamma, float texGamma, float overbright, bool allowCheats, bool linearFrameBuffer) {
		int i, inf;
		float g1, g3;
		float g;
		float brightness = 0.0f; // This used to be configurable. . hardcode to 0.0

		if (linearFrameBuffer) {
			screenGamma = 1.0f;
		}

		g = screenGamma;

		// clamp values to prevent cheating in multiplayer
		if (!allowCheats) {
			if (brightness > 2.0f)
				brightness = 2.0f;

			if (g < 1.8f)
				g = 1.8f;
		}

		if (g > 3.0f)
			g = 3.0f;

		g = 1.0f / g;
		g1 = texGamma * g;

		// pow( textureColor, g1 ) converts from on-disk texture space to framebuffer space

		if (brightness <= 0.0f) {
			g3 = 0.125f;
		}
		else if (brightness > 1.0f) {
			g3 = 0.05f;
		}
		else {
			g3 = 0.125f - (brightness * brightness) * 0.075f;
		}

		for (i = 0; i < 1024; i++) {
			float f1;

			f1 = i / 1023.0f;

			if (brightness > 1.0f)
				f1 *= brightness;

			if (f1 <= g3)
				f1 = (f1 / g3) * 0.125f;
			else
				f1 = 0.125f + ((f1 - g3) / (1.0f - g3)) * 0.875f;

			inf = (int)(255 * MathF.Pow(f1, g));

			if (inf < 0)
				inf = 0;
			if (inf > 255)
				inf = 255;
			linearToScreen[i] = inf;
		}

		for (i = 0; i < 256; i++) 
			textureToLinear[i] = MathF.Pow(i / 255.0f, texGamma);

		for (i = 0; i < 1024; i++) 
			linearToTexture[i] = (int)(MathF.Pow(i / 1023.0f, 1.0f / texGamma) * 255);

		float f, overbrightFactor;

		if (overbright == 2.0F) 
			overbrightFactor = 0.5F;
		else if (overbright == 4.0F) 
			overbrightFactor = 0.25F;
		else 
			overbrightFactor = 1.0F;

		for (i = 0; i < 4096; i++) {
			f = MathF.Pow(i / 1024.0f, 1.0f / screenGamma);

			g_LinearToVertex[i] = f * overbrightFactor;
			if (g_LinearToVertex[i] > 1)
				g_LinearToVertex[i] = 1;

			linearToLightmap[i] = (int)(f * 255 * overbrightFactor);
			if (linearToLightmap[i] > 255)
				linearToLightmap[i] = 255;
		}
	}
}
