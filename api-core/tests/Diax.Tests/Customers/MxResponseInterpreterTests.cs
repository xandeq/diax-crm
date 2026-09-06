using Diax.Application.Customers.Services;
using Diax.Infrastructure.Dns;
using Xunit;

namespace Diax.Tests.Customers;

/// <summary>
/// Cobertura determinística de MxResponseInterpreter (07-02, EXTR-01/D-02) — sem rede, sem
/// construir tipos do DnsClient.NET. Tabela-verdade espelha docs/email-marketing/mx_check.py.
/// </summary>
public class MxResponseInterpreterTests
{
    public static IEnumerable<object[]> MxCases()
    {
        // Real MX record → Valid
        yield return new object[]
        {
            new[] { "mail.acme.com." }, false, 0, MxResolutionStep.Valid
        };

        // Multiple real MX records → Valid
        yield return new object[]
        {
            new[] { "alt1.aspmx.l.google.com.", "alt2.aspmx.l.google.com." }, false, 0, MxResolutionStep.Valid
        };

        // NXDOMAIN — domain doesn't exist, don't even try A
        yield return new object[]
        {
            Array.Empty<string>(), true, 3, MxResolutionStep.NoMx
        };

        // No MX, no error → try A fallback
        yield return new object[]
        {
            Array.Empty<string>(), false, 0, MxResolutionStep.TryARecordFallback
        };

        // Null MX (RFC 7505) → try A fallback
        yield return new object[]
        {
            new[] { "." }, false, 0, MxResolutionStep.TryARecordFallback
        };

        // Blank/whitespace exchanges are ignored → try A fallback
        yield return new object[]
        {
            new[] { "", "  " }, false, 0, MxResolutionStep.TryARecordFallback
        };

        // Real MX wins over any error flag
        yield return new object[]
        {
            new[] { "mail.acme.com." }, true, 3, MxResolutionStep.Valid
        };

        // SERVFAIL (2) — server failure, D-02: never NoMx
        yield return new object[]
        {
            Array.Empty<string>(), true, 2, MxResolutionStep.Unverified
        };

        // NotImplemented (4) — server failure, D-02: never NoMx
        yield return new object[]
        {
            Array.Empty<string>(), true, 4, MxResolutionStep.Unverified
        };

        // Refused (5) — server failure, D-02: never NoMx
        yield return new object[]
        {
            Array.Empty<string>(), true, 5, MxResolutionStep.Unverified
        };
    }

    [Theory]
    [MemberData(nameof(MxCases))]
    public void InterpretMxResponse_MatchesPythonSemantics(
        string[] exchanges, bool hasError, int rcode, MxResolutionStep expected)
        => Assert.Equal(expected, MxResponseInterpreter.InterpretMxResponse(exchanges, hasError, rcode));

    [Fact]
    public void InterpretAFallback_HasARecords_ReturnsValid()
        => Assert.Equal(MxCheckResult.Valid, MxResponseInterpreter.InterpretAFallback(true));

    [Fact]
    public void InterpretAFallback_NoARecordsNoError_ReturnsNoMx()
        => Assert.Equal(MxCheckResult.NoMx, MxResponseInterpreter.InterpretAFallback(false));

    [Fact]
    public void InterpretAFallback_ServfailOnARecord_ReturnsUnverified_NeverNoMx()
        => Assert.Equal(MxCheckResult.Unverified, MxResponseInterpreter.InterpretAFallback(false, true, 2));

    [Fact]
    public void InterpretAFallback_NxDomainOnARecord_ReturnsNoMx()
        => Assert.Equal(MxCheckResult.NoMx, MxResponseInterpreter.InterpretAFallback(false, true, 3));

    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(5)]
    public void IsTransientServerFailure_ServerErrorCodes_ReturnTrue(int rcode)
        => Assert.True(MxResponseInterpreter.IsTransientServerFailure(rcode));

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void IsTransientServerFailure_SuccessAndNxDomain_ReturnFalse(int rcode)
        => Assert.False(MxResponseInterpreter.IsTransientServerFailure(rcode));
}
