using Diax.Application.WhatsApp;
using Diax.Application.Outreach;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Diax.Domain.EmailMarketing;
using Diax.Domain.Outreach;
using Moq;

namespace Diax.Tests.Application.Outreach;

public class OutreachDailyBudgetTests
{
    private static OutreachService Build(Guid userId, int dailyLimit, int sent, int committed, out Mock<ICustomerRepository> customers)
    {
        var config = new OutreachConfig(userId);
        config.UpdateModuleFlags(true, true, true);
        config.UpdateSendLimits(dailyLimit, 7);

        var configRepo = new Mock<IOutreachConfigRepository>();
        configRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(config);

        var queue = new Mock<IEmailQueueRepository>();
        queue.Setup(q => q.CountSentSinceAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(sent);
        queue.Setup(q => q.CountCommittedForDayAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(committed);

        var user = new Mock<ICurrentUserService>();
        user.Setup(u => u.UserId).Returns(userId);

        customers = new Mock<ICustomerRepository>();
        return new OutreachService(
            configRepo.Object, customers.Object, queue.Object,
            new Mock<IEmailCampaignRepository>().Object, user.Object,
            new Mock<IUnitOfWork>().Object, new Mock<IWhatsAppSender>().Object);
    }

    [Fact]
    public async Task CreateAndQueue_CountsAlreadyQueuedItemsAgainstDailyLimit()
    {
        // 4 enviados + 6 ainda na fila = 10 comprometidos; limite 10 → nada a enfileirar
        var sut = Build(Guid.NewGuid(), dailyLimit: 10, sent: 4, committed: 10, out var customers);

        var result = await sut.CreateAndQueueCampaignAsync();

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Outreach.DailyLimitReached", result.Error.Code);
        customers.Verify(c => c.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Customer, bool>>>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
