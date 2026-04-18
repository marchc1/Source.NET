#if CLIENT_DLL || GAME_DLL

using Source;
using Source.Common;

using System;
using System.Collections.Generic;
using System.Text;
using CommunityToolkit.HighPerformance;

#if GAME_DLL
using Game.Server;
#else
using Game.Client;
#endif

namespace Game.Shared;

public class SequenceTransitioner
{
	void ClearLayers() {
		AnimationQueue.Clear();
	}

	public void CheckForSequenceChange(StudioHdr? hdr, int curSequence, bool forceNewSequence, bool interpolate) {
		if (hdr == null)
			return;

		if (AnimationQueue.Count() == 0)
			AnimationQueue.Add(default);

		ref AnimationLayer currentblend = ref AnimationQueue.AsSpan()[^1];

		if (currentblend.LayerAnimtime != 0 && (currentblend.Sequence != curSequence || forceNewSequence)) {
			MStudioSeqDesc seqdesc = hdr.Seqdesc(curSequence);
			// sequence changed
			if ((seqdesc.Flags & StudioAnimSeqFlags.Snap) != 0 || !interpolate) {
				// remove all entries
				ClearLayers();
			}
			else {
				MStudioSeqDesc prevseqdesc = hdr.Seqdesc(currentblend.Sequence);
				currentblend.LayerFadeOuttime = Math.Min(prevseqdesc.FadeOutTime, seqdesc.FadeInTime);
			}

			AnimationQueue.Add(default);
			currentblend = ref AnimationQueue.AsSpan()[^1];
		}

		currentblend.Sequence = -1;
		currentblend.LayerAnimtime = 0.0;
		currentblend.LayerFadeOuttime = 0.0;
	}
	public void UpdateCurrent(StudioHdr? hdr, int curSequence, TimeUnit_t curCycle, TimeUnit_t curPlaybackRate, TimeUnit_t curTime) {
		if (hdr == null)
			return;

		if (AnimationQueue.Count == 0)
			AnimationQueue.Add(default);

		ref AnimationLayer currentblend = ref AnimationQueue.AsSpan()[^1];

		// keep track of current sequence
		currentblend.Sequence = curSequence;
		currentblend.LayerAnimtime = curTime;
		currentblend.Cycle = curCycle;
		currentblend.PlaybackRate = curPlaybackRate;

		// calc blending weights for previous sequences
		int i;
		Span<AnimationLayer> animationQueue = AnimationQueue.AsSpan();
		for (i = 0; i < animationQueue.Length - 1;) {
			double s = animationQueue[i].GetFadeout(curTime);

			if (s > 0) {
				animationQueue[i].Weight = (float)s;
				i++;
			}
			else {
				AnimationQueue.RemoveAt(i);
				animationQueue = AnimationQueue.AsSpan();
			}
		}
	}

	public void RemoveAll() => ClearLayers();

	public readonly List<AnimationLayer> AnimationQueue = [];
}
#endif
