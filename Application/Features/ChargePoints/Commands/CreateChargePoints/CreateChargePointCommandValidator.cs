using FluentValidation;

namespace Application.Features.ChargePoints.Commands.CreateChargePoints;

public class CreateChargePointCommandValidator : AbstractValidator<CreateChargePointCommand>
{
    public CreateChargePointCommandValidator()
    {
        RuleFor(x => x.ChargePointId)
            .NotEmpty().WithMessage("Charge Point ID is required")
            .MaximumLength(50).WithMessage("Charge Point ID must not exceed 50 characters");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters");
    }
}