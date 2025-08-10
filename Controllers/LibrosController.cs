﻿using AutoMapper;
using BibliotecaAPI.Datos;
using BibliotecaAPI.DTOs;
using BibliotecaAPI.Entidades;
using BibliotecaAPI.Utilidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaAPI.Controllers
{
    [ApiController]
    [Route("api/libros")]
    [Authorize(Policy = "esAdmin")]
    public class LibrosController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;
        private readonly IOutputCacheStore outputCacheStore;
        private readonly ITimeLimitedDataProtector protectorLimitadoPorTiempo;
        private const string cache = "libros-obtener";

        public LibrosController(
            ApplicationDbContext context, IMapper mapper, IDataProtectionProvider protectionProvider,
            IOutputCacheStore outputCacheStore)
        {
            this.context = context;
            this.mapper = mapper;
            this.outputCacheStore = outputCacheStore;
            protectorLimitadoPorTiempo = protectionProvider.CreateProtector("BibliotecaAPI.LibrosController")
                .ToTimeLimitedDataProtector();
        }

        [HttpGet("listado/obtener-token")]
        public ActionResult<string> ObtenerToken()
        {
            var textoPlano = Guid.NewGuid().ToString();
            var token = protectorLimitadoPorTiempo.Protect(textoPlano, lifetime: TimeSpan.FromSeconds(20));
            var url = Url.RouteUrl("ObtenerListadoLibrosUsandoToken", new { token }, "https");
            return Ok(new { url });
        }

        [HttpGet("listado/{token}", Name = "ObtenerListadoLibrosUsandoToken")]
        [AllowAnonymous]
        public async Task<ActionResult> ObtenerListadoLibrosUsandoToken(string token)
        {
            try
            {
                protectorLimitadoPorTiempo.Unprotect(token);
            }
            catch
            {
                ModelState.AddModelError(nameof(token), "El token es inválido o ha expirado.");
                return ValidationProblem();
            }
            var libros = await context.Libros.ToListAsync();
            var librosDTO = mapper.Map<IEnumerable<LibroDTO>>(libros);
            return Ok(librosDTO);
        }

        [HttpGet]
        [AllowAnonymous]
        [OutputCache(Tags = [cache])]
        public async Task<IEnumerable<LibroDTO>> Get([FromQuery] PaginacionDTO paginacionDTO)
        {
            var queryable = context.Libros.AsQueryable();
            await HttpContext.InsertarParametrosPaginacionEnCabecera(queryable);
            var libros = await queryable.OrderBy(x => x.Titulo).Paginar(paginacionDTO).ToListAsync();
            var librosDTO = mapper.Map<IEnumerable<LibroDTO>>(libros);
            return librosDTO;
        }

        [HttpGet("{id:int}", Name = "ObtenerLibro")]
        [AllowAnonymous]
        [OutputCache(Tags = [cache])]
        public async Task<ActionResult<LibroConAutoresDTO>> Get(int id)
        {
            var libro = await context.Libros
                .Include(x => x.Autores)
                    .ThenInclude(x => x.Autor)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (libro is null)
            {
                return NotFound();
            }

            var libroDTO = mapper.Map<LibroConAutoresDTO>(libro);

            return libroDTO;
        }

        [HttpPost]
        public async Task<ActionResult> Post(LibroCreacionDTO libroCreacionDTO)
        {
            if (libroCreacionDTO.AutoresIds is null || libroCreacionDTO.AutoresIds.Count == 0)
            {
                ModelState.AddModelError(nameof(libroCreacionDTO.AutoresIds),
                    "No se puede crear un libro sin autores");
                return ValidationProblem();
            }

            var autoresIdsExisten = await context.Autores
                                    .Where(x => libroCreacionDTO.AutoresIds.Contains(x.Id))
                                    .Select(x => x.Id).ToListAsync();

            if (autoresIdsExisten.Count != libroCreacionDTO.AutoresIds.Count)
            {
                var autoresNoExisten = libroCreacionDTO.AutoresIds.Except(autoresIdsExisten);
                var autoresNoExistenString = string.Join(",", autoresNoExisten);
                var mensajeDeError = $"Los siguientes autores no existen: {autoresNoExistenString}";
                ModelState.AddModelError(nameof(libroCreacionDTO.AutoresIds), mensajeDeError);
                return ValidationProblem();
            }

            var libro = mapper.Map<Libro>(libroCreacionDTO);
            AsignarOrdenAutores(libro);

            context.Add(libro);
            await context.SaveChangesAsync();
            await outputCacheStore.EvictByTagAsync(cache, default);

            var libroDTO = mapper.Map<LibroDTO>(libro);

            return CreatedAtRoute("ObtenerLibro", new { id = libro.Id }, libroDTO);
        }

        private void AsignarOrdenAutores(Libro libro)
        {
            if (libro.Autores is not null)
            {
                for (int i = 0; i < libro.Autores.Count; i++)
                {
                    libro.Autores[i].Orden = i;
                }
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult> Put(int id, LibroCreacionDTO libroCreacionDTO)
        {
            if (libroCreacionDTO.AutoresIds is null || libroCreacionDTO.AutoresIds.Count == 0)
            {
                ModelState.AddModelError(nameof(libroCreacionDTO.AutoresIds),
                    "No se puede crear un libro sin autores");
                return ValidationProblem();
            }

            var autoresIdsExisten = await context.Autores
                                    .Where(x => libroCreacionDTO.AutoresIds.Contains(x.Id))
                                    .Select(x => x.Id).ToListAsync();

            if (autoresIdsExisten.Count != libroCreacionDTO.AutoresIds.Count)
            {
                var autoresNoExisten = libroCreacionDTO.AutoresIds.Except(autoresIdsExisten);
                var autoresNoExistenString = string.Join(",", autoresNoExisten);
                var mensajeDeError = $"Los siguientes autores no existen: {autoresNoExistenString}";
                ModelState.AddModelError(nameof(libroCreacionDTO.AutoresIds), mensajeDeError);
                return ValidationProblem();
            }

            var libroDB = await context.Libros
                            .Include(x => x.Autores)
                            .FirstOrDefaultAsync(x => x.Id == id);

            if (libroDB is null)
            {
                return NotFound();
            }

            libroDB = mapper.Map(libroCreacionDTO, libroDB);
            AsignarOrdenAutores(libroDB);

            await context.SaveChangesAsync();
            await outputCacheStore.EvictByTagAsync(cache, default);
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            var registrosBorrados = await context.Libros.Where(x => x.Id == id).ExecuteDeleteAsync();

            if (registrosBorrados == 0)
            {
                return NotFound();
            }

            await outputCacheStore.EvictByTagAsync(cache, default);

            return NoContent();
        }
    }
}