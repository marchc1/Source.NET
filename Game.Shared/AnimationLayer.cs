// Made shared to avoid code duplication which was annoying me in the base animating overlay classes.

#if CLIENT_DLL
using Game.Client;
#endif

using Source.Common;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

#if CLIENT_DLL || GAME_DLL
using FIELD = Source.FIELD<Game.Shared.AnimationLayerRef>;
#endif
namespace Game.Shared;

#if CLIENT_DLL || GAME_DLL

[Flags]
public enum AnimLayerFlags : int
{
	Active = 0x0001,
	AutoKill = 0x0002,
	KillMe = 0x0004,
	DontRestore = 0x0008,
	CheckAccess = 0x0010,
	Dying = 0x0020,
}

public record struct AnimationLayer
{
	public AnimLayerFlags Flags;
	public bool SequenceFinished;
	public bool Looping;

	public int Sequence;
	public TimeUnit_t Cycle;
	public float PrevCycle;
	public float Weight;
	public int Order;

	public TimeUnit_t PlaybackRate;
	public TimeUnit_t LayerAnimtime;
	public TimeUnit_t LayerFadeOuttime;
	public double BlendIn;
	public double BlendOut;
	public bool ClientBlend;

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool IsActive() => ((Flags & AnimLayerFlags.Active) != 0);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool IsAutokill() => ((Flags & AnimLayerFlags.AutoKill) != 0);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool IsKillMe() => ((Flags & AnimLayerFlags.KillMe) != 0);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool IsAutoramp() => (BlendIn != 0.0f || BlendOut != 0.0f);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void KillMe() => Flags |= AnimLayerFlags.KillMe;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void Dying() => Flags |= AnimLayerFlags.Dying;
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool IsDying() => ((Flags & AnimLayerFlags.Dying) != 0);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void Dead() => Flags &= ~AnimLayerFlags.Dying;
	public void SetOrder(int order) => Order = order;
#if CLIENT_DLL
	public static AnimationLayer LoopingLerp(TimeUnit_t percent, in AnimationLayer from, in AnimationLayer to) {
		AnimationLayer output = default;

		output.Sequence = to.Sequence;
		output.Cycle = LerpFunctions.LoopingLerp(percent, (float)from.Cycle, (float)to.Cycle);
		output.PrevCycle = to.PrevCycle;
		output.Weight = LerpFunctions.Lerp(percent, from.Weight, to.Weight);
		output.Order = to.Order;

		output.LayerAnimtime = to.LayerAnimtime;
		output.LayerFadeOuttime = to.LayerFadeOuttime;
		return output;
	}
	public static AnimationLayer Lerp(TimeUnit_t percent, in AnimationLayer from, in AnimationLayer to) {
		AnimationLayer output = default;

		output.Sequence = to.Sequence;
		output.Cycle = LerpFunctions.Lerp(percent, from.Cycle, to.Cycle);
		output.PrevCycle = to.PrevCycle;
		output.Weight = LerpFunctions.Lerp(percent, from.Weight, to.Weight);
		output.Order = to.Order;

		output.LayerAnimtime = to.LayerAnimtime;
		output.LayerFadeOuttime = to.LayerFadeOuttime;
		return output;
	}
	public static AnimationLayer LoopingLerp_Hermite(TimeUnit_t percent, in AnimationLayer prev, in AnimationLayer from, in AnimationLayer to) {
		AnimationLayer output = default;

		output.Sequence = to.Sequence;
		output.Cycle = LerpFunctions.LoopingLerp_Hermite(percent, (float)prev.Cycle, (float)from.Cycle, (float)to.Cycle);
		output.PrevCycle = to.PrevCycle;
		output.Weight = LerpFunctions.Lerp(percent, from.Weight, to.Weight);
		output.Order = to.Order;

		output.LayerAnimtime = to.LayerAnimtime;
		output.LayerFadeOuttime = to.LayerFadeOuttime;
		return output;
	}
	public static AnimationLayer Lerp_Hermite(TimeUnit_t percent, in AnimationLayer prev, in AnimationLayer from, in AnimationLayer to) {
		AnimationLayer output = default;

		output.Sequence = to.Sequence;
		output.Cycle = LerpFunctions.Lerp_Hermite(percent, prev.Cycle, from.Cycle, to.Cycle);
		output.PrevCycle = to.PrevCycle;
		output.Weight = LerpFunctions.Lerp(percent, from.Weight, to.Weight);
		output.Order = to.Order;

		output.LayerAnimtime = to.LayerAnimtime;
		output.LayerFadeOuttime = to.LayerFadeOuttime;
		return output;
	}
#endif
	public double GetFadeout(double curTime) {
		double s;

		if (LayerFadeOuttime <= 0.0f) {
			s = 0;
		}
		else {
			// blend in over 0.2 seconds
			s = 1.0 - (curTime - LayerAnimtime) / LayerFadeOuttime;
			if (s > 0 && s <= 1.0f) {
				// do a nice spline curve
				s = 3 * s * s - 2 * s * s * s;
			}
			else if (s > 1.0f) {
				// Shouldn't happen, but maybe curtime is behind animtime?
				s = 1.0;
			}
		}

		return s;
	}

	public void Reset() {
		Sequence = 0;
		PrevCycle = 0;
		Weight = 0;
		PlaybackRate = 0;
		Cycle = 0;
		LayerAnimtime = 0;
		LayerFadeOuttime = 0;
		BlendIn = 0;
		BlendOut = 0;
		ClientBlend = false;
	}

	public void BlendWeight() {
		if (!ClientBlend)
			return;

		Weight = 1;

		// blend in?
		if (BlendIn != 0.0f)
			if (Cycle < BlendIn)
				Weight = (float)(Cycle / BlendIn);

		// blend out?
		if (BlendOut != 0.0f)
			if (Cycle > 1.0f - BlendOut)
				Weight = (float)((1.0f - (float)(Cycle)) / BlendOut);

		Weight = 3.0f * (float)(Weight) * (float)(Weight) * (3.0f - 2.0f * (float)(Weight));
		if (Sequence == 0)
			Weight = 0;
	}
}

/// <summary>
/// Because of FieldILAccess, these must be class instances. So this is basically just a wrapper around the AnimationLayer struct.
/// </summary>
public class AnimationLayerRef
{
	public AnimationLayer Struct;
	public static DynamicAccessor Accessor = Source.FIELD<AnimationLayerRef>.OF(nameof(Struct));

	public ref AnimLayerFlags Flags { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref Struct.Flags; }
	public ref bool SequenceFinished { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref Struct.SequenceFinished; }
	public ref bool Looping { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref Struct.Looping; }
	public ref int Sequence { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref Struct.Sequence; }
	public ref TimeUnit_t Cycle { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref Struct.Cycle; }
	public ref float PrevCycle { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref Struct.PrevCycle; }
	public ref float Weight { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref Struct.Weight; }
	public ref int Order { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref Struct.Order; }
	public ref TimeUnit_t PlaybackRate { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref Struct.PlaybackRate; }
	public ref TimeUnit_t LayerAnimtime { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref Struct.LayerAnimtime; }
	public ref TimeUnit_t LayerFadeOuttime { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref Struct.LayerFadeOuttime; }
	public ref double BlendIn { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref Struct.BlendIn; }
	public ref double BlendOut { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref Struct.BlendOut; }
	public ref bool ClientBlend { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => ref Struct.ClientBlend; }

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool IsActive() => Struct.IsActive();
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool IsAutokill() => Struct.IsAutokill();
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool IsKillMe() => Struct.IsKillMe();
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool IsAutoramp() => Struct.IsAutoramp();
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void KillMe() => Struct.KillMe();
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void Dying() => Struct.Dying();
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public bool IsDying() => Struct.IsDying();
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void Dead() => Struct.Dead();

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public double GetFadeout(double curtime) => Struct.GetFadeout(curtime);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void Reset() => Struct.Reset();
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void BlendWeight() => Struct.BlendWeight();
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetOrder(int order) => Struct.SetOrder(order);

	public const int ORDER_BITS = 4;
	public const int WEIGHT_BITS = 8;

	const string PREFIX_PROPS = $"{nameof(Struct)}.";
#if CLIENT_DLL
	public static readonly RecvTable DT_AnimationLayer = new([
		RecvPropInt(FIELD.OF(PREFIX_PROPS + nameof(AnimationLayer.Sequence))),
		RecvPropFloat(FIELD.OF(PREFIX_PROPS + nameof(AnimationLayer.Cycle))),
		RecvPropFloat(FIELD.OF(PREFIX_PROPS + nameof(AnimationLayer.PrevCycle))),
		RecvPropFloat(FIELD.OF(PREFIX_PROPS + nameof(AnimationLayer.Weight))),
		RecvPropInt(FIELD.OF(PREFIX_PROPS + nameof(AnimationLayer.Order))),
	]); public static readonly ClientClass ClientClass = new ClientClass("AnimationLayer", null, null, DT_AnimationLayer);
#else
	public static readonly SendTable DT_AnimationLayer = new([
		SendPropInt(FIELD.OF(PREFIX_PROPS + nameof(AnimationLayer.Sequence)), ANIMATION_SEQUENCE_BITS, PropFlags.Unsigned),
		SendPropFloat(FIELD.OF(PREFIX_PROPS + nameof(AnimationLayer.Cycle)), ANIMATION_CYCLE_BITS, PropFlags.RoundDown, 0.0f, 1.0f),
		SendPropFloat(FIELD.OF(PREFIX_PROPS + nameof(AnimationLayer.PrevCycle)), ANIMATION_CYCLE_BITS, PropFlags.RoundDown, 0.0f, 1.0f),
		SendPropFloat(FIELD.OF(PREFIX_PROPS + nameof(AnimationLayer.Weight)), WEIGHT_BITS, 0, 0.0f, 1.0f),
		SendPropInt(FIELD.OF(PREFIX_PROPS + nameof(AnimationLayer.Order)), ORDER_BITS, PropFlags.Unsigned),
	]); public static readonly ServerClass ServerClass = new ServerClass("AnimationLayer", DT_AnimationLayer);
#endif
}

#endif
