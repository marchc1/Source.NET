using System.Numerics;

namespace Source.Common;

public ref struct BoneSetup
{
	StudioHdr studioHdr;
	int boneMask;
	Span<float> poseParameter;
		
	public BoneSetup(StudioHdr studioHdr, int boneMask, Span<float> poseParameter) {
		this.studioHdr = studioHdr;
		this.boneMask = boneMask;
		this.poseParameter = poseParameter;
	}

	public void InitPose(Span<Vector3> pos, Span<Quaternion> q) => InitPose(studioHdr, pos, q, boneMask);
	public void InitPose(StudioHdr studioHdr, Span<Vector3> pos, Span<Quaternion> q, int boneMask) {
		if (studioHdr.LinearBones() == null) {
			for (int i = 0; i < studioHdr.NumBones(); i++) {
				if ((studioHdr.BoneFlags(i) & boneMask) != 0) {
					MStudioBone pbone = studioHdr.Bone(i);
					pos[i] = pbone.Position;
					q[i] = pbone.Quat;
				}
			}
		}
		else {
			MStudioLinearBone pLinearBones = studioHdr.LinearBones()!;
			for (int i = 0; i < studioHdr.NumBones(); i++) {
				if ((studioHdr.BoneFlags(i) & boneMask) != 0) {
					pos[i] = pLinearBones.Pos(i);
					q[i] = pLinearBones.Quat(i);
				}
			}
		}
	}

	public unsafe void AccumulatePose(Span<Vector3> pos, Span<Quaternion> q, int sequence, TimeUnit_t cycle, float weight, TimeUnit_t time, object? ikContext) {
		Span<Vector3> pos2 = stackalloc Vector3[Studio.MAXSTUDIOBONES];
		Span<Quaternion> q2 = stackalloc Quaternion[Studio.MAXSTUDIOBONES];

		Assert(weight >= 0.0f && weight <= 1.0f);
		// This shouldn't be necessary, but the Assert should help us catch whoever is screwing this up
		weight = Math.Clamp(weight, 0.0f, 1.0f);

		if (sequence < 0)
			return;

		MStudioSeqDesc seqdesc = studioHdr.Seqdesc(sequence);

		// TODO: IK stuff. We're ignoring it for now.

		if ((seqdesc.Flags & StudioAnimFlags.Local) != 0) 
			InitPose(studioHdr, pos2, q2, boneMask);

		if (CalcPoseSingle(studioHdr, pos2, q2, seqdesc, sequence, cycle, poseParameter, boneMask, time)) {
			AddLocalLayers(pos2, q2, seqdesc, sequence, cycle, 1.0, time, ikContext);
			SlerpBones(studioHdr, q, pos, seqdesc, sequence, q2, pos2, weight, boneMask);
		}

		// TODO: IK
		AddSequenceLayers(pos, q, seqdesc, sequence, cycle, weight, time, ikContext);
		// TODO: IK
	}

	private bool CalcPoseSingle(StudioHdr studioHdr, Span<Vector3> pos2, Span<Quaternion> q2, MStudioSeqDesc seqdesc, int sequence, double cycle, Span<float> poseParameter, int boneMask, double time) {
		throw new NotImplementedException();
	}

	private void AddLocalLayers(Span<Vector3> pos2, Span<Quaternion> q2, MStudioSeqDesc seqdesc, int sequence, double cycle, double v, double time, object? ikContext) {
		throw new NotImplementedException();
	}

	private void SlerpBones(StudioHdr studioHdr, Span<Quaternion> q, Span<Vector3> pos, MStudioSeqDesc seqdesc, int sequence, Span<Quaternion> q2, Span<Vector3> pos2, float weight, int boneMask) {
		throw new NotImplementedException();
	}

	private void AddSequenceLayers(Span<Vector3> pos, Span<Quaternion> q, MStudioSeqDesc seqdesc, int sequence, double cycle, float weight, double time, object? ikContext) {
		throw new NotImplementedException();
	}
}
