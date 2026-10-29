using Diax.Application.Outreach;
using Diax.Application.WhatsApp;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Diax.Domain.EmailMarketing;
using Diax.Domain.Outreach;
using Moq;

namespace Diax.Tests.Application.Outreach;

public class OutreachReadyLeadsDelegationTests
{
    [Fact]
    public async Task ReadyLeads_AreQueriedInRepositoryWithLimit_NotLoadedWholeTable()
    {
        var userId = Guid.NewGuid();
        var config = new OutreachConfig(userId);
        var configRepo = new Mock<IOutreachConfigRepository>();
        configRepo.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(config);
        var user = new Mock<ICurrentUserService>();
        user.Setup(u => u.UserId).Returns(userId);
        var customers = new Mock<ICustomerRepository>();
        var lead = new Customer("Lead", "l@x.com");
        customers.Setup(c => c.GetEmailOutreachReadyAsync(It.IsAny<DateTime>(), 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Customer> { lead });
        customers.Setup(c => c.GetWhatsAppOutreachReadyAsync(It.IsAny<DateTime>(), 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Customer> { lead });
        var sut = new OutreachService(configRepo.Object, customers.Object, new Mock<IEmailQueueRepository>().Object,
            new Mock<IEmailCampaignRepository>().Object, user.Object, new Mock<IUnitOfWork>().Object, new Mock<IWhatsAppSender>().Object);

        var email = await sut.GetReadyLeadsAsync(7);
        var wa = await sut.GetWhatsAppReadyLeadsAsync(7);

        Assert.Single(email.Value);
        Assert.Single(wa.Value);
        customers.Verify(c => c.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Customer, bool>>>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
