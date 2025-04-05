using Microsoft.Extensions.DependencyInjection;
using System;

namespace DTM
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddDeferredTaskManager<T>(this IServiceCollection services, Action<DeferredTaskManagerOptions<T>> configureOptions)
        {
            services.Configure(configureOptions);

            services.AddSingleton<IPoolPubSub, PoolPubSub>();

            services.AddSingleton<IEventStorage<T>, EventStorageDefault<T>>();

            services.AddSingleton<IEventSender<T>, EventSenderDefault<T>>();

            services.AddSingleton<IDeferredTaskManagerService<T>, DeferredTaskManagerService<T>>();

            return services;
        }
    }
}
