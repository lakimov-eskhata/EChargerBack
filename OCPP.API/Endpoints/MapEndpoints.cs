using OCPP.API.Endpoints.Endpoints;

namespace OCPP.API.Endpoints;

public static class MapEndpoints
{
    public static void MapEndpointsss(this IEndpointRouteBuilder app)
    {
        app.MapPaymentEndpoints();
        // app.MapUserEndpoints();
        // app.MapOtherEndpoints();
    }
}