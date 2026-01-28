using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Server;

public interface IEventRegisterCallback
{
	void FireEvent();
}
