namespace Source.Common.ShaderLib;

public enum ShaderMaterialVars
{
	Flags = 0,
	FlagsDefined,
	Flags2,
	FlagsDefined2,
	Color,
	Alpha,
	BaseTexture,
	Frame,
	BaseTextureTransform,
	FlashLightTexture,
	FlashLightTextureFrame,
	Color2,
	SRGBTint,

	Count,
}

public enum ShaderParamFlags
{
	NotEditable = 0x1,
	/// <summary>
	/// Marks the standard parameter as non-uploadable - ie, its upload to the GPU is handled by some other component.
	/// </summary>
	DoNotUpload = 0x2
}

public static class VertexShaderConst
{
	public const int MathConstants0 = 0;
	public const int MathConstants1 = 1;
	public const int CameraPos = 2;
	public const int FlexScale = 3;
	public const int LightIndex = 3;
	public const int ModelViewProj = 4;
	public const int ViewProj = 8;
	public const int ModelViewProjThirdRow = 12;
	public const int ViewProjThirdRow = 13;
	public const int ShaderSpecificConst10 = 14;
	public const int ShaderSpecificConst11 = 15;
	public const int FogParams = 16;
	public const int ViewModel = 17;
	public const int AmbientLight = 21;
	public const int Lights = 27;
	public const int Light0Position = 29;
	public const int ModulationColor = 47;
	public const int ShaderSpecificConst0 = 48;
	public const int ShaderSpecificConst1 = 49;
	public const int ShaderSpecificConst2 = 50;
	public const int ShaderSpecificConst3 = 51;
	public const int ShaderSpecificConst4 = 52;
	public const int ShaderSpecificConst5 = 53;
	public const int ShaderSpecificConst6 = 54;
	public const int ShaderSpecificConst7 = 55;
	public const int ShaderSpecificConst8 = 56;
	public const int ShaderSpecificConst9 = 57;
	public const int Model = 58;
	public const int ShaderSpecificConst13 = 217;
	public const int ShaderSpecificConst14 = 218;
	public const int ShaderSpecificConst15 = 219;
	public const int ShaderSpecificConst16 = 220;
	public const int ShaderSpecificConst17 = 221;
	public const int ShaderSpecificConst18 = 222;
	public const int ShaderSpecificConst19 = 223;
	public const int ShaderSpecificConst12 = 224;
	public const int FlexWeights = 1024;
	public const int MaxFlexWeightCount = 512;
}

public enum PixelShaderConst
{
	SelfIllumTint = 0,
	DiffuseModulation = 1,
	EnvMapTintShadowTweaks = 2,
	SelfIllumScaleBiasExp = 3,
	AmbientCube = 4,
	Constant05 = 5,
	Constant06 = 6,
	Constant07 = 7,
	Constant08 = 8,
	Constant09 = 9,
	EnvMapFresnelSelfIllumMask = 10,
	EyePosSpecExponent = 11,
	FogParams = 12,
	FlashlightAttenuation = 13,
	FlashlightPositionRimBoost = 14,
	FlashlightToWorldTexture = 15,
	Constant16 = 16,
	Constant17 = 17,
	Constant18 = 18,
	FresnelSpecParams = 19,
	LightInfoArray = 20,
	Constant21 = 21,
	Constant22 = 22,
	Constant23 = 23,
	Constant24 = 24,
	Constant25 = 25,
	SpecRimParams = 26,
	Constant27 = 27,
	FlashlightColor = 28,
	LinearFogColor = 29,
	LightScale = 30,
	FlashlightScreenScale = 31
}

public enum BlendType
{
	None = 0,
	Blend,
	Add,
	BlendAdd
}