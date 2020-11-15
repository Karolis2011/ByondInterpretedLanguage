using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using Grpc.Net.Client;
using System.Net.Http;

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
            process?.Kill(true);
            process?.Dispose();
            channel?.Dispose();
        }

        internal void Start(Func<int> portGenerator)
        {
            if(process != null)
            {
                if(process.HasExited)
                {
                    channel.Dispose();
                    spawn(portGenerator());
                }
            } else
            {
                spawn(portGenerator());
            }
        }

        private void spawn(int port)
        {
            var mp = Process.GetCurrentProcess();

            var startInfo = new ProcessStartInfo(mp.MainModule.FileName, "--worker");
            startInfo.EnvironmentVariables["ASPNETCORE_URLS"] = $"https://localhost:{port}";
            startInfo.UseShellExecute = false;
            process = Process.Start(startInfo);

            var httpHandler = new HttpClientHandler();
            httpHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

            channel = GrpcChannel.ForAddress($"https://localhost:{port}", new GrpcChannelOptions() { 
                HttpHandler = httpHandler
            });

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

        internal async Task Recycle()
        {
            await client.recycleAsync(new Api.VoidMessage());
            lastStatus = null;
        }
    }
}
