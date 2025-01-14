﻿using Autofac;
using DionysosFX.Swan;
using DionysosFX.Swan.Exceptions;
using System.Collections.Generic;
using System.Linq;

namespace DionysosFX.Module.OpenApi
{
    public static class OpenApiModuleExtension
    {
        /// <summary>
        /// OpenAPI module add the container
        /// </summary>
        /// <param name="this"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IHostBuilder AddOpenApiModule(this IHostBuilder @this, OpenApiModuleOptions options)
        {
            @this.ContainerBuilder.Register(r => options).As<OpenApiModuleOptions>().SingleInstance();
            @this
                .ContainerBuilder
                .RegisterType<OpenApiModule>()
                .SingleInstance();
            return @this;
        }

        /// <summary>
        /// OpenAPI module add module container
        /// </summary>
        /// <param name="this"></param>
        /// <returns></returns>
        public static IHostBuilder UseOpenApiModule(this IHostBuilder @this)
        {
            if (!@this.Container.TryResolve(out OpenApiModule module))
                throw new ModuleNotFoundException(typeof(OpenApiModule).Name);
            module.SetIContainer(@this.Container);
            @this.ModuleCollection.Add(module.GetType().Name, module);
            return @this;
        }

        public static List<string> AddHeader(this List<string> @this,string key)
        {
            if (@this.Any(f=> f.Equals(key)))
                return @this;
            @this.Add(key);
            return @this;
        }
    }
}
