using Game.Shared;

using Source;
using Source.Common;

namespace Game.Server;

// ###################################################################
//	> BaseFilter
// ###################################################################
[LinkEntityToClass("filter_base")]
public class BaseFilter : LogicalEntity
{
	public static readonly new DataMap DataDesc = new(typeof(BaseFilter), LogicalEntity.DataDesc, [
		DEFINE<BaseFilter>.KEYFIELD(nameof(Negated), FieldType.Boolean, "Negated"),

		// Inputs
		DEFINE<BaseFilter>.INPUTFUNC(FieldType.Input, "TestActivator", nameof(InputTestActivator), (INPUTFUNCPTR)((self, data) => ((BaseFilter)self).InputTestActivator(data))),

		// Outputs
		DEFINE<BaseFilter>.OUTPUT(nameof(OnPass), "OnPass", eventFuncs),
		DEFINE<BaseFilter>.OUTPUT(nameof(OnFail), "OnFail", eventFuncs),
	]);
	public override DataMap? GetDataDescMap() => DataDesc;

	public bool PassesFilter(BaseEntity? caller, BaseEntity? entity) {
		bool baseResult = PassesFilterImpl(caller, entity);
		return Negated ? !baseResult : baseResult;
	}

	public new bool PassesDamageFilter(in TakeDamageInfo info) {
		bool baseResult = PassesDamageFilterImpl(in info);
		return Negated ? !baseResult : baseResult;
	}

	public bool Negated;

	//-----------------------------------------------------------------------------
	// Purpose: Input handler for testing the activator. If the activator passes the
	//			filter test, the OnPass output is fired. If not, the OnFail output is fired.
	//-----------------------------------------------------------------------------
	public void InputTestActivator(InputData inputdata) {
		if (PassesFilter(inputdata.Caller, inputdata.Activator))
			OnPass.FireOutput(inputdata.Activator, this);
		else
			OnFail.FireOutput(inputdata.Activator, this);
	}

	public OutputEvent OnPass = new();      // Fired when filter is passed
	public OutputEvent OnFail = new();      // Fired when filter is failed

	protected virtual bool PassesFilterImpl(BaseEntity? caller, BaseEntity? entity) {
		return true;
	}

	protected virtual bool PassesDamageFilterImpl(in TakeDamageInfo info) {
		return PassesFilterImpl(null, info.GetAttacker());
	}
}

// ###################################################################
//	> FilterMultiple
//
//   Allows one to filter through mutiple filters
// ###################################################################
[LinkEntityToClass("filter_multi")]
public class FilterMultiple : BaseFilter
{
	public const int MAX_FILTERS = 5;

	public enum FilterType
	{
		And,
		Or,
	}

	int NFilterType;
	InlineArray5<string?> FilterName;
	InlineArray5<EHANDLE> Filter;

	public static readonly new DataMap DataDesc = new(typeof(FilterMultiple), BaseFilter.DataDesc, [
		// Keys
		DEFINE<FilterMultiple>.KEYFIELD(nameof(NFilterType), FieldType.Integer, "FilterType"),
	]);
	public override DataMap? GetDataDescMap() => DataDesc;

	public override bool KeyValue(ReadOnlySpan<char> keyName, ReadOnlySpan<char> value) {
		if (keyName.Length == 8 && keyName.StartsWith("Filter0", StringComparison.OrdinalIgnoreCase) && keyName[7] >= '1' && keyName[7] <= '5') {
			FilterName[keyName[7] - '1'] = new(value.SliceNullTerminatedString());
			return true;
		}

		return base.KeyValue(keyName, value);
	}

	//------------------------------------------------------------------------------
	// Purpose : Called after all entities have been loaded
	//------------------------------------------------------------------------------
	public override void Activate() {
		base.Activate();

		// We may reject an entity specified in the array of names, but we want the array of valid filters to be contiguous!
		int nextFilter = 0;

		// Get handles to my filter entities
		for (int i = 0; i < MAX_FILTERS; i++) {
			if (FilterName[i] != null) {
				BaseEntity? entity = gEntList.FindEntityByName(null, FilterName[i]);
				BaseFilter? filter = entity as BaseFilter;
				if (filter == null) {
					Warning($"filter_multi: Tried to add entity ({FilterName[i]}) which is not a filter entity!\n");
					continue;
				}

				// Take this entity and increment out array pointer
				Filter[nextFilter].Set(filter);
				nextFilter++;
			}
		}
	}

	//-----------------------------------------------------------------------------
	// Purpose: Returns true if the entity passes our filter, false if not.
	// Input  : pEntity - Entity to test.
	//-----------------------------------------------------------------------------
	protected override bool PassesFilterImpl(BaseEntity? caller, BaseEntity? entity) {
		// Test against each filter
		if (NFilterType == (int)FilterType.And) {
			for (int i = 0; i < MAX_FILTERS; i++) {
				if (Filter[i].Get() is BaseFilter filter) {
					if (!filter.PassesFilter(caller, entity))
						return false;
				}
			}
			return true;
		}
		else { // m_nFilterType == FILTER_OR
			for (int i = 0; i < MAX_FILTERS; i++) {
				if (Filter[i].Get() is BaseFilter filter) {
					if (filter.PassesFilter(caller, entity))
						return true;
				}
			}
			return false;
		}
	}

	//-----------------------------------------------------------------------------
	// Purpose: Returns true if the entity passes our filter, false if not.
	// Input  : pEntity - Entity to test.
	//-----------------------------------------------------------------------------
	protected override bool PassesDamageFilterImpl(in TakeDamageInfo info) {
		// Test against each filter
		if (NFilterType == (int)FilterType.And) {
			for (int i = 0; i < MAX_FILTERS; i++) {
				if (Filter[i].Get() is BaseFilter filter) {
					if (!filter.PassesDamageFilter(info))
						return false;
				}
			}
			return true;
		}
		else { // m_nFilterType == FILTER_OR
			for (int i = 0; i < MAX_FILTERS; i++) {
				if (Filter[i].Get() is BaseFilter filter) {
					if (filter.PassesDamageFilter(info))
						return true;
				}
			}
			return false;
		}
	}
}

// ###################################################################
//	> FilterName
// ###################################################################
[LinkEntityToClass("filter_activator_name")]
public class FilterName : BaseFilter
{
	public string? FilterNameValue;

	public static readonly new DataMap DataDesc = new(typeof(FilterName), BaseFilter.DataDesc, [
		// Keyfields
		DEFINE<FilterName>.KEYFIELD(nameof(FilterNameValue), FieldType.String, "filtername"),
	]);
	public override DataMap? GetDataDescMap() => DataDesc;

	protected override bool PassesFilterImpl(BaseEntity? caller, BaseEntity? entity) {
		// special check for !player as GetEntityName for player won't return "!player" as a name
		if (FStrEq(FilterNameValue, "!player"))
			return entity!.IsPlayer();
		else
			return entity!.NameMatches(FilterNameValue);
	}
}

// ###################################################################
//	> FilterClass
// ###################################################################
[LinkEntityToClass("filter_activator_class")]
public class FilterClass : BaseFilter
{
	public string? FilterClassValue;

	public static readonly new DataMap DataDesc = new(typeof(FilterClass), BaseFilter.DataDesc, [
		// Keyfields
		DEFINE<FilterClass>.KEYFIELD(nameof(FilterClassValue), FieldType.String, "filterclass"),
	]);
	public override DataMap? GetDataDescMap() => DataDesc;

	protected override bool PassesFilterImpl(BaseEntity? caller, BaseEntity? entity) {
		return entity!.ClassMatches(FilterClassValue);
	}
}

// ###################################################################
//	> FilterTeam
// ###################################################################
[LinkEntityToClass("filter_activator_team")]
public class FilterTeam : BaseFilter
{
	public int FilterTeamValue;

	public static readonly new DataMap DataDesc = new(typeof(FilterTeam), BaseFilter.DataDesc, [
		// Keyfields
		DEFINE<FilterTeam>.KEYFIELD(nameof(FilterTeamValue), FieldType.Integer, "filterteam"),
	]);
	public override DataMap? GetDataDescMap() => DataDesc;

	protected override bool PassesFilterImpl(BaseEntity? caller, BaseEntity? entity) {
		return entity!.GetTeamNumber() == FilterTeamValue;
	}
}

// ###################################################################
//	> FilterMassGreater
// ###################################################################
[LinkEntityToClass("filter_activator_mass_greater")]
public class FilterMassGreater : BaseFilter
{
	public float FilterMass;

	public static readonly new DataMap DataDesc = new(typeof(FilterMassGreater), BaseFilter.DataDesc, [
		// Keyfields
		DEFINE<FilterMassGreater>.KEYFIELD(nameof(FilterMass), FieldType.Float, "filtermass"),
	]);
	public override DataMap? GetDataDescMap() => DataDesc;

	protected override bool PassesFilterImpl(BaseEntity? caller, BaseEntity? entity) {
		if (entity!.VPhysicsGetObject() == null)
			return false;

		return entity.VPhysicsGetObject()!.GetMass() > FilterMass;
	}
}

// ###################################################################
//	> FilterDamageType
// ###################################################################
[LinkEntityToClass("filter_damage_type")]
public class FilterDamageType : BaseFilter
{
	protected int DamageType;

	public static readonly new DataMap DataDesc = new(typeof(FilterDamageType), BaseFilter.DataDesc, [
		// Keyfields
		DEFINE<FilterDamageType>.KEYFIELD(nameof(DamageType), FieldType.Integer, "damagetype"),
	]);
	public override DataMap? GetDataDescMap() => DataDesc;

	protected override bool PassesFilterImpl(BaseEntity? caller, BaseEntity? entity) {
		Assert(false);
		return true;
	}

	protected override bool PassesDamageFilterImpl(in TakeDamageInfo info) {
		return (int)info.GetDamageType() == DamageType;
	}
}
