namespace Source.Common.Commands;

public abstract class ConVar_ServerBounded : ConVar
{
	public ConVar_ServerBounded(string defaultValue, FCvar flags) : base(defaultValue, flags) {	}
	public ConVar_ServerBounded(string name, string defaultValue, FCvar flags) : base(name, defaultValue, flags) {	}
	public ConVar_ServerBounded(string defaultValue, FCvar flags, string helpText) : base(defaultValue, flags, helpText) {	}
	public ConVar_ServerBounded(string name, string defaultValue, FCvar flags, string helpText) : base(name, defaultValue, flags, helpText) {	}
	public ConVar_ServerBounded(string defaultValue, FCvar flags, string helpText, double? min = null, double? max = null, FnChangeCallback? callback = null) : base(defaultValue, flags, helpText, min, max, callback) {	}
	public ConVar_ServerBounded(string name, string defaultValue, FCvar flags, string helpText, double? min = null, double? max = null, FnChangeCallback? callback = null) : base(name, defaultValue, flags, helpText, min, max, callback) {}

	public abstract new float GetFloat();
	public override int GetInt() => (int)GetFloat();
	public override bool GetBool() => GetInt() != 0;

	public float GetBaseFloatValue() => base.GetFloat();
}
