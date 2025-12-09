using System.Reflection;
using Application;
using Application.Common.Interfaces;
using Application.Common.Middleware;
using Infrastructure;
using WebApi;
using WebApi.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// // Add services
builder.Services.AddWebApi(builder.Configuration);
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

builder.Logging.AddConsole();


// MediatR
builder.Services.Scan(scan => scan
    .FromAssemblies(Assembly.GetExecutingAssembly())
    .AddClasses(classes => classes.AssignableTo(typeof(IRequestHandler<,>)))
    .AsImplementedInterfaces()
    .WithScopedLifetime());

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseWebSockets(); 
app.UseHttpsRedirection();

// Configure OCPP 1.6 endpoint
// app.MapSimpleRToOcpp("/ocpp16/{chargePointId}/", b =>
// {
//     b.UseOcppProtocol().UseDispatcher<Ocpp16Dispatcher>();
// });

// // Configure OCPP 2.0.1 endpoint
// app.MapSimpleRToOcpp("/ocpp201/{chargePointId}/", b =>
// {
//     b.UseOcppProtocol().UseDispatcher<Ocpp20Dispatcher>();
// });

app.MapEndpointsss(); // единый вызов всех эндпоинтов

// Middleware
app.UseExceptionHandling();

// // Admin HTTP API for commands
// app.MapPost("/api/stations/{cpid}/remote-start", async ([FromRoute] string cpid, [FromQuery] int connectorId, [FromBody] RemoteStartDto body, IMediator mediator) =>
// {
//     var ok = await mediator.Send(new RemoteStartCommand(cpid, connectorId, body.IdTag));
//     return ok ? Results.Ok() : Results.BadRequest();
// });
//
// app.MapPost("/api/stations/{cpid}/remote-stop", async ([FromRoute] string cpid, [FromBody] RemoteStopDto body, IMediator mediator) =>
// {
//     var ok = await mediator.Send(new RemoteStopCommand(cpid, body.SessionOrTxnId));
//     return ok ? Results.Ok() : Results.BadRequest();
// });
//
// app.MapPost("/api/stations/{cpid}/unlock", async ([FromRoute] string cpid, [FromQuery] int connectorId, IMediator mediator) =>
// {
//     var ok = await mediator.Send(new UnlockConnectorCommand(cpid, connectorId));
//     return ok ? Results.Ok() : Results.BadRequest();
// });

app.Run();