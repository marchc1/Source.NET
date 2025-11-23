using Source.Common.Audio;

using System.Numerics;

namespace Source.AudioSystem;

public class AudioSystem : IAudioSystem
{
	public int DeviceChannels() {
		return 0;
	}

	public int DeviceDmaSpeed() {
		return 0;
	}

	public ReadOnlySpan<char> DeviceName() {
		return "";
	}

	public int DeviceSampleBits() {
		return 0;
	}

	public int DeviceSampleBytes() {
		return 0;
	}

	public int DeviceSampleCount() {
		return 0;
	}

	public bool Init() {
		return true;
	}

	public bool IsActive() {
		return true;
	}

	public void Update(double v) {

	}

	public void UpdateListener(in Vector3 listenerOrigin, in Vector3 listenerForward, in Vector3 listenerRight, in Vector3 listenerUp, bool isListenerUnderwater) {

	}
}
