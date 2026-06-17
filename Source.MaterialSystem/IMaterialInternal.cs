using Source.Common.Formats.Keyvalues;
using Source.Common.MaterialSystem;

using System.Collections;
using System.Runtime.InteropServices;

namespace Source.MaterialSystem;

public struct MaterialLookup(IMaterialInternal? material, ulong symbol, bool manuallyCreated) {
	public readonly ulong Hash() {
		Span<byte> ugh = stackalloc byte[9];
		Span<ulong> sym = [symbol]; MemoryMarshal.Cast<ulong, byte>(sym).CopyTo(ugh);
		ugh[8] = (byte)(manuallyCreated ? 1 : 0);
		return ugh.Hash();
	}

	public readonly IMaterialInternal? Material => material;
	public readonly ulong Symbol => symbol;
	public readonly bool ManuallyCreated => manuallyCreated;
}

public class MaterialDict(MaterialSystem materials) : IEnumerable<IMaterialInternal> {
	Dictionary<ulong, MaterialLookup> Dict = [];
	public IMaterialInternal? FindMaterial(ReadOnlySpan<char> name, bool manuallyCreated) {
		MaterialLookup lookup = new(null, name.Hash(), manuallyCreated);
		if (Dict.TryGetValue(lookup.Hash(), out MaterialLookup mat))
			return mat.Material;
		return null;
	}

	public void AddMaterialToMaterialList(IMaterialInternal material) {
		MaterialLookup lookup = new(material, material.GetName().Hash(), material.IsManuallyCreated());
		Dict[lookup.Hash()] = lookup;
	}

	public void RemoveMaterial(IMaterialInternal material) {
		MaterialLookup lookup = new(material, material.GetName().Hash(), material.IsManuallyCreated());
		Dict.Remove(lookup.Hash());
	}

	public IEnumerator<IMaterialInternal> GetEnumerator() {
		foreach (var kvp in Dict) {
			if (kvp.Value.Material == null)
				continue;
			yield return kvp.Value.Material;
		}
	}

	IEnumerator IEnumerable.GetEnumerator() {
		return GetEnumerator();
	}

	HashSet<ulong> Missing = [];

	public bool NoteMissing(ReadOnlySpan<char> name) {
		return Missing.Add(name.Hash());
	}

	public IMaterialInternal AddMaterialSubRect(Span<char> matNameWithExtension, ReadOnlySpan<char> textureGroupName, KeyValues keyValues, KeyValues pPatchKeyValues) {
		throw new NotImplementedException();
	}

	internal IMaterialInternal AddMaterial(Span<char> name, ReadOnlySpan<char> textureGroupName) {
		IMaterialInternal material = materials.CreateMaterial(name, textureGroupName, null);
		AddMaterialToMaterialList(material);
		return material;
	}
}
