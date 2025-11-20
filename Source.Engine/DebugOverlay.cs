using Source.Common;
using Source.Common.Engine;
using Source.Common.Mathematics;

using System.Numerics;

namespace Source.Engine;

public enum OverlayType {
	Box,
	Sphere,
	Line,
	Triangle,
	SweptBox,
	Box2
}
public class DebugOverlay : IVDebugOverlay
{
	private InlineArray1024<char> Text;
	private nint argptr;

	public void AddBoxOverlay(in Vector3 origin, in Vector3 mins, in Vector3 max, in QAngle orientation, int r, int g, int b, int a, float duration) {
		throw new NotImplementedException();
	}

	public void AddBoxOverlay2(in Vector3 origin, in Vector3 mins, in Vector3 max, in QAngle orientation, in Color faceColor, in Color edgeColor, float duration) {
		throw new NotImplementedException();
	}

	public void AddEntityTextOverlay(int ent_index, int line_offset, float duration, int r, int g, int b, int a, ReadOnlySpan<char> text) {
		throw new NotImplementedException();
	}

	public void AddGridOverlay(in Vector3 origin) {
		throw new NotImplementedException();
	}

	public void AddLineOverlay(in Vector3 origin, in Vector3 dest, int r, int g, int b, bool noDepthTest, float duration) {
		throw new NotImplementedException();
	}

	public void AddLineOverlayAlpha(in Vector3 origin, in Vector3 dest, int r, int g, int b, int a, bool noDepthTest, float duration) {
		throw new NotImplementedException();
	}

	public void AddScreenTextOverlay(float flXPos, float flYPos, float duration, int r, int g, int b, int a, ReadOnlySpan<char> text) {
		throw new NotImplementedException();
	}

	public void AddSweptBoxOverlay(in Vector3 start, in Vector3 end, in Vector3 mins, in Vector3 max, in QAngle angles, int r, int g, int b, int a, float flDuration) {
		throw new NotImplementedException();
	}

	public void AddTextOverlay(in Vector3 origin, float duration, ReadOnlySpan<char> text) {
		throw new NotImplementedException();
	}

	public void AddTextOverlay(in Vector3 origin, int line_offset, float duration, ReadOnlySpan<char> text) {
		throw new NotImplementedException();
	}

	public void AddTextOverlay(in Vector3 origin, int line_offset, float duration, int r, int g, int b, int a, ReadOnlySpan<char> text) {
		throw new NotImplementedException();
	}

	public void AddTextOverlayRGB(in Vector3 origin, int line_offset, float duration, float r, float g, float b, float alpha, ReadOnlySpan<char> text) {
		throw new NotImplementedException();
	}

	public void AddTextOverlayRGB(in Vector3 origin, int line_offset, float duration, int r, int g, int b, int a, ReadOnlySpan<char> text) {
		throw new NotImplementedException();
	}

	public void AddTriangleOverlay(in Vector3 p1, in Vector3 p2, in Vector3 p3, int r, int g, int b, int a, bool noDepthTest, float duration) {
		throw new NotImplementedException();
	}

	public void ClearAllOverlays() {
		throw new NotImplementedException();
	}

	public void ClearDeadOverlays() {
		throw new NotImplementedException();
	}

	public OverlayText? GetFirst() {
		throw new NotImplementedException();
	}

	public OverlayText? GetNext(OverlayText? current) {
		throw new NotImplementedException();
	}

	public int ScreenPosition(in Vector3 point, out Vector3 screen) {
		throw new NotImplementedException();
	}

	public int ScreenPosition(float xPos, float yPos, out Vector3 screen) {
		throw new NotImplementedException();
	}
}
