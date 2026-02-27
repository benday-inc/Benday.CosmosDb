namespace Benday.Identity.CosmosDb;

/// <summary>
/// Represents a passkey (WebAuthn/FIDO2) credential stored as a nested object
/// inside the user document. Byte arrays are stored as Base64Url strings
/// for Cosmos DB JSON serialization.
/// </summary>
public class CosmosIdentityUserPasskey
{
    /// <summary>
    /// The credential ID (Base64Url-encoded).
    /// </summary>
    public string CredentialId { get; set; } = string.Empty;

    /// <summary>
    /// The public key (Base64Url-encoded).
    /// </summary>
    public string PublicKey { get; set; } = string.Empty;

    /// <summary>
    /// The signature counter for replay protection.
    /// </summary>
    public uint SignCount { get; set; }

    /// <summary>
    /// The transports supported by this passkey (e.g., "usb", "nfc", "ble", "internal").
    /// </summary>
    public string[] Transports { get; set; } = Array.Empty<string>();

    /// <summary>
    /// The attestation object (Base64Url-encoded).
    /// </summary>
    public string AttestationObject { get; set; } = string.Empty;

    /// <summary>
    /// The client data JSON (Base64Url-encoded).
    /// </summary>
    public string ClientDataJson { get; set; } = string.Empty;

    /// <summary>
    /// Whether user verification was performed during registration.
    /// </summary>
    public bool IsUserVerified { get; set; }

    /// <summary>
    /// Whether the passkey is eligible for backup.
    /// </summary>
    public bool IsBackupEligible { get; set; }

    /// <summary>
    /// Whether the passkey is currently backed up.
    /// </summary>
    public bool IsBackedUp { get; set; }

    /// <summary>
    /// A user-friendly label for this passkey.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// When this passkey was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}
