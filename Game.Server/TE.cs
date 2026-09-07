global using static Game.Server.TempEnts;

using Game.Shared;

using Source.Common;
using Source.Common.Formats.Keyvalues;
using Source.Common.Mathematics;

using System.Numerics;
using System.Runtime.CompilerServices;

namespace Game.Server;

public static partial class TempEnts
{
	static readonly TempEntsSystem g_TESystem = new();
	public static ITempEntsSystem te = g_TESystem;
}

public class TempEntsSystem : ITempEntsSystem
{
	public bool SuppressTE<IRF>(scoped ref IRF filter) where IRF : IRecipientFilter {
		if (GetSuppressHost() != null) {
			RecipientFilter _filter = (RecipientFilter)(object)filter;

			if (!_filter.IgnorePredictionCull()) 
				_filter.RemoveRecipient((BasePlayer)GetSuppressHost()!);
			

			if (0 == _filter.GetRecipientCount()) {
				// Suppress it
				return true;
			}
		}

		// There's at least one recipient
		return false;
	}

	public override void ArmorRicochet<IRF>(scoped ref IRF filer, float delay, in Vector3 pos, in Vector3 dir) {
		throw new NotImplementedException();
	}

	public override void BeamEntPoint<IRF>(scoped ref IRF filer, float delay, int nStartEntity, in Vector3 start, int nEndEntity, in Vector3 end, int modelindex, int haloindex, int startframe, int framerate, float life, float width, float endWidth, int fadeLength, float amplitude, int r, int g, int b, int a, int speed) {
		throw new NotImplementedException();
	}

	public override void BeamEnts<IRF>(scoped ref IRF filer, float delay, int start, int end, int modelindex, int haloindex, int startframe, int framerate, float life, float width, float endWidth, int fadeLength, float amplitude, int r, int g, int b, int a, int speed) {
		throw new NotImplementedException();
	}

	public override void BeamFollow<IRF>(scoped ref IRecipientFilter filter, float delay, int iEntIndex, int modelIndex, int haloIndex, float life, float width, float endWidth, float fadeLength, float r, float g, float b, float a) {
		throw new NotImplementedException();
	}

	public override void BeamLaser<IRF>(scoped ref IRF filer, float delay, int start, int end, int modelindex, int haloindex, int startframe, int framerate, float life, float width, float endWidth, int fadeLength, float amplitude, int r, int g, int b, int a, int speed) {
		throw new NotImplementedException();
	}

	public override void BeamPoints<IRF>(scoped ref IRF filer, float delay, in Vector3 start, in Vector3 end, int modelindex, int haloindex, int startframe, int framerate, float life, float width, float endWidth, int fadeLength, float amplitude, int r, int g, int b, int a, int speed) {
		throw new NotImplementedException();
	}

	public override void BeamRing<IRF>(scoped ref IRF filer, float delay, int start, int end, int modelindex, int haloindex, int startframe, int framerate, float life, float width, int spread, float amplitude, int r, int g, int b, int a, int speed, int flags = 0) {
		throw new NotImplementedException();
	}

	public override void BeamRingPoint<IRF>(scoped ref IRF filer, float delay, in Vector3 center, float start_radius, float end_radius, int modelindex, int haloindex, int startframe, int framerate, float life, float width, int spread, float amplitude, int r, int g, int b, int a, int speed, int flags = 0) {
		throw new NotImplementedException();
	}

	public override void BeamSpline<IRF>(scoped ref IRF filer, float delay, int points, Span<Vector3> rgPoints) {
		throw new NotImplementedException();
	}

	public override void BloodSprite<IRF>(scoped ref IRF filer, float delay, in Vector3 org, in Vector3 dir, int r, int g, int b, int a, int size) {
		throw new NotImplementedException();
	}

	public override void BloodStream<IRF>(scoped ref IRF filer, float delay, in Vector3 org, in Vector3 dir, int r, int g, int b, int a, int amount) {
		throw new NotImplementedException();
	}

	public override void BreakModel<IRF>(scoped ref IRF filer, float delay, in Vector3 pos, in QAngle angle, in Vector3 size, in Vector3 vel, int modelindex, int randomization, int count, float time, int flags) {
		throw new NotImplementedException();
	}

	public override void BSPDecal<IRF>(scoped ref IRF filer, float delay, in Vector3 pos, int entity, int index) {
		throw new NotImplementedException();
	}

	public override void Bubbles<IRF>(scoped ref IRF filer, float delay, in Vector3 mins, in Vector3 maxs, float height, int modelindex, int count, float speed) {
		throw new NotImplementedException();
	}

	public override void BubbleTrail<IRF>(scoped ref IRF filer, float delay, in Vector3 mins, in Vector3 maxs, float height, int modelindex, int count, float speed) {
		throw new NotImplementedException();
	}

	public override void ClientProjectile<IRF>(scoped ref IRF filter, float delay, in Vector3 vecOrigin, in Vector3 vecVelocity, int modelindex, int lifetime, BaseEntity? owner) {
		throw new NotImplementedException();
	}

	public override void Decal<IRF>(scoped ref IRF filer, float delay, in Vector3 pos, in Vector3 start, int entity, int hitbox, int index) {
		throw new NotImplementedException();
	}

	public override void DispatchEffect<IRF>(scoped ref IRF filter, float delay, in Vector3 pos, ReadOnlySpan<char> name, EffectData data) {
		throw new NotImplementedException();
	}

	public override void Dust<IRF>(scoped ref IRF filer, float delay, in Vector3 pos, in Vector3 dir, float size, float speed) {
		throw new NotImplementedException();
	}

	public override void DynamicLight<IRF>(scoped ref IRF filer, float delay, in Vector3 org, int r, int g, int b, int exponent, float radius, float time, float decay) {
		throw new NotImplementedException();
	}

	public override void EnergySplash<IRF>(scoped ref IRF filer, float delay, in Vector3 pos, in Vector3 dir, bool bExplosive) {
		throw new NotImplementedException();
	}

	public override void Explosion<IRF>(scoped ref IRF filer, float delay, in Vector3 pos, int modelindex, float scale, int framerate, int flags, int radius, int magnitude, Vector3? normal = null, byte materialType = 67) {
		throw new NotImplementedException();
	}

	public override void Fizz<IRF>(scoped ref IRF filer, float delay, BaseEntity? ed, int modelindex, int density, int current) {
		throw new NotImplementedException();
	}

	public override void FootprintDecal<IRF>(scoped ref IRF filer, float delay, in Vector3 origin, in Vector3 right, int entity, int index, byte materialType) {
		throw new NotImplementedException();
	}

	public override void GaussExplosion<IRF>(scoped ref IRF filer, float delay, in Vector3 pos, in Vector3 dir, int type) {
		throw new NotImplementedException();
	}

	public override void GlowSprite<IRF>(scoped ref IRF filer, float delay, in Vector3 pos, int modelindex, float life, float size, int brightness) {
		throw new NotImplementedException();
	}

	public override void KillPlayerAttachments<IRF>(scoped ref IRF filer, float delay, int player) {
		throw new NotImplementedException();
	}

	public override void LargeFunnel<IRF>(scoped ref IRF filer, float delay, in Vector3 pos, int modelindex, int reversed) {
		throw new NotImplementedException();
	}

	public override void MetalSparks<IRF>(scoped ref IRF filer, float delay, in Vector3 pos, in Vector3 dir) {
		throw new NotImplementedException();
	}

	public override void MuzzleFlash<IRF>(scoped ref IRF filer, float delay, in Vector3 start, in QAngle angles, float scale, int type) {
		throw new NotImplementedException();
	}

	public override void PhysicsProp<IRF>(scoped ref IRF filter, float delay, int modelindex, int skin, in Vector3 pos, in QAngle angles, in Vector3 vel, int flags, int effects) {
		throw new NotImplementedException();
	}

	public override void PlayerDecal<IRF>(scoped ref IRF filer, float delay, in Vector3 pos, int player, int entity) {
		throw new NotImplementedException();
	}

	public override void ProjectDecal<IRF>(scoped ref IRF filter, float delay, in Vector3 pos, in QAngle angles, float distance, int index) {
		throw new NotImplementedException();
	}

	public override void ShatterSurface<IRF>(scoped ref IRF filer, float delay, in Vector3 pos, in QAngle angle, in Vector3 force, in Vector3 forcePos, float width, float height, float shardsize, ShatterSurface surfacetype, int front_r, int front_g, int front_b, int back_r, int back_g, int back_b) {
		throw new NotImplementedException();
	}

	public override void ShowLine<IRF>(scoped ref IRF filer, float delay, in Vector3 start, in Vector3 end) {
		throw new NotImplementedException();
	}

	public override void Smoke<IRF>(scoped ref IRF filer, float delay, in Vector3 pos, int modelindex, float scale, int framerate) {
		throw new NotImplementedException();
	}

	public override void Sparks<IRF>(scoped ref IRF filer, float delay, in Vector3 pos, int nMagnitude, int nTrailLength, in Vector3 pDir) {
		throw new NotImplementedException();
	}

	public override void Sprite<IRF>(scoped ref IRF filer, float delay, in Vector3 pos, int modelindex, float size, int brightness) {
		throw new NotImplementedException();
	}

	public override void SpriteSpray<IRF>(scoped ref IRF filer, float delay, in Vector3 pos, in Vector3 dir, int modelindex, int speed, float noise, int count) {
		throw new NotImplementedException();
	}

	public override void TriggerTempEntity(KeyValues pKeyValues) {
		throw new NotImplementedException();
	}

	public override void WorldDecal<IRF>(scoped ref IRF filer, float delay, in Vector3 pos, int index) {
		throw new NotImplementedException();
	}
}
