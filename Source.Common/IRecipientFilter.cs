namespace Source.Common;

public interface IRecipientFilter
{
	bool IsReliable();
	bool IsInitMessage();
	int GetRecipientCount();
	int GetRecipientIndex(int slot);
}
