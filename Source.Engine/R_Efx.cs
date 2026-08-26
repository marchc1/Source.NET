using Source.Common;
using Source.Common.Engine;
using Source.Common.MaterialSystem;
using Source.Common.Mathematics;

using System.Numerics;

using static Source.Constants;

namespace Source.Engine;

public class VEfx : IVEfx
{
	public int DrawDecalIndexFromName(ReadOnlySpan<char> name) => throw new NotImplementedException();

	public ReadOnlySpan<char> DrawDecalNameFromIndex(int index) => throw new NotImplementedException();

	public void DecalShoot(int textureIndex, int entity, Model? model, in Vector3 modelOrigin, in QAngle modelAngles, in Vector3 position, in Vector3 saxis, int flags) {
		Color white = new(255, 255, 255, 255);
		DecalColorShoot(textureIndex, entity, model, in modelOrigin, in modelAngles, in position, in saxis, flags, in white);
	}

	public void DecalColorShoot(int textureIndex, int entity, Model? model, in Vector3 modelOrigin, in QAngle modelAngles, in Vector3 position, in Vector3 saxis, int flags, in Color rgbaColor) => throw new NotImplementedException();

	public void PlayerDecalShoot(IMaterial material, object? userData, int entity, Model? model, in Vector3 modelOrigin, in QAngle modelAngles, in Vector3 position, in Vector3 saxis, int flags, in Color rgbaColor) => throw new NotImplementedException();

	public DLight AllocDlight(int key) => CL.AllocDlight(key);

	public int GetActiveDLights(Span<DLight> list) {
		int outCount = 0;
		if (CL.ActiveDlights) {
			for (int i = 0; i < MAX_DLIGHTS; i++) {
				if ((Render.DLightActive & (1 << i)) != 0)
					list[outCount++] = CL.DLights[i];
			}
		}
		return outCount;
	}

	public DLight AllocElight(int key) => CL.AllocElight(key);

	public DLight? GetElightByKey(int key) {
		if (CL.ActiveElights) {
			for (int i = 0; i < MAX_ELIGHTS; i++) {
				if (CL.ELights[i].Key == key) {
					if (CL.ELights[i].Die > cl.GetTime())
						return CL.ELights[i];
					else
						return null;
				}
			}
		}

		return null;
	}
}
