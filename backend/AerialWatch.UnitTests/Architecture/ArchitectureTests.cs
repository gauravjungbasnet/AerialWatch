using System.Reflection;
using AerialWatch.Application;
using AerialWatch.Contracts.Common;
using AerialWatch.Domain.Wfp.ResearchModel;
using AerialWatch.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace AerialWatch.UnitTests.Architecture;

/// <summary>
/// Keeps the code base from turning into spaghetti as it grows (D-027). Each rule reads the compiled assemblies,
/// so a wrong project reference, a controller that skips the service layer, or one module reaching into another
/// fails `dotnet test` straight away. Rules are explained in docs/06-architecture.md.
/// </summary>
public class ArchitectureTests
{
    private static readonly Assembly Domain = typeof(Hvala2023Coefficients).Assembly;
    private static readonly Assembly Contracts = typeof(NotFoundException).Assembly;
    private static readonly Assembly Application = typeof(DependencyInjection).Assembly;
    private static readonly Assembly Infrastructure = typeof(AerialWatchDbContext).Assembly;
    private static readonly Assembly Api = typeof(Program).Assembly;
    private static readonly Assembly[] All = { Domain, Contracts, Application, Infrastructure, Api };

    private static readonly string[] Modules = { "Wfp", "Wwl", "Wgn" };

    // ---- 1. Layers: which project may use which ----

    [Fact]
    public void Domain_depends_on_nothing() => ProjectsUsedBy(Domain, allowed: Array.Empty<string>());

    [Fact]
    public void Contracts_depend_only_on_domain()
    {
        ProjectsUsedBy(Contracts, allowed: new[] { "AerialWatch.Domain" });
        NoFramework(Contracts, "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore");
    }

    [Fact]
    public void Application_depends_only_on_contracts_and_domain()
    {
        ProjectsUsedBy(Application, allowed: new[] { "AerialWatch.Contracts", "AerialWatch.Domain" });
        NoFramework(Application, "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore");
    }

    [Fact]
    public void Infrastructure_depends_only_on_inner_layers() =>
        ProjectsUsedBy(Infrastructure, allowed: new[] { "AerialWatch.Application", "AerialWatch.Contracts", "AerialWatch.Domain" });

    // ---- 2. API: controllers talk to Contracts only ----

    [Fact]
    public void Controllers_depend_only_on_contract_interfaces()
    {
        var controllers = Api.GetTypes().Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract);
        foreach (var controller in controllers)
            foreach (var parameter in controller.GetConstructors().SelectMany(c => c.GetParameters()))
                Assert.True(parameter.ParameterType.IsInterface && parameter.ParameterType.Namespace!.StartsWith("AerialWatch.Contracts."),
                    $"{controller.Name} must take Contracts interfaces only, not {parameter.ParameterType.FullName}");
    }

    [Fact]
    public void Api_uses_application_and_infrastructure_only_in_Program()
    {
        foreach (var type in Api.GetTypes().Where(t => t != typeof(Program) && !IsCompilerGenerated(t)))
            foreach (var used in TypesUsedBy(type))
                Assert.False(used.Assembly == Application || used.Assembly == Infrastructure,
                    $"{type.Name} uses {used.FullName}; only Program.cs may touch Application/Infrastructure");
    }

    // ---- 3. Modules: Common is shared, modules never use each other ----

    [Fact]
    public void Common_code_never_depends_on_a_module()
    {
        foreach (var type in All.SelectMany(a => a.GetTypes()).Where(t => ModuleOf(t) == "Common"))
            foreach (var used in TypesUsedBy(type))
                Assert.True(ModuleOf(used) is null or "Common",
                    $"{type.FullName} is shared code but uses {used.FullName} from module {ModuleOf(used)}");
    }

    [Fact]
    public void Modules_never_depend_on_each_other()
    {
        foreach (var type in All.SelectMany(a => a.GetTypes()))
        {
            var module = ModuleOf(type);
            if (module is null or "Common") continue;
            foreach (var used in TypesUsedBy(type))
            {
                var other = ModuleOf(used);
                Assert.True(other is null or "Common" || other == module,
                    $"{type.FullName} (module {module}) uses {used.FullName} (module {other})");
            }
        }
    }

    // ---- 4. Contracts stay thin ----

    [Fact]
    public void Contracts_hold_only_interfaces_records_enums_and_exceptions()
    {
        var bad = Contracts.GetExportedTypes()
            .Where(t => !t.IsInterface && !t.IsEnum && !typeof(Exception).IsAssignableFrom(t) && t.GetMethod("<Clone>$") is null)
            .Select(t => t.FullName).ToArray();
        Assert.True(bad.Length == 0, "Implementation types in Contracts: " + string.Join(", ", bad));
    }

    [Fact]
    public void Contracts_keep_service_interfaces_in_Services_and_records_in_Models()
    {
        // Contracts/<Module>/<Feature>/Services holds I*Service; Contracts/<Module>/<Feature>/Models holds requests,
        // responses and DTOs. Common (shared exceptions, IDatabaseStatus) is exempt.
        foreach (var type in Contracts.GetExportedTypes().Where(t => ModuleOf(t) is not (null or "Common")))
        {
            var folder = type.Namespace!.Split('.').Last();
            var expected = type.IsInterface ? "Services" : "Models";
            Assert.True(folder == expected,
                $"{type.FullName} belongs in Contracts/<Module>/<Feature>/{expected} (namespace ending .{expected})");
        }
    }

    // ---- helpers ----

    private static void ProjectsUsedBy(Assembly a, string[] allowed)
    {
        var used = a.GetReferencedAssemblies().Select(r => r.Name!).Where(n => n.StartsWith("AerialWatch.", StringComparison.Ordinal));
        var extra = used.Except(allowed).ToArray();
        Assert.True(extra.Length == 0, $"{a.GetName().Name} must not reference {string.Join(", ", extra)}");
    }

    private static void NoFramework(Assembly a, params string[] prefixes)
    {
        var bad = a.GetReferencedAssemblies().Select(r => r.Name!).Where(n => prefixes.Any(p => n.StartsWith(p, StringComparison.Ordinal))).ToArray();
        Assert.True(bad.Length == 0, $"{a.GetName().Name} must not use {string.Join(", ", bad)}");
    }

    /// <summary>"Wfp", "Common" or null, from the namespace: AerialWatch.&lt;Project&gt;[.Persistence].&lt;Module&gt;....</summary>
    private static string? ModuleOf(Type t)
    {
        if (t.Namespace is null || !t.Namespace.StartsWith("AerialWatch.", StringComparison.Ordinal)) return null;
        var parts = t.Namespace.Split('.');
        return parts.Skip(2).FirstOrDefault(p => p == "Common" || Modules.Contains(p));
    }

    private static bool IsCompilerGenerated(Type t) =>
        t.Name.Contains('<') || t.GetCustomAttributes(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false).Length > 0;

    /// <summary>Types that appear in a type's signatures: base type, interfaces, fields, properties, methods.</summary>
    private static IEnumerable<Type> TypesUsedBy(Type t)
    {
        const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        var found = new List<Type>();
        if (t.BaseType is not null) found.Add(t.BaseType);
        found.AddRange(t.GetInterfaces());
        found.AddRange(t.GetFields(all).Select(f => f.FieldType));
        found.AddRange(t.GetProperties(all).Select(p => p.PropertyType));
        foreach (var m in t.GetMethods(all).Cast<MethodBase>().Concat(t.GetConstructors(all)))
        {
            if (m is MethodInfo mi) found.Add(mi.ReturnType);
            found.AddRange(m.GetParameters().Select(p => p.ParameterType));
        }
        return found.SelectMany(Unwrap).Where(x => x.Namespace?.StartsWith("AerialWatch.", StringComparison.Ordinal) == true).Distinct();
    }

    private static IEnumerable<Type> Unwrap(Type t)
    {
        if (t.HasElementType) return Unwrap(t.GetElementType()!);
        if (t.IsGenericType) return new[] { t.GetGenericTypeDefinition() }.Concat(t.GetGenericArguments().SelectMany(Unwrap));
        return new[] { t };
    }
}
