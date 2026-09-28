using Source.Common.Audio;

namespace Source.Engine;

public static class VoiceSE
{
	static IAudioSystem? audioSystem;
	static IAudioSystem? AudioSystem => audioSystem ??= OptionalSingleton<IAudioSystem>();

	internal static bool Init() => AudioSystem?.VoiceSE_Init() ?? false;
	internal static void Term() => AudioSystem?.VoiceSE_Term();
	internal static void Idle(double frametime) => AudioSystem?.VoiceSE_Idle((float)frametime);
	internal static int StartChannel(int channel, int entity, bool proximity, int viewEntityIndex) => AudioSystem?.VoiceSE_StartChannel(channel, entity, proximity, viewEntityIndex) ?? 0;
	internal static void EndChannel(int channel, int entity) => AudioSystem?.VoiceSE_EndChannel(channel, entity);
	internal static void StartOverdrive() => AudioSystem?.VoiceSE_StartOverdrive();
	internal static void EndOverdrive() => AudioSystem?.VoiceSE_EndOverdrive();
	internal static void InitMouth(int entnum) => AudioSystem?.VoiceSE_InitMouth(entnum);
	internal static void CloseMouth(int entnum) => AudioSystem?.VoiceSE_CloseMouth(entnum);
	internal static void MoveMouth(int entnum, Span<short> samples, int numSamples) => AudioSystem?.VoiceSE_MoveMouth(entnum, samples, numSamples);
}
