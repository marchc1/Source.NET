#if (CLIENT_DLL || GAME_DLL) && GMOD_DLL

#if CLIENT_DLL
using Game.Client;

using Source;

#endif

using Source.Common;
using Source.Common.MaterialSystem;
using Source.Common.Mathematics;

using Steamworks;

using System;
using System.Numerics;
namespace Game.Shared.GarrysMod;
using FIELD = Source.FIELD<WeaponPhysCannon>;
public class WeaponPhysCannon : BaseHL2MPCombatWeapon
{
#if CLIENT_DLL
	public enum EffectType {
		Core,
		
		Blast,
		
		Glow1,
		Glow2,
		Glow3,
		Glow4,
		Glow5,
		Glow6,

		EndCap1,
		EndCap2,
		EndCap3,

		NumPhyscannonParameters
	}
	public const int NUM_GLOW_SPRITES = (int)(EffectType.Glow6 - EffectType.Glow1) + 1;
	public const int NUM_ENDCAP_SPRITES = (int)(EffectType.EndCap3- EffectType.EndCap1) + 1;
	public const int NUM_PHYSCANNON_BEAMS = 3;

	public enum EffectState_t {
		None,
		Closed,
		Ready,
		Holding,
		Launch
	}

	public class PhysCannonEffect {
		public PhysCannonEffect() {
			Color = new(255, 255, 255);
			Visible = true;
			Attachment = -1;
		}
		readonly InterpolatedValue Alpha = new();
		readonly InterpolatedValue Scale = new();

		public void SetAttachment(int attachment) => Attachment = attachment;
		public int GetAttachment() => Attachment;

		public void SetVisible(bool visible = true) => Visible = visible;
		public bool IsVisible() => Visible;

		public bool SetMaterial(ReadOnlySpan<char> name) {
			Material.Init(name, MaterialDefines.TEXTURE_GROUP_CLIENT_EFFECTS);
			return Material.IsNotNull;
		}

		public void SetColor(in Vector3 color) => Color = color;
		public ref readonly Vector3 GetColor() => ref Color;

		public MaterialReference GetMaterial() => Material;
		public InterpolatedValue GetAlpha() => Alpha;
		public InterpolatedValue GetScale() => Scale;

		Vector3 Color;
		bool Visible;
		int Attachment;
		readonly MaterialReference Material = new();
	}

	readonly PhysCannonEffect[] Parameters = new PhysCannonEffect[(int)EffectType.NumPhyscannonParameters].InstantiateArray();

#endif

	public static readonly
#if CLIENT_DLL
		RecvTable
#else
		SendTable
#endif
		DT_WeaponPhysCannon = new(DT_BaseHL2MPCombatWeapon, [
#if CLIENT_DLL
			RecvPropBool(FIELD.OF(nameof(Active))),
			RecvPropEHandle(FIELD.OF(nameof(AttachedObject))),
			RecvPropVector(FIELD.OF(nameof(AttachedPositionObjectSpace))),
			RecvPropFloat(FIELD.OF_VECTORELEM(nameof(AttachedAnglesPlayerSpace), 0)),
			RecvPropFloat(FIELD.OF_VECTORELEM(nameof(AttachedAnglesPlayerSpace), 1)),
			RecvPropFloat(FIELD.OF_VECTORELEM(nameof(AttachedAnglesPlayerSpace), 2)),
			RecvPropInt(FIELD.OF(nameof(EffectState))),
			RecvPropBool(FIELD.OF(nameof(Open))),
			RecvPropBool(FIELD.OF(nameof(PhyscannonState))),
#else
			SendPropBool(FIELD.OF(nameof(Active))),
			SendPropEHandle(FIELD.OF(nameof(AttachedObject))),
			SendPropVector(FIELD.OF(nameof(AttachedPositionObjectSpace)), 0, PropFlags.Coord),
			SendPropFloat(FIELD.OF_VECTORELEM(nameof(AttachedAnglesPlayerSpace), 0), 11, PropFlags.RoundDown),
			SendPropFloat(FIELD.OF_VECTORELEM(nameof(AttachedAnglesPlayerSpace), 1), 11, PropFlags.RoundDown),
			SendPropFloat(FIELD.OF_VECTORELEM(nameof(AttachedAnglesPlayerSpace), 2), 11, PropFlags.RoundDown),
			SendPropInt(FIELD.OF(nameof(EffectState))),
			SendPropBool(FIELD.OF(nameof(Open))),
			SendPropBool(FIELD.OF(nameof(PhyscannonState))),
#endif
		]);
#if CLIENT_DLL
	public static readonly new ClientClass ClientClass = new ClientClass("WeaponPhysCannon", null, null, DT_WeaponPhysCannon).WithManualClassID(StaticClassIndices.CWeaponPhysCannon);
#else
	public static readonly new ServerClass ServerClass = new ServerClass("WeaponPhysCannon", DT_WeaponPhysCannon).WithManualClassID(StaticClassIndices.CWeaponPhysCannon);
#endif
	public bool Active;
	public readonly EHANDLE AttachedObject = new();
	public Vector3 AttachedPositionObjectSpace;
	public QAngle AttachedAnglesPlayerSpace;
	public int EffectState;
	public bool Open;
	public bool PhyscannonState;
#if CLIENT_DLL
	public bool OldOpen;
	public int OldEffectState;
	public readonly InterpolatedValue ElementParameter = new();
#endif
	public void OpenElements() {
		if (Open)
			return;
		WeaponSound(Shared.WeaponSound.Special1);

		BasePlayer? owner = ToBasePlayer(GetOwner());
		if (owner == null)
			return;

		SendWeaponAnim(Activity.ACT_VM_IDLE);
		Open = true;
		// DoEffect() todo
	}
	public void CloseElements() {
		if (!Open)
			return;
		WeaponSound(Shared.WeaponSound.MeleeHit);

		BasePlayer? owner = ToBasePlayer(GetOwner());
		if (owner == null)
			return;

		SendWeaponAnim(Activity.ACT_VM_IDLE);
		Open = false;
		// DoEffect() todo
	}

	public const float SPRITE_SCALE = 128f;

	static readonly string[] attachNamesGlowThirdPerson = [
		"fork1m",
		"fork1t",
		"fork2m",
		"fork2t",
		"fork3m",
		"fork3t",
	];

	static readonly string[] attachNamesGlow = [
		"fork1b",
		"fork1m",
		"fork1t",
		"fork2b",
		"fork2m",
		"fork2t"
	];
	static readonly string[] attachNamesEndCap = [
		"fork1t",
		"fork2t",
		"fork3t"
	];
	public const string PHYSCANNON_BEAM_SPRITE = "sprites/orangelight1.vmt";
	public const string PHYSCANNON_BEAM_SPRITE_NOZ = "sprites/orangelight1_noz.vmt";
	public const string PHYSCANNON_GLOW_SPRITE = "sprites/glow04_noz";
	public const string PHYSCANNON_ENDCAP_SPRITE = "sprites/orangeflare1";
	public const string PHYSCANNON_CENTER_GLOW = "sprites/orangecore1";
	public const string PHYSCANNON_BLAST_SPRITE = "sprites/orangecore2";

	
	public void StartEffects() {
#if CLIENT_DLL
		if (Parameters[(int)EffectType.Core].GetMaterial().IsNull) {
			Parameters[(int)EffectType.Core].GetScale().Init(0.0f, 1.0f, 0.1f);
			Parameters[(int)EffectType.Core].GetAlpha().Init(255.0f, 255.0f, 0.1f);
			Parameters[(int)EffectType.Core].SetAttachment(1);

			if (Parameters[(int)EffectType.Core].SetMaterial(PHYSCANNON_CENTER_GLOW) == false) {
				// This means the texture was not found
				Assert(false);
			}
		}

		// ------------------------------------------
		// Blast
		// ------------------------------------------

		if (Parameters[(int)EffectType.Blast].GetMaterial().IsNull) {
			Parameters[(int)EffectType.Blast].GetScale().Init(0.0f, 1.0f, 0.1f);
			Parameters[(int)EffectType.Blast].GetAlpha().Init(255.0f, 255.0f, 0.1f);
			Parameters[(int)EffectType.Blast].SetAttachment(1);
			Parameters[(int)EffectType.Blast].SetVisible(false);

			if (Parameters[(int)EffectType.Blast].SetMaterial(PHYSCANNON_BLAST_SPRITE) == false) {
				Assert(false);
			}
		}

		// ------------------------------------------
		// Glows
		// ------------------------------------------

		

		//Create the glow sprites
		for (int i = (int)EffectType.Glow1; i < ((int)EffectType.Glow1 + NUM_GLOW_SPRITES); i++) {
			if (Parameters[i].GetMaterial() != null)
				continue;

			Parameters[i].GetScale().SetAbsolute(0.05f * SPRITE_SCALE);
			Parameters[i].GetAlpha().SetAbsolute(64.0f);

			// Different for different views
			if (ShouldDrawUsingViewModel()) 
				Parameters[i].SetAttachment(LookupAttachment(attachNamesGlow[i - (int)EffectType.Glow1]));
			else 
				Parameters[i].SetAttachment(LookupAttachment(attachNamesGlowThirdPerson[i - (int)EffectType.Glow1]));
			Parameters[i].SetColor(new(255, 128, 0));

			if (Parameters[i].SetMaterial(PHYSCANNON_GLOW_SPRITE) == false) {
				// This means the texture was not found
				Assert(0);
			}
		}

		// ------------------------------------------
		// End caps
		// ------------------------------------------


		//Create the glow sprites
		for (int i = (int)EffectType.EndCap1; i < ((int)EffectType.EndCap1 + NUM_ENDCAP_SPRITES); i++) {
			if (Parameters[i].GetMaterial().IsNull)
				continue;

			Parameters[i].GetScale().SetAbsolute(0.05f * SPRITE_SCALE);
			Parameters[i].GetAlpha().SetAbsolute(255.0f);
			Parameters[i].SetAttachment(LookupAttachment(attachNamesEndCap[i - (int)EffectType.EndCap1]));
			Parameters[i].SetVisible(false);

			if (Parameters[i].SetMaterial(PHYSCANNON_ENDCAP_SPRITE) == false) {
				// This means the texture was not found
				Assert(0);
			}
		}
#endif
	}
	public void DoEffectIdle() {
#if CLIENT_DLL
		StartEffects();
		for (EffectType i = EffectType.Glow1; i < (EffectType.Glow1 + NUM_GLOW_SPRITES); i++) {
			Parameters[(int)i].GetScale().SetAbsolute(random.RandomFloat(0.075f, 0.05f) * SPRITE_SCALE);
			Parameters[(int)i].GetAlpha().SetAbsolute(random.RandomInt(24, 32));
		}
		for (EffectType i = EffectType.EndCap1; i < (EffectType.EndCap1 + NUM_ENDCAP_SPRITES); i++) {
			Parameters[(int)i].GetScale().SetAbsolute(random.RandomFloat(3, 5));
			Parameters[(int)i].GetAlpha().SetAbsolute(random.RandomInt(200, 255));
		}
		if (EffectState != (int)EffectState_t.Holding) {
			// todo: beams
		}
#endif
	}
	public void DoEffectReady() {
#if CLIENT_DLL
		// Special POV case
		if (ShouldDrawUsingViewModel()) {
			//Turn on the center sprite
			Parameters[(int)EffectType.Core].GetScale().InitFromCurrent(14.0f, 0.2f);
			Parameters[(int)EffectType.Core].GetAlpha().InitFromCurrent(128.0f, 0.2f);
			Parameters[(int)EffectType.Core].SetVisible();
		}
		else {
			//Turn off the center sprite
			Parameters[(int)EffectType.Core].GetScale().InitFromCurrent(8.0f, 0.2f);
			Parameters[(int)EffectType.Core].GetAlpha().InitFromCurrent(0.0f, 0.2f);
			Parameters[(int)EffectType.Core].SetVisible();
		}

		// Turn on the glow sprites
		for (int i = (int)EffectType.Glow1; i < ((int)EffectType.Glow1 + NUM_GLOW_SPRITES); i++) {
			Parameters[i].GetScale().InitFromCurrent(0.4f * SPRITE_SCALE, 0.2f);
			Parameters[i].GetAlpha().InitFromCurrent(64.0f, 0.2f);
			Parameters[i].SetVisible();
		}

		// Turn on the glow sprites
		for (int i = (int)EffectType.EndCap1; i < ((int)EffectType.EndCap1 + NUM_ENDCAP_SPRITES); i++) {
			Parameters[i].SetVisible(false);
		}
#endif
	}
#if CLIENT_DLL
	public bool IsEffectVisible(EffectType effectID) {
		return Parameters[(int)effectID].IsVisible();
	}

	public void DrawEffectSprite(EffectType effectID) {
		if (!IsEffectVisible(effectID))
			return;

		GetEffectParameters(effectID, out Color color, out float scale, out IMaterial material, out Vector3 attachment);

		if (color.A <= 0)
			return;

		using MatRenderContextPtr renderContext = new(materials);
		renderContext.Bind(material, this);
		DrawSprite(attachment, scale, scale, color);
	}

	private void GetEffectParameters(EffectType effectID, out Color color, out float scale, out IMaterial material, out Vector3 attachment) {
		TimeUnit_t dt = gpGlobals.CurTime;
		float alpha = Parameters[(int)effectID].GetAlpha().Interp(dt);
		scale = Parameters[(int)effectID].GetScale().Interp(dt);
		material = Parameters[(int)effectID].GetMaterial().Get()!;
		color.R = (byte)(int)Parameters[(int)effectID].GetColor().X;
		color.B = (byte)(int)Parameters[(int)effectID].GetColor().Y;
		color.G = (byte)(int)Parameters[(int)effectID].GetColor().Z;
		color.A = (byte)(int)alpha;
		int attachmentIdx = Parameters[(int)effectID].GetAttachment(); 

		// Format for first-person
		if (ShouldDrawUsingViewModel()) {
			BasePlayer? owner = ToBasePlayer(GetOwner());

			if (owner != null) {
				owner.GetViewModel()!.GetAttachment(attachmentIdx, out attachment, out _);
				BaseViewModel.FormatViewModelAttachment(ref attachment, true);
			}
			else {
				attachment = default;
			}
		}
		else {
			GetAttachment(attachmentIdx, out attachment, out _);
		}
	}

	public override void ClientThink() {
		UpdateElementPosition();
		DoEffectIdle();
	}

	public void DrawEffects() {
		DrawEffectSprite(EffectType.Core);
		DrawEffectSprite(EffectType.Blast);
		for (EffectType i = EffectType.Glow1; i < (EffectType.Glow1 + NUM_GLOW_SPRITES); i++)
			DrawEffectSprite(i);
		for (EffectType i = EffectType.EndCap1; i < (EffectType.EndCap1 + NUM_ENDCAP_SPRITES); i++)
			DrawEffectSprite(i);
	}

	public override int DrawModel(StudioFlags flags) {
		if((flags & StudioFlags.Transparency) != 0) {
			return 1;
		}
		// TODO: MOVE THIS NEXT LINE UP TO ONLY TRANSPARENCY!!!
		// Engine doesnt support this yet
		DrawEffects();
		return base.DrawModel(flags);
	}
	public override void ViewModelDrawn(BaseViewModel viewmodelflags) {
		DrawEffects();
		base.ViewModelDrawn(viewmodelflags);
	}
	public void UpdateElementPosition() {
		BasePlayer? owner = ToBasePlayer(GetOwner());
		float elementPosition = ElementParameter.Interp(gpGlobals.CurTime);

		if (ShouldDrawUsingViewModel()) {
			if(owner != null) {
				BaseViewModel? vm = owner.GetViewModel();
				if (vm != null)
					vm.SetPoseParameter("active", elementPosition);
			}
		}
	}
	public override void OnDataChanged(DataUpdateType type) {
		base.OnDataChanged(type);

		if (type == DataUpdateType.Created) {
			SetNextClientThink(CLIENT_THINK_ALWAYS);
		}

		// Update effect state when out of parity with the server
		if (OldEffectState != EffectState) {
			// DoEffect(EffectState);
			OldEffectState = EffectState;
		}

		// Update element state when out of parity
		if (OldOpen != Open) {
			if (Open) 
				ElementParameter.InitFromCurrent(1.0f, 0.2f, InterpType.Spline);
			else 
				ElementParameter.InitFromCurrent(0.0f, 0.5f, InterpType.Spline);

			OldOpen = (bool)Open;
		}
	}
#endif

}
#endif
