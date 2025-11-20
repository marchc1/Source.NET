using Source.Common;

using System;
using System.Collections.Generic;
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

	internal static string GetSequenceName(StudioHdr? studioHdr, int sequence) {
		if (studioHdr == null || !studioHdr.SequencesAvailable() || sequence < 0 || sequence >= studioHdr.GetNumSeq()) {
			if(studioHdr != null)
				Msg($"Bad sequence in GetSequenceName() for model '{studioHdr.Name()}'!\n");
			return "Unknown";
		}

		MStudioSeqDesc seqdesc = studioHdr.Seqdesc(sequence);
		return seqdesc.Label();
	}
}
