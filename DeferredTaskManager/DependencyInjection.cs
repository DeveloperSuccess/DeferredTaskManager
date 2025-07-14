using Microsoft.Extensions.DependencyInjection;
using System;
using System.Data;
using System.Linq;

namespace DTM
{
    /// <summary>
    /// Dependency Injection for DeferredTaskManager
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Dependency Injection for DeferredTaskManager
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="services">IServiceCollection</param>
        /// <param name="configureOptions">Parameters for DeferredTaskManager</param>
        /// <param name="lifetime">Service Lifetime</param>
        /// <param name="pubSubType">Overriding the PubSub module</param>
        /// <param name="storageStrategyType">Overriding the CollectionStrategy module</param>
        /// <param name="eventStorageType">Overriding the EventStorage module</param>
        /// <param name="eventSenderType">Overriding the EventSender module</param>
        /// <param name="deferredTaskManagerServiceType">Overriding the DeferredTaskManagerService module</param>
        public static IServiceCollection AddDeferredTaskManager<T>(this IServiceCollection services,
            Action<DeferredTaskManagerOptions<T>> configureOptions,
            ServiceLifetime lifetime = ServiceLifetime.Singleton,
            Type? pubSubType = null,
            Type? storageStrategyType = null,
            Type? eventStorageType = null,
            Type? eventSenderType = null,
            Type? deferredTaskManagerServiceType = null)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configureOptions == null) throw new ArgumentNullException(nameof(configureOptions));

            services.Configure(configureOptions);

            AddDependencyInjection<IPoolPubSub<T>, PoolPubSub<T>>(services, pubSubType, lifetime);

            AddDependencyInjectionStorageStrategy<T>(services, storageStrategyType, lifetime, configureOptions);

            AddDependencyInjection<IEventStorage<T>, EventStorageDefault<T>>(services, eventStorageType, lifetime);

            AddDependencyInjection<IEventSender<T>, EventSenderDefault<T>>(services, eventSenderType, lifetime);

            AddDependencyInjection<IDeferredTaskManagerService<T>, DeferredTaskManagerService<T>>(services, deferredTaskManagerServiceType, lifetime);

            return services;
        }

        static void AddDependencyInjection<TServiceType, TDefaultType>(IServiceCollection services,
            Type? customType, ServiceLifetime lifetime)
        {
            switch (lifetime)
            {
                case ServiceLifetime.Singleton:
                    services.AddSingleton(typeof(TServiceType), serviceProvider =>
                    {
                        return CreateServiceInstance<TServiceType, TDefaultType>(serviceProvider, customType);
                    });
                    break;
                case ServiceLifetime.Scoped:
                    services.AddScoped(typeof(TServiceType), serviceProvider =>
                    {
                        return CreateServiceInstance<TServiceType, TDefaultType>(serviceProvider, customType);
                    });
                    break;
                case ServiceLifetime.Transient:
                    throw new NotSupportedException($"{nameof(ServiceLifetime.Transient)} Not Supported");
            }
        }

        static void AddDependencyInjectionStorageStrategy<T>(IServiceCollection services,
            Type? customType, ServiceLifetime lifetime, Action<DeferredTaskManagerOptions<T>> configureOptions)
        {
            if (customType != null)
            {
                AddDependencyInjection<IStorageStrategy<T>, EventStorageDefault<T>>(services, customType, lifetime);
            }
            else
            {
                var options = new DeferredTaskManagerOptions<T>();

                configureOptions(options);

                if (options.CollectionType == CollectionType.Bag)
                {
                    AddDependencyInjection<IStorageStrategy<T>, BagStrategy<T>>(services, null, lifetime);
                }
                else
                {
                    AddDependencyInjection<IStorageStrategy<T>, QueueStrategy<T>>(services, null, lifetime);
                }
            }
        }

        static object CreateServiceInstance<TServiceType, TDefaultType>(IServiceProvider serviceProvider, Type? customType)
        {
            var implementationType = customType ?? typeof(TDefaultType);

            return Activator.CreateInstance(implementationType, GetConstructorArguments(serviceProvider, implementationType));
        }

        static object[] GetConstructorArguments(IServiceProvider serviceProvider, Type type)
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
    }
}
