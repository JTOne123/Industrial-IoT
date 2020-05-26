﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using System;

    /// <summary>
    /// Handle config update
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="eventArgs"></param>

    public delegate void ConfigUpdatedEventHandler(object sender, EventArgs eventArgs);

    /// <summary>
    /// Agent provider
    /// </summary>
    public interface IAgentConfigProvider {

        /// <summary>
        /// Agent Configuration
        /// </summary>
        PublisherConfigModel Config { get; }

        /// <summary>
        /// Configuration change events
        /// </summary>
        event ConfigUpdatedEventHandler OnConfigUpdated;
    }
}