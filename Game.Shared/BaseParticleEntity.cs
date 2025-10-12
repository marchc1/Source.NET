#if CLIENT_DLL || GAME_DLL

#if CLIENT_DLL
global using BaseParticleEntity = Game.Client.C_BaseParticleEntity;
using Source.Common;

using Game.Shared;
namespace Game.Client;
#else
using Source.Common;

using Game.Shared;
namespace Game.Server;
#endif

using Table =
#if CLIENT_DLL
	RecvTable;
#else
	SendTable;
#endif

using Class =
#if CLIENT_DLL
	ClientClass;
#else
	ServerClass;
#endif

using FIELD = Source.FIELD<BaseParticleEntity>;

public partial class
#if CLIENT_DLL
	C_BaseParticleEntity: C_BaseEntity
#else
	BaseParticleEntity : BaseEntity
#endif
{
	public static Table DT_BaseParticleEntity = new(DT_BaseEntity, []);
	public static readonly Class Class = new Class("BaseParticleEntity", DT_BaseParticleEntity).WithManualClassID(StaticClassIndices.CBaseParticleEntity);
}

#endif
