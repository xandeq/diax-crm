using System;
using System.Security.Cryptography;
using System.Text;
using Diax.Api.Controllers.V1;
using Xunit;

namespace Diax.Tests.Application.EmailMarketing;

/// <summary>
/// Tests for the SendGrid Event Webhook ECDSA signature verification.
/// Mirrors SendGrid's signing scheme: ECDSA P-256 over SHA-256 of (timestamp + rawBody),
/// DER-encoded signature, base64 SPKI verification key.
/// </summary>
public class SendGridSignatureTests
{
    private static (string publicKeyBase64, ECDsa key) NewKey()
    {
        var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var spki = ecdsa.ExportSubjectPublicKeyInfo();
        return (Convert.ToBase64String(spki), ecdsa);
    }

    private static string Sign(ECDsa key, string timestamp, string body)
    {
        var payload = Encoding.UTF8.GetBytes(timestamp + body);
        var sig = key.SignData(payload, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
        return Convert.ToBase64String(sig);
    }

    [Fact]
    public void VerifySignature_ValidSignature_ReturnsTrue()
    {
        var (publicKey, key) = NewKey();
        const string timestamp = "1600112502";
        const string body = "[{\"event\":\"delivered\",\"email\":\"a@b.com\"}]";
        var signature = Sign(key, timestamp, body);

        Assert.True(SendGridWebhookController.VerifySignature(publicKey, timestamp, body, signature));
        key.Dispose();
    }

    [Fact]
    public void VerifySignature_TamperedBody_ReturnsFalse()
    {
        var (publicKey, key) = NewKey();
        const string timestamp = "1600112502";
        var signature = Sign(key, timestamp, "[{\"event\":\"delivered\"}]");

        Assert.False(SendGridWebhookController.VerifySignature(
            publicKey, timestamp, "[{\"event\":\"open\"}]", signature));
        key.Dispose();
    }

    [Fact]
    public void VerifySignature_WrongKey_ReturnsFalse()
    {
        var (_, signingKey) = NewKey();
        var (otherPublicKey, otherKey) = NewKey();
        const string timestamp = "1600112502";
        const string body = "[{\"event\":\"delivered\"}]";
        var signature = Sign(signingKey, timestamp, body);

        Assert.False(SendGridWebhookController.VerifySignature(otherPublicKey, timestamp, body, signature));
        signingKey.Dispose();
        otherKey.Dispose();
    }

    [Theory]
    [InlineData("not-base64!!", "1600112502", "body", "also-not-base64")]
    [InlineData("", "1600112502", "body", "")]
    public void VerifySignature_MalformedInputs_ReturnsFalse(string key, string ts, string body, string sig)
    {
        Assert.False(SendGridWebhookController.VerifySignature(key, ts, body, sig));
    }
}
