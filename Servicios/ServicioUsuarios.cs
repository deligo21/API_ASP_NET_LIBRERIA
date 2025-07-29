using Microsoft.AspNetCore.Identity;

namespace BibliotecaAPI.Servicios
{
    public class ServicioUsuarios : IServicioUsuarios
    {
        private readonly UserManager<IdentityUser> userManager;
        private readonly IHttpContextAccessor httpContextAccessor;

        public ServicioUsuarios(UserManager<IdentityUser> userManager, IHttpContextAccessor httpContextAccessor)
        {
            this.userManager = userManager;
            this.httpContextAccessor = httpContextAccessor;
        }

        public async Task<IdentityUser?> ObtenerUsuario()
        {
            var emailClaim = httpContextAccessor.HttpContext!.User.Claims.Where(c => c.Type == "email").FirstOrDefault();
            if (emailClaim is null)
            {
                return null;
            }
            var email = emailClaim.Value;
            return await userManager.FindByEmailAsync(email);
        }
    }
}
