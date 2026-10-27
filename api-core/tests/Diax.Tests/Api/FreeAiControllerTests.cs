using Diax.Api.Controllers;
using Diax.Application.AI.FreeGateway;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Diax.Tests.Api;

/// <summary>
/// Testes de contrato do <see cref="FreeAiController"/>: mapeamento de validação/falha total e
/// formato de resposta OpenAI-compatível. O gateway em si é testado em FreeAiGatewayServiceTests.
/// </summary>
public class FreeAiControllerTests
{
    private static FreeAiController BuildSut(IFreeAiGatewayService gateway) =>
        new(gateway, Mock.Of<ILogger<FreeAiController>>());

    [Fact]
    public async Task ChatCompletions_InvalidRequest_Returns400()
    {
        var gateway = new Mock<IFreeAiGatewayService>();
        gateway.Setup(g => g.CompleteAsync(It.IsAny<FreeAiChatRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("messages precisa ter pelo menos 1 item."));

        var sut = BuildSut(gateway.Object);
        var result = await sut.ChatCompletions(new FreeAiChatCompletionRequestDto { Messages = new() }, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    [Fact]
    public async Task ChatCompletions_AllProvidersFail_Returns503WithAttempts()
    {
        var gateway = new Mock<IFreeAiGatewayService>();
        gateway.Setup(g => g.CompleteAsync(It.IsAny<FreeAiChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FreeAiChatResult
            {
                Success = false,
                Attempts = new List<FreeAiAttempt> { new() { Provider = "groq", Model = "m", Status = "500" } },
            });

        var sut = BuildSut(gateway.Object);
        var dto = new FreeAiChatCompletionRequestDto
        {
            Messages = new() { new FreeAiChatMessageDto { Role = "user", Content = "oi" } },
        };

        var result = await sut.ChatCompletions(dto, CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, objectResult.StatusCode);
    }

    [Fact]
    public async Task ChatCompletions_Success_ReturnsOpenAiFormatWithXDiax()
    {
        var gateway = new Mock<IFreeAiGatewayService>();
        gateway.Setup(g => g.CompleteAsync(It.IsAny<FreeAiChatRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FreeAiChatResult
            {
                Success = true,
                Content = "ola",
                Provider = "groq",
                Model = "llama-3.3-70b-versatile",
                FinishReason = "stop",
                PromptTokens = 3,
                CompletionTokens = 2,
                Attempts = new List<FreeAiAttempt> { new() { Provider = "groq", Model = "llama-3.3-70b-versatile", Status = "ok" } },
            });

        var sut = BuildSut(gateway.Object);
        var dto = new FreeAiChatCompletionRequestDto
        {
            Messages = new() { new FreeAiChatMessageDto { Role = "user", Content = "oi" } },
        };

        var result = await sut.ChatCompletions(dto, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<FreeAiChatCompletionResponseDto>(ok.Value);
        Assert.Equal("groq/llama-3.3-70b-versatile", body.Model);
        Assert.Equal("ola", body.Choices[0].Message.Content);
        Assert.Equal("assistant", body.Choices[0].Message.Role);
        Assert.Equal("groq", body.XDiax.Provider);
        Assert.Equal(5, body.Usage.TotalTokens);
    }

    [Fact]
    public async Task ChatCompletions_StreamRequested_Returns400()
    {
        var gateway = new Mock<IFreeAiGatewayService>();
        var sut = BuildSut(gateway.Object);
        var dto = new FreeAiChatCompletionRequestDto { Stream = true, Messages = new() };

        var result = await sut.ChatCompletions(dto, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
        gateway.Verify(g => g.CompleteAsync(It.IsAny<FreeAiChatRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
