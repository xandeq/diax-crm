namespace Diax.Application.EmailMarketing;

public interface IEmailTemplateEngine
{
    /// <summary>Substituição crua — só para texto puro (assunto).</summary>
    string Render(string template, IReadOnlyDictionary<string, string?> variables);

    /// <summary>Para corpo HTML: valores escapados (&amp; &lt; &gt; " '). O template em si fica intacto.</summary>
    string RenderHtml(string template, IReadOnlyDictionary<string, string?> variables);
}
