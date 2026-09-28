using Game.Shared;

using Source.Common;

using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Server;

public class DamageModifier
{
	public DamageModifier() {
		Modifier = 1;
		DoneToMe = false;
	}

	public void AddModifierToEntity(BaseEntity ent) {

	}
	public void RemoveModifier() {
		if (Ent.Get() != null) {
			LinkedList<DamageModifier> modifiers = Ent.Get()!.DamageModifiers;
			modifiers.Remove(this);
			Ent.Set(null);
		}
	}

	public void SetModifier(float damageScale) => Modifier = damageScale;
	public float GetModifier() => Modifier;

	public void SetDoneToMe(bool doneToMe) => DoneToMe = doneToMe;
	public bool IsDamageDoneToMe() => DoneToMe;

	public BaseEntity? GetCharacter() => Ent.Get();

	float Modifier;
	Handle<BaseEntity> Ent;
	bool DoneToMe;   // True = modifies damage done to the entity, false = damage done by the entity
}
