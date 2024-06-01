### Reflection

```C#
<ItemGroup>
  <Using Include="System.Console" Static="true" />
</ItemGroup>
```

```C#
using System.Reflection; // To use Assembly.
WriteLine("Assembly metadata:");
Assembly? assembly = Assembly.GetEntryAssembly();
if (assembly is null)
{
  WriteLine("Failed to get entry assembly.");
  return; // Exit the app.
}
WriteLine($"  Full name: {assembly.FullName}"); 
WriteLine($"  Location: {assembly.Location}");
WriteLine($"  Entry point: {assembly.EntryPoint?.Name}");
IEnumerable<Attribute> attributes = assembly.GetCustomAttributes(); 
WriteLine($"  Assembly-level attributes:");
foreach (Attribute a in attributes)
{
  WriteLine($"    {a.GetType()}");
}

AssemblyInformationalVersionAttribute? version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
WriteLine($"  Version: {version?.InformationalVersion}");
AssemblyCompanyAttribute? company = assembly.GetCustomAttribute<AssemblyCompanyAttribute>();
WriteLine($"  Company: {company?.Company}");
```

```C#
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <Version>8.0.1</Version>
    <Company>Packt Publishing</Company>
  </PropertyGroup>
</Project>
```


**Creating custom attributes**

*CoderAttribute.cs*
```C#
namespace Packt.Shared;
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class CoderAttribute(string coder, string lastModified) : Attribute
{
  public string Coder { get; set; } = coder;
  public DateTime LastModified { get; set; } = DateTime.Parse(lastModified);
}
```

*Animal.cs*
```C#
namespace Packt.Shared;
public class Animal
{
  [Coder("Mark Price", "22 June 2024")]
  [Coder("Johnni Rasmussen", "13 July 2024")] 
  public void Speak()
  {
    WriteLine("Woof...");
  }
}
```

```C#
using Packt.Shared; // To use CoderAttribute.

WriteLine();
WriteLine("* Types:");
Type[] types = assembly.GetTypes();
foreach (Type type in types)
{
  WriteLine();
  WriteLine($"Type: {type.FullName}"); 
  MemberInfo[] members = type.GetMembers();
  foreach (MemberInfo member in members)
  {
    WriteLine($"{member.MemberType}: {member.Name} ({ member.DeclaringType?.Name })");
    IOrderedEnumerable<CoderAttribute> coders = member.GetCustomAttributes<CoderAttribute>().OrderByDescending(c => c.LastModified);
    foreach (CoderAttribute coder in coders)
    {
      WriteLine($"-> Modified by {coder.Coder} on {coder.LastModified.ToShortDateString()}");
    }
  }
}
```


**Making a type or member obsolete**

```C#
public void SpeakBetter()
{
  WriteLine("Wooooooooof...");
}

[Coder("Mark Price", "22 August 2024")]
[Coder("Johnni Rasmussen", "13 September 2024")]
[Obsolete($"use {nameof(SpeakBetter)} instead.")]
public void Speak()
{
  WriteLine("Woof...");
}
```

```C#
foreach (MemberInfo member in members)
{
  ObsoleteAttribute? obsolete = member.GetCustomAttribute<ObsoleteAttribute>();
  WriteLine($"{member.MemberType}: {member.Name} ({member.DeclaringType?.Name}) {(obsolete is null ? "" : "Obsolete! " + obsolete.Message)}");
}
```


**Dynamically loading assemblies and executing methods**

```C#
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
  <ItemGroup>
    <Using Include="System.Reflection" />
    <Using Include="System.Console" Static="true" />
  </ItemGroup>
</Project>
```

```C#
namespace DynamicLoadAndExecute.Library;

public class Dog
{
  public void Speak(string? name)
  {
    WriteLine($"{name} says Woof!");
  }
}
```

*DynamicLoadAndExecute.Console.csproj*

```C#
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
  <ItemGroup>
    <Using Include="System.Reflection" />
    <Using Include="System.Console" Static="true" />
  </ItemGroup>
</Project>
```

*Program.Helpers.cs*

```C#
// No explicit namespace!
partial class Program
{
  private static void OutputAssemblyInfo(Assembly a)
  {
    WriteLine($"FullName: {a.FullName}");
    WriteLine($"Location: {Path.GetDirectoryName(a.Location)}");
    WriteLine($"IsCollectible: {a.IsCollectible}");
    WriteLine("Defined types:");

    foreach (TypeInfo info in a.DefinedTypes)
    {
      if (!info.Name.EndsWith("Attribute"))
      {
        WriteLine($"  Name: {info.Name}, Members: {info.GetMembers().Count()}");
      }
    }
    WriteLine();
  }
}
```

*DemoAssemblyLoadContext.cs*

```C#
using System.Runtime.Loader; // To use AssemblyDependencyResolver.

internal class DemoAssemblyLoadContext : AssemblyLoadContext
{
  private AssemblyDependencyResolver _resolver;

  public DemoAssemblyLoadContext(string mainAssemblyToLoadPath)
    : base(isCollectible: true)
  {
    _resolver = new AssemblyDependencyResolver(mainAssemblyToLoadPath);
  }
}
```

*Program.cs*

```C#
Assembly? thisAssembly = Assembly.GetEntryAssembly();

if (thisAssembly is null)
{
  WriteLine("Could not get the entry assembly.");
  return; // Exit the app.
}

OutputAssemblyInfo(thisAssembly);

WriteLine($"Creating load context for:\n  {Path.GetFileName(thisAssembly.Location)}\n");

DemoAssemblyLoadContext loadContext = new(thisAssembly.Location);

string assemblyPath = Path.Combine(
  Path.GetDirectoryName(thisAssembly.Location) ?? "",
  "DynamicLoadAndExecute.Library.dll");

WriteLine($"Loading:\n  {Path.GetFileName(assemblyPath)}\n");

Assembly dogAssembly = loadContext.LoadFromAssemblyPath(assemblyPath);

OutputAssemblyInfo(dogAssembly);

Type? dogType = dogAssembly.GetType("DynamicLoadAndExecute.Library.Dog");

if (dogType is null)
{
  WriteLine("Could not get the Dog type.");
  return;
}

MethodInfo? method = dogType.GetMethod("Speak");

if (method != null)
{
  object? dog = Activator.CreateInstance(dogType);

  for (int i = 0; i < 10; i++)
  {
    method.Invoke(dog, new object[] { "Fido" });
  }
}

WriteLine();
WriteLine("Unloading context and assemblies.");
loadContext.Unload();
```


## Working with expression trees

```C#
<ItemGroup>
  <Using Include="System.Console" Static="true" />
</ItemGroup>
```

*Program.cs*

```C#
using System.Linq.Expressions; // To use Expression and so on.

ConstantExpression one = Expression.Constant(1, typeof(int));
ConstantExpression two = Expression.Constant(2, typeof(int));
BinaryExpression add = Expression.Add(one, two);
Expression<Func<int>> expressionTree = Expression.Lambda<Func<int>>(add);
Func<int> compiledTree = expressionTree.Compile();
WriteLine($"Result: {compiledTree()}");
```


## Creating source generators

*Program.cs*

```C#
// The source-generated code.
partial class Program
{
  static partial void Message(string message)
  {
    System.Console.WriteLine($"Generator says: '{message}'"); 
  }
}
```

*GeneratingCodeApp.csproj*

```C#
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <LangVersion>12</LangVersion>
    <EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>
  </PropertyGroup>

  <ItemGroup>
    <Using Include="System.Console" Static="true" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.Analyzers" Version="3.3.4">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>
        runtime; build; native; contentfiles; analyzers; buildtransitive
      </IncludeAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.8.0" />
  </ItemGroup>

</Project>
```

*MessageSourceGenerator.cs*

```C#
// To use [Generator], ISourceGenerator, and so on.
using Microsoft.CodeAnalysis;

namespace Packt.Shared;

[Generator]
public class MessageSourceGenerator : ISourceGenerator
{
  public void Execute(GeneratorExecutionContext execContext)
  {
    IMethodSymbol mainMethod = execContext.Compilation.GetEntryPoint(execContext.CancellationToken);

    string sourceCode = $$"""
      // The source-generated code.

      partial class {{mainMethod.ContainingType.Name}}
      {
        static partial void Message(string message)
        {
          System.Console.WriteLine($"Generator says: '{message}'");
        }
      }
      """;

    string typeName = mainMethod.ContainingType.Name;

    execContext.AddSource($"{typeName}.Methods.g.cs", sourceCode);
  }

  public void Initialize(GeneratorInitializationContext initContext)
  {
    // This source generator does not need any initialization.
  }
}
```



*GeneratingCodeApp.csproj*

```C#
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
  </PropertyGroup>

  <ItemGroup>
    <Using Include="System.Console" Static="true" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\GeneratingCodeLib\GeneratingCodeLib.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
  </ItemGroup>

</Project>
```

*Program.Methods.cs*

```C#
partial class Program
{
  static partial void Message(string message);
}
```

*Program.cs*

```C#
Message("Hello from some source generator code.");
```