using Source.Common.Engine;
using Source.Common.Mathematics;

using System.Diagnostics;
using System.Numerics;

namespace Source.Common;

public interface IClientRenderable {
	IClientUnknown GetIClientUnknown();
	ref readonly Vector3 GetRenderOrigin();
	ref readonly QAngle GetRenderAngles();
	bool ShouldDraw();
	bool IsTransparent();
	Model? GetModel();
	int DrawModel(StudioFlags flags);
	bool SetupBones(Span<Matrix3x4> boneToWorldOut, int maxBones, int boneMask, double currentTime);
	void GetRenderBounds(out Vector3 mins, out Vector3 maxs);
	void GetRenderBoundsWorldspace(out Vector3 mins, out Vector3 maxs);
	ref ClientRenderHandle_t RenderHandle();
	IPVSNotify? GetPVSNotifyInterface();
	void ComputeFxBlend();
	bool IsTwoPass();
	public void GetColorModulation(Span<float> color) => color[0] = color[1] = color[2] = 1.0f;
	bool GetAttachment(int attachmentIndex, out Matrix3x4 attachment);
}

public abstract class DefaultClientRenderable : IClientUnknown, IClientRenderable
{
	public virtual void ComputeFxBlend() { }
	public virtual int DrawModel(StudioFlags flags) => 0;
	public virtual Model? GetModel() => null;
	public virtual IPVSNotify? GetPVSNotifyInterface() => null;
	public virtual ClientEntityHandle? GetRefEHandle() { Debugger.Break(); Environment.Exit(-1); return null; }
	public abstract ref readonly QAngle GetRenderAngles();
	public abstract void GetRenderBounds(out Vector3 mins, out Vector3 maxs);
	public virtual void GetRenderBoundsWorldspace(out Vector3 mins, out Vector3 maxs) => IClientLeafSystemEngine.DefaultRenderBoundsWorldspace(this, out mins, out maxs);
	public abstract ref readonly Vector3 GetRenderOrigin();
	public abstract bool IsTransparent();
	public virtual bool IsTwoPass() => false;
	public virtual int GetFxBlend() => 255;
	public virtual ref ClientRenderHandle_t RenderHandle() => ref m_RenderHandle;
	public virtual void SetRefEHandle(ClientEntityHandle handle) => Assert(false);
	public virtual bool SetupBones(Span<Matrix3x4> boneToWorldOut, int maxBones, int boneMask, double currentTime) => true;
	public abstract bool ShouldDraw();

	public ClientRenderHandle_t m_RenderHandle;

	public virtual IClientUnknown GetIClientUnknown() => this;
	public virtual ICollideable? GetCollideable() => null;
	public virtual IClientRenderable? GetClientRenderable() => this;
	public virtual IClientNetworkable? GetClientNetworkable() => null;
	public virtual IClientEntity? GetIClientEntity() => null;
	public virtual IClientThinkable? GetClientThinkable() => null;

	public virtual void RecordToolMessage() { }
	public virtual bool IgnoresZBuffer() => false;

	public virtual bool IsShadowDirty() { return false; }
	public virtual void MarkShadowDirty(bool dirty) { }
	public virtual IClientRenderable? GetShadowParent() { return null; }
	public virtual IClientRenderable? FirstShadowChild() { return null; }
	public virtual IClientRenderable? NextShadowPeer() { return null; }
	public virtual void CreateModelInstance() { }
	public virtual ModelInstanceHandle_t GetModelInstance() { return MODEL_INSTANCE_INVALID; }

	// Attachments
	public virtual int LookupAttachment(ReadOnlySpan<char> name) { return -1; }
	public virtual bool GetAttachment(int i, out Vector3 v, out QAngle a) { v = default; a = default; return false; }
	public virtual bool GetAttachment(int i, out Matrix3x4 m ) { m = default; return false; }
}
