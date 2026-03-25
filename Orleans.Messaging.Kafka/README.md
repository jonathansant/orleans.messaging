# Orleans.Messaging.Kafka

Kafka broker implementation for Orleans.Messaging. Wraps [Confluent.Kafka](https://github.com/confluentinc/confluent-kafka-dotnet) behind the unified `IMessagingClient` API, with grain-based consumers and producers managed by Orleans.

## Table of Contents

- [Setup](#setup)
- [Configuration](#configuration)
  - [Broker options](#broker-options)
  - [Security](#security)
  - [Topic configuration](#topic-configuration)
- [Producing messages](#producing-messages)
- [Subscribing](#subscribing)
  - [Subscribe by pattern](#subscribe-by-pattern)
- [Dead letter queue (DLQ)](#dead-letter-queue-dlq)
- [Unsubscribing](#unsubscribing)
- [Serialization](#serialization)
- [Grain handler setup](#grain-handler-setup)
- [Multiple broker instances](#multiple-broker-instances)

---

## Setup

Register the Kafka provider on your Orleans silo builder, then call `.Build()`:

```csharp
siloBuilder.ConfigureServices(services =>
{
    // register the builder under a service key
    services.AddKeyedSingleton<MessagingKafkaBuilder>(
        MessageBrokerNames.Platform,
        (sp, _) => new MessagingKafkaBuilder(siloBuilder, MessageBrokerNames.Platform)
    );
});

// configure topics after the DI container is built
app.Services.ConfigureMessagingKafka(MessageBrokerNames.Platform, (sp, builder) =>
{
    builder
        .WithOptions(opts =>
        {
            opts.BrokerList = new[] { "localhost:9092" };
            opts.ConsumerGroupId = "my-service";
            opts.ConsumeMode = ConsumeMode.LastCommittedMessage;
        })
        .WithProducerRetries(maxRetries: 3, maxRetryDelay: TimeSpan.FromMilliseconds(50))
        .Build();

    sp.AddTopic("orders", MessageBrokerNames.Platform, topic =>
        topic
            .WithContract<OrderCreated>()
            .WithTopicType(TopicType.InOut)
            .WithBatchSize(25)
    );
});
```

Resolve `IMessagingClient` from DI:

```csharp
var client = sp.GetRequiredKeyedService<IMessagingClient>(MessageBrokerNames.Platform);
```

---

## Configuration

### Broker options

Configure via `MessagingKafkaOptions` inside `WithOptions(...)`:

| Property | Default | Description |
|----------|---------|-------------|
| `BrokerList` | — | **Required.** List of Kafka bootstrap servers (e.g. `["broker:9092"]`) |
| `ConsumerGroupId` | `"odin-messaging-kafka"` | Kafka consumer group ID |
| `ConsumeMode` | `LastCommittedMessage` | Where to start reading: `Beginning`, `LastCommittedMessage`, `Last` |
| `PollRate` | 17 ms | How often the consumer grain polls Kafka |
| `PollTimeout` | 50 ms | Kafka client poll timeout |
| `BatchSize` | `10` | Max messages per batch delivered to handlers |
| `ProducerTimeout` | 5000 ms | Kafka producer delivery timeout |
| `AdminRequestTimeout` | 5 s | Timeout for admin operations (topic creation) |
| `IdleTimeout` | `0` (disabled) | Consumer idle timeout in ms |
| `IsConsumeEnabled` | `true` | Enable or disable the consumer |
| `IsProduceEnabled` | `true` | Enable or disable the producer |
| `AvroUrl` | `null` | Schema registry URL for Avro serialization |
| `StoreName` | `"default-store"` | Grain storage provider name |
| `EnsureHandlerDeliveryOnFailure` | `false` | Retry failed handler invocations |
| `ProducerRetryOptions` | `MaxRetries=2, RetryDelay=10ms` | Producer-side retry settings |
| `DesiredProducerTimeout` | 175 ms | Soft timeout for producer operations |

```csharp
builder.WithOptions(opts =>
{
    opts.BrokerList = new[] { "kafka-broker:9092" };
    opts.ConsumerGroupId = "order-service";
    opts.ConsumeMode = ConsumeMode.LastCommittedMessage;
    opts.BatchSize = 50;
    opts.PollRate = TimeSpan.FromMilliseconds(20);
    opts.IsConsumeEnabled = true;
    opts.IsProduceEnabled = true;
});
```

Builder convenience methods (available on `MessagingKafkaBuilder` and base `MessagingBuilder<T>`):

```csharp
builder
    .WithStoreName("my-grain-store")
    .WithProducerRetries(maxRetries: 3, maxRetryDelay: TimeSpan.FromMilliseconds(25))
    .WithEnsureHandlerDeliveryOnFailure();  // retry handlers on failure
```

### Security

```csharp
builder.WithOptions(opts =>
{
    opts.SecurityProtocol = SecurityProtocol.SaslSsl;
    opts.SaslMechanism    = SaslMechanism.ScramSha512;
    opts.SaslUserName     = "my-user";
    opts.SaslPassword     = "my-password";
    opts.SslCaLocation    = "/etc/ssl/certs/ca-certificates.crt";
});
```

`SecurityProtocol` values: `Plaintext`, `Ssl`, `SaslPlaintext`, `SaslSsl`
`SaslMechanism` values: `Gssapi`, `Plain`, `ScramSha256`, `ScramSha512`

### Topic configuration

Topics are registered after container build using `AddTopic` on the `IServiceProvider`:

```csharp
sp.AddTopic("orders", MessageBrokerNames.Platform, topic =>
    topic
        .WithContract<OrderCreated>()           // payload type
        .WithTopicType(TopicType.InOut)          // Consumer | Producer | InOut
        .WithPartitioning(isPartitioned: true)  // default true
        .WithBatchSize(25)                       // override global BatchSize
        .WithPollRate(TimeSpan.FromMilliseconds(30)) // override global PollRate
        .WithSerializer<JsonSerializer<OrderCreated>>()
        .UseProcessingErrorHandlingMode(ProcessingFailedHandlingMode.Dlq, dlqName: "orders-dlq")
        .WithCreationOptions(new TopicCreationConfig
        {
            AutoCreate       = true,
            Partitions       = 6,
            ReplicationFactor = 2,
            RetentionPeriodInMs = 604_800_000UL // 7 days
        })
);
```

`TopicType` values:

| Value | Description |
|-------|-------------|
| `Consumer` | Topic is only consumed |
| `Producer` | Topic is only produced to |
| `InOut` | Topic is both consumed and produced to |

---

## Producing messages

```csharp
// Simple: key + payload
await client.Produce<OrderCreated>("orders", key: orderId, message: new OrderCreated { ... });

// With headers and metadata
var msg = new Message<OrderCreated>
{
    Key     = orderId,
    Payload = new OrderCreated { ... }
}.AddHeader("correlation-id", correlationId);

await client.Produce("orders", msg);

// Convenience extension
var msg = new OrderCreated { ... }.AsMessage(key: orderId, headers: new() { ["source"] = "checkout" });
await client.Produce("orders", msg);
```

---

## Subscribing

Subscriptions route messages from a topic to a specific grain method. The grain interface must:
- Implement `IMessagingGrainContract`
- Have a handler method with signature `Task MethodName(ImmutableList<Message<TMessage>> messages)`

```csharp
var subscriptionId = await client.Subscribe<OrderCreated>(builder => builder
    .WithQueueName("orders")
    .WithGrainType<IOrderProcessorGrain>()
    .WithPrimaryKey("order-processor")
    .WithSubscriptionPattern("*")            // subscription key / pattern
    .WithGrainAction(async messages =>
    {
        foreach (var msg in messages)
            await ProcessOrder(msg.Payload);
    })
);
```

Alternatively, point to a method by name:

```csharp
builder.WithGrainAction("HandleOrderCreated")
```

Or use `[SubscriptionHandler]` on the grain interface method to auto-discover it (no `.WithGrainAction()` needed):

```csharp
public interface IOrderProcessorGrain : IGrainWithStringKey, IMessagingGrainContract
{
    [SubscriptionHandler]
    Task HandleOrderCreated(ImmutableList<Message<OrderCreated>> messages);
}
```

### Subscribe by pattern

Use `WithSubscriptionPattern` to filter messages by key. Three matching modes are available via `PatternType`:

```csharp
// Exact match (default) — only messages whose key equals "order-key-abc"
builder.WithSubscriptionPattern("order-key-abc");

// Substring match — keys containing "region-us"
builder.WithSubscriptionPattern("region-us", opts =>
    opts.PatternType = PatternType.Substring
);

// Regex match
builder.WithSubscriptionPattern(@"^order\.\d+$", opts =>
    opts.PatternType = PatternType.Regex
);
```

`PatternType` values: `Exact`, `Substring`, `Regex`

Optionally throttle delivery with `SubscriptionDelayOptions`:

```csharp
builder.WithSubscriptionPattern("orders", opts =>
{
    opts.PatternType = PatternType.Substring;
    opts.SubscriptionDelayOptions = new ScheduledThrottledActionOptions
    {
        ThrottleTime    = TimeSpan.FromMilliseconds(200),
        HasLeadingDelay = false,
    };
});
```

---

## Dead letter queue (DLQ)

Configure per topic to automatically route messages when handler processing fails:

```csharp
sp.AddTopic("orders", MessageBrokerNames.Platform, topic =>
    topic
        .WithContract<OrderCreated>()
        .UseProcessingErrorHandlingMode(
            ProcessingFailedHandlingMode.Dlq,
            dlqName: "orders-dlq"        // omit to default to "{topicName}_dlq"
        )
);
```

`ProcessingFailedHandlingMode` values:

| Value | Behaviour |
|-------|-----------|
| `AckOnComplete` | Acknowledge message regardless of handler outcome (default) |
| `Dlq` | Route to dead letter queue on handler failure |
| `Ignore` | Do not acknowledge; log and move on |

To manually send a message to the DLQ from application code:

```csharp
// Note: SendToDlq is an internal API — call via MessagingClient cast
await ((MessagingClient)client).SendToDlq("orders", failedMessage);
```

The DLQ topic must also be configured as a `Producer` or `InOut` topic.

---

## Unsubscribing

```csharp
// By subscription ID returned from Subscribe()
await client.Unsubscribe<OrderCreated>(subscriptionId);

// By explicit parts
await client.Unsubscribe<OrderCreated>(
    queueName: "orders",
    subscriptionPattern: "region-us",
    subscriptionId: subscriptionId
);

// Via TopicSubscription record
await client.Unsubscribe<OrderCreated>(new TopicSubscription(
    ServiceKey: MessageBrokerNames.Platform,
    SubscriptionId: subscriptionId,
    TopicName: "orders",
    SubscriptionPattern: "region-us"
));
```

---

## Serialization

Specify a serializer per topic via `WithSerializer<TSerializer>()`. The type argument must be an open generic implementing `IMessageSerializer<T>`.

```csharp
// JSON (default if none specified)
topic.WithSerializer<JsonSerializer<OrderCreated>>();

// Avro — requires AvroUrl in MessagingKafkaOptions
topic.WithSerializer<AvroSerializer<OrderCreated>>();

// Raw string payload
topic.WithSerializer<StringSerializer<MyStringMessage>>();

// Custom serializer
public class MySerializer<T> : IMessageSerializer<T>
{
    public ValueTask<T> Deserialize(string queueName, byte[] data) { ... }
    public ValueTask<byte[]> Serialize(string queueName, T data) { ... }
    public ValueTask<object> Deserialize(string queueName, byte[] data) { ... }
    public void Dispose() { }
}

topic.WithSerializer<MySerializer<OrderCreated>>();
```

---

## Grain handler setup

```csharp
// Interface
public interface IOrderProcessorGrain : IGrainWithStringKey, IMessagingGrainContract
{
    [SubscriptionHandler]
    Task HandleOrderCreated(ImmutableList<Message<OrderCreated>> messages);
}

// Implementation
public class OrderProcessorGrain : Grain, IOrderProcessorGrain
{
    public Task Activate() => Task.CompletedTask;
    public Task ActivateOneWay() => Task.CompletedTask;

    public async Task HandleOrderCreated(ImmutableList<Message<OrderCreated>> messages)
    {
        foreach (var msg in messages)
        {
            // msg.Payload     — the typed payload
            // msg.Key         — the message key used for routing/partitioning
            // msg.Headers     — key/value metadata headers
            // msg.MessageId   — auto-generated ULID
            // msg.ConsumedTimestamp — when the message was consumed
            // msg.QueueIdentity   — partition, sequence key, broker metadata
        }
    }
}
```

Subscribe inside the grain's `OnActivateAsync` to self-register:

```csharp
public override async Task OnActivateAsync(CancellationToken ct)
{
    var client = ServiceProvider.GetRequiredKeyedService<IMessagingClient>(MessageBrokerNames.Platform);

    await this.CreateSubscriptionConfig<OrderCreated, IOrderProcessorGrain>(
            queueName: "orders",
            pattern: "*",
            serviceKey: MessageBrokerNames.Platform
        )
        .WithSubscriptionPattern("*")
        .Build()
        |> (input => client.Subscribe(input));
}
```

Or use the `SubscriptionClientExtensions` helper:

```csharp
var builder = this.CreateSubscriptionConfig<OrderCreated, IOrderProcessorGrain>(
    queueName:  "orders",
    pattern:    "*",
    serviceKey: MessageBrokerNames.Platform
);

await client.Subscribe(builder.Build());
```

---

## Multiple broker instances

Register and configure separate `MessagingKafkaBuilder` instances under different service keys:

```csharp
// Register
services.AddKeyedSingleton<MessagingKafkaBuilder>(MessageBrokerNames.Platform, ...);
services.AddKeyedSingleton<MessagingKafkaBuilder>(MessageBrokerNames.Bifrost, ...);

// Configure
app.Services.ConfigureMessagingKafka(MessageBrokerNames.Platform, (sp, b) =>
{
    b.WithOptions(o => { o.BrokerList = new[] { "internal-kafka:9092" }; }).Build();
    sp.AddTopic("orders", MessageBrokerNames.Platform, t => t.WithContract<OrderCreated>()...);
});

app.Services.ConfigureMessagingKafka(MessageBrokerNames.Bifrost, (sp, b) =>
{
    b.WithOptions(o => { o.BrokerList = new[] { "external-kafka:9092" }; }).Build();
    sp.AddTopic("events", MessageBrokerNames.Bifrost, t => t.WithContract<ExternalEvent>()...);
});

// Resolve the right client
var internalClient  = sp.GetRequiredKeyedService<IMessagingClient>(MessageBrokerNames.Platform);
var externalClient  = sp.GetRequiredKeyedService<IMessagingClient>(MessageBrokerNames.Bifrost);
```
