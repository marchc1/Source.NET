using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Source.Physics;

public ref struct TraceBepu : ITraceObject
{
	public Vector3 GetVertByIndex(int index) {
		throw new NotImplementedException();
	}

	public float Radius() {
		throw new NotImplementedException();
	}

	public ushort SupportMap(in Vector3 dir, out Vector3 outVec) {
		throw new NotImplementedException();
	}
}
