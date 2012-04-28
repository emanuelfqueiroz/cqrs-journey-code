﻿// ==============================================================================================================
// Microsoft patterns & practices
// CQRS Journey project
// ==============================================================================================================
// ©2012 Microsoft. All rights reserved. Certain content used with permission from contributors
// http://cqrsjourney.github.com/contributors/members
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance 
// with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is 
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and limitations under the License.
// ==============================================================================================================

namespace Infrastructure.Azure.Tests.EventSourcing
{
    using System;
    using System.Linq;
    using System.Threading;
    using Infrastructure.Azure.EventSourcing;
    using Infrastructure.Azure.Tests.EventSourcing.Mocks;
    using Moq;
    using Xunit;

    public class EventStoreBusPublisherFixture
    {
        [Fact]
        public void when_calling_publish_then_gets_unpublished_events_and_sends_them_and_deletes_them()
        {
            string partitionKey = Guid.NewGuid().ToString();
            string version = "0001";
            string rowKey = "Unpublished_" + version;
            var testEvent = Mock.Of<IEventRecord>(x => x.PartitionKey == partitionKey && x.RowKey == rowKey  && x.EventType == "TestType" && x.Payload == "serialized event");
            var queue = new Mock<IPendingEventsQueue>();
            queue.Setup(x => x.GetPending(partitionKey)).Returns(new[] { testEvent });
            var sender = new MessageSenderMock();
            var sut = new EventStoreBusPublisher(sender, queue.Object);
            var cancellationTokenSource = new CancellationTokenSource();
            sut.Start(cancellationTokenSource.Token);
            
            sut.SendAsync(partitionKey);

            Assert.True(sender.ResetEvent.WaitOne(3000));
            cancellationTokenSource.Cancel();
            string expectedMessageId = string.Format("{0}_{1}", partitionKey, version);

            Assert.Equal(expectedMessageId, sender.Sent.Single().MessageId);
            queue.Verify(q => q.Delete(partitionKey, rowKey));
        }
    }
}