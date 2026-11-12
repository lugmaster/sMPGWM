using Newtonsoft.Json; // TEST-SUPPORT: INIT-DESERIALIZER-001 | Tests: sMPGWM.Tests.FileLoader.DefinitionDeserializerTests.ConverterProgrammingFailurePropagates
using Newtonsoft.Json.Linq; // TEST-SUPPORT: INIT-DESERIALIZER-002 | Tests: sMPGWM.Tests.FileLoader.DefinitionDeserializerTests.ConverterProgrammingFailurePropagates
using sMPGWM.Scripts.FileLoadingSystem; // TEST-SUPPORT: INIT-DESERIALIZER-003 | Tests: sMPGWM.Tests.FileLoader.DefinitionDeserializerTests.ConverterProgrammingFailurePropagates

namespace sMPGWM.Tests.FileLoader; // TEST-SUPPORT: INIT-DESERIALIZER-005 | Tests: sMPGWM.Tests.FileLoader.DefinitionDeserializerTests.ConverterProgrammingFailurePropagates

public class DefinitionDeserializerTests // TEST-SUPPORT: INIT-DESERIALIZER-007 | Tests: sMPGWM.Tests.FileLoader.DefinitionDeserializerTests.ConverterProgrammingFailurePropagates
{ // TEST-SUPPORT: INIT-DESERIALIZER-008 | Tests: sMPGWM.Tests.FileLoader.DefinitionDeserializerTests.ConverterProgrammingFailurePropagates
    [Test] // TEST-SUPPORT: INIT-DESERIALIZER-009 | Tests: sMPGWM.Tests.FileLoader.DefinitionDeserializerTests.ConverterProgrammingFailurePropagates
    public void ConverterProgrammingFailurePropagates() // TEST-SUPPORT: INIT-DESERIALIZER-010 | Tests: sMPGWM.Tests.FileLoader.DefinitionDeserializerTests.ConverterProgrammingFailurePropagates
    { // TEST-SUPPORT: INIT-DESERIALIZER-011 | Tests: sMPGWM.Tests.FileLoader.DefinitionDeserializerTests.ConverterProgrammingFailurePropagates
        var error = Assert.Throws<InvalidOperationException>(() => // TEST-SUPPORT: INIT-DESERIALIZER-012 | Tests: sMPGWM.Tests.FileLoader.DefinitionDeserializerTests.ConverterProgrammingFailurePropagates
            DefinitionDeserializer.Deserialize<BrokenDefinition>(new JObject(), "definitions.yml", "schema.yml")); // TEST-SUPPORT: INIT-DESERIALIZER-013 | Tests: sMPGWM.Tests.FileLoader.DefinitionDeserializerTests.ConverterProgrammingFailurePropagates
        Assert.That(error!.Message, Is.EqualTo("converter bug")); // TEST-SUPPORT: INIT-DESERIALIZER-014 | Tests: sMPGWM.Tests.FileLoader.DefinitionDeserializerTests.ConverterProgrammingFailurePropagates
    } // TEST-SUPPORT: INIT-DESERIALIZER-015 | Tests: sMPGWM.Tests.FileLoader.DefinitionDeserializerTests.ConverterProgrammingFailurePropagates

    [JsonConverter(typeof(BrokenConverter))] // TEST-SUPPORT: INIT-DESERIALIZER-017 | Tests: sMPGWM.Tests.FileLoader.DefinitionDeserializerTests.ConverterProgrammingFailurePropagates
    private sealed class BrokenDefinition { } // TEST-SUPPORT: INIT-DESERIALIZER-018 | Tests: sMPGWM.Tests.FileLoader.DefinitionDeserializerTests.ConverterProgrammingFailurePropagates

    private sealed class BrokenConverter : JsonConverter<BrokenDefinition> // TEST-SUPPORT: INIT-DESERIALIZER-020 | Tests: sMPGWM.Tests.FileLoader.DefinitionDeserializerTests.ConverterProgrammingFailurePropagates
    { // TEST-SUPPORT: INIT-DESERIALIZER-021 | Tests: sMPGWM.Tests.FileLoader.DefinitionDeserializerTests.ConverterProgrammingFailurePropagates
        public override BrokenDefinition? ReadJson(JsonReader reader, Type objectType, BrokenDefinition? existingValue, bool hasExistingValue, JsonSerializer serializer) => // TEST-SUPPORT: INIT-DESERIALIZER-022 | Tests: sMPGWM.Tests.FileLoader.DefinitionDeserializerTests.ConverterProgrammingFailurePropagates
            throw new InvalidOperationException("converter bug"); // TEST-SUPPORT: INIT-DESERIALIZER-023 | Tests: sMPGWM.Tests.FileLoader.DefinitionDeserializerTests.ConverterProgrammingFailurePropagates
        public override void WriteJson(JsonWriter writer, BrokenDefinition? value, JsonSerializer serializer) => // TEST-SUPPORT: INIT-DESERIALIZER-024 | Tests: sMPGWM.Tests.FileLoader.DefinitionDeserializerTests.ConverterProgrammingFailurePropagates
            throw new NotSupportedException(); // TEST-SUPPORT: INIT-DESERIALIZER-025 | Tests: sMPGWM.Tests.FileLoader.DefinitionDeserializerTests.ConverterProgrammingFailurePropagates
    } // TEST-SUPPORT: INIT-DESERIALIZER-026 | Tests: sMPGWM.Tests.FileLoader.DefinitionDeserializerTests.ConverterProgrammingFailurePropagates
} // TEST-SUPPORT: INIT-DESERIALIZER-027 | Tests: sMPGWM.Tests.FileLoader.DefinitionDeserializerTests.ConverterProgrammingFailurePropagates

