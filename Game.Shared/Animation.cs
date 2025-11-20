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
}
