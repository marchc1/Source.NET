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