using System.Text.Json.Nodes;

namespace Benday.CosmosDb.MigrationTool;

/// <summary>
/// Transforms Cosmos DB documents from the old v5 schema to v6 schema.
/// Works with raw JSON — no typed classes needed.
/// </summary>
public class DocumentTransformer
{
    private static readonly HashSet<string> SystemProperties = new(StringComparer.Ordinal)
    {
        "id", "_etag", "_ts", "_rid", "_self", "_attachments"
    };

    private static readonly HashSet<string> NewFormatProperties = new(StringComparer.Ordinal)
    {
        "id", "_ts", "_rid", "_self", "_attachments",
        "tenantId", "entityType"
    };

    /// <summary>
    /// Transforms a document from old schema to new schema.
    /// </summary>
    /// <returns>Result containing the transformed document or errors</returns>
    public TransformResult Transform(JsonObject doc)
    {
        var result = new TransformResult();

        // Step 1: Extract tenantId value from OwnerId or pk (fallback)
        string? tenantIdValue = GetAndRemoveStringProperty(doc, "OwnerId");

        if (tenantIdValue == null)
        {
            tenantIdValue = GetAndRemoveStringProperty(doc, "pk");
        }
        else
        {
            // Still remove pk if it exists (it was a duplicate)
            RemoveProperty(doc, "pk");
        }

        // Check if already in new format
        if (tenantIdValue == null)
        {
            var existingTenantId = doc["tenantId"];
            if (existingTenantId != null)
            {
                tenantIdValue = existingTenantId.GetValue<string>();
                result.Warnings.Add("Document already has 'tenantId' — applying camelCase pass only.");
            }
        }

        if (tenantIdValue == null)
        {
            result.IsError = true;
            result.Errors.Add("Document has no 'OwnerId', 'pk', or 'tenantId' property. Cannot determine partition key.");
            return result;
        }

        // Step 2: Extract entityType value from discriminator
        string? entityTypeValue = GetAndRemoveStringProperty(doc, "discriminator");

        if (entityTypeValue == null)
        {
            var existingEntityType = doc["entityType"];
            if (existingEntityType != null)
            {
                entityTypeValue = existingEntityType.GetValue<string>();
            }
        }

        if (entityTypeValue == null)
        {
            result.IsError = true;
            result.Errors.Add("Document has no 'discriminator' or 'entityType' property. Cannot determine entity type.");
            return result;
        }

        // Step 3: Set new properties
        doc["tenantId"] = tenantIdValue;
        doc["entityType"] = entityTypeValue;

        // Step 4: Remove _etag (Cosmos assigns new ones on upsert)
        RemoveProperty(doc, "_etag");

        // Step 5: Convert PascalCase property names to camelCase (recursively)
        ConvertToCamelCase(doc, result);

        result.TransformedDocument = doc;
        return result;
    }

    private void ConvertToCamelCase(JsonObject obj, TransformResult result)
    {
        // Collect renames to avoid modifying collection during iteration
        var renames = new List<(string oldName, string newName, JsonNode? value)>();

        foreach (var property in obj.ToList())
        {
            var name = property.Key;

            // Skip system and known new-format properties
            if (NewFormatProperties.Contains(name) || SystemProperties.Contains(name))
            {
                // Still recurse into nested objects
                if (property.Value is JsonObject nested)
                {
                    ConvertToCamelCase(nested, result);
                }
                else if (property.Value is JsonArray array)
                {
                    ConvertArrayToCamelCase(array, result);
                }
                continue;
            }

            // Check if first char is uppercase
            if (name.Length > 0 && char.IsUpper(name[0]))
            {
                var camelName = char.ToLowerInvariant(name[0]) + name.Substring(1);

                // Check for collision
                if (obj.ContainsKey(camelName))
                {
                    result.Warnings.Add($"Property name collision: both '{name}' and '{camelName}' exist. Keeping original '{name}'.");
                    continue;
                }

                renames.Add((name, camelName, property.Value));
            }
            else
            {
                // Already camelCase — still recurse into nested objects
                if (property.Value is JsonObject nested)
                {
                    ConvertToCamelCase(nested, result);
                }
                else if (property.Value is JsonArray array)
                {
                    ConvertArrayToCamelCase(array, result);
                }
            }
        }

        // Apply renames
        foreach (var (oldName, newName, value) in renames)
        {
            obj.Remove(oldName);
            var clonedValue = value != null ? JsonNode.Parse(value.ToJsonString()) : null;

            // Recurse into nested objects after cloning
            if (clonedValue is JsonObject nested)
            {
                ConvertToCamelCase(nested, result);
            }
            else if (clonedValue is JsonArray array)
            {
                ConvertArrayToCamelCase(array, result);
            }

            obj[newName] = clonedValue;
        }
    }

    private void ConvertArrayToCamelCase(JsonArray array, TransformResult result)
    {
        foreach (var item in array)
        {
            if (item is JsonObject obj)
            {
                ConvertToCamelCase(obj, result);
            }
        }
    }

    private static string? GetAndRemoveStringProperty(JsonObject doc, string propertyName)
    {
        var node = doc[propertyName];
        if (node == null) return null;

        var value = node.GetValue<string>();
        doc.Remove(propertyName);
        return value;
    }

    private static void RemoveProperty(JsonObject doc, string propertyName)
    {
        doc.Remove(propertyName);
    }
}

/// <summary>
/// Result of a document transformation.
/// </summary>
public class TransformResult
{
    public JsonObject? TransformedDocument { get; set; }
    public bool IsError { get; set; }
    public List<string> Errors { get; } = new();
    public List<string> Warnings { get; } = new();
}
