using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using sMPGWM.Scripts.FileLoadingSystem;

namespace sMPGWM.Tests.FileLoader;

public class DefinitionDeserializerTests
{
    [Test]
    public void ConverterProgrammingFailurePropagates()
    {
        var error = Assert.Throws<InvalidOperationException>(() =>
            DefinitionDeserializer.Deserialize<BrokenDefinition>(new JObject(), "definitions.yml", "schema.yml"));
        Assert.That(error!.Message, Is.EqualTo("converter bug"));
    }

    [JsonConverter(typeof(BrokenConverter))]
    private sealed class BrokenDefinition { }

    private sealed class BrokenConverter : JsonConverter<BrokenDefinition>
    {
        public override BrokenDefinition? ReadJson(JsonReader reader, Type objectType, BrokenDefinition? existingValue, bool hasExistingValue, JsonSerializer serializer) =>
            throw new InvalidOperationException("converter bug");
        public override void WriteJson(JsonWriter writer, BrokenDefinition? value, JsonSerializer serializer) =>
            throw new NotSupportedException();
    }
}

