
using FluentValidation;

namespace OrderManagement.Application.Features.Shipments.GetShipmentStatusHistory;

public sealed class GetShipmentStatusHistoryValidator
    : AbstractValidator<GetShipmentStatusHistoryRequest>
{
    public GetShipmentStatusHistoryValidator()
    {
        RuleFor(x => x.ShipmentId)
            .GreaterThan(0)
            .WithMessage("Shipment ID must be greater than zero.");
    }
}
