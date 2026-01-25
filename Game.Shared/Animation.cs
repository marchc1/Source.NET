using Source;
using Source.Common;
using Source.Common.Mathematics;

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Game.Shared;

public static class Animation
{
	public static StudioAnimSeqFlags GetSequenceFlags(StudioHdr? studioHdr, int sequence) {
		if (studioHdr == null || !studioHdr.SequencesAvailable() || sequence < 0 || sequence >= studioHdr.GetNumSeq())
			return 0;

		MStudioSeqDesc seqdesc = studioHdr.Seqdesc(sequence);
		return seqdesc.Flags;
	}

	internal static void GetSequenceLinearMotion(StudioHdr studioHdr, int sequence, InlineArrayMaxStudioPoseParam<float> poseParameter, out Vector3 vecReturn) {
		vecReturn = default;

		if (studioHdr == null) {
			Msg("Bad pstudiohdr in GetSequenceLinearMotion()!\n");
			return;
		}

		if (!studioHdr.SequencesAvailable())
			return;

		if (sequence < 0 || sequence >= studioHdr.GetNumSeq()) {
			// Don't spam on bogus model
			if (studioHdr.GetNumSeq() > 0)
				Msg($"Bad sequence ({sequence} out of {studioHdr.GetNumSeq()} max) in GetSequenceLinearMotion() for model '{studioHdr.Name()}'!\n");

			return;
		}

		BoneSetup.Studio_SeqMovement(studioHdr, sequence, 0, 1.0f, poseParameter, out vecReturn, out _);
	}

	internal static string GetSequenceName(StudioHdr? studioHdr, int sequence) {
		if (studioHdr == null || !studioHdr.SequencesAvailable() || sequence < 0 || sequence >= studioHdr.GetNumSeq()) {
			if (studioHdr != null)
				Msg($"Bad sequence in GetSequenceName() for model '{studioHdr.Name()}'!\n");
			return "Unknown";
		}

		MStudioSeqDesc seqdesc = studioHdr.Seqdesc(sequence);
		return seqdesc.Label();
	}
}
