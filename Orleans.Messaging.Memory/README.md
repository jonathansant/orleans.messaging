# Orleans.Messaging.Memory

In-memory broker implementation for Orleans.Messaging. Messages are routed entirely within the Orleans silo — no external broker required. Ideal for **unit/integration testing** and **local development**.

## Table of Contents

- [Setup](#setup)
- [Configuration](#configuration)
- [Producing messages](#producing-messages)
- [Subscribing](#subscribing)
  - [Subscribe by pattern](#subscribe-by-pattern)
- [Unsubscribing](#unsubscribing)
- [Grain handler setup](#grain-handler-setup)
- [Multiple broker instances](#multiple-broker-instances)
- [Differences from Kafka](#differences-from-kafka)

---

## Setup

Register the memory provider on your Orleans silo builder:

```csharp
siloBuilder.ConfigureServices(services =>
{
    var memoryBuilder = new MessagingMemoryBuilder(siloBuilder, MessageBrokerNames.Platform);

    memoryBuilder
        .WithOptions(opts =>
        {
            opts.MaxPartitionCount  = 4;
            opts.ProducePollRateMs  = 50;
        })
        .WithProducerRetries(maxRetries: 2)
        .Build();

    services.AddKeyedSingleton<MessagingMemoryBuilder>(MessageBrokerNames.Platform, memoryBuilder);
});
```

Resolve `IMessagingClient`:

```csharp
var client = sp.GetRequiredKeyedService<IMessagingClient>(MessageBrokerNames.Platform);
```

---

## Configuration

Configure via `MessagingMemoryOptions` inside `WithOptions(...)`:

| Property | Default | Description |
|----------|---------|-------------|
| `MaxPartitionCount` | `Environment.ProcessorCount` | Number of virtual partitions for message distribution |
| `ProduceInitDelayMs` | `500` | Initial delay (ms) before the producer starts delivering messages |
| `ProducePollRateMs` | `50` | How often (ms) the producer grain polls its internal queue |
| `DesiredProducerTimeout` | 1 s | Soft timeout for producer operations |
| `IsProduceEnabled` | `true` | Enable or disable the producer |
| `StoreName` | `"default-store"` | Grain storage provider name |
| `EnsureHandlerDeliveryOnFailure` | `false` | Retry failed handler invocations |
| `ProducerRetryOptions` | `MaxRetries=2, RetryDelay=0` | Producer-side retry settings |

```csharp
builder.WithOptions(opts =>
{
    opts.MaxPartitionCount = 2;
    opts.ProduceInitDelayMs = 0;    // no initial delay in tests
    opts.ProducePollRateMs  = 10;   // faster polling for tests
});
```

Builder convenience methods (inherited from `MessagingBuilder<T>`):

```csharp
builder
    .WithStoreName("my-grain-store")
    .WithProducerRetries(maxRetries: 3, maxRetryDelay: TimeSpan.FromMilliseconds(25))
    .WithEnsureHandlerDeliveryOnFailure();
```

---

## Producing messages

The API is identical to the Kafka provider:

```csharp
// Simple: key + payload
await client.Produce<OrderCreated>("orders", key: orderId, message: new OrderCreated { ... });

// With headers
var msg = new Message<OrderCreated>
{
    Key     = orderId,
    Payload = new OrderCreated { ... }
}.AddHeader("correlation-id", correlationId);

await client.Produce("orders", msg);

// Convenience extension
var msg = new OrderCreated { ... }.AsMessage(key: orderId);
await client.Produce("orders", msg);
```

Messages are stored in the `MemoryProducerGrain`'s in-memory queue and flushed to subscribers on each poll tick (`ProducePollRateMs`).

---

## Subscribing

Subscriptions follow the same pattern as the Kafka provider. The grain interface must implement `IMessagingGrainContract` and have a handler method with signature `Task MethodName(ImmutableList<Message<TMessage>> messages)`.

```csharp
var subscriptionId = await client.Subscribe<OrderCreated>(builder => builder
    .WithQueueName("orders")
    .WithGrainType<IOrderProcessorGrain>()
    .WithPrimaryKey("order-processor")
    .WithSubscriptionPattern("*")
    .WithGrainAction(async messages =>
    {
        foreach (var msg in messages)
            await ProcessOrder(msg.Payload);
    })
);
```

Or point to a named method:

```csharp
builder.WithGrainAction("HandleOrderCreated")
```

Or use `[SubscriptionHandler]` on the grain interface to auto-discover the handler:

```csharp
public interface IOrderProcessorGrain : IGrainWithStringKey, IMessagingGrainContract
{
    [SubscriptionHandler]
    Task HandleOrderCreated(ImmutableList<Message<OrderCreated>> messages);
}
```

### Subscribe by pattern

Filter messages by key using `WithSubscriptionPattern`. Three modes are supported via `PatternType`:

```csharp
// Exact match (default)
builder.WithSubscriptionPattern("order-key-abc");

// Substring — matches keys containing "region-us"
builder.WithSubscriptionPattern("region-us", opts =>
    opts.PatternType = PatternType.Substring
);

// Regex
builder.WithSubscriptionPattern(@"^order\.\d+$", opts =>
    opts.PatternType = PatternType.Regex
);
```

`PatternType` values: `Exact`, `Substring`, `Regex`

Throttle delivery using `SubscriptionDelayOptions`:

```csharp
builder.WithSubscriptionPattern("orders", opts =>
{
    opts.PatternType = PatternType.Substring;
    opts.SubscriptionDelayOptions = new ScheduledThrottledActionOptions
    {
        ThrottleTime    = TimeSpan.FromMilliseconds(100),
        HasLeadingDelay = false,
    };
});
```

---

## Unsubscribing

```csharp
// By subscription ID returned from Subscribe()
await client.Unsubscribe<OrderCreated>(subscriptionId);

// By explicit parts
await client.Unsubscribe<OrderCreated>(
    queueName: "orders",
    subscriptionPattern: "*",
    subscriptionId: subscriptionId
);

// Via TopicSubscription record
await client.Unsubscribe<OrderCreated>(new TopicSubscription(
    ServiceKey: MessageBrokerNames.Platform,
    SubscriptionId: subscriptionId,
    TopicName: "orders",
    SubscriptionPattern: "*"
));
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
            // msg.Payload           — the typed payload
            // msg.Key               — message key
            // msg.Headers           — key/value metadata headers
            // msg.MessageId         — auto-generated ULID
            // msg.ConsumedTimestamp — when the message was consumed
        }
    }
}
```

Subscribe inside `OnActivateAsync` using the `SubscriptionClientExtensions` helper:

```csharp
public override async Task OnActivateAsync(CancellationToken ct)
{
    var client = ServiceProvider.GetRequiredKeyedService<IMessagingClient>(MessageBrokerNames.Platform);

    var input = this.CreateSubscriptionConfig<OrderCreated, IOrderProcessorGrain>(
        queueName:  "orders",
        pattern:    "*",
        serviceKey: MessageBrokerNames.Platform
    ).Build();

    await client.Subscribe(input);
}
```

---

## Multiple broker instances

```csharp
// Register two independent memory brokers
var platformBuilder = new MessagingMemoryBuilder(siloBuilder, MessageBrokerNames.Platform);
platformBuilder.WithOptions(o => { o.MaxPartitionCount = 4; }).Build();

var bifrostBuilder = new MessagingMemoryBuilder(siloBuilder, MessageBrokerNames.Bifrost);
bifrostBuilder.WithOptions(o => { o.MaxPartitionCount = 2; }).Build();

services.AddKeyedSingleton(MessageBrokerNames.Platform, platformBuilder);
services.AddKeyedSingleton(MessageBrokerNames.Bifrost, bifrostBuilder);

// Resolve independently
var platformClient = sp.GetRequiredKeyedService<IMessagingClient>(MessageBrokerNames.Platform);
var bifrostClient  = sp.GetRequiredKeyedService<IMessagingClient>(MessageBrokerNames.Bifrost);
```

---

## Differences from Kafka

| Feature | Memory | Kafka |
|---------|--------|-------|
| External broker required | No | Yes |
| Topic pre-configuration (`AddTopic`) | Not needed | Required |
| Serializer configuration | Not needed (Orleans serialization) | Per-topic via `WithSerializer<T>()` |
| DLQ support | No | Yes — via `UseProcessingErrorHandlingMode(Dlq)` |
| Security (SASL/SSL) | N/A | Configurable |
| Message retention / offsets | None (in-process) | Managed by Kafka broker |
| Recommended for | Tests, local dev | Production |
