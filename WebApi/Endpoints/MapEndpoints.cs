using WebApi.Endpoints.Endpoints;

namespace WebApi.Endpoints;

public static class MapEndpoints
{
    public static void MapEndpointsss(this IEndpointRouteBuilder app)
    {
        app.MapPaymentEndpoints();
        // app.MapUserEndpoints();
        // app.MapOtherEndpoints();
    }
}