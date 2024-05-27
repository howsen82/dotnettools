## Visual Studio Tools

List all extensions installed

```
code --list-extensions
code --list-extensions | % { "code --install-extension $_" }
```

**Install extension**

```
code --install-extension ms-dotnettools.csdevkit
```

```
# Install JetBrain Rider
sudo snap install rider --classic
```

**Global Using**

```
<ItemGroup>
  <Using Include="System.Console" Static="true" />
  <Using Include="System.IO.Path" Static="true" />
</ItemGroup>
```

Install Template

```
dotnet new install Microsoft.TemplateEngine.Authoring.Templates
```

Package NuGet

```
dotnet pack

# Install template
dotnet new install .\bin\Release\ConsolePlusTemplate.1.0.0.nupkg
```