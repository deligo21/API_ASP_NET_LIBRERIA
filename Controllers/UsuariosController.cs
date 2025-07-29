using BibliotecaAPI.DTOs;
using BibliotecaAPI.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace BibliotecaAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsuariosController: ControllerBase
    {
        private readonly UserManager<IdentityUser> userManager;
        private readonly SignInManager<IdentityUser> signInManager;
        private readonly IConfiguration configuration;
        private readonly IServicioUsuarios servicioUsuarios;

        public UsuariosController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, IConfiguration configuration, IServicioUsuarios servicioUsuarios)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.configuration = configuration;
            this.servicioUsuarios = servicioUsuarios;
        }

        [HttpPost("registro")]
        [AllowAnonymous]
        public async Task<ActionResult<RespuestaAutenticacionDTO>> Registrar(CredencialesUsuarioDTO credencialesUsuarioDTO)
        {
            var usuario = new IdentityUser
            {
                UserName = credencialesUsuarioDTO.Email,
                Email = credencialesUsuarioDTO.Email
            };

            var resultado = await userManager.CreateAsync(usuario, credencialesUsuarioDTO.Password!);

            if (resultado.Succeeded)
            {
                var respuestaAutenticacion = await ConstruirToken(credencialesUsuarioDTO);
                return Ok(respuestaAutenticacion);
            }
            else
            {
                foreach (var error in resultado.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return ValidationProblem();
            }
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<RespuestaAutenticacionDTO>> Login(CredencialesUsuarioDTO credencialesUsuarioDTO)
        {
            var usuario = await userManager.FindByEmailAsync(credencialesUsuarioDTO.Email);
            if (usuario == null)
            {
                return RetornanLoginIncorrecto();
            }
            var resultado = await signInManager.CheckPasswordSignInAsync(usuario, credencialesUsuarioDTO.Password!, lockoutOnFailure: false);
            if (resultado.Succeeded)
            {
                return await ConstruirToken(credencialesUsuarioDTO);
            }
            else
            {
                return RetornanLoginIncorrecto();
            }
        }

        [HttpGet("renovar-token")]
        public async Task<ActionResult<RespuestaAutenticacionDTO>> RenovarToken()
        {
            var usuario = await servicioUsuarios.ObtenerUsuario();
            if (usuario is null)
            {
                return NotFound();
            }

            var credencialesUsuarioDTO = new CredencialesUsuarioDTO
            {
                Email = usuario.Email!
            };

            var respuestaAutenticacion = await ConstruirToken(credencialesUsuarioDTO);
            return respuestaAutenticacion;
        }

        private ActionResult RetornanLoginIncorrecto()
        {
            ModelState.AddModelError(string.Empty, "Usuario o contraseña incorrectos");
            return ValidationProblem();
        }

        private async Task<RespuestaAutenticacionDTO> ConstruirToken(CredencialesUsuarioDTO credencialesUsuarioDTO)
        {
            var claims = new List<Claim>
            {
                new Claim("email", credencialesUsuarioDTO.Email)
            };

            var usuario = await userManager.FindByEmailAsync(credencialesUsuarioDTO.Email);
            var claimsDB = await userManager.GetClaimsAsync(usuario!);

            claims.AddRange(claimsDB);

            var llave = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(configuration["LlaveJWT"]!));
            var credenciales = new SigningCredentials(llave, SecurityAlgorithms.HmacSha256);

            var expiracion = DateTime.UtcNow.AddDays(1);

            var tokenDeSeguridad = new JwtSecurityToken(
                issuer: null,
                audience: null,
                claims: claims,
                expires: expiracion,
                signingCredentials: credenciales
            );

            var token = new JwtSecurityTokenHandler().WriteToken(tokenDeSeguridad);
            return new RespuestaAutenticacionDTO
            {
                Token = token,
                Expiracion = expiracion
            };
        }
    }
}
