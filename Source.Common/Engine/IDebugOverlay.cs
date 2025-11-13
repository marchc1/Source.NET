using Source.Common.Mathematics;

using System.Drawing;
using System.Numerics;

namespace Source.Common.Engine;

public interface IDebugOverlay
{
	void AddEntityTextOverlay(int ent_index, int line_offset, float duration, int r, int g, int b, int a, ReadOnlySpan<char> text);
	void AddBoxOverlay(in Vector3 origin, in Vector3 mins, in Vector3 max, in QAngle orientation, int r, int g, int b, int a, float duration);
	void AddTriangleOverlay(in Vector3 p1, in Vector3 p2, in Vector3 p3, int r, int g, int b, int a, bool noDepthTest, float duration);
	void AddLineOverlay(in Vector3 origin, in Vector3 dest, int r, int g, int b, bool noDepthTest, float duration);
	void AddTextOverlay(in Vector3 origin, float duration, ReadOnlySpan<char> text);
	void AddTextOverlay(in Vector3 origin, int line_offset, float duration, ReadOnlySpan<char> text);
	void AddScreenTextOverlay(float flXPos, float flYPos, float duration, int r, int g, int b, int a, ReadOnlySpan<char> text);
	void AddSweptBoxOverlay(in Vector3 start, in Vector3 end, in Vector3 mins, in Vector3 max, in QAngle angles, int r, int g, int b, int a, float flDuration);
	void AddGridOverlay(in Vector3 origin);
	int ScreenPosition(in Vector3 point, out Vector3 screen);
	int ScreenPosition(float xPos, float yPos, out Vector3 screen);

	OverlayText? GetFirst();
	OverlayText? GetNext(OverlayText? current);
	void ClearDeadOverlays();
	void ClearAllOverlays();

	void AddTextOverlayRGB(in Vector3 origin, int line_offset, float duration, float r, float g, float b, float alpha, ReadOnlySpan<char> text);
	void AddTextOverlayRGB(in Vector3 origin, int line_offset, float duration, int r, int g, int b, int a, ReadOnlySpan<char> text);

	void AddLineOverlayAlpha(in Vector3 origin, in Vector3 dest, int r, int g, int b, int a, bool noDepthTest, float duration);
	void AddBoxOverlay2(in Vector3 origin, in Vector3 mins, in Vector3 max, in QAngle orientation, in Color faceColor, in Color edgeColor, float duration);

	void AddTextOverlay(in Vector3 origin, int line_offset, float duration, int r, int g, int b, int a, ReadOnlySpan<char> text);
}
