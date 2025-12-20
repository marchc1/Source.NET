using Source.Common;
using Source.Common.Formats.Keyvalues;
using Source.Common.MaterialSystem;

using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Client;

public class FloatInput {
	public bool Init(IMaterial material, KeyValues keyValues, ReadOnlySpan<char> keyName, float def = 0.0f){
		FloatVar = null;
		KeyValues? section = keyValues.FindKey(keyName);
		if (section != null) {
			if (section.Type == KeyValues.Types.String) {
				ReadOnlySpan<char> varName = section.GetString();

				// Look for numbers...
				float flValue;
				int nCount = new ScanF(varName, "%f").Read(out flValue).ReadArguments;
				if (nCount == 1) {
					Value = flValue;
					return true;
				}

				// Look for array specification...
				Span<char> pTemp = stackalloc char[256];
				// TODO ^^^^^^^^^^^^^^^
				FloatVecComp = -1;

				bool foundVar;
				FloatVar = material.FindVar(varName, out foundVar, true);
				if (!foundVar)
					return false;
			}
			else {
				Value = section.GetFloat();
			}
		}
		else {
			Value = def;
		}

		return true;
	}

	public float GetFloat(){
		if (FloatVar == null)
			return Value;

		if (FloatVecComp < 0)
			return FloatVar.GetFloatValue();

		int vecSize = FloatVar.VectorSize();
		if (FloatVecComp >= vecSize)
			return 0;

		Span<float> v = stackalloc float[4];
		FloatVar.GetVecValue(v[..vecSize]);
		return v[FloatVecComp];
	}

	float Value;
	IMaterialVar? FloatVar;
	int FloatVecComp;
}

public abstract class ResultProxy : IMaterialProxy
{
	public virtual bool Init(IMaterial material, KeyValues keyValues) {
		ReadOnlySpan<char> result = keyValues.GetString("resultVar");
		if (result.IsEmpty)
			return false;

		Span<char> temp = stackalloc char[256];
		if (result.Contains('[')) {
			// todo
			ResultVecComp = -1;
		}
		else
			ResultVecComp = -1;

		bool foundVar;
		Result = material.FindVar(result, out foundVar, true);
		return foundVar;
	}
	public abstract void OnBind(object o);
	public virtual void Release() { }
	public virtual IMaterial GetMaterial() => Result!.GetOwningMaterial();

	protected C_BaseEntity? BindArgToEntity(object? arg) {
		IClientRenderable? rend = (IClientRenderable?)arg;
		return rend != null ? rend.GetIClientUnknown().GetBaseEntity() : null;
	}
	protected void SetFloatResult(float result) {
		if (Result.GetVarType() == MaterialVarType.Vector) {
			if (ResultVecComp >= 0) 
				Result.SetVecComponentValue(result, ResultVecComp);
			else {
				Span<float> v = stackalloc float[4];
				int vecSize = Result.VectorSize();

				for (int i = 0; i < vecSize; ++i)
					v[i] = result;

				Result.SetVecValue(v[..vecSize]);
			}
		}
		else {
			Result.SetFloatValue(result);
		}
	}

	protected IMaterialVar? Result;
	protected int ResultVecComp;
}
