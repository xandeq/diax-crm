using System.Reflection;
using Diax.Api.Auth;
using Diax.Api.Controllers.V1;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Diax.Tests.Security;

public class AnonymousEndpointsTests
{
    // Toda action pública de propósito. Adicionar aqui é uma decisão de segurança consciente:
    // com a FallbackPolicy, o resto exige login.
    private static readonly HashSet<string> ExpectedAnonymous = new(StringComparer.Ordinal)
    {
        "AppointmentsController.TriggerDailyNotification", // cron, valida X-Cron-Key
        "AuthController.Login",
        "BlogController.GetBySlug",
        "BlogController.GetFeaturedPosts",
        "BlogController.GetPublishedPosts",
        "BrevoWebhookController.HandleWebhook",
        "EmailUnsubscribeController.Unsubscribe",
        "ErrorLogsController.Ingest",          // API key com escopo
        "ErrorLogsController.IngestBatch",     // API key com escopo
        "HealthController.Get",
        // IntegrationsController: [AllowAnonymous] na classe; toda action valida a chave de
        // integração com FixedTimeEquals e devolve 401.
        "IntegrationsController.GetCashFlowProjection",
        "IntegrationsController.GetEmailQuota",
        "IntegrationsController.IngestDailyBriefing",
        "IntegrationsController.SendEmail",
        "IntegrationsController.ServicePackagesForBot",
        "IntegrationsController.WhatsAppEvent",
        "MailjetWebhookController.HandleWebhook",
        "MailjetWebhookController.Ping",
        "MeetingsController.Book",
        "MeetingsController.GetAvailability",
        "ProposalsController.AcceptPublic",
        "ProposalsController.GetPublic",
        "ResendWebhookController.HandleWebhook",
        "SendGridWebhookController.HandleWebhook",
        "SnippetsController.DownloadPublicAttachment",
        "SnippetsController.GetPublicById",
        "SurfAlertsController.GetRecent",      // JWT ou API key, checado no controller
        "SurfAlertsController.Ingest",         // API key com escopo
    };

    [Fact]
    public void FallbackPolicy_RequiresAuthenticatedUser()
    {
        var options = new AuthorizationOptions();
        AuthorizationPolicies.Configure(options);

        Assert.NotNull(options.FallbackPolicy);
        Assert.Same(options.DefaultPolicy, options.FallbackPolicy);
    }

    [Fact]
    public void OnlyAllowlistedActionsAreAnonymous()
    {
        var actual = typeof(AuthController).Assembly.GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract)
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => m.GetCustomAttributes<HttpMethodAttribute>(inherit: true).Any())
                .Where(m => IsAnonymous(t, m))
                .Select(m => $"{t.Name}.{m.Name}"))
            .ToHashSet(StringComparer.Ordinal);

        var unexpected = actual.Except(ExpectedAnonymous).OrderBy(x => x).ToList();
        var missing = ExpectedAnonymous.Except(actual).OrderBy(x => x).ToList();

        Assert.True(unexpected.Count == 0, "Endpoints anônimos não previstos: " + string.Join(", ", unexpected));
        Assert.True(missing.Count == 0, "Endpoints que deveriam ser públicos e não são: " + string.Join(", ", missing));
    }

    [Fact]
    public void SchemaPatch_RequiresAuthorization()
    {
        var method = typeof(HealthController).GetMethod(nameof(HealthController.SchemaPatch))!;
        Assert.False(IsAnonymous(typeof(HealthController), method));
        Assert.NotEmpty(method.GetCustomAttributes<AuthorizeAttribute>());
    }

    private static bool IsAnonymous(Type controller, MethodInfo action)
    {
        if (action.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true).Any()) return true;
        if (action.GetCustomAttributes<AuthorizeAttribute>(inherit: true).Any()) return false;
        return controller.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true).Any();
    }
}
