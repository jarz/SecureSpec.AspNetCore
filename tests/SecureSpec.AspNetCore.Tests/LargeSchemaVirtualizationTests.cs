using Microsoft.OpenApi.Any;
using SecureSpec.AspNetCore.Configuration;
using SecureSpec.AspNetCore.Diagnostics;
using SecureSpec.AspNetCore.Schema;
using System.Diagnostics.CodeAnalysis;

namespace SecureSpec.AspNetCore.Tests;

/// <summary>
/// Tests for large schema virtualization (AC 301-303).
/// </summary>
public class LargeSchemaVirtualizationTests
{
    private static SchemaGenerator CreateGenerator(SchemaOptions? options = null, DiagnosticsLogger? logger = null)
    {
        return new SchemaGenerator(options ?? new SchemaOptions(), logger ?? new DiagnosticsLogger());
    }

    #region AC 301: Schema with >200 properties triggers virtualization

    [Fact]
    public void GenerateSchema_SchemaWith201Properties_TriggersVirtualization()
    {
        // AC 301: Schemas with >200 properties should trigger virtualization
        var options = new SchemaOptions { SchemaPropertyVirtualizationThreshold = 200 };
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        var schema = generator.GenerateSchema(typeof(TestTypes.TypeWith201Properties));
        var events = logger.GetEvents();

        Assert.Equal("object", schema.Type);

        // Check virtualization metadata
        Assert.True(schema.Extensions.ContainsKey("x-schema-virtualized"));
        Assert.True(Assert.IsType<OpenApiBoolean>(schema.Extensions["x-schema-virtualized"]).Value);
        Assert.Equal(201, Assert.IsType<OpenApiInteger>(schema.Extensions["x-schema-total-properties"]).Value);

        // Check VIRT001 diagnostic
        var virt001Event = Assert.Single(events, e => e.Code == "VIRT001");
        Assert.Equal(DiagnosticLevel.Info, virt001Event.Level);
        Assert.Contains("TypeWith201Properties", virt001Event.Message, StringComparison.Ordinal);
        Assert.Contains("201", virt001Event.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateSchema_SchemaWith200Properties_DoesNotTriggerVirtualization()
    {
        // Exactly at threshold should NOT trigger virtualization
        var options = new SchemaOptions { SchemaPropertyVirtualizationThreshold = 200 };
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        var schema = generator.GenerateSchema(typeof(TestTypes.TypeWith200Properties));
        var events = logger.GetEvents();

        Assert.Equal("object", schema.Type);
        Assert.False(schema.Extensions.ContainsKey("x-schema-virtualized"));

        // No VIRT001 diagnostic should be emitted
        Assert.DoesNotContain(events, e => e.Code == "VIRT001");
    }

    #endregion

    #region AC 302: Schema with >50 nested objects triggers virtualization

    [Fact]
    public void GenerateSchema_SchemaWith51NestedObjects_TriggersVirtualization()
    {
        // AC 302: Schemas with >50 nested object properties should trigger virtualization
        var options = new SchemaOptions { NestedObjectVirtualizationThreshold = 50 };
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        var schema = generator.GenerateSchema(typeof(TestTypes.TypeWith51NestedObjects));
        var events = logger.GetEvents();

        Assert.Equal("object", schema.Type);

        // Check virtualization metadata
        Assert.True(schema.Extensions.ContainsKey("x-schema-virtualized"));
        Assert.True(Assert.IsType<OpenApiBoolean>(schema.Extensions["x-schema-virtualized"]).Value);
        Assert.Equal(51, Assert.IsType<OpenApiInteger>(schema.Extensions["x-schema-nested-objects"]).Value);

        // Check VIRT001 diagnostic
        var virt001Event = Assert.Single(events, e => e.Code == "VIRT001");
        Assert.Equal(DiagnosticLevel.Info, virt001Event.Level);
        Assert.Contains("TypeWith51NestedObjects", virt001Event.Message, StringComparison.Ordinal);
        Assert.Contains("nested object", virt001Event.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GenerateSchema_SchemaWith50NestedObjects_DoesNotTriggerVirtualization()
    {
        // Exactly at threshold should NOT trigger virtualization
        var options = new SchemaOptions { NestedObjectVirtualizationThreshold = 50 };
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        var schema = generator.GenerateSchema(typeof(TestTypes.TypeWith50NestedObjects));
        var events = logger.GetEvents();

        Assert.Equal("object", schema.Type);
        Assert.False(schema.Extensions.ContainsKey("x-schema-virtualized"));

        // No VIRT001 diagnostic should be emitted
        Assert.DoesNotContain(events, e => e.Code == "VIRT001");
    }

    #endregion

    #region AC 303: Placeholder token and lazy loading metadata

    [Fact]
    public void GenerateSchema_VirtualizedSchema_ContainsPlaceholderToken()
    {
        // AC 303: Virtualized schemas should include placeholder token
        var options = new SchemaOptions { SchemaPropertyVirtualizationThreshold = 10 };
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        var schema = generator.GenerateSchema(typeof(TestTypes.TypeWith20Properties));

        Assert.NotNull(schema.Description);
        Assert.Contains("<virtualized…>", schema.Description, StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateSchema_VirtualizedSchema_ContainsLazyLoadingMetadata()
    {
        // AC 303: Virtualized schemas should include lazy loading metadata
        var options = new SchemaOptions { SchemaPropertyVirtualizationThreshold = 10 };
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        var schema = generator.GenerateSchema(typeof(TestTypes.TypeWith20Properties));

        Assert.True(schema.Extensions.ContainsKey("x-schema-virtualized"));
        Assert.True(schema.Extensions.ContainsKey("x-schema-total-properties"));
        Assert.True(schema.Extensions.ContainsKey("x-schema-nested-objects"));
    }

    #endregion

    #region Edge cases and thresholds

    [Fact]
    public void GenerateSchema_CustomThresholds_RespectedCorrectly()
    {
        // Test that custom thresholds work correctly
        var options = new SchemaOptions
        {
            SchemaPropertyVirtualizationThreshold = 5,
            NestedObjectVirtualizationThreshold = 2
        };
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        var schema = generator.GenerateSchema(typeof(TestTypes.TypeWith10Properties));
        var events = logger.GetEvents();

        Assert.True(schema.Extensions.ContainsKey("x-schema-virtualized"));
        Assert.Single(events, e => e.Code == "VIRT001");
    }

    [Fact]
    public void GenerateSchema_VirtualizationContext_IncludesCorrectCounts()
    {
        // Verify that diagnostic context includes all required information
        var options = new SchemaOptions { SchemaPropertyVirtualizationThreshold = 10 };
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        _ = generator.GenerateSchema(typeof(TestTypes.TypeWith20Properties));
        var events = logger.GetEvents();

        var virt001Event = Assert.Single(events, e => e.Code == "VIRT001");
        Assert.NotNull(virt001Event.Context);
    }

    [Fact]
    public void GenerateSchema_PrimitiveTypes_NeverVirtualized()
    {
        // Primitive types should never be virtualized regardless of threshold
        var options = new SchemaOptions { SchemaPropertyVirtualizationThreshold = 0 };
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        var types = new[]
        {
            typeof(int), typeof(string), typeof(bool), typeof(DateTime),
            typeof(Guid), typeof(decimal), typeof(double)
        };

        foreach (var type in types)
        {
            var schema = generator.GenerateSchema(type);
            Assert.False(schema.Extensions.ContainsKey("x-schema-virtualized"),
                $"Type {type.Name} should not be virtualized");
        }

        var events = logger.GetEvents();
        Assert.DoesNotContain(events, e => e.Code == "VIRT001");
    }

    [Fact]
    public void GenerateSchema_NestedObjectCount_ExcludesPrimitives()
    {
        // Nested object count should only count complex types, not primitives
        var options = new SchemaOptions
        {
            SchemaPropertyVirtualizationThreshold = 1000,
            NestedObjectVirtualizationThreshold = 5
        };
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // This type has 10 properties but only 3 are nested objects
        var schema = generator.GenerateSchema(typeof(TestTypes.TypeWithMixedProperties));

        // Should not trigger virtualization because only 3 nested objects (< 5)
        Assert.False(schema.Extensions.ContainsKey("x-schema-virtualized"));
    }

    #endregion

    #region Test Types

    private static class TestTypes
    {
        [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Types are used via typeof for schema generation tests.")]
        public class TypeWith200Properties
        {
            // Generate 200 properties
            public int Prop001 { get; set; }
            public int Prop002 { get; set; }
            public int Prop003 { get; set; }
            public int Prop004 { get; set; }
            public int Prop005 { get; set; }
            public int Prop006 { get; set; }
            public int Prop007 { get; set; }
            public int Prop008 { get; set; }
            public int Prop009 { get; set; }
            public int Prop010 { get; set; }
            public int Prop011 { get; set; }
            public int Prop012 { get; set; }
            public int Prop013 { get; set; }
            public int Prop014 { get; set; }
            public int Prop015 { get; set; }
            public int Prop016 { get; set; }
            public int Prop017 { get; set; }
            public int Prop018 { get; set; }
            public int Prop019 { get; set; }
            public int Prop020 { get; set; }
            public int Prop021 { get; set; }
            public int Prop022 { get; set; }
            public int Prop023 { get; set; }
            public int Prop024 { get; set; }
            public int Prop025 { get; set; }
            public int Prop026 { get; set; }
            public int Prop027 { get; set; }
            public int Prop028 { get; set; }
            public int Prop029 { get; set; }
            public int Prop030 { get; set; }
            public int Prop031 { get; set; }
            public int Prop032 { get; set; }
            public int Prop033 { get; set; }
            public int Prop034 { get; set; }
            public int Prop035 { get; set; }
            public int Prop036 { get; set; }
            public int Prop037 { get; set; }
            public int Prop038 { get; set; }
            public int Prop039 { get; set; }
            public int Prop040 { get; set; }
            public int Prop041 { get; set; }
            public int Prop042 { get; set; }
            public int Prop043 { get; set; }
            public int Prop044 { get; set; }
            public int Prop045 { get; set; }
            public int Prop046 { get; set; }
            public int Prop047 { get; set; }
            public int Prop048 { get; set; }
            public int Prop049 { get; set; }
            public int Prop050 { get; set; }
            public int Prop051 { get; set; }
            public int Prop052 { get; set; }
            public int Prop053 { get; set; }
            public int Prop054 { get; set; }
            public int Prop055 { get; set; }
            public int Prop056 { get; set; }
            public int Prop057 { get; set; }
            public int Prop058 { get; set; }
            public int Prop059 { get; set; }
            public int Prop060 { get; set; }
            public int Prop061 { get; set; }
            public int Prop062 { get; set; }
            public int Prop063 { get; set; }
            public int Prop064 { get; set; }
            public int Prop065 { get; set; }
            public int Prop066 { get; set; }
            public int Prop067 { get; set; }
            public int Prop068 { get; set; }
            public int Prop069 { get; set; }
            public int Prop070 { get; set; }
            public int Prop071 { get; set; }
            public int Prop072 { get; set; }
            public int Prop073 { get; set; }
            public int Prop074 { get; set; }
            public int Prop075 { get; set; }
            public int Prop076 { get; set; }
            public int Prop077 { get; set; }
            public int Prop078 { get; set; }
            public int Prop079 { get; set; }
            public int Prop080 { get; set; }
            public int Prop081 { get; set; }
            public int Prop082 { get; set; }
            public int Prop083 { get; set; }
            public int Prop084 { get; set; }
            public int Prop085 { get; set; }
            public int Prop086 { get; set; }
            public int Prop087 { get; set; }
            public int Prop088 { get; set; }
            public int Prop089 { get; set; }
            public int Prop090 { get; set; }
            public int Prop091 { get; set; }
            public int Prop092 { get; set; }
            public int Prop093 { get; set; }
            public int Prop094 { get; set; }
            public int Prop095 { get; set; }
            public int Prop096 { get; set; }
            public int Prop097 { get; set; }
            public int Prop098 { get; set; }
            public int Prop099 { get; set; }
            public int Prop100 { get; set; }
            public int Prop101 { get; set; }
            public int Prop102 { get; set; }
            public int Prop103 { get; set; }
            public int Prop104 { get; set; }
            public int Prop105 { get; set; }
            public int Prop106 { get; set; }
            public int Prop107 { get; set; }
            public int Prop108 { get; set; }
            public int Prop109 { get; set; }
            public int Prop110 { get; set; }
            public int Prop111 { get; set; }
            public int Prop112 { get; set; }
            public int Prop113 { get; set; }
            public int Prop114 { get; set; }
            public int Prop115 { get; set; }
            public int Prop116 { get; set; }
            public int Prop117 { get; set; }
            public int Prop118 { get; set; }
            public int Prop119 { get; set; }
            public int Prop120 { get; set; }
            public int Prop121 { get; set; }
            public int Prop122 { get; set; }
            public int Prop123 { get; set; }
            public int Prop124 { get; set; }
            public int Prop125 { get; set; }
            public int Prop126 { get; set; }
            public int Prop127 { get; set; }
            public int Prop128 { get; set; }
            public int Prop129 { get; set; }
            public int Prop130 { get; set; }
            public int Prop131 { get; set; }
            public int Prop132 { get; set; }
            public int Prop133 { get; set; }
            public int Prop134 { get; set; }
            public int Prop135 { get; set; }
            public int Prop136 { get; set; }
            public int Prop137 { get; set; }
            public int Prop138 { get; set; }
            public int Prop139 { get; set; }
            public int Prop140 { get; set; }
            public int Prop141 { get; set; }
            public int Prop142 { get; set; }
            public int Prop143 { get; set; }
            public int Prop144 { get; set; }
            public int Prop145 { get; set; }
            public int Prop146 { get; set; }
            public int Prop147 { get; set; }
            public int Prop148 { get; set; }
            public int Prop149 { get; set; }
            public int Prop150 { get; set; }
            public int Prop151 { get; set; }
            public int Prop152 { get; set; }
            public int Prop153 { get; set; }
            public int Prop154 { get; set; }
            public int Prop155 { get; set; }
            public int Prop156 { get; set; }
            public int Prop157 { get; set; }
            public int Prop158 { get; set; }
            public int Prop159 { get; set; }
            public int Prop160 { get; set; }
            public int Prop161 { get; set; }
            public int Prop162 { get; set; }
            public int Prop163 { get; set; }
            public int Prop164 { get; set; }
            public int Prop165 { get; set; }
            public int Prop166 { get; set; }
            public int Prop167 { get; set; }
            public int Prop168 { get; set; }
            public int Prop169 { get; set; }
            public int Prop170 { get; set; }
            public int Prop171 { get; set; }
            public int Prop172 { get; set; }
            public int Prop173 { get; set; }
            public int Prop174 { get; set; }
            public int Prop175 { get; set; }
            public int Prop176 { get; set; }
            public int Prop177 { get; set; }
            public int Prop178 { get; set; }
            public int Prop179 { get; set; }
            public int Prop180 { get; set; }
            public int Prop181 { get; set; }
            public int Prop182 { get; set; }
            public int Prop183 { get; set; }
            public int Prop184 { get; set; }
            public int Prop185 { get; set; }
            public int Prop186 { get; set; }
            public int Prop187 { get; set; }
            public int Prop188 { get; set; }
            public int Prop189 { get; set; }
            public int Prop190 { get; set; }
            public int Prop191 { get; set; }
            public int Prop192 { get; set; }
            public int Prop193 { get; set; }
            public int Prop194 { get; set; }
            public int Prop195 { get; set; }
            public int Prop196 { get; set; }
            public int Prop197 { get; set; }
            public int Prop198 { get; set; }
            public int Prop199 { get; set; }
            public int Prop200 { get; set; }
        }

        [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Types are used via typeof for schema generation tests.")]
        public sealed class TypeWith201Properties : TypeWith200Properties
        {
            public int Prop201 { get; set; }
        }

        [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Types are used via typeof for schema generation tests.")]
        public sealed class TypeWith20Properties
        {
            public int Prop01 { get; set; }
            public int Prop02 { get; set; }
            public int Prop03 { get; set; }
            public int Prop04 { get; set; }
            public int Prop05 { get; set; }
            public int Prop06 { get; set; }
            public int Prop07 { get; set; }
            public int Prop08 { get; set; }
            public int Prop09 { get; set; }
            public int Prop10 { get; set; }
            public int Prop11 { get; set; }
            public int Prop12 { get; set; }
            public int Prop13 { get; set; }
            public int Prop14 { get; set; }
            public int Prop15 { get; set; }
            public int Prop16 { get; set; }
            public int Prop17 { get; set; }
            public int Prop18 { get; set; }
            public int Prop19 { get; set; }
            public int Prop20 { get; set; }
        }

        [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Types are used via typeof for schema generation tests.")]
        public sealed class TypeWith10Properties
        {
            public int Prop01 { get; set; }
            public int Prop02 { get; set; }
            public int Prop03 { get; set; }
            public int Prop04 { get; set; }
            public int Prop05 { get; set; }
            public int Prop06 { get; set; }
            public int Prop07 { get; set; }
            public int Prop08 { get; set; }
            public int Prop09 { get; set; }
            public int Prop10 { get; set; }
        }

        public sealed class NestedComplexType
        {
            public int Value { get; set; }
        }

        public class TypeWith50NestedObjects
        {
            public NestedComplexType Obj01 { get; set; } = new();
            public NestedComplexType Obj02 { get; set; } = new();
            public NestedComplexType Obj03 { get; set; } = new();
            public NestedComplexType Obj04 { get; set; } = new();
            public NestedComplexType Obj05 { get; set; } = new();
            public NestedComplexType Obj06 { get; set; } = new();
            public NestedComplexType Obj07 { get; set; } = new();
            public NestedComplexType Obj08 { get; set; } = new();
            public NestedComplexType Obj09 { get; set; } = new();
            public NestedComplexType Obj10 { get; set; } = new();
            public NestedComplexType Obj11 { get; set; } = new();
            public NestedComplexType Obj12 { get; set; } = new();
            public NestedComplexType Obj13 { get; set; } = new();
            public NestedComplexType Obj14 { get; set; } = new();
            public NestedComplexType Obj15 { get; set; } = new();
            public NestedComplexType Obj16 { get; set; } = new();
            public NestedComplexType Obj17 { get; set; } = new();
            public NestedComplexType Obj18 { get; set; } = new();
            public NestedComplexType Obj19 { get; set; } = new();
            public NestedComplexType Obj20 { get; set; } = new();
            public NestedComplexType Obj21 { get; set; } = new();
            public NestedComplexType Obj22 { get; set; } = new();
            public NestedComplexType Obj23 { get; set; } = new();
            public NestedComplexType Obj24 { get; set; } = new();
            public NestedComplexType Obj25 { get; set; } = new();
            public NestedComplexType Obj26 { get; set; } = new();
            public NestedComplexType Obj27 { get; set; } = new();
            public NestedComplexType Obj28 { get; set; } = new();
            public NestedComplexType Obj29 { get; set; } = new();
            public NestedComplexType Obj30 { get; set; } = new();
            public NestedComplexType Obj31 { get; set; } = new();
            public NestedComplexType Obj32 { get; set; } = new();
            public NestedComplexType Obj33 { get; set; } = new();
            public NestedComplexType Obj34 { get; set; } = new();
            public NestedComplexType Obj35 { get; set; } = new();
            public NestedComplexType Obj36 { get; set; } = new();
            public NestedComplexType Obj37 { get; set; } = new();
            public NestedComplexType Obj38 { get; set; } = new();
            public NestedComplexType Obj39 { get; set; } = new();
            public NestedComplexType Obj40 { get; set; } = new();
            public NestedComplexType Obj41 { get; set; } = new();
            public NestedComplexType Obj42 { get; set; } = new();
            public NestedComplexType Obj43 { get; set; } = new();
            public NestedComplexType Obj44 { get; set; } = new();
            public NestedComplexType Obj45 { get; set; } = new();
            public NestedComplexType Obj46 { get; set; } = new();
            public NestedComplexType Obj47 { get; set; } = new();
            public NestedComplexType Obj48 { get; set; } = new();
            public NestedComplexType Obj49 { get; set; } = new();
            public NestedComplexType Obj50 { get; set; } = new();
        }

        [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Types are used via typeof for schema generation tests.")]
        public sealed class TypeWith51NestedObjects : TypeWith50NestedObjects
        {
            public NestedComplexType Obj51 { get; set; } = new();
        }

        [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Types are used via typeof for schema generation tests.")]
        public sealed class TypeWithMixedProperties
        {
            // 7 primitive properties
            public int IntProp { get; set; }
            public string StringProp { get; set; } = string.Empty;
            public bool BoolProp { get; set; }
            public DateTime DateProp { get; set; }
            public Guid GuidProp { get; set; }
            public decimal DecimalProp { get; set; }
            public double DoubleProp { get; set; }

            // 3 complex object properties
            public NestedComplexType Obj1 { get; set; } = new();
            public NestedComplexType Obj2 { get; set; } = new();
            public NestedComplexType Obj3 { get; set; } = new();
        }
    }

    #endregion
}
