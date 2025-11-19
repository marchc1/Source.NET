using Source.Common.Commands;
using Source.Common.Mathematics;

using System.Buffers;
using System.Numerics;

namespace Source.Common;

public class BoneSetupMemoryPool<T> where T : struct
{
	readonly ArrayPool<T> instance = ArrayPool<T>.Create();

	public T[] Alloc() => instance.Rent(Studio.MAXSTUDIOBONES);
	public void Free(T[] p) => instance.Return(p, true);
}

public ref struct BoneSetup
{
	public static readonly ConVar anim_3wayblend = new("anim_3wayblend", "1", FCvar.Replicated, "Toggle the 3-way animation blending code.");

	static readonly BoneSetupMemoryPool<Vector3> VectorPool = new();
	static readonly BoneSetupMemoryPool<Quaternion> QuaternionPool = new();
	static readonly BoneSetupMemoryPool<Matrix3x4> MatrixPool = new();

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

	private static bool CalcPoseSingle(StudioHdr studioHdr, Span<Vector3> pos, Span<Quaternion> q, MStudioSeqDesc seqdesc, int sequence, double cycle, Span<float> poseParameter, int boneMask, double time) {
		bool bResult = true;

		Vector3[] pos2 = VectorPool.Alloc();
		Quaternion[] q2 = QuaternionPool.Alloc();
		Vector3[] pos3 = VectorPool.Alloc();
		Quaternion[] q3 = QuaternionPool.Alloc();

		if (sequence >= studioHdr.GetNumSeq()) {
			sequence = 0;
			seqdesc = studioHdr.Seqdesc(sequence);
		}

		int i0 = 0, i1 = 0;
		float s0 = 0, s1 = 0;

		Studio_LocalPoseParameter(studioHdr, poseParameter, seqdesc, sequence, 0, ref s0, ref i0);
		Studio_LocalPoseParameter(studioHdr, poseParameter, seqdesc, sequence, 1, ref s1, ref i1);

		if ((seqdesc.Flags & StudioAnimFlags.Realtime) != 0) {
			float cps = Studio_CPS(studioHdr, seqdesc, sequence, poseParameter);
			cycle = time * cps;
			cycle = cycle - (int)cycle;
		}
		else if ((seqdesc.Flags & StudioAnimFlags.CyclePose) != 0) {
			int iPose = studioHdr.GetSharedPoseParameter(sequence, seqdesc.CyclePoseIndex);
			if (iPose != -1) {
				cycle = poseParameter[iPose];
			}
			else {
				cycle = 0.0f;
			}
		}
		else if (cycle < 0 || cycle >= 1) {
			if ((seqdesc.Flags & StudioAnimFlags.Looping) != 0) {
				cycle = cycle - (int)cycle;
				if (cycle < 0) cycle += 1;
			}
			else {
				cycle = Math.Clamp(cycle, 0.0, 1.0);
			}
		}

		if (s0 < 0.001) {
			if (s1 < 0.001) {
				if (PoseIsAllZeros(studioHdr, sequence, seqdesc, i0, i1)) {
					bResult = false;
				}
				else {
					CalcAnimation(studioHdr, pos, q, seqdesc, sequence, seqdesc.Anim(i0, i1), cycle, boneMask);
				}
			}
			else if (s1 > 0.999) {
				CalcAnimation(studioHdr, pos, q, seqdesc, sequence, seqdesc.Anim(i0, i1 + 1), cycle, boneMask);
			}
			else {
				CalcAnimation(studioHdr, pos, q, seqdesc, sequence, seqdesc.Anim(i0, i1), cycle, boneMask);
				CalcAnimation(studioHdr, pos2, q2, seqdesc, sequence, seqdesc.Anim(i0, i1 + 1), cycle, boneMask);
				BlendBones(studioHdr, q, pos, seqdesc, sequence, q2, pos2, s1, boneMask);
			}
		}
		else if (s0 > 0.999) {
			if (s1 < 0.001) {
				if (PoseIsAllZeros(studioHdr, sequence, seqdesc, i0 + 1, i1)) {
					bResult = false;
				}
				else {
					CalcAnimation(studioHdr, pos, q, seqdesc, sequence, seqdesc.Anim(i0 + 1, i1), cycle, boneMask);
				}
			}
			else if (s1 > 0.999) {
				CalcAnimation(studioHdr, pos, q, seqdesc, sequence, seqdesc.Anim(i0 + 1, i1 + 1), cycle, boneMask);
			}
			else {
				CalcAnimation(studioHdr, pos, q, seqdesc, sequence, seqdesc.Anim(i0 + 1, i1), cycle, boneMask);
				CalcAnimation(studioHdr, pos2, q2, seqdesc, sequence, seqdesc.Anim(i0 + 1, i1 + 1), cycle, boneMask);
				BlendBones(studioHdr, q, pos, seqdesc, sequence, q2, pos2, s1, boneMask);
			}
		}
		else {
			if (s1 < 0.001) {
				if (PoseIsAllZeros(studioHdr, sequence, seqdesc, i0 + 1, i1)) {
					CalcAnimation(studioHdr, pos, q, seqdesc, sequence, seqdesc.Anim(i0, i1), cycle, boneMask);
					ScaleBones(studioHdr, q, pos, sequence, 1.0f - s0, boneMask);
				}
				else if (PoseIsAllZeros(studioHdr, sequence, seqdesc, i0, i1)) {
					CalcAnimation(studioHdr, pos, q, seqdesc, sequence, seqdesc.Anim(i0 + 1, i1), cycle, boneMask);
					ScaleBones(studioHdr, q, pos, sequence, s0, boneMask);
				}
				else {
					CalcAnimation(studioHdr, pos, q, seqdesc, sequence, seqdesc.Anim(i0, i1), cycle, boneMask);
					CalcAnimation(studioHdr, pos2, q2, seqdesc, sequence, seqdesc.Anim(i0 + 1, i1), cycle, boneMask);

					BlendBones(studioHdr, q, pos, seqdesc, sequence, q2, pos2, s0, boneMask);
				}
			}
			else if (s1 > 0.999) {
				CalcAnimation(studioHdr, pos, q, seqdesc, sequence, seqdesc.Anim(i0, i1 + 1), cycle, boneMask);
				CalcAnimation(studioHdr, pos2, q2, seqdesc, sequence, seqdesc.Anim(i0 + 1, i1 + 1), cycle, boneMask);
				BlendBones(studioHdr, q, pos, seqdesc, sequence, q2, pos2, s0, boneMask);
			}
			else if (!anim_3wayblend.GetBool()) {
				CalcAnimation(studioHdr, pos, q, seqdesc, sequence, seqdesc.Anim(i0, i1), cycle, boneMask);
				CalcAnimation(studioHdr, pos2, q2, seqdesc, sequence, seqdesc.Anim(i0 + 1, i1), cycle, boneMask);
				BlendBones(studioHdr, q, pos, seqdesc, sequence, q2, pos2, s0, boneMask);

				CalcAnimation(studioHdr, pos2, q2, seqdesc, sequence, seqdesc.Anim(i0, i1 + 1), cycle, boneMask);
				CalcAnimation(studioHdr, pos3, q3, seqdesc, sequence, seqdesc.Anim(i0 + 1, i1 + 1), cycle, boneMask);
				BlendBones(studioHdr, q2, pos2, seqdesc, sequence, q3, pos3, s0, boneMask);

				BlendBones(studioHdr, q, pos, seqdesc, sequence, q2, pos2, s1, boneMask);
			}
			else {
				Span<int> iAnimIndices = stackalloc int[3];
				Span<float> weight = stackalloc float[3];

				Calc3WayBlendIndices(i0, i1, s0, s1, seqdesc, iAnimIndices, weight);

				if (weight[1] < 0.001) {
					CalcAnimation(studioHdr, pos, q, seqdesc, sequence, iAnimIndices[0], cycle, boneMask);
					CalcAnimation(studioHdr, pos2, q2, seqdesc, sequence, iAnimIndices[2], cycle, boneMask);
					BlendBones(studioHdr, q, pos, seqdesc, sequence, q2, pos2, weight[2] / (weight[0] + weight[2]), boneMask);
				}
				else {
					CalcAnimation(studioHdr, pos, q, seqdesc, sequence, iAnimIndices[0], cycle, boneMask);
					CalcAnimation(studioHdr, pos2, q2, seqdesc, sequence, iAnimIndices[1], cycle, boneMask);
					BlendBones(studioHdr, q, pos, seqdesc, sequence, q2, pos2, weight[1] / (weight[0] + weight[1]), boneMask);

					CalcAnimation(studioHdr, pos3, q3, seqdesc, sequence, iAnimIndices[2], cycle, boneMask);
					BlendBones(studioHdr, q, pos, seqdesc, sequence, q3, pos3, weight[2], boneMask);
				}
			}
		}

		VectorPool.Free(pos2);
		QuaternionPool.Free(q2);
		VectorPool.Free(pos3);
		QuaternionPool.Free(q3);

		return bResult;
	}

	private static void Studio_LocalPoseParameter(StudioHdr studioHdr, Span<float> poseParameter, MStudioSeqDesc seqdesc, int sequence, int localIndex, ref float flSetting, ref int index) {
		if (studioHdr == null) {
			flSetting = 0;
			index = 0;
			return;
		}

		int iPose = studioHdr.GetSharedPoseParameter(sequence, seqdesc.ParamIndex[localIndex]);

		if (iPose == -1) {
			flSetting = 0;
			index = 0;
			return;
		}

		MStudioPoseParamDesc Pose = studioHdr.PoseParameter(iPose);

		float flValue = poseParameter[iPose];

		if (Pose.Loop != 0) {
			float wrap = (Pose.Start + Pose.End) / 2.0F + Pose.Loop / 2.0F;
			float shift = Pose.Loop - wrap;

			flValue = flValue - Pose.Loop * MathF.Floor((flValue + shift) / Pose.Loop);
		}

		if (seqdesc.PoseKeyIndex == 0) {
			float flLocalStart = ((float)seqdesc.ParamStart[localIndex] - Pose.Start) / (Pose.End - Pose.Start);
			float flLocalEnd = ((float)seqdesc.ParamEnd[localIndex] - Pose.Start) / (Pose.End - Pose.Start);

			flSetting = (flValue - flLocalStart) / (flLocalEnd - flLocalStart);

			if (flSetting < 0)
				flSetting = 0;
			if (flSetting > 1)
				flSetting = 1;

			index = 0;
			if (seqdesc.GroupSize[localIndex] > 2) {
				index = (int)(flSetting * (seqdesc.GroupSize[localIndex] - 1));
				if (index == seqdesc.GroupSize[localIndex] - 1) index = seqdesc.GroupSize[localIndex] - 2;
				flSetting = flSetting * (seqdesc.GroupSize[localIndex] - 1) - index;
			}
		}
		else {
			flValue = flValue * (Pose.End - Pose.Start) + Pose.Start;
			index = 0;

			// FIXME: this needs to be 2D
			// FIXME: this shouldn't be a linear search

			while (true) {
				flSetting = (flValue - seqdesc.PoseKey(localIndex, index)) / (seqdesc.PoseKey(localIndex, index + 1) - seqdesc.PoseKey(localIndex, index));
				if (index < seqdesc.GroupSize[localIndex] - 2 && flSetting > 1.0) {
					index++;
					continue;
				}
				break;
			}

			// clamp.
			if (flSetting < 0.0f)
				flSetting = 0.0f;
			if (flSetting > 1.0f)
				flSetting = 1.0f;
		}
	}

	private static void AddLocalLayers(Span<Vector3> pos2, Span<Quaternion> q2, MStudioSeqDesc seqdesc, int sequence, double cycle, double v, double time, object? ikContext) {

	}

	private static void SlerpBones(StudioHdr studioHdr, Span<Quaternion> q, Span<Vector3> pos, MStudioSeqDesc seqdesc, int sequence, Span<Quaternion> q2, Span<Vector3> pos2, float weight, int boneMask) {

	}

	private static void AddSequenceLayers(Span<Vector3> pos, Span<Quaternion> q, MStudioSeqDesc seqdesc, int sequence, double cycle, float weight, double time, object? ikContext) {

	}
}
