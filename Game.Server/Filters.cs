using Game.Shared;

using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Server;

public class BaseFilter : LogicalEntity
{
	public bool PassesFilter(BaseEntity? caller, BaseEntity? entity){
		bool baseResult = PassesFilter(caller, entity);
		return Negated ? !baseResult : baseResult;
	}
	public new bool PassesDamageFilter(in TakeDamageInfo info) {
		bool baseResult = PassesDamageFilterImpl(in info);
		return Negated ? !baseResult : baseResult;
	}

	public bool Negated;

	public void InputTestActivator(ref InputData inputdata ){
		// if (PassesFilter(inputdata.Caller, inputdata.Activator)) 
			// OnPass.FireOutput(inputdata.Activator, this);
		// else 
			// OnFail.FireOutput(inputdata.Activator, this);
	}

	// Outputs
	// TODO: public OutputEvent OnPass;      // Fired when filter is passed
	// TODO: public OutputEvent OnFail;      // Fired when filter is failed


	protected virtual bool PassesFilterImpl(BaseEntity? caller, BaseEntity? entity){
		return true;
	}
	protected virtual bool PassesDamageFilterImpl(in TakeDamageInfo info){
		return PassesFilterImpl(null, info.GetAttacker());
	}
}
