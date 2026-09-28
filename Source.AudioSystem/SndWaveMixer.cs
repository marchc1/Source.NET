global using static Source.AudioSystem.SndWaveMixer;

using Source.Common.Audio;

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using static Source.Common.Formats.RiffConstants;

namespace Source.AudioSystem;

public static class SndWaveMixer
{
	//-----------------------------------------------------------------------------
	// Purpose: Create an appropriate mixer type given the data format
	// Input  : *data - data access abstraction
	//			format - pcm or adpcm (1 or 2 -- RIFF format)
	//			channels - number of audio channels (1 = mono, 2 = stereo)
	//			bits - bits per sample
	// Output : CAudioMixer * abstract mixer type that maps mixing to appropriate code
	//-----------------------------------------------------------------------------
	public static AudioMixer? CreateWaveMixer(IWaveData data, int format, int channels, int bits, int initialStreamPosition) {
		AudioMixer? mixer = null;

		if (format == WAVE_FORMAT_PCM) {
			if (channels > 1) {
				if (bits == 8)
					mixer = new AudioMixerWave8Stereo(data);
				else
					mixer = new AudioMixerWave16Stereo(data);
			}
			else {
				if (bits == 8)
					mixer = new AudioMixerWave8Mono(data);
				else
					mixer = new AudioMixerWave16Mono(data);
			}
		}
		else if (format == WAVE_FORMAT_ADPCM)
			return CreateADPCMMixer(data);
		else {
			// unsupported format or wav file missing!!!
			return null;
		}

		if (mixer != null)
			Assert(CalcSampleSize(bits, channels) == mixer.GetMixSampleSize());
		else
			Assert(false);

		return mixer;
	}

	//-----------------------------------------------------------------------------
	// Purpose: Abstract factory function for ADPCM mixers
	// Input  : *data - wave data access object
	//			channels -
	// Output : CAudioMixer
	//-----------------------------------------------------------------------------
	public static AudioMixer CreateADPCMMixer(IWaveData data) {
		return new AudioMixerWaveADPCM(data);
	}

	// Helper routine to round (rate * samples) down to fixed point precision

	public static double RoundToFixedPoint(double rate, int samples, bool interpolatedPitch) {
		fixedint fixp_rate;
		long d64_newSamps;      // need to use double precision int to avoid overflow

		double newSamps;

		// get rate, in fixed point, determine new samples at rate

		if (interpolatedPitch)
			fixp_rate = (fixedint)FIX_FLOAT14(rate);        // 14 bit iterator
		else
			fixp_rate = (fixedint)FIX_FLOAT(rate);      // 28 bit iterator

		// get number of new samples, convert back to float

		d64_newSamps = (long)fixp_rate * (long)samples;

		if (interpolatedPitch)
			newSamps = (double)d64_newSamps / (double)FIX_SCALE14;
		else
			newSamps = (double)d64_newSamps / (double)FIX_SCALE;

		return newSamps;
	}
}

//-----------------------------------------------------------------------------
// These mixers provide an abstraction layer between the audio device and
// mixing/decoding code.  They allow data to be decoded and mixed using
// optimized, format sensitive code by calling back into the device that
// controls them.
//-----------------------------------------------------------------------------
public abstract class AudioMixerWave : AudioMixer
{
	protected double fsample_index;         // index of next sample to output
	protected int sample_max_loaded;        // count of total samples loaded - ie: the index of
											// the next sample to be loaded.
	protected int sample_loaded_index;      // index of last sample loaded

	protected IWaveData? data;
	protected double forcedEndSample;
	protected bool finished;
	protected int delaySamples;

	//-----------------------------------------------------------------------------
	// Purpose: Init the base WAVE mixer.
	// Input  : *data - data access object
	//-----------------------------------------------------------------------------
	public AudioMixerWave(IWaveData data) {
		this.data = data;

		AudioSourceBase? source = GetSourceInternal();
		source?.ReferenceAdd(this);

		fsample_index = 0;
		sample_max_loaded = 0;
		sample_loaded_index = -1;
		finished = false;
		forcedEndSample = 0;
		delaySamples = 0;
	}

	//-----------------------------------------------------------------------------
	// Purpose: Frees the data access object (we own it after construction)
	//-----------------------------------------------------------------------------
	public override void Dispose() {
		AudioSourceBase? source = GetSourceInternal();
		source?.ReferenceRemove(this);
		data?.Dispose();
		data = null;
		base.Dispose();
	}

	public abstract void Mix(IAudioDevice device, Channel channel, ReadOnlySpan<byte> data, int outputOffset, int inputOffset, fixedint fracRate, int outCount, int timecompress);

	public override bool IsReadyToMix() {
		return data!.IsReadyToMix();
	}

	//-----------------------------------------------------------------------------
	// Purpose: Decode and read the data
	//			by default we just pass the request on to the data access object
	//			other mixers may need to buffer or decode the data for some reason
	//
	// Input  : **pData - dest pointer
	//			sampleCount - number of samples needed
	// Output : number of samples available in this batch
	//-----------------------------------------------------------------------------
	public virtual int GetOutputData(out ReadOnlySpan<byte> data, int sampleCount, Span<byte> copyBuf) {
		int samples_loaded;
		// clear this out in case the underlying code leaves it unmodified
		data = default;
		samples_loaded = this.data!.ReadSourceData(out data, sample_max_loaded, sampleCount, copyBuf);

		// keep track of total samples loaded
		sample_max_loaded += samples_loaded;

		// keep track of index of last sample loaded
		sample_loaded_index += samples_loaded;

		return samples_loaded;
	}

	AudioSourceBase? GetSourceInternal() => data?.Source();

	//-----------------------------------------------------------------------------
	// Purpose: calls through the wavedata to get the audio source
	// Output : CAudioSource
	//-----------------------------------------------------------------------------
	public override AudioSourceBase GetSource() {
		return GetSourceInternal()!;
	}

	//-----------------------------------------------------------------------------
	// Purpose: Gets the current sample location in playback (index of next sample
	//			to be loaded).
	// Output : int (samples from start of wave)
	//-----------------------------------------------------------------------------
	public override int GetSamplePosition() {
		return sample_max_loaded;
	}

	//-----------------------------------------------------------------------------
	// Purpose:
	// Input  : delaySamples -
	//-----------------------------------------------------------------------------
	public override void SetStartupDelaySamples(int delaySamples) {
		this.delaySamples = delaySamples;
	}

	// Move the current position to newPosition
	public override void SetSampleStart(int newPosition) {
		AudioSourceBase? source = GetSourceInternal();
		if (source != null)
			newPosition = source.ZeroCrossingAfter(newPosition);

		fsample_index = newPosition;

		// index of last sample loaded - set to sample at new position
		sample_loaded_index = newPosition;
		sample_max_loaded = sample_loaded_index + 1;
	}

	// End playback at newEndPosition
	public override void SetSampleEnd(int newEndPosition) {
		// forced end of zero means play the whole sample
		if (newEndPosition == 0)
			newEndPosition = 1;

		AudioSourceBase? source = GetSourceInternal();
		if (source != null)
			newEndPosition = source.ZeroCrossingBefore(newEndPosition);

		// past current position?  limit.
		if (newEndPosition < fsample_index)
			newEndPosition = (int)fsample_index;

		forcedEndSample = newEndPosition;
	}

	//-----------------------------------------------------------------------------
	// Purpose: Skip source data (read but don't mix).  The mixer must provide the
	//			full amount of samples or have silence in its output stream.
	//-----------------------------------------------------------------------------
	public override int SkipSamples(Channel channel, int sampleCount, int outputRate, int outputOffset) {
		float tempPitch = channel.Pitch;
		channel.Pitch = 1.0f;
		int retVal = MixDataToDevice_(null, channel, sampleCount, outputRate, outputOffset, true);
		channel.Pitch = tempPitch;
		return retVal;
	}

	// wrapper routine to append without overflowing the temp buffer
	static int AppendToBuffer(Span<byte> buffer, ReadOnlySpan<byte> sampleData, int bytes) {
		if (buffer.Length > 0) {
			int avail = buffer.Length;
			int copy = Math.Min(bytes, avail);
			sampleData[..copy].CopyTo(buffer);
			return copy;
		}
		else
			return 0;
	}

	// Load a static copy buffer (g_temppaintbuffer) with the requested number of samples,
	// with the first sample(s) in the buffer always set up as the last sample(s) of the previous load.
	// Return a pointer to the head of the copy buffer.
	// This ensures that interpolating pitch shifters always have the previous sample to reference.
	//		pChannel:				sound's channel data
	//		sample_load_request:	number of samples to load from source data
	//		pSamplesLoaded:			returns the actual number of samples loaded (should always = sample_load_request)
	//		copyBuf:				req'd by GetOutputData, used by some Mixers
	// Returns: NULL ptr to data if no samples available, otherwise always fills remainder of copy buffer with
	// 0 to pad remainder.
	// NOTE: DO NOT MODIFY THIS ROUTINE (KELLYB)
	public ReadOnlySpan<byte> LoadMixBuffer(Channel channel, int sample_load_request, out int samplesLoaded, Span<byte> copyBuf) {
		int samples_loaded;
		scoped ReadOnlySpan<byte> sample;
		scoped ReadOnlySpan<byte> data;
		int cCopySamps = 0;

		// save index of last sample loaded (updated in GetOutputData)
		int sample_loaded_index = this.sample_loaded_index;

		// get data from source (copyBuf is expected to be available for use)
		samples_loaded = GetOutputData(out data, sample_load_request, copyBuf);
		if (samples_loaded == 0 && sample_load_request != 0) {
			// none available, bail out
			// 360 might not be able to get samples due to latency of loop seek
			// could also be the valid EOF for non-loops (caller keeps polling for data, until no more)
			samplesLoaded = 0;
			return default;
		}

		int samplesize = GetMixSampleSize();
		int nTempCopyBufferSize = TEMP_COPY_BUFFER_SIZE * PortableSamplePair.SIZE;
		Span<byte> copyBuffer = MemoryMarshal.AsBytes(g_temppaintbuffer.AsSpan());
		int copy = 0;
		int copyBufferEnd = nTempCopyBufferSize;

#if DEBUG
		// for safety, 360 always validates sample request, due to new xma audio code and possible logic flaws
		// PC can expect number of requested samples to be within tolerances due to exisiting aged code
		// otherwise buffer overruns cause hard to track random crashes
		if (((sample_load_request + 1) * samplesize) > nTempCopyBufferSize) {
			// make sure requested samples will fit in temp buffer.
			// if this assert fails, then pitch is too high (ie: > 2.0) or the sample counters have diverged.
			// NOTE: to prevent this, pitch should always be capped in MixDataToDevice (but isn't nor are the sample counters).
			DevWarning($"LoadMixBuffer: sample load request {sample_load_request} exceeds buffer sizes\n");
			Assert(false);
			samplesLoaded = 0;
			return default;
		}
#endif

		// copy all samples from pData to copy buffer, set 0th sample to saved previous sample - this ensures
		// interpolation pitch shift routines always have a previous sample to reference.

		// copy previous sample(s) to head of copy buffer pCopy
		// In some cases, we'll need the previous 2 samples.  This occurs when
		// Rate < 1.0 - in example below, sample 4.86 - 6.48 requires samples 4-7 (previous samples saved are 4 & 5)

		/*
		Example:
			rate = 0.81, sampleCount = 3 (ie: # of samples to return )

			_____load 3______       ____load 3_______       __load 2__

			0		1		 2		 3		 4		 5		6		7		sample_index     (whole samples)

			^     ^      ^      ^      ^     ^     ^     ^     ^
			|     |      |      |      |     |     |     |     |
			0    0.81   1.68   2.43   3.24  4.05  4.86  5.67  6.48          m_fsample_index  (rate*sample)
			_______________    ________________   ________________
							^   ^                  ^ ^
							|   |                  | |
		m_sample_loaded_index   |                  | m_sample_loaded_index
								|                  |
			m_fsample_index----   		       ----m_fsample_index

			[return 3 samp]     [return 3 samp]    [return 3 samp]
		*/
		{
			sample = channel.SamplePrev;

			// determine how many saved samples we need to copy to head of copy buffer (0,1 or 2)
			// so that pitch interpolation will correctly reference samples.
			// NOTE: pitch interpolators always reference the sample before and after the indexed sample.

			// cCopySamps = sample_max_loaded - floor(m_fsample_index);

			if (sample_loaded_index < 0 || (Math.Floor(fsample_index) > sample_loaded_index)) {
				// no samples previously loaded, or
				// next sample index is entirely within the next block of samples to be loaded,
				// so we won't need any samples from the previous block. (can occur when rate > 2.0)
				cCopySamps = 0;
			}
			else if (fsample_index < sample_loaded_index) {
				// next sample index is entirely within the previous block of samples loaded,
				// so we'll need the last 2 samples loaded.  (can occur when rate < 1.0)
				Assert(Math.Ceiling(fsample_index + 0.00000001) == sample_loaded_index);
				cCopySamps = 2;
			}
			else {
				// next sample index is between the next block and the previously loaded block,
				// so we'll need the last sample loaded.  (can occur when 1.0 < rate < 2.0)
				Assert(Math.Floor(fsample_index) == sample_loaded_index);
				cCopySamps = 1;
			}
			Assert(cCopySamps >= 0 && cCopySamps <= 2);

			// point to the sample(s) we are to copy
			if (cCopySamps != 0) {
				sample = cCopySamps == 1 ? sample[samplesize..] : sample;
				copy += AppendToBuffer(copyBuffer[copy..copyBufferEnd], sample, samplesize * cCopySamps);
			}
		}

		// copy loaded samples from pData into pCopy
		// and update pointer to free space in copy buffer
		if ((samples_loaded * samplesize) != 0 && data.IsEmpty) {
			ReadOnlySpan<char> wavName = "";
			Common.Audio.SfxTable? source = channel.Sfx;
			if (source != null)
				wavName = source.GetName();

			Warning($"CAudioMixerWave::LoadMixBuffer: '{wavName}' samples_loaded * samplesize = {(samples_loaded * samplesize)} but pData == NULL\n");
			samplesLoaded = 0;
			return default;
		}

		copy += AppendToBuffer(copyBuffer[copy..copyBufferEnd], data, samples_loaded * samplesize);

		// if we loaded fewer samples than we wanted to, and we're not
		// delaying, load more samples or, if we run out of samples from non-looping source,
		// pad copy buffer.
		if (samples_loaded < sample_load_request) {
			// retry loading source data until 0 bytes returned, or we've loaded enough data.
			// if we hit 0 bytes, fill remaining space in copy buffer with 0 and exit
			int samples_load_extra;
			int samples_loaded_retry = -1;

			for (int k = 0; (k < 10000 && samples_loaded_retry != 0 && samples_loaded < sample_load_request); k++) {
				// how many more samples do we need to satisfy load request
				samples_load_extra = sample_load_request - samples_loaded;
				samples_loaded_retry = GetOutputData(out data, samples_load_extra, copyBuf);

				// copy loaded samples from pData into pCopy
				if (samples_loaded_retry != 0) {
					if ((samples_loaded_retry * samplesize) != 0 && data.IsEmpty) {
						Warning($"CAudioMixerWave::LoadMixBuffer:  samples_loaded_retry * samplesize = {(samples_loaded_retry * samplesize)} but pData == NULL\n");
						samplesLoaded = 0;
						return default;
					}

					copy += AppendToBuffer(copyBuffer[copy..copyBufferEnd], data, samples_loaded_retry * samplesize);
					samples_loaded += samples_loaded_retry;
				}
			}
		}

		// if we still couldn't load the requested samples, fill rest of copy buffer with 0
		if (samples_loaded < sample_load_request) {
			// these samples are filled with 0, not loaded.
			// non-looping source hit end of data, fill rest of g_temppaintbuffer with 0
			int samples_zero_fill = sample_load_request - samples_loaded;

			int avail = copyBufferEnd - copy;
			int fill = samples_zero_fill * samplesize;
			fill = Math.Min(avail, fill);
			copyBuffer.Slice(copy, fill).Clear();
			copy += fill;
			samples_loaded += samples_zero_fill;
		}

		if (samples_loaded >= 2) {
			// always save last 2 samples from copy buffer to channel
			// (we'll need 0,1 or 2 samples as start of next buffer for interpolation)
			Assert(channel.SamplePrev.Length >= samplesize * 2);
			copyBuffer.Slice(copy - samplesize * 2, samplesize * 2).CopyTo(channel.SamplePrev);
		}

		// this routine must always return as many samples loaded (or zeros) as requested.
		Assert(samples_loaded == sample_load_request);

		samplesLoaded = samples_loaded;

		return MemoryMarshal.AsBytes(g_temppaintbuffer.AsSpan());
	}

	// Helper routine for MixDataToDevice:
	// Compute number of new samples to load at 'rate' so we can
	// output 'sampleCount' samples, from m_fsample_index to fsample_index_end (inclusive)
	// rate:				sample rate
	// sampleCountOut:		number of samples calling routine needs to output
	// bInterpolated_pitch: true if mixers use interpolating pitch shifters
	public int GetSampleLoadRequest(double rate, int sampleCountOut, bool interpolatedPitch) {
		double fsample_index_end;       // index of last sample we'll need
		int sample_index_high;          // rounded up last sample index
		int sample_load_request;        // number of samples to load

		// NOTE: we must use fixed point math here, identical to math in mixers, to make sure
		// we predict iteration results exactly.
		// get floating point sample index of last sample we'll need
		fsample_index_end = fsample_index + RoundToFixedPoint(rate, sampleCountOut - 1, interpolatedPitch);

		// always round up to ensure we'll have that n+1 sample for interpolation
		sample_index_high = (int)Math.Ceiling(fsample_index_end);

		// make sure we always round the floating point index up by at least 1 sample,
		// ie: make sure integer sample_index_high is greater than floating point sample index
		if ((double)sample_index_high <= fsample_index_end)
			sample_index_high++;
		Assert(sample_index_high > fsample_index_end);

		// attempt to load enough samples so we can reach sample_index_high sample.
		sample_load_request = sample_index_high - sample_loaded_index;
		Assert(sample_index_high >= sample_loaded_index);

		// NOTE: we can actually return 0 samples to load if rate < 1.0
		// and sampleCountOut == 1.  In this case, the output sample
		// is computed from the previously saved buffer data.
		return sample_load_request;
	}

	public override int MixDataToDevice(IAudioDevice device, Channel channel, int sampleCount, int outputRate, int outputOffset) {
		return MixDataToDevice_(device, channel, sampleCount, outputRate, outputOffset, false);
	}

	//-----------------------------------------------------------------------------
	// Purpose: The device calls this to request data.  The mixer must provide the
	//			full amount of samples or have silence in its output stream.
	//			Mix channel to all active paintbuffers.
	//			NOTE: cannot be called consecutively to mix into multiple paintbuffers!
	// Input  : *pDevice - requesting device
	//			sampleCount - number of samples at the output rate - should never be more than size of paintbuffer.
	//			outputRate - sampling rate of the request
	//			outputOffset - starting offset to mix to in paintbuffer
	//			bskipallmixing - true if we just want to skip ahead in source data

	// Output : Returns true to keep mixing, false to delete this mixer

	// NOTE:	DO NOT MODIFY THIS ROUTINE (KELLYB)

	//-----------------------------------------------------------------------------
	[SkipLocalsInit]
	public int MixDataToDevice_(IAudioDevice? device, Channel channel, int sampleCount, int outputRate, int outputOffset, bool skipAllMixing) {
		// shouldn't be playing this if finished, but return if we are
		if (finished)
			return 0;

		// save this to compute total output
		int startingOffset = outputOffset;

		double inputRate = channel.Pitch * data!.Source().SampleRate();
		double rate_max = inputRate / outputRate;

		// If we are terminating this wave prematurely, then make sure we detect the limit
		if (forcedEndSample != 0) {
			// How many total input samples will we need?
			int samplesRequired = (int)(sampleCount * rate_max);
			// will this hit the end?
			if (fsample_index + samplesRequired >= forcedEndSample) {
				// yes, mark finished and truncate the sample request
				finished = true;
				sampleCount = (int)((forcedEndSample - fsample_index) / rate_max);
			}
		}

		/*
		Example:
		rate = 1.2, sampleCount = 3 (ie: # of samples to return )

		______load 4 samples_____       ________load 4 samples____     ___load 3 samples__

		0		1		2		3		4		5		6		7		8		9		10		sample_index     (whole samples)

		^         ^         ^        ^        ^         ^         ^         ^         ^
		|         |         |        |        |         |         |         |         |
		0		 1.2	   2.4      3.6      4.8       6.0       7.2	    8.4		  9.6		m_fsample_index  (rate*sample)
		_______return 3_______      _______return 3_______       _______return 3__________
		 						 ^   ^
								 |   |
		m_sample_loaded_index-----   |     		(after first load 4 samples, this is where pointers are)
			  m_fsample_index---------
		*/
		Span<byte> copyBuf = stackalloc byte[AudioSource.AUDIOSOURCE_COPYBUF_SIZE];
		while (sampleCount > 0) {
			bool advanceSample = true;
			int samples_loaded, outputSampleCount;
			scoped ReadOnlySpan<byte> data = default;
			double fsample_index_prev = fsample_index;      // save so we can modify in LoadMixBuffer
			bool interpolatedPitch = FUseHighQualityPitch(channel);
			double rate;

			// process samples in paintbuffer-sized batches
			int sampleCountOut = Math.Min(sampleCount, PAINTBUFFER_SIZE);

			// cap rate so that we never overflow the input copy buffer.
			rate = MIX_GetMaxRate(rate_max, sampleCountOut);

			if (delaySamples > 0) {
				// If we are preceding sample playback with a delay,
				// just fill data buffer with 0 value samples.
				// Because there is no pitch shift applied, outputSampleCount == sampleCountOut.
				int num_zero_samples = Math.Min(delaySamples, sampleCountOut);

				// Decrement delay counter
				delaySamples -= num_zero_samples;

				int sampleSize = GetMixSampleSize();
				int readBytes = sampleSize * num_zero_samples;

				// make sure we don't overflow temp copy buffer (g_temppaintbuffer)
				Assert(TEMP_COPY_BUFFER_SIZE * PortableSamplePair.SIZE > readBytes);
				Span<byte> zeroData = MemoryMarshal.AsBytes(g_temppaintbuffer.AsSpan());

				// Now copy in some zeroes
				zeroData[..readBytes].Clear();
				data = zeroData;

				// we don't pitch shift these samples, so outputSampleCount == samples_loaded
				samples_loaded = num_zero_samples;
				outputSampleCount = num_zero_samples;

				advanceSample = false;

				// the zero samples are at the output rate, so set the input/output ratio to 1.0
				rate = 1.0f;
			}
			else {
				// ask the source for the data...
				// temp buffer req'd by some data loaders

				// compute number of new samples to load at 'rate' so we can
				// output 'sampleCount' samples, from m_fsample_index to fsample_index_end (inclusive)
				int sample_load_request = GetSampleLoadRequest(rate, sampleCountOut, interpolatedPitch);

				// return pointer to a new copy buffer (g_temppaintbuffer) loaded with sample_load_request samples +
				// first sample(s), which are always the last sample(s) from the previous load.
				// Always returns sample_load_request samples. Updates m_sample_max_loaded, m_sample_loaded_index.
				data = LoadMixBuffer(channel, sample_load_request, out samples_loaded, copyBuf);

				// LoadMixBuffer should always return requested samples.
				Assert(data.IsEmpty || (samples_loaded == sample_load_request));

				outputSampleCount = sampleCountOut;
			}

			// no samples available
			if (data.IsEmpty)
				break;

			// get sample fraction from 0th sample in copy buffer
			double sampleFraction = fsample_index - Math.Floor(fsample_index);

			// if just skipping samples in source, don't mix, just keep reading
			if (!skipAllMixing) {
				// mix this data to all active paintbuffers
				// Verify that we won't get a buffer overrun.
				Assert(Math.Floor(sampleFraction + RoundToFixedPoint(rate, (outputSampleCount - 1), interpolatedPitch)) <= samples_loaded);

				int saveIndex = MIX_GetCurrentPaintbufferIndex();
				for (int i = 0; i < g_paintBuffers.Count; i++) {
					if (g_paintBuffers[i].Active) {
						// mix channel into all active paintbuffers
						MIX_SetCurrentPaintbuffer(i);

						Mix(
							device!,                        // Device.
							channel,                        // Channel.
							data,                           // Input buffer.
							outputOffset,                   // Output position.
							(int)FIX_FLOAT(sampleFraction),    // Iterators.
							(fixedint)FIX_FLOAT(rate),
							outputSampleCount,
							0);
					}
				}
				MIX_SetCurrentPaintbuffer(saveIndex);
			}

			if (advanceSample) {
				// update sample index to point to the next sample to output
				// if we're not delaying
				// Use fixed point math to make sure we exactly match results of mix
				// iterators.
				fsample_index = fsample_index_prev + RoundToFixedPoint(rate, outputSampleCount, interpolatedPitch);
			}

			outputOffset += outputSampleCount;
			sampleCount -= outputSampleCount;
		}

		// Did we run out of samples? if so, mark finished
		if (sampleCount > 0)
			finished = true;

		// total number of samples mixed !!! at the output clock rate !!!
		return outputOffset - startingOffset;
	}

	public override bool ShouldContinueMixing() {
		return !finished;
	}

	public override float ModifyPitch(float pitch) {
		return pitch;
	}

	public override float GetVolumeScale() {
		return 1.0f;
	}

	public override int GetPositionForSave() => GetSamplePosition();
	public override void SetPositionFromSaved(int savedPosition) => SetSampleStart(savedPosition);
}

//-----------------------------------------------------------------------------
// Purpose: maps mixing to 8-bit mono mixer
//-----------------------------------------------------------------------------
public class AudioMixerWave8Mono(IWaveData data) : AudioMixerWave(data)
{
	public override int GetMixSampleSize() => CalcSampleSize(8, 1);
	public override void Mix(IAudioDevice device, Channel channel, ReadOnlySpan<byte> data, int outputOffset, int inputOffset, fixedint fracRate, int outCount, int timecompress) {
		device.Mix8Mono(channel, data, outputOffset, inputOffset, fracRate, outCount, timecompress);
	}
}

//-----------------------------------------------------------------------------
// Purpose: maps mixing to 8-bit stereo mixer
//-----------------------------------------------------------------------------
public class AudioMixerWave8Stereo(IWaveData data) : AudioMixerWave(data)
{
	public override int GetMixSampleSize() => CalcSampleSize(8, 2);
	public override void Mix(IAudioDevice device, Channel channel, ReadOnlySpan<byte> data, int outputOffset, int inputOffset, fixedint fracRate, int outCount, int timecompress) {
		device.Mix8Stereo(channel, data, outputOffset, inputOffset, fracRate, outCount, timecompress);
	}
}

//-----------------------------------------------------------------------------
// Purpose: maps mixing to 16-bit mono mixer
//-----------------------------------------------------------------------------
public class AudioMixerWave16Mono(IWaveData data) : AudioMixerWave(data)
{
	public override int GetMixSampleSize() => CalcSampleSize(16, 1);
	public override void Mix(IAudioDevice device, Channel channel, ReadOnlySpan<byte> data, int outputOffset, int inputOffset, fixedint fracRate, int outCount, int timecompress) {
		device.Mix16Mono(channel, MemoryMarshal.Cast<byte, short>(data), outputOffset, inputOffset, fracRate, outCount, timecompress);
	}
}

//-----------------------------------------------------------------------------
// Purpose: maps mixing to 16-bit stereo mixer
//-----------------------------------------------------------------------------
public class AudioMixerWave16Stereo(IWaveData data) : AudioMixerWave(data)
{
	public override int GetMixSampleSize() => CalcSampleSize(16, 2);
	public override void Mix(IAudioDevice device, Channel channel, ReadOnlySpan<byte> data, int outputOffset, int inputOffset, fixedint fracRate, int outCount, int timecompress) {
		device.Mix16Stereo(channel, MemoryMarshal.Cast<byte, short>(data), outputOffset, inputOffset, fracRate, outCount, timecompress);
	}
}

//-----------------------------------------------------------------------------
// Purpose: Mixer for ADPCM encoded audio
//-----------------------------------------------------------------------------
public class AudioMixerWaveADPCM : AudioMixerWave
{
	// max size of ADPCM block in bytes
	const int MAX_BLOCK_SIZE = 4096;

	const int WAVEFORMATEX_SIZE = 18;

	readonly byte[]? format;
	readonly int coefficientsOffset;

	short[]? samples;
	int sampleCount;
	int samplePosition;

	int blockSize;
	int offset;

	int totalBytes;

	int wSamplesPerBlock => BinaryPrimitives.ReadUInt16LittleEndian(format.AsSpan(WAVEFORMATEX_SIZE));
	int nChannels => BinaryPrimitives.ReadUInt16LittleEndian(format.AsSpan(2));
	short iCoef1(int index) => BinaryPrimitives.ReadInt16LittleEndian(format.AsSpan(coefficientsOffset + index * 4));
	short iCoef2(int index) => BinaryPrimitives.ReadInt16LittleEndian(format.AsSpan(coefficientsOffset + index * 4 + 2));

	public AudioMixerWaveADPCM(IWaveData data) : base(data) {
		samples = null;
		sampleCount = 0;
		samplePosition = 0;
		offset = 0;

		AudioSourceWave source = (AudioSourceWave)this.data!.Source();

		format = source.GetHeader();
		if (format != null) {
			coefficientsOffset = WAVEFORMATEX_SIZE + 4;

			// create the decode buffer
			samples = new short[wSamplesPerBlock * nChannels];

			// number of bytes for samples
			blockSize = ((wSamplesPerBlock - 2) * nChannels) / 2;
			// size of channel header
			blockSize += 7 * nChannels;
			Assert(blockSize < MAX_BLOCK_SIZE);

			totalBytes = source.DataSize();
		}
	}

	public override void Dispose() {
		samples = null;
		base.Dispose();
	}

	int NumChannels() {
		if (format != null)
			return nChannels;
		return 0;
	}

	public override int GetMixSampleSize() => CalcSampleSize(16, NumChannels());

	public override void Mix(IAudioDevice device, Channel channel, ReadOnlySpan<byte> data, int outputOffset, int inputOffset, fixedint fracRate, int outCount, int timecompress) {
		if (NumChannels() == 1)
			device.Mix16Mono(channel, MemoryMarshal.Cast<byte, short>(data), outputOffset, inputOffset, fracRate, outCount, timecompress);
		else
			device.Mix16Stereo(channel, MemoryMarshal.Cast<byte, short>(data), outputOffset, inputOffset, fracRate, outCount, timecompress);
	}

	static ReadOnlySpan<int> error_sign_lut => [0, 1, 2, 3, 4, 5, 6, 7, -8, -7, -6, -5, -4, -3, -2, -1];
	static ReadOnlySpan<int> error_coefficients_lut => [230, 230, 230, 230, 307, 409, 512, 614,
										768, 614, 512, 409, 307, 230, 230, 230];

	//-----------------------------------------------------------------------------
	// Purpose: ADPCM decompress a single block of 1-channel audio
	// Input  : *pOut - output buffer 16-bit
	//			*pIn - input block
	//			count - number of samples to decode (to support partial blocks)
	//-----------------------------------------------------------------------------
	void DecompressBlockMono(Span<short> @out, ReadOnlySpan<byte> @in, int count) {
		int ip = 0, op = 0;

		int pred = (sbyte)@in[ip++];
		int co1 = iCoef1(pred);
		int co2 = iCoef2(pred);

		// read initial delta
		int delta = BinaryPrimitives.ReadInt16LittleEndian(@in[ip..]);
		ip += 2;

		// read initial samples for prediction
		int samp1 = BinaryPrimitives.ReadInt16LittleEndian(@in[ip..]);
		ip += 2;

		int samp2 = BinaryPrimitives.ReadInt16LittleEndian(@in[ip..]);
		ip += 2;

		// write out the initial samples (stored in reverse order)
		@out[op++] = (short)samp2;
		@out[op++] = (short)samp1;

		// subtract the 2 samples in the header
		count -= 2;

		// this is a toggle to read nibbles, first nibble is high
		int high = 1;

		int error, sample = 0;

		// now process the block
		while (count != 0) {
			// read the error nibble from the input stream
			if (high != 0) {
				sample = @in[ip++];
				// high nibble
				error = sample >> 4;
				// cache low nibble for next read
				sample = sample & 0xf;
				// Next read is from cache, not stream
				high = 0;
			}
			else {
				// stored in previous read (low nibble)
				error = sample;
				// next read is from stream
				high = 1;
			}
			// convert to signed with LUT
			int errorSign = error_sign_lut[error];

			// interpolate the new sample
			int predSample = (samp1 * co1) + (samp2 * co2);
			// coefficients are fixed point 8-bit, so shift back to 16-bit integer
			predSample >>= 8;

			// Add in current error estimate
			predSample += errorSign * delta;

			// Correct error estimate
			delta = (delta * error_coefficients_lut[error]) >> 8;
			// Clamp error estimate
			if (delta < 16)
				delta = 16;

			// clamp
			if (predSample > 32767)
				predSample = 32767;
			else if (predSample < -32768)
				predSample = -32768;

			// output
			@out[op++] = (short)predSample;
			// move samples over
			samp2 = samp1;
			samp1 = predSample;

			count--;
		}
	}

	//-----------------------------------------------------------------------------
	// Purpose: Decode a single block of stereo ADPCM audio
	// Input  : *pOut - 16-bit output buffer
	//			*pIn - ADPCM encoded block data
	//			count - number of sample pairs to decode
	//-----------------------------------------------------------------------------
	void DecompressBlockStereo(Span<short> @out, ReadOnlySpan<byte> @in, int count) {
		Span<int> pred = stackalloc int[2], co1 = stackalloc int[2], co2 = stackalloc int[2];
		int i;
		int ip = 0, op = 0;

		for (i = 0; i < 2; i++) {
			pred[i] = (sbyte)@in[ip++];
			co1[i] = iCoef1(pred[i]);
			co2[i] = iCoef2(pred[i]);
		}

		Span<int> delta = stackalloc int[2], samp1 = stackalloc int[2], samp2 = stackalloc int[2];

		for (i = 0; i < 2; i++, ip += 2) {
			// read initial delta
			delta[i] = BinaryPrimitives.ReadInt16LittleEndian(@in[ip..]);
		}

		// read initial samples for prediction
		for (i = 0; i < 2; i++, ip += 2)
			samp1[i] = BinaryPrimitives.ReadInt16LittleEndian(@in[ip..]);
		for (i = 0; i < 2; i++, ip += 2)
			samp2[i] = BinaryPrimitives.ReadInt16LittleEndian(@in[ip..]);

		// write out the initial samples (stored in reverse order)
		@out[op++] = (short)samp2[0];  // left
		@out[op++] = (short)samp2[1];  // right
		@out[op++] = (short)samp1[0];  // left
		@out[op++] = (short)samp1[1];  // right

		// subtract the 2 samples in the header
		count -= 2;

		// this is a toggle to read nibbles, first nibble is high
		int high = 1;

		int error, sample = 0;

		// now process the block
		while (count != 0) {
			for (i = 0; i < 2; i++) {
				// read the error nibble from the input stream
				if (high != 0) {
					sample = @in[ip++];
					// high nibble
					error = sample >> 4;
					// cache low nibble for next read
					sample = sample & 0xf;
					// Next read is from cache, not stream
					high = 0;
				}
				else {
					// stored in previous read (low nibble)
					error = sample;
					// next read is from stream
					high = 1;
				}
				// convert to signed with LUT
				int errorSign = error_sign_lut[error];

				// interpolate the new sample
				int predSample = (samp1[i] * co1[i]) + (samp2[i] * co2[i]);
				// coefficients are fixed point 8-bit, so shift back to 16-bit integer
				predSample >>= 8;

				// Add in current error estimate
				predSample += errorSign * delta[i];

				// Correct error estimate
				delta[i] = (delta[i] * error_coefficients_lut[error]) >> 8;
				// Clamp error estimate
				if (delta[i] < 16)
					delta[i] = 16;

				// clamp
				if (predSample > 32767)
					predSample = 32767;
				else if (predSample < -32768)
					predSample = -32768;

				// output
				@out[op++] = (short)predSample;
				// move samples over
				samp2[i] = samp1[i];
				samp1[i] = predSample;
			}
			count--;
		}
	}

	//-----------------------------------------------------------------------------
	// Purpose: Read data from the source and pass it to the appropriate decompress
	//			routine.
	// Output : Returns true if data was decoded, false if none.
	//-----------------------------------------------------------------------------
	[SkipLocalsInit]
	bool DecodeBlock() {
		Span<byte> tmpBlock = stackalloc byte[MAX_BLOCK_SIZE];
		scoped ReadOnlySpan<byte> data;
		int blockSize;
		int firstSample;

		// fixup position with possible loop
		AudioSourceWave source = (AudioSourceWave)this.data!.Source();
		offset = source.ConvertLoopedPosition(offset);

		if (offset >= totalBytes) {
			// no more data
			return false;
		}

		// can only decode in block sized chunks
		firstSample = offset % this.blockSize;
		offset = offset - firstSample;

		// adpcm must calculate and request correct block size for proper decoding
		// last block size may be truncated
		blockSize = totalBytes - offset;
		if (blockSize > this.blockSize)
			blockSize = this.blockSize;

		// get requested data
		int available = this.data.ReadSourceData(out data, offset, blockSize, default);
		if (available < blockSize) {
			// pump to get all of requested data
			int total = 0;
			while (available != 0 && total < blockSize) {
				data[..available].CopyTo(tmpBlock[total..]);
				total += available;
				available = this.data.ReadSourceData(out data, offset + total, blockSize - total, default);
			}
			data = tmpBlock;
			available = total;
		}

		if (available == 0) {
			// no more data
			return false;
		}

		// advance the file pointer
		offset += available;

		int channelCount = NumChannels();

		// this is sample pairs for stereo, samples for mono
		sampleCount = wSamplesPerBlock;

		// short block?, fixup sample count (2 samples per byte, divided by number of channels per sample set)
		sampleCount -= ((this.blockSize - available) * 2) / channelCount;

		// new block, start at the first sample
		samplePosition = firstSample;

		// no need to subclass for different channel counts...
		if (channelCount == 1)
			DecompressBlockMono(samples, data, sampleCount);
		else
			DecompressBlockStereo(samples, data, sampleCount);
		return true;
	}

	//-----------------------------------------------------------------------------
	// Purpose: Read existing buffer or decompress a new block when necessary
	// Input  : **pData - output data pointer
	//			sampleCount - number of samples (or pairs)
	// Output : int - available samples (zero to stop decoding)
	//-----------------------------------------------------------------------------
	public override int GetOutputData(out ReadOnlySpan<byte> data, int sampleCount, Span<byte> copyBuf) {
		data = default;
		if (samplePosition >= this.sampleCount) {
			if (!DecodeBlock())
				return 0;
		}

		if (samples != null && samplePosition < this.sampleCount) {
			data = MemoryMarshal.AsBytes(samples.AsSpan(samplePosition * NumChannels()));
			int available = this.sampleCount - samplePosition;
			if (available > sampleCount)
				available = sampleCount;

			samplePosition += available;

			// update count of max samples loaded in CAudioMixerWave
			sample_max_loaded += available;

			// update index of last sample loaded
			sample_loaded_index += available;

			return available;
		}

		return 0;
	}

	//-----------------------------------------------------------------------------
	// Purpose: Seek to a new position in the file
	//			NOTE: In most cases, only call this once, and call it before playing
	//			any data.
	// Input  : newPosition - new position in the sample clocks of this sample
	//-----------------------------------------------------------------------------
	public override void SetSampleStart(int newPosition) {
		// cascade to base wave to update sample counter
		base.SetSampleStart(newPosition);

		// which block is the desired starting sample in?
		int blockStart = newPosition / wSamplesPerBlock;
		// how far into the block is the sample
		int blockOffset = newPosition % wSamplesPerBlock;

		// set the file position
		offset = blockStart * blockSize;

		// NOTE: Must decode a block here to properly position the sample Index
		// THIS MEANS YOU DON'T WANT TO CALL THIS ROUTINE OFTEN FOR ADPCM SOUNDS
		DecodeBlock();

		// limit to the samples decoded
		if (blockOffset < sampleCount)
			blockOffset = sampleCount;

		// set the new current position
		samplePosition = blockOffset;
	}
}
