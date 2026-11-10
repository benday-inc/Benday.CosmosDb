// Passkey (WebAuthn) helper functions for Benday.Identity.CosmosDb.UI

function isPasskeySupported() {
    return window.PublicKeyCredential !== undefined &&
        typeof window.PublicKeyCredential === 'function';
}

function base64UrlToBytes(base64url) {
    var base64 = base64url.replace(/-/g, '+').replace(/_/g, '/');
    while (base64.length % 4 !== 0) {
        base64 += '=';
    }
    var binary = atob(base64);
    var bytes = new Uint8Array(binary.length);
    for (var i = 0; i < binary.length; i++) {
        bytes[i] = binary.charCodeAt(i);
    }
    return bytes;
}

function bytesToBase64Url(bytes) {
    if (bytes instanceof ArrayBuffer) {
        bytes = new Uint8Array(bytes);
    }
    var binary = '';
    for (var i = 0; i < bytes.byteLength; i++) {
        binary += String.fromCharCode(bytes[i]);
    }
    return btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
}

function getAntiForgeryToken() {
    var tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    return tokenInput ? tokenInput.value : '';
}

// True when the browser can offer passkeys in a text field's autofill
// dropdown (WebAuthn conditional mediation).
async function isPasskeyAutofillSupported() {
    if (!isPasskeySupported() ||
        typeof PublicKeyCredential.isConditionalMediationAvailable !== 'function') {
        return false;
    }

    try {
        return await PublicKeyCredential.isConditionalMediationAvailable();
    } catch (err) {
        return false;
    }
}

// Only one WebAuthn request can be pending at a time, so each login request
// aborts the one before it.
var passkeyLoginAbortController = null;

// mediation: optional. Pass 'conditional' to offer passkeys through autofill on
// an input with autocomplete="username webauthn" instead of showing a prompt.
// returnUrl is unused and kept for backward compatibility.
async function startPasskeyLogin(optionsEndpoint, returnUrl, mediation) {
    if (passkeyLoginAbortController) {
        passkeyLoginAbortController.abort();
    }

    var abortController = new AbortController();
    passkeyLoginAbortController = abortController;

    // Request assertion options from server
    var optionsResponse = await fetch(optionsEndpoint, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': getAntiForgeryToken()
        },
        signal: abortController.signal
    });

    if (!optionsResponse.ok) {
        throw new Error('Failed to get passkey options');
    }

    var optionsJson = await optionsResponse.json();

    // Parse and call WebAuthn API
    var options = PublicKeyCredential.parseRequestOptionsFromJSON(optionsJson);
    var request = { publicKey: options, signal: abortController.signal };

    if (mediation) {
        request.mediation = mediation;
    }

    var credential = await navigator.credentials.get(request);

    // Serialize credential to JSON
    if (credential.toJSON) {
        return JSON.stringify(credential.toJSON());
    }

    // Fallback manual serialization for browsers that don't support toJSON
    return JSON.stringify({
        authenticatorAttachment: credential.authenticatorAttachment,
        clientExtensionResults: credential.getClientExtensionResults(),
        id: credential.id,
        rawId: bytesToBase64Url(credential.rawId),
        response: {
            authenticatorData: bytesToBase64Url(credential.response.authenticatorData),
            clientDataJSON: bytesToBase64Url(credential.response.clientDataJSON),
            signature: bytesToBase64Url(credential.response.signature),
            userHandle: credential.response.userHandle ? bytesToBase64Url(credential.response.userHandle) : null
        },
        type: credential.type
    });
}

async function startPasskeyRegistration(optionsEndpoint, registerEndpoint, passkeyName) {
    // Request creation options from server
    var optionsResponse = await fetch(optionsEndpoint, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': getAntiForgeryToken()
        }
    });

    if (!optionsResponse.ok) {
        throw new Error('Failed to get passkey creation options');
    }

    var optionsJson = await optionsResponse.json();

    // Parse and call WebAuthn API
    var options = PublicKeyCredential.parseCreationOptionsFromJSON(optionsJson);
    var credential = await navigator.credentials.create({ publicKey: options });

    // Serialize credential to JSON
    var credentialJson;
    if (credential.toJSON) {
        credentialJson = JSON.stringify(credential.toJSON());
    } else {
        // Fallback manual serialization
        credentialJson = JSON.stringify({
            authenticatorAttachment: credential.authenticatorAttachment,
            clientExtensionResults: credential.getClientExtensionResults(),
            id: credential.id,
            rawId: bytesToBase64Url(credential.rawId),
            response: {
                attestationObject: bytesToBase64Url(credential.response.attestationObject),
                clientDataJSON: bytesToBase64Url(credential.response.clientDataJSON),
                transports: credential.response.getTransports ? credential.response.getTransports() : []
            },
            type: credential.type
        });
    }

    // Submit registration to server
    var formData = new FormData();
    formData.append('CredentialJson', credentialJson);
    formData.append('PasskeyName', passkeyName || '');

    var registerResponse = await fetch(registerEndpoint, {
        method: 'POST',
        body: formData
    });

    return registerResponse;
}
