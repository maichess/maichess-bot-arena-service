using System.Diagnostics.CodeAnalysis;
using Confluent.Kafka;
using Google.Protobuf;

namespace MaichessBotArenaService.Kafka;

// Raw-Protobuf Kafka value deserializer for the maichess.events.v1 generated
// messages. Mirrors match-manager's serde: Kafka task 09 removed the Confluent
// Schema Registry, so the wire format is the bare Protobuf bytes
// (Parser.ParseFrom) — interoperable with the Scala / C# producers on the shared
// match.events.v1 topic. The arena only consumes, so only a deserializer is
// provided. Excluded from coverage like the other Kafka glue.
[ExcludeFromCodeCoverage]
internal static class ProtobufEventSerdes
{
    public static IDeserializer<T> Deserializer<T>()
        where T : IMessage<T>, new()
        => new RawDeserializer<T>();

    private sealed class RawDeserializer<T> : IDeserializer<T>
        where T : IMessage<T>, new()
    {
        private static readonly MessageParser<T> Parser = new(() => new T());

        public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context) =>
            isNull ? new T() : Parser.ParseFrom(data);
    }
}
