// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Common.Jobs.Edge.Runtime {
    using Microsoft.Azure.IIoT.Services.Cors;
    using Microsoft.Azure.IIoT.Services.Cors.Runtime;
    using Microsoft.Azure.IIoT.Services.Swagger;
    using Microsoft.Azure.IIoT.Services.Swagger.Runtime;
    using Microsoft.Azure.IIoT.Agent.Framework;
    using Microsoft.Azure.IIoT.Agent.Framework.Jobs;
    using Microsoft.Azure.IIoT.Agent.Framework.Jobs.Runtime;
    using Microsoft.Azure.IIoT.Agent.Framework.Storage.Database;
    using Microsoft.Azure.IIoT.Auth.Clients;
    using Microsoft.Azure.IIoT.Auth.Runtime;
    using Microsoft.Azure.IIoT.Auth.Server;
    using Microsoft.Azure.IIoT.Diagnostics;
    using Microsoft.Azure.IIoT.Hub.Client;
    using Microsoft.Azure.IIoT.Hub.Client.Runtime;
    using Microsoft.Azure.IIoT.Storage.CosmosDb;
    using Microsoft.Azure.IIoT.Storage.CosmosDb.Runtime;
    using Microsoft.Extensions.Configuration;
    using System;

    /// <summary>
    /// Common web service configuration aggregation
    /// </summary>
    public class Config : DiagnosticsConfig, IAuthConfig, IIoTHubConfig,
        ICorsConfig, IClientConfig, ISwaggerConfig, IJobOrchestratorConfig,
        ICosmosDbConfig, IJobDatabaseConfig, IWorkerDatabaseConfig,
        IJobOrchestratorEndpoint {

        /// <inheritdoc/>
        public string DbConnectionString => _cosmos.DbConnectionString;
        /// <inheritdoc/>
        public int? ThroughputUnits => _cosmos.ThroughputUnits;
        /// <inheritdoc/>
        public string ContainerName => "iiot_opc";
        /// <inheritdoc/>
        public string DatabaseName => "iiot_opc";

        /// <inheritdoc/>
        public string IoTHubConnString => _hub.IoTHubConnString;
        /// <inheritdoc/>
        public string IoTHubResourceId => _hub.IoTHubResourceId;

        /// <inheritdoc/>
        public string AppId => _auth.AppId;
        /// <inheritdoc/>
        public string AppSecret => _auth.AppSecret;
        /// <inheritdoc/>
        public string TenantId => _auth.TenantId;
        /// <inheritdoc/>
        public string InstanceUrl => _auth.InstanceUrl;
        /// <inheritdoc/>
        public string Audience => _auth.Audience;
        /// <inheritdoc/>
        public bool AuthRequired => _auth.AuthRequired;
        /// <inheritdoc/>
        public int HttpsRedirectPort => _auth.HttpsRedirectPort;
        /// <inheritdoc/>
        public string TrustedIssuer => _auth.TrustedIssuer;
        /// <inheritdoc/>
        public TimeSpan AllowedClockSkew => _auth.AllowedClockSkew;

        /// <inheritdoc/>
        public TimeSpan JobStaleTime => _jobs.JobStaleTime;
        /// <inheritdoc/>
        public string JobOrchestratorUrl => _edge.JobOrchestratorUrl;

        /// <inheritdoc/>
        public string CorsWhitelist => _cors.CorsWhitelist;
        /// <inheritdoc/>
        public bool CorsEnabled => _cors.CorsEnabled;

        /// <inheritdoc/>
        public bool UIEnabled => _swagger.UIEnabled;
        /// <inheritdoc/>
        public bool WithAuth => _swagger.WithAuth;
        /// <inheritdoc/>
        public bool WithHttpScheme => _swagger.WithHttpScheme;
        /// <inheritdoc/>
        public string SwaggerAppId => _swagger.SwaggerAppId;
        /// <inheritdoc/>
        public string SwaggerAppSecret => _swagger.SwaggerAppSecret;


        /// <summary>
        /// Whether to use role based access
        /// </summary>
        public bool UseRoles => GetBoolOrDefault("PCS_AUTH_ROLES");


        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public Config(IConfiguration configuration) :
            base(configuration) {

            _swagger = new SwaggerConfig(configuration);
            _auth = new AuthConfig(configuration);
            _hub = new IoTHubConfig(configuration);
            _cors = new CorsConfig(configuration);
            _cosmos = new CosmosDbConfig(configuration);
            _jobs = new JobOrchestratorConfig(configuration);
            _edge = new JobOrchestratorApiConfig(configuration);
        }

        private readonly SwaggerConfig _swagger;
        private readonly AuthConfig _auth;
        private readonly CorsConfig _cors;
        private readonly CosmosDbConfig _cosmos;
        private readonly JobOrchestratorConfig _jobs;
        private readonly JobOrchestratorApiConfig _edge;
        private readonly IoTHubConfig _hub;
    }
}