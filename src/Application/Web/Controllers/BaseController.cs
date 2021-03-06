﻿using System;
using System.Fabric;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Web.Controllers
{
    public abstract class BaseController : Controller
    {
        private static readonly JsonSerializer serializer = new JsonSerializer
        {
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore
        };

        private static int index = 0;

        protected string GetBasketId()
        {
            if (!Request.Cookies.TryGetValue("Basket", out var basketId))
            {
                basketId = $"{Guid.NewGuid()}";
                Response.Cookies.Append("Basket", basketId);
            }

            return basketId;
        }

        protected void ClearBasketId()
        {
            Response.Cookies.Delete("Basket");
        }

        protected StringContent GetJsonContent<T>(T obj)
        {
            var content = new StringContent(JsonConvert.SerializeObject(obj));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return content;
        }

        protected async Task<Uri> GetServiceUriAsync(string application, string service, string path, Func<ServicePartitionKey> partitionKeyGenerator = null)
        {
            var resolver = ServicePartitionResolver.GetDefault();
            var partition = await resolver.ResolveAsync(
                new Uri($"fabric:/{application}/{service}"),
                partitionKeyGenerator?.Invoke() ?? new ServicePartitionKey(),
                CancellationToken.None);

            var endpoints = partition.Endpoints
                .Where(ep => ep.Role == ServiceEndpointRole.Stateless || ep.Role == ServiceEndpointRole.StatefulPrimary)
                .Select(ep => ep.Address)
                .ToArray();

            var idx = Interlocked.Increment(ref index) % endpoints.Length;
            var endpoint = endpoints[idx];

            var address = JObject.Parse(endpoint)
                .SelectToken("Endpoints")
                .ToObject<JObject>()
                .Properties()
                .First()
                .Value.Value<string>();

            var uri = new Uri($"{address}{path}");
            return uri;
        }

        protected async Task<T> DeserializeResponseAsync<T>(HttpResponseMessage message)
        {
            using (var stream = await message.Content.ReadAsStreamAsync())
            using (var reader = new StreamReader(stream))
            using (var json = new JsonTextReader(reader))
            {
                return serializer.Deserialize<T>(json);
            }
        }
    }
}
