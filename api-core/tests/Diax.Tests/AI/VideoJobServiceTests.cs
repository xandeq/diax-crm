using Diax.Application.AI;
using Diax.Application.AI.MediaStorage;
using Diax.Application.AI.VideoGeneration;
using Diax.Application.AI.VideoGeneration.Dtos;
using Diax.Domain.AI;
using Diax.Domain.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Diax.Tests.AI;

public class VideoJobServiceTests
{
    private readonly Mock<IVideoGenerationJobRepository> _jobRepo = new();
    private readonly Mock<IVideoGenerationService> _videoService = new();
    private readonly Mock<IAiModelValidator> _validator = new();
    private readonly Mock<IGeneratedMediaStorageService> _mediaStorage = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private VideoJobService CreateService() => new(
        _jobRepo.Object,
        _videoService.Object,
        _validator.Object,
        _mediaStorage.Object,
        _unitOfWork.Object,
        NullLogger<VideoJobService>.Instance);

    private static VideoGenerationRequestDto ValidRequest(string provider = "falai") => new(
        Provider: provider,
        Model: "fal-ai/ltx-video",
        Prompt: "um gato programando em C#");

    [Fact]
    public async Task EnqueueAsync_PersistsJob_AndReturnsQueuedDto()
    {
        _validator.Setup(v => v.IsValidProviderAsync("falai", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _jobRepo.Setup(r => r.CountQueuedAheadAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var dto = await CreateService().EnqueueAsync(ValidRequest(), Guid.NewGuid());

        Assert.Equal(VideoGenerationJobStatus.Queued, dto.Status);
        Assert.Equal(2, dto.QueuePosition);
        _jobRepo.Verify(r => r.AddAsync(It.IsAny<VideoGenerationJob>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EnqueueAsync_Throws_WhenNoPromptAndNoReferenceImage()
    {
        var request = new VideoGenerationRequestDto(Provider: "falai", Model: "fal-ai/ltx-video", Prompt: null);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            CreateService().EnqueueAsync(request, Guid.NewGuid()));
    }

    [Fact]
    public async Task EnqueueAsync_Throws_WhenProviderInactive()
    {
        _validator.Setup(v => v.IsValidProviderAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            CreateService().EnqueueAsync(ValidRequest(), Guid.NewGuid()));
    }

    [Fact]
    public async Task GetAsync_ReturnsNull_WhenJobBelongsToAnotherUser()
    {
        var owner = Guid.NewGuid();
        var job = new VideoGenerationJob(owner, "falai", "fal-ai/ltx-video",
            "prompt", null, 5, 1280, 720, "16:9", null, null, true);
        _jobRepo.Setup(r => r.GetByIdAsync(job.Id, It.IsAny<CancellationToken>())).ReturnsAsync(job);

        var dto = await CreateService().GetAsync(job.Id, Guid.NewGuid()); // outro usuário

        Assert.Null(dto);
    }

    [Fact]
    public async Task ProcessNextAsync_ReturnsZero_WhenQueueEmpty()
    {
        _jobRepo.Setup(r => r.GetNextQueuedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((VideoGenerationJob?)null);

        var processed = await CreateService().ProcessNextAsync();

        Assert.Equal(0, processed);
        _videoService.Verify(v => v.GenerateAsync(
            It.IsAny<VideoGenerationRequestDto>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessNextAsync_MarksCompleted_OnSuccess()
    {
        var job = new VideoGenerationJob(Guid.NewGuid(), "falai", "fal-ai/ltx-video",
            "prompt", null, 5, 1280, 720, "16:9", null, null, true);
        _jobRepo.Setup(r => r.GetNextQueuedAsync(It.IsAny<CancellationToken>())).ReturnsAsync(job);
        _videoService.Setup(v => v.GenerateAsync(
                It.IsAny<VideoGenerationRequestDto>(), job.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VideoGenerationResponseDto(
                ProviderUsed: "huggingface",
                ModelUsed: "Wan-AI/Wan2.1-T2V-1.3B",
                RequestId: "req-1",
                DurationMs: 45000,
                VideoUrl: "https://cdn.example.com/video.mp4",
                ThumbnailUrl: null,
                FallbackOccurred: true,
                RequestedProvider: "falai",
                AttemptedProviders: new List<string> { "falai", "huggingface" }));

        var processed = await CreateService().ProcessNextAsync();

        Assert.Equal(1, processed);
        Assert.Equal(VideoGenerationJobStatus.Completed, job.Status);
        Assert.Equal("huggingface", job.ProviderUsed);
        // storage durável falhou (mock retorna null) → mantém URL do provider
        Assert.Equal("https://cdn.example.com/video.mp4", job.VideoUrl);
        Assert.True(job.FallbackOccurred);
        Assert.Null(job.ReferenceImageBase64); // limpo ao concluir
        // claim (Processing) + resultado final = 2 saves
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ProcessNextAsync_StoresDurableVideoUrl_WhenStorageSucceeds()
    {
        var job = new VideoGenerationJob(Guid.NewGuid(), "runway", "gen4_turbo",
            "prompt", null, 5, 1280, 720, "16:9", null, null, true);
        _jobRepo.Setup(r => r.GetNextQueuedAsync(It.IsAny<CancellationToken>())).ReturnsAsync(job);
        _videoService.Setup(v => v.GenerateAsync(
                It.IsAny<VideoGenerationRequestDto>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VideoGenerationResponseDto(
                "runway", "gen4_turbo", "req-2", 21000,
                "https://provider.example.com/expira-em-24h.mp4?jwt=abc", null));
        _mediaStorage.Setup(m => m.TrySaveVideoAsync(
                "https://provider.example.com/expira-em-24h.mp4?jwt=abc", job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync($"https://api.example.com/generated-media/{job.Id:N}.mp4");

        await CreateService().ProcessNextAsync();

        Assert.Equal(VideoGenerationJobStatus.Completed, job.Status);
        Assert.Equal($"https://api.example.com/generated-media/{job.Id:N}.mp4", job.VideoUrl);
    }

    [Fact]
    public async Task ProcessNextAsync_MarksFailed_WithCategory_OnAiProviderException()
    {
        var job = new VideoGenerationJob(Guid.NewGuid(), "falai", "fal-ai/ltx-video",
            "prompt", null, 5, 1280, 720, "16:9", null, null, true);
        _jobRepo.Setup(r => r.GetNextQueuedAsync(It.IsAny<CancellationToken>())).ReturnsAsync(job);
        _videoService.Setup(v => v.GenerateAsync(
                It.IsAny<VideoGenerationRequestDto>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AiProviderException("todos falharam", AiErrorCategory.QuotaExhausted));

        var processed = await CreateService().ProcessNextAsync();

        Assert.Equal(1, processed);
        Assert.Equal(VideoGenerationJobStatus.Failed, job.Status);
        Assert.Equal(AiErrorCategory.QuotaExhausted, job.ErrorCategory);
        Assert.Contains("todos falharam", job.ErrorMessage);
    }

    [Fact]
    public async Task RecoverStaleJobsAsync_FailsStaleProcessingJobs()
    {
        var job = new VideoGenerationJob(Guid.NewGuid(), "falai", "fal-ai/ltx-video",
            "prompt", null, 5, 1280, 720, "16:9", null, null, true);
        job.MarkProcessing();
        _jobRepo.Setup(r => r.GetStaleProcessingAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<VideoGenerationJob> { job });

        await CreateService().RecoverStaleJobsAsync();

        Assert.Equal(VideoGenerationJobStatus.Failed, job.Status);
        Assert.Equal(AiErrorCategory.Timeout, job.ErrorCategory);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
