using BibliotecaAPI.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace BibliotecaAPI.Servicios.v1
{
    public interface IServicioAutores
    {
        Task<IEnumerable<AutorDTO>> Get([FromQuery] PaginacionDTO paginacionDTO);
    }
}