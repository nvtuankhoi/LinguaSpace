using LinguaSpace.Application.Common.Behaviours;
using LinguaSpace.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace LinguaSpace.Application.UnitTests.Common.Behaviours;

// Minimal request dùng để test LoggingBehaviour mà không phụ thuộc vào domain feature cụ thể
public record TestRequest : IRequest<Unit>;

public class RequestLoggerTests
{
    private Mock<ILogger<TestRequest>> _logger = null!;
    private Mock<IUser> _user = null!;
    private Mock<IIdentityService> _identityService = null!;

    [SetUp]
    public void Setup()
    {
        _logger = new Mock<ILogger<TestRequest>>();
        _user = new Mock<IUser>();
        _identityService = new Mock<IIdentityService>();
    }

    [Test]
    public async Task ShouldCallGetUserNameAsyncOnceIfAuthenticated()
    {
        _user.Setup(x => x.Id).Returns(Guid.NewGuid().ToString());

        var requestLogger = new LoggingBehaviour<TestRequest>(_logger.Object, _user.Object, _identityService.Object);

        await requestLogger.Process(new TestRequest(), new CancellationToken());

        _identityService.Verify(i => i.GetUserNameAsync(It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task ShouldNotCallGetUserNameAsyncOnceIfUnauthenticated()
    {
        var requestLogger = new LoggingBehaviour<TestRequest>(_logger.Object, _user.Object, _identityService.Object);

        await requestLogger.Process(new TestRequest(), new CancellationToken());

        _identityService.Verify(i => i.GetUserNameAsync(It.IsAny<string>()), Times.Never);
    }
}

