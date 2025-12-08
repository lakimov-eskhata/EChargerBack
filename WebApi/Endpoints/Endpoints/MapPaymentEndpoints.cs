using Application.Common;
using Application.Common.Interfaces;
using Application.Features.Command.ProcessBootNotification;

namespace WebApi.Endpoints.Endpoints;

public static class PaymentEndpoints
{
    public static void MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/payments");

        group.MapPost("/", async (ProcessBootNotificationCommand request, IMediatorHandler  mediator) =>
            {
                var result = await mediator.Send(request);
                return Results.Ok(result);
            })
            .AddEndpointFilter<CustomAuthorizationFilter>();

        // group.MapGet("/", async (ISender sender) =>
        // {
        //     var result = await sender.Send(new GetPaymentsQuery());
        //     return Results.Ok(result);
        // });
    }
}