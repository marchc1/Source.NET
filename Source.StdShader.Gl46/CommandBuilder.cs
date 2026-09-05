using Source.Common;
using Source.Common.MaterialSystem;
using Source.Common.Mathematics;
using Source.Common.ShaderAPI;

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Source.StdShader.Gl46;

public class FixedCommandStorageBuffer : ICommandStorageBuffer
{
	private readonly byte[] Data;
	private readonly List<ICommandStorageBuffer> References = [];
	private int Position;

#if DEBUG
	private int Remaining;
#endif

	public FixedCommandStorageBuffer(int capacity) {
		Data = GC.AllocateUninitializedArray<byte>(capacity);
		Reset();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void EnsureCapacity(int size) {
#if DEBUG
		Debug.Assert(Remaining >= size);
#endif
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Put<T>(in T value) where T : unmanaged {
		EnsureCapacity(Unsafe.SizeOf<T>());
		MemoryMarshal.Write(Data.AsSpan(Position), in value);
		Position += Unsafe.SizeOf<T>();
#if DEBUG
		Remaining -= Unsafe.SizeOf<T>();
#endif
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void PutInt(int value) => Put(value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void PutIntPtr(nint value) => Put(value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void PutFloat(float value) => Put(value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void PutPtr(nint ptr) => Put(ptr);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void PutMemory(ReadOnlySpan<byte> memory) {
		EnsureCapacity(memory.Length);
		memory.CopyTo(Data.AsSpan(Position));
		Position += memory.Length;
#if DEBUG
		Remaining -= memory.Length;
#endif
	}

	public int AddReference(ICommandStorageBuffer buffer) {
		int index = References.IndexOf(buffer);
		if (index < 0) {
			index = References.Count;
			References.Add(buffer);
		}
		return index;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public ICommandStorageBuffer Reference(int index) => References[index];

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Span<byte> Base() => Data.AsSpan(0, Position);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Reset() {
		Position = 0;
		References.Clear();
#if DEBUG
		Remaining = Data.Length;
#endif
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int Size() => Position;
}

public class CommandBufferBuilder<TStorage> where TStorage : ICommandStorageBuffer
{
	static readonly Lazy<IMaterialSystem> s_materials = new(Singleton<IMaterialSystem>);
	protected static IMaterialSystem Materials => s_materials.Value;
	public static MaterialSystem_Config Config => Materials.GetCurrentConfigForVideoCard();

	public TStorage Storage = default!;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void End() => Storage.PutInt((int)CommandBufferCommand.End);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public IMaterialVar Param(int var) => BaseShader.Params![var];

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetPixelShaderConstants(int firstConstant, int constants) {
		Storage.PutInt((int)CommandBufferCommand.SetPixelShaderFloatConst);
		Storage.PutInt(firstConstant);
		Storage.PutInt(constants);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void OutputConstantData(ReadOnlySpan<float> srcData) {
		Storage.PutFloat(srcData[0]);
		Storage.PutFloat(srcData[1]);
		Storage.PutFloat(srcData[2]);
		Storage.PutFloat(srcData[3]);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void OutputConstantData4(float val0, float val1, float val2, float val3) {
		Storage.PutFloat(val0);
		Storage.PutFloat(val1);
		Storage.PutFloat(val2);
		Storage.PutFloat(val3);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetPixelShaderConstant(int firstConstant, ReadOnlySpan<float> srcData, int numConstantsToSet) {
		SetPixelShaderConstants(firstConstant, numConstantsToSet);
		Storage.PutMemory(MemoryMarshal.AsBytes(srcData[..(4 * numConstantsToSet)]));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetPixelShaderConstant(int firstConstant, int var) {
		Span<float> vec = stackalloc float[4];
		Param(var).GetVecValue(vec);
		SetPixelShaderConstant(firstConstant, vec);
	}

	public void SetPixelShaderConstantGammaToLinear(int pixelReg, int constantVar) {
		Span<float> val = stackalloc float[4];
		Param(constantVar).GetVecValue(val[..3]);
		val[0] = val[0] > 1.0f ? val[0] : MathLib.GammaToLinear(val[0]);
		val[1] = val[1] > 1.0f ? val[1] : MathLib.GammaToLinear(val[1]);
		val[2] = val[2] > 1.0f ? val[2] : MathLib.GammaToLinear(val[2]);
		val[3] = 1.0f;
		SetPixelShaderConstant(pixelReg, val);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetPixelShaderConstant(int firstConstant, ReadOnlySpan<float> srcData) {
		SetPixelShaderConstants(firstConstant, 1);
		OutputConstantData(srcData);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetPixelShaderConstant4(int firstConstant, float val0, float val1, float val2, float val3) {
		SetPixelShaderConstants(firstConstant, 1);
		OutputConstantData4(val0, val1, val2, val3);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetPixelShaderConstant_W(int pixelReg, int constantVar, float wValue) {
		if (constantVar != -1) {
			Span<float> val = stackalloc float[3];
			Param(constantVar).GetVecValue(val);
			SetPixelShaderConstant4(pixelReg, val[0], val[1], val[2], wValue);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetVertexShaderConstant(int firstConstant, ReadOnlySpan<float> srcData) {
		Storage.PutInt((int)CommandBufferCommand.SetVertexShaderFloatConst);
		Storage.PutInt(firstConstant);
		Storage.PutInt(1);
		OutputConstantData(srcData);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetVertexShaderConstant(int firstConstant, ReadOnlySpan<float> srcData, int consts) {
		Storage.PutInt((int)CommandBufferCommand.SetVertexShaderFloatConst);
		Storage.PutInt(firstConstant);
		Storage.PutInt(consts);
		Storage.PutMemory(MemoryMarshal.AsBytes(srcData[..(4 * consts)]));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetVertexShaderConstant4(int firstConstant, float val0, float val1, float val2, float val3) {
		Storage.PutInt((int)CommandBufferCommand.SetVertexShaderFloatConst);
		Storage.PutInt(firstConstant);
		Storage.PutInt(1);
		Storage.PutFloat(val0);
		Storage.PutFloat(val1);
		Storage.PutFloat(val2);
		Storage.PutFloat(val3);
	}

	public void SetVertexShaderTextureTransform(int vertexReg, int transformVar) {
		Span<Vector4> transformation = stackalloc Vector4[2];
		IMaterialVar? transformationVar = Param(transformVar);

		if (transformationVar is not null && transformationVar.GetVarType() == MaterialVarType.Matrix) {
			Matrix4x4 mat = transformationVar.GetMatrixValue();
			transformation[0] = new Vector4(mat.M11, mat.M12, mat.M13, mat.M14);
			transformation[1] = new Vector4(mat.M21, mat.M22, mat.M23, mat.M24);
		}
		else {
			transformation[0] = new Vector4(1.0f, 0.0f, 0.0f, 0.0f);
			transformation[1] = new Vector4(0.0f, 1.0f, 0.0f, 0.0f);
		}

		SetVertexShaderConstant(vertexReg, MemoryMarshal.Cast<Vector4, float>(transformation), 2);
	}

	public void SetVertexShaderTextureScaledTransform(int vertexReg, int transformVar, int scaleVar) {
		Span<Vector4> transformation = stackalloc Vector4[2];
		IMaterialVar? transformationVar = Param(transformVar);

		if (transformationVar is not null && transformationVar.GetVarType() == MaterialVarType.Matrix) {
			Matrix4x4 mat = transformationVar.GetMatrixValue();
			transformation[0] = new Vector4(mat.M11, mat.M12, mat.M13, mat.M14);
			transformation[1] = new Vector4(mat.M21, mat.M22, mat.M23, mat.M24);
		}
		else {
			transformation[0] = new Vector4(1.0f, 0.0f, 0.0f, 0.0f);
			transformation[1] = new Vector4(0.0f, 1.0f, 0.0f, 0.0f);
		}

		Vector2 scale = new(1.0f, 1.0f);
		IMaterialVar? scaleVarParam = Param(scaleVar);
		if (scaleVarParam is not null) {
			if (scaleVarParam.GetVarType() == MaterialVarType.Vector) {
				Span<float> scaleValues = stackalloc float[2];
				scaleVarParam.GetVecValue(scaleValues);
				scale = new Vector2(scaleValues[0], scaleValues[1]);
			}
			else if (scaleVarParam.IsDefined()) {
				float s = scaleVarParam.GetFloatValue();
				scale = new Vector2(s, s);
			}
		}

		transformation[0].X *= scale.X;
		transformation[0].Y *= scale.Y;
		transformation[1].X *= scale.X;
		transformation[1].Y *= scale.Y;
		transformation[0].W *= scale.X;
		transformation[1].W *= scale.Y;

		SetVertexShaderConstant(vertexReg, MemoryMarshal.Cast<Vector4, float>(transformation), 2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetEnvMapTintPixelShaderDynamicState(int pixelReg, int tintVar) {
		if (Config.ShowSpecular/* && mat_fullbright.GetInt() != 2*/) {
			SetPixelShaderConstant(pixelReg, Param(tintVar).GetVecValue());
		}
		else
			SetPixelShaderConstant4(pixelReg, 0.0f, 0.0f, 0.0f, 0.0f);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetEnvMapTintPixelShaderDynamicStateGammaToLinear(int pixelReg, int tintVar, float alphaValue = 1.0f) {
		if (tintVar != -1 && Config.ShowSpecular/* && mat_fullbright.GetInt() != 2*/) {
			Span<float> color = stackalloc float[4];
			color[3] = alphaValue;
			Param(tintVar).GetLinearVecValue(color, 3);
			SetPixelShaderConstant(pixelReg, color);
		}
		else {
			SetPixelShaderConstant4(pixelReg, 0.0f, 0.0f, 0.0f, alphaValue);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void StoreEyePosInPixelShaderConstant(int constant) {
		Storage.PutInt((int)CommandBufferCommand.StoreEyePosInPsConst);
		Storage.PutInt(constant);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CommitPixelShaderLighting(int constant) {
		Storage.PutInt((int)CommandBufferCommand.CommitPixelShaderLighting);
		Storage.PutInt(constant);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetPixelShaderStateAmbientLightCube(int constant) {
		Storage.PutInt((int)CommandBufferCommand.SetPixelShaderStateAmbientLightCube);
		Storage.PutInt(constant);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetAmbientCubeDynamicStateVertexShader() => Storage.PutInt((int)CommandBufferCommand.SetAmbientCubeDynamicStateVertexShader);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetPixelShaderFogParams(int reg) {
		Storage.PutInt((int)CommandBufferCommand.SetPixelShaderFogParams);
		Storage.PutInt(reg);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void BindStandardTexture(Sampler sampler, StandardTextureId textureId) {
		Storage.PutInt((int)CommandBufferCommand.BindStandardTexture);
		Storage.PutInt((int)sampler);
		Storage.PutInt((int)textureId);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void BindTexture(Sampler sampler, ShaderAPITextureHandle_t texture) {
		Debug.Assert(texture != INVALID_SHADERAPI_TEXTURE_HANDLE);
		if (texture != INVALID_SHADERAPI_TEXTURE_HANDLE) {
			Storage.PutInt((int)CommandBufferCommand.BindShaderApiTextureHandle);
			Storage.PutInt((int)sampler);
			Storage.PutIntPtr(texture);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void BindTexture(BaseVSShader shader, Sampler sampler, int textureVar, int frameVar) {
		int texture = shader.GetShaderApiTextureBindHandle(textureVar, frameVar);
		BindTexture(sampler, texture);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void BindMultiTexture(BaseVSShader shader, Sampler sampler1, Sampler sampler2, int textureVar, int frameVar) {
		int texture = shader.GetShaderApiTextureBindHandle(textureVar, frameVar, 0);
		BindTexture(sampler1, texture);
		texture = shader.GetShaderApiTextureBindHandle(textureVar, frameVar, 1);
		BindTexture(sampler2, texture);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetPixelShaderIndex(int index) {
		Storage.PutInt((int)CommandBufferCommand.SetPsHIndex);
		Storage.PutInt(index);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetVertexShaderIndex(int index) {
		Storage.PutInt((int)CommandBufferCommand.SetVsHIndex);
		Storage.PutInt(index);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetDepthFeatheringPixelShaderConstant(int constant, float depthBlendScale) {
		Storage.PutInt((int)CommandBufferCommand.SetDepthFeatheringConst);
		Storage.PutInt(constant);
		Storage.PutFloat(depthBlendScale);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Goto(ICommandStorageBuffer cmdBuf) {
		Storage.PutInt((int)CommandBufferCommand.Jump);
		Storage.PutInt(Storage.AddReference(cmdBuf));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Call(ICommandStorageBuffer cmdBuf) {
		Storage.PutInt((int)CommandBufferCommand.Jsr);
		Storage.PutInt(Storage.AddReference(cmdBuf));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Reset() => Storage.Reset();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int Size() => Storage.Size();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Span<byte> Base() => Storage.Base();
}