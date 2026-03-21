using Source.Common.Audio;
using Source.Common.Bitbuffers;
using Source.Common.Mathematics;
using Source.Common.Networking;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;
using System.Xml.Linq;

namespace Source.Common;

public static class SoundConstants
{
	public const int SOUND_SEQNUMBER_BITS = 10;
	public const int MAX_SNDLVL_BITS = 9;
	public const int SOUND_SEQNUMBER_MASK = (1 << SOUND_SEQNUMBER_BITS) - 1;
	public const float SOUND_DELAY_OFFSET = 0.1f;
	public const int MAX_SOUND_DELAY_MSEC_ENCODE_BITS = 13;

	public const int MAX_SOUND_DELAY_MSEC = (1 << (MAX_SOUND_DELAY_MSEC_ENCODE_BITS - 1)) - 1;

	public const int SND_FLAG_BITS_ENCODE = 12;

	public const int PITCH_NORM = 100;
	public const int PITCH_LOW = 95;
	public const int PITCH_HIGH = 120;

	public const float DEFAULT_SOUND_PACKET_VOLUME = 1.0f;
	public const int DEFAULT_SOUND_PACKET_PITCH = 100;
	public const float DEFAULT_SOUND_PACKET_DELAY = 0.0f;
}

public struct SoundInfo
{

	public int SequenceNumber;
	public int EntityIndex;
	public SoundEntityChannel Channel;
	public FileNameHandle_t Name;
	public Vector3 Origin;
	public Vector3 Direction;
	public float Volume;
	public SoundLevel Soundlevel;
	public bool Looping;
	public int Pitch;
	public int SpecialDSP;
	public Vector3 ListenerOrigin;
	public SoundFlags Flags;
	public int SoundNum;
	public float Delay;
	public bool IsSentence;
	public bool IsAmbient;
	public int SpeakerEntity;

	public void ReadDelta(ref SoundInfo delta, bf_read buffer, int nProtoVersion) {
		if (buffer.ReadOneBit() == 0)
			EntityIndex = delta.EntityIndex;
		else {
			if (buffer.ReadOneBit() != 0)
				EntityIndex = (int)buffer.ReadUBitLong(5);
			else
				EntityIndex = (int)buffer.ReadUBitLong(Constants.MAX_EDICT_BITS);
		}

		if (nProtoVersion > 22) {
			if (buffer.ReadOneBit() != 0)
				SoundNum = (int)buffer.ReadUBitLong(StringTableBits.g_MaxSoundIndexBits);
			else
				SoundNum = delta.SoundNum;
		}
		else {
			if (buffer.ReadOneBit() != 0)
				SoundNum = (int)buffer.ReadUBitLong(13);
			else
				SoundNum = delta.SoundNum;
		}

		if (nProtoVersion > 18) {
			if (buffer.ReadOneBit() != 0)
				Flags = (SoundFlags)buffer.ReadUBitLong(SND_FLAG_BITS_ENCODE);
			else
				Flags = delta.Flags;
		}
		else {
			if (buffer.ReadOneBit() != 0)
				Flags = (SoundFlags)buffer.ReadUBitLong(9);
			else
				Flags = delta.Flags;
		}

		if (buffer.ReadOneBit() != 0)
			Channel = (SoundEntityChannel)buffer.ReadUBitLong(3);
		else
			Channel = delta.Channel;

		IsAmbient = buffer.ReadOneBit() != 0;
		IsSentence = buffer.ReadOneBit() != 0;

		if (Flags != SoundFlags.Stop) {
			if (buffer.ReadOneBit() != 0)
				SequenceNumber = delta.SequenceNumber;
			else if (buffer.ReadOneBit() != 0)
				SequenceNumber = delta.SequenceNumber + 1;
			else
				SequenceNumber = (int)buffer.ReadUBitLong(SOUND_SEQNUMBER_BITS);

			if (buffer.ReadOneBit() != 0)
				Volume = (float)buffer.ReadUBitLong(7) / 127.0f;
			else
				Volume = delta.Volume;

			if (buffer.ReadOneBit() != 0)
				Soundlevel = (SoundLevel)buffer.ReadUBitLong(MAX_SNDLVL_BITS);
			else
				Soundlevel = delta.Soundlevel;

			if (buffer.ReadOneBit() != 0)
				Pitch = (int)buffer.ReadUBitLong(8);
			else
				Pitch = delta.Pitch;

			if (nProtoVersion > 21) {
				// These bit weren't written in version 19 and below
				if (buffer.ReadOneBit() != 0)
					SpecialDSP = (int)buffer.ReadUBitLong(8);
				else
					SpecialDSP = delta.SpecialDSP;
			}

			if (buffer.ReadOneBit() != 0) {
				// Up to 4096 msec delay
				Delay = (float)buffer.ReadSBitLong(MAX_SOUND_DELAY_MSEC_ENCODE_BITS) / 1000.0f; ;

				if (Delay < 0) {
					Delay *= 10.0f;
				}
				// bias results so that we only incur the precision loss on relatively large skipaheads
				Delay -= SOUND_DELAY_OFFSET;
			}
			else
				Delay = delta.Delay;

			{
				const float SCALE = 8.0f;
				const int BITS = (int)BitBuffer.COORD_INTEGER_BITS - 2;
				if (buffer.ReadOneBit() != 0) Origin.X = SCALE * buffer.ReadSBitLong(BITS); else Origin.X = delta.Origin.X;
				if (buffer.ReadOneBit() != 0) Origin.Y = SCALE * buffer.ReadSBitLong(BITS); else Origin.Y = delta.Origin.Y;
				if (buffer.ReadOneBit() != 0) Origin.Z = SCALE * buffer.ReadSBitLong(BITS); else Origin.Z = delta.Origin.Z;
			}

			if (buffer.ReadOneBit() != 0)
				SpeakerEntity = buffer.ReadSBitLong(Constants.MAX_EDICT_BITS + 1);
			else
				SpeakerEntity = delta.SpeakerEntity;
		}
		else {
			ClearStopFields();
		}
	}

	public void WriteDelta(ref SoundInfo delta, bf_write buffer, int nProtoVersion) {
		if (EntityIndex == delta.EntityIndex)
			buffer.WriteOneBit(0);
		else {
			buffer.WriteOneBit(1);
			if (EntityIndex <= 31) {
				buffer.WriteOneBit(1);
				buffer.WriteUBitLong((uint)EntityIndex, 5);
			}
			else {
				buffer.WriteOneBit(0);
				buffer.WriteUBitLong((uint)EntityIndex, Constants.MAX_EDICT_BITS);
			}
		}

		if (nProtoVersion > 22) {
			if (SoundNum == delta.SoundNum)
				buffer.WriteOneBit(0);
			else {
				buffer.WriteOneBit(1);
				buffer.WriteUBitLong((uint)SoundNum, StringTableBits.g_MaxSoundIndexBits);
			}
		}
		else {
			if (SoundNum == delta.SoundNum)
				buffer.WriteOneBit(0);
			else {
				buffer.WriteOneBit(1);
				buffer.WriteUBitLong((uint)SoundNum, 13);
			}
		}

		if (nProtoVersion > 18) {
			if (Flags == delta.Flags)
				buffer.WriteOneBit(0);
			else {
				buffer.WriteOneBit(1);
				buffer.WriteUBitLong((uint)Flags, SND_FLAG_BITS_ENCODE);
			}
		}
		else {
			if (Flags == delta.Flags)
				buffer.WriteOneBit(0);
			else {
				buffer.WriteOneBit(1);
				buffer.WriteUBitLong((uint)Flags, 9);
			}
		}

		if (Channel == delta.Channel)
			buffer.WriteOneBit(0);
		else {
			buffer.WriteOneBit(1);
			buffer.WriteUBitLong((uint)Channel, 3);
		}

		buffer.WriteOneBit(IsAmbient ? 1 : 0);
		buffer.WriteOneBit(IsSentence ? 1 : 0);

		if (Flags != SoundFlags.Stop) {
			if (SequenceNumber == delta.SequenceNumber)
				buffer.WriteOneBit(1);
			else if (SequenceNumber == delta.SequenceNumber + 1) {
				buffer.WriteOneBit(0);
				buffer.WriteOneBit(1);
			}
			else {
				buffer.WriteUBitLong(0, 2);
				buffer.WriteUBitLong((uint)SequenceNumber, SOUND_SEQNUMBER_BITS);
			}

			if (Volume == delta.Volume)
				buffer.WriteOneBit(0);
			else {
				buffer.WriteOneBit(1);
				buffer.WriteUBitLong((uint)(Volume * 127.0f), 7);
			}

			if (Soundlevel == delta.Soundlevel)
				buffer.WriteOneBit(0);
			else {
				buffer.WriteOneBit(1);
				buffer.WriteUBitLong((uint)Soundlevel, MAX_SNDLVL_BITS);
			}

			if (Pitch == delta.Pitch)
				buffer.WriteOneBit(0);
			else {
				buffer.WriteOneBit(1);
				buffer.WriteUBitLong((uint)Pitch, 8);
			}

			if (nProtoVersion > 21) {
				if (SpecialDSP == delta.SpecialDSP)
					buffer.WriteOneBit(0);
				else {
					buffer.WriteOneBit(1);
					buffer.WriteUBitLong((uint)SpecialDSP, 8);
				}
			}

			if (Delay == delta.Delay)
				buffer.WriteOneBit(0);
			else {
				buffer.WriteOneBit(1);
				float d = Delay + SOUND_DELAY_OFFSET;
				int iDelay = (int)(d * 1000.0f);
				iDelay = Math.Clamp(iDelay, -10 * MAX_SOUND_DELAY_MSEC, MAX_SOUND_DELAY_MSEC);
				if (iDelay < 0) iDelay /= 10;
				buffer.WriteSBitLong(iDelay, MAX_SOUND_DELAY_MSEC_ENCODE_BITS);
			}

			const float SCALE = 8.0f;
			const int BITS = (int)(BitBuffer.COORD_INTEGER_BITS - 2);
			if (Origin.X == delta.Origin.X) buffer.WriteOneBit(0); else { buffer.WriteOneBit(1); buffer.WriteSBitLong((int)(Origin.X / SCALE), BITS); }
			if (Origin.Y == delta.Origin.Y) buffer.WriteOneBit(0); else { buffer.WriteOneBit(1); buffer.WriteSBitLong((int)(Origin.Y / SCALE), BITS); }
			if (Origin.Z == delta.Origin.Z) buffer.WriteOneBit(0); else { buffer.WriteOneBit(1); buffer.WriteSBitLong((int)(Origin.Z / SCALE), BITS); }

			if (SpeakerEntity == delta.SpeakerEntity) buffer.WriteOneBit(0);
			else { buffer.WriteOneBit(1); buffer.WriteSBitLong(SpeakerEntity, Constants.MAX_EDICT_BITS + 1); }
		}
		else {
			ClearStopFields();
		}
	}

	private void ClearStopFields() {
		Volume = 0;
		Soundlevel = SoundLevel.LvlNone;
		Pitch = PITCH_NORM;
		SpecialDSP = 0;
		Name = 0;
		Delay = 0.0f;
		SequenceNumber = 0;

		Origin.Init();
		SpeakerEntity = -1;
	}

	public void SetDefault() {
		Delay = DEFAULT_SOUND_PACKET_DELAY;
		Volume = DEFAULT_SOUND_PACKET_VOLUME;
		Soundlevel = SoundLevel.LvlNorm;
		Pitch = DEFAULT_SOUND_PACKET_PITCH;
		SpecialDSP = 0;

		EntityIndex = 0;
		SpeakerEntity = -1;
		Channel = SoundEntityChannel.Static;
		SoundNum = 0;
		Flags = 0;
		SequenceNumber = 0;

		Name = 0;

		Looping = false;
		IsSentence = false;
		IsAmbient = false;

		Origin.Init();
		Direction.Init();
		ListenerOrigin.Init();
	}
}

public enum SpatializationType
{
	InCreation,
	InSpatialization
}

public ref struct SpatializationInfo
{
	public SpatializationType Type;
	public SoundInfo Info;
	public ref Vector3 Origin;
	public ref QAngle Angles;
	public ref float Radius;
}
