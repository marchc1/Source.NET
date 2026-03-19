using Source;
using Source.Common;

using System.Numerics;

namespace Game.Shared;

public static class PlayerNetVars
{

}

public struct FogParams()
{
	public Vector3 DirPrimary;
	public Color ColorPrimary;
	public Color ColorSecondary;
	public Color ColorPrimaryHDR;
	public Color ColorSecondaryHDR;
	public Color ColorPrimaryLerpTo;
	public Color ColorSecondaryLerpTo;
	public float Start;
	public float End;
	public float FarZ;
	public float MaxDensity;
	public float StartLerpTo;
	public float EndLerpTo;
	public float MaxDensityLerpTo;
	public TimeUnit_t LerpTime;
	public TimeUnit_t Duration;
	public bool Enable;
	public bool Blend;
	public bool Radial;
	public float HDRColorScale;
}


public struct FogPlayerParams()
{
#if CLIENT_DLL || GAME_DLL
	public Handle<SharedBaseEntity> Ctrl = new();
	#endif
	public float TransitionTime;

	public Color OldColor;
	public float OldStart;
	public float OldEnd;

	public Color NewColor;
	public float NewStart;
	public float NewEnd;
}



public struct Sky3DParams()
{
	public int Scale;
	public Vector3 Origin;
	public int Area;

	public FogParams Fog = new();
}

public struct AudioParams()
{
	public InlineArrayNumLocalAudioSounds<Vector3> LocalSound;
	public int SoundscapeIndex;
	public int LocalBits;
	public EHANDLE Ent = new();
}
