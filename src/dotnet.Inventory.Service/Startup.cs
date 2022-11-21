using System;
using System.Net.Http;
using dotnet.Common.HealthChecks;
using dotnet.Common.Identity;
using dotnet.Common.Logging;
using dotnet.Common.MassTransit;
using dotnet.Common.MongoDB;
using dotnet.Common.OpenTelemetry;
using dotnet.Inventory.Service.Clients;
using dotnet.Inventory.Service.Entities;
using dotnet.Inventory.Service.Exceptions;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Polly;
using Polly.Timeout;

namespace dotnet.Inventory.Service
{
    public class Startup
    {
        private const string AllowedOriginSettings = "AllowedOrigin";
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMongo()
                    .AddMongoRepository<InventoryItem>("inventoryitems")
                    .AddMongoRepository<CatalogItem>("catalogitems")
                    .AddMassTransitWithMessageBroker(Configuration, retryConfiurator =>
                    {
                        retryConfiurator.Interval(3, TimeSpan.FromSeconds(5));
                        retryConfiurator.Ignore(typeof(UnknownItemException));
                    })
                    .AddJwtBearerAuthentication();

            AddCatalogClient(services);

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "dotnet.Inventory.Service", Version = "v1" });
            });

            services.AddHealthChecks()
                    .AddMongoDb();

            services.AddSeqLogging(Configuration)
                    .AddTracing(Configuration)
                    .AddMetrics(Configuration);
        }


        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "dotnet.Inventory.Service v1"));

                app.UseCors(builder =>
               {
                   builder.WithOrigins(Configuration[AllowedOriginSettings])
                   .AllowAnyHeader()
                   .AllowAnyMethod();
               });

            }

            app.UseOpenTelemetryPrometheusScrapingEndpoint();
            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapDotnetEconomyHealthChecks();
            });
        }

        private static void AddCatalogClient(IServiceCollection services)
        {
            Random jit = new Random();

            services.AddHttpClient<CatalogClient>(client =>
            {
                client.BaseAddress = new System.Uri("https://localhost:5001");
            })
            //Implementing retries with exponential backoff
            .AddTransientHttpErrorPolicy(builder => builder.Or<TimeoutRejectedException>().WaitAndRetryAsync(
                5,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                                + TimeSpan.FromMilliseconds(jit.Next(0, 1000)),
                //Development mode
                onRetry: (outcome, timespan, retryAttempt) =>
                {
                    var serviceProvider = services.BuildServiceProvider();
                    serviceProvider.GetService<ILogger<CatalogClient>>()?
                    .LogWarning($"Delaying for {timespan.TotalSeconds} seconds, then making attempt {retryAttempt}");
                }
            ))
            //Implementing the circuit breaker pattern
            .AddTransientHttpErrorPolicy(builder => builder.Or<TimeoutRejectedException>().CircuitBreakerAsync(
                3,
                TimeSpan.FromSeconds(15),
                onBreak: (outCome, timespan) =>
                {
                    //Development mode
                    var serviceProvider = services.BuildServiceProvider();
                    serviceProvider.GetService<ILogger<CatalogClient>>()?
                    .LogWarning($"Opening the circuit for {timespan.TotalSeconds} seconds...");

                },
                onReset: () =>
                {
                    var serviceProvider = services.BuildServiceProvider();
                    serviceProvider.GetService<ILogger<CatalogClient>>()?
                    .LogWarning("Closing the circuit...");

                }
                ))

            // Implementing a timeout policy via Polly
            .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(1));
        }

    }
}
