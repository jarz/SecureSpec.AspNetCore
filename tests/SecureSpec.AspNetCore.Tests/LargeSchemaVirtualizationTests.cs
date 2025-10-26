#pragma warning disable IDE0005 // Using directive is unnecessary - false positive for OpenApi types and Xunit
using System.Diagnostics.CodeAnalysis;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using SecureSpec.AspNetCore.Configuration;
using SecureSpec.AspNetCore.Diagnostics;
using SecureSpec.AspNetCore.Schema;
using Xunit;
#pragma warning restore IDE0005

namespace SecureSpec.AspNetCore.Tests;

/// <summary>
/// Tests for Phase 4: Large Schema Virtualization (AC 301-303).
/// </summary>
[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Test types are internal and used only for schema generation testing.")]
public class LargeSchemaVirtualizationTests
{
    #region Test Types

    // Type with exactly 200 properties (at threshold)
    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Types are used via typeof for schema generation scenarios.")]
    internal sealed class TypeWith200Properties
    {
        public string Property1 { get; set; } = string.Empty;
        public string Property2 { get; set; } = string.Empty;
        public string Property3 { get; set; } = string.Empty;
        public string Property4 { get; set; } = string.Empty;
        public string Property5 { get; set; } = string.Empty;
        public string Property6 { get; set; } = string.Empty;
        public string Property7 { get; set; } = string.Empty;
        public string Property8 { get; set; } = string.Empty;
        public string Property9 { get; set; } = string.Empty;
        public string Property10 { get; set; } = string.Empty;
        public string Property11 { get; set; } = string.Empty;
        public string Property12 { get; set; } = string.Empty;
        public string Property13 { get; set; } = string.Empty;
        public string Property14 { get; set; } = string.Empty;
        public string Property15 { get; set; } = string.Empty;
        public string Property16 { get; set; } = string.Empty;
        public string Property17 { get; set; } = string.Empty;
        public string Property18 { get; set; } = string.Empty;
        public string Property19 { get; set; } = string.Empty;
        public string Property20 { get; set; } = string.Empty;
        public string Property21 { get; set; } = string.Empty;
        public string Property22 { get; set; } = string.Empty;
        public string Property23 { get; set; } = string.Empty;
        public string Property24 { get; set; } = string.Empty;
        public string Property25 { get; set; } = string.Empty;
        public string Property26 { get; set; } = string.Empty;
        public string Property27 { get; set; } = string.Empty;
        public string Property28 { get; set; } = string.Empty;
        public string Property29 { get; set; } = string.Empty;
        public string Property30 { get; set; } = string.Empty;
        public string Property31 { get; set; } = string.Empty;
        public string Property32 { get; set; } = string.Empty;
        public string Property33 { get; set; } = string.Empty;
        public string Property34 { get; set; } = string.Empty;
        public string Property35 { get; set; } = string.Empty;
        public string Property36 { get; set; } = string.Empty;
        public string Property37 { get; set; } = string.Empty;
        public string Property38 { get; set; } = string.Empty;
        public string Property39 { get; set; } = string.Empty;
        public string Property40 { get; set; } = string.Empty;
        public string Property41 { get; set; } = string.Empty;
        public string Property42 { get; set; } = string.Empty;
        public string Property43 { get; set; } = string.Empty;
        public string Property44 { get; set; } = string.Empty;
        public string Property45 { get; set; } = string.Empty;
        public string Property46 { get; set; } = string.Empty;
        public string Property47 { get; set; } = string.Empty;
        public string Property48 { get; set; } = string.Empty;
        public string Property49 { get; set; } = string.Empty;
        public string Property50 { get; set; } = string.Empty;
        public string Property51 { get; set; } = string.Empty;
        public string Property52 { get; set; } = string.Empty;
        public string Property53 { get; set; } = string.Empty;
        public string Property54 { get; set; } = string.Empty;
        public string Property55 { get; set; } = string.Empty;
        public string Property56 { get; set; } = string.Empty;
        public string Property57 { get; set; } = string.Empty;
        public string Property58 { get; set; } = string.Empty;
        public string Property59 { get; set; } = string.Empty;
        public string Property60 { get; set; } = string.Empty;
        public string Property61 { get; set; } = string.Empty;
        public string Property62 { get; set; } = string.Empty;
        public string Property63 { get; set; } = string.Empty;
        public string Property64 { get; set; } = string.Empty;
        public string Property65 { get; set; } = string.Empty;
        public string Property66 { get; set; } = string.Empty;
        public string Property67 { get; set; } = string.Empty;
        public string Property68 { get; set; } = string.Empty;
        public string Property69 { get; set; } = string.Empty;
        public string Property70 { get; set; } = string.Empty;
        public string Property71 { get; set; } = string.Empty;
        public string Property72 { get; set; } = string.Empty;
        public string Property73 { get; set; } = string.Empty;
        public string Property74 { get; set; } = string.Empty;
        public string Property75 { get; set; } = string.Empty;
        public string Property76 { get; set; } = string.Empty;
        public string Property77 { get; set; } = string.Empty;
        public string Property78 { get; set; } = string.Empty;
        public string Property79 { get; set; } = string.Empty;
        public string Property80 { get; set; } = string.Empty;
        public string Property81 { get; set; } = string.Empty;
        public string Property82 { get; set; } = string.Empty;
        public string Property83 { get; set; } = string.Empty;
        public string Property84 { get; set; } = string.Empty;
        public string Property85 { get; set; } = string.Empty;
        public string Property86 { get; set; } = string.Empty;
        public string Property87 { get; set; } = string.Empty;
        public string Property88 { get; set; } = string.Empty;
        public string Property89 { get; set; } = string.Empty;
        public string Property90 { get; set; } = string.Empty;
        public string Property91 { get; set; } = string.Empty;
        public string Property92 { get; set; } = string.Empty;
        public string Property93 { get; set; } = string.Empty;
        public string Property94 { get; set; } = string.Empty;
        public string Property95 { get; set; } = string.Empty;
        public string Property96 { get; set; } = string.Empty;
        public string Property97 { get; set; } = string.Empty;
        public string Property98 { get; set; } = string.Empty;
        public string Property99 { get; set; } = string.Empty;
        public string Property100 { get; set; } = string.Empty;
        public string Property101 { get; set; } = string.Empty;
        public string Property102 { get; set; } = string.Empty;
        public string Property103 { get; set; } = string.Empty;
        public string Property104 { get; set; } = string.Empty;
        public string Property105 { get; set; } = string.Empty;
        public string Property106 { get; set; } = string.Empty;
        public string Property107 { get; set; } = string.Empty;
        public string Property108 { get; set; } = string.Empty;
        public string Property109 { get; set; } = string.Empty;
        public string Property110 { get; set; } = string.Empty;
        public string Property111 { get; set; } = string.Empty;
        public string Property112 { get; set; } = string.Empty;
        public string Property113 { get; set; } = string.Empty;
        public string Property114 { get; set; } = string.Empty;
        public string Property115 { get; set; } = string.Empty;
        public string Property116 { get; set; } = string.Empty;
        public string Property117 { get; set; } = string.Empty;
        public string Property118 { get; set; } = string.Empty;
        public string Property119 { get; set; } = string.Empty;
        public string Property120 { get; set; } = string.Empty;
        public string Property121 { get; set; } = string.Empty;
        public string Property122 { get; set; } = string.Empty;
        public string Property123 { get; set; } = string.Empty;
        public string Property124 { get; set; } = string.Empty;
        public string Property125 { get; set; } = string.Empty;
        public string Property126 { get; set; } = string.Empty;
        public string Property127 { get; set; } = string.Empty;
        public string Property128 { get; set; } = string.Empty;
        public string Property129 { get; set; } = string.Empty;
        public string Property130 { get; set; } = string.Empty;
        public string Property131 { get; set; } = string.Empty;
        public string Property132 { get; set; } = string.Empty;
        public string Property133 { get; set; } = string.Empty;
        public string Property134 { get; set; } = string.Empty;
        public string Property135 { get; set; } = string.Empty;
        public string Property136 { get; set; } = string.Empty;
        public string Property137 { get; set; } = string.Empty;
        public string Property138 { get; set; } = string.Empty;
        public string Property139 { get; set; } = string.Empty;
        public string Property140 { get; set; } = string.Empty;
        public string Property141 { get; set; } = string.Empty;
        public string Property142 { get; set; } = string.Empty;
        public string Property143 { get; set; } = string.Empty;
        public string Property144 { get; set; } = string.Empty;
        public string Property145 { get; set; } = string.Empty;
        public string Property146 { get; set; } = string.Empty;
        public string Property147 { get; set; } = string.Empty;
        public string Property148 { get; set; } = string.Empty;
        public string Property149 { get; set; } = string.Empty;
        public string Property150 { get; set; } = string.Empty;
        public string Property151 { get; set; } = string.Empty;
        public string Property152 { get; set; } = string.Empty;
        public string Property153 { get; set; } = string.Empty;
        public string Property154 { get; set; } = string.Empty;
        public string Property155 { get; set; } = string.Empty;
        public string Property156 { get; set; } = string.Empty;
        public string Property157 { get; set; } = string.Empty;
        public string Property158 { get; set; } = string.Empty;
        public string Property159 { get; set; } = string.Empty;
        public string Property160 { get; set; } = string.Empty;
        public string Property161 { get; set; } = string.Empty;
        public string Property162 { get; set; } = string.Empty;
        public string Property163 { get; set; } = string.Empty;
        public string Property164 { get; set; } = string.Empty;
        public string Property165 { get; set; } = string.Empty;
        public string Property166 { get; set; } = string.Empty;
        public string Property167 { get; set; } = string.Empty;
        public string Property168 { get; set; } = string.Empty;
        public string Property169 { get; set; } = string.Empty;
        public string Property170 { get; set; } = string.Empty;
        public string Property171 { get; set; } = string.Empty;
        public string Property172 { get; set; } = string.Empty;
        public string Property173 { get; set; } = string.Empty;
        public string Property174 { get; set; } = string.Empty;
        public string Property175 { get; set; } = string.Empty;
        public string Property176 { get; set; } = string.Empty;
        public string Property177 { get; set; } = string.Empty;
        public string Property178 { get; set; } = string.Empty;
        public string Property179 { get; set; } = string.Empty;
        public string Property180 { get; set; } = string.Empty;
        public string Property181 { get; set; } = string.Empty;
        public string Property182 { get; set; } = string.Empty;
        public string Property183 { get; set; } = string.Empty;
        public string Property184 { get; set; } = string.Empty;
        public string Property185 { get; set; } = string.Empty;
        public string Property186 { get; set; } = string.Empty;
        public string Property187 { get; set; } = string.Empty;
        public string Property188 { get; set; } = string.Empty;
        public string Property189 { get; set; } = string.Empty;
        public string Property190 { get; set; } = string.Empty;
        public string Property191 { get; set; } = string.Empty;
        public string Property192 { get; set; } = string.Empty;
        public string Property193 { get; set; } = string.Empty;
        public string Property194 { get; set; } = string.Empty;
        public string Property195 { get; set; } = string.Empty;
        public string Property196 { get; set; } = string.Empty;
        public string Property197 { get; set; } = string.Empty;
        public string Property198 { get; set; } = string.Empty;
        public string Property199 { get; set; } = string.Empty;
        public string Property200 { get; set; } = string.Empty;
    }

    // Type with 201 properties (exceeds threshold)
    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Types are used via typeof for schema generation scenarios.")]
    internal sealed class TypeWith201Properties
    {
        public string Property1 { get; set; } = string.Empty;
        public string Property2 { get; set; } = string.Empty;
        public string Property3 { get; set; } = string.Empty;
        public string Property4 { get; set; } = string.Empty;
        public string Property5 { get; set; } = string.Empty;
        public string Property6 { get; set; } = string.Empty;
        public string Property7 { get; set; } = string.Empty;
        public string Property8 { get; set; } = string.Empty;
        public string Property9 { get; set; } = string.Empty;
        public string Property10 { get; set; } = string.Empty;
        public string Property11 { get; set; } = string.Empty;
        public string Property12 { get; set; } = string.Empty;
        public string Property13 { get; set; } = string.Empty;
        public string Property14 { get; set; } = string.Empty;
        public string Property15 { get; set; } = string.Empty;
        public string Property16 { get; set; } = string.Empty;
        public string Property17 { get; set; } = string.Empty;
        public string Property18 { get; set; } = string.Empty;
        public string Property19 { get; set; } = string.Empty;
        public string Property20 { get; set; } = string.Empty;
        public string Property21 { get; set; } = string.Empty;
        public string Property22 { get; set; } = string.Empty;
        public string Property23 { get; set; } = string.Empty;
        public string Property24 { get; set; } = string.Empty;
        public string Property25 { get; set; } = string.Empty;
        public string Property26 { get; set; } = string.Empty;
        public string Property27 { get; set; } = string.Empty;
        public string Property28 { get; set; } = string.Empty;
        public string Property29 { get; set; } = string.Empty;
        public string Property30 { get; set; } = string.Empty;
        public string Property31 { get; set; } = string.Empty;
        public string Property32 { get; set; } = string.Empty;
        public string Property33 { get; set; } = string.Empty;
        public string Property34 { get; set; } = string.Empty;
        public string Property35 { get; set; } = string.Empty;
        public string Property36 { get; set; } = string.Empty;
        public string Property37 { get; set; } = string.Empty;
        public string Property38 { get; set; } = string.Empty;
        public string Property39 { get; set; } = string.Empty;
        public string Property40 { get; set; } = string.Empty;
        public string Property41 { get; set; } = string.Empty;
        public string Property42 { get; set; } = string.Empty;
        public string Property43 { get; set; } = string.Empty;
        public string Property44 { get; set; } = string.Empty;
        public string Property45 { get; set; } = string.Empty;
        public string Property46 { get; set; } = string.Empty;
        public string Property47 { get; set; } = string.Empty;
        public string Property48 { get; set; } = string.Empty;
        public string Property49 { get; set; } = string.Empty;
        public string Property50 { get; set; } = string.Empty;
        public string Property51 { get; set; } = string.Empty;
        public string Property52 { get; set; } = string.Empty;
        public string Property53 { get; set; } = string.Empty;
        public string Property54 { get; set; } = string.Empty;
        public string Property55 { get; set; } = string.Empty;
        public string Property56 { get; set; } = string.Empty;
        public string Property57 { get; set; } = string.Empty;
        public string Property58 { get; set; } = string.Empty;
        public string Property59 { get; set; } = string.Empty;
        public string Property60 { get; set; } = string.Empty;
        public string Property61 { get; set; } = string.Empty;
        public string Property62 { get; set; } = string.Empty;
        public string Property63 { get; set; } = string.Empty;
        public string Property64 { get; set; } = string.Empty;
        public string Property65 { get; set; } = string.Empty;
        public string Property66 { get; set; } = string.Empty;
        public string Property67 { get; set; } = string.Empty;
        public string Property68 { get; set; } = string.Empty;
        public string Property69 { get; set; } = string.Empty;
        public string Property70 { get; set; } = string.Empty;
        public string Property71 { get; set; } = string.Empty;
        public string Property72 { get; set; } = string.Empty;
        public string Property73 { get; set; } = string.Empty;
        public string Property74 { get; set; } = string.Empty;
        public string Property75 { get; set; } = string.Empty;
        public string Property76 { get; set; } = string.Empty;
        public string Property77 { get; set; } = string.Empty;
        public string Property78 { get; set; } = string.Empty;
        public string Property79 { get; set; } = string.Empty;
        public string Property80 { get; set; } = string.Empty;
        public string Property81 { get; set; } = string.Empty;
        public string Property82 { get; set; } = string.Empty;
        public string Property83 { get; set; } = string.Empty;
        public string Property84 { get; set; } = string.Empty;
        public string Property85 { get; set; } = string.Empty;
        public string Property86 { get; set; } = string.Empty;
        public string Property87 { get; set; } = string.Empty;
        public string Property88 { get; set; } = string.Empty;
        public string Property89 { get; set; } = string.Empty;
        public string Property90 { get; set; } = string.Empty;
        public string Property91 { get; set; } = string.Empty;
        public string Property92 { get; set; } = string.Empty;
        public string Property93 { get; set; } = string.Empty;
        public string Property94 { get; set; } = string.Empty;
        public string Property95 { get; set; } = string.Empty;
        public string Property96 { get; set; } = string.Empty;
        public string Property97 { get; set; } = string.Empty;
        public string Property98 { get; set; } = string.Empty;
        public string Property99 { get; set; } = string.Empty;
        public string Property100 { get; set; } = string.Empty;
        public string Property101 { get; set; } = string.Empty;
        public string Property102 { get; set; } = string.Empty;
        public string Property103 { get; set; } = string.Empty;
        public string Property104 { get; set; } = string.Empty;
        public string Property105 { get; set; } = string.Empty;
        public string Property106 { get; set; } = string.Empty;
        public string Property107 { get; set; } = string.Empty;
        public string Property108 { get; set; } = string.Empty;
        public string Property109 { get; set; } = string.Empty;
        public string Property110 { get; set; } = string.Empty;
        public string Property111 { get; set; } = string.Empty;
        public string Property112 { get; set; } = string.Empty;
        public string Property113 { get; set; } = string.Empty;
        public string Property114 { get; set; } = string.Empty;
        public string Property115 { get; set; } = string.Empty;
        public string Property116 { get; set; } = string.Empty;
        public string Property117 { get; set; } = string.Empty;
        public string Property118 { get; set; } = string.Empty;
        public string Property119 { get; set; } = string.Empty;
        public string Property120 { get; set; } = string.Empty;
        public string Property121 { get; set; } = string.Empty;
        public string Property122 { get; set; } = string.Empty;
        public string Property123 { get; set; } = string.Empty;
        public string Property124 { get; set; } = string.Empty;
        public string Property125 { get; set; } = string.Empty;
        public string Property126 { get; set; } = string.Empty;
        public string Property127 { get; set; } = string.Empty;
        public string Property128 { get; set; } = string.Empty;
        public string Property129 { get; set; } = string.Empty;
        public string Property130 { get; set; } = string.Empty;
        public string Property131 { get; set; } = string.Empty;
        public string Property132 { get; set; } = string.Empty;
        public string Property133 { get; set; } = string.Empty;
        public string Property134 { get; set; } = string.Empty;
        public string Property135 { get; set; } = string.Empty;
        public string Property136 { get; set; } = string.Empty;
        public string Property137 { get; set; } = string.Empty;
        public string Property138 { get; set; } = string.Empty;
        public string Property139 { get; set; } = string.Empty;
        public string Property140 { get; set; } = string.Empty;
        public string Property141 { get; set; } = string.Empty;
        public string Property142 { get; set; } = string.Empty;
        public string Property143 { get; set; } = string.Empty;
        public string Property144 { get; set; } = string.Empty;
        public string Property145 { get; set; } = string.Empty;
        public string Property146 { get; set; } = string.Empty;
        public string Property147 { get; set; } = string.Empty;
        public string Property148 { get; set; } = string.Empty;
        public string Property149 { get; set; } = string.Empty;
        public string Property150 { get; set; } = string.Empty;
        public string Property151 { get; set; } = string.Empty;
        public string Property152 { get; set; } = string.Empty;
        public string Property153 { get; set; } = string.Empty;
        public string Property154 { get; set; } = string.Empty;
        public string Property155 { get; set; } = string.Empty;
        public string Property156 { get; set; } = string.Empty;
        public string Property157 { get; set; } = string.Empty;
        public string Property158 { get; set; } = string.Empty;
        public string Property159 { get; set; } = string.Empty;
        public string Property160 { get; set; } = string.Empty;
        public string Property161 { get; set; } = string.Empty;
        public string Property162 { get; set; } = string.Empty;
        public string Property163 { get; set; } = string.Empty;
        public string Property164 { get; set; } = string.Empty;
        public string Property165 { get; set; } = string.Empty;
        public string Property166 { get; set; } = string.Empty;
        public string Property167 { get; set; } = string.Empty;
        public string Property168 { get; set; } = string.Empty;
        public string Property169 { get; set; } = string.Empty;
        public string Property170 { get; set; } = string.Empty;
        public string Property171 { get; set; } = string.Empty;
        public string Property172 { get; set; } = string.Empty;
        public string Property173 { get; set; } = string.Empty;
        public string Property174 { get; set; } = string.Empty;
        public string Property175 { get; set; } = string.Empty;
        public string Property176 { get; set; } = string.Empty;
        public string Property177 { get; set; } = string.Empty;
        public string Property178 { get; set; } = string.Empty;
        public string Property179 { get; set; } = string.Empty;
        public string Property180 { get; set; } = string.Empty;
        public string Property181 { get; set; } = string.Empty;
        public string Property182 { get; set; } = string.Empty;
        public string Property183 { get; set; } = string.Empty;
        public string Property184 { get; set; } = string.Empty;
        public string Property185 { get; set; } = string.Empty;
        public string Property186 { get; set; } = string.Empty;
        public string Property187 { get; set; } = string.Empty;
        public string Property188 { get; set; } = string.Empty;
        public string Property189 { get; set; } = string.Empty;
        public string Property190 { get; set; } = string.Empty;
        public string Property191 { get; set; } = string.Empty;
        public string Property192 { get; set; } = string.Empty;
        public string Property193 { get; set; } = string.Empty;
        public string Property194 { get; set; } = string.Empty;
        public string Property195 { get; set; } = string.Empty;
        public string Property196 { get; set; } = string.Empty;
        public string Property197 { get; set; } = string.Empty;
        public string Property198 { get; set; } = string.Empty;
        public string Property199 { get; set; } = string.Empty;
        public string Property200 { get; set; } = string.Empty;
        public string Property201 { get; set; } = string.Empty;
    }

    // Type with few properties but all primitive
    public class SimpleType
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public bool Active { get; set; }
    }

    // Complex nested object type
    public class NestedObject
    {
        public string Value { get; set; } = string.Empty;
    }

    #endregion

    #region AC 301: Property Count Threshold Tests

    [Fact]
    public void AC301_TypeWith200Properties_DoesNotTriggerVirtualization()
    {
        // Arrange
        var options = new SchemaOptions
        {
            SchemaPropertyVirtualizationThreshold = 200
        };
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var schema = generator.GenerateSchema(typeof(TypeWith200Properties));

        // Assert
        Assert.NotNull(schema);
        Assert.Equal("object", schema.Type);
        Assert.False(schema.Extensions.ContainsKey("x-schema-virtualized"));

        var events = logger.GetEvents();
        Assert.DoesNotContain(events, e => e.Code == "VIRT001");
    }

    [Fact]
    public void AC301_TypeWith201Properties_TriggersVirtualization()
    {
        // Arrange
        var options = new SchemaOptions
        {
            SchemaPropertyVirtualizationThreshold = 200
        };
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var schema = generator.GenerateSchema(typeof(TypeWith201Properties));

        // Assert
        Assert.NotNull(schema);
        Assert.Equal("object", schema.Type);
        Assert.True(schema.Extensions.ContainsKey("x-schema-virtualized"));
        Assert.IsType<OpenApiBoolean>(schema.Extensions["x-schema-virtualized"]);
        Assert.True(((OpenApiBoolean)schema.Extensions["x-schema-virtualized"]).Value);

        // Verify metadata
        Assert.True(schema.Extensions.ContainsKey("x-property-total-count"));
        var totalCount = (OpenApiInteger)schema.Extensions["x-property-total-count"];
        Assert.Equal(201, totalCount.Value);

        // Verify VIRT001 diagnostic was emitted
        var events = logger.GetEvents();
        var virt001Event = Assert.Single(events, e => e.Code == "VIRT001");
        Assert.Equal(DiagnosticLevel.Info, virt001Event.Level);
        Assert.Contains("201 properties", virt001Event.Message, StringComparison.Ordinal);
        Assert.Contains("TypeWith201Properties", virt001Event.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AC301_CustomThreshold_RespectedCorrectly()
    {
        // Arrange - set threshold to 5
        var options = new SchemaOptions
        {
            SchemaPropertyVirtualizationThreshold = 5
        };
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act - SimpleType has 3 properties, should not virtualize
        var schema = generator.GenerateSchema(typeof(SimpleType));

        // Assert
        Assert.False(schema.Extensions.ContainsKey("x-schema-virtualized"));
        Assert.DoesNotContain(logger.GetEvents(), e => e.Code == "VIRT001");
    }

    #endregion

    #region AC 302: Nested Object Threshold Tests

    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Types are used via typeof for schema generation scenarios.")]
    internal sealed class TypeWith50NestedObjects
    {
        public NestedObject Nested1 { get; set; } = new();
        public NestedObject Nested2 { get; set; } = new();
        public NestedObject Nested3 { get; set; } = new();
        public NestedObject Nested4 { get; set; } = new();
        public NestedObject Nested5 { get; set; } = new();
        public NestedObject Nested6 { get; set; } = new();
        public NestedObject Nested7 { get; set; } = new();
        public NestedObject Nested8 { get; set; } = new();
        public NestedObject Nested9 { get; set; } = new();
        public NestedObject Nested10 { get; set; } = new();
        public NestedObject Nested11 { get; set; } = new();
        public NestedObject Nested12 { get; set; } = new();
        public NestedObject Nested13 { get; set; } = new();
        public NestedObject Nested14 { get; set; } = new();
        public NestedObject Nested15 { get; set; } = new();
        public NestedObject Nested16 { get; set; } = new();
        public NestedObject Nested17 { get; set; } = new();
        public NestedObject Nested18 { get; set; } = new();
        public NestedObject Nested19 { get; set; } = new();
        public NestedObject Nested20 { get; set; } = new();
        public NestedObject Nested21 { get; set; } = new();
        public NestedObject Nested22 { get; set; } = new();
        public NestedObject Nested23 { get; set; } = new();
        public NestedObject Nested24 { get; set; } = new();
        public NestedObject Nested25 { get; set; } = new();
        public NestedObject Nested26 { get; set; } = new();
        public NestedObject Nested27 { get; set; } = new();
        public NestedObject Nested28 { get; set; } = new();
        public NestedObject Nested29 { get; set; } = new();
        public NestedObject Nested30 { get; set; } = new();
        public NestedObject Nested31 { get; set; } = new();
        public NestedObject Nested32 { get; set; } = new();
        public NestedObject Nested33 { get; set; } = new();
        public NestedObject Nested34 { get; set; } = new();
        public NestedObject Nested35 { get; set; } = new();
        public NestedObject Nested36 { get; set; } = new();
        public NestedObject Nested37 { get; set; } = new();
        public NestedObject Nested38 { get; set; } = new();
        public NestedObject Nested39 { get; set; } = new();
        public NestedObject Nested40 { get; set; } = new();
        public NestedObject Nested41 { get; set; } = new();
        public NestedObject Nested42 { get; set; } = new();
        public NestedObject Nested43 { get; set; } = new();
        public NestedObject Nested44 { get; set; } = new();
        public NestedObject Nested45 { get; set; } = new();
        public NestedObject Nested46 { get; set; } = new();
        public NestedObject Nested47 { get; set; } = new();
        public NestedObject Nested48 { get; set; } = new();
        public NestedObject Nested49 { get; set; } = new();
        public NestedObject Nested50 { get; set; } = new();
    }

    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Types are used via typeof for schema generation scenarios.")]
    internal sealed class TypeWith51NestedObjects
    {
        public NestedObject Nested1 { get; set; } = new();
        public NestedObject Nested2 { get; set; } = new();
        public NestedObject Nested3 { get; set; } = new();
        public NestedObject Nested4 { get; set; } = new();
        public NestedObject Nested5 { get; set; } = new();
        public NestedObject Nested6 { get; set; } = new();
        public NestedObject Nested7 { get; set; } = new();
        public NestedObject Nested8 { get; set; } = new();
        public NestedObject Nested9 { get; set; } = new();
        public NestedObject Nested10 { get; set; } = new();
        public NestedObject Nested11 { get; set; } = new();
        public NestedObject Nested12 { get; set; } = new();
        public NestedObject Nested13 { get; set; } = new();
        public NestedObject Nested14 { get; set; } = new();
        public NestedObject Nested15 { get; set; } = new();
        public NestedObject Nested16 { get; set; } = new();
        public NestedObject Nested17 { get; set; } = new();
        public NestedObject Nested18 { get; set; } = new();
        public NestedObject Nested19 { get; set; } = new();
        public NestedObject Nested20 { get; set; } = new();
        public NestedObject Nested21 { get; set; } = new();
        public NestedObject Nested22 { get; set; } = new();
        public NestedObject Nested23 { get; set; } = new();
        public NestedObject Nested24 { get; set; } = new();
        public NestedObject Nested25 { get; set; } = new();
        public NestedObject Nested26 { get; set; } = new();
        public NestedObject Nested27 { get; set; } = new();
        public NestedObject Nested28 { get; set; } = new();
        public NestedObject Nested29 { get; set; } = new();
        public NestedObject Nested30 { get; set; } = new();
        public NestedObject Nested31 { get; set; } = new();
        public NestedObject Nested32 { get; set; } = new();
        public NestedObject Nested33 { get; set; } = new();
        public NestedObject Nested34 { get; set; } = new();
        public NestedObject Nested35 { get; set; } = new();
        public NestedObject Nested36 { get; set; } = new();
        public NestedObject Nested37 { get; set; } = new();
        public NestedObject Nested38 { get; set; } = new();
        public NestedObject Nested39 { get; set; } = new();
        public NestedObject Nested40 { get; set; } = new();
        public NestedObject Nested41 { get; set; } = new();
        public NestedObject Nested42 { get; set; } = new();
        public NestedObject Nested43 { get; set; } = new();
        public NestedObject Nested44 { get; set; } = new();
        public NestedObject Nested45 { get; set; } = new();
        public NestedObject Nested46 { get; set; } = new();
        public NestedObject Nested47 { get; set; } = new();
        public NestedObject Nested48 { get; set; } = new();
        public NestedObject Nested49 { get; set; } = new();
        public NestedObject Nested50 { get; set; } = new();
        public NestedObject Nested51 { get; set; } = new();
    }

    [Fact]
    public void AC302_TypeWith50NestedObjects_DoesNotTriggerVirtualization()
    {
        // Arrange
        var options = new SchemaOptions
        {
            NestedObjectVirtualizationThreshold = 50,
            SchemaPropertyVirtualizationThreshold = 1000 // Set high to isolate nested test
        };
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var schema = generator.GenerateSchema(typeof(TypeWith50NestedObjects));

        // Assert
        Assert.NotNull(schema);
        Assert.Equal("object", schema.Type);
        Assert.False(schema.Extensions.ContainsKey("x-schema-virtualized"));

        var events = logger.GetEvents();
        Assert.DoesNotContain(events, e => e.Code == "VIRT001");
    }

    [Fact]
    public void AC302_TypeWith51NestedObjects_TriggersVirtualization()
    {
        // Arrange
        var options = new SchemaOptions
        {
            NestedObjectVirtualizationThreshold = 50,
            SchemaPropertyVirtualizationThreshold = 1000 // Set high to isolate nested test
        };
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var schema = generator.GenerateSchema(typeof(TypeWith51NestedObjects));

        // Assert
        Assert.NotNull(schema);
        Assert.Equal("object", schema.Type);
        Assert.True(schema.Extensions.ContainsKey("x-schema-virtualized"));

        // Verify nested object count
        Assert.True(schema.Extensions.ContainsKey("x-nested-object-count"));
        var nestedCount = (OpenApiInteger)schema.Extensions["x-nested-object-count"];
        Assert.Equal(51, nestedCount.Value);

        // Verify VIRT001 diagnostic
        var events = logger.GetEvents();
        var virt001Event = Assert.Single(events, e => e.Code == "VIRT001");
        Assert.Contains("51 nested objects", virt001Event.Message, StringComparison.Ordinal);
    }

    #endregion

    #region AC 303: Virtualization Metadata Tests

    [Fact]
    public void AC303_VirtualizedSchema_ContainsRequiredMetadata()
    {
        // Arrange
        var options = new SchemaOptions
        {
            SchemaPropertyVirtualizationThreshold = 200
        };
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var schema = generator.GenerateSchema(typeof(TypeWith201Properties));

        // Assert - verify all required extension metadata
        Assert.True(schema.Extensions.ContainsKey("x-schema-virtualized"));
        Assert.True(((OpenApiBoolean)schema.Extensions["x-schema-virtualized"]).Value);

        Assert.True(schema.Extensions.ContainsKey("x-property-total-count"));
        Assert.Equal(201, ((OpenApiInteger)schema.Extensions["x-property-total-count"]).Value);

        Assert.True(schema.Extensions.ContainsKey("x-nested-object-count"));
        Assert.Equal(0, ((OpenApiInteger)schema.Extensions["x-nested-object-count"]).Value);

        Assert.True(schema.Extensions.ContainsKey("x-property-threshold-exceeded"));
        Assert.True(((OpenApiBoolean)schema.Extensions["x-property-threshold-exceeded"]).Value);

        Assert.True(schema.Extensions.ContainsKey("x-property-threshold"));
        Assert.Equal(200, ((OpenApiInteger)schema.Extensions["x-property-threshold"]).Value);

        // Description should mention virtualization
        Assert.NotNull(schema.Description);
        Assert.Contains("virtualized", schema.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("201", schema.Description, StringComparison.Ordinal);
    }

    [Fact]
    public void AC303_VIRT001Diagnostic_ContainsCorrectInformation()
    {
        // Arrange
        var options = new SchemaOptions
        {
            SchemaPropertyVirtualizationThreshold = 200,
            NestedObjectVirtualizationThreshold = 50
        };
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var schema = generator.GenerateSchema(typeof(TypeWith201Properties));

        // Assert
        var events = logger.GetEvents();
        var virt001Event = Assert.Single(events, e => e.Code == "VIRT001");

        Assert.Equal(DiagnosticLevel.Info, virt001Event.Level);
        Assert.Contains("TypeWith201Properties", virt001Event.Message, StringComparison.Ordinal);
        Assert.Contains("201 properties", virt001Event.Message, StringComparison.Ordinal);
        Assert.Contains("threshold", virt001Event.Message, StringComparison.OrdinalIgnoreCase);

        // Context should contain detailed information
        Assert.NotNull(virt001Event.Context);
    }

    [Fact]
    public void AC303_BothThresholdsExceeded_IndicatedInMetadata()
    {
        // Arrange - create type that exceeds both thresholds
        var options = new SchemaOptions
        {
            SchemaPropertyVirtualizationThreshold = 50,
            NestedObjectVirtualizationThreshold = 50
        };
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act - TypeWith51NestedObjects has 51 total properties, all are nested objects
        var schema = generator.GenerateSchema(typeof(TypeWith51NestedObjects));

        // Assert - both thresholds should be exceeded
        Assert.True(schema.Extensions.ContainsKey("x-property-threshold-exceeded"));
        Assert.True(((OpenApiBoolean)schema.Extensions["x-property-threshold-exceeded"]).Value);

        Assert.True(schema.Extensions.ContainsKey("x-nested-threshold-exceeded"));
        Assert.True(((OpenApiBoolean)schema.Extensions["x-nested-threshold-exceeded"]).Value);

        // Verify the counts
        Assert.Equal(51, ((OpenApiInteger)schema.Extensions["x-property-total-count"]).Value);
        Assert.Equal(51, ((OpenApiInteger)schema.Extensions["x-nested-object-count"]).Value);

        var description = schema.Description;
        Assert.NotNull(description);
        Assert.Contains("both", description, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void PrimitiveTypes_NeverVirtualized()
    {
        // Arrange
        var options = new SchemaOptions
        {
            SchemaPropertyVirtualizationThreshold = 0,
            NestedObjectVirtualizationThreshold = 0
        };
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act & Assert - primitives should never be virtualized
        var stringSchema = generator.GenerateSchema(typeof(string));
        Assert.False(stringSchema.Extensions.ContainsKey("x-schema-virtualized"));

        var intSchema = generator.GenerateSchema(typeof(int));
        Assert.False(intSchema.Extensions.ContainsKey("x-schema-virtualized"));

        var guidSchema = generator.GenerateSchema(typeof(Guid));
        Assert.False(guidSchema.Extensions.ContainsKey("x-schema-virtualized"));
    }

    public enum TestEnum
    {
        Value1,
        Value2,
        Value3
    }

    [Fact]
    public void EnumTypes_NeverVirtualizedBySchemaVirtualization()
    {
        // Arrange
        var options = new SchemaOptions
        {
            SchemaPropertyVirtualizationThreshold = 0,
            NestedObjectVirtualizationThreshold = 0
        };
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act
        var schema = generator.GenerateSchema(typeof(TestEnum));

        // Assert - enums have their own virtualization (AC 440), not schema virtualization
        Assert.False(schema.Extensions.ContainsKey("x-schema-virtualized"));
    }

    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Types are used via typeof for schema generation scenarios.")]
    internal sealed class TypeWithMixedProperties
    {
        public string String1 { get; set; } = string.Empty;
        public string String2 { get; set; } = string.Empty;
        public string String3 { get; set; } = string.Empty;
        public NestedObject Object1 { get; set; } = new();
        public NestedObject Object2 { get; set; } = new();
        public int Int1 { get; set; }
        public IList<string> List1 { get; set; } = new List<string>();
        public Dictionary<string, int> Dict1 { get; set; } = new();
    }

    [Fact]
    public void MixedPropertyTypes_CountedCorrectly()
    {
        // Arrange
        var options = new SchemaOptions
        {
            SchemaPropertyVirtualizationThreshold = 7,
            NestedObjectVirtualizationThreshold = 1
        };
        var logger = new DiagnosticsLogger();
        var generator = new SchemaGenerator(options, logger);

        // Act - type has 8 total properties, 2 nested objects
        var schema = generator.GenerateSchema(typeof(TypeWithMixedProperties));

        // Assert
        Assert.True(schema.Extensions.ContainsKey("x-schema-virtualized"));
        Assert.Equal(8, ((OpenApiInteger)schema.Extensions["x-property-total-count"]).Value);
        Assert.Equal(2, ((OpenApiInteger)schema.Extensions["x-nested-object-count"]).Value);
    }

    #endregion
}
