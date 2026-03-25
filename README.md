# Orleans.Messaging

A distributed messaging framework built on [Microsoft Orleans](https://learn.microsoft.com/en-us/dotnet/orleans/), providing a unified API for producing and consuming messages across different brokers.

## Projects

| Project | Description |
|---------|-------------|
| [Orleans.Messaging](./Orleans.Messaging/) | Core abstractions — `IMessagingClient`, `Message<T>`, `SubscriptionBuilder`, grain contracts |
| [Orleans.Messaging.Kafka](./Orleans.Messaging.Kafka/README.md) | Kafka broker implementation |
| [Orleans.Messaging.Memory](./Orleans.Messaging.Memory/README.md) | In-memory broker — ideal for testing and local development |
| [Orleans.Messaging.SerDes](./Orleans.Messaging.SerDes/) | Pluggable serialization framework (JSON, Avro, String) |

## Core Concepts

### IMessagingClient

All broker implementations expose the same `IMessagingClient` interface, resolved via keyed DI using a service key (see `MessageBrokerNames`).

```csharp
public interface IMessagingClient
{
    Task<string> Subscribe<TMessage>(Action<SubscriptionBuilder<TMessage>> configure);
    Task<string> Subscribe<TMessage>(MessageSubscriptionInput<TMessage> input);
    Task Unsubscribe<TMessage>(string subscriptionId);
    Task Unsubscribe<TMessage>(TopicSubscription subscription);
    Task Unsubscribe<TMessage>(string queueName, string subscriptionPattern, string subscriptionId);
    Task Produce<TMessage>(string queueName, string key, TMessage message);
    Task Produce<TMessage>(string queueName, Message<TMessage> message);
}
```

### Messages

```csharp
// Payload only
await client.Produce<OrderCreated>("orders", orderId, new OrderCreated { ... });

// Full message with headers
var msg = new Message<OrderCreated>
{
    Key = orderId,
    Payload = new OrderCreated { ... }
}.AddHeader("source", "checkout");

await client.Produce("orders", msg);

// Convenience extension
var msg = new OrderCreated { ... }.AsMessage(key: orderId);
```

### Service Keys (Multiple Brokers)

Run multiple independent broker instances side by side using `MessageBrokerNames`:

```csharp
public static class MessageBrokerNames
{
    public const string DefaultBroker   = "messageBroker";        // default
    public const string Conduit    = "conduitMessageBroker";
    public const string IronwoodRelay = "ironwoodRelayMessageBroker";
    // ...
}
```

Resolve the right client via keyed DI:

```csharp
var client = serviceProvider.GetRequiredKeyedService<IMessagingClient>(MessageBrokerNames.DefaultBroker);
```

## Provider READMEs

- [Kafka provider](./Orleans.Messaging.Kafka/README.md)
- [Memory provider](./Orleans.Messaging.Memory/README.md)
