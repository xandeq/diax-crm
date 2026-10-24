using Diax.Application.Customers;
using Diax.Domain.Common;
using Diax.Domain.Customers;
using Moq;

namespace Diax.Tests.Application.Customers;

public class ServicePackageServiceTests
{
    private readonly Mock<IServicePackageRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private ServicePackageService Svc() => new(_repo.Object, _uow.Object);

    [Fact]
    public async Task Create_Valid_SavesAndReturnsDto()
    {
        var user = Guid.NewGuid();
        var r = await Svc().CreateAsync(new UpsertServicePackageRequest("Site", "desc", "a\nb", 1800m, 15, 1, true), user);
        Assert.True(r.IsSuccess);
        Assert.Equal("Site", r.Value.Name);
        _repo.Verify(x => x.AddAsync(It.Is<ServicePackage>(p => p.UserId == user), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_Invalid_ReturnsValidationFailure()
    {
        var r = await Svc().CreateAsync(new UpsertServicePackageRequest("", "", "", 0m, 0, 0, true), Guid.NewGuid());
        Assert.False(r.IsSuccess);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Update_OtherUsersPackage_NotFound()
    {
        var pkg = new ServicePackage(Guid.NewGuid(), "Site", "", "", 100m, 1, 0);
        _repo.Setup(x => x.GetByIdAsync(pkg.Id, It.IsAny<CancellationToken>())).ReturnsAsync(pkg);
        var r = await Svc().UpdateAsync(pkg.Id, new UpsertServicePackageRequest("X", "", "", 1m, 1, 0, true), Guid.NewGuid());
        Assert.False(r.IsSuccess);
    }
}
