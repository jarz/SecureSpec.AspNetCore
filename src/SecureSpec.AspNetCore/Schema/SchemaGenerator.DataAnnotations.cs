using Microsoft.OpenApi.Models;
using SecureSpec.AspNetCore.Diagnostics;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace SecureSpec.AspNetCore.Schema;

public partial class SchemaGenerator
{
    /// <summary>
    /// Applies DataAnnotations from an array of attributes.
    /// </summary>
    private void ApplyDataAnnotationsFromAttributes(OpenApiSchema schema, object[] attributes, string memberName)
    {
        foreach (var attribute in attributes)
        {
            switch (attribute)
            {
                // AC 31: Required attribute adds to required array (handled at property level)
                case RangeAttribute range:
                    ApplyRangeAttribute(schema, range, memberName);
                    break;

                case MinLengthAttribute minLength:
                    ApplyMinLengthAttribute(schema, minLength, memberName);
                    break;

                case MaxLengthAttribute maxLength:
                    ApplyMaxLengthAttribute(schema, maxLength, memberName);
                    break;

                case StringLengthAttribute stringLength:
                    ApplyStringLengthAttribute(schema, stringLength, memberName);
                    break;

                case RegularExpressionAttribute regex:
                    ApplyRegularExpressionAttribute(schema, regex, memberName);
                    break;
            }
        }
    }

    /// <summary>
    /// Applies Range attribute to schema with conflict detection.
    /// </summary>
    private void ApplyRangeAttribute(OpenApiSchema schema, RangeAttribute range, string memberName)
    {
        var minimum = Convert.ToDouble(range.Minimum, CultureInfo.InvariantCulture);
        var maximum = Convert.ToDouble(range.Maximum, CultureInfo.InvariantCulture);

        if (schema.Minimum.HasValue || schema.Maximum.HasValue)
        {
            _logger.LogWarning(
                DiagnosticCodes.Annotations.DataAnnotationsConflict,
                $"DataAnnotations conflict detected on member '{memberName}': Range attribute overrides existing minimum/maximum constraints. Last wins.");
        }

        schema.Minimum = (decimal)minimum;
        schema.Maximum = (decimal)maximum;
    }

    /// <summary>
    /// Applies MinLength attribute to schema with conflict detection.
    /// </summary>
    private void ApplyMinLengthAttribute(OpenApiSchema schema, MinLengthAttribute minLength, string memberName)
    {
        if (schema.MinLength.HasValue)
        {
            _logger.LogWarning(
                DiagnosticCodes.Annotations.DataAnnotationsConflict,
                $"DataAnnotations conflict detected on member '{memberName}': MinLength attribute overrides existing minLength constraint. Last wins.");
        }

        schema.MinLength = minLength.Length;
    }

    /// <summary>
    /// Applies MaxLength attribute to schema with conflict detection.
    /// </summary>
    private void ApplyMaxLengthAttribute(OpenApiSchema schema, MaxLengthAttribute maxLength, string memberName)
    {
        if (schema.MaxLength.HasValue)
        {
            _logger.LogWarning(
                DiagnosticCodes.Annotations.DataAnnotationsConflict,
                $"DataAnnotations conflict detected on member '{memberName}': MaxLength attribute overrides existing maxLength constraint. Last wins.");
        }

        schema.MaxLength = maxLength.Length;
    }

    /// <summary>
    /// Applies StringLength attribute to schema with conflict detection.
    /// </summary>
    private void ApplyStringLengthAttribute(OpenApiSchema schema, StringLengthAttribute stringLength, string memberName)
    {
        if (schema.MinLength.HasValue || schema.MaxLength.HasValue)
        {
            _logger.LogWarning(
                DiagnosticCodes.Annotations.DataAnnotationsConflict,
                $"DataAnnotations conflict detected on member '{memberName}': StringLength attribute overrides existing minLength/maxLength constraints. Last wins.");
        }

        schema.MaxLength = stringLength.MaximumLength;

        if (stringLength.MinimumLength > 0)
        {
            schema.MinLength = stringLength.MinimumLength;
        }
    }

    /// <summary>
    /// Applies RegularExpression attribute to schema with conflict detection.
    /// </summary>
    private void ApplyRegularExpressionAttribute(OpenApiSchema schema, RegularExpressionAttribute regex, string memberName)
    {
        if (!string.IsNullOrEmpty(schema.Pattern))
        {
            _logger.LogWarning(
                DiagnosticCodes.Annotations.DataAnnotationsConflict,
                $"DataAnnotations conflict detected on member '{memberName}': RegularExpression attribute overrides existing pattern constraint. Last wins.");
        }

        schema.Pattern = regex.Pattern;
    }
}
