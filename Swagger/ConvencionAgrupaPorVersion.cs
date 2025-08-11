using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace BibliotecaAPI.Swagger
{
    public class ConvencionAgrupaPorVersion : IControllerModelConvention
    {
        public void Apply(ControllerModel controller)
        {
            var namespaceControlador = controller.ControllerType.Namespace; // "BibliotecaAPI.Controllers.v1"
            var version = namespaceControlador!.Split('.').Last().ToLower(); // "v1"
            controller.ApiExplorer.GroupName = version; // "v1"
        }
    }
}
