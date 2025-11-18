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

	public void InitPose(Span<Vector3> pos, Span<Quaternion> q) {
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

	public void AccumulatePose(Span<Vector3> positions, Span<Quaternion> angles, int sequence, TimeUnit_t cycle, double weight, double time) {

	}
}
