global using static Game.Shared.WeaponProficiencyStatic;

using System.Reflection;

namespace Game.Shared;

[AttributeUsage(AttributeTargets.Field)]
public class WeaponProficiencyNameAttribute(string name) : Attribute {
	public string Name => name;
}

public enum WeaponProficiency
{
	[WeaponProficiencyName("Poor")] Poor,
	[WeaponProficiencyName("Average")] Average,
	[WeaponProficiencyName("Good")] Good,
	[WeaponProficiencyName("Very Good")] VeryGood,
	[WeaponProficiencyName("Perfect")] Perfect
}

public struct WeaponProficiencyInfo
{
	public float SpreadScale;
	public float Bias;
}

public static class WeaponProficiencyStatic
{
	public static readonly string[] g_ProficiencyNames = Enum.GetValues<WeaponProficiency>()
															.OrderBy(x => x)
															.Select(x => typeof(WeaponProficiency)
																			.GetField(Enum.GetName(x)!)
																			!.GetCustomAttribute<WeaponProficiencyNameAttribute>()
																			!.Name
																	)
															.ToArray()!;

	public static ReadOnlySpan<char> GetWeaponProficiencyName(WeaponProficiency proficiency) {
		if (proficiency < 0 || (int)proficiency > g_ProficiencyNames.Length)
			return "<<Invalid>>";
		return g_ProficiencyNames[(int)proficiency];
	}
}
