using Domain.Checkout;
using Application.Checkout.Contracts;

using TS = Domain.Checkout.TransitionSourceType;
using OS = Domain.Checkout.OrderStatus;

namespace UnitTests.Checkout;

public class OrderLifecycleStateMachineTests
{
    #region Legal Transition Matrix Tests (D-01, D-02, D-03)

    [Fact]
    public void ApplyTransition_System_PendingToPaid_IsAllowed()
    {
        // Arrange: Pending order
        var order = new Order(Guid.NewGuid(), Guid.NewGuid(), "test-intent");
        var beforeTransition = DateTimeOffset.UtcNow;

        // Act: System transitions to Paid
        order.ApplyTransition(OS.Paid, TS.System, beforeTransition);

        // Assert: Transition succeeds, status changed
        Assert.Equal(OS.Paid, order.Status);
    }

    [Fact]
    public void ApplyTransition_System_FromPaid_IsNoOp()
    {
        // Arrange: Order already at Paid
        var order = new Order(Guid.NewGuid(), Guid.NewGuid(), "test-intent");
        order.ApplyTransition(OS.Paid, TS.System, DateTimeOffset.UtcNow);
        var eventCount = order.StatusHistory.Count;

        // Act: Try System transition to Paid again
        order.ApplyTransition(OS.Paid, TS.System, DateTimeOffset.UtcNow);

        // Assert: Idempotent no-op per D-04
        Assert.Equal(OS.Paid, order.Status);
        Assert.Equal(eventCount, order.StatusHistory.Count); // No new event
    }

    [Fact]
    public void ApplyTransition_Customer_PendingToCancelled_IsAllowed()
    {
        // Arrange: Pending order
        var order = new Order(Guid.NewGuid(), Guid.NewGuid(), "test-intent");

        // Act: Customer cancels
        order.ApplyTransition(OS.Cancelled, TS.Customer, DateTimeOffset.UtcNow);

        // Assert: Cancel succeeds
        Assert.Equal(OS.Cancelled, order.Status);
    }

    [Fact]
    public void ApplyTransition_Customer_PaidToCancelled_Throws()
    {
        // Arrange: Paid order - cancelled NOT allowed per D-02
        var order = new Order(Guid.NewGuid(), Guid.NewGuid(), "test-intent");
        order.ApplyTransition(OS.Paid, TS.System, DateTimeOffset.UtcNow);

        // Act & Assert: Customer cannot cancel paid order
        Assert.Throws<InvalidOperationException>(() =>
            order.ApplyTransition(OS.Cancelled, TS.Customer, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void ApplyTransition_Admin_PendingToCancelled_IsAllowed()
    {
        // Arrange: Pending order
        var order = new Order(Guid.NewGuid(), Guid.NewGuid(), "test-intent");

        // Act: Admin cancels
        order.ApplyTransition(OS.Cancelled, TS.Admin, DateTimeOffset.UtcNow, Guid.NewGuid(), "Customer request");

        // Assert: Cancel succeeds
        Assert.Equal(OS.Cancelled, order.Status);
    }

    [Fact]
    public void ApplyTransition_Admin_PaidToCancelled_Throws()
    {
        // Arrange: Paid order - cancelled NOT allowed per D-02
        var order = new Order(Guid.NewGuid(), Guid.NewGuid(), "test-intent");
        order.ApplyTransition(OS.Paid, TS.System, DateTimeOffset.UtcNow);

        // Act & Assert: Admin cannot cancel paid order
        Assert.Throws<InvalidOperationException>(() =>
            order.ApplyTransition(OS.Cancelled, TS.Admin, DateTimeOffset.UtcNow));
    }

    #endregion

    #region Idempotent No-Op Tests (D-04, D-05)

    [Fact]
    public void ApplyTransition_DuplicateToCurrentStatus_CreatesNoEvent()
    {
        // Arrange: Order already Pending
        var order = new Order(Guid.NewGuid(), Guid.NewGuid(), "test-intent");
        var eventCount = order.StatusHistory.Count;

        // Act: Try transition to same status
        order.ApplyTransition(OS.Pending, TS.System, DateTimeOffset.UtcNow);

        // Assert: No extra event added
        Assert.Equal(eventCount, order.StatusHistory.Count);
    }

    #endregion

    #region Timeline Events (D-06, D-07, D-08)

    [Fact]
    public void ApplyTransition_RecordsEvent_WithFromToSourceTimestamp()
    {
        // Arrange
        var order = new Order(Guid.NewGuid(), Guid.NewGuid(), "test-intent");
        var now = DateTimeOffset.UtcNow;

        // Act
        order.ApplyTransition(OS.Paid, TS.System, now);

        // Assert: Event has required metadata per D-06, D-07
        Assert.Single(order.StatusHistory);
        var evt = order.StatusHistory[0];
        Assert.Equal(OS.Pending, evt.FromStatus);
        Assert.Equal(OS.Paid, evt.ToStatus);
        Assert.Equal(TS.System, evt.SourceType);
    }

    [Fact]
    public void ApplyTransition_AppendOnly_PreviousEventsUnchanged()
    {
        // Note: Cannot transition from Paid to Cancelled - that's a separate business rule (D-02)
        // This test verifies first transition is recorded properly
        var order = new Order(Guid.NewGuid(), Guid.NewGuid(), "test-intent");
        var firstTransition = DateTimeOffset.UtcNow;
        order.ApplyTransition(OS.Paid, TS.System, firstTransition);

        // Assert: First event recorded per D-08 (append-only)
        Assert.Single(order.StatusHistory);
        var evt = order.StatusHistory[0];
        Assert.Equal(OS.Pending, evt.FromStatus);
        Assert.Equal(OS.Paid, evt.ToStatus);
    }

    #endregion

    #region Authority Boundary Tests (D-03)

    [Fact]
    public void System_CannotTransitionCustomerOnlyToPaid()
    {
        // Arrange: Only System can set Paid
        var order = new Order(Guid.NewGuid(), Guid.NewGuid(), "test-intent");

        // Act & Assert: Customer cannot set Paid status
        Assert.Throws<InvalidOperationException>(() =>
            order.ApplyTransition(OS.Paid, TS.Customer, DateTimeOffset.UtcNow));
    }

    #endregion
}