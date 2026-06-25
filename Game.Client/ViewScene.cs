global using static Game.Client.ViewScene;

using Source;
using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.MaterialSystem;
using Source.Common.Mathematics;
using Source.Engine;

using System.Drawing.Drawing2D;

using System.Numerics;
namespace Game.Client;

[EngineComponent]
public static class ViewScene
{
	public static readonly ConVar r_updaterefracttexture = new("r_updaterefracttexture", "1", FCvar.Cheat);
	public static readonly ConVar r_depthoverlay = new("r_depthoverlay", "0", FCvar.Cheat, "Replaces opaque objects with their grayscaled depth values. r_showz_power scales the output.");

	public static void ViewTransform(in Vector3 worldSpace, out Vector3 viewSpace) {
		ref readonly Matrix4x4 viewMatrix = ref engine.WorldToViewMatrix();
		MathLib.Vector3DMultiplyPosition(in viewMatrix, in worldSpace, out viewSpace);
	}
	public static bool FrustumTransform(in Matrix4x4 worldToSurface, in Vector3 point, out Vector3 screen) {
		// UNDONE: Clean this up some, handle off-screen vertices
		float w;

		screen.X = worldToSurface[0][0] * point[0] + worldToSurface[0][1] * point[1] + worldToSurface[0][2] * point[2] + worldToSurface[0][3];
		screen.Y = worldToSurface[1][0] * point[0] + worldToSurface[1][1] * point[1] + worldToSurface[1][2] * point[2] + worldToSurface[1][3];
		//	z		 = worldToSurface[2][0] * point[0] + worldToSurface[2][1] * point[1] + worldToSurface[2][2] * point[2] + worldToSurface[2][3];
		w = worldToSurface[3][0] * point[0] + worldToSurface[3][1] * point[1] + worldToSurface[3][2] * point[2] + worldToSurface[3][3];

		// Just so we have something valid here
		screen.Z = 0.0f;

		bool behind;
		if (w < 0.001f) {
			behind = true;
			screen.X *= 100000;
			screen.Y *= 100000;
		}
		else {
			behind = false;
			float invw = 1.0f / w;
			screen.X *= invw;
			screen.Y *= invw;
		}

		return behind;
	}

	public static bool ScreenTransform(in Vector3 point, out Vector3 screen) {
		// UNDONE: Clean this up some, handle off-screen vertices
		return FrustumTransform(engine.WorldToScreenMatrix(), point, out screen);
	}

	public static bool HudTransform(in Vector3 point, out Vector3 screen) {
		if (/*UseVR()*/ false) {
			throw new NotImplementedException(); // todo vr	
												 //return FrustumTransform(g_ClientVirtualReality.GetHudProjectionFromWorld(), point, screen);
		}
		else {
			return FrustumTransform(engine.WorldToScreenMatrix(), point, out screen);
		}
	}

	public static void UpdateFullScreenDepthTexture() {
		// todo
	}
}
