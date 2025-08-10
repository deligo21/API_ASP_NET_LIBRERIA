using BibliotecaAPI.Servicios;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;

namespace BibliotecaAPI.Controllers.v1
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class SeguridadController: ControllerBase
    {
        private readonly IDataProtector protector;
        private readonly ITimeLimitedDataProtector protectorLimitadoPorTiempo;
        private readonly IServicioHash servicioHash;

        public SeguridadController(IDataProtectionProvider protectionProvider, IServicioHash servicioHash)
        {
            protector = protectionProvider.CreateProtector("BibliotecaAPI.SeguridadController");
            protectorLimitadoPorTiempo  = protector.ToTimeLimitedDataProtector();
            this.servicioHash = servicioHash;
        }

        [HttpGet("hash")]
        public ActionResult Hash([FromQuery] string textoPlano)
        {
            if (string.IsNullOrEmpty(textoPlano))
            {
                return BadRequest("El texto no puede ser nulo o vacío.");
            }
            var hash1 = servicioHash.Hash(textoPlano);
            var hash2 = servicioHash.Hash(textoPlano);
            var hash3 = servicioHash.Hash(textoPlano, hash2.Sal);
            return Ok(new { textoPlano, hash1, hash2, hash3 });
        }

        [HttpGet("encriptar")]
        public ActionResult<string> Encriptar([FromQuery] string textoPlano)
        {
            if (string.IsNullOrEmpty(textoPlano))
            {
                return BadRequest("El texto no puede ser nulo o vacío.");
            }
            var textoCifrado = protector.Protect(textoPlano);
            return Ok(new { textoCifrado });
        }

        [HttpGet("desencriptar")]
        public ActionResult<string> Desencriptar(string textoCifrado)
        {
            if (string.IsNullOrEmpty(textoCifrado))
            {
                return BadRequest("El texto encriptado no puede ser nulo o vacío.");
            }
            try
            {
                var textoDesencriptado = protector.Unprotect(textoCifrado);
                return Ok(new { textoDesencriptado });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error al desencriptar: {ex.Message}");
            }
        }

        [HttpGet("encriptar-limitado-por-tiempo")]
        public ActionResult<string> EncriptarLimitadoPorTiempo([FromQuery] string textoPlano)
        {
            if (string.IsNullOrEmpty(textoPlano))
            {
                return BadRequest("El texto no puede ser nulo o vacío.");
            }
            var textoCifrado = protectorLimitadoPorTiempo.Protect(textoPlano, lifetime: TimeSpan.FromSeconds(20));
            return Ok(new { textoCifrado });
        }

        [HttpGet("desencriptar-limitado-por-tiempo")]
        public ActionResult<string> DesencriptarLimitadoPorTiempo(string textoCifrado)
        {
            if (string.IsNullOrEmpty(textoCifrado))
            {
                return BadRequest("El texto encriptado no puede ser nulo o vacío.");
            }
            try
            {
                var textoDesencriptado = protectorLimitadoPorTiempo.Unprotect(textoCifrado);
                return Ok(new { textoDesencriptado });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error al desencriptar: {ex.Message}");
            }
        }
    }
}
