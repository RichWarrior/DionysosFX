﻿using DionysosFX.Module.IWebApi;
using DionysosFX.Module.OpenApi.Attributes;
using DionysosFX.Module.OpenApi.Entities;
using DionysosFX.Module.WebApi;
using DionysosFX.Swan.Extensions;
using DionysosFX.Swan.Routing;
using HttpMultipartParser;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DionysosFX.Module.OpenApi
{
    internal static class OpenApiExtension
    {
        /// <summary>
        /// Type convert to controller
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static ControllerItem ToController(this Type type)
        {
            ControllerItem controllerItem = new ControllerItem();
            controllerItem.Name = type.Name;

            var controllerDescriptionAttribute = type.GetCustomAttribute(typeof(DescriptionAttribute));
            if (controllerDescriptionAttribute != null)
                controllerItem.Description = ((DescriptionAttribute)controllerDescriptionAttribute).Description;

            return controllerItem;
        }

        /// <summary>
        /// Type methods convert to endpoint
        /// </summary>
        /// <param name="method"></param>
        /// <param name="routePrefix"></param>
        /// <param name="schemaTypes"></param>
        /// <returns></returns>
        public static EndpointItem ToEndpoint(this MethodInfo method, string routePrefix = "", List<Type> schemaTypes = null)
        {
            EndpointItem endpointItem = new EndpointItem();
            var routeAttribute = method.GetCustomAttribute(typeof(RouteAttribute));
            if (routeAttribute == null)
                throw new Exception($"{method.Name} Route Attribute not found");

            endpointItem.Name = string.Format("{0}{1}", routePrefix, ((RouteAttribute)routeAttribute).Route);
            if (endpointItem.Name.Contains("{"))
                endpointItem.Name = endpointItem.Name.Substring(0, endpointItem.Name.IndexOf("{"));

            endpointItem.Verb = ((RouteAttribute)routeAttribute).Verb.ToString();

            var endpointDescriptionAttribute = method.GetCustomAttribute(typeof(DescriptionAttribute));
            if (endpointDescriptionAttribute != null)
                endpointItem.Description = ((DescriptionAttribute)endpointDescriptionAttribute).Description;

            var methodParameters = method.GetParameters();
            var parameterAttributes = method.GetAttributes<ParameterAttribute>();
            endpointItem.Parameters = parameterAttributes
                .Select(f => new ParameterItem(f.Name, f.Description))
                .ToList();

            foreach (var parameter in endpointItem.Parameters)
            {
                var methodParameter = methodParameters.FirstOrDefault(f => f.Name == parameter.Name);
                if (methodParameter == null)
                    continue;

                parameter.TypeName = methodParameter.ParameterType.GetName();
                if (!methodParameter.ParameterType.IsSystemType())
                    if (schemaTypes != null && !schemaTypes.Any(f => f.FullName == methodParameter.ParameterType.FullName))
                        schemaTypes.Add(methodParameter.ParameterType);
                var convertAttribute = methodParameter.GetCustomAttributes()
                    .Where(f => f.GetType().GetInterface(nameof(IParameterConverter)) != null)
                    .FirstOrDefault();
                if (convertAttribute != null)
                    parameter.PrefixType = convertAttribute.GetType().FullName;
            }

            var responseTypeAttributes = method.GetAttributes<ResponseTypeAttribute>();
            endpointItem.ResponseTypes = responseTypeAttributes
                .Select(f => new ResponseTypeItem(f.StatusCode, f.Type.GetName(), f.Description))
                .ToList();

            List<Type> responseGenericTypes = new List<Type>();
            foreach (var responseType in responseTypeAttributes)
                responseType.Type.GetGenericTypesRecursive(ref responseGenericTypes);

            schemaTypes.AddRange(responseGenericTypes.Where(f => schemaTypes.Any(y => y.FullName != f.FullName)).Select(f => f));

            return endpointItem;
        }

        /// <summary>
        /// Type convert to schema item
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static SchemaItem ToSchemaItem(this Type type)
        {
            SchemaItem schemaItem = new SchemaItem(type.FullName);
            var schemaDescriptionAttribute = type.GetAttributes<DescriptionAttribute>()
                .FirstOrDefault();
            if (schemaDescriptionAttribute != null)
                schemaItem.Description = schemaDescriptionAttribute.Description;
            schemaItem.SchemaProperties = type
                .GetProperties()
                .Select(f => new SchemaItem.SchemaPropertyItem()
                {
                    Name = f.Name,
                    Type = f.PropertyType.FullName,
                    Description = (f.GetCustomAttribute(typeof(DescriptionAttribute)) as DescriptionAttribute)?.Description
                }).ToList();
            return schemaItem;
        }

        public static List<Type> GetControllers()
        {
            List<Type> types = new List<Type>();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                var controllers = assembly
                    .GetTypes()
                    .Where(f => f.IsWebApiController())
                    .ToList();
                types.AddRange(controllers);
            }
            return types;
        }

        public static List<MethodInfo> GetEndpoints(Type controller)
        {
            List<MethodInfo> endpoints = new List<MethodInfo>();
            var methods = controller
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(f => f.IsRoute())
                .ToList();
            endpoints.AddRange(methods);
            return endpoints;
        }

        public static IDictionary<string,OpenApiSchema> GetOpenApiSchema(Type type)
        {
            IDictionary<string, OpenApiSchema> schema = new Dictionary<string, OpenApiSchema>();

            var properties = type.GetProperties();

            foreach (var property in properties)
            {
                OpenApiSchema openApiSchema = new OpenApiSchema();

                if(property.PropertyType == typeof(string) ||property.PropertyType == typeof(DateTime) || property.PropertyType == typeof(DateTime?))
                {
                    openApiSchema.Type = "string";
                }  
                else if(property.PropertyType == typeof(List<FilePart>) || property.PropertyType == typeof(FilePart))
                {
                    openApiSchema.Type = "binary";
                }
                else if(property.PropertyType == typeof(int) ||property.PropertyType == typeof(int?))
                {
                    openApiSchema.Type = "integer";
                }
                else if(property.PropertyType == typeof(float) || property.PropertyType == typeof(float? )||property.PropertyType == typeof(double) || property.PropertyType == typeof(double?) )
                {
                    openApiSchema.Type = "number";
                }
                else if(property.PropertyType == typeof(bool) || property.PropertyType == typeof(bool?) )
                {
                    openApiSchema.Type = "boolean";
                }    
                else if (property.IsArray())
                {
                    openApiSchema.Type = "array";
                }
                else
                {
                    openApiSchema.Type = "object";
                }

                schema.Add(property.Name, openApiSchema);
            }

            return schema;
        }
    }
}
