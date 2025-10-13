using Source.Common.Audio;

using System.Data;

namespace Source.Engine;

public class AudioDeviceBase : IAudioDevice
{
	public virtual bool Init() => false;
	public virtual bool IsActive() => false;
	public virtual float MixDryVolume() => 0;
	public virtual void Pause() { }
	public virtual bool Should3DMix() => Surround;
	public virtual void Shutdown() { }
	public virtual void StopAllSounds() { }
	public virtual void UnPause() { }

	public virtual bool IsSurround() => Surround;
	public virtual bool IsSurroundCenter() => SurroundCenter;
	public virtual bool IsHeadphone() => Headphone;

	bool Surround;
	bool SurroundCenter;
	bool Headphone;

}

public static partial class Audio {
	static bool FirstTime = true;

	static readonly AudioDeviceNull nullDevice = new();
	public static IAudioDevice GetNullDevice() => nullDevice;

	public static IAudioDevice? AutoDetectInit(bool _) {
		IAudioDevice? device = null;
		device = CreateSDLAudioDevice();

		FirstTime = false;
		if(device == null) {
			return GetNullDevice();
		}
		return device;
	}
}


public class AudioDeviceNull : AudioDeviceBase;
