# AGENTS.md

This file guides AI coding agents working in the Orleans.Messaging repository.

## Quick Start

**Framework:** .NET 10.0, C# 14.0 with nullable reference types and implicit usings.

**Build:** `dotnet build` (solution uses modern `orleans.messaging.slnx` format with central package versioning via `Directory.Packages.props`)

**Architecture:** Provider pattern with a core messaging abstraction (`IMessagingClient`) and multiple broker implementations (Kafka, in-memory) using keyed DI.

## Project Structure

### Core Packages

| Project | Purpose |
|---------|---------|
| `Orleans.Messaging/` | Core abstractions: `IMessagingClient`, `Message<T>`, `SubscriptionBuilder<T>`, `TopicSubscription`, grain contracts |
| `Orleans.Messaging.Kafka/` | Kafka broker via Confluent.Kafka |
| `Orleans.Messaging.Memory/` | In-memory broker for testing/local development |
| `Orleans.Messaging.SerDes/` | Pluggable serializers (JSON, Avro, String) |

### Test Infrastructure

The project includes **comprehensive test suites** using xUnit with Aspire.Hosting.Testing:

| Test Project | Purpose | Fixture |
|--------------|---------|---------|
| `Orleans.Messaging.Tests.Memory/` | Memory broker integration tests | `MemoryClusterFixture` (xUnit `IAsyncLifetime`) |
| `Orleans.Messaging.Tests.Kafka/` | Kafka broker integration tests | `KafkaClusterFixture` (xUnit `IAsyncLifetime`) |
| `Orleans.Messaging.Tests.Grains/` | Shared test grain implementations (`ITestReceiverGrain`, `TestMessage`) | N/A (shared library) |
| `Orleans.Messaging.Tests.Memory.SiloHost/` | Silo for Memory tests | N/A (Orleans host) |
| `Orleans.Messaging.Tests.Kafka.SiloHost/` | Silo for Kafka tests | N/A (Orleans host) |
| `Orleans.Messaging.MemoryAppHost/` | Aspire host for Memory testing | Launches 2 silos via DistributedApplication |
| `Orleans.Messaging.KafkaAppHost/` | Aspire host for Kafka testing | Launches Kafka container + silo |

Tests use **xUnit test collections** with fixtures (e.g., `[Collection("Memory")]`, `[Collection("Kafka")]`).

## Architecture Deep Dive

### Core Abstractions (Orleans.Messaging/)

- **`IMessagingClient`** — Main user interface; resolved via keyed DI using `MessageBrokerNames` constants
  - Methods: `Subscribe<T>()`, `Unsubscribe<T>()`, `Produce<T>()`
- **`Message<T>`** — Serializable message record with metadata (MessageId, Key, Headers, Payload, ConsumedTimestamp)
- **`SubscriptionBuilder<T>`** — Fluent subscription configuration; compiles to `TopicSubscription`
- **`MessageSubscriptionInput<T>`** — Alternative subscription input record
- **`ISubscriptionClient` / `IProducerClient`** — Provider-specific internal interfaces resolved via same service key
- **`IMessagingRuntimeOptionsService`** — Abstract service for resolving subscription/producer grain types by queue name

### Message Records

```csharp
// Base record: Message (abstract)
// Generic variant: Message<TPayload>

// Metadata structure:
public record Message<TPayload>
{
    public string MessageId { get; } // ULID
    public string Key { get; }
    public ConsumerQueueIdentity QueueIdentity { get; }
    public Dictionary<string, string> Headers { get; }
    public TPayload Payload { get; }
    public DateTime? ConsumedTimestamp { get; }
}

// Convenience extension: payload.AsMessage(key: "...")
```

### Service Keys & Multiple Brokers

Brokers are registered via `MessageBrokerNames` constants in `Orleans.Messaging/`:

```csharp
public static class MessageBrokerNames
{
    public const string DefaultBroker = "messageBroker";
    public const string Conduit = "conduitMessageBroker";
    public const string IronwoodRelay = "ironwoodRelayMessageBroker";
}
```

Both core abstractions and provider-specific clients are resolved using the same service key:

```csharp
var client = serviceProvider.GetRequiredKeyedService<IMessagingClient>(MessageBrokerNames.DefaultBroker);
```

### Subscription System

- Built via `SubscriptionBuilder<T>` fluent API or passed as `MessageSubscriptionInput<T>` record
- Compiles to `TopicSubscription` containing: `ServiceKey`, `SubscriptionId`, `TopicName`, `SubscriptionPattern`
- Subscription IDs encode grain identity for unsubscribe operations
- **Pattern matching modes** (via `PatternType` enum):
  - Exact key match
  - Substring match
  - Regex match (e.g., `@"^order\.\d+$"`)
- **Grain-based handler model:** Subscriptions route matched messages to a target grain's method via `WithGrainAction(methodName)`

### Grains & Message Routing

Providers implement topic grains (e.g., `TopicGrain` in Kafka) that:
- Manage message routing from producers to subscribed consumers
- Implement error handling with `ProcessingErrorHandlingMode` (route to DLQ, retry, drop)
- Persist subscription state using Orleans grain state

## Build & Development Workflow

### Build Commands

```bash
# Build entire solution
dotnet build

# Build specific project (example)
dotnet build Orleans.Messaging.Kafka/Orleans.Messaging.Kafka.csproj

# Run tests (via test fixtures)
dotnet test tests/Orleans.Messaging.Tests.Memory/
dotnet test tests/Orleans.Messaging.Tests.Kafka/
```

### Running Tests Locally

Tests use **Aspire.Hosting.Testing** with distributed application builders:

1. **Memory tests** spawn 2 in-memory Orleans silos via `DistributedApplicationTestingBuilder`
2. **Kafka tests** spawn Kafka container + Orleans silo via `DistributedApplicationTestingBuilder`
3. Each test class receives a fixture (e.g., `MemoryClusterFixture`) implementing `IAsyncLifetime`
4. Fixtures provide `ServiceProvider` and `GrainFactory` for test access

Example test pattern:
```csharp
[Collection("Memory")]
public class SubscribeTests
{
    private readonly IMessagingClient _client;
    private readonly IGrainFactory _grains;

    public SubscribeTests(MemoryClusterFixture fixture)
    {
        _client = fixture.ServiceProvider
            .GetRequiredKeyedService<IMessagingClient>(MessageBrokerNames.DefaultBroker);
        _grains = fixture.GrainFactory;
    }

    [Fact]
    public async Task Subscribe_ExactPattern_ReceivesOnlyMatchingMessages() { /*...*/ }
}
```

### Project Structure Details

- **Solution file:** `orleans.messaging.slnx` (modern format)
- **Central package versioning:** `Directory.Packages.props` (defines all dependency versions)
- **Common build config:** `Directory.Build.props` (targets net10.0, enables nullable, implicit usings, LangVersion 14.0, XML doc comments)
- **NuGet metadata:** Authors, repository, package tags, and icon in `Directory.Build.props`

## Key Dependencies

| Dependency | Version | Notes |
|-----------|---------|-------|
| Orleans | 9.2.1 | Runtime, SDK, serialization abstractions |
| Confluent.Kafka | 2.3.0 | Kafka client (Kafka provider only) |
| Chr.Avro.Confluent | 10.2.1 | Avro serialization (SerDes) |
| Aspire.Hosting.Testing | Latest | Integration test framework for distributed apps |
| FluentAssertions | Latest | Test assertions |
| Polly | 8.6.4 | Retry policies |
| Ulid | Latest | Distributed ID generation for messages |
| Serilog | Latest | Logging (infrastructure, not used in library code) |
| xUnit | Latest | Test framework |

## Conventions & Patterns

### Configuration & Registration

- **Silo mode** (`siloBuilder.AddMessagingKafka(key, ...)` / `siloBuilder.AddMessagingMemory(key, ...)`): Full grain infrastructure for producing and consuming
- **Client mode** (`hostBuilder.AddMessagingKafkaClient(key, ...)` / `hostBuilder.AddMessagingMemoryClient(key, ...)`): Producer-only setup (no consumer grain infrastructure)
- Both modes resolve the same `IMessagingClient` interface
- Configuration methods named `ConfigureMessaging{Provider}` (e.g., `ConfigureMessagingKafka`)
- Each provider exposes an Options class: `MessagingKafkaOptions`, `MessagingMemoryOptions`
- Configuration via builder pattern: `.WithOptions(opts => { ... }).Build()`

### Message Serialization

- **Orleans codegen** (via `[GenerateSerializer]` attribute) handles `Message` and `Message<T>` serialization
- **Custom serializers** (JSON, Avro, String) in `Orleans.Messaging.SerDes/` are for payloads only
- Implement `IMessageSerializer` to add custom serializers

### Subscription Matching

- Pattern-based via `PatternType` enum: `Exact`, `Substring`, `Regex`
- `ConsumerQueueIdentity.Metadata` stores partition and sequence info
- Subscription IDs encode full grain reference for unsubscribe

### Memory Provider Specifics

- Partitioning via simple ring hash (`SimpleRingHash.cs`)
- Configurable `MaxPartitionCount` for scaling
- Pure in-memory with grain state persistence
- No external broker dependencies

### Kafka Provider Specifics

- `TopicGrain` manages Kafka topic and consumer group lifecycle
- Configurable consumer start modes: `Beginning`, `LastCommittedMessage`, `Last`
- Poll-based consumption with configurable batch sizes and timeouts
- Optional Avro schema registry integration via Confluent packages

### Error Handling & DLQ

- Error mode enum: `ProcessingErrorHandlingMode` (route to DLQ, retry, drop)
- Grain contracts include `SendToDlq` method
- Consumer grains implement error handling based on configured mode

## File Organization Guide

### Core Package (`Orleans.Messaging/`)

```
Accessors/        → Keyed DI accessor helpers
Config/           → MessagingBuilder, MessagingClientBuilder, options services
Consuming/        → IQueueConsumer, MessageDigestingService
Producing/        → Producer interfaces and routing
Subscription/     → SubscriptionBuilder, SubscriptionClient, TopicSubscription
Utils/            → Extension methods, helpers
Message.cs        → Message<T>, ConsumerQueueIdentity records
MessagingClient.cs → Main IMessagingClient implementation
```

### Provider Packages

Both Kafka and Memory providers follow the same structure:

```
Config/     → ProviderOptions, builders, extension methods
Consuming/  → ConsumerGrain, subscription handling
Producing/  → ProducerGrain, message routing
Utils/      → Provider-specific helpers
```

## Important Notes

- **No test projects in solution root** — tests are in `tests/` directory, organized by provider
- **Aspire-based testing** — fixtures use `DistributedApplicationTestingBuilder` for realistic multi-silo scenarios
- **Orleans manages grain lifecycle** — Messaging framework only handles messaging, not grain instantiation
- **Post-startup configuration** — Kafka topics are registered after DI container is built
- **Shared grain implementations** — `Orleans.Messaging.Tests.Grains/` provides `ITestReceiverGrain` and `TestMessage` for all tests
- **xUnit test collections** — Use `[Collection("Memory")]` or `[Collection("Kafka")]` to group tests sharing fixtures
- **Aspire AppHost pattern** — Test hosts (MemoryAppHost, KafkaAppHost) launch via `DistributedApplicationTestingBuilder.CreateAsync<T>()`

## Documentation Sources

- **Root README:** High-level concepts, IMessagingClient interface, silo vs. client modes, service keys, basic usage
- **Kafka README:** Kafka-specific configuration, topic registration, consumer modes, serialization, error handling, examples
- **Memory README:** In-memory broker setup, testing patterns, multi-broker examples
- **Code XML docs:** Public APIs include inline documentation (enabled in build config)

