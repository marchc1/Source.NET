using CommunityToolkit.HighPerformance;

using Source;
using Source.Common;
using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.Mathematics;

using System.Numerics;
using System.Runtime.InteropServices;

namespace Game.Client;

public enum RenderFlags : byte
{
	TwoPass = 0x01,
	StaticProp = 0x02,
	BrushModel = 0x04,
	StudioModel = 0x08,
	HasChanged = 0x10,
	AlternateSorting = 0x20
}

public class ClientLeafSubSystemData
{
}

public struct RenderableInfo
{
	public IClientRenderable? Renderable;
	public long RenderFrame;
	public long RenderFrame2;
	public long EnumCount;
	public long TranslucencyCalculated;
	public uint LeafList;
	public uint RenderLeaf;
	public RenderFlags Flags;
	public RenderGroup RenderGroup;
	public uint FirstShadow;
	public short Area;
	public ViewID TranslucencyCalculatedView;
}

public struct ClientLeaf
{
	public uint FirstElement;
	public uint FirstShadow;
	public ushort FirstDetailProp;
	public ushort DetailPropCount;
	public int DetailPropRenderFrame;
	public InlineArray1<ClientLeafSubSystemData?> SubSystemData;
}

public struct ShadowInfo_t
{
	public uint FirstLeaf;
	public uint FirstRenderable;
	public int EnumCount;
	// public ClientShadowHandle_t Shadow;
	public ushort Flags;
}
public class EnumResult
{
	public int Leaf;
	public EnumResult? Next;
}

public class EnumResultList
{
	public EnumResult? Head;
	public ClientRenderHandle_t Handle;
}

class RenderableInfoBox
{
	public RenderableInfo Info = new();
}

public class ClientLeafSystem : IClientLeafSystem, ISpatialLeafEnumerator
{
	public const int CLSUBSYSTEM_DETAILOBJECTS = 0;
	public const int N_CLSUBSYSTEMS = 1;

	readonly List<ClientLeaf> Leaf = [];
	readonly List<ClientRenderHandle_t> DirtyRenderables = [];
	readonly List<ClientRenderHandle_t> ViewModels = [];
	readonly Dictionary<ClientRenderHandle_t, RenderableInfoBox> Renderables = [];
	readonly HashSet<ClientRenderHandle_t> ValidHandles = [];

	bool DrawStaticProps = true;

	readonly BidirectionalSet<int, ClientRenderHandle_t> RenderablesInLeaf = new();
	int ShadowEnum;
	readonly Queue<EnumResultList> DeferredInserts = new();

	public ClientLeafSystem() {
		RenderablesInLeaf.Init(FirstRenderableInLeaf, FirstLeafInRenderable);
		IClientLeafSystemEngine.DefaultRenderBoundsWorldspaceEv += DefaultRenderBoundsWorldspace;
	}

	ref uint FirstRenderableInLeaf(int leaf) => ref Leaf.AsSpan()[leaf].FirstElement;
	ref uint FirstLeafInRenderable(ClientRenderHandle_t renderable) => ref Renderables[renderable].Info.LeafList;

	void AddRenderableToLeaf(int leaf, ClientRenderHandle_t renderable) {
		RenderablesInLeaf.AddElementToBucket(leaf, renderable);
	}

	void RemoveFromTree(ClientRenderHandle_t handle) {
		RenderablesInLeaf.RemoveElement(handle);
		// todo
	}

	public bool EnumerateLeaf(int leaf, nint context) {
		EnumResultList list = (EnumResultList)GCHandle.FromIntPtr(context).Target!;
		if (ThreadInMainThread()) {
			AddRenderableToLeaf(leaf, list.Handle);
		}
		else {
			EnumResult p = new() { Leaf = leaf, Next = list.Head };
			list.Head = p;
		}
		return true;
	}

	void InsertIntoTree(ClientRenderHandle_t handle) {
		if (ThreadInMainThread())
			ShadowEnum++;

		EnumResultList list = new() { Head = null, Handle = handle };

		IClientRenderable renderable = Renderables[handle].Info.Renderable!;
		renderable.GetRenderBoundsWorldspace(out Vector3 absMins, out Vector3 absMaxs);
		Assert(absMins.IsValid() && absMaxs.IsValid());

		ISpatialQuery query = engine.GetBSPTreeQuery()!;
		GCHandle gcHandle = GCHandle.Alloc(list);
		try {
			query.EnumerateLeavesInBox(absMins, absMaxs, this, GCHandle.ToIntPtr(gcHandle));
		}
		finally {
			gcHandle.Free();
		}

		if (list.Head != null)
			DeferredInserts.Enqueue(list);
	}

	public void PreRender() {
		int iterations = 0;
		while (DirtyRenderables.Count != 0) {
			if (++iterations > 10) {
				Warning("Too many dirty renderables!\n");
				break;
			}

			int dirty = DirtyRenderables.Count;
			for (int i = dirty; --i >= 0;) {
				ClientRenderHandle_t handle = DirtyRenderables[i];
				Assert((Renderables[handle].Info.Flags & RenderFlags.HasChanged) != 0);
				RemoveFromTree(handle);
			}

			bool threaded = false;
			if (!threaded) {
				for (int i = dirty; --i >= 0;)
					InsertIntoTree(DirtyRenderables[i]);
			}
			// else: ParallelProcess

			if (DeferredInserts.Count != 0) {
				while (DeferredInserts.Count != 0) {
					EnumResultList enumResultList = DeferredInserts.Dequeue();
					ShadowEnum++;
					while (enumResultList.Head != null) {
						EnumResult p = enumResultList.Head;
						enumResultList.Head = p.Next;
						AddRenderableToLeaf(p.Leaf, enumResultList.Handle);
					}
				}
			}

			for (int i = dirty; --i >= 0;) {
				ClientRenderHandle_t handle = DirtyRenderables[i];
				ref RenderableInfo renderable = ref Renderables[handle].Info;

				renderable.Flags &= ~RenderFlags.HasChanged;
				// renderable.Area = (short)GetRenderableArea(handle); // todo: portal areas
			}

			DirtyRenderables.RemoveRange(0, dirty);
		}
	}

	public void Update(double frametime) { }
	public void PostRender() { }

	public static void DefaultRenderBoundsWorldspace(IClientRenderable renderable, out Vector3 absMins, out Vector3 absMaxs) {
		IClientUnknown unk = renderable.GetIClientUnknown();
		C_BaseEntity? ent = unk.GetBaseEntity();
		if (ent != null && ent.IsFollowingEntity()) {
			C_BaseEntity? parent = ent.GetFollowedEntity();
			if (parent != null) {
				// todo: CalcRenderableWorldSpaceAABB_Fast
			}
		}

		renderable.GetRenderBounds(out Vector3 mins, out Vector3 maxs);

		ref readonly QAngle angles = ref renderable.GetRenderAngles();
		ref readonly Vector3 origin = ref renderable.GetRenderOrigin();
		if (angles == vec3_angle) {
			MathLib.VectorAdd(mins, origin, out absMins);
			MathLib.VectorAdd(maxs, origin, out absMaxs);
		}
		else {
			MathLib.AngleMatrix(angles, origin, out Matrix3x4 boxToWorld);
			MathLib.TransformAABB(boxToWorld, mins, maxs, out absMins, out absMaxs);
		}
		Assert(absMins.IsValid() && absMaxs.IsValid());
	}

	ClientRenderHandle_t curHandleIdx;
	ClientRenderHandle_t AllocHandle() {
		ClientRenderHandle_t handle = Interlocked.Increment(ref curHandleIdx);
		ValidHandles.Add(handle);
		return handle;
	}

	public void SetSubSystemDataInLeaf(int leaf, int subSystemIdx, ClientLeafSubSystemData? data) {
		Assert(subSystemIdx < N_CLSUBSYSTEMS);
		if (!Leaf.IsValidIndex(leaf)) {
			Assert(false);
			return;
		}
		ref ClientLeaf l = ref Leaf.AsSpan()[leaf];
		l.SubSystemData[subSystemIdx] = data;
	}

	public ClientLeafSubSystemData? GetSubSystemDataInLeaf(int leaf, int subSystemIdx) {
		Assert(subSystemIdx < N_CLSUBSYSTEMS);
		if (!Leaf.IsValidIndex(leaf)) {
			Assert(false);
			return null;
		}
		return Leaf[leaf].SubSystemData[subSystemIdx];
	}

	public void SetDetailObjectsInLeaf(int leaf, int firstDetailObject, int detailObjectCount) {
		if (!Leaf.IsValidIndex(leaf)) {
			Assert(false);
			return;
		}
		ref ClientLeaf l = ref Leaf.AsSpan()[leaf];
		l.FirstDetailProp = (ushort)firstDetailObject;
		l.DetailPropCount = (ushort)detailObjectCount;
	}

	public void GetDetailObjectsInLeaf(int leaf, out int firstDetailObject, out int detailObjectCount) {
		firstDetailObject = 0;
		detailObjectCount = 0;
		if (!Leaf.IsValidIndex(leaf)) {
			Assert(false);
			return;
		}
		firstDetailObject = Leaf[leaf].FirstDetailProp;
		detailObjectCount = Leaf[leaf].DetailPropCount;
	}

	public void DrawDetailObjectsInLeaf(int leaf, int frameNumber, out int firstDetailObject, out int detailObjectCount) {
		ref ClientLeaf leafInfo = ref Leaf.AsSpan()[leaf];
		leafInfo.DetailPropRenderFrame = frameNumber;
		firstDetailObject = leafInfo.FirstDetailProp;
		detailObjectCount = leafInfo.DetailPropCount;
	}



	public void AddRenderable(IClientRenderable renderable, RenderGroup group) {
		RenderFlags flags = RenderFlags.HasChanged;
		if (group == RenderGroup.TwoPass) {
			group = RenderGroup.TranslucentEntity;
			flags |= RenderFlags.TwoPass;
		}

		NewRenderable(renderable, group, flags);
		ClientRenderHandle_t handle = renderable.RenderHandle();
		DirtyRenderables.Add(handle);
	}

	private void NewRenderable(IClientRenderable renderable, RenderGroup type, RenderFlags flags) {
		Assert(renderable);
		Assert(renderable.RenderHandle() == INVALID_CLIENT_RENDER_HANDLE);

		ClientRenderHandle_t handle = AllocHandle();
		Renderables[handle] = new();
		ValidHandles.Add(handle);
		ref RenderableInfo info = ref Renderables[handle].Info;

		// We need to know if it's a brush model for shadows
		ModelType modelType = modelinfo.GetModelType(renderable.GetModel());
		if (modelType == ModelType.Brush)
			flags |= RenderFlags.BrushModel;
		else if (modelType == ModelType.Studio)
			flags |= RenderFlags.StudioModel;

		info.Renderable = renderable;
		info.RenderFrame = -1;
		info.RenderFrame2 = -1;
		info.TranslucencyCalculated = -1;
		info.TranslucencyCalculatedView = ViewID.Illegal;
		info.FirstShadow = unchecked((uint)-1);
		info.LeafList = unchecked((uint)-1);
		info.Flags = flags;
		info.RenderGroup = type;
		info.EnumCount = 0;
		info.RenderLeaf = unchecked((uint)-1);
		if (IsViewModelRenderGroup(info.RenderGroup))
			AddToViewModelList(handle);

		renderable.RenderHandle() = handle;
	}

	private void AddToViewModelList(ClientRenderHandle_t handle) {
		Assert(ViewModels.Find(handle) == -1);
		ViewModels.Add(handle);
	}

	private bool IsViewModelRenderGroup(RenderGroup renderGroup) {
		return renderGroup == RenderGroup.ViewModelTranslucent || renderGroup == RenderGroup.ViewModelOpaque;
	}

	public void AddRenderableToLeaves(ClientRenderHandle_t handle, Span<ushort> leaves) {
		for (int j = 0; j < leaves.Length; ++j)
			AddRenderableToLeaf(leaves[j], handle);
		// Renderables[handle].Info.Area = GetRenderableArea(handle); // todo: portal areas
	}

	const ClientRenderHandle_t DETAIL_PROP_RENDER_HANDLE = 0xfffe;

	private static RenderGroup DetectBucketedRenderGroup(RenderGroup group, float dimension) {
		ReadOnlySpan<float> thresholds = [200.0f, 80.0f, 30.0f];

		Assert(thresholds.Length + 1 >= (int)RenderGroup_Config_t.NumOpaqueEntBuckets);
		Assert(group >= RenderGroup.OpaqueStatic && group <= RenderGroup.OpaqueEntity);

		int bucketedGroupIndex;
		if ((int)RenderGroup_Config_t.NumOpaqueEntBuckets <= 2 || dimension >= thresholds[1]) {
			if ((int)RenderGroup_Config_t.NumOpaqueEntBuckets <= 1 || dimension >= thresholds[0])
				bucketedGroupIndex = 0;
			else
				bucketedGroupIndex = 1;
		}
		else {
			if ((int)RenderGroup_Config_t.NumOpaqueEntBuckets <= 3 || dimension >= thresholds[2])
				bucketedGroupIndex = 2;
			else
				bucketedGroupIndex = 3;
		}

		RenderGroup bucketedGroup = group - (((int)RenderGroup_Config_t.NumOpaqueEntBuckets - 1) - bucketedGroupIndex) * 2;
		Assert(bucketedGroup >= RenderGroup.OpaqueStaticHuge && bucketedGroup <= RenderGroup.OpaqueEntity);
		return bucketedGroup;
	}

	public void BuildRenderablesList(in SetupRenderInfo info) {
		int leafCount = info.WorldListInfo.LeafCount;
		ref int translucentEntries = ref info.RenderList!.Count(RenderGroup.TranslucentEntity);

		for (int i = 0; i < leafCount; i++) {
			int translucent = translucentEntries;

			CollateRenderablesInLeaf(info.WorldListInfo.LeafList[i], i, in info);

			int newTranslucent = translucentEntries - translucent;
			if (newTranslucent != 0 && info.DrawTranslucentObjects)
				SortEntities(info.RenderOrigin, info.RenderForward, info.RenderList!, translucent, newTranslucent);
		}
	}

	private void CollateRenderablesInLeaf(LeafIndex_t leaf, int worldListLeafIndex, in SetupRenderInfo info) {
		AddRenderableToRenderList(info.RenderList!, null, (ushort)worldListLeafIndex, RenderGroup.OpaqueStatic);
		AddRenderableToRenderList(info.RenderList!, null, (ushort)worldListLeafIndex, RenderGroup.OpaqueEntity);

		for (int idx = RenderablesInLeaf.FirstElementInBucket(leaf); idx != BidirectionalSet<int, ClientRenderHandle_t>.InvalidIndex; idx = RenderablesInLeaf.NextElement(idx)) {
			ClientRenderHandle_t handle = RenderablesInLeaf.Element(idx);
			ref RenderableInfo renderable = ref Renderables[handle].Info;

			if (!DrawStaticProps && (renderable.Flags & RenderFlags.StaticProp) != 0)
				continue;

			if (renderable.RenderGroup != RenderGroup.TranslucentEntity) {
				if (renderable.RenderFrame2 == info.RenderFrame)
					continue;

				renderable.RenderFrame2 = info.RenderFrame;
			}
			else {
				if (renderable.RenderLeaf != leaf)
					continue;
			}

			byte alpha = 255;
			if (info.DrawTranslucentObjects) {
				alpha = (byte)renderable.Renderable!.GetFxBlend();
				if (alpha == 0)
					continue;
			}

			renderable.Renderable!.GetRenderBoundsWorldspace(out Vector3 absMins, out Vector3 absMaxs);

			// todo: portal area frustum cull (r_PortalTestEnts / DoesBoxTouchAreaFrustum)
			if (engine.CullBox(absMins, absMaxs))
				continue;

			// todo: occlusion query cull (engine.IsOccluded) for studio models

			if (renderable.RenderGroup != RenderGroup.TranslucentEntity) {
				RenderGroup group = renderable.RenderGroup;

				if ((int)RenderGroup_Config_t.NumOpaqueEntBuckets > 1 &&
					group >= RenderGroup.OpaqueStatic &&
					group <= RenderGroup.OpaqueEntity) {
					Vector3 dims = absMaxs - absMins;
					float dimension = MathF.Max(MathF.Max(MathF.Abs(dims.X), MathF.Abs(dims.Y)), MathF.Abs(dims.Z));
					group = DetectBucketedRenderGroup(group, dimension);
				}

				AddRenderableToRenderList(info.RenderList!, renderable.Renderable, (ushort)worldListLeafIndex, group, handle);
			}
			else {
				bool twoPass = (renderable.Flags & RenderFlags.TwoPass) != 0 && alpha == 255;

				if (info.DrawTranslucentObjects)
					AddRenderableToRenderList(info.RenderList!, renderable.Renderable, (ushort)worldListLeafIndex, renderable.RenderGroup, handle, twoPass);

				if (twoPass)
					AddRenderableToRenderList(info.RenderList!, renderable.Renderable, (ushort)worldListLeafIndex, RenderGroup.OpaqueEntity, handle, twoPass);
			}
		}

		if (info.DrawDetailObjects && ShouldDrawDetailObjectsInLeaf(leaf, (int)info.DetailBuildFrame)) {
			int idx = Leaf[leaf].FirstDetailProp;
			int count = Leaf[leaf].DetailPropCount;
			while (--count >= 0) {
				IClientRenderable? renderable = ((DetailObjectSystem)DetailObjectSystem.GetDetailObjectSystem()).GetDetailModel(idx);

				if (renderable != null) {
					if (renderable.IsTransparent()) {
						if (info.DrawTranslucentObjects) {
							if (renderable.GetFxBlend() > 0)
								AddRenderableToRenderList(info.RenderList!, renderable, (ushort)worldListLeafIndex, RenderGroup.TranslucentEntity, DETAIL_PROP_RENDER_HANDLE);
						}
					}
					else {
						AddRenderableToRenderList(info.RenderList!, renderable, (ushort)worldListLeafIndex, RenderGroup.OpaqueEntity, DETAIL_PROP_RENDER_HANDLE);
					}
				}
				++idx;
			}
		}
	}

	public bool ShouldDrawDetailObjectsInLeaf(int leaf, int frameNumber) {
		ref ClientLeaf leafInfo = ref Leaf.AsSpan()[leaf];
		return (leafInfo.DetailPropRenderFrame == frameNumber) && ((leafInfo.DetailPropCount != 0) || (leafInfo.SubSystemData[CLSUBSYSTEM_DETAILOBJECTS] != null));
	}

	private void SortEntities(in Vector3 renderOrigin, in Vector3 renderForward, ClientRenderablesList renderList, int firstEntity, int entities) {
		if (entities <= 1)
			return;

		Span<float> dists = stackalloc float[entities];

		int i;
		for (i = 0; i < entities; i++) {
			IClientRenderable renderable = renderList[RenderGroup.TranslucentEntity, firstEntity + i].Renderable!;

			renderable.GetRenderBounds(out Vector3 mins, out Vector3 maxs);
			Vector3 boxcenter = mins + maxs;
			MathLib.VectorMA(renderable.GetRenderOrigin(), 0.5f, boxcenter, out boxcenter);

			Vector3 delta = boxcenter - renderOrigin;
			dists[i] = Vector3.Dot(delta, renderForward);
		}

		int stepSize = 4;
		while (stepSize != 0) {
			int end = entities - stepSize;
			for (i = 0; i < end; i += stepSize) {
				if (dists[i] > dists[i + stepSize]) {
					(renderList[RenderGroup.TranslucentEntity, firstEntity + i], renderList[RenderGroup.TranslucentEntity, firstEntity + i + stepSize]) =
						(renderList[RenderGroup.TranslucentEntity, firstEntity + i + stepSize], renderList[RenderGroup.TranslucentEntity, firstEntity + i]);
					(dists[i], dists[i + stepSize]) = (dists[i + stepSize], dists[i]);

					if (i == 0)
						i = -stepSize;
					else
						i -= stepSize << 1;
				}
			}

			stepSize >>= 1;
		}
	}

	static readonly ConVar cl_drawleaf = new("cl_drawleaf", "-1", FCvar.Cheat);

	private void AddRenderableToRenderList(ClientRenderablesList renderList, IClientRenderable? renderable, ushort leaf, RenderGroup renderGroup, ClientRenderHandle_t renderHandle = 0, bool twoPass = false) {
		if (cl_drawleaf.GetInt() >= 0) {
			if (leaf != cl_drawleaf.GetInt())
				return;
		}

		Assert(renderGroup >= 0 && (int)renderGroup < ClientRenderablesList.RENDER_GROUP_COUNT);

		ref int curCount = ref renderList.RenderGroupCounts[(int)renderGroup];
		if (curCount < ClientRenderablesList.MAX_GROUP_ENTITIES) {
			Assert(leaf >= 0 && leaf <= 65535);

			ref ClientRenderablesList.Entry entry = ref renderList[renderGroup, curCount];
			entry.Renderable = renderable;
			entry.WorldListInfoLeaf = leaf;
			entry.TwoPass = twoPass;
			entry.RenderHandle = renderHandle;
			curCount++;
		}
		else {
			engine.Con_NPrintf(10, $"Warning: overflowed CClientRenderablesList group {(int)renderGroup}");
		}
	}

	public void ChangeRenderableRenderGroup(ClientRenderHandle_t handle, RenderGroup group) {
		ref RenderableInfo info = ref Renderables[handle].Info;
		info.RenderGroup = group;
	}

	public void CreateRenderableHandle(IClientRenderable? renderable, bool bIsStaticProp = false) {
		RenderGroup group = renderable!.IsTransparent() ? RenderGroup.TranslucentEntity : RenderGroup.OpaqueEntity;

		bool bTwoPass = false;
		if (group == RenderGroup.TranslucentEntity)
			bTwoPass = renderable.IsTwoPass();

		RenderFlags flags = 0;
		if (bIsStaticProp) {
			flags = RenderFlags.StaticProp;
			if (group == RenderGroup.OpaqueEntity)
				group = RenderGroup.OpaqueStatic;
		}

		if (bTwoPass)
			flags |= RenderFlags.TwoPass;

		NewRenderable(renderable, group, flags);
	}

	public void EnableAlternateSorting(ClientRenderHandle_t renderHandle, bool alternateSorting) {

	}

	public bool Init() => true;

	public bool IsPerFrame() => true;

	public bool IsRenderableInPVS(IClientRenderable renderable) {
		throw new NotImplementedException();
	}

	public void LevelInitPostEntity() { }

	public void LevelInitPreEntity() {
		Renderables.EnsureCapacity(1024);
		DirtyRenderables.EnsureCapacity(256);

		int leafCount = engine.LevelLeafCount();
		Leaf.EnsureCapacity(leafCount);

		ClientLeaf newLeaf = new() {
			FirstElement = unchecked((uint)-1),
			FirstShadow = unchecked((uint)-1),
			FirstDetailProp = 0,
			DetailPropCount = 0,
			DetailPropRenderFrame = -1
		};
		while (--leafCount >= 0)
			Leaf.Add(newLeaf);

#if DEBUG
		DevMsg($"ClientLeafSystem.LevelInitPreEntity: {Leaf.Count} leaves\n");
#endif
	}

	public void LevelShutdownPostEntity() {
		foreach (RenderableInfoBox box in Renderables.Values) {
			IClientRenderable? renderable = box.Info.Renderable;
			renderable?.RenderHandle() = INVALID_CLIENT_RENDER_HANDLE;
		}

		ViewModels.Clear();
		Renderables.Clear();
		Leaf.Clear();
		DirtyRenderables.Clear();
		ValidHandles.Clear();
	}

	public void LevelShutdownPreClearSteamAPIContext() { }

	public void LevelShutdownPreEntity() { }

	public ReadOnlySpan<char> Name() => "CClientLeafSystem";

	public void OnRestore() { }

	public void OnSave() { }

	public void PostInit() { }

	public void RemoveRenderable(ClientRenderHandle_t handle) {
		if (!ValidHandles.Contains(handle))
			return;

		IClientRenderable renderable = Renderables[handle].Info.Renderable!;
		renderable.RenderHandle() = INVALID_CLIENT_RENDER_HANDLE;

		if ((Renderables[handle].Info.Flags & RenderFlags.HasChanged) != 0) {
			var i = DirtyRenderables.IndexOf(handle);
			Assert(i != -1);
			DirtyRenderables.RemoveAt(i);
		}

		if (IsViewModelRenderGroup(Renderables[handle].Info.RenderGroup))
			RemoveFromViewModelList(handle);

		RemoveFromTree(handle);
		ValidHandles.Remove(handle);
		Renderables.Remove(handle);
	}

	public void RenderableChanged(ClientRenderHandle_t handle) {
		if (!ValidHandles.Contains(handle))
			return;

		if ((Renderables[handle].Info.Flags & RenderFlags.HasChanged) == 0) {
			Renderables[handle].Info.Flags |= RenderFlags.HasChanged;
			DirtyRenderables.Add(handle);
		}
		else {
			Assert(DirtyRenderables.IndexOf(handle) != -1);
		}
	}

	private void RemoveFromViewModelList(ClientRenderHandle_t handle) {
		int i = ViewModels.IndexOf(handle);
		Assert(i != -1);
		ViewModels.RemoveAt(i);
	}

	public void SafeRemoveIfDesired() { }

	public void SetRenderGroup(ClientRenderHandle_t handle, RenderGroup group) {
		ref RenderableInfo pInfo = ref Renderables[handle].Info;

		bool twoPass = false;
		if (group == RenderGroup.TwoPass) {
			twoPass = true;
			group = RenderGroup.TranslucentEntity;
		}

		if (twoPass)
			pInfo.Flags |= RenderFlags.TwoPass;
		else
			pInfo.Flags &= ~RenderFlags.TwoPass;

		bool bOldViewModelRenderGroup = IsViewModelRenderGroup(pInfo.RenderGroup);
		bool bNewViewModelRenderGroup = IsViewModelRenderGroup(group);
		if (bOldViewModelRenderGroup != bNewViewModelRenderGroup) {
			if (bOldViewModelRenderGroup) {
				RemoveFromViewModelList(handle);
			}
			else {
				AddToViewModelList(handle);
			}
		}

		pInfo.RenderGroup = group;
	}

	public void Shutdown() {
		throw new NotImplementedException();
	}

	public void CollateViewModelRenderables(List<IClientRenderable> opaque, List<IClientRenderable> translucent) {
		for (int i = ViewModels.Count - 1; i >= 0; --i) {
			ClientRenderHandle_t handle = ViewModels[i];
			ref RenderableInfo renderable = ref Renderables[handle].Info;

			// NOTE: In some cases, this removes the entity from the view model list
			renderable.Renderable!.ComputeFxBlend();

			// That's why we need to test RENDER_GROUP_OPAQUE_ENTITY - it may have changed in ComputeFXBlend()
			if (renderable.RenderGroup == RenderGroup.ViewModelOpaque || renderable.RenderGroup == RenderGroup.OpaqueEntity) {
				opaque.Add(renderable.Renderable);
			}
			else {
				translucent.Add(renderable.Renderable);
			}
		}
	}
}
