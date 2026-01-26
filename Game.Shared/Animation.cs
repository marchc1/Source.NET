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

	internal static void GetSequenceLinearMotion(StudioHdr studioHdr, int sequence, ReadOnlySpan<float> poseParameter, out Vector3 vecReturn) {
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


	public static ReadOnlySpan<char> GetSequenceActivityName(StudioHdr? studiohdr, int sequence) {
		if (studiohdr == null || sequence < 0 || sequence >= studiohdr.GetNumSeq()) {
			if (studiohdr != null)
				Msg($"Bad sequence in GetSequenceActivityName() for model '{studiohdr.Name()}'!\n");
			return "Unknown";
		}

		MStudioSeqDesc seqdesc = studiohdr.Seqdesc(sequence);
		return seqdesc.ActivityName();
	}
	public static void SetActivityForSequence(StudioHdr studiohdr, int i) {
		int activityIndex;
		ReadOnlySpan<char> activityName;
		MStudioSeqDesc seqdesc = studiohdr.Seqdesc(i);

		seqdesc.Flags |= StudioAnimSeqFlags.Activity;

		activityName = GetSequenceActivityName(studiohdr, i);
		if (activityName[0] != '\0') {
			activityIndex = ActivityList.IndexForName(activityName);

			if (activityIndex == -1) {
				// Allow this now.  Animators can create custom activities that are referenced only on the client or by scripts, etc.
				//Warning( "***\nModel %s tried to reference unregistered activity: %s \n***\n", pstudiohdr->name, pszActivityName );
				//Assert(0);
				// HACK: the client and server don't share the private activity list so registering it on the client would hose the server
#if CLIENT_DLL
				seqdesc.Flags &= ~StudioAnimSeqFlags.Activity;
#else
				seqdesc.Activity = (int)ActivityList.RegisterPrivateActivity(activityName);
#endif
			}
			else {
				seqdesc.Activity = activityIndex;
			}
		}
	}

	public static void SetEventIndexForSequence(MStudioSeqDesc seqdesc) {
		seqdesc.Flags |= StudioAnimSeqFlags.Event;

		if (seqdesc.NumEvents == 0)
			return;

		for (int index = 0; index < (int)seqdesc.NumEvents; index++) {
			// TODO
			// Studio doesn't have events (yet)
		}
	}
	public static int FindTransitionSequence(StudioHdr? studiohdr, int currentSequence, int goalSequence, ref int dir){
		if (studiohdr == null)
			return goalSequence;

		if (!studiohdr.SequencesAvailable())
			return goalSequence;

		if ((currentSequence < 0) || (currentSequence >= studiohdr.GetNumSeq()))
			return goalSequence;

		if ((goalSequence < 0) || (goalSequence >= studiohdr.GetNumSeq())) {
			// asking for a bogus sequence.  Punt.
			Assert(0);
			return goalSequence;
		}


		// bail if we're going to or from a node 0
		if (studiohdr.EntryNode(currentSequence) == 0 || studiohdr.EntryNode(goalSequence) == 0) {
			dir = 1;
			return goalSequence;
		}

		int iEndNode;

		// Msg( "from %d to %d: ", pEndNode->iEndNode, pGoalNode->iStartNode );

		// check to see if we should be going forward or backward through the graph
		if (dir > 0) 
			iEndNode = studiohdr.ExitNode(currentSequence);
		else 
			iEndNode = studiohdr.EntryNode(currentSequence);

		// if both sequences are on the same node, just go there
		if (iEndNode == studiohdr.EntryNode(goalSequence)) {
			dir = 1;
			return goalSequence;
		}

		int iInternNode = studiohdr.GetTransition(iEndNode, studiohdr.EntryNode(goalSequence));

		// if there is no transitionial node, just go to the goal sequence
		if (iInternNode == 0)
			return goalSequence;

		int i;

		// look for someone going from the entry node to next node it should hit
		// this may be the goal sequences node or an intermediate node
		for (i = 0; i < studiohdr.GetNumSeq(); i++) {
			MStudioSeqDesc seqdesc = studiohdr.Seqdesc(i);
			if (studiohdr.EntryNode(i) == iEndNode && studiohdr.ExitNode(i) == iInternNode) {
				dir = 1;
				return i;
			}
			if (seqdesc.NodeFlags != 0) {
				if (studiohdr.ExitNode(i) == iEndNode && studiohdr.EntryNode(i) == iInternNode) {
					dir = -1;
					return i;
				}
			}
		}

		// Go ahead and jump to the goal sequence
		return goalSequence;
	}

	public static void IndexModelSequences(StudioHdr? studiohdr) {
		int i;

		if (studiohdr == null)
			return;

		if (!studiohdr.SequencesAvailable())
			return;

		for (i = 0; i < studiohdr.GetNumSeq(); i++) {
			SetActivityForSequence(studiohdr, i);
			SetEventIndexForSequence(studiohdr.Seqdesc(i));
		}

		studiohdr.SetActivityListVersion(ActivityList.Version);
	}
	public static void VerifySequenceIndex(StudioHdr? studiohdr) {
		if (studiohdr == null)
			return;

		if (studiohdr.GetActivityListVersion() != ActivityList.Version) {
			// this model's sequences have not yet been indexed by activity
			IndexModelSequences(studiohdr);
		}
	}

	public static int SelectWeightedSequence(StudioHdr? studiohdr, Activity activity, int curSequence = -1) {
		if (studiohdr == null)
			return 0;

		if (!studiohdr.SequencesAvailable())
			return 0;

		VerifySequenceIndex(studiohdr);

		return studiohdr.SelectWeightedSequence((int)activity, curSequence);
	}
}
