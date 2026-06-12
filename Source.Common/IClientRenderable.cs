using Source.Common.Engine;
using Source.Common.Mathematics;

using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Source.Common;

public enum ShadowType
{
	None = 0,
	Simple,
	RenderToTexture,
	RenderToTextureDynamic,  // the shadow is always changing state
	RenderToDepthTexture,
}

public interface IPVSNotify
{
	void OnPVSStatusChanged(bool inPVS);
}


public interface IClientRenderable
{
	IClientUnknown GetIClientUnknown();

	// Data accessors
	ref readonly Vector3 GetRenderOrigin();
	ref readonly QAngle GetRenderAngles();
	bool ShouldDraw();
	bool IsTransparent();
	bool UsesPowerOfTwoFrameBufferTexture();
	bool UsesFullFrameBufferTexture();

	ClientShadowHandle_t GetShadowHandle();

	// Used by the leaf system to store its render handle.
	ref ClientRenderHandle_t RenderHandle();

	// Render baby!
	Model? GetModel();
	int DrawModel(StudioFlags flags);

	// Get the body parameter
	int GetBody();

	// Determine alpha and blend amount for transparent objects based on render state info
	void ComputeFxBlend();
	int GetFxBlend();

	// Determine the color modulation amount
	void GetColorModulation(Span<float> color);

	// Returns false if the entity shouldn't be drawn due to LOD. 
	// (NOTE: This is no longer used/supported, but kept in the vtable for backwards compat)
	bool LODTest();

	// Call this to get the current bone transforms for the model.
	// currentTime parameter will affect interpolation
	// nMaxBones specifies how many matrices pBoneToWorldOut can hold. (Should be greater than or
	// equal to studiohdr_t::numbones. Use MAXSTUDIOBONES to be safe.)
	bool SetupBones(Span<Matrix3x4> boneToWorldOut, int maxBones, int boneMask, TimeUnit_t currentTime);

	void SetupWeights(Span<Matrix3x4> boneToWorld, Span<float> flexWeights, Span<float> flexDelayedWeights );
	void DoAnimationEvents();

	// Return this if you want PVS notifications. See IPVSNotify for more info.	
	// Note: you must always return the same value from this function. If you don't,
	// undefined things will occur, and they won't be good.
	IPVSNotify? GetPVSNotifyInterface();

	// Returns the bounds relative to the origin (render bounds)
	void GetRenderBounds(out Vector3 mins, out Vector3 maxs );

	// returns the bounds as an AABB in worldspace
	void GetRenderBoundsWorldspace(out Vector3 mins, out Vector3 maxs);

	// These normally call through to GetRenderAngles/GetRenderBounds, but some entities custom implement them.
	void GetShadowRenderBounds(out Vector3 mins, out Vector3 maxs, ShadowType shadowType);

	// Should this object be able to have shadows cast onto it?
	bool ShouldReceiveProjectedTextures(ShadowFlags flags);

	// These methods return true if we want a per-renderable shadow cast direction + distance
	bool GetShadowCastDistance(out float dist, ShadowType shadowType);
	bool GetShadowCastDirection(out Vector3 direction, ShadowType shadowType);

	// Other methods related to shadow rendering
	bool IsShadowDirty();
	void MarkShadowDirty(bool bDirty);

	// Iteration over shadow hierarchy
	IClientRenderable? GetShadowParent();
	IClientRenderable? FirstShadowChild();
	IClientRenderable? NextShadowPeer();

	// Returns the shadow cast type
	ShadowType ShadowCastType();

	// Create/get/destroy model instance
	void CreateModelInstance();
	ModelInstanceHandle_t GetModelInstance();

	// Returns the transform from RenderOrigin/RenderAngles to world
	ref readonly Matrix3x4 RenderableToWorldTransform();

	// Attachments
	int LookupAttachment(ReadOnlySpan<char> attachmentName );
	bool GetAttachment(int number, out Vector3 origin, out QAngle angles );
	bool GetAttachment(int number, out Matrix3x4 matrix );

	// Rendering clip plane, should be 4 floats, return value of NULL indicates a disabled render clip plane
	Span<float> GetRenderClipPlane();

	// Get the skin parameter
	int GetSkin();

	// Is this a two-pass renderable?
	bool IsTwoPass();

	void OnThreadedDrawSetup();

	bool UsesFlexDelayedWeights();

	void RecordToolMessage();

	bool IgnoresZBuffer();
}

public abstract class DefaultClientRenderable : IClientUnknown, IClientRenderable
{
	public void ComputeFxBlend() { }
	public virtual int DrawModel(StudioFlags flags) => 0;
	public Model? GetModel() => null;
	public IPVSNotify? GetPVSNotifyInterface() => null;
	public ref readonly ClientEntityHandle GetRefEHandle() { Debugger.Break(); Environment.Exit(-1); return ref Unsafe.NullRef<ClientEntityHandle>(); }
	public abstract ref readonly QAngle GetRenderAngles();
	public abstract void GetRenderBounds(out Vector3 mins, out Vector3 maxs);
	public void GetRenderBoundsWorldspace(out Vector3 mins, out Vector3 maxs) => IClientLeafSystemEngine.DefaultRenderBoundsWorldspace(this, out mins, out maxs);
	public abstract ref readonly Vector3 GetRenderOrigin();
	public abstract bool IsTransparent();
	public bool IsTwoPass() => false;
	public int GetFxBlend() => 255;
	public ref ClientRenderHandle_t RenderHandle() => ref m_RenderHandle;
	public void SetRefEHandle(in ClientEntityHandle handle) => Assert(false);
	public bool SetupBones(Span<Matrix3x4> boneToWorldOut, int maxBones, int boneMask, double currentTime) => true;
	public abstract bool ShouldDraw();

	public ClientRenderHandle_t m_RenderHandle;

	public IClientUnknown GetIClientUnknown() => this;
	public ICollideable? GetCollideable() => null;
	public IClientRenderable? GetClientRenderable() => this;
	public IClientNetworkable? GetClientNetworkable() => null;
	public IClientEntity? GetIClientEntity() => null;
	public IClientThinkable? GetClientThinkable() => null;

	public void RecordToolMessage() { }
	public bool IgnoresZBuffer() => false;

	public bool IsShadowDirty() { return false; }
	public void MarkShadowDirty(bool dirty) { }
	public IClientRenderable? GetShadowParent() { return null; }
	public IClientRenderable? FirstShadowChild() { return null; }
	public IClientRenderable? NextShadowPeer() { return null; }
	public void CreateModelInstance() { }
	public ModelInstanceHandle_t GetModelInstance() { return MODEL_INSTANCE_INVALID; }

	// Attachments
	public int LookupAttachment(ReadOnlySpan<char> name) { return -1; }
	public bool GetAttachment(int i, out Vector3 v, out QAngle a) { v = default; a = default; return false; }
	public bool GetAttachment(int i, out Matrix3x4 m) { m = default; return false; }

	public bool UsesPowerOfTwoFrameBufferTexture() {
		throw new NotImplementedException();
	}

	public bool UsesFullFrameBufferTexture() {
		throw new NotImplementedException();
	}

	public ushort GetShadowHandle() {
		throw new NotImplementedException();
	}

	public int GetBody() {
		throw new NotImplementedException();
	}

	public void GetColorModulation(Span<float> color) {

	}

	public bool LODTest() {
		throw new NotImplementedException();
	}

	public void SetupWeights(Span<Matrix3x4> boneToWorld, Span<float> flexWeights, Span<float> flexDelayedWeights) {
		throw new NotImplementedException();
	}

	public void DoAnimationEvents() {
		throw new NotImplementedException();
	}

	public void GetShadowRenderBounds(out Vector3 mins, out Vector3 maxs, ShadowType shadowType) {
		throw new NotImplementedException();
	}

	public bool ShouldReceiveProjectedTextures(ShadowFlags flags) {
		throw new NotImplementedException();
	}

	public bool GetShadowCastDistance(out float dist, ShadowType shadowType) {
		throw new NotImplementedException();
	}

	public bool GetShadowCastDirection(out Vector3 direction, ShadowType shadowType) {
		throw new NotImplementedException();
	}

	public ShadowType ShadowCastType() {
		throw new NotImplementedException();
	}

	public ref readonly Matrix3x4 RenderableToWorldTransform() {
		throw new NotImplementedException();
	}

	public Span<float> GetRenderClipPlane() {
		throw new NotImplementedException();
	}

	public int GetSkin() {
		throw new NotImplementedException();
	}

	public void OnThreadedDrawSetup() {
		throw new NotImplementedException();
	}

	public bool UsesFlexDelayedWeights() {
		throw new NotImplementedException();
	}
}
