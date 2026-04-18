using Microsoft.VisualBasic;

using Source.Common.GUI;
using Source.Common.Physics;
using Source.Common.Utilities;

using System;
using System.Collections.Generic;
using System.Text;

namespace Source.Physics;

public class PhysicsSurfaceProps : IPhysicsSurfaceProps
{
	public void GetPhysicsParameters(nint surfaceDataIndex, out SurfacePhysicsParams paramsOut) {
		throw new NotImplementedException();
	}

	public void GetPhysicsProperties(nint surfaceDataIndex, out float density, out float thickness, out float friction, out float elasticity) {
		throw new NotImplementedException();
	}

	public ReadOnlySpan<char> GetPropName(nint surfaceDataIndex) {
		throw new NotImplementedException();
	}

	public ReadOnlySpan<char> GetString(ushort stringTableIndex) {
		throw new NotImplementedException();
	}

	public bool IsReservedMaterialIndex(nint materialIndex) {
		return (materialIndex > 127) ? true : false;
	}

	public nint GetReservedFallBack(nint materialIndex) {
		switch (materialIndex) {
			case MATERIAL_INDEX_SHADOW:
				return ShadowFallback;

		}
		return 0;
	}

	public Surface? GetInternalSurface(nint materialIndex) {
		if (IsReservedMaterialIndex(materialIndex))
			materialIndex = GetReservedFallBack(materialIndex);

		if (materialIndex < 0 || materialIndex > Props.Count - 1)
			return null;

		return Props[(int)materialIndex];
	}

	public SurfaceData_ptr? GetSurfaceData(nint materialIndex) {
		Surface? surface = GetInternalSurface(materialIndex);
		surface ??= GetInternalSurface(0); // Zero is always the "default" property

		Assert(surface != null);
		return surface?.Data;
	}

	public const int MATERIAL_INDEX_SHADOW = 0xF000;

	public nint GetReservedSurfaceIndex(ReadOnlySpan<char> propName) {
		if (stricmp(propName, "$MATERIAL_INDEX_SHADOW") == 0)
			return MATERIAL_INDEX_SHADOW;

		return -1;
	}

	public nint GetSurfaceIndex(ReadOnlySpan<char> surfacePropName) {
		if (surfacePropName[0] == '$') {
			nint index = GetReservedSurfaceIndex(surfacePropName);
			if (index >= 0)
				return index;
		}

		UtlSymId_t id = Strings.Find(surfacePropName);
		if (id != 0)
			for (int i = 0; i < Props.Count; i++)
				if (Props[i].Name == id)
					return i;

		return -1;
	}

	public nint ParseSurfaceData(ReadOnlySpan<char> filename, ReadOnlySpan<char> textfile) {
		throw new NotImplementedException();
	}

	public void SetWorldMaterialIndexTable(Span<nint> mapArray) {
		throw new NotImplementedException();
	}

	public nint SurfacePropCount() {
		throw new NotImplementedException();
	}

	readonly UtlSymbolTableMT Strings = new();
	readonly List<Surface> Props = new();
	readonly List<UtlSymbol> FileList = new();
	public nint ShadowFallback;
}


public class Surface
{
	public UtlSymbol Name;
	public ushort Pad;
	public readonly SurfaceData_ptr Data = new();
}
