using Source.Common.Physics;

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

	public ref SurfaceData GetSurfaceData(nint surfaceDataIndex) {
		throw new NotImplementedException();
	}

	public nint GetSurfaceIndex(ReadOnlySpan<char> surfacePropName) {
		throw new NotImplementedException();
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
}
