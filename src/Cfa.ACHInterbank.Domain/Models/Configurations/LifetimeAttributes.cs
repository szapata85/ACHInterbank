namespace Cfa.ACHInterbank.Domain.Models.Configurations;

[AttributeUsage(AttributeTargets.Class)]
public class SingletonAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Class)]
public class ScopedAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Class)]
public class TransientAttribute : Attribute { }
