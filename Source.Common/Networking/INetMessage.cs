using Source.Common.Bitbuffers;

namespace Source.Common.Networking;

public interface INetMessage
{
	public virtual INetChannel? GetNetChannel() => null;
	public virtual void SetNetChannel(INetChannel netchan) { }

	public virtual bool IsReliable() => false;
	public virtual void SetReliable(bool state) { }

	public virtual int GetMessageType() => 0;
	public virtual string GetName() => "";

	public virtual bool Process() => false;

	public virtual bool ReadFromBuffer(bf_read buffer) => false;
	public virtual bool WriteToBuffer(bf_write buffer) => false;


	public virtual NetChannelGroup GetGroup() => NetChannelGroup.Generic;
}
