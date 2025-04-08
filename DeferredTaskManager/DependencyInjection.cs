using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Linq;

namespace DTM
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddDeferredTaskManagerSingleton<T>(
            this IServiceCollection services,
            Action<DeferredTaskManagerOptions<T>> configureOptions,
            Type? pubSubType = null,
            Type? eventStorageType = null,
            Type? eventSenderType = null,
            Type? deferredTaskManagerServiceType = null)
        {
            AddDependencyInjection<T>(services, configureOptions, DIType.Singleton, pubSubType, eventStorageType, eventSenderType, deferredTaskManagerServiceType);

            return services;
        }
        public static IServiceCollection AddDeferredTaskManagerScoped<T>(
            this IServiceCollection services,
            Action<DeferredTaskManagerOptions<T>> configureOptions,
            Type? pubSubType = null,
            Type? eventStorageType = null,
            Type? eventSenderType = null,
            Type? deferredTaskManagerServiceType = null)
        {
            AddDependencyInjection<T>(services, configureOptions, DIType.Scoped, pubSubType, eventStorageType, eventSenderType, deferredTaskManagerServiceType);

            return services;
        }

        public static IServiceCollection AddDeferredTaskManagerTransient<T>(
            this IServiceCollection services,
            Action<DeferredTaskManagerOptions<T>> configureOptions,
            Type? pubSubType = null,
            Type? eventStorageType = null,
            Type? eventSenderType = null,
            Type? deferredTaskManagerServiceType = null)
        {
            AddDependencyInjection<T>(services, configureOptions, DIType.Transient, pubSubType, eventStorageType, eventSenderType, deferredTaskManagerServiceType);

            return services;
        }

        static void AddDependencyInjection<T>(IServiceCollection services, Action<DeferredTaskManagerOptions<T>> configureOptions, DIType dIType,
            Type? pubSubType = null,
            Type? eventStorageType = null,
            Type? eventSenderType = null,
            Type? deferredTaskManagerServiceType = null)
        {
            services.Configure(configureOptions);

            Add<T, IPoolPubSub, PoolPubSub>(services, configureOptions, pubSubType, dIType);

            Add<T, IEventStorage<T>, EventStorageDefault<T>>(services, configureOptions, eventStorageType, dIType);

            Add<T, IEventSender<T>, EventSenderDefault<T>>(services, configureOptions, eventSenderType, dIType);

            Add<T, IDeferredTaskManagerService<T>, DeferredTaskManagerService<T>>(services, configureOptions, deferredTaskManagerServiceType, dIType);
        }

        static void Add<T, TServiceType, TDefaultType>(IServiceCollection services, Action<DeferredTaskManagerOptions<T>> configureOptions, Type? customType, DIType dIType)
        {
            services.Configure(configureOptions);

            switch (dIType)
            {
                case DIType.Singleton:
                    services.AddSingleton(typeof(TServiceType), serviceProvider =>
                    {
                        return BodyAddDI<TServiceType, TDefaultType>(serviceProvider, customType);
                    });
                    break;
                case DIType.Scoped:
                    services.AddScoped(typeof(TServiceType), serviceProvider =>
                    {
                        return BodyAddDI<TServiceType, TDefaultType>(serviceProvider, customType);
                    });
                    break;
                case DIType.Transient:
                    services.AddTransient(typeof(TServiceType), serviceProvider =>
                    {
                        return BodyAddDI<TServiceType, TDefaultType>(serviceProvider, customType);
                    });
                    break;
            }
        }

        static object BodyAddDI<TServiceType, TDefaultType>(IServiceProvider serviceProvider, Type? customType)
        {
            if (customType != null)
                return Activator.CreateInstance(customType, GetConstructorArguments(serviceProvider, customType));

            return Activator.CreateInstance(typeof(TDefaultType), GetConstructorArguments(serviceProvider, typeof(TDefaultType)));
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
