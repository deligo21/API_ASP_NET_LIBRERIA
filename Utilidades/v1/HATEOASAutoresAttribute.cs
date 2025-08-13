﻿using BibliotecaAPI.DTOs;
using BibliotecaAPI.Servicios.v1;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BibliotecaAPI.Utilidades.v1
{
    public class HATEOASAutoresAttribute : HATEOASFilterAttribute
    {
        private readonly IGeneradorEnlaces generadorEnlaces;

        public HATEOASAutoresAttribute(IGeneradorEnlaces generadorEnlaces)
        {
            this.generadorEnlaces = generadorEnlaces;
        }

        public override async Task OnResultExecutionAsync
            (ResultExecutingContext context, ResultExecutionDelegate next)
        {
            var incluirHATEOAS = DebeIncluirHATEOAS(context);

            if (!incluirHATEOAS)
            {
                await next();
                return;
            }

            var result = context.Result as ObjectResult;
            var modelo = result!.Value as List<AutorDTO> ??
                    throw new ArgumentNullException("Se esperaba una instancia de List<AutorDTO>");
            context.Result = new OkObjectResult(await generadorEnlaces.GenerarEnlaces(modelo));
            await next();
        }
    }
}
