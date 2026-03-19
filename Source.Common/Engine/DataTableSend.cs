using System.Collections;

namespace Source.Common.Engine;

public class SendProxyRecipients
{
	public const int MAX_DATATABLE_PROXIES = 32;

	public BitArray Bits;

	public SendProxyRecipients(int maxPlayers) => Bits = new(maxPlayers);
	public void SetAllRecipients() => Bits.SetAll(true);
	public void ClearAllRecipients() => Bits.SetAll(false);
	public void SetRecipient(int clientIndex) => Bits.Set(clientIndex, true);
	public void ClearRecipient(int clientIndex) => Bits.Set(clientIndex, false);
	public bool GetRecipient(int clientIndex) => Bits[clientIndex];
	public void SetOnly(int clientIndex) {
		Bits.SetAll(false);
		Bits.Set(clientIndex, true);
	}
}
