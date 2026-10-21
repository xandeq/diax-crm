using System.Reflection;
using Diax.Application.Finance;
using Diax.Infrastructure.Finance;

namespace Diax.Tests.Infrastructure;

// Regra do dono: a planilha Google de finanças é a fonte da verdade. O CRM só lê; nunca escreve.
public class GoogleSheetsReadOnlyTests
{
    [Fact]
    public void Interface_ExposesOnlyReadMethods()
    {
        var methods = typeof(IGoogleSheetsService).GetMethods().Select(m => m.Name).ToList();

        Assert.NotEmpty(methods);
        Assert.All(methods, name => Assert.StartsWith("Read", name));
    }

    [Fact]
    public void Service_HasNoPublicWriteMethods()
    {
        var methods = typeof(GoogleSheetsService)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(m => m.Name);

        Assert.All(methods, name => Assert.StartsWith("Read", name));
    }

    [Fact]
    public void Credential_UsesReadOnlyScope()
    {
        var scope = (string?)typeof(GoogleSheetsService)
            .GetField("Scope", BindingFlags.NonPublic | BindingFlags.Static)!
            .GetValue(null);

        Assert.Equal("https://www.googleapis.com/auth/spreadsheets.readonly", scope);
    }
}
