using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Game.Shared;

public enum ParticleAttachment : byte
{
	AbsOrigin = 0,
	AbsOriginFollow,
	CustomOrigin,
	Point,
	PointFollow,
	WorldOrigin,
	RootBoneFollow,

	Max,
}

public struct ParticleEffectsColors
{
	public Vector3 Color1;
	public Vector3 Color2;
}

public struct ParticleEffectsControlPoint
{
	public byte ParticleAttachment;
	public Vector3 Offset;
}
