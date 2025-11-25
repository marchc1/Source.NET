using System.Numerics;

namespace Source.Common.Engine;


public enum RenderGroup_Config_t
{
	NumOpaqueEntBuckets = 4
}

public enum RenderGroup
{
	OpaqueStaticHuge = 0,
	OpaqueEntityHuge = 1,
	OpaqueStatic = OpaqueStaticHuge + (RenderGroup_Config_t.NumOpaqueEntBuckets - 1) * 2,
	OpaqueEntity,

	TranslucentEntity,
	TwoPass,
	ViewModelOpaque,
	ViewModelTranslucent,

	OpaqueBrush,

	Other,

	Count
}

public interface IClientLeafSystemEngine
{
	public delegate void DefaultRenderBoundsWorldspaceFn(IClientRenderable renderable, out Vector3 mins, out Vector3 maxs);
	public static event DefaultRenderBoundsWorldspaceFn? DefaultRenderBoundsWorldspaceEv;
	public static void DefaultRenderBoundsWorldspace(IClientRenderable renderable, out Vector3 mins, out Vector3 maxs) {
		if(DefaultRenderBoundsWorldspaceEv == null) {
			mins = default;
			maxs = default;
			return;
		}
		DefaultRenderBoundsWorldspaceEv(renderable, out mins, out maxs);
	}

	void CreateRenderableHandle(IClientRenderable? renderable, bool bIsStaticProp = false);
	void RemoveRenderable(ClientRenderHandle_t handle);
	void AddRenderableToLeaves(ClientRenderHandle_t renderable, Span<ushort> pLeaves);
	void ChangeRenderableRenderGroup(ClientRenderHandle_t handle, RenderGroup group);
};
