using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using Grpc.Net.Client;

namespace ByondLang.State
{
    public class Program : IDisposable
    {
        Process process;
        GrpcChannel channel;
        Api.Program.ProgramClient client;
        Api.StatusResponse lastStatus;

        public async Task ExecuteScript(string script)
        {
            await client.executeAsync(new Api.ExecuteRequest()
            {
                Code = script
            });
        }

        public async Task InitializeProgram(Api.ProgramType type)
        {
            lastStatus = await client.statusAsync(new Api.StatusRequest()
            {
                Initialize = true,
                Type = type,
            });
        }

        public async Task<string> GetBuffer()
        {
            lastStatus = await client.statusAsync(new Api.StatusRequest());
            return lastStatus?.Terminal?.Buffer;
        }

        public void Dispose()
        {
            process?.Kill();
            process?.Dispose();
            channel?.Dispose();
        }

        internal void Spawn(int port)
        {
            var mp = Process.GetCurrentProcess();

            var startInfo = new ProcessStartInfo(mp.MainModule.FileName, "--worker");
            startInfo.EnvironmentVariables["ASPNETCORE_URLS"] = $"https://localhost:{port}";
            startInfo.UseShellExecute = false;
            process = Process.Start(startInfo);

            channel = GrpcChannel.ForAddress($"https://localhost:{port}");

            client = new Api.Program.ProgramClient(channel);
        }

        // Computer only
        internal async Task HandleTopic(string topic, string data)
        {
            await client.handleTopicAsync(new Api.TopicRequest() { 
                TopicId = topic,
                Data = data
            });;
        }
    }
}
