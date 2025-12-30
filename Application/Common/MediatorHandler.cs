using Application.Common.Behaviors;
using Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Common;

public class MediatorHandler : IMediatorHandler
{
    private readonly IServiceProvider _serviceProvider;

    public MediatorHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(request.GetType(), typeof(TResponse));
        var handler = _serviceProvider.GetRequiredService(handlerType);
        
        // Получаем все поведения
        var behaviorType = typeof(IPipelineBehavior<,>).MakeGenericType(request.GetType(), typeof(TResponse));
        var behaviors = _serviceProvider.GetServices(behaviorType).Cast<object>().ToList();
        
        RequestHandlerDelegate<TResponse> handlerDelegate = () => 
            (Task<TResponse>)handlerType.GetMethod("Handle").Invoke(handler, new object[] { request, cancellationToken });

        // Применяем поведения в обратном порядке
        
        behaviors.Reverse();
        foreach (var behavior in behaviors)
        {
            var next = handlerDelegate;
            var currentBehavior = behavior;
            handlerDelegate = () => (Task<TResponse>)behaviorType.GetMethod("Handle")
                .Invoke(currentBehavior, new object[] { request, next, cancellationToken });
        }

        return await handlerDelegate();
    }

    public async Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
    {
        var handlerType = typeof(INotificationHandler<>).MakeGenericType(typeof(TNotification));
        var handlers = _serviceProvider.GetServices(handlerType);

        var tasks = handlers.Select(handler =>
            (Task)handlerType.GetMethod("Handle").Invoke(handler, new object[] { notification, cancellationToken }));

        await Task.WhenAll(tasks);
    }
}

public interface INotificationHandler<in TNotification> where TNotification : INotification
{
    Task Handle(TNotification notification, CancellationToken cancellationToken);
}