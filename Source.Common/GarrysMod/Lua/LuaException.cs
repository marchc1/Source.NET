namespace Source.Common.GarrysMod.Lua;

public enum LuaRaise
{
	Rethrow,
	Value,
	Message,
	Argument,
	Type
}

public sealed class LuaException : Exception
{
	public LuaRaise Raise { get; }
	public int Status { get; }
	public int Reference { get; }
	public int Argument { get; }
	public string Text { get; }

	public LuaException(LuaRaise raise, int status, int reference, int argument, string text) : base(text) {
		Raise = raise;
		Status = status;
		Reference = reference;
		Argument = argument;
		Text = text;
	}
}
