﻿using System;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Clockwise;
using FluentAssertions;
using FluentAssertions.Extensions;
using Pocket;
using Recipes;
using WorkspaceServer.Models.Execution;
using WorkspaceServer.Servers.Dotnet;
using WorkspaceServer.WorkspaceFeatures;
using Xunit;
using Xunit.Abstractions;

namespace WorkspaceServer.Tests
{
    public class DotnetWorkspaceServerAspNetProjectTests : IDisposable
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        public DotnetWorkspaceServerAspNetProjectTests(ITestOutputHelper output)
        {
            _disposables.Add(output.SubscribeToPocketLogger());
        }

        public void Dispose() => _disposables.Dispose();

        [Fact]
        public async Task Run_starts_the_kestrel_server_and_provides_a_WebServer_feature_that_can_receive_requests()
        {
            var workspaceServer = await GetWorkspaceServer();

            var workspace = Workspace.FromDirectory((await Default.WebApiWorkspace).Directory, "aspnet.webapi");

            using (var runResult = await workspaceServer.Run(workspace))
            {
                var webServer = runResult.GetFeature<WebServer>();

                var response = await webServer.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/api/values")).CancelIfExceeds(new TimeBudget(35.Seconds()));

                var result = await response.EnsureSuccess()
                                           .DeserializeAs<string[]>();

                result.Should().Equal("value1", "value2");
            }
        }
        
        protected async Task<IWorkspaceServer> GetWorkspaceServer(
            [CallerMemberName] string testName = null)
        {
            var project = await Create.WebApiWorkspace(testName);

            var workspaceServer = new DotnetWorkspaceServer(project, 45);

            await workspaceServer.EnsureInitializedAndNotDisposed();

            return workspaceServer;
        }
    }
}