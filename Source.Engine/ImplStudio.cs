using Source.Common;
using Source.Common.Commands;
using Source.Common.DataCache;
using Source.Common.Engine;
using Source.Common.MaterialSystem;
using Source.Common.Mathematics;
using Source.Engine.Client;

using System.Drawing.Drawing2D;
using System.Net.NetworkInformation;
using System.Numerics;
using System.Runtime.CompilerServices;

using static Source.Common.Engine.IStaticPropMgrEngine;

using static Source.Common.OptimizedModel;

namespace Source.Engine;

public enum ModelInstanceFlags
{
	HasStaticLighting = 0x1,
	HasDiskCompiledColor = 0x2,
	DiskCompiledColorBad = 0x4,
	HasColorDAta = 0x8
}
public class ModelInstance
{
	public IClientRenderable? Renderable;
	public Model? Model;
	public ModelInstanceFlags Flags;

	public LightingState CurrentLightingState;
	public LightingState AmbientLightingState;
}

public struct LightingState
{
	public InlineArray6<Vector3> BoxColor;
	public int NumLights;
}

public class ModelRender : IModelRender
{
	static readonly ConVar r_drawmodelstatsoverlay = new("r_drawmodelstatsoverlay", "0", FCvar.Cheat);
	static readonly ConVar r_drawmodelstatsoverlaydistance = new("r_drawmodelstatsoverlaydistance", "500", FCvar.Cheat);
	static readonly ConVar r_drawmodellightorigin = new("r_DrawModelLightOrigin", "0", FCvar.Cheat);
	readonly ConVar r_lod = new("r_lod", "-1", 0, "");
	static readonly ConVar r_entity = new("r_entity", "-1", FCvar.Cheat | FCvar.DevelopmentOnly);
	static readonly ConVar r_lightaverage = new("r_lightaverage", "1", 0, "Activates/deactivate light averaging");
	static readonly ConVar r_lightinterp = new("r_lightinterp", "5", FCvar.Cheat, "Controls the speed of light interpolation, 0 turns off interpolation");
	static readonly ConVar r_eyeglintlodpixels = new("r_eyeglintlodpixels", "20.0", FCvar.Cheat, "The number of pixels wide an eyeball has to be before rendering an eyeglint.  Is a floating point value.");
	// static readonly ConVar r_rootlod = new( "r_rootlod", "0", FCvar.MaterialSystemThread | FCvar.Archive, "Root LOD", true, 0, true, Studio.MAX_NUM_LODS-1, SetRootLOD_f ); << todo
	static readonly ConVar r_decalstaticprops = new("r_decalstaticprops", "1", 0, "Decal static props test");
	// static readonly ConCommand r_flushlod = new( "r_flushlod", FlushLOD_f, "Flush and reload LODs." ); << todo
	static readonly ConVar r_debugrandomstaticlighting = new("r_debugrandomstaticlighting", "0", FCvar.Cheat, "Set to 1 to randomize static lighting for debugging.  Must restart for change to take affect.");
	static readonly ConVar r_proplightingfromdisk = new("r_proplightingfromdisk", "1", FCvar.Cheat, "0=Off, 1=On, 2=Show Errors");
	static readonly ConVar r_itemblinkmax = new("r_itemblinkmax", ".3", FCvar.Cheat);
	static readonly ConVar r_itemblinkrate = new("r_itemblinkrate", "4.5", FCvar.Cheat);
	static readonly ConVar r_proplightingpooling = new("r_proplightingpooling", "-1.0", FCvar.Cheat, "0 - off, 1 - static prop color meshes are allocated from a single shared vertex buffer (on hardware that supports stream offset)");

	static readonly ConVar r_showenvcubemap = new("r_showenvcubemap", "0", FCvar.Cheat);
	static readonly ConVar r_eyemove = new("r_eyemove", "1", FCvar.Archive); // look around
	static readonly ConVar r_eyeshift_x = new("r_eyeshift_x", "0", FCvar.Archive); // eye X position
	static readonly ConVar r_eyeshift_y = new("r_eyeshift_y", "0", FCvar.Archive); // eye Y position
	static readonly ConVar r_eyeshift_z = new("r_eyeshift_z", "0", FCvar.Archive); // eye Z position
	static readonly ConVar r_eyesize = new("r_eyesize", "0", FCvar.Archive); // adjustment to iris textures
	static readonly ConVar mat_softwareskin = new("mat_softwareskin", "0", FCvar.Cheat);
	static readonly ConVar r_nohw = new("r_nohw", "0", FCvar.Cheat);
	static readonly ConVar r_nosw = new("r_nosw", "0", FCvar.Cheat);
	static readonly ConVar r_teeth = new("r_teeth", "1", 0);
	static readonly ConVar r_drawentities = new("r_drawentities", "1", FCvar.Cheat);
	static readonly ConVar r_flex = new("r_flex", "1", 0);
	static readonly ConVar r_eyes = new("r_eyes", "1", 0);
	static readonly ConVar r_skin = new("r_skin", "0", FCvar.Cheat);
	static readonly ConVar r_modelwireframedecal = new("r_modelwireframedecal", "0", FCvar.Cheat);
	static readonly ConVar r_maxmodeldecal = new("r_maxmodeldecal", "50", 0);

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

	public bool DrawModelSetup(ref ModelRenderInfo info, ref DrawModelState state, Span<Matrix3x4> customBoneToWorld, out Span<Matrix3x4> boneToWorldOut) {
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
		Span<Matrix3x4> boneToWorld = customBoneToWorld;
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

	readonly IMDLCache MDLCache;
	readonly ClientState cl;
	readonly IStudioRender StudioRender;
	readonly IMaterialSystem materials;
	readonly RenderView RenderView;
	readonly Render Render;
	readonly Host Host;
	public ModelRender(IMDLCache MDLCache, ClientState cl, IStudioRender StudioRender, Host Host, Render Render, IRenderView renderView, IMaterialSystem materialSystem) {
		this.MDLCache = MDLCache;
		this.cl = cl;
		this.StudioRender = StudioRender;
		this.Host = Host;
		this.materials = materialSystem;
		this.Render = Render;
		this.RenderView = (RenderView)renderView;

		r_lod.Changed += r_lod_f;
	}

	protected void r_lod_f(IConVar var, in ConVarChangeContext ctx) {
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

	public void DrawModelExecute(ref DrawModelState state, ref ModelRenderInfo pInfo, Span<Matrix3x4> boneToWorldArray) {
#if !SWDS
		bool bShadowDepth = (pInfo.Flags & StudioFlags.ShadowDepthTexture) != 0;
		bool bSSAODepth = (pInfo.Flags & StudioFlags.SSAODepthTexture) != 0;

		if (bShadowDepth && ((pInfo.Model!.Flags & ModelFlag.DoNotCastShadows) != 0))
			return;

		// Shadow state...
		// ShadowMgr.SetModelShadowState(pInfo.Instance);

		//  if (textMode)
		//  	return;

		// TODO: Flexes

		// OPTIMIZE: Try to precompute part of this mess once a frame at the very least.
		bool bUsesBumpmapping = (pInfo.Model!.Flags & ModelFlag.UsesBumpMapping) != 0;

		bool bStaticLighting = (state.DrawFlags & StudioRenderFlags.DrawStaticLighting) != 0 &&
									(state.StudioHdr!.Flags & StudioHdrFlags.StaticProp) != 0 &&
									(!bUsesBumpmapping) &&
									(pInfo.Instance != MODEL_INSTANCE_INVALID);

		bool bVertexLit = (pInfo.Model.Flags & ModelFlag.VertexLit) != 0;

		bool bNeedsEnvCubemap = r_showenvcubemap.GetInt() != 0 || (pInfo.Model.Flags & ModelFlag.UsesEnvCubemap) != 0;

		// todo: r_drawmodellightorigin

		ColorMeshInfo[]? pColorMeshes = null;
		// TODO: static lighting

		DrawModelInfo info = default;
		info.StaticLighting = false;

		if ((bVertexLit || bNeedsEnvCubemap) && !bShadowDepth && !bSSAODepth) {
			// todo
		}

		// Set up the camera state
		StudioRender.SetViewState(in Render.CurrentViewOrigin, in Render.CurrentViewRight, in Render.CurrentViewUp, in Render.CurrentViewForward);

		// Color + alpha modulation
		StudioRender.SetColorModulation(RenderView.r_colormod);
		StudioRender.SetAlphaModulation(RenderView.r_blend);

		info.StudioHdr = state.StudioHdr!;
		info.HardwareData = state.StudioHWData;
		info.Skin = pInfo.Skin;
		info.Body = pInfo.Body;
		info.HitboxSet = pInfo.HitboxSet;
		info.ClientEntity = state.Renderable;
		info.Lod = state.LOD;
		info.ColorMeshes = pColorMeshes;

		// TODO: decals

		// TODO: perf stats
		StudioRenderFlags drawFlags = state.DrawFlags;

		if (bShadowDepth) {
			drawFlags |= StudioRenderFlags.DrawOpaqueOnly;
			drawFlags |= StudioRenderFlags.ShadowDepthTexture;
		}

		if (bSSAODepth == true) {
			drawFlags |= StudioRenderFlags.DrawOpaqueOnly;
			drawFlags |= StudioRenderFlags.SSAODepthTexture;
		}

		// TODO: perf stats
		DrawModelResults results = default;
		StudioRender.DrawModel(ref results, ref info, boneToWorldArray, null,
			null, in pInfo.Origin, drawFlags);

		// TODO: debug overlay

#endif
	}
}
