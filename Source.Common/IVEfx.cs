using Source.Common.Engine;
using Source.Common.MaterialSystem;
using Source.Common.Mathematics;

using System.Numerics;

namespace Source.Common;

public interface IVEfx
{
	int DrawDecalIndexFromName(ReadOnlySpan<char> name);
	void DecalShoot(int textureIndex, int entity, Model? model, in Vector3 modelOrigin, in QAngle modelAngles, in Vector3 position, Vector3? saxis, int flags);
	void DecalColorShoot(int textureIndex, int entity, Model? model, in Vector3 modelOrigin, in QAngle modelAngles, in Vector3 position, Vector3? saxis, int flags, in Color rgbaColor);
	void PlayerDecalShoot(IMaterial material, object? userData, int entity, Model? model, in Vector3 modelOrigin, in QAngle modelAngles, in Vector3 position, Vector3? saxis, int flags, in Color rgbaColor);
	DLight AllocDlight(int key);
	DLight AllocElight(int key);
	int GetActiveDLights(Span<DLight> list);
	ReadOnlySpan<char> DrawDecalNameFromIndex(int index);
	DLight? GetElightByKey(int key);
}
