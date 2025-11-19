namespace Source;
/// <summary> Pull out an interface from the service provider by its field or property type. </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class DependencyAttribute : Attribute
{
	public bool Required = true;
	public virtual Type? GetUnderlyingType() => null;
}

/// <summary> Pull out an keyed interface from the service provider by its field or property type. </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class KeyedDependencyAttribute : DependencyAttribute
{
	public object? Key;
}

/// <summary> Pull out an interface from the service provider by a specific type, which is cast to the field/property type this attribute is placed on. </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class DependencyAttribute<T> : DependencyAttribute
{
	public override Type? GetUnderlyingType() => typeof(T);
}

/// <summary> Pull out an keyed interface from the service provider by a specific type, which is cast to the field/property type this attribute is placed on. </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class KeyedDependencyAttribute<T> : DependencyAttribute<T>
{
	public object? Key;
}
