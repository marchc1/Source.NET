namespace Source.Common;

public enum RenderParamVector
{
	HmdWarpLeftCentre = 0,
	HmdWarpLeftCoeff012,
	HmdWarpLeftCoeff34RedOffset,
	HmdWarpRightCentre,
	HmdWarpRightCoeff012,
	HmdWarpRightCoeff34BlueOffset,
	HmdWarpGrowOutIn,
	HmdWarpGrowAboveBelow,
	HmdWarpAspect,
	DistortionType,
	WindDirection,

	Max = 20
}

public enum RenderParamInt
{
	EnableFixedLighting = 0,
	MorphAccumulatorXOffset,
	MorphAccumulatorYOffset,
	MorphAccumulatorSubrectWidth,
	MorphAccumulatorSubrectHeight,
	MorphAccumulator4TupleCount,
	MorphWeightXOffset,
	MorphWeightYOffset,
	MorphWeightSubrectWidth,
	MorphWeightSubrectHeight,
	WriteDepthToDestAlpha,
	BackBufferIndex,
	Max = 20
}

public enum RenderParamTexture
{
	AmbientOcclusion = 0,
	Max = 2
}

public enum BackBufferIndex
{
	Default = 0,
	Hdr = 1
}

public enum EnableFixedLighting
{
	None = 0,
	BasicLight = 1,
	OutputMrtsForDeferredLighting = 2,
	OutputNormalAndDepth = 3
}
