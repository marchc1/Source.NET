using CommunityToolkit.HighPerformance;

using Source;
using Source.Common;
using Source.Common.Mathematics;

using System.Numerics;

namespace Game.Client;

public class BoneMergeCache
{
	C_BaseAnimating? Owner;
	C_BaseAnimating? Follow;
	StudioHdr? FollowHdr;
	StudioHeader? FollowRenderHdr;
	StudioHdr? OwnerHdr;

	int FollowBoneSetupMask;

	struct MergedBone
	{
		public int MyBone;
		public int ParentBone;
	}

	readonly List<MergedBone> MergedBones = [];
	readonly List<byte> BoneMergeBits = [];

	public BoneMergeCache() {
		Owner = null;
		Follow = null;
		FollowHdr = null;
		FollowRenderHdr = null;
		OwnerHdr = null;
		FollowBoneSetupMask = 0;
	}

	public void Init(C_BaseAnimating owner) {
		Owner = owner;
		Follow = null;
		FollowHdr = null;
		FollowRenderHdr = null;
		OwnerHdr = null;
		FollowBoneSetupMask = 0;
	}

	public void UpdateCache() {
		StudioHdr? ownerHdr = Owner?.GetModelPtr();
		if (ownerHdr == null) {
			if (OwnerHdr != null) {
				// Owner's model got swapped out
				MergedBones.Clear();
				BoneMergeBits.Clear();
				Follow = null;
				FollowHdr = null;
				FollowRenderHdr = null;
				OwnerHdr = null;
				FollowBoneSetupMask = 0;
			}
			return;
		}

		C_BaseAnimating? testFollow = Owner!.FindFollowedEntity();
		StudioHdr? testHdr = testFollow?.GetModelPtr();
		StudioHeader? testStudioHDR = testHdr?.GetRenderHdr();
		if (testFollow != Follow || testHdr != FollowHdr || testStudioHDR != FollowRenderHdr || ownerHdr != OwnerHdr) {
			MergedBones.Clear();
			BoneMergeBits.Clear();

			// Update the cache.
			if (testFollow != null && testHdr != null) {
				Follow = testFollow;
				FollowHdr = testHdr;
				FollowRenderHdr = testStudioHDR;
				OwnerHdr = ownerHdr;

				for (int i = 0; i < OwnerHdr.NumBones() / 8 + 1; i++)
					BoneMergeBits.Add(0);

				FollowBoneSetupMask = Studio.BONE_USED_BY_BONE_MERGE;
				for (int i = 0; i < OwnerHdr.NumBones(); i++) {
					int parentBoneIndex = BoneSetup.Studio_BoneIndexByName(FollowHdr, OwnerHdr.Bone(i).Name());
					if (parentBoneIndex < 0)
						continue;

					// Add a merged bone here.
					MergedBone mergedBone = new() {
						MyBone = i,
						ParentBone = parentBoneIndex
					};
					MergedBones.Add(mergedBone);

					BoneMergeBits[i >> 3] |= (byte)(1 << (i & 7));

					if ((FollowHdr.BoneFlags(parentBoneIndex) & Studio.BONE_USED_BY_BONE_MERGE) == 0)
						FollowBoneSetupMask = Studio.BONE_USED_BY_ANYTHING;
				}

				// No merged bones found? Slam the mask to 0
				if (MergedBones.Count == 0)
					FollowBoneSetupMask = 0;
			}
			else {
				Follow = null;
				FollowHdr = null;
				FollowRenderHdr = null;
				OwnerHdr = null;
				FollowBoneSetupMask = 0;
			}
		}
	}

	public void MergeMatchingBones(int boneMask) {
		UpdateCache();

		// If this is set, then all the other cache data is set.
		if (OwnerHdr == null || MergedBones.Count == 0)
			return;

		ReadOnlySpan<MergedBone> mergedBones = MergedBones.AsSpan();

		// Have the entity we're following setup its bones.
		bool worked = Follow!.SetupBones(null, -1, FollowBoneSetupMask, gpGlobals.CurTime);
		// We suspect there's some cases where SetupBones couldn't do its thing, and then this causes Captain Canteen.
		Assert(worked);
		if (!worked) {
			// Usually this means your parent is invisible or gone or whatever.
			// This routine has no way to tell its caller not to draw itself unfortunately.
			// But we can shrink all the bones down to zero size.
			// But it might still spawn particle systems? :-(
			Matrix3x4 newBone = default;
			MathLib.MatrixScaleByZero(ref newBone);
			MathLib.MatrixSetTranslation(new Vector3(0.0f, 0.0f, 0.0f), ref newBone);

			foreach (MergedBone bone in mergedBones) {
				int ownerBone = bone.MyBone;

				// Only update bones reference by the bone mask.
				if ((OwnerHdr.BoneFlags(ownerBone) & boneMask) == 0)
					continue;

				Owner!.GetBoneForWrite(ownerBone) = newBone;
			}
		}
		else {
			// Now copy the bone matrices.
			foreach (MergedBone bone in mergedBones) {
				int ownerBone = bone.MyBone;
				int parentBone = bone.ParentBone;

				// Only update bones reference by the bone mask.
				if ((OwnerHdr.BoneFlags(ownerBone) & boneMask) == 0)
					continue;

				MathLib.MatrixCopy(Follow.GetBone(parentBone), out Owner!.GetBoneForWrite(ownerBone));
			}
		}
	}

	// copy bones instead of matrices
	public void CopyParentToChild(ReadOnlySpan<Vector3> parentPos, ReadOnlySpan<Quaternion> parentQ, Span<Vector3> childPos, Span<Quaternion> childQ, int boneMask) {
		UpdateCache();

		// If this is set, then all the other cache data is set.
		if (OwnerHdr == null || MergedBones.Count == 0)
			return;

		ReadOnlySpan<MergedBone> mergedBones = MergedBones.AsSpan();

		// Now copy the bone matrices.
		foreach (MergedBone bone in mergedBones) {
			int ownerBone = bone.MyBone;
			int parentBone = bone.ParentBone;

			if (OwnerHdr.BoneParent(ownerBone) == -1 || FollowHdr!.BoneParent(parentBone) == -1)
				continue;

			// Only update bones reference by the bone mask.
			if ((OwnerHdr.BoneFlags(ownerBone) & boneMask) == 0)
				continue;

			childPos[ownerBone] = parentPos[parentBone];
			childQ[ownerBone] = parentQ[parentBone];
		}
	}

	public void CopyChildToParent(ReadOnlySpan<Vector3> childPos, ReadOnlySpan<Quaternion> childQ, Span<Vector3> parentPos, Span<Quaternion> parentQ, int boneMask) {
		UpdateCache();

		// If this is set, then all the other cache data is set.
		if (OwnerHdr == null || MergedBones.Count == 0)
			return;

		ReadOnlySpan<MergedBone> mergedBones = MergedBones.AsSpan();

		// Now copy the bone matrices.
		foreach (MergedBone bone in mergedBones) {
			int ownerBone = bone.MyBone;
			int parentBone = bone.ParentBone;

			if (OwnerHdr.BoneParent(ownerBone) == -1 || FollowHdr!.BoneParent(parentBone) == -1)
				continue;

			// Only update bones reference by the bone mask.
			if ((OwnerHdr.BoneFlags(ownerBone) & boneMask) == 0)
				continue;

			parentPos[parentBone] = childPos[ownerBone];
			parentQ[parentBone] = childQ[ownerBone];
		}
	}

	// Returns true if the specified bone is one that gets merged in MergeMatchingBones.
	public int IsBoneMerged(int bone) {
		if (OwnerHdr != null)
			return BoneMergeBits[bone >> 3] & (1 << (bone & 7));
		else
			return 0;
	}

	// Gets the origin for the first merge bone on the parent.
	public bool GetAimEntOrigin(ref Vector3 absOrigin, ref QAngle absAngles) {
		UpdateCache();

		// If this is set, then all the other cache data is set.
		if (OwnerHdr == null || MergedBones.Count == 0)
			return false;

		// We want the abs origin such that if we put the entity there, the first merged bone
		// will be aligned. This way the entity will be culled in the correct position.
		//
		// ie: mEntity * mBoneLocal = mFollowBone
		// so: mEntity = mFollowBone * Inverse( mBoneLocal )
		//
		// Note: the code below doesn't take animation into account. If the attached entity animates
		// all over the place, then this won't get the right results.

		// Get mFollowBone.
		Follow!.SetupBones(null, -1, FollowBoneSetupMask, gpGlobals.CurTime);
		ref readonly Matrix3x4 followBone = ref Follow.GetBone(MergedBones[0].ParentBone);

		// Get Inverse( mBoneLocal )
		BoneSetup.SetupSingleBoneMatrix(OwnerHdr, Owner!.GetSequence(), 0, MergedBones[0].MyBone, out Matrix3x4 boneLocal);
		MathLib.MatrixInvert(in boneLocal, out Matrix3x4 boneLocalInv);

		// Now calculate mEntity = mFollowBone * Inverse( mBoneLocal )
		MathLib.ConcatTransforms(in followBone, in boneLocalInv, out Matrix3x4 entity);
		MathLib.MatrixAngles(in entity, out absAngles, out absOrigin);

		return true;
	}

	public bool GetRootBone(out Matrix3x4 rootBone) {
		UpdateCache();

		// If this is set, then all the other cache data is set.
		if (OwnerHdr == null || MergedBones.Count == 0) {
			rootBone = default;
			return false;
		}

		// Get mFollowBone.
		Follow!.SetupBones(null, -1, FollowBoneSetupMask, gpGlobals.CurTime);
		rootBone = Follow.GetBone(MergedBones[0].ParentBone);
		return true;
	}
}
