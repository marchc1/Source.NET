using Source;
using Source.Common;

using System.Numerics;

namespace Game.Shared;

public static class PlayerNetVars
{

}

public partial struct FogParams()
{
	[NetworkName("dirPrimary")]
	[NetworkVar] public Vector3 DirPrimary;
	[NetworkName("colorPrimary")]
	[NetworkVar] public Color ColorPrimary;
	[NetworkName("colorSecondary")]
	[NetworkVar] public Color ColorSecondary;
	[NetworkName("colorPrimaryHDR")]
	[NetworkVar] public Color ColorPrimaryHDR;
	[NetworkName("colorSecondaryHDR")]
	[NetworkVar] public Color ColorSecondaryHDR;
	[NetworkName("colorPrimaryLerpTo")]
	[NetworkVar] public Color ColorPrimaryLerpTo;
	[NetworkName("colorSecondaryLerpTo")]
	[NetworkVar] public Color ColorSecondaryLerpTo;
	[NetworkName("start")]
	[NetworkVar] public float Start;
	[NetworkName("end")]
	[NetworkVar] public float End;
	[NetworkName("farz")]
	[NetworkVar] public float FarZ;
	[NetworkName("maxdensity")]
	[NetworkVar] public float MaxDensity;
	[NetworkName("startLerpTo")]
	[NetworkVar] public float StartLerpTo;
	[NetworkName("endLerpTo")]
	[NetworkVar] public float EndLerpTo;
	[NetworkName("maxdensityLerpTo")]
	[NetworkVar] public float MaxDensityLerpTo;
	[NetworkName("lerptime")]
	[NetworkVar] public TimeUnit_t LerpTime;
	[NetworkName("duration")]
	[NetworkVar] public TimeUnit_t Duration;
	[NetworkName("enable")]
	[NetworkVar] public bool Enable;
	[NetworkName("blend")]
	[NetworkVar] public bool Blend;
	[NetworkName("radial")]
	[NetworkVar] public bool Radial;
	[NetworkName("HDRColorScale")]
	[NetworkVar] public float HDRColorScale;
}


public partial struct FogPlayerParams()
{
#if CLIENT_DLL || GAME_DLL
	[NetworkName("m_hCtrl")]
	[NetworkVar] public Handle<BaseEntity> Ctrl = new();
#endif
	public float TransitionTime;

	public Color OldColor;
	public float OldStart;
	public float OldEnd;

	public Color NewColor;
	public float NewStart;
	public float NewEnd;
}



public partial struct Sky3DParams()
{
	[NetworkName("scale")]
	[NetworkVar] public int Scale;
	[NetworkName("origin")]
	[NetworkVar] public Vector3 Origin;
	[NetworkName("area")]
	[NetworkVar] public int Area;

	[NetworkName("fog")]
	[NetworkVarEmbedded] public FogParams Fog = new();
}

public partial struct AudioParams()
{
	[NetworkName("localSound")]
	[NetworkVar] public InlineArrayNumLocalAudioSounds<Vector3> LocalSound;
	[NetworkName("soundscapeIndex")]
	[NetworkVar] public int SoundscapeIndex;
	[NetworkName("localBits")]
	[NetworkVar] public int LocalBits;
	[NetworkName("ent")]
	[NetworkVar] public EHANDLE Ent = new();
}
