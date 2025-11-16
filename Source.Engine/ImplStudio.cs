using Source.Common;
using Source.Common.Commands;
using Source.Common.DataCache;
using Source.Common.Engine;
using Source.Common.MaterialSystem;
using Source.Engine.Client;

using System.Net.NetworkInformation;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Source.Engine;

public enum ModelInstanceFlags {
	HasStaticLighting = 0x1,
	HasDiskCompiledColor = 0x2,
	DiskCompiledColorBad = 0x4,
	HasColorDAta = 0x8
}
public class ModelInstance {
	public IClientRenderable? Renderable;
	public Model? Model;
	public ModelInstanceFlags Flags;

	public LightingState CurrentLightingState;
	public LightingState AmbientLightingState;
}

public struct LightingState {
	public InlineArray6<Vector3> BoxColor;
	public int NumLights;
}

public class ModelRender : IModelRender
{
	ModelInstanceHandle_t curModelHandle;
	readonly Dictionary<ModelInstanceHandle_t, ModelInstance> ModelInstances = [];

	ModelInstanceHandle_t NewHandle() {
		ModelInstanceHandle_t handle = Interlocked.Increment(ref curModelHandle);
		ModelInstances[handle] = new();
		return handle;
	}
	void DeleteHandle(ModelInstanceHandle_t handle) {
		ModelInstances.Remove(handle);
	}

	public ModelInstanceHandle_t CreateInstance(IClientRenderable renderable) {
		Model? model = renderable.GetModel();

		// We're ok, allocate a new instance handle
		ModelInstanceHandle_t handle = NewHandle();
		ModelInstance instance = ModelInstances[handle];

		instance.Renderable = renderable;
		instance.Model = model;
		instance.Flags = 0;

		for (int i = 0; i < 6; ++i) 
			instance.AmbientLightingState.BoxColor[i].X = 1.0f;

		// TODO: static prop lighting, shadows, etc

		return handle;
	}
	public void DestroyInstance(ModelInstanceHandle_t modelInstance) {
		// TODO
	}

	public ref Matrix4x4 SetupModelState(IClientRenderable renderable) {
		throw new NotImplementedException(); // todo
	}

	public bool DrawModelSetup(ref ModelRenderInfo info, ref DrawModelState state, Span<Matrix4x4> customBoneToWorld, out Span<Matrix4x4> boneToWorldOut) {
		state.StudioHdr = MDLCache.GetStudioHdr(info.Model!.Studio);
		state.Renderable = info.Renderable;

		// r_entity todo

		// quick exit
		if (state.StudioHdr!.NumBodyParts == 0) {
			boneToWorldOut = default;
			return false;
		}
		
		state.ModelToWorld = info.ModelToWorld;

		Assert(info.Renderable != null);

		state.StudioHWData = MDLCache.GetHardwareData(info.Model!.Studio)!;
		if (state.StudioHWData == null) {
			boneToWorldOut = default;
			return false;
		}

		state.LOD = ComputeLOD(ref info, state.StudioHWData);
		int boneMask = Studio.BONE_USED_BY_VERTEX_AT_LOD(state.LOD);
		// Why isn't this always set?!?

		bool ok;
		if ((info.Flags & StudioFlags.Render) == 0) {
			// no rendering, just force a bone setup.  Don't copy the bones
			ok = info.Renderable.SetupBones(default, Studio.MAXSTUDIOBONES, boneMask, cl.GetTime());
			boneToWorldOut = default;
			return ok;
		}

		int boneCount = state.StudioHdr.NumBones;
		Span<Matrix4x4> boneToWorld = customBoneToWorld;
		if (customBoneToWorld.IsEmpty)
			boneToWorld = StudioRender.LockBoneMatrices(boneCount);
		
		ok = info.Renderable.SetupBones(boneToWorld, boneCount, boneMask, cl.GetTime());
		if (customBoneToWorld.IsEmpty) 
			StudioRender.UnlockBoneMatrices();
		
		if (!ok) {
			boneToWorldOut = default;
			return false;
		}

		boneToWorldOut = boneToWorld;

		state.DrawFlags = StudioRenderFlags.DrawEntireModel;
		if ((info.Flags & StudioFlags.TwoPass) != 0) {
			if ((info.Flags & StudioFlags.Transparency) != 0) 
				state.DrawFlags = StudioRenderFlags.DrawTranslucentOnly;
			else 
				state.DrawFlags = StudioRenderFlags.DrawOpaqueOnly;
		}

		if ((info.Flags & StudioFlags.StaticLighting) != 0) 
			state.DrawFlags |= StudioRenderFlags.DrawStaticLighting;

		if ((info.Flags & StudioFlags.ItemBlink) != 0) 
			state.DrawFlags |= StudioRenderFlags.DrawItemBlink;

		if ((info.Flags & StudioFlags.Wireframe) != 0) 
			state.DrawFlags |= StudioRenderFlags.DrawWireframe;

		if ((info.Flags & StudioFlags.NoShadows) != 0) 
			state.DrawFlags |= StudioRenderFlags.DrawNoShadows;

		if ((info.Flags & StudioFlags.ShadowDepthTexture) != 0) 
			state.DrawFlags |= StudioRenderFlags.ShadowDepthTexture;

		return true;
	}

	readonly ConVar r_lod = new("r_lod", "-1", 0, "");
	readonly IMDLCache MDLCache;
	readonly ClientState cl;
	readonly IStudioRender StudioRender;
	readonly IMaterialSystem materials;
	readonly Host Host;
	public ModelRender(IMDLCache MDLCache, ClientState cl, IStudioRender StudioRender, Host Host, IMaterialSystem materialSystem) {
		this.MDLCache = MDLCache;
		this.cl = cl;
		this.StudioRender = StudioRender;
		this.Host = Host;
		this.materials = materialSystem;

		r_lod.Changed += r_lod_f;
	}

	private void r_lod_f(IConVar var, in ConVarChangeContext ctx) {
		CheckVarRange_r_lod();
	}

	private bool CheckVarRange_r_lod() {
		return CheckVarRange_Generic(r_lod, -1, 2);
	}

	private bool CheckVarRange_Generic(ConVar var, int minVal, int maxVal) {
		if (!Host.CanCheat() && !Host.IsSinglePlayerGame()) {
			int clampedValue = Math.Clamp(var.GetInt(), minVal, maxVal);
			if (clampedValue != var.GetInt()) {
				Warning($"sv_cheats=0 prevented changing {var.GetName()} outside of the range [{minVal},{maxVal}] (was {var.GetInt()}).\n");
				var.SetValue(clampedValue);
			}
		}

		return false;
	}

	private int ComputeLOD(ref ModelRenderInfo info, StudioHWData studioHWData) {
		int lod = r_lod.GetInt();
		// FIXME!!!  This calc should be in studiorender, not here!!!!!  But since the bone setup
		// is done here, and we need the bone mask, we'll do it here for now.
		if (lod == -1) {
			using MatRenderContextPtr renderContext = new(materials);
			float screenSize = renderContext.ComputePixelWidthOfSphere(info.Renderable!.GetRenderOrigin(), 0.5f);
			float metric = studioHWData.LODMetric(screenSize);
			lod = studioHWData.GetLODForMetric(metric);
		}
		else {
			if ((info.Flags & (StudioFlags)StudioHdrFlags.HasShadowLod) != 0 && (lod > studioHWData.NumLODs - 2)) 
				lod = studioHWData.NumLODs - 2;
			else if (lod > studioHWData.NumLODs - 1) 
				lod = studioHWData.NumLODs - 1;
			else if (lod < 0) 
				lod = 0;
		}

		if (lod < 0) 
			lod = 0;
		else if (lod >= studioHWData.NumLODs) 
			lod = studioHWData.NumLODs - 1;

		if (lod < studioHWData.RootLOD) 
			lod = studioHWData.RootLOD;

		Assert(lod >= 0 && lod < studioHWData.NumLODs);
		return lod;
	}

	public void DrawModelExecute(ref DrawModelState state, ref ModelRenderInfo info, Span<Matrix4x4> boneToWorldArray) {

	}
}
