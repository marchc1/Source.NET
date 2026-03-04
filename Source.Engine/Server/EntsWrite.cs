using Source.Common.Bitbuffers;

namespace Source.Engine.Server;

class EntityWriteInfo : EntityInfo
{
	public bf_write Buffer;
	public int ClientEntity;
	public PackedEntity OldPack;
	public PackedEntity NewPack;
	public FrameSnapshot? FromSnapshot; // = From->GetSnapshot();
	public FrameSnapshot ToSnapshot; // = m_pTo->GetSnapshot();
	public FrameSnapshot Baseline; // the clients baseline
	public BaseServer Server; // the server who writes this entity
	public int FullProps; // number of properties send as full update (Enter PVS)
	public bool CullProps;  // filter props by clients in recipient lists
};