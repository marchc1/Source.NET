using Source.Common.Engine;
using Source.Common.Mathematics;

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
	bool SetupBones(ref Matrix4x4 boneToWOrldOut, int maxBones, int boneMask, double currentTime);
	void GetRenderBounds(out Vector3 mins, out Vector3 maxs);
	void GetRenderBoundsWorldspace(out Vector3 mins, out Vector3 maxs);
	ref ClientRenderHandle_t RenderHandle();
	IPVSNotify? GetPVSNotifyInterface();
}
