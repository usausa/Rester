namespace Example.Server.Infrastructure;

using System.Globalization;

using Microsoft.AspNetCore.Mvc.ApplicationModels;

public sealed class LowercaseControllerModelConvention : IControllerModelConvention
{
    public void Apply(ControllerModel controller)
    {
        controller.ControllerName = controller.ControllerName.ToLower(CultureInfo.InvariantCulture);
        foreach (var action in controller.Actions)
        {
            action.ActionName = action.ActionName.ToLower(CultureInfo.InvariantCulture);
        }
    }
}
