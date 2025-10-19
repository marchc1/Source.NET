using Game.Client;

using Source.Common;
namespace Game.Shared.GarrysMod;

public class C_CrossbowBolt : C_BaseCombatCharacter {
	public static readonly RecvTable DT_CrossbowBolt = new(DT_BaseCombatCharacter, []);
	public static readonly new ClientClass ClientClass = new ClientClass("CrossbowBolt", DT_CrossbowBolt).WithManualClassID(StaticClassIndices.CCrossbowBolt);
}
