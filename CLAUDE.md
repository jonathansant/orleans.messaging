# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Orleans.Messaging** is a distributed messaging framework built on [Microsoft Orleans 9.2.1](https://learn.microsoft.com/en-us/dotnet/orleans/). It provides a unified API for producing and consuming messages across different broker implementations (Kafka, in-memory). The framework uses keyed Dependency Injection to support multiple broker instances running side-by-side.

**Target Framework:** .NET 10.0 (C# 14.0) with nullable reference types and implicit usings enabled.

## Architecture

The project uses a **provider pattern** with three tiers:

1. **Core** (`Orleans.Messaging/`): Shared abstractions
   - `IMessagingClient`: Main user-facing interface for producing/consuming messages
   - `Message<T>`: Serializable message record with metadata, headers, and payload
   - `SubscriptionBuilder<T>` & `MessageSubscriptionInput<T>`: Fluent subscription configuration
   - `ISubscriptionClient` & `IProducerClient`: Provider-specific internal interfaces
   - Grain contracts for topic and handler logic

2. **Providers**: Implementation-specific packages
   - `Orleans.Messaging.Kafka/`: Kafka broker implementation via Confluent.Kafka
   - `Orleans.Messaging.Memory/`: In-memory broker for testing/local development

3. **Serialization** (`Orleans.Messaging.SerDes/`): Pluggable serializers
   - `IMessageSerializer`: JSON, Avro, String implementations
   - Shared by all providers

## Key Concepts

### Service Keys & Multiple Brokers

Brokers are registered via keyed DI using `MessageBrokerNames` constants:

```csharp
public static class MessageBrokerNames
{
    public const string DefaultBroker = "messageBroker";
    public const string Conduit = "conduitMessageBroker";
    public const string IronwoodRelay = "ironwoodRelayMessageBroker";
}
```

Each broker instance operates independently. Applications resolve the right client:

```csharp
var client = serviceProvider.GetRequiredKeyedService<IMessagingClient>(MessageBrokerNames.DefaultBroker);
```

### Messages

- **Base:** `Message` (abstract record) with `MessageId` (ULID), `Key`, `QueueIdentity`, `Headers`, `Payload`, `ConsumedTimestamp`
- **Generic:** `Message<TPayload>` with strongly-typed payload access
- Orleans serializes via `[GenerateSerializer]` attribute (Orleans codegen)
- Convenience extension: `myPayload.AsMessage(key: "...")` creates a `Message<T>`

### Subscriptions

- Built fluently via `SubscriptionBuilder<T>` or passed as `MessageSubscriptionInput<T>`
- Compiled to `TopicSubscription` record containing `ServiceKey`, `SubscriptionId`, `TopicName`, `SubscriptionPattern`
- Subscription IDs encode grain identity and subscription reference for unsubscribe operations

### Grains

Providers implement topic grains (e.g., `TopicGrain` in Kafka) that:
- Handle message routing between producers and consumers
- Manage error handling and DLQ routing
- Implement processing error modes (route to DLQ, retry, drop)

## Directory Structure

```
Orleans.Messaging/
├── Accessors/              # Keyed DI accessors for resolving clients
├── Config/                 # Configuration builders and extension methods
├── Consuming/              # Subscription logic and handlers
├── Producing/              # Producer interfaces and logic
├── Subscription/           # Subscription builder and client abstractions
├── Utils/                  # Utilities (extension methods, helpers)
├── Message.cs              # Message, Message<T>, ConsumerQueueIdentity records
├── MessagingClient.cs      # Main IMessagingClient implementation
├── IMessagingGrainContract.cs # Common grain interface

Orleans.Messaging.Kafka/
├── Config/                 # KafkaOptions, configuration builders
├── Consuming/              # ConsumerGrain and subscription logic
├── Producing/              # ProducerGrain and message routing
├── Serialization/          # Kafka-specific serialization helpers
├── TopicGrain.cs           # Main Kafka topic grain

Orleans.Messaging.Memory/
├── Config/                 # MemoryOptions, builders
├── Consuming/              # In-memory consumer implementation
├── Producing/              # In-memory producer implementation
├── Utilities/              # Partition hashing, utilities

Orleans.Messaging.SerDes/
├── AvroSerializer.cs
├── JsonSerializer.cs
├── StringSerializer.cs
└── IMessageSerializer.cs
```

## Build & Development

### Commands

```bash
# Build solution
dotnet build

# Build specific project
dotnet build Orleans.Messaging.Kafka/Orleans.Messaging.Kafka.csproj

# Run solution (requires Orleans host setup - not typical for libraries)
# No built-in tests or CLI tools in the solution currently
```

### Project Structure Details

- **Solution file:** `orleans.messaging.slnx` (modern format)
- **Central package versioning:** `Directory.Packages.props`
- **Common build config:** `Directory.Build.props` (targets net10.0, enables nullable, implicit usings, LangVersion 14.0)
- **NuGet metadata:** Authors, repository, package tags, and icon defined in `Directory.Build.props`

### Key Dependencies

- **Orleans 9.2.1:** Runtime, SDK, serialization abstractions
- **Confluent.Kafka 2.3.0:** Kafka client (Kafka provider only)
- **Chr.Avro.Confluent 10.2.1:** Avro serialization (SerDes)
- **Polly 8.6.4:** Retry policies
- **Ulid:** Distributed ID generation for messages
- **Serilog:** Logging (infrastructure, not used in library code)

## Conventions & Patterns

### Naming & Service Keys

- Broker implementation constants in `MessageBrokerNames` class (in Orleans.Messaging root)
- Internal provider interfaces (`ISubscriptionClient`, `IProducerClient`) resolved via the same service key
- Configuration methods typically named `ConfigureMessaging{Provider}` (e.g., `ConfigureMessagingKafka`)

### Configuration

- Each provider exposes an Options class (`MessagingKafkaOptions`, `MessagingMemoryOptions`)
- Configuration via builder pattern: `.WithOptions(opts => { ... }).Build()`
- Builders return themselves for fluent chaining

### Error Handling

- Dead Letter Queue (DLQ) routing via `ProcessingErrorHandlingMode`
- Grain contracts include `SendToDlq` method
- Consumer grains implement error handling based on configured mode (route to DLQ, retry, drop)

### Subscription Matching

- Pattern-based matching using subscription patterns (e.g., `"region-us"`, `"*"`)
- `ConsumerQueueIdentity.Metadata` stores partition and sequence info
- Subscription IDs encode full grain reference for unsubscribe

### Memory Provider Specifics

- Partitioning via simple ring hash (see `SimpleRingHash.cs`)
- Configurable `MaxPartitionCount` for scaling
- No external dependencies; pure in-memory with grain state persistence

### Kafka Provider Specifics

- `TopicGrain` manages Kafka topic and consumer group lifecycle
- Configurable consumer modes: `Beginning`, `LastCommittedMessage`, `Last`
- Poll-based consumption with configurable batch sizes and timeouts
- Avro schema registry integration (optional, via Confluent packages)

## Documentation

- **Root README:** High-level concepts, service keys, basic usage
- **Kafka README:** Kafka-specific configuration, topic registration, consumer modes, serialization, error handling, examples
- **Memory README:** In-memory broker setup, testing patterns, multi-broker examples
- Code includes XML doc comments for public APIs (enabled in build config)

## Important Notes

- **No test projects** currently in the solution; providers ship as production libraries
- **Grain IDs are Orleans responsibilities:** Messaging framework doesn't manage grain lifecycle, only messaging
- **Configuration timing:** Kafka topics are registered after the DI container is built (post-startup configuration)
- **Serialization:** Orleans' built-in codegen handles Message serialization; custom serializers are for payloads only
