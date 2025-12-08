namespace Application.Common.Interfaces;

// Common/Interfaces/IMediatorHandler.cs
public interface IMediatorHandler
{
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
    Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification;
}

// Common/Interfaces/IRequest.cs
public interface IRequest<TResponse> { }

public interface IRequest : IRequest<Unit> { }

public record Unit;

// Common/Interfaces/INotification.cs
public interface INotification { }

// Common/Interfaces/IRequestHandler.cs
public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}

public interface IRequestHandler<in TRequest> : IRequestHandler<TRequest, Unit> where TRequest : IRequest<Unit>
{ }