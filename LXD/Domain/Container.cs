﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace LXD.Domain
{
    public class Container : RemoteObject
    {
        public string Architecture;
        public Dictionary<string, string> Config;
        public DateTime CreatedAt;
        public Dictionary<string, Dictionary<string, string>> Devices;
        public bool Ephemaral;
        public Dictionary<string, string> ExpandedConfig;
        public Dictionary<string, Dictionary<string, string>> ExpandedDevices;
        public string Name;
        public string[] Profiles;
        public bool Stateful;
        public string Status;
        public int StatusCode;

        public JToken Start(int timeout = 30, bool stateful = false)
        {
            ContainerStatePut payload = new ContainerStatePut()
            {
                Action = "start",
                Timeout = timeout,
                Stateful = stateful,
            };

            JToken response = API.Put($"/{Client.Version}/containers/{Name}/state", payload);
            return API.WaitForOperationComplete(response);
        }

        public JToken Stop(int timeout = 30, bool force = false, bool stateful = false)
        {
            ContainerStatePut payload = new ContainerStatePut()
            {
                Action = "stop",
                Timeout = timeout,
                Force = force,
                Stateful = stateful,
            };

            JToken response = API.Put($"/{Client.Version}/containers/{Name}/state", payload);
            return API.WaitForOperationComplete(response);
        }


        public JToken Restart(int timeout = 30, bool force = false)
        {
            ContainerStatePut payload = new ContainerStatePut()
            {
                Action = "restart",
                Timeout = timeout,
                Force = force,
            };

            JToken response = API.Put($"/{Client.Version}/container/{Name}/state", payload);
            return API.WaitForOperationComplete(response);
        }

        public JToken Freeze(int timeout = 30)
        {
            ContainerStatePut payload = new ContainerStatePut()
            {
                Action = "freeze",
                Timeout = timeout,
            };

            JToken response = API.Put($"/{Client.Version}/container/{Name}/state", payload);
            return API.WaitForOperationComplete(response);
        }

        public JToken Unfreeze(int timeout = 30)
        {
            ContainerStatePut payload = new ContainerStatePut()
            {
                Action = "unfreeze",
                Timeout = timeout,
            };

            JToken response = API.Put($"/{Client.Version}/container/{Name}/state", payload);
            return API.WaitForOperationComplete(response);
        }

        public struct ContainerStatePut
        {
            public string Action;
            public int Timeout;
            public bool Force;
            public bool Stateful;
        }

        public IEnumerable<ClientWebSocket> Exec(string[] command,
            Dictionary<string, string> environment = null,
            bool waitForWebSocket = true,
            bool interactive = true,
            int width = 80,
            int height = 25)
        {
            ContainerExec exec = new ContainerExec()
            {
                Command = command,
                Environment = environment,
                WaitForWebSocket = waitForWebSocket,
                Interactive = interactive,
                Width = width,
                Height = height,
            };

            JToken response = API.Post($"/{Client.Version}/containers/{Name}/exec", exec);
            string operationUrl = response.Value<string>("operation");
            if (waitForWebSocket == false)
            {
                API.WaitForOperationComplete(response);
                // wait-for-websocket is false. Nothing to return.
                return null;
            }

            response = API.Get(operationUrl);

            int fdsN = interactive ? 1 : 3;

            List<Task<ClientWebSocket>> tasks = new List<Task<ClientWebSocket>>();
            for (int i = 0; i < fdsN; i++)
            {
                string fdsSecret = response.SelectToken($"metadata.metadata.fds.{i}").Value<string>();
                string wsUrl = $"{API.BaseUrlWebSocket}{operationUrl}/websocket?secret={fdsSecret}";
                Task<ClientWebSocket> task = ClientWebSocketExtensions.CreateAndConnectAsync(wsUrl);
                tasks.Add(task);
            }
            Task.WaitAll(tasks.ToArray());

            return tasks.Select(t => t.Result);
        }

        public struct ContainerExec
        {
            public string[] Command;
            public Dictionary<string, string> Environment;
            [JsonProperty("wait-for-websocket")]
            public bool WaitForWebSocket;
            public bool Interactive;
            public int Width;
            public int Height;
        }

        public string GetFile(string path)
        {
            IRestRequest request = new RestRequest($"/{Client.Version}/containers/{Name}/files");
            request.AddParameter("path", path);
            return "";
            //IRestResponse response = API.Execute(request);
            //return response.Content;
        }

        // TODO: fill body.
        public void PutFile(string path, byte[] content)
        {
            return;
        }

        public ContainerState State => API.Get<ContainerState>($"/{Client.Version}/containers/{Name}/state");
        public Collection<object> Logs => new Collection<object>(API, $"/{Client.Version}/containers/{Name}/logs");
        public Collection<Container> Snapshots => new Collection<Container>(API, $"/{Client.Version}/containers/{Name}/snapshots");
    }
}
