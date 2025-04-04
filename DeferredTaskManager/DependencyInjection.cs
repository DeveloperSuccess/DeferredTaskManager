using Microsoft.Extensions.DependencyInjection;

namespace DTM
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddDeferredTaskManager<T>(this IServiceCollection services)
        {
            services.AddScoped<IPubSub, PubSub>();

            services.AddScoped<IEventStorage<T>, EventStorageDefault<T>>();

            services.AddScoped<IEventSender<T>, EventSenderDefault<T>>();

            return services;
        }
    }
}
