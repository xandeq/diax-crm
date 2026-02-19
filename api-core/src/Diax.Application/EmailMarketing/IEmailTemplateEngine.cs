namespace Diax.Application.EmailMarketing;

public interface IEmailTemplateEngine
{
    string Render(string template, IReadOnlyDictionary<string, string?> variables);
}
