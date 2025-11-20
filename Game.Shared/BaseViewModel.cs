#if CLIENT_DLL || GAME_DLL
#if CLIENT_DLL
global using BaseViewModel = Game.Client.C_BaseViewModel;
namespace Game.Client;
#else
global using BaseViewModel = Game.Server.BaseViewModel;
namespace Game.Server;
#endif

using Source.Common;
using Source;

using FIELD = Source.FIELD<BaseViewModel>;
using Game.Shared;
using Source.Common.Mathematics;
using System.Numerics;
using System;

public partial class
#if CLIENT_DLL
	C_BaseViewModel
#else
	BaseViewModel
#endif

	:

#if CLIENT_DLL
	C_BaseAnimating
#else
	BaseAnimating
#endif
{

	public const int VIEWMODEL_INDEX_BITS = 4;

	public static readonly
#if CLIENT_DLL
		RecvTable
#else
		SendTable
#endif
		DT_BaseViewModel = new([
#if CLIENT_DLL
			RecvPropInt(FIELD.OF(nameof(ModelIndex))),
			RecvPropInt(FIELD.OF(nameof(Body))),
			RecvPropInt(FIELD.OF(nameof(Skin))),
			RecvPropInt(FIELD.OF(nameof(Sequence)), 0, RecvProxy_SequenceNum),
			RecvPropInt(FIELD.OF(nameof(ViewModelIndex))),
			RecvPropFloat(FIELD.OF(nameof(PlaybackRate))),
			RecvPropInt(FIELD.OF(nameof(Effects))),
			RecvPropInt(FIELD.OF(nameof(AnimationParity))),
			RecvPropEHandle(FIELD.OF(nameof(Weapon))),
			RecvPropEHandle(FIELD.OF(nameof(Owner))),

			RecvPropInt(FIELD.OF(nameof(NewSequenceParity))),
			RecvPropInt(FIELD.OF(nameof(ResetEventsParity))),
			RecvPropInt(FIELD.OF(nameof(MuzzleFlashParity))),

			RecvPropFloat(FIELD.OF_ARRAYINDEX(nameof(PoseParameter), 0)),
			RecvPropArray(FIELD.OF_ARRAY(nameof(PoseParameter))),
#else
			SendPropModelIndex(FIELD.OF(nameof(ModelIndex))),
			SendPropInt(FIELD.OF(nameof(Body)), 32),
			SendPropInt(FIELD.OF(nameof(Skin)), 10),
			SendPropInt(FIELD.OF(nameof(Sequence)), 12, PropFlags.Unsigned),
			SendPropInt(FIELD.OF(nameof(ViewModelIndex)), VIEWMODEL_INDEX_BITS, PropFlags.Unsigned),
			SendPropFloat(FIELD.OF(nameof(PlaybackRate)), 8, PropFlags.RoundUp, -4.0f, 12.0f),
			SendPropInt(FIELD.OF(nameof(Effects)), 10, PropFlags.Unsigned),
			SendPropInt(FIELD.OF(nameof(AnimationParity)), 3, PropFlags.Unsigned),
			SendPropEHandle(FIELD.OF(nameof(Weapon))),
			SendPropEHandle(FIELD.OF(nameof(Owner))),

			SendPropInt(FIELD.OF(nameof(NewSequenceParity)), (int)EntityEffects.ParityBits, PropFlags.Unsigned ),
			SendPropInt(FIELD.OF(nameof(ResetEventsParity)), (int)EntityEffects.ParityBits, PropFlags.Unsigned ),
			SendPropInt(FIELD.OF(nameof(MuzzleFlashParity)), (int)EntityEffects.MuzzleflashBits, PropFlags.Unsigned ),

			SendPropFloat(FIELD.OF_ARRAYINDEX(nameof(PoseParameter), 0), 8, 0, 0.0f, 1.0f),
			SendPropArray(FIELD.OF_ARRAY(nameof(PoseParameter))),
#endif
		]);

#if CLIENT_DLL
	private static void RecvProxy_SequenceNum(ref readonly RecvProxyData data, object instance, IFieldAccessor field) {
		BaseViewModel model = (BaseViewModel)instance;
		if (data.Value.Int != model.GetSequence()) {
			model.SetSequence(data.Value.Int);
			model.AnimTime = gpGlobals.CurTime;
			model.SetCycle(0);
		}
	}
	public static readonly new ClientClass ClientClass = new ClientClass("BaseViewModel", null, null, DT_BaseViewModel).WithManualClassID(StaticClassIndices.CBaseViewModel);
#else
#pragma warning disable CS0109 // Member does not hide an inherited member; new keyword is not required
	public static readonly new ServerClass ServerClass = new ServerClass("BaseViewModel", DT_BaseViewModel).WithManualClassID(StaticClassIndices.CBaseViewModel);
#pragma warning restore CS0109 // Member does not hide an inherited member; new keyword is not required
#endif
	public int ViewModelIndex;
	public readonly EHANDLE Owner = new();
	public readonly Handle<BaseCombatWeapon> Weapon = new();
	public int AnimationParity;

	public BaseCombatWeapon? GetOwningWeapon() => Weapon.Get();

	public void CalcViewModelView(BasePlayer owner, in Vector3 eyePosition, in QAngle eyeAngles) {
		QAngle vmangoriginal = eyeAngles;
		QAngle vmangles = eyeAngles;
		Vector3 vmorigin = eyePosition;

		BaseCombatWeapon? pWeapon = Weapon.Get();
		//Allow weapon lagging
		if (pWeapon != null) {
#if CLIENT_DLL
			if (!prediction.InPrediction())
#endif
			{
				// add weapon-specific bob 
				// TODO: pWeapon.AddViewmodelBob(this, vmorigin, vmangles);
			}
		}
		// Add model-specific bob even if no weapon associated (for head bob for off hand models)
		// todo: AddViewModelBob(owner, vmorigin, vmangles);
		// todo: CalcViewModelLag
#if CLIENT_DLL
		if (!prediction.InPrediction()) {
			// Let the viewmodel shake at about 10% of the amplitude of the player's view
			// TODO: vieweffects.ApplyShake( vmorigin, vmangles, 0.1 );	
		}
#endif
		SetLocalOrigin(in vmorigin);
		SetLocalAngles(in vmangles);
	}
}
#endif
