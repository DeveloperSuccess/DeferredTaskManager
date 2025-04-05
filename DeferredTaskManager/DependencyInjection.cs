using Microsoft.Extensions.DependencyInjection;

namespace DTM
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddDeferredTaskManager<T>(this IServiceCollection services)
        {
            services.AddSingleton<DeferredTaskManagerOptions<T>, DeferredTaskManagerOptions<T>>();

            services.AddSingleton<IPoolPubSub, PoolPubSub>();

            services.AddSingleton<IEventStorage<T>, EventStorageDefault<T>>();

            services.AddSingleton<IEventSender<T>, EventSenderDefault<T>>();

            services.AddSingleton<IDeferredTaskManagerService<T>, DeferredTaskManagerService<T>>();

            return services;
        }
    }
}
