using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Windows;

namespace Minimal.Mvvm.Wpf;

/// <summary>
/// Locates and creates Views by registered, short, or fully qualified type name.
/// </summary>
public class ViewLocator : ViewLocatorBase
{
    private static ViewLocatorBase? s_custom;

    private readonly ConcurrentDictionary<string, Type> _registeredTypes = new();
    private readonly ConcurrentDictionary<string, Type> _nameCache = new();
    private readonly ConcurrentDictionary<string, Type> _fullNameCache = new();
    private readonly ConcurrentDictionary<Assembly, Type[]> _assemblyCache = new();
    private readonly ConcurrentDictionary<Assembly, bool> _skipAssemblyCache = new();

    #region Properties

    /// <summary>
    /// Gets the assemblies to search for view types.
    /// </summary>
    protected IEnumerable<Assembly> Assemblies => GetAssemblies();

    /// <summary>
    /// Gets or sets the default instance of the <see cref="ViewLocatorBase"/>.
    /// </summary>
    public static ViewLocatorBase Default
    {
        get => s_custom ?? field;
        set => s_custom = value;
    } = new ViewLocator();

    #endregion

    #region Methods

    /// <summary>
    /// Clears the internal cache of view types.
    /// </summary>
    public void ClearCache()
    {
        _nameCache.Clear();
        _fullNameCache.Clear();
        _assemblyCache.Clear();
        _skipAssemblyCache.Clear();
    }

    /// <summary>
    /// Clears all registered view types.
    /// </summary>
    public void ClearRegisteredTypes()
    {
        _registeredTypes.Clear();
    }

    /// <summary>
    /// Gets the assemblies to search for view types. This method can be overridden to customize the assembly collection.
    /// </summary>
    /// <returns>An enumerable collection of assemblies.</returns>
    protected virtual IEnumerable<Assembly> GetAssemblies()
    {
        return AppDomain.CurrentDomain.GetAssemblies();
    }

    /// <summary>
    /// Gets the type of the view based on the specified view name.
    /// </summary>
    /// <param name="viewName">The name of the view.</param>
    /// <returns>The type of the view if found; otherwise, null.</returns>
    protected override Type? GetViewType(string? viewName)
    {
        if (string.IsNullOrWhiteSpace(viewName))
        {
            return null;
        }

        if (_registeredTypes.TryGetValue(viewName!, out var registeredType))
        {
            return registeredType;
        }
        if (_nameCache.TryGetValue(viewName!, out var byName))
        {
            return byName;
        }
        if (_fullNameCache.TryGetValue(viewName!, out var byFullName))
        {
            return byFullName;
        }

        foreach (var type in GetTypes())
        {
            Debug.Assert(type != null);
            if (string.Equals(type!.Name, viewName, StringComparison.Ordinal))
            {
                return (_nameCache[type.Name] = type);
            }
            if (string.Equals(type.FullName, viewName, StringComparison.Ordinal))
            {
                return (_fullNameCache[type.FullName!] = type);
            }
        }
        return null;
    }

    /// <summary>
    /// Returns view-eligible types from assemblies specified by <see cref="Assemblies"/> using cached assembly filtering.
    /// </summary>
    /// <returns>An enumerable collection of view candidate types.</returns>
    protected virtual IEnumerable<Type> GetTypes()
    {
        var entryAssembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var processedAssemblies = new HashSet<Assembly>() { entryAssembly };

        // 1. Entry assembly first (most likely to contain views)
        foreach (var type in _assemblyCache.GetOrAdd(entryAssembly, GetAssemblyTypes))
        {
            yield return type;
        }

        // 2. Other assemblies
        foreach (Assembly assembly in Assemblies)
        {
            if (!processedAssemblies.Add(assembly))
            {
                continue;
            }
            if (_skipAssemblyCache.GetOrAdd(assembly, ShouldSkipAssembly))
            {
                continue;
            }

            foreach (var type in _assemblyCache.GetOrAdd(assembly, GetAssemblyTypes))
            {
                yield return type;
            }
        }
    }

    private static Type[] GetAssemblyTypes(Assembly assembly)
    {
        var builder = new ValueListBuilder<Type>(100);
        Type?[] assemblyTypes;
        try
        {
            assemblyTypes = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            assemblyTypes = ex.Types;
        }
        foreach (var type in assemblyTypes)
        {
            if (type == null || ShouldSkipType(type))
            {
                continue;
            }
            builder.Append(type);
        }
        Debug.Assert(builder.Length < 128, $"builder.Length: {builder.Length}. Increase capacity.");
        return builder.ToArray();
    }

    /// <summary>
    /// Determines whether to skip the specified type.
    /// </summary>
    private static bool ShouldSkipType(Type type)
    {
        // Skip abstract, interface, generic type definitions types and enums with ValueType (not Views)
        if (type.IsAbstract || type.IsInterface || type.IsGenericTypeDefinition || type.IsValueType ||
            !typeof(DependencyObject).IsAssignableFrom(type))
        {
            return true;
        }

        // Skip anonymous && compiler-generated types
        var typeName = type.Name;
        if (typeName.Length >= 2 && typeName[0] == '<' && typeName.IndexOf('>') > 0)
        {
            return true;
        }

        // Skip types in system namespaces (even in user assemblies)
        var typeNamespace = type.Namespace;
        if (typeNamespace != null &&
           (typeNamespace.StartsWith("System.", StringComparison.Ordinal) ||
            typeNamespace.StartsWith("Microsoft.", StringComparison.Ordinal) ||
            typeNamespace.StartsWith("Windows.", StringComparison.Ordinal)))
        {
            return true;
        }

        // Skip delegates, attributes. etc. (not Views)
        var baseTypeFullName = type.BaseType?.FullName;
        if (baseTypeFullName != null && (baseTypeFullName == "System.Delegate" ||
                                        baseTypeFullName == "System.MulticastDelegate" ||
                                        baseTypeFullName == "System.Attribute" ||
                                        baseTypeFullName == "System.Object" ||
                                        baseTypeFullName == "System.EventArgs" ||
                                        baseTypeFullName == "System.Exception"))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Determines whether types from the specified assembly should be excluded from view discovery.
    /// </summary>
    protected virtual bool ShouldSkipAssembly(Assembly assembly)
    {
        if (assembly.IsDynamic)
        {
            return true;
        }

        var assemblyName = assembly.GetName().Name;
        if (assemblyName == null)
        {
            return true;
        }

        // System assemblies
        if (assemblyName.StartsWith("System.", StringComparison.Ordinal) ||
            assemblyName.StartsWith("Microsoft.", StringComparison.Ordinal) ||
            assemblyName.StartsWith("Windows.", StringComparison.Ordinal) ||
            assemblyName.StartsWith("WindowsBase", StringComparison.Ordinal) ||
            assemblyName.StartsWith("WindowsForms", StringComparison.Ordinal) ||
            assemblyName.StartsWith("Presentation", StringComparison.Ordinal) ||
            assemblyName.StartsWith("mscorlib", StringComparison.Ordinal) ||
            assemblyName.StartsWith("netstandard", StringComparison.Ordinal) ||
            assemblyName.StartsWith("Accessibility", StringComparison.Ordinal) ||
            assemblyName.StartsWith("UIAutomation", StringComparison.Ordinal))
        {
            return true;
        }

        // Known infrastructure assemblies that do not contain Views
        if (assemblyName.StartsWith("Newtonsoft.", StringComparison.Ordinal) ||
            assemblyName.StartsWith("NLog", StringComparison.Ordinal) ||
            assemblyName.StartsWith("log4net", StringComparison.Ordinal) ||
            assemblyName.StartsWith("Serilog", StringComparison.Ordinal) ||
            assemblyName.StartsWith("AutoMapper", StringComparison.Ordinal) ||
            assemblyName.StartsWith("EntityFramework", StringComparison.Ordinal) ||
            assemblyName.StartsWith("Dapper", StringComparison.Ordinal) ||
            assemblyName.StartsWith("NuExt.System", StringComparison.Ordinal))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Registers a view type with a specified name.
    /// </summary>
    /// <param name="name">The name to associate with the view type.</param>
    /// <param name="type">The type of the view to register.</param>
    /// <exception cref="ArgumentNullException">Thrown if the name or type is null.</exception>
    public void RegisterType(string name, Type type)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(type);

        _registeredTypes[name] = type;
    }

    /// <summary>
    /// Unregisters a view type with a specified name.
    /// </summary>
    /// <param name="name">The name associated with the view type to unregister.</param>
    /// <returns>True if the view type was successfully unregistered; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the name is null or empty.</exception>
    public bool UnregisterType(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        return _registeredTypes.TryRemove(name, out _);
    }

    #endregion
}
