namespace OctoHook.CommonComposition
{
    using Autofac;
    using Autofac.Builder;
    using Autofac.Core;
    using Autofac.Features.Scanning;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Provides automatic component registration by scanning assemblies and types for 
    /// those that have the <see cref="ComponentAttribute"/> annotation.
    /// </summary>
    /// <remarks>
    /// Several overloads provide seamless chaining with other Autofac registration 
    /// extensions.
    /// <example>
    /// The following example registers all annotated components from the given 
    /// given assembly on the given container builder:
    ///     <code>
    ///     var builder = new ContainerBuilder();
    ///     builder.RegisterComponents(typeof(IFoo).Assembly);
    ///     
    ///     var container = builder.Build();
    ///     </code>
    /// </example>
    /// </remarks>
    public static class CompositionExtensions
    {
        /// <summary>
        /// Registers the components found in the given assemblies.
        /// </summary>
        public static IRegistrationBuilder<object, ScanningActivatorData, DynamicRegistrationStyle> RegisterComponents(this ContainerBuilder builder, params Assembly[] assemblies)
        {
            // Allow non-public types just like MEF does.
            return RegisterComponents(builder, assemblies.SelectMany(x => x.GetTypes()));
        }

        /// <summary>
        /// Registers the components found in the given set of types.
        /// </summary>
        public static IRegistrationBuilder<object, ScanningActivatorData, DynamicRegistrationStyle> RegisterComponents(this ContainerBuilder builder, params Type[] types)
        {
            return RegisterComponents(builder, (IEnumerable<Type>)types);
        }

        /// <summary>
        /// Registers the components found in the given set of types.
        /// </summary>
        public static IRegistrationBuilder<object, ScanningActivatorData, DynamicRegistrationStyle> RegisterComponents(this ContainerBuilder builder, IEnumerable<Type> types)
        {
            var registration = builder
                .RegisterTypes(types.Where(t => t.GetCustomAttributes(true).OfType<ComponentAttribute>().Any()).ToArray())
                .AsSelf()
                .AsImplementedInterfaces();
                // TODO: auto-set properties?
                //.PropertiesAutowired(PropertyWiringOptions.PreserveSetValues);

            registration.As(t => 
            {
                var name = t.GetCustomAttributes(true).OfType<NamedAttribute>().Select(x => x.Name).FirstOrDefault();
                if (string.IsNullOrEmpty(name))
                    return Enumerable.Empty<Service>();

                return t.GetInterfaces()
                    .Where(i => i != typeof(IDisposable))
                    .Select(i => new KeyedService(name, i))
                    .Concat(new[] { new KeyedService(name, t) })
                    .ToArray();
            });

            registration.ActivatorData.ConfigurationActions.Add((t, rb) =>
            {
                // Optionally set the SingleInstance behavior.
                // TODO: check SingletonScope
                if (rb.ActivatorData.ImplementationType.GetCustomAttributes(true).OfType<ComponentAttribute>().First().IsSingleton)
                    rb.SingleInstance();
            });

            return registration;
        }
    }
}