using Game.Shared;

using Source.Common;

using System.Numerics;

using FIELD = Source.FIELD<Game.Client.C_BaseAnimatingOverlay>;

namespace Game.Client;

public partial class C_BaseAnimatingOverlay : C_BaseAnimating {
	public const int MAX_OVERLAYS = 15;

	public static readonly RecvTable DT_OverlayVars = new([
		RecvPropList<AnimationLayerRef>(FIELD.OF_LIST(nameof(AnimOverlay), MAX_OVERLAYS), ResizeAnimationLayerCallback, RecvPropDataTable(null, AnimationLayerRef.DT_AnimationLayer))
	]);

	public static readonly string[] iv_AnimOverlayNames = [
		"C_BaseAnimatingOverlay.iv_AnimOverlay00",
		"C_BaseAnimatingOverlay.iv_AnimOverlay01",
		"C_BaseAnimatingOverlay.iv_AnimOverlay02",
		"C_BaseAnimatingOverlay.iv_AnimOverlay03",
		"C_BaseAnimatingOverlay.iv_AnimOverlay04",
		"C_BaseAnimatingOverlay.iv_AnimOverlay05",
		"C_BaseAnimatingOverlay.iv_AnimOverlay06",
		"C_BaseAnimatingOverlay.iv_AnimOverlay07",
		"C_BaseAnimatingOverlay.iv_AnimOverlay08",
		"C_BaseAnimatingOverlay.iv_AnimOverlay09",
		"C_BaseAnimatingOverlay.iv_AnimOverlay10",
		"C_BaseAnimatingOverlay.iv_AnimOverlay11",
		"C_BaseAnimatingOverlay.iv_AnimOverlay12",
		"C_BaseAnimatingOverlay.iv_AnimOverlay13",
		"C_BaseAnimatingOverlay.iv_AnimOverlay14"
	];

	private static void ResizeAnimationLayerCallback(object instance, object list, int len) {
		C_BaseAnimatingOverlay ent = (C_BaseAnimatingOverlay)instance;
		List<AnimationLayerRef> vec = ent.AnimOverlay;
		List<InterpolatedVar<AnimationLayer>> iv = ent.iv_AnimOverlay;

		Assert(list == vec);
		Assert(vec.Count == iv.Count);
		Assert(vec.Count <= MAX_OVERLAYS);

		int diff = len - vec.Count;

		if (diff == 0)
			return;

		// remove all entries
		for (int i = 0; i < vec.Count; i++) 
			ent.RemoveVar(vec[i], AnimationLayerRef.Accessor);

		// adjust vector sizes
		if (diff > 0) {
			for (int i = 0; i < diff; i++) {
				vec.Add(new());
				iv.Add(new());
			}
		}
		else {
			for (int i = 0; i < -diff; i++) {
				if(vec.Count > 0)
				vec.RemoveAt(vec.Count - 1);
				if(iv.Count > 0)
				iv.RemoveAt(vec.Count - 1);
			}
		}

		// Rebind all the variables in the ent's list.
		for (int i = 0; i < len; i++) {
			IInterpolatedVar watcher = iv[i];
			watcher.SetDebugName(iv_AnimOverlayNames[i]);
			ent.AddVar(vec[i], AnimationLayerRef.Accessor, watcher, LatchFlags.LatchAnimationVar, true);
		}
	}

	public static readonly ClientClass CC_OverlayVars = new ClientClass("OverlayVars", null, null, DT_OverlayVars);

	public static readonly RecvTable DT_BaseAnimatingOverlay = new(DT_BaseAnimating, [
		RecvPropDataTable("overlay_vars", DT_OverlayVars)
	]); public static readonly new ClientClass ClientClass = new ClientClass("BaseAnimatingOverlay", null, null, DT_BaseAnimatingOverlay);

	readonly List<AnimationLayerRef> AnimOverlay = [];
	readonly List<InterpolatedVar<AnimationLayer>> iv_AnimOverlay = [];
	readonly float[] OverlayPrevEventCycle = new float[MAX_OVERLAYS];

	public int GetNumAnimOverlays() => AnimOverlay.Count;
	public void SetNumAnimOverlays(int num){
		if (AnimOverlay.Count < num) 
			for (int i = 0, diff = num - AnimOverlay.Count; i < diff; i++) 
				AnimOverlay.Add(new());
		else if (AnimOverlay.Count > num) 
			for (int i = 0, diff = AnimOverlay.Count - num; i < diff; i++)
				AnimOverlay.RemoveAt(AnimOverlay.Count - 1);
	}
	public void CheckForLayerChanges(StudioHdr hdr, TimeUnit_t currentTime){
	
	}
	public override void AccumulateLayers(ref BoneSetup boneSetup, Span<AngularImpulse> pos, Span<Quaternion> q, double currentTime) {
		base.AccumulateLayers(ref boneSetup, pos, q, currentTime);

		int i;

		// resort the layers
		Span<int> layer = stackalloc int[MAX_OVERLAYS];
		for (i = 0; i < MAX_OVERLAYS; i++) 
			layer[i] = MAX_OVERLAYS;
		
		for (i = 0; i < AnimOverlay.Count; i++) {
			if (AnimOverlay[i].Order < MAX_OVERLAYS) {
				if (layer[AnimOverlay[i].Order] != MAX_OVERLAYS) 
					AnimOverlay[i].Order = MAX_OVERLAYS;
				else 
					layer[AnimOverlay[i].Order] = i;
			}
		}

		CheckForLayerChanges(boneSetup.GetStudioHdr(), currentTime);

		int nSequences = boneSetup.GetStudioHdr().GetNumSeq();

		// add in the overlay layers
		int j;
		for (j = 0; j < MAX_OVERLAYS; j++) {
			i = layer[j];
			if (i < AnimOverlay.Count) {
				if (AnimOverlay[i].Sequence >= nSequences) 
					continue;
				
				AnimOverlay[i].BlendWeight();

				float fWeight = AnimOverlay[i].Weight;

				if (fWeight > 0) {
					// check to see if the sequence changed
					// FIXME: move this to somewhere more reasonable
					// do a nice spline interpolation of the values
					// if ( m_AnimOverlay[i].m_nSequence != m_iv_AnimOverlay.GetPrev( i )->nSequence )
					TimeUnit_t fCycle = AnimOverlay[i].Cycle;

					fCycle = ClampCycle(fCycle, IsSequenceLooping(AnimOverlay[i].Sequence));

					if (fWeight > 1)
						fWeight = 1;

					boneSetup.AccumulatePose(pos, q, AnimOverlay[i].Sequence, fCycle, fWeight, currentTime, null);


				}
			}

		}
	}
	public override void DoAnimationEvents(StudioHdr hdr) {
		base.DoAnimationEvents(hdr);
	}
	protected override StudioHdr? OnNewModel() {
		StudioHdr? hdr =  base.OnNewModel();
		for (int i = 0; i < AnimOverlay.Count; i++) {
			AnimOverlay[i].Reset();
			AnimOverlay[i].Order = MAX_OVERLAYS;
		}
		return hdr;
	}
}
