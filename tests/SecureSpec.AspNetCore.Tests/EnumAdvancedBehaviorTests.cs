using Microsoft.OpenApi.Any;
using SecureSpec.AspNetCore.Configuration;
using SecureSpec.AspNetCore.Diagnostics;
using SecureSpec.AspNetCore.Schema;

namespace SecureSpec.AspNetCore.Tests;

/// <summary>
/// Tests for enum advanced behavior (AC 438-443).
/// </summary>
public class EnumAdvancedBehaviorTests
{
    private static SchemaGenerator CreateGenerator(SchemaOptions? options = null, DiagnosticsLogger? logger = null)
    {
        return new SchemaGenerator(options ?? new SchemaOptions(), logger ?? new DiagnosticsLogger());
    }

    #region AC 438: Enum declaration order stable across rebuilds

    [Fact]
    public void GenerateSchema_EnumStringMode_PreservesDeclarationOrder()
    {
        // AC 438: Declaration order must be stable
        var options = new SchemaOptions { UseEnumStrings = true };
        var schema = CreateGenerator(options).GenerateSchema(typeof(TestEnums.OrderedEnum));

        Assert.Equal("string", schema.Type);
        Assert.Equal(5, schema.Enum.Count);
        Assert.Equal("First", Assert.IsType<OpenApiString>(schema.Enum[0]).Value);
        Assert.Equal("Second", Assert.IsType<OpenApiString>(schema.Enum[1]).Value);
        Assert.Equal("Third", Assert.IsType<OpenApiString>(schema.Enum[2]).Value);
        Assert.Equal("Fourth", Assert.IsType<OpenApiString>(schema.Enum[3]).Value);
        Assert.Equal("Fifth", Assert.IsType<OpenApiString>(schema.Enum[4]).Value);
    }

    [Fact]
    public void GenerateSchema_EnumIntegerMode_PreservesDeclarationOrder()
    {
        // AC 438: Declaration order must be stable in integer mode too
        var options = new SchemaOptions { UseEnumStrings = false };
        var schema = CreateGenerator(options).GenerateSchema(typeof(TestEnums.OrderedEnum));

        Assert.Equal("integer", schema.Type);
        Assert.Equal(5, schema.Enum.Count);
        Assert.Equal(0, Assert.IsType<OpenApiInteger>(schema.Enum[0]).Value);
        Assert.Equal(1, Assert.IsType<OpenApiInteger>(schema.Enum[1]).Value);
        Assert.Equal(2, Assert.IsType<OpenApiInteger>(schema.Enum[2]).Value);
        Assert.Equal(3, Assert.IsType<OpenApiInteger>(schema.Enum[3]).Value);
        Assert.Equal(4, Assert.IsType<OpenApiInteger>(schema.Enum[4]).Value);
    }

    #endregion

    #region AC 439: Enum switching integer→string toggles representation

    [Fact]
    public void GenerateSchema_EnumToggleStringToInteger_ChangesRepresentation()
    {
        // AC 439: Switching modes should toggle representation
        var stringOptions = new SchemaOptions { UseEnumStrings = true };
        var integerOptions = new SchemaOptions { UseEnumStrings = false };
        var enumType = typeof(TestEnums.OrderedEnum);

        var stringSchema = CreateGenerator(stringOptions).GenerateSchema(enumType);
        var integerSchema = CreateGenerator(integerOptions).GenerateSchema(enumType);

        // String mode
        Assert.Equal("string", stringSchema.Type);
        Assert.Null(stringSchema.Format);
        Assert.Equal("First", Assert.IsType<OpenApiString>(stringSchema.Enum[0]).Value);

        // Integer mode
        Assert.Equal("integer", integerSchema.Type);
        Assert.Equal("int32", integerSchema.Format);
        Assert.Equal(0, Assert.IsType<OpenApiInteger>(integerSchema.Enum[0]).Value);
    }

    #endregion

    #region AC 440: Enum >10K triggers virtualization + VIRT001 diagnostic

    [Fact]
    public void GenerateSchema_LargeEnumStringMode_TriggersVirtualization()
    {
        // AC 440: Large enums should trigger virtualization
        var options = new SchemaOptions { UseEnumStrings = true, EnumVirtualizationThreshold = 100 };
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        var schema = generator.GenerateSchema(typeof(TestEnums.LargeEnum));
        var events = logger.GetEvents();

        Assert.Equal("string", schema.Type);
        Assert.Equal(100, schema.Enum.Count); // Truncated to threshold

        // AC 440: VIRT001 diagnostic should be emitted
        var virt001Event = Assert.Single(events, e => e.Code == "VIRT001");
        Assert.Equal(DiagnosticLevel.Info, virt001Event.Level);
        Assert.Contains("LargeEnum", virt001Event.Message, StringComparison.Ordinal);
        Assert.Contains("200", virt001Event.Message, StringComparison.Ordinal); // Total count

        // AC 441: Virtualization metadata should be present
        Assert.True(schema.Extensions.ContainsKey("x-enum-virtualized"));
        Assert.True(Assert.IsType<OpenApiBoolean>(schema.Extensions["x-enum-virtualized"]).Value);
        Assert.Equal(200, Assert.IsType<OpenApiInteger>(schema.Extensions["x-enum-total-count"]).Value);
        Assert.Equal(100, Assert.IsType<OpenApiInteger>(schema.Extensions["x-enum-truncated-count"]).Value);
    }

    [Fact]
    public void GenerateSchema_LargeEnumIntegerMode_TriggersVirtualization()
    {
        // AC 440: Large enums should trigger virtualization in integer mode too
        var options = new SchemaOptions { UseEnumStrings = false, EnumVirtualizationThreshold = 100 };
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        var schema = generator.GenerateSchema(typeof(TestEnums.LargeEnum));
        var events = logger.GetEvents();

        Assert.Equal("integer", schema.Type);
        Assert.Equal(100, schema.Enum.Count); // Truncated to threshold

        // AC 440: VIRT001 diagnostic should be emitted
        var virt001Event = Assert.Single(events, e => e.Code == "VIRT001");
        Assert.Equal(DiagnosticLevel.Info, virt001Event.Level);

        // AC 441: Virtualization metadata should be present
        Assert.True(schema.Extensions.ContainsKey("x-enum-virtualized"));
        Assert.Equal(200, Assert.IsType<OpenApiInteger>(schema.Extensions["x-enum-total-count"]).Value);
    }

    [Fact]
    public void GenerateSchema_SmallEnum_DoesNotTriggerVirtualization()
    {
        // Enums below threshold should not be virtualized
        var options = new SchemaOptions { UseEnumStrings = true, EnumVirtualizationThreshold = 100 };
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        var schema = generator.GenerateSchema(typeof(TestEnums.OrderedEnum));
        var events = logger.GetEvents();

        Assert.Equal(5, schema.Enum.Count); // All values present
        Assert.Empty(events); // No VIRT001 diagnostic
        Assert.False(schema.Extensions.ContainsKey("x-enum-virtualized"));
    }

    [Fact]
    public void GenerateSchema_ExactThresholdEnum_DoesNotTriggerVirtualization()
    {
        // Exactly at threshold should not trigger virtualization
        var options = new SchemaOptions { UseEnumStrings = true, EnumVirtualizationThreshold = 5 };
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        var schema = generator.GenerateSchema(typeof(TestEnums.OrderedEnum));
        var events = logger.GetEvents();

        Assert.Equal(5, schema.Enum.Count); // All values present
        Assert.Empty(events); // No VIRT001 diagnostic
        Assert.False(schema.Extensions.ContainsKey("x-enum-virtualized"));
    }

    [Fact]
    public void GenerateSchema_JustOverThresholdEnum_TriggersVirtualization()
    {
        // Just over threshold should trigger virtualization
        var options = new SchemaOptions { UseEnumStrings = true, EnumVirtualizationThreshold = 4 };
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        var schema = generator.GenerateSchema(typeof(TestEnums.OrderedEnum));
        var events = logger.GetEvents();

        Assert.Equal(4, schema.Enum.Count); // Truncated
        Assert.Single(events, e => e.Code == "VIRT001");
        Assert.True(schema.Extensions.ContainsKey("x-enum-virtualized"));
    }

    #endregion

    #region AC 441: Enum search returns results across virtualized segments

    [Fact]
    public void GenerateSchema_VirtualizedEnum_ContainsSearchMetadata()
    {
        // AC 441: Virtualized enums should have metadata to support search
        var options = new SchemaOptions { UseEnumStrings = true, EnumVirtualizationThreshold = 50 };
        var generator = CreateGenerator(options);

        var schema = generator.GenerateSchema(typeof(TestEnums.LargeEnum));

        // Metadata for UI to implement search across all values
        Assert.True(schema.Extensions.ContainsKey("x-enum-virtualized"));
        Assert.True(schema.Extensions.ContainsKey("x-enum-total-count"));
        Assert.True(schema.Extensions.ContainsKey("x-enum-truncated-count"));

        var totalCount = Assert.IsType<OpenApiInteger>(schema.Extensions["x-enum-total-count"]).Value;
        var truncatedCount = Assert.IsType<OpenApiInteger>(schema.Extensions["x-enum-truncated-count"]).Value;

        Assert.Equal(200, totalCount);
        Assert.Equal(150, truncatedCount);
    }

    #endregion

    #region AC 442: Enum naming policy modifies emitted value casing

    [Fact]
    public void GenerateSchema_WithNamingPolicy_ModifiesEnumValues()
    {
        // AC 442: Naming policy should modify emitted values
        var options = new SchemaOptions
        {
            UseEnumStrings = true,
            EnumNamingPolicy = name => name.ToUpperInvariant()
        };
        var schema = CreateGenerator(options).GenerateSchema(typeof(TestEnums.OrderedEnum));

        Assert.Equal("string", schema.Type);
        Assert.Equal("FIRST", Assert.IsType<OpenApiString>(schema.Enum[0]).Value);
        Assert.Equal("SECOND", Assert.IsType<OpenApiString>(schema.Enum[1]).Value);
        Assert.Equal("THIRD", Assert.IsType<OpenApiString>(schema.Enum[2]).Value);
    }

    [Fact]
    public void GenerateSchema_WithNamingPolicy_PreservesOrder()
    {
        // AC 442: Naming policy should not reorder values
        // Note: We use custom policy to demonstrate flexibility (CA1308 suppressed for test purposes)
#pragma warning disable CA1308 // Normalize strings to uppercase
        var options = new SchemaOptions
        {
            UseEnumStrings = true,
            EnumNamingPolicy = name => name.ToLowerInvariant()
        };
#pragma warning restore CA1308 // Normalize strings to uppercase
        var schema = CreateGenerator(options).GenerateSchema(typeof(TestEnums.OrderedEnum));

        Assert.Equal("first", Assert.IsType<OpenApiString>(schema.Enum[0]).Value);
        Assert.Equal("second", Assert.IsType<OpenApiString>(schema.Enum[1]).Value);
        Assert.Equal("third", Assert.IsType<OpenApiString>(schema.Enum[2]).Value);
        Assert.Equal("fourth", Assert.IsType<OpenApiString>(schema.Enum[3]).Value);
        Assert.Equal("fifth", Assert.IsType<OpenApiString>(schema.Enum[4]).Value);
    }

    [Fact]
    public void GenerateSchema_WithNamingPolicyAndVirtualization_WorksTogether()
    {
        // AC 442: Naming policy should work with virtualization
        var options = new SchemaOptions
        {
            UseEnumStrings = true,
            EnumNamingPolicy = name => name.ToUpperInvariant(),
            EnumVirtualizationThreshold = 50
        };
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        var schema = generator.GenerateSchema(typeof(TestEnums.LargeEnum));
        var events = logger.GetEvents();

        Assert.Equal(50, schema.Enum.Count);
        Assert.Equal("VALUE_0", Assert.IsType<OpenApiString>(schema.Enum[0]).Value);
        Assert.Single(events, e => e.Code == "VIRT001");
    }

    #endregion

    #region AC 443: Enum nullable adds "null" union in 3.1 only

    [Fact]
    public void GenerateSchema_NullableEnum_OpenApi30_UsesNullableProperty()
    {
        // AC 443: OpenAPI 3.0 uses nullable property
        var options = new SchemaOptions
        {
            UseEnumStrings = true,
            SpecVersion = SchemaSpecVersion.OpenApi3_0
        };
        var schema = CreateGenerator(options).GenerateSchema(typeof(TestEnums.OrderedEnum?));

        Assert.Equal("string", schema.Type);
        Assert.True(schema.Nullable);
        Assert.Empty(schema.AnyOf);
        Assert.Empty(schema.OneOf);
    }

    [Fact]
    public void GenerateSchema_NullableEnum_OpenApi31_UsesNullUnion()
    {
        // AC 443: OpenAPI 3.1 uses type union with null
        var options = new SchemaOptions
        {
            UseEnumStrings = true,
            SpecVersion = SchemaSpecVersion.OpenApi3_1
        };
        var schema = CreateGenerator(options).GenerateSchema(typeof(TestEnums.OrderedEnum?));

        // OpenAPI 3.1 uses AnyOf with null schema
        Assert.Equal(2, schema.AnyOf.Count);

        var enumSchema = schema.AnyOf[0];
        Assert.Equal("string", enumSchema.Type);
        Assert.Equal(5, enumSchema.Enum.Count);

        var nullSchema = schema.AnyOf[1];
        Assert.Equal("null", nullSchema.Type);
    }

    [Fact]
    public void GenerateSchema_NullableEnumIntegerMode_OpenApi31_UsesNullUnion()
    {
        // AC 443: Integer mode should also support null union in OpenAPI 3.1
        var options = new SchemaOptions
        {
            UseEnumStrings = false,
            SpecVersion = SchemaSpecVersion.OpenApi3_1
        };
        var schema = CreateGenerator(options).GenerateSchema(typeof(TestEnums.OrderedEnum?));

        Assert.Equal(2, schema.AnyOf.Count);

        var enumSchema = schema.AnyOf[0];
        Assert.Equal("integer", enumSchema.Type);
        Assert.Equal("int32", enumSchema.Format);

        var nullSchema = schema.AnyOf[1];
        Assert.Equal("null", nullSchema.Type);
    }

    [Fact]
    public void GenerateSchema_NonNullableEnum_OpenApi31_NoNullUnion()
    {
        // Non-nullable enums should not have null union
        var options = new SchemaOptions
        {
            UseEnumStrings = true,
            SpecVersion = SchemaSpecVersion.OpenApi3_1
        };
        var schema = CreateGenerator(options).GenerateSchema(typeof(TestEnums.OrderedEnum));

        Assert.Equal("string", schema.Type);
        Assert.False(schema.Nullable);
        Assert.Empty(schema.AnyOf);
        Assert.Empty(schema.OneOf);
    }

    #endregion

    #region Test Enums

    private static class TestEnums
    {
        internal enum OrderedEnum
        {
            First = 0,
            Second = 1,
            Third = 2,
            Fourth = 3,
            Fifth = 4
        }

        internal enum LargeEnum
        {
            Value_0 = 0, Value_1 = 1, Value_2 = 2, Value_3 = 3, Value_4 = 4,
            Value_5 = 5, Value_6 = 6, Value_7 = 7, Value_8 = 8, Value_9 = 9,
            Value_10 = 10, Value_11 = 11, Value_12 = 12, Value_13 = 13, Value_14 = 14,
            Value_15 = 15, Value_16 = 16, Value_17 = 17, Value_18 = 18, Value_19 = 19,
            Value_20 = 20, Value_21 = 21, Value_22 = 22, Value_23 = 23, Value_24 = 24,
            Value_25 = 25, Value_26 = 26, Value_27 = 27, Value_28 = 28, Value_29 = 29,
            Value_30 = 30, Value_31 = 31, Value_32 = 32, Value_33 = 33, Value_34 = 34,
            Value_35 = 35, Value_36 = 36, Value_37 = 37, Value_38 = 38, Value_39 = 39,
            Value_40 = 40, Value_41 = 41, Value_42 = 42, Value_43 = 43, Value_44 = 44,
            Value_45 = 45, Value_46 = 46, Value_47 = 47, Value_48 = 48, Value_49 = 49,
            Value_50 = 50, Value_51 = 51, Value_52 = 52, Value_53 = 53, Value_54 = 54,
            Value_55 = 55, Value_56 = 56, Value_57 = 57, Value_58 = 58, Value_59 = 59,
            Value_60 = 60, Value_61 = 61, Value_62 = 62, Value_63 = 63, Value_64 = 64,
            Value_65 = 65, Value_66 = 66, Value_67 = 67, Value_68 = 68, Value_69 = 69,
            Value_70 = 70, Value_71 = 71, Value_72 = 72, Value_73 = 73, Value_74 = 74,
            Value_75 = 75, Value_76 = 76, Value_77 = 77, Value_78 = 78, Value_79 = 79,
            Value_80 = 80, Value_81 = 81, Value_82 = 82, Value_83 = 83, Value_84 = 84,
            Value_85 = 85, Value_86 = 86, Value_87 = 87, Value_88 = 88, Value_89 = 89,
            Value_90 = 90, Value_91 = 91, Value_92 = 92, Value_93 = 93, Value_94 = 94,
            Value_95 = 95, Value_96 = 96, Value_97 = 97, Value_98 = 98, Value_99 = 99,
            Value_100 = 100, Value_101 = 101, Value_102 = 102, Value_103 = 103, Value_104 = 104,
            Value_105 = 105, Value_106 = 106, Value_107 = 107, Value_108 = 108, Value_109 = 109,
            Value_110 = 110, Value_111 = 111, Value_112 = 112, Value_113 = 113, Value_114 = 114,
            Value_115 = 115, Value_116 = 116, Value_117 = 117, Value_118 = 118, Value_119 = 119,
            Value_120 = 120, Value_121 = 121, Value_122 = 122, Value_123 = 123, Value_124 = 124,
            Value_125 = 125, Value_126 = 126, Value_127 = 127, Value_128 = 128, Value_129 = 129,
            Value_130 = 130, Value_131 = 131, Value_132 = 132, Value_133 = 133, Value_134 = 134,
            Value_135 = 135, Value_136 = 136, Value_137 = 137, Value_138 = 138, Value_139 = 139,
            Value_140 = 140, Value_141 = 141, Value_142 = 142, Value_143 = 143, Value_144 = 144,
            Value_145 = 145, Value_146 = 146, Value_147 = 147, Value_148 = 148, Value_149 = 149,
            Value_150 = 150, Value_151 = 151, Value_152 = 152, Value_153 = 153, Value_154 = 154,
            Value_155 = 155, Value_156 = 156, Value_157 = 157, Value_158 = 158, Value_159 = 159,
            Value_160 = 160, Value_161 = 161, Value_162 = 162, Value_163 = 163, Value_164 = 164,
            Value_165 = 165, Value_166 = 166, Value_167 = 167, Value_168 = 168, Value_169 = 169,
            Value_170 = 170, Value_171 = 171, Value_172 = 172, Value_173 = 173, Value_174 = 174,
            Value_175 = 175, Value_176 = 176, Value_177 = 177, Value_178 = 178, Value_179 = 179,
            Value_180 = 180, Value_181 = 181, Value_182 = 182, Value_183 = 183, Value_184 = 184,
            Value_185 = 185, Value_186 = 186, Value_187 = 187, Value_188 = 188, Value_189 = 189,
            Value_190 = 190, Value_191 = 191, Value_192 = 192, Value_193 = 193, Value_194 = 194,
            Value_195 = 195, Value_196 = 196, Value_197 = 197, Value_198 = 198, Value_199 = 199
        }
    }

    #endregion
}
