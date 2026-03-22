using Source.Common.Physics;

using System;
using System.Collections.Generic;
using System.Text;

namespace Source.Common;

public class VCollide
{
	public ushort SolidCount;
	public bool IsPacked;
	public short DescSize;

	public PhysCollide[]? Solids;
	public byte[]? KeyValues;
}
