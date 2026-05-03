using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Persistly.Unity;

namespace Persistly.Unity.LastBeacon.Tests
{
    public sealed class PersistlyClientTests
    {
        [Test]
        public async Task CreateSaveStoresCanonicalPayloadInCache()
        {
            var transport = new StubTransport(201, "{\"save\":{\"saveId\":\"sv_01\",\"externalUserId\":\"auth0|player\",\"metadata\":{\"characterName\":\"Ayla\"},\"state\":{\"Scrap\":12,\"Workers\":1},\"version\":1,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:00:00Z\"}}");
            var client = BuildClient(transport);

            var created = await client.CreateSaveAsync(new PersistlyCreateSaveRequest("{\"Scrap\":12,\"Workers\":1}", "{\"characterName\":\"Ayla\"}", "auth0|player"));

            Assert.That(created.SaveId, Is.EqualTo("sv_01"));
            Assert.That(created.Version, Is.EqualTo(1));
            Assert.That(created.MetadataJson, Does.Contain("Ayla"));
            Assert.That(client.TryGetLocal("sv_01", out var cached), Is.True);
            Assert.That(cached.StateJson, Does.Contain("\"Scrap\":12"));
        }

        [Test]
        public async Task ConflictSyncReturnsCanonicalRemoteSave()
        {
            var transport = new StubTransport(
                409,
                "{\"status\":\"conflict\",\"save\":{\"saveId\":\"sv_01\",\"externalUserId\":\"auth0|player\",\"metadata\":{\"characterName\":\"Ayla\"},\"state\":{\"Scrap\":77,\"Workers\":3},\"version\":5,\"createdAt\":\"2026-04-10T00:00:00Z\",\"updatedAt\":\"2026-04-10T00:05:00Z\"},\"details\":{\"reason\":\"base_version_mismatch\"}}");
            var client = BuildClient(transport);

            var result = await client.SyncSaveAsync("sv_01", new PersistlySyncSaveRequest("{\"Scrap\":14}", 4, "{\"characterName\":\"Ayla\"}"));

            Assert.That(result.Status, Is.EqualTo(PersistlySyncStatus.Conflict));
            Assert.That(result.Save.Version, Is.EqualTo(5));
            Assert.That(result.Save.StateJson, Does.Contain("\"Scrap\":77"));
            Assert.That(client.TryGetLocal("sv_01", out var cached), Is.True);
            Assert.That(cached.Version, Is.EqualTo(5));
        }

        private static PersistlyClient BuildClient(IPersistlyTransport transport)
        {
            return new PersistlyClient(new PersistlyClientOptions("http://127.0.0.1:8080", "ps_test_example")
            {
                Transport = transport,
            });
        }

        private sealed class StubTransport : IPersistlyTransport
        {
            private readonly PersistlyTransportResponse _response;

            public StubTransport(int statusCode, string body)
            {
                _response = new PersistlyTransportResponse(statusCode, body);
            }

            public Task<PersistlyTransportResponse> SendAsync(PersistlyTransportRequest request, CancellationToken cancellationToken)
            {
                return Task.FromResult(_response);
            }
        }
    }
}
