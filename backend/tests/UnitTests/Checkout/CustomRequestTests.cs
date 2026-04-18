using Domain.Checkout;

namespace UnitTests.Checkout;

public class CustomRequestTests
{
    #region Creation Tests

    [Fact]
    public void Create_WithValidData_ReturnsPendingRequest()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var description = "Custom macro forleveling";

        // Act
        var request = CustomRequest.Create(customerId, description);

        // Assert
        Assert.NotEqual(Guid.Empty, request.Id);
        Assert.Equal(customerId, request.CustomerId);
        Assert.Equal(description, request.Description);
        Assert.Equal(CustomRequestStatus.Pending, request.Status);
        Assert.True(request.CreatedAtUtc <= DateTime.UtcNow);
        Assert.True(request.UpdatedAtUtc <= DateTime.UtcNow);
    }

    [Fact]
    public void Create_WithOrderId_LinksToOrder()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var description = "Custom script for order";

        // Act
        var request = CustomRequest.Create(customerId, description, orderId);

        // Assert
        Assert.Equal(orderId, request.OrderId);
    }

    [Fact]
    public void Create_WithoutOrderId_HasNullOrderId()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var description = "Standalone custom request";

        // Act
        var request = CustomRequest.Create(customerId, description);

        // Assert
        Assert.Null(request.OrderId);
    }

    #endregion

    #region Status Transition Tests

    [Fact]
    public void StartProgress_FromPending_Succeeds()
    {
        // Arrange
        var request = CustomRequest.Create(Guid.NewGuid(), "Test");

        // Act
        request.StartProgress();

        // Assert
        Assert.Equal(CustomRequestStatus.InProgress, request.Status);
    }

    [Fact]
    public void StartProgress_FromPending_UpdatesTimestamp()
    {
        // Arrange
        var request = CustomRequest.Create(Guid.NewGuid(), "Test");
        var beforeStart = DateTime.UtcNow;

        // Act
        request.StartProgress();

        // Assert
        Assert.True(request.UpdatedAtUtc >= beforeStart);
    }

    [Fact]
    public void StartProgress_FromInProgress_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = CustomRequest.Create(Guid.NewGuid(), "Test");
        request.StartProgress();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => request.StartProgress());
        Assert.Contains("Can only start pending requests", ex.Message);
    }

    [Fact]
    public void StartProgress_FromDelivered_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = CustomRequest.Create(Guid.NewGuid(), "Test");
        request.StartProgress();
        request.MarkDelivered();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => request.StartProgress());
        Assert.Contains("Can only start pending requests", ex.Message);
    }

    [Fact]
    public void MarkDelivered_FromInProgress_Succeeds()
    {
        // Arrange
        var request = CustomRequest.Create(Guid.NewGuid(), "Test");
        request.StartProgress();

        // Act
        request.MarkDelivered();

        // Assert
        Assert.Equal(CustomRequestStatus.Delivered, request.Status);
    }

    [Fact]
    public void MarkDelivered_FromInProgress_UpdatesTimestamp()
    {
        // Arrange
        var request = CustomRequest.Create(Guid.NewGuid(), "Test");
        request.StartProgress();
        var beforeDeliver = DateTime.UtcNow;

        // Act
        request.MarkDelivered();

        // Assert
        Assert.True(request.UpdatedAtUtc >= beforeDeliver);
    }

    [Fact]
    public void MarkDelivered_FromPending_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = CustomRequest.Create(Guid.NewGuid(), "Test");

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => request.MarkDelivered());
        Assert.Contains("Can only deliver in-progress requests", ex.Message);
    }

    [Fact]
    public void MarkDelivered_FromDelivered_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = CustomRequest.Create(Guid.NewGuid(), "Test");
        request.StartProgress();
        request.MarkDelivered();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => request.MarkDelivered());
        Assert.Contains("Can only deliver in-progress requests", ex.Message);
    }

    #endregion

    #region Status Values Test

    [Fact]
    public void Status_Enum_HasCorrectValues()
    {
        // Assert
        Assert.Equal(0, (int)CustomRequestStatus.Pending);
        Assert.Equal(1, (int)CustomRequestStatus.InProgress);
        Assert.Equal(2, (int)CustomRequestStatus.Delivered);
    }

    #endregion
}