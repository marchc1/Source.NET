using System.Reflection;

namespace Source.Common.Commands;

public class ConCommandBase
{
	public ConCommandBase() {
		Registered = false;
		Name = null;
		HelpString = null;
		Flags = 0;
		Next = null;
	}

	public ConCommandBase(string name, string? helpString = null, FCvar flags = 0) {
		CreateBase(name, helpString, flags);
	}

	public virtual bool IsCommand() => false;
	public virtual bool IsFlagSet(FCvar flag) => (Flags & flag) == flag;
	public virtual void AddFlags(FCvar flags) => Flags |= flags;
	public virtual string GetName() => Name;
	/// <summary>
	/// For internal use only
	/// </summary>
	/// <param name="name"></param>
	/// <returns></returns>
	public virtual void SetName(string name) => Name = name!;
	public virtual string? GetHelpText() => HelpString;
	public ConCommandBase? GetNext() => Next;
	public virtual bool IsRegistered() => Registered;

	protected virtual void CreateBase(string name, string? helpString = null, FCvar flags = 0) {
		Registered = false;
		Name = name;
		HelpString = helpString;
		Flags = flags;
		if ((flags & FCvar.Unregistered) != FCvar.Unregistered) {
			Next = ConCommandBases;
			ConCommandBases = this;
		}
		else {
			Next = null;
		}
	}

	internal ConCommandBase? Next;
	internal bool Registered;
	internal string Name;
	internal string? HelpString;
	internal FCvar Flags;
	internal Assembly? Assembly;
	protected static ConCommandBase? ConCommandBases;
}
