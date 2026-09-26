// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using Xunit;
using Zerra.CQRS.Network;
using Zerra.Web;

namespace Zerra.Test.Web
{
    public class KestrelCqrsServerLinkedSettingsTests
    {
        [Fact]
        public void Constructor_SetsValues_AllowsAllOrigins()
        {
            using var settings = new KestrelCqrsServerLinkedSettings("/cqrs", null, ContentType.Json);

            Assert.Equal("/cqrs", settings.Route);
            Assert.Null(settings.Authorizer);
            Assert.Equal(ContentType.Json, settings.ContentType);
            Assert.Null(settings.AllowOrigins);
            Assert.Equal("*", settings.AllowOriginsString);
            Assert.Empty(settings.Types);
        }

        [Fact]
        public void AllowOrigins_Values_JoinsHeaderValue()
        {
            using var settings = new KestrelCqrsServerLinkedSettings(null, null, ContentType.Bytes) { AllowOrigins = ["https://app.example.com", "example.com"] };

            Assert.Equal(["https://app.example.com", "example.com"], settings.AllowOrigins);
            Assert.Equal("https://app.example.com, example.com", settings.AllowOriginsString);
        }

        //an empty list would otherwise block every origin, it means no restriction the same as null
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void AllowOrigins_NullOrEmpty_AllowsAllOrigins(bool empty)
        {
            using var settings = new KestrelCqrsServerLinkedSettings(null, null, ContentType.Bytes) { AllowOrigins = ["example.com"] };

            settings.AllowOrigins = empty ? [] : null;

            Assert.Null(settings.AllowOrigins);
            Assert.Equal("*", settings.AllowOriginsString);
        }

        [Fact]
        public void Dispose_ClearsTypesAndLeavesThrottlesForRunningRequests()
        {
            var settings = new KestrelCqrsServerLinkedSettings(null, null, ContentType.Bytes);
            var throttle = new SemaphoreSlim(1, 1);
            Assert.True(settings.Types.TryAdd(typeof(string), throttle));
            Assert.True(throttle.Wait(0, TestContext.Current.CancellationToken)); //a request in progress

            settings.Dispose();

            Assert.Empty(settings.Types);
            _ = throttle.Release(); //the request finishing after dispose doesn't throw
        }
    }
}
