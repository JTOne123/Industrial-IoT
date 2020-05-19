﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Agent {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;

    /// <summary>
    /// Provides configuration
    /// </summary>
    public class AgentConfigProvider : IAgentConfigProvider {

        /// <summary>
        /// Create provider
        /// </summary>
        /// <param name="config"></param>
        public AgentConfigProvider(PublisherConfigModel config) {
            Config = config;
        }

        /// <inheritdoc/>
        public PublisherConfigModel Config { get; }

        /// <inheritdoc/>
#pragma warning disable 0067
        public event ConfigUpdatedEventHandler OnConfigUpdated;
#pragma warning restore 0067
    }
}