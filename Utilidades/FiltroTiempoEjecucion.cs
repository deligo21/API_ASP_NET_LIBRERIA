﻿using Microsoft.AspNetCore.Mvc.Filters;
using System.Diagnostics;

namespace BibliotecaAPI.Utilidades
{
    public class FiltroTiempoEjecucion : IAsyncActionFilter
    {
        private readonly ILogger<FiltroTiempoEjecucion> logger;

        public FiltroTiempoEjecucion(ILogger<FiltroTiempoEjecucion> logger)
        {
            this.logger = logger;
        }
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // Antes de la ejecución de la acción
            var stopWatch = Stopwatch.StartNew();

            logger.LogInformation(
                "Iniciando la ejecución de la acción: {ActionName}", 
                context.ActionDescriptor.DisplayName);

            await next();
            
            // Después de la ejecución de la acción
            
            stopWatch.Stop();

            logger.LogInformation(
                "Acción ejecutada: {ActionName} en {ElapsedMilliseconds} ms", 
                context.ActionDescriptor.DisplayName, stopWatch.ElapsedMilliseconds);
        }
    }
}
