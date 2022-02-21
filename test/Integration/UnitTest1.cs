// Copyright (c) Kaylumah, 2022. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using FluentAssertions;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Test.Integration;

public class UnitTest1
{
    private const string FullyQualifiedNamespace = "<your-namespace>.servicebus.windows.net";
    private const string ConnectionString = "<your-connectionstring>";
    private const string QueueName = "demoqueue";

    [Fact]
    public async Task Test_Scenario01_UsePrimaryConnectionString()
    {
        await using var client = new ServiceBusClient(ConnectionString);
        var scenario = async () => await client.RunScenario(QueueName, nameof(Test_Scenario01_UsePrimaryConnectionString));
        await scenario();
    }

    [Fact]
    public async Task Test_Scenario02_UseFullyQualifiedNamespace()
    {
        await using var client = new ServiceBusClient(FullyQualifiedNamespace, new DefaultAzureCredential());
        var scenario = async () => await client.RunScenario(QueueName, nameof(Test_Scenario02_UseFullyQualifiedNamespace));
        await scenario();
    }

    [Fact]
    public async Task Test_Scenario03_UseDependencyInjectionWithPrimaryConnectionString()
    {
        var services = new ServiceCollection();
        services.AddAzureClients(builder => {
            builder.AddServiceBusClient(ConnectionString);
        });
        var serviceProvider = services.BuildServiceProvider();
        var client = serviceProvider.GetRequiredService<ServiceBusClient>();
        var scenario = async () => await client.RunScenario(QueueName, nameof(Test_Scenario03_UseDependencyInjectionWithPrimaryConnectionString));
        await scenario();
    }

    [Fact]
    public async Task Test_Scenario04_UseDependencyInjectionWithFullyQualifiedNamespace()
    {
        var services = new ServiceCollection();
        services.AddAzureClients(builder => {
            builder.AddServiceBusClientWithNamespace(FullyQualifiedNamespace);
        });
        var serviceProvider = services.BuildServiceProvider();
        var client = serviceProvider.GetRequiredService<ServiceBusClient>();
        var scenario = async () => await client.RunScenario(QueueName, nameof(Test_Scenario04_UseDependencyInjectionWithFullyQualifiedNamespace));
        await scenario();
    }

    [Fact]
    public async Task Test_Scenario05_DependencyInjectionChangeDefaultToken()
    {
        var services = new ServiceCollection();
        services.AddAzureClients(builder => {
            builder.AddServiceBusClientWithNamespace(FullyQualifiedNamespace);
            
            builder.UseCredential(new ManagedIdentityCredential());
        });
        var serviceProvider = services.BuildServiceProvider();
        var client = serviceProvider.GetRequiredService<ServiceBusClient>();
        var scenario = async () => await client.RunScenario(QueueName, nameof(Test_Scenario05_DependencyInjectionChangeDefaultToken));
        await scenario.Should().ThrowAsync<CredentialUnavailableException>();
    }

    [Fact]
    public async Task Test_Scenario06_DependencyInjectionChangeDefaultTokenOnClientLevel()
    {
        var services = new ServiceCollection();
        services.AddAzureClients(builder => {
            builder.AddServiceBusClientWithNamespace(FullyQualifiedNamespace)
                .WithCredential(new AzureCliCredential());
            
            builder.UseCredential(new ManagedIdentityCredential());
        });
        var serviceProvider = services.BuildServiceProvider();
        var client = serviceProvider.GetRequiredService<ServiceBusClient>();
        var scenario = async () => await client.RunScenario(QueueName, nameof(Test_Scenario06_DependencyInjectionChangeDefaultTokenOnClientLevel));
        await scenario();
    }
    
    [Fact]
    public async Task Test_Scenario07_MultipleClients()
    {
        var services = new ServiceCollection();
        services.AddAzureClients(builder =>
        {
            builder.AddServiceBusClient(ConnectionString);

            builder.AddServiceBusClientWithNamespace(FullyQualifiedNamespace)
                .WithName("OtherClient");
        });
        var serviceProvider = services.BuildServiceProvider();
        var clientFactory = serviceProvider.GetRequiredService<IAzureClientFactory<ServiceBusClient>>();
        
        var clientDefault = clientFactory.CreateClient("Default");
        var scenarioDefaultClient = async () => await clientDefault.RunScenario(QueueName, nameof(Test_Scenario07_MultipleClients) + "A");
        await scenarioDefaultClient();
        
        var otherClient = clientFactory.CreateClient("OtherClient");
        var scenarioOtherClient = async () => await otherClient.RunScenario(QueueName, nameof(Test_Scenario07_MultipleClients) + "B");
        await scenarioOtherClient();
    }
    
    [Fact]
    public async Task Test_Scenario08_StronglyTypedOptions()
    {
        var services = new ServiceCollection();
        services.Configure<DemoOptions>(options =>
        {
            options.ServiceBusNamespace = FullyQualifiedNamespace;
        });
        services.AddAzureClients(builder =>
        {
            builder.AddClient<ServiceBusClient, ServiceBusClientOptions>((options, credential, provider) =>
            {
                var demoOptions = provider.GetRequiredService<IOptions<DemoOptions>>();
                return new ServiceBusClient(demoOptions.Value.ServiceBusNamespace, credential, options);
            });
        });
        var serviceProvider = services.BuildServiceProvider();
        var client = serviceProvider.GetRequiredService<ServiceBusClient>();
        var scenario = async () => await client.RunScenario(QueueName, nameof(Test_Scenario08_StronglyTypedOptions));
        await scenario();
    }
}
