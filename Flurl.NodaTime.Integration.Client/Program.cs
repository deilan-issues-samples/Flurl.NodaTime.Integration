using System;
using Flurl.Http;
using Flurl.Http.Configuration;
using Flurl.NodaTime.Integration.Common;
using Newtonsoft.Json;
using NodaTime;
using NodaTime.Serialization.JsonNet;

namespace Flurl.NodaTime.Integration.Client
{
    class Program
    {
        private static readonly Actor[] Actors = new Actor[]
        {
            new Actor
            {
                Id = "2",
                Date = LocalDate.FromDateTime(DateTime.Now).PlusDays(1)
            }
        };
        static void Main(string[] args)
        {
            //FlurlHttp.Configure(settings =>
            //{
            //    var serializerSettings = new JsonSerializerSettings();
            //    serializerSettings.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
            //    settings.JsonSerializer = new NewtonsoftJsonSerializer(serializerSettings);
            //});
            var url = "http://localhost:5000";
            try
            {
                var response = url.AppendPathSegment("actors")
                    .SetQueryParam("date", "2021-03-03")
                    //.AppendPathSegment( "2021-03-03")
                    .GetAsync().GetAwaiter().GetResult();
                //var body = response.GetStringAsync().GetAwaiter().GetResult();
                var result = response.GetJsonAsync<Actor[]>().GetAwaiter().GetResult();
                var response2 = url.AppendPathSegment("actors")
                    .PostJsonAsync(Actors).GetAwaiter().GetResult();
                //var body2 = response.GetStringAsync().GetAwaiter().GetResult();
                var result2 = response2.GetJsonAsync<Actor[]>().GetAwaiter().GetResult();
            }
            catch (FlurlHttpException e)
            {
                var body = e.Call.Response.GetStringAsync().GetAwaiter().GetResult();
            }
            Console.ReadKey();
        }
    }
}
