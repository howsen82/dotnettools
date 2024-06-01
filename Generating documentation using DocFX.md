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


**Adding custom documentation content**

```
- name: Introduction
  href: introduction.md
- name: Getting Started
  href: getting-started.md
```


**Documenting a Minimal APIs service using OpenAPI**

```
<GenerateDocumentationFile>true</GenerateDocumentationFile>
```

```
builder.Services.AddSwaggerGen(c =>
{
  c.SwaggerDoc("v1", new OpenApiInfo { Title = "Northwind API", Version = "v1" });
    
  // Set the comments path for the Swagger JSON and UI.
  string xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
  string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
  c.IncludeXmlComments(xmlPath);
});
```

```
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <InvariantGlobalization>false</InvariantGlobalization>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <NoWarn>CS9057</NoWarn>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="8.0.1" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
  </ItemGroup>
</Project>
```


**Documenting visually with Mermaid diagrams**

```mermaid

```

```
npm install -g @mermaid-js/mermaid-cli
```

*Flowcharts using Mermaid*

```mermaid
flowchart
    A(Start) --> B{Budget?}
    B -->|Under $30k| C[Used EV]
    B -->|Between $30k-$50k| D[New Mid-Range EV]
    B -->|Over $50k| E[New Luxury EV]
    
    C --> F{Range Requirement?}
    D --> F
    E --> F
    
    F -->|Under 200 miles| G[City Car]
    F -->|200-300 miles| H[All-Purpose Vehicle]
    F -->|Over 300 miles| I[Long Range Vehicle]
    
    G --> J{Charging at Home?}
    H --> J
    I --> J
    
    J -->|Yes| K[Proceed with Purchase]
    J -->|No| L[Consider Charging Options]
    
    L --> M{Public Charging Available?}
    M -->|Yes| N[Proceed with Purchase]
    M -->|No| O[Reevaluate Requirements]   
```

*Class diagrams using Mermaid*

```mermaid
classDiagram
    class Stream {
        <<abstract>>
        +Read(byte[] buffer, int offset, int count) int
        +Write(byte[] buffer, int offset, int count) void
        +Seek(long offset, SeekOrigin origin) long
        +Flush() void
        +Close() void
        -Length long
        -Position long
    }
    class MemoryStream {
        +MemoryStream()
        +MemoryStream(byte[] buffer)
        -Capacity int
    }
    class FileStream {
        +FileStream(string path, FileMode mode)
        +FileStream(string path, FileMode mode, FileAccess access)
        +FileStream(string path, FileMode mode, FileAccess access, FileShare share)
        -Name string
        -SafeFileHandle SafeFileHandle
    }
    Stream <|-- MemoryStream
    Stream <|-- FileStream
```


**Converting Mermaid to SVG**

```
mmdc -i mermaid-examples.md -o output.md
```