namespace WebApi.Endpoints.Endpoints;

public static class PaymentEndpoints
{
    public static void MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/payments");

        group.MapGet("/", () => Results.Ok("payments endpoint not yet implemented"));

        // group.MapGet("/", async (ISender sender) =>
        // {
        //     var result = await sender.Send(new GetPaymentsQuery());
        //     return Results.Ok(result);
        // });
    }
}