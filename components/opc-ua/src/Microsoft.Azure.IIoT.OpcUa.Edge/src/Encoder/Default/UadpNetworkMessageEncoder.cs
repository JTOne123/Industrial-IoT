// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Services {
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Core;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Opc.Ua;
    using Opc.Ua.Extensions;
    using Opc.Ua.PubSub;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    /// <summary>
    /// Creates pub/sub encoded messages
    /// </summary>
    public class UadpNetworkMessageEncoder : INetworkMessageEncoder {

        /// <inheritdoc/>
        public string MessageScheme => MessageSchemaTypes.NetworkMessageUadp;

        /// <inheritdoc/>
        public IEnumerable<NetworkMessageModel> EncodeBatch(
            IEnumerable<DataSetMessageModel> messages, int maxMessageSize) {

            var notifications = GetNetworkMessages(messages);
            if (notifications.Count() == 0) {
                yield break;
            }
            var encodingContext = messages.First().ServiceMessageContext;
            var current = notifications.GetEnumerator();
            var processing = current.MoveNext();
            var messageSize = 4; // array length size
            maxMessageSize -= 2048; // reserve 2k for header
            var chunk = new Collection<NetworkMessage>();
            while (processing) {
                var notification = current.Current;
                var messageCompleted = false;
                if (notification != null) {
                    var helperEncoder = new BinaryEncoder(encodingContext);
                    helperEncoder.WriteEncodeable(null, notification);
                    var notificationSize = helperEncoder.CloseAndReturnBuffer().Length;
                    messageCompleted = maxMessageSize < (messageSize + notificationSize);

                    if (!messageCompleted) {
                        chunk.Add(notification);
                        processing = current.MoveNext();
                        messageSize += notificationSize;
                    }
                }
                if (!processing || messageCompleted) {
                    var encoder = new BinaryEncoder(encodingContext);
                    encoder.WriteBoolean(null, true); // is Batch
                    encoder.WriteEncodeableArray(null, chunk);
                    chunk.Clear();
                    messageSize = 4;
                    var encoded = new NetworkMessageModel {
                        Body = encoder.CloseAndReturnBuffer(),
                        Timestamp = DateTime.UtcNow,
                        ContentType = ContentMimeType.Uadp,
                        MessageSchema = MessageSchemaTypes.NetworkMessageUadp
                    };
                    yield return encoded;
                }
            }
        }

        /// <inheritdoc/>
        public IEnumerable<NetworkMessageModel> Encode(
            IEnumerable<DataSetMessageModel> messages) {
            var notifications = GetNetworkMessages(messages);
            if (notifications.Count() == 0) {
                yield break;
            }
            var encodingContext = messages.First().ServiceMessageContext;
            foreach (var networkMessage in notifications) {
                var encoder = new BinaryEncoder(encodingContext);
                encoder.WriteBoolean(null, false); // is not Batch
                encoder.WriteEncodeable(null, networkMessage);
                networkMessage.Encode(encoder);
                var encoded = new NetworkMessageModel {
                    Body = encoder.CloseAndReturnBuffer(),
                    Timestamp = DateTime.UtcNow,
                    ContentType = ContentMimeType.Uadp,
                    MessageSchema = MessageSchemaTypes.NetworkMessageUadp
                };
                yield return encoded;
            }
        }

        /// <summary>
        /// Produce network messages from the data set message model
        /// </summary>
        /// <param name="messages"></param>
        /// <returns></returns>
        private IEnumerable<NetworkMessage> GetNetworkMessages(
            IEnumerable<DataSetMessageModel> messages) {

            // TODO: Honor single message
            // TODO: Group by writer
            foreach (var message in messages) {
                var networkMessage = new NetworkMessage() {
                    MessageContentMask = ((NetworkMessageContentMask?)message.ContentMask)
                        .ToStackType(NetworkMessageType.Uadp),
                    PublisherId = message.PublisherId,
                    DataSetClassId = message.Writer?.DataSet?
                        .DataSetMetaData?.DataSetClassId.ToString(),
                    MessageId = message.SequenceNumber.ToString()
                };
                var notificationQueues = message.Notifications.GroupBy(m => m.NodeId)
                    .Select(c => new Queue<MonitoredItemNotificationModel>(c.ToArray())).ToArray();
                while (notificationQueues.Where(q => q.Any()).Any()) {
                    var payload = notificationQueues
                        .Select(q => q.Any() ? q.Dequeue() : null)
                            .Where(s => s != null)
                                .ToDictionary(
                                    s => s.NodeId.ToExpandedNodeId(message.ServiceMessageContext.NamespaceUris)
                                        .AsString(message.ServiceMessageContext),
                                    s => s.Value);
                    var dataSetMessage = new DataSetMessage {
                        DataSetWriterId = message.Writer.DataSetWriterId,
                        MetaDataVersion = new ConfigurationVersionDataType {
                            MajorVersion = message.Writer?.DataSet?.DataSetMetaData?
                                .ConfigurationVersion?.MajorVersion ?? 1,
                            MinorVersion = message.Writer?.DataSet?.DataSetMetaData?
                                .ConfigurationVersion?.MinorVersion ?? 0
                        },
                        MessageContentMask = (message.Writer?.MessageSettings?.DataSetMessageContentMask)
                            .ToStackType(NetworkMessageType.Uadp),
                        Timestamp = message.TimeStamp ?? DateTime.UtcNow,
                        SequenceNumber = message.SequenceNumber,
                        Status = payload.Values.Any(s => StatusCode.IsNotGood(s.StatusCode)) ?
                            StatusCodes.Bad : StatusCodes.Good,
                        Payload = new DataSet(payload, (uint)message.Writer?.DataSetFieldContentMask.ToStackType())
                    };
                    networkMessage.Messages.Add(dataSetMessage);
                }
                yield return networkMessage;
            }
        }
    }
}