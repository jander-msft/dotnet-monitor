using Microsoft.Diagnostics.Monitoring.UnitTests.HttpApi;
using Microsoft.Diagnostics.Monitoring.UnitTests.Options;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.UnitTests.Runners
{
    internal static class TestExecutor
    {
        public static async Task SingleAppAsync(
            ITestOutputHelper outputHelper,
            IHttpClientFactory clientFactory,
            DiagnosticPortConnectionMode monitorMode,
            string scenarionName,
            Func<ApiClient, AppRunner, Task> executeFunc,
            Action<AppRunner> preExecuteAction = null,
            Func<ApiClient, Task> postExecuteFunc = null)
        {
            DiagnosticPortHelper.Generate(
                monitorMode,
                out DiagnosticPortConnectionMode appConnectionMode,
                out string diagnosticPortPath);

            await using MonitorRunner toolRunner = new(outputHelper);
            toolRunner.ConnectionMode = monitorMode;
            toolRunner.DiagnosticPortPath = diagnosticPortPath;
            toolRunner.DisableAuthentication = true;
            await toolRunner.StartAsync();

            using HttpClient httpClient = await toolRunner.CreateHttpClientDefaultAddressAsync(clientFactory);
            ApiClient apiClient = new(outputHelper, httpClient);

            AppRunner appRunner = new(outputHelper);
            appRunner.ConnectionMode = appConnectionMode;
            appRunner.DiagnosticPortPath = diagnosticPortPath;
            appRunner.ScenarioName = scenarionName;

            if (null != preExecuteAction)
            {
                preExecuteAction(appRunner);
            }

            await appRunner.ExecuteAsync(() => executeFunc(apiClient, appRunner));

            Assert.Equal(0, appRunner.ExitCode);

            if (null != postExecuteFunc)
            {
                await postExecuteFunc(apiClient);
            }
        }
    }
}
