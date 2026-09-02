using Source.Common;
using Source.Common.Engine;
using Source.Common.Formats.BSP;
using Source.Common.Mathematics;

using System.Numerics;

using static Source.Common.CollisionUtils;
using static Source.Constants;
using static Source.Engine.ModelLoader;

namespace Source.Engine;

public static class GLRLight
{
	public const float DLIGHT_BEHIND_PLANE_DIST = -15;

	public static void AnimateLight() {
		INetworkStringTable? table = cl.LightStyleTable;

		if (table == null)
			return;

		int i = (int)(cl.GetTime() * 10);

		for (int j = 0; j < BSPFileCommon.MAX_LIGHTSTYLES; j++) {
			byte[]? lightstyle = table.GetStringUserData(j);
			int length = (lightstyle?.Length ?? 0) - 1;

			if (lightstyle == null || length <= 0) {
				MatSysInterface.LightStyleValue[j] = 256;
				MatSysInterface.LightStyleNumFrames[j] = 0;
				continue;
			}

			MatSysInterface.LightStyleNumFrames[j] = length;
			int k = i % length;
			k = lightstyle[k] - 'a';
			k *= 22;
			if (MatSysInterface.LightStyleValue[j] != k) {
				MatSysInterface.LightStyleValue[j] = k;
				MatSysInterface.LightStyleFrame[j] = Render.r_framecount;
			}
		}
	}

	static bool IsDLightAlreadyMarked(ref BSPMSurfaceLighting lighting, int bit) => lighting.DLightFrame == Render.r_framecount && (lighting.DLightBits & bit) != 0;

	static void MarkSurfaceDLight(ref BSPMSurface2 surfID, ref BSPMSurfaceLighting lighting, int bit) {
		lighting.DLightFrame = Render.r_framecount;
		lighting.DLightBits |= bit;
		MSurf_Flags(ref surfID) |= SurfDraw.HasDLight;
	}

	public static int TryLightMarkSurface(DLight light, ref BSPMSurfaceLighting lighting, ref BSPMSurface2 surfID, int bit) {
		float perpDistSq = MathLib.DotProduct(light.Origin, MSurf_Plane(ref surfID).Normal) - MSurf_Plane(ref surfID).Dist;
		if (perpDistSq < DLIGHT_BEHIND_PLANE_DIST)
			return 0;

		perpDistSq *= perpDistSq;

		float inPlaneRadiusSq = light.GetRadiusSquared() - perpDistSq;
		if (inPlaneRadiusSq <= 0)
			return 0;

		ref ModelTexInfo tex = ref MSurf_TexInfo(ref surfID);

		Vector2 mins = new(lighting.LightmapMins[0], lighting.LightmapMins[1]);
		Vector2 maxs = new(mins.X + lighting.LightmapExtents[0], mins.Y + lighting.LightmapExtents[1]);

		Vector2 circleCenter;
		circleCenter.X = MathLib.DotProduct(light.Origin, tex.LightmapVecsLuxelsPerWorldUnits[0].AsVector3D()) + tex.LightmapVecsLuxelsPerWorldUnits[0][3];
		circleCenter.Y = MathLib.DotProduct(light.Origin, tex.LightmapVecsLuxelsPerWorldUnits[1].AsVector3D()) + tex.LightmapVecsLuxelsPerWorldUnits[1][3];

		float inPlaneLuxelRadius = MathF.Sqrt(inPlaneRadiusSq * tex.LuxelsPerWorldUnit * tex.LuxelsPerWorldUnit);

		if (!IsCircleIntersectingRectangle(in mins, in maxs, in circleCenter, inPlaneLuxelRadius))
			return 0;

		MarkSurfaceDLight(ref surfID, ref lighting, bit);
		return 1;
	}

	public static int MarkLightsLeaf(DLight light, int bit, BSPMLeaf leaf) {
		WorldBrushData brushData = host_state.WorldBrush!;
		int countMarked = 0;

		for (int i = 0; i < leaf.DispCount; i++) {
			IDispInfo? dispInfo = DispInfo.MLeaf_Disaplcement(leaf, i);

			ref BSPMSurface2 parentSurfID = ref dispInfo!.GetParent();
			ref BSPMSurfaceLighting parentLighting = ref SurfaceLighting(ref parentSurfID, brushData);
			if (!IsDLightAlreadyMarked(ref parentLighting, bit)) {
				dispInfo.GetBoundingBox(out Vector3 bmin, out Vector3 bmax);
				if (IsBoxIntersectingSphere(in bmin, in bmax, in light.Origin, light.GetRadius())) {
					MarkSurfaceDLight(ref parentSurfID, ref parentLighting, bit);
					countMarked++;
				}
			}
		}

		for (int i = 0; i < leaf.NumMarkSurfaces; i++) {
			SurfaceHandle_t surfIndex = brushData.MarkSurfaces![leaf.FirstMarkSurface + i];
			ref BSPMSurface2 surfID = ref SurfaceHandleFromIndex(surfIndex, brushData);

			if ((MSurf_Flags(ref surfID) & SurfDraw.Node) != 0)
				continue;

			ref BSPMSurfaceLighting lighting = ref SurfaceLighting(ref surfID, brushData);
			if (IsDLightAlreadyMarked(ref lighting, bit))
				continue;

			float dist = MathLib.DotProduct(light.Origin, MSurf_Plane(ref surfID).Normal) - MSurf_Plane(ref surfID).Dist;

			if (dist > light.GetRadius() || dist < -light.GetRadius())
				continue;

			countMarked += TryLightMarkSurface(light, ref lighting, ref surfID, bit);
		}

		return countMarked;
	}

	public static int MarkLights(DLight light, int bit, BSPMNode? node) {
		if (node == null)
			return 0;

		if (node.Contents >= 0)
			return MarkLightsLeaf(light, bit, (BSPMLeaf)node);

		ref CollisionPlane splitplane = ref node.Plane;
		float dist = MathLib.DotProduct(light.Origin, splitplane.Normal) - splitplane.Dist;

		if (dist > light.GetRadius())
			return MarkLights(light, bit, node.Children[0]);

		if (dist < -light.GetRadius())
			return MarkLights(light, bit, node.Children[1]);

		WorldBrushData brushData = host_state.WorldBrush!;
		int countMarked = 0;
		for (int i = 0; i < node.NumSurfaces; i++) {
			ref BSPMSurface2 surfID = ref SurfaceHandleFromIndex(node.FirstSurface + i, brushData);

			ref BSPMSurfaceLighting lighting = ref SurfaceLighting(ref surfID, brushData);
			if (IsDLightAlreadyMarked(ref lighting, bit))
				continue;

			countMarked += TryLightMarkSurface(light, ref lighting, ref surfID, bit);
		}

		countMarked += MarkLights(light, bit, node.Children[0]);
		return countMarked + MarkLights(light, bit, node.Children[1]);
	}

	public static void MarkDLightsOnSurface(BSPMNode? node) {
		if (node == null || !CL.ActiveDlights)
			return;

		for (int i = 0; i < MAX_DLIGHTS; i++) {
			DLight l = CL.DLights[i];

			if (l.Die < cl.GetTime() || !l.IsRadiusGreaterThanZero())
				continue;

			// float rad = l.GetRadius();
			// MathLib.VectorMA(l.Origin, rad, l.Direction, out Vector3 tip);

			if ((l.Flags & DLightFlags.NoWorldIllumination) != 0) {
				// debugoverlay.AddTextOverlay(l.Origin, 0.0f, $"dlight {i} key={l.Key} r={l.GetRadius():F0} NOWORLD");
				// debugoverlay.AddBoxOverlay(l.Origin, new(-rad, -rad, -rad), new(rad, rad, rad), new QAngle(0, 0, 0), 255, 0, 0, 0, 0.0f);
				// debugoverlay.AddLineOverlay(l.Origin, tip, 255, 0, 0, true, 0.0f);
				continue;
			}

			// debugoverlay.AddTextOverlay(l.Origin, 0.0f, $"dlight {i} key={l.Key} r={l.GetRadius():F0} marked={MarkLights(l, 1 << i, node)}");
			// debugoverlay.AddBoxOverlay(l.Origin, new(-rad, -rad, -rad), new(rad, rad, rad), new QAngle(0, 0, 0), 0, 255, 0, 0, 0.0f);
			// debugoverlay.AddLineOverlay(l.Origin, tip, 0, 255, 0, true, 0.0f);

			MarkLights(l, 1 << i, node);
		}
	}

	public static void PushDlights() {
		MarkDLightsOnSurface(host_state.WorldBrush!.Nodes![0]);
		// MarkDLightsOnStaticProps();
	}
}
