using Microsoft.AspNetCore.Identity;

namespace BibliotecaAPI.Servicios
{
    public interface IServicioUsuarios
    {
        Task<IdentityUser?> ObtenerUsuario();
    }
}