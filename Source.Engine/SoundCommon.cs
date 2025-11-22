using Source.Common.Audio;

using System.Data;
using System.Numerics;

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
	public virtual int PaintBegin(double mixAheadTime, int soundtime, int paintedtime) => 0;
	public virtual void PaintEnd() { }

	public virtual void SpatializeChannel(Span<int> volume, int master_vol, in Vector3 sourceDir, float gain, float mono) { }
	public virtual void ApplyDSPEffects(int idsp, Span<PortableSamplePair> bufFront, Span<PortableSamplePair> bufRear, Span<PortableSamplePair> bufCenter, int samplecount) { }

	public virtual int GetOutputPosition() => 0;
	public virtual void ClearBuffer() { }
	public virtual void UpdateListener(in Vector3 position, in Vector3 forward, in Vector3 right, in Vector3 up) { }
	public virtual void MixBegin(int sampleCount) { }

	public virtual void MixUpsample(int sampleCount, int filtertype) { }

	public virtual void Mix8Mono(ref AudioChannel channel, Span<byte> data, int outputOffset, int inputOffset, uint rateScaleFix, int outCount, int timecompress) { }

	public virtual void Mix8Stereo(ref AudioChannel channel, Span<byte> data, int outputOffset, int inputOffset, uint rateScaleFix, int outCount, int timecompress) { }

	public virtual void Mix16Mono(ref AudioChannel channel, Span<short> data, int outputOffset, int inputOffset, uint rateScaleFix, int outCount, int timecompress) { }
	public virtual void Mix16Stereo(ref AudioChannel channel, Span<short> data, int outputOffset, int inputOffset, uint rateScaleFix, int outCount, int timecompress) { }
	public virtual void ChannelReset(int entnum, int channelIndex, float distanceMod) { }

	public virtual void TransferSamples(int end) { }
	public virtual ReadOnlySpan<char> DeviceName() => default;

	public virtual int DeviceChannels() => 0;
	public virtual int DeviceSampleBits() => 0;
	public virtual int DeviceSampleBytes() => 0;
	public virtual int DeviceDmaSpeed() => 0;
	public virtual int DeviceSampleCount() => 0;

	protected bool Surround;
	protected bool SurroundCenter;
	protected bool Headphone;

}

public static partial class Audio
{
	static bool FirstTime = true;

	static readonly AudioDeviceNull nullDevice = new();
	public static IAudioDevice GetNullDevice() => nullDevice;

	public static IAudioDevice? AutoDetectInit(bool _) {
		IAudioDevice? device = null;
		device = CreateSDLAudioDevice();

		FirstTime = false;
		if (device == null) {
			return GetNullDevice();
		}
		return device;
	}
}


public class AudioDeviceNull : AudioDeviceBase;
