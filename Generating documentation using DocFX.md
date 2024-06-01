## Generating documentation using DocFX

DocFX is installed as a dotnet CLI global tool. Let's do that now and configure the class library project to generate a documentation XML file from the source code

```
dotnet tool install -g docfx
# Or
dotnet tool update -g docfx

dotnet tool list -g
```

```
<PropertyGroup>
  <TargetFramework>net8.0</TargetFramework>
  <ImplicitUsings>enable</ImplicitUsings>
  <Nullable>enable</Nullable>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <DocumentationFile>
    bin\$(Configuration)\$(TargetFramework)\$(AssemblyName).xml
  </DocumentationFile>
</PropertyGroup>
```


**Creating a DocFX project**

```
docfx init
```

```
Name (mysite): PacktLibrary
Generate .NET API documentation? [y/n] (y): y
.NET projects location (src): PacktLibrary
Markdown docs location (docs): docs
Enable site search? [y/n] (y): y
Enable PDF? [y/n] (y): y
Is this OK? [y/n] (y): y
```

```
docfx docfx.json --serve
```