using System;
using Ninject.Activation;
using Ninject.Activation.Caching;
using Ninject.Syntax;
using Rebus.Pipeline;

namespace Rebus.Ninject
{
    /// <summary>
    /// Extension methods for making it easy to register Rebus handlers in your <see cref="Ninject"/> dependency injector
    /// </summary>
    public static class NinjectContainerExtensions
    {
        const string LifestimeScopeItemKey = "ninject-lifetime-scope";

        /// <summary>
        /// Sets the scope to Rebus MessageContext scope.
        /// </summary>
        /// <typeparam name="T">The type of the service.</typeparam>
        /// <param name="syntax">The syntax.</param>
        /// <returns>The syntax to define more information.</returns>
        public static IBindingNamedWithOrOnSyntax<T> InRebusMessageScope<T>(this IBindingInSyntax<T> syntax)
        {
            return syntax.InScope(GetScope);
        }

        /// <summary>
        /// Gets the scope.
        /// </summary>
        /// <param name="ctx">The context.</param>
        /// <returns>The scope.</returns>
        private static object GetScope(IContext ctx)
        {
            var messageContext = MessageContext.Current;

            if (messageContext == null)
            {
                throw new InvalidOperationException(
                    $"Attempted to resolve {ctx.Request.Target.Type} outside of Rebus message context!");
            }

            var items = messageContext.TransactionContext.Items;

            object lifetimeScope;

            if (items.TryGetValue(LifestimeScopeItemKey, out lifetimeScope))
            {
                return lifetimeScope;
            }

            lifetimeScope = new MessageContextScope();
            items[LifestimeScopeItemKey] = lifetimeScope;

            messageContext.TransactionContext.OnDisposed(() =>
                ctx.Kernel.Components.Get<ICache>().Clear(lifetimeScope));

            return lifetimeScope;
        }

        private class MessageContextScope
        {
        }
    }

}