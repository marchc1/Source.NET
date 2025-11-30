using Source.Common.Mathematics;

namespace Source.MaterialSystem;

public class ImagePacker(MaterialSystem MaterialSystem)
{
	public const int MAX_MAX_LIGHTMAP_WIDTH = 2048;

	public bool Reset(int sortId, int maxLightmapWidth, int maxLightmapHeight) {
		int i;

		Assert(maxLightmapWidth <= MAX_MAX_LIGHTMAP_WIDTH);

		MaxLightmapWidth = maxLightmapWidth;
		MaxLightmapHeight = maxLightmapHeight;

		MaxBlockWidth = maxLightmapWidth + 1;
		MaxBlockHeight = maxLightmapHeight + 1;

		SortID = sortId;

		AreaUsed = 0;
		MinimumHeight = -1;
		for (i = 0; i < MaxLightmapWidth; i++) 
			LightmapWavefront[i] = -1;
		
		return true;
	}
	public bool AddBlock(int width, int height, ref int returnX, ref int returnY) {
#if ADD_ONE_TEXEL_BORDER
		width += 2;
		height += 2;
		width = Math.Clamp(width, 0, MaxLightmapWidth);
		height = Math.Clamp(height, 0, MaxLightmapHeight);
#endif
		if ((width >= MaxBlockWidth) && (height >= MaxBlockHeight)) 
			return false;

		int bestX = -1;
		int maxYIdx;
		int outerX = 0;
		int outerMinY = MaxLightmapHeight;
		int lastX = MaxLightmapWidth - width;
		int lastMaxYVal = -2;
		while (outerX <= lastX) {
			if (LightmapWavefront[outerX] == lastMaxYVal) {
				++outerX;
				continue;
			}

			maxYIdx = GetMaxYIndex(outerX, width);
			lastMaxYVal = LightmapWavefront[maxYIdx];
			if (outerMinY > lastMaxYVal) {
				outerMinY = lastMaxYVal;
				bestX = outerX;
			}
			outerX = maxYIdx + 1;
		}

		if (bestX == -1) {
			if ((width <= MaxBlockWidth) && (height <= MaxBlockHeight)) {
				MaxBlockWidth = width;
				MaxBlockHeight = height;
			}
			return false;
		}

		returnX = bestX;
		returnY = outerMinY + 1;

		if (returnY + height >= MaxLightmapHeight - 1) {
			if ((width <= MaxBlockWidth) && (height <= MaxBlockHeight)) {
				MaxBlockWidth = width;
				MaxBlockHeight = height;
			}
			return false;
		}

		if (returnY + height > MinimumHeight)
			MinimumHeight = returnY + height;

		int x;
		for (x = bestX; x < bestX + width; x++) 
			LightmapWavefront[x] = outerMinY + height;
		
		AreaUsed += width * height;
#if ADD_ONE_TEXEL_BORDER
		returnX++;
		returnY++;
#endif
		return true;
	}
	public void GetMinimumDimensions(out int returnWidth, out int returnHeight) {
		returnWidth = MathLib.CeilPow2(MaxLightmapWidth);
		returnHeight = MathLib.CeilPow2(MinimumHeight);

		int aspect = returnWidth / returnHeight;
		if (aspect > MaterialSystem.HardwareConfig.MaxTextureAspectRatio()) {
			returnHeight = returnWidth / MaterialSystem.HardwareConfig.MaxTextureAspectRatio();
		}
	}
	public float GetEfficiency() => (float)AreaUsed / (float)(MaxLightmapWidth * MathLib.CeilPow2(MinimumHeight));
	public int GetSortId() => SortID;
	public void IncrementSortId() => SortID++;
	protected int GetMaxYIndex(int firstX, int width) {
		int maxY = -1;
		int maxYIndex = 0;
		for (int x = firstX; x < firstX + width; ++x) {
			if (LightmapWavefront[x] >= maxY) {
				maxY = LightmapWavefront[x];
				maxYIndex = x;
			}
		}
		return maxYIndex;
	}

	protected int MaxLightmapWidth;
	protected int MaxLightmapHeight;
	protected readonly int[] LightmapWavefront = new int[MAX_MAX_LIGHTMAP_WIDTH];
	protected int AreaUsed;
	protected int MinimumHeight;
	protected int MaxBlockWidth;
	protected int MaxBlockHeight;
	protected int SortID;
}
