using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VetRIS.Application.Billing;
using VetRIS.Application.Billing.Commands;
using VetRIS.Application.Billing.Queries;
using VetRIS.Domain.Enums;

namespace VetRIS.Api.Controllers;

[Authorize(Roles = "Billing,Accounts,HospitalAdmin,SuperAdmin")]
public class InvoicesController : ApiControllerBase
{
    [HttpPost]
    public async Task<ActionResult<InvoiceDto>> Create(CreateInvoiceCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InvoiceDto>> GetById(Guid id, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetInvoiceByIdQuery(id), ct));

    [HttpGet]
    public async Task<ActionResult> GetList([FromQuery] GetInvoicesQuery query, CancellationToken ct) =>
        Ok(await Mediator.Send(query, ct));

    [HttpGet("{id:guid}/payments")]
    public async Task<ActionResult> GetPayments(Guid id, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetPaymentsByInvoiceQuery(id), ct));

    public record AddLineItemBody(string Description, int Quantity, decimal UnitPrice);

    [HttpPost("{id:guid}/line-items")]
    public async Task<ActionResult<InvoiceDto>> AddLineItem(Guid id, AddLineItemBody body, CancellationToken ct) =>
        Ok(await Mediator.Send(new AddInvoiceLineItemCommand { InvoiceId = id, Description = body.Description, Quantity = body.Quantity, UnitPrice = body.UnitPrice }, ct));

    public record ApplyGstBody(decimal GstRate);

    [HttpPost("{id:guid}/apply-gst")]
    public async Task<ActionResult<InvoiceDto>> ApplyGst(Guid id, ApplyGstBody body, CancellationToken ct) =>
        Ok(await Mediator.Send(new ApplyGstCommand { InvoiceId = id, GstRate = body.GstRate }, ct));

    [HttpPost("{id:guid}/send")]
    public async Task<ActionResult<InvoiceDto>> Send(Guid id, CancellationToken ct) =>
        Ok(await Mediator.Send(new SendInvoiceCommand(id), ct));

    public record RegisterPaymentBody(decimal Amount, PaymentMethod Method, string? TransactionReference);

    [HttpPost("{id:guid}/payments")]
    public async Task<ActionResult<PaymentDto>> RegisterPayment(Guid id, RegisterPaymentBody body, CancellationToken ct) =>
        Ok(await Mediator.Send(new RegisterPaymentCommand
        {
            InvoiceId = id,
            Amount = body.Amount,
            Method = body.Method,
            ReceivedByUserId = CurrentUser.UserId,
            TransactionReference = body.TransactionReference
        }, ct));
}
