global using static Game.Client.TempEntsSystemGlobals;

using Game.Shared;

using Source.Common;
using Source.Common.Formats.Keyvalues;
using Source.Common.Mathematics;

using System.Numerics;

namespace Game.Client;

public class TempEntsSystem : IPredictionSystem
{
	private bool SuppressTE(IRecipientFilter filter) {
		if (!CanPredict())
			return true;

		if (filter.GetRecipientCount() == 0)
			return true;

		return false;
	}

	public void ArmorRicochet(IRecipientFilter filter, float delay, in Vector3 pos, in Vector3 dir) {
		if (!SuppressTE(filter))
			TE_ArmorRicochet(filter, delay, in pos, in dir);
	}

	public void BeamEntPoint(IRecipientFilter filter, float delay, int startEntity, in Vector3 start, int endEntity, in Vector3 end, int modelIndex, int haloIndex, int startFrame, int frameRate, float life, float width, float endWidth, int fadeLength, float amplitude, int r, int g, int b, int a, int speed) {
		if (!SuppressTE(filter))
			TE_BeamEntPoint(filter, delay, startEntity, in start, endEntity, in end, modelIndex, haloIndex, startFrame, frameRate, life, width, endWidth, fadeLength, amplitude, r, g, b, a, speed);
	}

	public void BeamEnts(IRecipientFilter filter, float delay, int start, int end, int modelIndex, int haloIndex, int startFrame, int frameRate, float life, float width, float endWidth, int fadeLength, float amplitude, int r, int g, int b, int a, int speed) {
		if (!SuppressTE(filter))
			TE_BeamEnts(filter, delay, start, end, modelIndex, haloIndex, startFrame, frameRate, life, width, endWidth, fadeLength, amplitude, r, g, b, a, speed);
	}

	public void BeamFollow(IRecipientFilter filter, float delay, int entIndex, int modelIndex, int haloIndex, float life, float width, float endWidth, float fadeLength, float r, float g, float b, float a) {
		if (!SuppressTE(filter))
			TE_BeamFollow(filter, delay, entIndex, modelIndex, haloIndex, life, width, endWidth, fadeLength, r, g, b, a);
	}

	public void BeamPoints(IRecipientFilter filter, float delay, in Vector3 start, in Vector3 end, int modelIndex, int haloIndex, int startFrame, int frameRate, float life, float width, float endWidth, int fadeLength, float amplitude, int r, int g, int b, int a, int speed) {
		if (!SuppressTE(filter))
			TE_BeamPoints(filter, delay, in start, in end, modelIndex, haloIndex, startFrame, frameRate, life, width, endWidth, fadeLength, amplitude, r, g, b, a, speed);
	}

	public void BeamLaser(IRecipientFilter filter, float delay, int start, int end, int modelIndex, int haloIndex, int startFrame, int frameRate, float life, float width, float endWidth, int fadeLength, float amplitude, int r, int g, int b, int a, int speed) {
		if (!SuppressTE(filter))
			TE_BeamLaser(filter, delay, start, end, modelIndex, haloIndex, startFrame, frameRate, life, width, endWidth, fadeLength, amplitude, r, g, b, a, speed);
	}

	public void BeamRing(IRecipientFilter filter, float delay, int start, int end, int modelIndex, int haloIndex, int startFrame, int frameRate, float life, float width, int spread, float amplitude, int r, int g, int b, int a, int speed, int flags = 0) {
		if (!SuppressTE(filter))
			TE_BeamRing(filter, delay, start, end, modelIndex, haloIndex, startFrame, frameRate, life, width, spread, amplitude, r, g, b, a, speed, flags);
	}

	public void BeamRingPoint(IRecipientFilter filter, float delay, in Vector3 center, float startRadius, float endRadius, int modelIndex, int haloIndex, int startFrame, int frameRate, float life, float width, int spread, float amplitude, int r, int g, int b, int a, int speed, int flags = 0) {
		if (!SuppressTE(filter))
			TE_BeamRingPoint(filter, delay, in center, startRadius, endRadius, modelIndex, haloIndex, startFrame, frameRate, life, width, spread, amplitude, r, g, b, a, speed, flags);
	}

	public void BeamSpline(IRecipientFilter filter, float delay, int points, Span<Vector3> rgPoints) {
		if (!SuppressTE(filter))
			TE_BeamSpline(filter, delay, points, rgPoints);
	}

	public void BloodStream(IRecipientFilter filter, float delay, in Vector3 org, in Vector3 dir, int r, int g, int b, int a, int amount) {
		if (!SuppressTE(filter))
			TE_BloodStream(filter, delay, in org, in dir, r, g, b, a, amount);
	}

	public void BloodSprite(IRecipientFilter filter, float delay, in Vector3 org, in Vector3 dir, int r, int g, int b, int a, int size) {
		if (!SuppressTE(filter))
			TE_BloodSprite(filter, delay, in org, in dir, r, g, b, a, size);
	}

	public void BreakModel(IRecipientFilter filter, float delay, in Vector3 pos, in QAngle angles, in Vector3 size, in Vector3 vel, int modelIndex, int randomization, int count, float time, int flags) {
		if (!SuppressTE(filter))
			TE_BreakModel(filter, delay, in pos, in angles, in size, in vel, modelIndex, randomization, count, time, flags);
	}

	public void BSPDecal(IRecipientFilter filter, float delay, in Vector3 pos, int entity, int index) {
		if (!SuppressTE(filter))
			TE_BSPDecal(filter, delay, in pos, entity, index);
	}

	public void ProjectDecal(IRecipientFilter filter, float delay, in Vector3 pos, in QAngle angles, float distance, int index) {
		if (!SuppressTE(filter))
			TE_ProjectDecal(filter, delay, in pos, in angles, distance, index);
	}

	public void Bubbles(IRecipientFilter filter, float delay, in Vector3 mins, in Vector3 maxs, float height, int modelIndex, int count, float speed) {
		if (!SuppressTE(filter))
			TE_Bubbles(filter, delay, in mins, in maxs, height, modelIndex, count, speed);
	}

	public void BubbleTrail(IRecipientFilter filter, float delay, in Vector3 mins, in Vector3 maxs, float waterZ, int modelIndex, int count, float speed) {
		if (!SuppressTE(filter))
			TE_BubbleTrail(filter, delay, in mins, in maxs, waterZ, modelIndex, count, speed);
	}

	public void Decal(IRecipientFilter filter, float delay, in Vector3 pos, in Vector3 start, int entity, int hitbox, int index) {
		if (!SuppressTE(filter))
			TE_Decal(filter, delay, in pos, in start, entity, hitbox, index);
	}

	public void DynamicLight(IRecipientFilter filter, float delay, in Vector3 org, int r, int g, int b, int exponent, float radius, float time, float decay) {
		if (!SuppressTE(filter))
			TE_DynamicLight(filter, delay, in org, r, g, b, exponent, radius, time, decay);
	}

	public void Explosion(IRecipientFilter filter, float delay, in Vector3 pos, int modelIndex, float scale, int frameRate, int flags, int radius, int magnitude, in Vector3 normal, byte materialType = (byte)'C') {
		if (!SuppressTE(filter))
			TE_Explosion(filter, delay, in pos, modelIndex, scale, frameRate, flags, radius, magnitude, in normal, materialType);
	}

	public void ShatterSurface(IRecipientFilter filter, float delay, in Vector3 pos, in QAngle angle, in Vector3 force, in Vector3 forcePos, float width, float height, float shardSize, int surfaceType, int frontR, int frontG, int frontB, int backR, int backG, int backB) {
		if (!SuppressTE(filter))
			TE_ShatterSurface(filter, delay, in pos, in angle, in force, in forcePos, width, height, shardSize, surfaceType, frontR, frontG, frontB, backR, backG, backB);
	}

	public void GlowSprite(IRecipientFilter filter, float delay, in Vector3 pos, int modelIndex, float life, float size, int brightness) {
		if (!SuppressTE(filter))
			TE_GlowSprite(filter, delay, in pos, modelIndex, life, size, brightness);
	}

	public void FootprintDecal(IRecipientFilter filter, float delay, in Vector3 origin, in Vector3 right, int entity, int index, byte materialType) {
		if (!SuppressTE(filter))
			TE_FootprintDecal(filter, delay, in origin, in right, entity, index, materialType);
	}

	public void Fizz(IRecipientFilter filter, float delay, C_BaseEntity? ed, int modelIndex, int density, int current) {
		if (!SuppressTE(filter))
			TE_Fizz(filter, delay, ed, modelIndex, density, current);
	}

	public void KillPlayerAttachments(IRecipientFilter filter, float delay, int player) {
		if (!SuppressTE(filter))
			TE_KillPlayerAttachments(filter, delay, player);
	}

	public void LargeFunnel(IRecipientFilter filter, float delay, in Vector3 pos, int modelIndex, int reversed) {
		if (!SuppressTE(filter))
			TE_LargeFunnel(filter, delay, in pos, modelIndex, reversed);
	}

	public void MetalSparks(IRecipientFilter filter, float delay, in Vector3 pos, in Vector3 dir) {
		if (!SuppressTE(filter))
			TE_MetalSparks(filter, delay, in pos, in dir);
	}

	public void EnergySplash(IRecipientFilter filter, float delay, in Vector3 pos, in Vector3 dir, bool explosive) {
		if (!SuppressTE(filter))
			TE_EnergySplash(filter, delay, in pos, in dir, explosive);
	}

	public void PlayerDecal(IRecipientFilter filter, float delay, in Vector3 pos, int player, int entity) {
		if (!SuppressTE(filter))
			TE_PlayerDecal(filter, delay, in pos, player, entity);
	}

	public void ShowLine(IRecipientFilter filter, float delay, in Vector3 start, in Vector3 end) {
		if (!SuppressTE(filter))
			TE_ShowLine(filter, delay, in start, in end);
	}

	public void Smoke(IRecipientFilter filter, float delay, in Vector3 pos, int modelIndex, float scale, int frameRate) {
		if (!SuppressTE(filter))
			TE_Smoke(filter, delay, in pos, modelIndex, scale, frameRate);
	}

	public void Sparks(IRecipientFilter filter, float delay, in Vector3 pos, int magnitude, int trailLength, in Vector3 dir) {
		if (!SuppressTE(filter))
			TE_Sparks(filter, delay, in pos, magnitude, trailLength, in dir);
	}

	public void Sprite(IRecipientFilter filter, float delay, in Vector3 pos, int modelIndex, float size, int brightness) {
		if (!SuppressTE(filter))
			TE_Sprite(filter, delay, in pos, modelIndex, size, brightness);
	}

	public void SpriteSpray(IRecipientFilter filter, float delay, in Vector3 pos, in Vector3 dir, int modelIndex, int speed, float noise, int count) {
		if (!SuppressTE(filter))
			TE_SpriteSpray(filter, delay, in pos, in dir, modelIndex, speed, noise, count);
	}

	public void WorldDecal(IRecipientFilter filter, float delay, in Vector3 pos, int index) {
		if (!SuppressTE(filter))
			TE_WorldDecal(filter, delay, in pos, index);
	}

	public void MuzzleFlash(IRecipientFilter filter, float delay, in Vector3 start, in QAngle angles, float scale, int type) {
		if (!SuppressTE(filter))
			TE_MuzzleFlash(filter, delay, in start, in angles, scale, type);
	}

	public void Dust(IRecipientFilter filter, float delay, in Vector3 pos, in Vector3 dir, float size, float speed) {
		if (!SuppressTE(filter))
			TE_Dust(filter, delay, in pos, in dir, size, speed);
	}

	public void GaussExplosion(IRecipientFilter filter, float delay, in Vector3 pos, in Vector3 dir, int type) {
		if (!SuppressTE(filter))
			TE_GaussExplosion(filter, delay, in pos, in dir, type);
	}

	public void DispatchEffect(IRecipientFilter filter, float delay, in Vector3 pos, ReadOnlySpan<char> name, EffectData data) {
		if (!SuppressTE(filter))
			TE_DispatchEffect(filter, delay, in pos, name, data);
	}

	public void PhysicsProp(IRecipientFilter filter, float delay, int modelIndex, int skin, in Vector3 pos, in QAngle angles, in Vector3 vel, int flags, int effects) {
		if (!SuppressTE(filter))
			TE_PhysicsProp(filter, delay, modelIndex, skin, in pos, in angles, in vel, flags != 0, effects);
	}

	public void ClientProjectile(IRecipientFilter filter, float delay, in Vector3 origin, in Vector3 velocity, int modelIndex, int lifetime, BaseEntity? owner) {
		if (!SuppressTE(filter))
			TE_ClientProjectile(filter, delay, in origin, in velocity, modelIndex, lifetime, owner);
	}

	public void TriggerTempEntity(KeyValues keyValues) => throw new NotImplementedException();
}

public static class TempEntsSystemGlobals
{
	public static readonly TempEntsSystem te = new();
}
