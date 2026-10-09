using FluentAssertions;
using Moq;
using VetRIS.Application.Billing.Commands;
using VetRIS.Domain.Entities.Billing;
using VetRIS.Domain.Enums;
using VetRIS.Domain.Exceptions;
using VetRIS.Domain.Repositories;
using VetRIS.Domain.ValueObjects;
using Xunit;

namespace VetRIS.Application.UnitTests.Billing;

public class RegisterPaymentCommandHandlerTests
{
    private readonly Mock<IRepository<Invoice>> _invoicesMock = new();
    private readonly Mock<IRepository<Payment>> _paymentsMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    private RegisterPaymentCommandHandler CreateHandler() =>
        new(_invoicesMock.Object, _paymentsMock.Object, _unitOfWorkMock.Object);

    private static Invoice CreateSentInvoice()
    {
        var invoice = new Invoice("INV-500", Guid.NewGuid(), DateTime.UtcNow.AddDays(30));
        invoice.AddLineItem("CT Scan", 1, new Money(2000, "INR"));
        invoice.Send();
        return invoice;
    }

    [Fact]
    public async Task Handle_InvoiceNotFound_ThrowsEntityNotFoundException()
    {
        _invoicesMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Invoice?)null);

        var command = new RegisterPaymentCommand { InvoiceId = Guid.NewGuid(), Amount = 100, Method = PaymentMethod.Cash };

        var act = async () => await CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task Handle_ValidPayment_CreatesPaymentAndUpdatesInvoiceInOneUseCase()
    {
        var invoice = CreateSentInvoice();
        _invoicesMock.Setup(r => r.GetByIdAsync(invoice.Id, It.IsAny<CancellationToken>())).ReturnsAsync(invoice);

        var command = new RegisterPaymentCommand
        {
            InvoiceId = invoice.Id,
            Amount = 2000,
            Method = PaymentMethod.Card,
            ReceivedByUserId = "cashier-1"
        };

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.Status.Should().Be(nameof(PaymentStatus.Completed));
        invoice.Status.Should().Be(InvoiceStatus.Paid);
        _paymentsMock.Verify(r => r.AddAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Once);
        _invoicesMock.Verify(r => r.Update(invoice), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PartialAmount_InvoiceRemainsPartiallyPaid()
    {
        var invoice = CreateSentInvoice();
        _invoicesMock.Setup(r => r.GetByIdAsync(invoice.Id, It.IsAny<CancellationToken>())).ReturnsAsync(invoice);

        var command = new RegisterPaymentCommand { InvoiceId = invoice.Id, Amount = 500, Method = PaymentMethod.Cash };

        await CreateHandler().Handle(command, CancellationToken.None);

        invoice.Status.Should().Be(InvoiceStatus.PartiallyPaid);
    }
}
