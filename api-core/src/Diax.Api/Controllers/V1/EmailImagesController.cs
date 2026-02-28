using Asp.Versioning;
using Diax.Application.EmailMarketing;
using Diax.Application.EmailMarketing.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diax.Api.Controllers.V1;

[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/email-images")]
[Produces("application/json")]
public class EmailImagesController : BaseApiController
{
    private readonly IEmailImageStorageService _emailImageStorage;

    public EmailImagesController(IEmailImageStorageService emailImageStorage)
    {
        _emailImageStorage = emailImageStorage;
    }

    /// <summary>
    /// Faz upload de uma imagem para uso em email marketing.
    /// Retorna URL pública para ser usada em &lt;img src="URL"&gt;.
    /// </summary>
    /// <param name="request">Dados da imagem (base64)</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>URL pública da imagem hospedada</returns>
    [HttpPost("upload")]
    public async Task<IActionResult> UploadImage(
        [FromBody] UploadEmailImageRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _emailImageStorage.SaveImageAsync(request, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Remove uma imagem previamente hospedada.
    /// </summary>
    /// <param name="imageId">ID único da imagem</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    [HttpDelete("{imageId}")]
    public async Task<IActionResult> DeleteImage(
        [FromRoute] string imageId,
        CancellationToken cancellationToken)
    {
        var result = await _emailImageStorage.DeleteImageAsync(imageId, cancellationToken);
        return HandleResult(result);
    }
}
