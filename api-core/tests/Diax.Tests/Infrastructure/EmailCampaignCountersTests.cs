using Diax.Domain.EmailMarketing;
using Diax.Infrastructure.Data;
using Diax.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Diax.Tests.Infrastructure;

// 21/10: worker (Sent) e webhook (Delivered) atualizam a mesma campanha em contextos diferentes.
// Com DbSet.Update cada um gravava a linha inteira e o último apagava o contador do outro.
public class EmailCampaignCountersTests
{
    [Fact]
    public async Task WorkerAndWebhook_Concurrently_BothCountersSurvive()
    {
        var options = new DbContextOptionsBuilder<DiaxDbContext>()
            .UseInMemoryDatabase("campaign-counters-" + Guid.NewGuid()).Options;
        var campaign = new EmailCampaign(Guid.NewGuid(), "Wave", "Assunto", "<p>x</p>");
        campaign.SetTotalRecipients(10);
        await using (var seed = new DiaxDbContext(options)) { seed.Add(campaign); await seed.SaveChangesAsync(); }

        await using var worker = new DiaxDbContext(options);
        await using var webhook = new DiaxDbContext(options);
        var workerRepo = new EmailCampaignRepository(worker);
        var webhookRepo = new EmailCampaignRepository(webhook);

        // Os dois leem a mesma versão da linha antes de qualquer um gravar.
        var seenByWebhook = await webhookRepo.GetByIdAsync(campaign.Id);
        await workerRepo.IncrementSentAsync(campaign.Id);

        seenByWebhook!.IncrementDelivered();
        await webhookRepo.UpdateAsync(seenByWebhook);

        // Webhook grava primeiro; o worker grava depois com a versão antiga em memória.
        await webhook.SaveChangesAsync();
        await worker.SaveChangesAsync();

        await using var check = new DiaxDbContext(options);
        var final = await check.Set<EmailCampaign>().SingleAsync(c => c.Id == campaign.Id);
        Assert.Equal(1, final.SentCount);
        Assert.Equal(1, final.DeliveredCount);
    }
}
