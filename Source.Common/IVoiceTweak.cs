namespace Source.Common;

public enum VoiceTweakControl
{
	MicrophoneVolume = 0,           // values 0-1.
	OtherSpeakerScale,          // values 0-1. Scales how loud other players are.
	MicBoost,
	SpeakingVolume,             // values 0-1.  Current voice volume received through Voice Tweak mode

}

public delegate int StartVoiceTweakModeFn();
public delegate void EndVoiceTweakModeFn();
public delegate void SetControlFloatFn(VoiceTweakControl control, float value);
public delegate float GetControlFloatFn(VoiceTweakControl control);
public delegate bool IsStillTweakingFn();

public struct IVoiceTweak{
	public StartVoiceTweakModeFn StartVoiceTweakMode;
	public EndVoiceTweakModeFn EndVoiceTweakMode;
	public SetControlFloatFn SetControlFloat;
	public GetControlFloatFn GetControlFloat;
	public IsStillTweakingFn IsStillTweaking;
}
