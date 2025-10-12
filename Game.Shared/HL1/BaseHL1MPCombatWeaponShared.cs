#if CLIENT_DLL
global using BaseHL1MPCombatWeapon = Game.Client.HL1.C_BaseHL1MPCombatWeapon;
#elif GAME_DLL
global using BaseHL1MPCombatWeapon = Game.Server.HL1.BaseHL1MPCombatWeapon;
#endif
