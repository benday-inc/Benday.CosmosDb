using System.Text.Json;
using System.Text.Json.Nodes;
using Benday.CosmosDb.MigrationTool;

namespace Benday.CosmosDb.MigrationTool.Tests;

public class DocumentTransformerTests
{
    private readonly DocumentTransformer _sut = new();

    private static JsonObject ParseDoc(string json)
    {
        return JsonNode.Parse(json)!.AsObject();
    }

    [Fact]
    public void StandardTransformation_RemovesPkAndRenames()
    {
        var doc = ParseDoc("""
        {
            "id": "abc-123",
            "pk": "user-42",
            "OwnerId": "user-42",
            "discriminator": "Person",
            "FirstName": "Ben",
            "LastName": "Day",
            "EmailAddress": "ben@example.com",
            "_etag": "\"old-etag\"",
            "_ts": 1700000000,
            "_rid": "abc123",
            "_self": "dbs/abc/colls/xyz/docs/abc123",
            "_attachments": "attachments/"
        }
        """);

        var result = _sut.Transform(doc);

        Assert.False(result.IsError, string.Join(", ", result.Errors));
        var transformed = result.TransformedDocument!;

        // pk removed
        Assert.False(transformed.ContainsKey("pk"));
        // OwnerId removed
        Assert.False(transformed.ContainsKey("OwnerId"));
        // discriminator removed
        Assert.False(transformed.ContainsKey("discriminator"));

        // New properties set
        Assert.Equal("user-42", transformed["tenantId"]!.GetValue<string>());
        Assert.Equal("Person", transformed["entityType"]!.GetValue<string>());

        // PascalCase converted to camelCase
        Assert.True(transformed.ContainsKey("firstName"));
        Assert.True(transformed.ContainsKey("lastName"));
        Assert.True(transformed.ContainsKey("emailAddress"));
        Assert.False(transformed.ContainsKey("FirstName"));
        Assert.False(transformed.ContainsKey("LastName"));
        Assert.False(transformed.ContainsKey("EmailAddress"));

        Assert.Equal("Ben", transformed["firstName"]!.GetValue<string>());
        Assert.Equal("Day", transformed["lastName"]!.GetValue<string>());

        // _etag removed
        Assert.False(transformed.ContainsKey("_etag"));

        // System properties preserved
        Assert.Equal("abc-123", transformed["id"]!.GetValue<string>());
        Assert.Equal(1700000000, transformed["_ts"]!.GetValue<long>());
        Assert.Equal("abc123", transformed["_rid"]!.GetValue<string>());
    }

    [Fact]
    public void FallsBackToPkWhenOwnerIdMissing()
    {
        var doc = ParseDoc("""
        {
            "id": "abc-123",
            "pk": "tenant-99",
            "discriminator": "Note",
            "Title": "Hello"
        }
        """);

        var result = _sut.Transform(doc);

        Assert.False(result.IsError);
        Assert.Equal("tenant-99", result.TransformedDocument!["tenantId"]!.GetValue<string>());
        Assert.Equal("Note", result.TransformedDocument!["entityType"]!.GetValue<string>());
        Assert.False(result.TransformedDocument!.ContainsKey("pk"));
    }

    [Fact]
    public void AlreadyMigratedDocument_SkipsRename_DoesCamelCase()
    {
        var doc = ParseDoc("""
        {
            "id": "abc-123",
            "tenantId": "user-42",
            "entityType": "Person",
            "FirstName": "Ben",
            "LastName": "Day"
        }
        """);

        var result = _sut.Transform(doc);

        Assert.False(result.IsError);
        Assert.Single(result.Warnings);
        Assert.Contains("already has 'tenantId'", result.Warnings[0]);

        var transformed = result.TransformedDocument!;
        Assert.Equal("user-42", transformed["tenantId"]!.GetValue<string>());
        Assert.True(transformed.ContainsKey("firstName"));
        Assert.False(transformed.ContainsKey("FirstName"));
    }

    [Fact]
    public void MissingAllPartitionKeySources_ReturnsError()
    {
        var doc = ParseDoc("""
        {
            "id": "abc-123",
            "discriminator": "Person",
            "FirstName": "Ben"
        }
        """);

        var result = _sut.Transform(doc);

        Assert.True(result.IsError);
        Assert.Contains("Cannot determine partition key", result.Errors[0]);
    }

    [Fact]
    public void MissingDiscriminator_ReturnsError()
    {
        var doc = ParseDoc("""
        {
            "id": "abc-123",
            "OwnerId": "user-42",
            "FirstName": "Ben"
        }
        """);

        var result = _sut.Transform(doc);

        Assert.True(result.IsError);
        Assert.Contains("Cannot determine entity type", result.Errors[0]);
    }

    [Fact]
    public void NestedObjects_ConvertedToCamelCase()
    {
        var doc = ParseDoc("""
        {
            "id": "abc-123",
            "OwnerId": "user-42",
            "discriminator": "Person",
            "HomeAddress": {
                "StreetLine1": "123 Main St",
                "City": "Springfield",
                "StateCode": "IL"
            }
        }
        """);

        var result = _sut.Transform(doc);

        Assert.False(result.IsError);
        var transformed = result.TransformedDocument!;

        Assert.True(transformed.ContainsKey("homeAddress"));
        Assert.False(transformed.ContainsKey("HomeAddress"));

        var address = transformed["homeAddress"]!.AsObject();
        Assert.True(address.ContainsKey("streetLine1"));
        Assert.True(address.ContainsKey("city"));
        Assert.True(address.ContainsKey("stateCode"));
        Assert.Equal("123 Main St", address["streetLine1"]!.GetValue<string>());
    }

    [Fact]
    public void ArrayOfObjects_ConvertedToCamelCase()
    {
        var doc = ParseDoc("""
        {
            "id": "abc-123",
            "OwnerId": "user-42",
            "discriminator": "Person",
            "PhoneNumbers": [
                { "PhoneType": "Home", "PhoneNumber": "555-1234" },
                { "PhoneType": "Work", "PhoneNumber": "555-5678" }
            ]
        }
        """);

        var result = _sut.Transform(doc);

        Assert.False(result.IsError);
        var transformed = result.TransformedDocument!;

        Assert.True(transformed.ContainsKey("phoneNumbers"));
        var phones = transformed["phoneNumbers"]!.AsArray();
        Assert.Equal(2, phones.Count);

        var first = phones[0]!.AsObject();
        Assert.True(first.ContainsKey("phoneType"));
        Assert.True(first.ContainsKey("phoneNumber"));
        Assert.Equal("Home", first["phoneType"]!.GetValue<string>());
    }

    [Fact]
    public void CamelCaseCollision_WarnsAndKeepsOriginal()
    {
        var doc = ParseDoc("""
        {
            "id": "abc-123",
            "OwnerId": "user-42",
            "discriminator": "Person",
            "Name": "uppercase",
            "name": "lowercase"
        }
        """);

        var result = _sut.Transform(doc);

        Assert.False(result.IsError);
        Assert.Single(result.Warnings, w => w.Contains("collision") && w.Contains("abc-123"));

        var transformed = result.TransformedDocument!;
        // Both should exist — the original PascalCase kept and lowercase kept
        Assert.True(transformed.ContainsKey("Name"));
        Assert.True(transformed.ContainsKey("name"));
    }

    [Fact]
    public void AlreadyCamelCaseProperties_NotTouched()
    {
        var doc = ParseDoc("""
        {
            "id": "abc-123",
            "OwnerId": "user-42",
            "discriminator": "Person",
            "firstName": "Ben",
            "lastName": "Day"
        }
        """);

        var result = _sut.Transform(doc);

        Assert.False(result.IsError);
        var transformed = result.TransformedDocument!;
        Assert.Equal("Ben", transformed["firstName"]!.GetValue<string>());
        Assert.Equal("Day", transformed["lastName"]!.GetValue<string>());
    }

    [Fact]
    public void EtagAlwaysRemoved()
    {
        var doc = ParseDoc("""
        {
            "id": "abc-123",
            "OwnerId": "user-42",
            "discriminator": "Person",
            "_etag": "\"some-etag-value\""
        }
        """);

        var result = _sut.Transform(doc);

        Assert.False(result.IsError);
        Assert.False(result.TransformedDocument!.ContainsKey("_etag"));
    }

    [Fact]
    public void SystemPropertyId_NotCamelCased()
    {
        var doc = ParseDoc("""
        {
            "id": "abc-123",
            "OwnerId": "user-42",
            "discriminator": "Person"
        }
        """);

        var result = _sut.Transform(doc);

        Assert.False(result.IsError);
        Assert.True(result.TransformedDocument!.ContainsKey("id"));
        Assert.Equal("abc-123", result.TransformedDocument!["id"]!.GetValue<string>());
    }
}
