using Source.Common;
using Source.Common.Audio;
using Source.Common.Commands;
using Source.Common.Mathematics;

using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Shared;


public struct SoundEnvelope
{
	public SoundEnvelope() {
		Current = 0.0f;
		Target = 0.0f;
		Rate = 0.0f;
		ForceUpdate = false;
	}

	public void SetTarget(float target, TimeUnit_t deltaTime) {
		float deltaValue = target - Current;

		if (deltaValue != 0 && deltaTime > 0) {
			Target = target;
			Rate = Math.Max(0.1f, Math.Abs(deltaValue / deltaTime));
		}
		else {
			if (target != Current) 
				ForceUpdate = true;

			SetValue(target);
		}
	}


	public void SetValue(float value) {
		if (Target != value) 
			ForceUpdate = true;

		Current = Target = value;
		Rate = 0;
	}
	public bool ShouldUpdate() {
		if (ForceUpdate) {
			ForceUpdate = false;
			return true;
		}

		if (Current != Target) 
			return true;

		return false;
	}


	public void Update(TimeUnit_t deltaTime) => Current = (float)MathLib.Approach(Target, Current, Rate * deltaTime);
	public float Value() => Current;

	float Current;
	float Target;
	TimeUnit_t Rate;
	bool ForceUpdate;
};



public class CopyRecipientFilter : IRecipientFilter
{
	public CopyRecipientFilter() => Flags = 0;

	public void Init(IRecipientFilter pSrc) {
		Flags = FLAG_ACTIVE;
		if (pSrc.IsReliable())
			Flags |= FLAG_RELIABLE;

		if (pSrc.IsInitMessage())
			Flags |= FLAG_INIT_MESSAGE;

		for (int i = 0; i < pSrc.GetRecipientCount(); i++) {
			int index = pSrc.GetRecipientIndex(i);

			if (index >= 0)
				Recipients.Add(index);
		}
	}

	public bool IsActive() {
		return (Flags & FLAG_ACTIVE) != 0;
	}

	public bool IsReliable() {
		return (Flags & FLAG_RELIABLE) != 0;
	}

	public int GetRecipientCount() {
		return Recipients.Count;
	}

	public int GetRecipientIndex(int slot) {
		return Recipients[slot];
	}

	public bool IsInitMessage() {
		return (Flags & FLAG_INIT_MESSAGE) != 0;
	}

#if CLIENT_DLL || GAME_DLL
	public bool AddRecipient(BasePlayer player) {
		Assert(player);

		int index = player.EntIndex();

		if (index < 0)
			return false;

		// Already in list
		if (Recipients.IndexOf(index) != -1)
			return false;

		Recipients.Add(index);
		return true;
	}
#endif

	public const int FLAG_ACTIVE = 0x1;
	public const int FLAG_RELIABLE = 0x2;
	public const int FLAG_INIT_MESSAGE = 0x4;

	int Flags;
	readonly List<int> Recipients = [];
}


public class SoundPatch : IDisposable
{
	static int g_SoundPatchCount;
	static readonly ConVar soundpatch_captionlength = new("soundpatch_captionlength", "2.0", FCvar.Replicated, "How long looping soundpatch captions should display for.");


	SoundEnvelope Pitch;
	SoundEnvelope Volume;
	SoundLevel SoundLevel;
	TimeUnit_t ShutdownTime;
	TimeUnit_t LastTime;
	string? SoundName;
	string? SoundScriptName;
	EHANDLE Ent;
	int EntityChannel;
	int Flags;
	int BaseFlags;
	int IsPlaying;
	float ScriptVolume; // Volume for this sound in sounds.txt
	CopyRecipientFilter Filter;
	float CloseCaptionDuration;

	public SoundPatch() {
		g_SoundPatchCount++;
		SoundName = null;
		SoundScriptName = null;
		CloseCaptionDuration = soundpatch_captionlength.GetFloat();
	}
	public void Dispose() {
		g_SoundPatchCount--;
	}
}

