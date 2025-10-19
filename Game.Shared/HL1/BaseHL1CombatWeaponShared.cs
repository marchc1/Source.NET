#if CLIENT_DLL
global using BaseHL1CombatWeapon = Game.Client.HL1.C_BaseHL1CombatWeapon;
#elif GAME_DLL
global using BaseHL1CombatWeapon = Game.Server.HL1.BaseHL1CombatWeapon;
#endif
