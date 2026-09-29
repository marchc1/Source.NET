using Source.Common;
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
	[NetworkName("m_vecColor1")]
	public Vector3 Color1;
	[NetworkName("m_vecColor2")]
	public Vector3 Color2;
}

public struct ParticleEffectsControlPoint
{
	[NetworkName("m_eParticleAttachment")]
	public byte ParticleAttachment;
	[NetworkName("m_vecOffset")]
	public Vector3 Offset;
}
