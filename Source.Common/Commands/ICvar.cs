using System.Reflection;

namespace Source.Common.Commands;

public delegate void FnChangeCallback(IConVar var, in ConVarChangeContext ctx);
public delegate void FnChangeCallbackSrc(IConVar var, ReadOnlySpan<char> oldString, float oldFloat);

public interface IConsoleDisplayFunc
{
	public void ColorPrint(in Color clr, ReadOnlySpan<char> message);
	public void Print(ReadOnlySpan<char> message);
	public void DPrint(ReadOnlySpan<char> message);
}

public interface ICvarQuery
{
	public bool AreConVarsLinkable(ConVar child, ConVar parent);
}

public interface ICvar
{
	public void RegisterConCommand(ConCommandBase commandBase);
	public void UnregisterConCommand(ConCommandBase commandBase);
	public void UnregisterConCommands(Assembly sourceAssembly);

	public void SetAssemblyIdentifier(Assembly assembly);

	public string? GetCommandLineValue(ReadOnlySpan<char> variableName);

	public ConCommandBase? FindCommandBase(ReadOnlySpan<char> name);
	public ConVar? FindVar(ReadOnlySpan<char> name);
	public ConCommand? FindCommand(ReadOnlySpan<char> name);

	public IEnumerable<ConCommandBase> GetCommands();

	public event FnChangeCallback? Changed;

	public void InstallConsoleDisplayFunc(IConsoleDisplayFunc displayFunc);
	public void RemoveConsoleDisplayFunc(IConsoleDisplayFunc displayFunc);

	public void ConsoleColorPrintf(in Color clr, ReadOnlySpan<char> format, params object?[]? args);
	public void ConsolePrintf(ReadOnlySpan<char> format, params object?[]? args);
	public void ConsoleDPrintf(ReadOnlySpan<char> format, params object?[]? args);

	public void RevertFlaggedConVars(FCvar flag);
}
