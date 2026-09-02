namespace Source.Common;

public enum CommandBufferCommand
{
	/// <summary>End of stream.</summary>
	End = 0,
	/// <summary>int cmd, int reference. Jump to another stream. Can be used to implement non-sequentially allocated storage.</summary>
	Jump = 1,
	/// <summary>int cmd, int reference. Subroutine call to another stream.</summary>
	Jsr = 2,
	/// <summary>int cmd, int first_reg, int nregs, float values[nregs*4]</summary>
	SetPixelShaderFloatConst = 256,
	/// <summary>int cmd, int first_reg, int nregs, float values[nregs*4]</summary>
	SetVertexShaderFloatConst = 257,
	/// <summary>int cmd, int first_reg, int nregs, &amp;float values[nregs*4]</summary>
	SetVertexShaderFloatConstRef = 258,
	/// <summary>int cmd, int regdest</summary>
	SetPixelShaderFogParams = 259,
	/// <summary>int cmd, int regdest</summary>
	StoreEyePosInPsConst = 260,
	/// <summary>int cmd, int regdest</summary>
	CommitPixelShaderLighting = 261,
	/// <summary>int cmd, int regdest</summary>
	SetPixelShaderStateAmbientLightCube = 262,
	/// <summary>int cmd</summary>
	SetAmbientCubeDynamicStateVertexShader = 263,
	/// <summary>int cmd, int constant register, float blend scale</summary>
	SetDepthFeatheringConst = 264,
	/// <summary>cmd, sampler, texture id</summary>
	BindStandardTexture = 512,
	/// <summary>cmd, sampler, texture handle</summary>
	BindShaderApiTextureHandle = 513,
	/// <summary>cmd, idx</summary>
	SetPsHIndex = 1024,
	/// <summary>cmd, idx</summary>
	SetVsHIndex = 1025,
	/// <summary>cmd, int first_reg (for worldToTexture matrix)</summary>
	SetVertexShaderFlashlightState = 2000,
	/// <summary>cmd, int color reg, int atten reg, int origin reg, sampler (for flashlight texture)</summary>
	SetPixelShaderFlashlightState = 2001,
	/// <summary>cmd</summary>
	SetPixelShaderUberlightState = 2002,
	/// <summary>cmd</summary>
	SetVertexShaderNearZFarZState = 2003
}

public enum CommandBufferInstanceCommand
{
  /// <summary>End of stream.</summary>
	End = 0,
	/// <summary>int cmd, void* adr. Jump to another stream. Can be used to implement non-sequentially allocated storage.</summary>
	Jump,
	/// <summary>int cmd, void* adr. Subroutine call to another stream.</summary>
	Jsr,
	/// <summary>int cmd</summary>
	SetSkinningMatrices,
	/// <summary>int cmd</summary>
	SetVertexShaderLocalLighting,
	/// <summary>int cmd, int regdest</summary>
	SetPixelShaderLocalLighting,
	/// <summary>int cmd</summary>
	SetVertexShaderAmbientLightCube,
	/// <summary>int cmd, int regdest</summary>
	SetPixelShaderAmbientLightCube,
	/// <summary>int cmd, int regdest</summary>
	SetPixelShaderAmbientLightCubeLuminance,
	/// <summary>int cmd, int regdest</summary>
	SetPixelShaderGlintDamping,
	/// <summary>cmd, sampler</summary>
	BindEnvCubemapTexture,
	SetModulationPixelShaderDynamicState,
	/// <summary>int cmd, int constant register, Vector color2</summary>
	SetModulationPixelShaderDynamicStateLinearColorSpaceLinearScale,
	/// <summary>int cmd, int constant register, Vector color2</summary>
	SetModulationPixelShaderDynamicStateLinearColorSpace,
	/// <summary>int cmd, int constant register, Vector color2, float scale</summary>
	SetModulationPixelShaderDynamicStateLinearScale,
	/// <summary>int cmd, int constant register, Vector color2</summary>
	SetModulationVertexShaderDynamicState,
	/// <summary>int cmd, int constant register</summary>
	SetModulationPixelShaderDynamicStateIdentity,
	/// <summary>int cmd, int constant register, Vector color2, float scale</summary>
	SetModulationVertexShaderDynamicStateLinearScale,
	/// <summary>This must be last.</summary>
	Count
}

public interface ICommandStorageBuffer
{
	void EnsureCapacity(int size);
	void Put<T>(in T value) where T : unmanaged;
	void PutInt(int value);
	void PutIntPtr(nint value);
	void PutFloat(float value);
	void PutPtr(nint ptr);
	void PutMemory(ReadOnlySpan<byte> memory);
	int AddReference(ICommandStorageBuffer buffer);
	ICommandStorageBuffer Reference(int index);
	Span<byte> Base();
	void Reset();
	int Size();
}