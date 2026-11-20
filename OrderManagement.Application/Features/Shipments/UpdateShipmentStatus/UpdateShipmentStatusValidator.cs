
using FluentValidation;
using OrderManagement.Domain.Enums;

namespace OrderManagement.Application.Features.Shipments.UpdateShipmentStatus;

public sealed class UpdateShipmentStatusValidator
    : AbstractValidator<UpdateShipmentStatusRequest>
{
    public UpdateShipmentStatusValidator()
    {
        RuleFor(x => x.ShipmentId)
            .GreaterThan(0)
            .WithMessage("Shipment ID must be greater than zero.");

        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("Invalid shipment status.");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Description cannot exceed 500 characters.");
    }
}
