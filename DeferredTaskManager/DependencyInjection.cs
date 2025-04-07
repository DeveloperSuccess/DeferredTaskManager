using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace DTM
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddDeferredTaskManager<T>(
            this IServiceCollection services,
            Action<DeferredTaskManagerOptions<T>> configureOptions,
            Type? pubSubType = null,
            Type? eventStorageType = null,
            Type? eventSenderType = null,
            Type? deferredTaskManagerServiceType = null)
        {
            services.Configure(configureOptions);

            AddSingleton<IPoolPubSub, PoolPubSub>(pubSubType);

            AddSingleton<IEventStorage<T>, EventStorageDefault<T>>(eventStorageType);

            AddSingleton<IEventSender<T>, EventSenderDefault<T>>(eventSenderType);

            AddSingleton<IDeferredTaskManagerService<T>, DeferredTaskManagerService<T>>(deferredTaskManagerServiceType);

            void AddSingleton<TServiceType, TDefaultType>(Type? customType)
            {
                services.AddSingleton(typeof(TServiceType), serviceProvider =>
                {
                    if (customType != null)
                        return Activator.CreateInstance(customType, GetConstructorArguments(serviceProvider, customType));

                    return Activator.CreateInstance(typeof(TDefaultType), GetConstructorArguments(serviceProvider, typeof(TDefaultType)));
                });
            }

            object[] GetConstructorArguments(IServiceProvider serviceProvider, Type type)
            {
                var constructor = type.GetConstructors()
               .FirstOrDefault(c => c.GetCustomAttributes(typeof(ActivatorUtilitiesConstructorAttribute), true).Any())
                ?? type.GetConstructors().OrderByDescending(c => c.GetParameters().Length).FirstOrDefault();

                if (constructor == null)
                {
                    throw new InvalidOperationException($"No public constructor found for type {type.FullName}.");
                }

                var parameters = constructor.GetParameters();
                object[] arguments = new object[parameters.Length];

                for (int i = 0; i < parameters.Length; i++)
                {
                    var parameterType = parameters[i].ParameterType;
                    arguments[i] = serviceProvider.GetRequiredService(parameterType);
                }

                return arguments;
            }

            return services;
        }
    }
}
