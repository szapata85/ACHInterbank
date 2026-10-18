using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Cfa.ACHInterbank.Api.Controllers;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class ControllerContractNameAttribute(string name) : Attribute, IControllerModelConvention
{
    public string Name { get; } = name;

    public void Apply(ControllerModel controller)
    {
        controller.ControllerName = Name;
    }
}
