using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Windows.Threading;

namespace Minimal.Mvvm.Wpf;

internal static class DispatcherSynchronizationContextExtensions
{
    private static readonly Func<DispatcherSynchronizationContext, bool> s_checkAccess = CreateCheckAccess();

    internal static bool CheckAccess(this DispatcherSynchronizationContext sc)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(sc);
#else
        _ = sc ?? throw new ArgumentNullException(nameof(sc));
#endif
        return s_checkAccess(sc);
    }

    private static Func<DispatcherSynchronizationContext, bool> CreateCheckAccess()
    {
        var dispatcherFields = typeof(DispatcherSynchronizationContext).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        FieldInfo? dispatcherField = null;
        foreach (var field in dispatcherFields)
        {
            if (field.FieldType != typeof(Dispatcher))
            {
                continue;
            }
            if (dispatcherField != null)
            {
                throw new AmbiguousMatchException(
                    $"Multiple Dispatcher fields were found on {typeof(DispatcherSynchronizationContext).FullName}.");
            }
            dispatcherField = field;
        }
        Debug.Assert(dispatcherField != null);
        if (dispatcherField == null)
        {
            throw new MissingFieldException(typeof(DispatcherSynchronizationContext).FullName, "Dispatcher");
        }
        //return CreateCheckAccessFromExpression(dispatcherField);
        return CreateCheckAccessFromDynamicMethod(dispatcherField);
    }

    private static Func<DispatcherSynchronizationContext, bool> CreateCheckAccessFromDynamicMethod(FieldInfo dispatcherField)
    {
        var checkAccessMethod = typeof(Dispatcher).GetMethod(
            nameof(Dispatcher.CheckAccess),
            BindingFlags.Instance | BindingFlags.Public,
            binder: null,
            types: Type.EmptyTypes,
            modifiers: null);

        Debug.Assert(checkAccessMethod != null);
        if (checkAccessMethod == null)
        {
            throw new MissingMethodException(typeof(Dispatcher).FullName, nameof(Dispatcher.CheckAccess));
        }

        var method = new DynamicMethod("CheckAccess", typeof(bool), [typeof(DispatcherSynchronizationContext)],
            typeof(DispatcherSynchronizationContext), skipVisibility: true);

        var il = method.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, dispatcherField);
        il.Emit(OpCodes.Callvirt, checkAccessMethod);
        il.Emit(OpCodes.Ret);

        return (Func<DispatcherSynchronizationContext, bool>)method.CreateDelegate(
            typeof(Func<DispatcherSynchronizationContext, bool>));
    }

    private static Func<DispatcherSynchronizationContext, bool> CreateCheckAccessFromExpression(FieldInfo field)
    {
        var context = Expression.Parameter(typeof(DispatcherSynchronizationContext), "context");
        var dispatcher = Expression.Field(context, field);
        var checkAccess = Expression.Call(dispatcher, nameof(Dispatcher.CheckAccess), Type.EmptyTypes);

        return Expression.Lambda<Func<DispatcherSynchronizationContext, bool>>(checkAccess, context).Compile();
    }
}
