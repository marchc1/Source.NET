using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Game.Shared;
#if CLIENT_DLL || GAME_DLL
public interface IEntityDataInstantiator
{
	ref T GetDataObject<T>(BaseEntity instance) where T : new();
	ref T CreateDataObject<T>(BaseEntity instance) where T : new();
	void DestroyDataObject(BaseEntity instance);
}

public class EntityDataInstantiator<T> : IEntityDataInstantiator where T : new()
{

	readonly Dictionary<BaseEntity, T> Data = [];

	public ref U CreateDataObject<U>(BaseEntity instance) where U : new() {
		if (!Data.ContainsKey(instance)) 
			Data[instance] = new();
		
		return ref CollectionsMarshal.GetValueRefOrNullRef(((EntityDataInstantiator<U>)(object)this).Data, instance);
	}

	public void DestroyDataObject(BaseEntity instance) {
		throw new NotImplementedException();
	}

	public ref U GetDataObject<U>(BaseEntity instance) where U : new() {
		return ref CollectionsMarshal.GetValueRefOrNullRef(((EntityDataInstantiator<U>)(object)this).Data, instance);
	}
}
#endif
