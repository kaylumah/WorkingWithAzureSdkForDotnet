// Copyright (c) Kaylumah, 2022. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using FluentAssertions;

namespace Test.Integration;

public static partial class ServiceBusClientTestExtensions
{
    public static async Task RunScenario(this ServiceBusClient client, string queueName, string scenarioName)
    {
        var sender = client.CreateSender(queueName);
        var receiver = client.CreateReceiver(queueName);

        var message = $"{scenarioName}-{DateTimeOffset.Now:s}";
        await sender.SendMessageAsync(new ServiceBusMessage(message));
        var receivedMessage = await receiver.ReceiveMessageAsync();

        receivedMessage.Body.ToString().Should().Be(message);
        await Task.Delay(TimeSpan.FromSeconds(35));
    }
}
