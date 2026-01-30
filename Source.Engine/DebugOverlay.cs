using Source.Common;
using Source.Common.Commands;
using Source.Common.Engine;
using Source.Common.Mathematics;

using System.Numerics;

namespace Source.Engine;

public enum OverlayType
{
	Box,
	Sphere,
	Line,
	Triangle,
	SweptBox,
	Box2
}

public class OverlayBase : IPoolableObject
{
	public static T New<T>() where T : OverlayBase, new() {
		return ObjectPool<T>.Shared.Alloc();
	}

	public static void Free<T>(T obj) where T : OverlayBase, new() {
		ObjectPool<T>.Shared.Free(obj);
	}

	public OverlayBase() { Reset(); }
	public OverlayType Type;
	public long CreationTick;
	public long ServerCount;
	public TimeUnit_t EndTime;
	public OverlayBase? NextOverlay;

	public void Init() { }
	public virtual void Reset() {
		Type = OverlayType.Box;
		ServerCount = -1;
		CreationTick = -1;
		EndTime = 0;
		NextOverlay = null;
	}

	public bool IsDead() {
		if (ServerCount != cl.ServerCount)
			return true;

		if (CreationTick != -1) {
			if (DebugOverlay.GetOverlayTick() > CreationTick)
				return true;

			return false;
		}

		if (EndTime == IVDebugOverlay.NDEBUG_PERSIST_TILL_NEXT_SERVER)
			return false;

		return (cl.GetTime() >= EndTime);
	}

	public void SetEndTime(float duration) {
		ServerCount = cl.ServerCount;

		if (duration <= 0.0f) {
			CreationTick = DebugOverlay.GetOverlayTick();
			return;
		}

		if (duration == IVDebugOverlay.NDEBUG_PERSIST_TILL_NEXT_SERVER)
			EndTime = IVDebugOverlay.NDEBUG_PERSIST_TILL_NEXT_SERVER;
		else
			EndTime = cl.GetTime() + duration;
	}
}

public class OverlayBox : OverlayBase
{
	public override void Reset() {
		base.Reset();
		Type = OverlayType.Box;
	}
	public Vector3 Origin;
	public Vector3 Mins;
	public Vector3 Maxs;
	public Vector3 Angles;
	public int R;
	public int G;
	public int B;
	public int A;
}

public class DebugOverlay : IVDebugOverlay
{
	private InlineArray1024<char> Text;
	private nint argptr;

	public static long GetOverlayTick() => sv.IsActive() ? sv.TickCount : cl.GetClientTickCount();

	static readonly object s_OverlayMutex = new();
	static OverlayBase? s_pOverlays;

	public void AddBoxOverlay(in Vector3 origin, in Vector3 mins, in Vector3 maxs, in QAngle angles, int r, int g, int b, int a, float duration) {
		if (cl.IsPaused())
			return;

		lock (s_OverlayMutex) {
			OverlayBox new_overlay = OverlayBase.New<OverlayBox>();

			new_overlay.Origin = origin;

			new_overlay.Mins[0] = mins[0];
			new_overlay.Mins[1] = mins[1];
			new_overlay.Mins[2] = mins[2];

			new_overlay.Maxs[0] = maxs[0];
			new_overlay.Maxs[1] = maxs[1];
			new_overlay.Maxs[2] = maxs[2];

			new_overlay.Angles = angles;

			new_overlay.R = r;
			new_overlay.G = g;
			new_overlay.B = b;
			new_overlay.A = a;

			new_overlay.SetEndTime(duration);

			new_overlay.NextOverlay = s_pOverlays;
			s_pOverlays = new_overlay;
		}
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

	public static void ClearAllOverlays() {
		lock (s_OverlayMutex) {
			while (s_pOverlays != null) {
				OverlayBase pOldOverlay = s_pOverlays;
				s_pOverlays = s_pOverlays.NextOverlay;
				DestroyOverlay(pOldOverlay);
			}
			// todo: overlay text
		}

		s_bDrawGrid = false;
	}

	void IVDebugOverlay.ClearAllOverlays() => DebugOverlay.ClearAllOverlays();

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

	static readonly ConVar enable_debug_overlays = new("enable_debug_overlays", "1", FCvar.GameDLL | FCvar.Cheat, "Enable rendering of debug overlays");

	static long previous_servercount = 0;
	static bool s_bDrawGrid = false;
	public static void Draw3DOverlays() {
		lock (s_OverlayMutex) {
			if (previous_servercount != cl.ServerCount) {
				ClearAllOverlays();
				previous_servercount = cl.ServerCount;
			}

			DrawAllOverlays();

			// if (s_bDrawGrid) 
			// DrawGridOverlay();
		}
	}
	public static void DrawAllOverlays() {
		if (!enable_debug_overlays.GetBool())
			return;

		lock (s_OverlayMutex) {
			OverlayBase? currOverlay = s_pOverlays;
			OverlayBase? prevOverlay = null;
			OverlayBase? nextOverlay;

			while (currOverlay != null) {
				if (currOverlay.IsDead()) {
					if (prevOverlay != null)
						prevOverlay.NextOverlay = currOverlay.NextOverlay;
					else
						s_pOverlays = currOverlay.NextOverlay;


					nextOverlay = currOverlay.NextOverlay;
					DestroyOverlay(currOverlay);
					currOverlay = nextOverlay;
				}
				else {
					DrawOverlay(currOverlay);
					prevOverlay = currOverlay;
					currOverlay = currOverlay.NextOverlay;
				}
			}
		}
	}

	private static void DrawOverlay(OverlayBase overlay) {
		switch (overlay.Type) {
			case OverlayType.Box:
				OverlayBox box = (OverlayBox)overlay;

				if (box.A > 0)
					renderUtils.RenderBox(box.Origin, box.Angles, box.Mins, box.Maxs, new Color(box.R, box.G, box.B, box.A), false);

				renderUtils.RenderWireframeBox(box.Origin, box.Angles, box.Mins, box.Maxs, new Color(box.R, box.G, box.B, 255), true);
				break;
		}
	}

	private static void DestroyOverlay(OverlayBase overlay) {
		switch (overlay.Type) {
			case OverlayType.Line:
			case OverlayType.Box:
				OverlayBox box = (OverlayBox)overlay;
				OverlayBase.Free(box);
				break;
			case OverlayType.Box2:
			case OverlayType.Sphere:
			case OverlayType.SweptBox:
			case OverlayType.Triangle:
				break;
		}
	}
}
