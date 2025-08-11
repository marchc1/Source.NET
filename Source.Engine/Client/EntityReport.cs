using Source.Common.Commands;
using Source.Common.Networking.DataTable;

namespace Source.Engine.Client;

enum FENTITYBITS
{
	ZERO = 0,
	ADD = 0x01,
	LEAVEPVS = 0x02,
	DELETE = 0x04,
};

struct ENTITYBITS
{
	// Bits used for last message
	public int bits;
	// Rolling average of bits used
	public float average;
	// Last bit peak
	public int peak;
	// Time at which peak was last reset
	public double peaktime;
	// Event info
	public FENTITYBITS flags;
	// If doing effect, when it will finish
	public double effectfinishtime;
	// If event was deletion, remember client class for a little bit
	public ClientClass? deletedclientclass;
};

public class EntityReport
{
	private ENTITYBITS[] EntityBits = new ENTITYBITS[Constants.MAX_EDICTS];
	public static ConVar cl_entityreport = new("0", FCvar.Cheat, "For debugging, draw entity states to console");
	private const double EFFECT_TIME = 1.5;
	public double renderTime = 0.0;

	public void RecordDeleteEntity(int Entity, ClientClass clientClass)
	{
		if (!cl_entityreport.GetBool() || Entity < 0 || Entity > Constants.MAX_EDICTS)
		{
			return;
		}

		ENTITYBITS Slot = EntityBits[Entity];
		Slot.flags = FENTITYBITS.DELETE;
		Slot.effectfinishtime = renderTime + EFFECT_TIME;
		Slot.deletedclientclass = clientClass;
	}

	public void RecordAddEntity(int Entity)
	{
		if (!cl_entityreport.GetBool() || Entity < 0 || Entity > Constants.MAX_EDICTS)
		{
			return;
		}

		ENTITYBITS Slot = EntityBits[Entity];
		Slot.flags = FENTITYBITS.ADD;
		Slot.effectfinishtime = renderTime + EFFECT_TIME;
	}
} 