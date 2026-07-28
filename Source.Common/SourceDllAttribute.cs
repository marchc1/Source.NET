namespace Source.Common;

/// <summary>
/// Registers an assembly as a Source.NET Engine assembly; which allows various systems such as the VGUI and ConVar registration
/// systems to pick up the assembly for type initialization on engine startup.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class SourceDllAttribute : Attribute;
