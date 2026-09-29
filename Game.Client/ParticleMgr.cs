using Source.Common;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Game.Client;

public struct ParticleLightInfo
{
	[NetworkName("m_vPos")]
	public Vector3 Pos;
	[NetworkName("m_vColor")]
	public Vector3 Color;
	[NetworkName("m_flIntensity")]
	public float Intensity;
}
