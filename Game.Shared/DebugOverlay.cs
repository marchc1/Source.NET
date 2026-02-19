using Source.Common.Engine;
using Source.Common.Mathematics;

using System.Numerics;

namespace Game.Shared;

static class DebugOverlay
{
	readonly static IVDebugOverlay debugoverlay = Singleton<IVDebugOverlay>();
	public const float Persist = 0.0f;
	const int MAX_OVERLAY_DIST_SQR = 90000000;

	static BasePlayer? GetLocalPlayer() {
#if CLIENT_DLL
	return BasePlayer.GetLocalPlayer();
#else
		return Util.GetListenServerHost();
#endif
	}

	static BasePlayer? GetDebugPlayer() {
#if CLIENT_DLL
	return GetLocalPlayer();
#else
		return Util.PlayerByIndex(-1 /*DebugPlayer*/);
#endif
	}

	public static void Box(in Vector3 origin, in Vector3 mins, in Vector3 maxs, int r, int g, int b, int a, float duration) => BoxAngles(origin, mins, maxs, QAngle.Zero, r, g, b, a, duration);

	public static void BoxDirection(in Vector3 origin, in Vector3 mins, in Vector3 maxs, in Vector3 orientation, int r, int g, int b, int a, float duration) {
		QAngle angle = QAngle.Zero;
		angle.X = Util.VecToYaw(orientation);
		BoxAngles(origin, mins, maxs, angle, r, g, b, a, duration);
	}

	public static void BoxAngles(in Vector3 origin, in Vector3 mins, in Vector3 maxs, QAngle angles, int r, int g, int b, int a, float duration) => debugoverlay.AddBoxOverlay(origin, mins, maxs, angles, r, g, b, a, duration);

	public static void SweptBox(in Vector3 start, in Vector3 end, in Vector3 mins, in Vector3 maxs, QAngle angles, int r, int g, int b, int a, float duration) {
		throw new NotImplementedException();
	}

	public static void EntityBounds(SharedBaseEntity entity, int r, int g, int b, int a, float duration) {
		throw new NotImplementedException();
	}

	public static void Line(in Vector3 origin, in Vector3 target, int r, int g, int b, bool noDepthTest, float duration) {
		BasePlayer? player = GetLocalPlayer();
		if (player == null)
			return;

		// Clip line that is far away
		if (((player.GetAbsOrigin() - origin).LengthSqr() > MAX_OVERLAY_DIST_SQR) &&
			((player.GetAbsOrigin() - target).LengthSqr() > MAX_OVERLAY_DIST_SQR))
			return;

		// Clip line that is behind the client
		player.EyeVectors(out AngularImpulse clientForward);

		Vector3 toOrigin = origin - player.GetAbsOrigin();
		Vector3 toTarget = target - player.GetAbsOrigin();
		float dotOrigin = MathLib.DotProduct(clientForward, toOrigin);
		float dotTarget = MathLib.DotProduct(clientForward, toTarget);

		if (dotOrigin < 0 && dotTarget < 0)
			return;

		debugoverlay.AddLineOverlay(origin, target, r, g, b, noDepthTest, duration);
	}

	public static void Triangle(in Vector3 p1, in Vector3 p2, in Vector3 p3, int r, int g, int b, int a, bool noDepthTest, float duration) {
		BasePlayer? player = GetLocalPlayer();
		if (player == null)
			return;

		// Clip triangles that are far away
		Vector3 to1 = p1 - player.GetAbsOrigin();
		Vector3 to2 = p2 - player.GetAbsOrigin();
		Vector3 to3 = p3 - player.GetAbsOrigin();

		if ((to1.LengthSqr() > MAX_OVERLAY_DIST_SQR) &&
			(to2.LengthSqr() > MAX_OVERLAY_DIST_SQR) &&
			(to3.LengthSqr() > MAX_OVERLAY_DIST_SQR)) {
			return;
		}

		// Clip triangles that are behind the client
		player.EyeVectors(out Vector3 clientForward, out _, out _);

		float dot1 = MathLib.DotProduct(clientForward, to1);
		float dot2 = MathLib.DotProduct(clientForward, to2);
		float dot3 = MathLib.DotProduct(clientForward, to3);

		if (dot1 < 0 && dot2 < 0 && dot3 < 0)
			return;

		debugoverlay.AddTriangleOverlay(p1, p2, p3, r, g, b, a, noDepthTest, duration);
	}

	public static void EntityText(int entityID, int text_offset, char text, float duration, int r, int g, int b, int a) {
		throw new NotImplementedException();
	}

	public static void EntityTextAtPosition(in Vector3 origin, int text_offset, char text, float duration, int r, int g, int b, int a) {
		throw new NotImplementedException();
	}

	public static void Grid(in Vector3 position) {
		throw new NotImplementedException();
	}

	public static void Text(in Vector3 origin, char text, bool viewCheck, float duration) {
		throw new NotImplementedException();
	}

	public static void ScreenText(float flXpos, float flYpos, char text, int r, int g, int b, int a, float duration) {
		throw new NotImplementedException();
	}

	public static void Cross3D(in Vector3 position, in Vector3 mins, in Vector3 maxs, int r, int g, int b, bool noDepthTest, float duration) {
		throw new NotImplementedException();
	}

	public static void Cross3D(in Vector3 position, float size, int r, int g, int b, bool noDepthTest, float duration) {
		throw new NotImplementedException();
	}

	public static void Cross3DOriented(in Vector3 position, QAngle angles, float size, int r, int g, int b, bool noDepthTest, float duration) {
		throw new NotImplementedException();
	}

	public static void Cross3DOriented(Matrix3x4 m, float size, int c, bool noDepthTest, float duration) {
		throw new NotImplementedException();
	}

	public static void DrawTickMarkedLine(in Vector3 startPos, in Vector3 endPos, float tickDist, int tickTextDist, int r, int g, int b, bool noDepthTest, float duration) {
		throw new NotImplementedException();
	}

	public static void DrawGroundCrossHairOverlay() {
		throw new NotImplementedException();
	}

	public static void HorzArrow(in Vector3 startPos, in Vector3 endPos, float width, int r, int g, int b, int a, bool noDepthTest, float duration) {
		Vector3 lineDir = (endPos - startPos);
		MathLib.VectorNormalize(ref lineDir);
		Vector3 upVec = new(0, 0, 1);

		float radius = (float)(width / 2.0);

		MathLib.CrossProduct(lineDir, upVec, out AngularImpulse sideDir);

		Vector3 p1 = startPos - sideDir * radius;
		Vector3 p2 = endPos - lineDir * width - sideDir * radius;
		Vector3 p3 = endPos - lineDir * width - sideDir * width;
		Vector3 p4 = endPos;
		Vector3 p5 = endPos - lineDir * width + sideDir * width;
		Vector3 p6 = endPos - lineDir * width + sideDir * radius;
		Vector3 p7 = startPos + sideDir * radius;

		// Outline the arrow
		Line(p1, p2, r, g, b, noDepthTest, duration);
		Line(p2, p3, r, g, b, noDepthTest, duration);
		Line(p3, p4, r, g, b, noDepthTest, duration);
		Line(p4, p5, r, g, b, noDepthTest, duration);
		Line(p5, p6, r, g, b, noDepthTest, duration);
		Line(p6, p7, r, g, b, noDepthTest, duration);

		if (a > 0) {
			// Fill us in with triangles
			Triangle(p5, p4, p3, r, g, b, a, noDepthTest, duration); // Tip
			Triangle(p1, p7, p6, r, g, b, a, noDepthTest, duration); // Shaft
			Triangle(p6, p2, p1, r, g, b, a, noDepthTest, duration);

			// And backfaces
			Triangle(p3, p4, p5, r, g, b, a, noDepthTest, duration); // Tip
			Triangle(p6, p7, p1, r, g, b, a, noDepthTest, duration); // Shaft
			Triangle(p1, p2, p6, r, g, b, a, noDepthTest, duration);
		}
	}

	public static void YawArrow(in Vector3 startPos, float yaw, float length, float width, int r, int g, int b, int a, bool noDepthTest, float duration) {
		throw new NotImplementedException();
	}

	public static void VertArrow(in Vector3 startPos, in Vector3 endPos, float width, int r, int g, int b, int a, bool noDepthTest, float duration) {
		throw new NotImplementedException();
	}

	public static void Axis(in Vector3 position, QAngle angles, float size, bool noDepthTest, float duration) {
		throw new NotImplementedException();
	}

	public static void Sphere(in Vector3 center, float radius, int r, int g, int b, bool noDepthTest, float duration) {
		throw new NotImplementedException();
	}

	public static void Circle(in Vector3 position, float radius, int r, int g, int b, int a, bool bNoDepthTest, float duration) {
		throw new NotImplementedException();
	}

	public static void Circle(in Vector3 position, QAngle angles, float radius, int r, int g, int b, int a, bool bNoDepthTest, float duration) {
		throw new NotImplementedException();
	}

	public static void Circle(in Vector3 position, in Vector3 xAxis, in Vector3 yAxis, float radius, int r, int g, int b, int a, bool bNoDepthTest, float duration) {
		throw new NotImplementedException();
	}

	public static void Sphere(in Vector3 position, QAngle angles, float radius, int r, int g, int b, int a, bool bNoDepthTest, float duration) {
		throw new NotImplementedException();
	}
}