// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Processor.Telemetry {
    using Microsoft.Azure.IIoT.Services.Processor.Telemetry.Runtime;
    using Microsoft.Azure.IIoT.Messaging.EventHub.Services;
    using Microsoft.Azure.IIoT.Messaging.EventHub.Runtime;
    using Microsoft.Azure.IIoT.Messaging;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Services;
    using Microsoft.Azure.IIoT.OpcUa.Subscriber.Handlers;
    using Microsoft.Azure.IIoT.OpcUa.Subscriber.Processors;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Serializers;
    using Microsoft.Azure.IIoT.Hub.Processor.EventHub;
    using Microsoft.Azure.IIoT.Hub.Processor.Services;
    using Microsoft.Azure.IIoT.Hub.Services;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;
    using Autofac;
    using Serilog;
    using System;
    using System.IO;
    using System.Runtime.Loader;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// IoT Hub device telemetry event processor host.  Processes all
    /// telemetry from devices - forwards unknown telemetry on to
    /// time series event hub.
    /// </summary>
    public class Program {

        /// <summary>
        /// Main entry point for iot hub device event processor host
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args) {

            // Load hosting configuration
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true)
                .AddFromDotEnvFile()
                .AddEnvironmentVariables()
                .AddEnvironmentVariables(EnvironmentVariableTarget.User)
                .AddCommandLine(args)
                // Above configuration providers will provide connection
                // details for KeyVault configuration provider.
                .AddFromKeyVault(providerPriority: ConfigurationProviderPriority.Lowest)
                .Build();

            // Set up dependency injection for the event processor host
            RunAsync(config).Wait();
        }

        /// <summary>
        /// Run
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static async Task RunAsync(IConfiguration config) {
            var exit = false;
            while (!exit) {
                // Wait until the event processor host unloads or is cancelled
                var tcs = new TaskCompletionSource<bool>();
                AssemblyLoadContext.Default.Unloading += _ => tcs.TrySetResult(true);
                using (var container = ConfigureContainer(config).Build()) {
                    var logger = container.Resolve<ILogger>();
                    try {
                        logger.Information("Telemetry event processor host started.");
                        exit = await tcs.Task;
                    }
                    catch (InvalidConfigurationException e) {
                        logger.Error(e,
                            "Error starting telemetry event processor host - exit!");
                        return;
                    }
                    catch (Exception ex) {
                        logger.Error(ex,
                            "Error running telemetry event processor host - restarting!");
                    }
                }
            }
        }

        /// <summary>
        /// Autofac configuration.
        /// </summary>
        public static ContainerBuilder ConfigureContainer(
            IConfiguration configuration) {

            var serviceInfo = new ServiceInfo();
            var config = new Config(configuration);
            var builder = new ContainerBuilder();

            builder.RegisterInstance(serviceInfo)
                .AsImplementedInterfaces();

            // Register configuration interfaces
            builder.RegisterInstance(config)
                .AsImplementedInterfaces();
            builder.RegisterInstance(config.Configuration)
                .AsImplementedInterfaces();

            // Add Application Insights dependency tracking.
            builder.AddDependencyTracking(config, serviceInfo);

            // Add diagnostics
            builder.AddDiagnostics(config);

            builder.RegisterModule<NewtonSoftJsonModule>();

            // Event processor services
            builder.RegisterType<EventProcessorHost>()
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<EventProcessorFactory>()
                .AsImplementedInterfaces();
            // ... and auto start
            builder.RegisterType<HostAutoStart>()
                .AutoActivate()
                .AsImplementedInterfaces().SingleInstance();

            // Handle telemetry events
            builder.RegisterType<IoTHubDeviceEventHandler>()
                .AsImplementedInterfaces();

            builder.RegisterType<VariantEncoderFactory>()
                .AsImplementedInterfaces();

            // Handle opc-ua pub/sub subscriber messages
            builder.RegisterType<MonitoredItemSampleBinaryHandler>()
                .AsImplementedInterfaces();
            builder.RegisterType<MonitoredItemSampleJsonHandler>()
                .AsImplementedInterfaces();
            builder.RegisterType<PubSubNetworkMessageBinaryHandler>()
                .AsImplementedInterfaces();
            builder.RegisterType<PubSubNetworkMessageJsonHandler>()
                .AsImplementedInterfaces();

            // ... and forward result to secondary eventhub
            builder.RegisterType<MonitoredItemSampleForwarder>()
                .AsImplementedInterfaces();

            builder.RegisterType<EventHubNamespaceClient>()
                .AsImplementedInterfaces();
            builder.RegisterType<EventHubClientConfig>()
                .AsImplementedInterfaces();

            // ... forward unknown samples to the secondary eventhub
            builder.RegisterType<UnknownTelemetryForwarder>()
                .AsImplementedInterfaces();

            return builder;
        }

        /// <summary>
        /// Forwards telemetry not part of the platform for example from other devices
        /// </summary>
        internal sealed class UnknownTelemetryForwarder : IUnknownEventProcessor, IDisposable {

            /// <summary>
            /// Create forwarder
            /// </summary>
            /// <param name="queue"></param>
            public UnknownTelemetryForwarder(IEventQueueService queue) {
                if (queue == null) {
                    throw new ArgumentNullException(nameof(queue));
                }
                _client = queue.OpenAsync().Result;
            }

            /// <inheritdoc/>
            public void Dispose() {
                _client.Dispose();
            }

            /// <inheritdoc/>
            public Task HandleAsync(byte[] eventData, IDictionary<string, string> properties) {
                return _client.SendAsync(eventData, properties);
            }

            private readonly IEventQueueClient _client;
        }
    }
}
