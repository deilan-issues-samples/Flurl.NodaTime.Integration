using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Carter;
using Carter.ModelBinding;
using Carter.Request;
using Carter.Response;
using Flurl.NodaTime.Integration.Common;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NodaTime;
using NodaTime.Serialization.JsonNet;

namespace Flurl.NodaTime.Integration.Server
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCarter(configurator: configurator =>
            {
                configurator.WithResponseNegotiator<MyNewtonsoftJsonResponseNegotiator>();
                configurator.WithModelBinder<MyNewtonsoftJsonModelBinder>();
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapCarter();
                endpoints.Map("/", async context =>
                {
                    var result = JsonConvert.SerializeObject(ActorsModule.Actors);
                    await context.Response.WriteAsync(result);
                });
            });
        }
    }

    public class ActorsModule : CarterModule
    {
        public static readonly Actor[] Actors = new Actor[]
        {
            new Actor
            {
                Id = "1",
                Date = LocalDate.FromDateTime(DateTime.Now)
            }
        };
        public ActorsModule()
        {
            Get("/actors", async (req, res) =>
            {
                var q = req.Query.As<LocalDate?>("date");
                await res.Negotiate(Actors);
            });

            Get("/actors/{date}", async (req, res) =>
            {
                var date = req.RouteValues.As<LocalDate>("date");
                await res.Negotiate(new [] {new { Date = date }});
            });

            Post("/actors", async (req, res) =>
            {
                var result = await req.Bind<Actor[]>();
                await res.Negotiate(result);
            });

        }
    }

    public sealed class MyNewtonsoftJsonModelBinder : ModelBinderBase
    {
        private readonly JsonSerializer _jsonSerializer;

        public MyNewtonsoftJsonModelBinder(ILoggerFactory loggerFactory)
            : base(loggerFactory)
        {
            var settings = new JsonSerializerSettings();
            //settings.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
            _jsonSerializer = JsonSerializer.Create(settings);
        }

        protected override Task<T> BindCore<T>(HttpRequest request)
        {
            var syncIOFeature = request.HttpContext.Features.Get<IHttpBodyControlFeature>();
            if (syncIOFeature != null)
            {
                syncIOFeature.AllowSynchronousIO = true;
            }

            using var streamReader = new StreamReader(request.Body);
            using var jsonTextReader = new JsonTextReader(streamReader);

            var result = _jsonSerializer.Deserialize<T>(jsonTextReader);
            return Task.FromResult(result);
        }
    }
    public class MyNewtonsoftJsonResponseNegotiator : IResponseNegotiator
    {
        private readonly JsonSerializerSettings jsonSettings;

        public MyNewtonsoftJsonResponseNegotiator()
        {
            var contractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            };
            this.jsonSettings = new JsonSerializerSettings { ContractResolver = contractResolver, NullValueHandling = NullValueHandling.Ignore };
            //jsonSettings.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
        }
        public bool CanHandle(MediaTypeHeaderValue accept)
        {
            return accept.MediaType.ToString().IndexOf("json", StringComparison.OrdinalIgnoreCase) >= 0;
        }
        public Task Handle(HttpRequest req, HttpResponse res, object model, CancellationToken cancellationToken)
        {
            res.ContentType = "application/json; charset=utf-8";
            var result = JsonConvert.SerializeObject(model, this.jsonSettings);
            return res.WriteAsync(result, cancellationToken);
        }
    }
}
