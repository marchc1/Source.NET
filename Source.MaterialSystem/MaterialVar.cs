using Source.Common.MaterialSystem;

using System.Numerics;

namespace Source.MaterialSystem;

public sealed class MaterialVar : IMaterialVar
{
	IMaterialInternal owningMaterial;

	void Init() {

	}


	public MaterialVar(IMaterial material, ReadOnlySpan<char> key) {
		Init();
		owningMaterial = (IMaterialInternal)material!;
		Name = new(key);
		Type = MaterialVarType.Undefined;
	}
	public MaterialVar(IMaterial material, ReadOnlySpan<char> key, int val) {
		Init();
		owningMaterial = (IMaterialInternal)material!;
		Name = new(key);
		Type = MaterialVarType.Int;
		VecVal[0] = VecVal[1] = VecVal[2] = VecVal[3] = (float)val;
		IntVal = val;
	}
	public MaterialVar(IMaterial material, ReadOnlySpan<char> key, float val) {
		Init();
		owningMaterial = (IMaterialInternal)material!;
		Name = new(key);
		Type = MaterialVarType.Float;
		VecVal[0] = VecVal[1] = VecVal[2] = VecVal[3] = val;
		IntVal = (int)val;
	}
	public MaterialVar(IMaterial material, ReadOnlySpan<char> key, Span<float> val) {
		Init();
		owningMaterial = (IMaterialInternal)material!;
		Name = new(key);
		Type = MaterialVarType.Vector;
		NumVectorComps = (byte)Math.Min(val.Length, 4);
		for (int i = 0; i < NumVectorComps; i++) 
			VecVal[i] = val[i];
		
		IntVal = (int)VecVal[0];
	}
	public MaterialVar(IMaterial material, ReadOnlySpan<char> key, ReadOnlySpan<char> val) {
		Init();
		owningMaterial = (IMaterialInternal)material!;
		Name = new(key);
		StringVal = new(val);
		Type = MaterialVarType.String;
		VecVal[0] = VecVal[1] = VecVal[2] = VecVal[3] = float.TryParse(val, out float r) ? r : 0;
		IntVal = (int)VecVal[0];
	}

	public override void CopyFrom(IMaterialVar materialVar) {
		throw new NotImplementedException();
	}

	public override void GetFourCCValue(ulong type, out object? data) {
		throw new NotImplementedException();
	}

	public override IMaterial? GetMaterialValue() {
		throw new NotImplementedException();
	}

	public override Matrix4x4 GetMatrixValue() {
		throw new NotImplementedException();
	}

	public override ReadOnlySpan<char> GetName() {
		return Name.String()?.ToLowerInvariant();
	}

	public override IMaterial GetOwningMaterial() {
		return owningMaterial;
	}

	public override string GetStringValue() {
		return StringVal;
	}

	public override ITexture? GetTextureValue() {
		if (owningMaterial != null)
			owningMaterial.Precache();

		if (Type == MaterialVarType.Texture)
			return TextureValue;

		return null;
	}

	public override unsafe void GetVecValue(Span<float> val) {
		fixed(Vector4* v4 = &VecVal)
			new Span<float>(v4, val.Length > 4 ? 4 : val.Length).CopyTo(val);
	}

	public override bool IsDefined() {
		return Type != MaterialVarType.Undefined;
	}

	public override bool MatrixIsIdentity() {
		throw new NotImplementedException();
	}

	public override void SetFloatValue(float val) {
		VecVal[0] = val;
	}

	public override void SetFourCCValue(ulong type, object? data) {
		throw new NotImplementedException();
	}

	public override void SetIntValue(int val) {
		IntVal = val;
	}

	public override void SetMaterialValue(IMaterial? material) {
		throw new NotImplementedException();
	}

	public override void SetMatrixValue(in Matrix4x4 matrix) {

	}

	public override void SetStringValue(ReadOnlySpan<char> val) {
		throw new NotImplementedException();
	}

	public override void SetTextureValue(ITexture? texture) {
		Type = MaterialVarType.Texture;
		TextureValue = texture;
	}

	public override void SetUndefined() {
		Type = MaterialVarType.Undefined;
	}

	public override void SetValueAutodetectType(ReadOnlySpan<char> val) {
		throw new NotImplementedException();
	}

	public override void SetVecComponentValue(float val, int component) {
		throw new NotImplementedException();
	}

	public override unsafe void SetVecValue(ReadOnlySpan<float> val) {
		fixed (Vector4* v4 = &VecVal)
			val.CopyTo(new Span<float>(v4, 4));
	}

	public override void SetVecValue(float x, float y) {
		VecVal[0] = x;
		VecVal[1] = y;
	}

	public override void SetVecValue(float x, float y, float z) {
		VecVal[0] = x;
		VecVal[1] = y;
		VecVal[2] = z;
	}

	public override void SetVecValue(float x, float y, float z, float w) {
		throw new NotImplementedException();
	}

	protected override float GetFloatValueInternal() {
		throw new NotImplementedException();
	}

	protected override int GetIntValueInternal() {
		throw new NotImplementedException();
	}

	protected override Span<float> GetVecValueInternal() {
		throw new NotImplementedException();
	}

	protected override void GetVecValueInternal(Span<float> val) {
		throw new NotImplementedException();
	}

	protected override int VectorSizeInternal() {
		throw new NotImplementedException();
	}
}
