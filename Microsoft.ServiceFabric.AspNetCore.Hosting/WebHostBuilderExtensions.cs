﻿using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ServiceFabric.AspNetCore.Hosting.Internal;
using System;
using System.Fabric;
using System.Linq;

namespace Microsoft.ServiceFabric.AspNetCore.Hosting
{
    public static class WebHostBuilderExtensions
    {
        public static IWebHostBuilder UseServiceFabric(this IWebHostBuilder webHostBuilder, ServiceFabricOptions options)
        {
            if (webHostBuilder == null)
            {
                throw new ArgumentNullException(nameof(webHostBuilder));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            //
            // Configure server URL.
            //
            var endpoint = FabricRuntime.GetActivationContext().GetEndpoint(options.EndpointName);

            string serverUrl = $"{endpoint.Protocol}://{FabricRuntime.GetNodeContext().IPAddressOrFQDN}:{endpoint.Port}";

            webHostBuilder.UseUrls(serverUrl);

            //
            // Configure services and middlewares.
            //
            webHostBuilder.ConfigureServices(services =>
            {
                if (options.ServiceDescriptions != null && options.ServiceDescriptions.Any())
                {
                    services.AddTransient<IStartupFilter, ServiceFabricStartupFilter>();

                    services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

                    foreach (var serviceDescription in options.ServiceDescriptions)
                    {
                        if (serviceDescription.ServiceType != null && serviceDescription.InterfaceTypes != null)
                        {
                            foreach (var interfaceType in serviceDescription.InterfaceTypes)
                            {
                                if (interfaceType != null)
                                {
                                    services.AddScoped(interfaceType, serviceProvider =>
                                    {
                                        var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();

                                        var feature = httpContextAccessor.HttpContext.Features.Get<ServiceFabricFeature>();

                                        var instanceOrReplica = feature?.InstanceOrReplica;

                                        return instanceOrReplica?.GetType() == serviceDescription.ServiceType ? instanceOrReplica : null;
                                    });
                                }
                            }
                        }
                    }
                }
            });

            return webHostBuilder;
        }
    }
}
