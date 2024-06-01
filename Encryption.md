### Encryption

```C#
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>

  <ItemGroup>
    <Using Include="System.Console" Static="true" />
  </ItemGroup>
  
</Project>
```

*Protector.cs*

```C#
using System.Diagnostics; // To use Stopwatch.
using System.Security.Cryptography; // To use Aes and so on.
using System.Text; // To use Encoding.

// To use GenericIdentity, GenericPrincipal.
using System.Security.Principal;

using static System.Convert; // To use ToBase64String and so on.

namespace Packt.Shared;

public static class Protector
{
  // Salt size must be at least 8 bytes, we will use 16 bytes.
  private static readonly byte[] salt =
    Encoding.Unicode.GetBytes("7BANANAS");

  // Default iterations for Rfc2898DeriveBytes is 1000.
  // Iterations should be high enough to take at least 100ms to 
  // generate a Key and IV on the target machine. 150,000 iterations
  // takes 139ms on my 11th Gen Intel Core i7-1165G7 @ 2.80GHz.
  private static readonly int iterations = 150_000;

  public static string Encrypt(
    string plainText, string password)
  {
    byte[] encryptedBytes;
    byte[] plainBytes = Encoding.Unicode.GetBytes(plainText);

    using (Aes aes = Aes.Create()) // abstract class factory method
    {
      // Record how long it takes to generate the Key and IV.
      Stopwatch timer = Stopwatch.StartNew();

      using (Rfc2898DeriveBytes pbkdf2 = new(
        password, salt, iterations, HashAlgorithmName.SHA256))
      {
        WriteLine($"PBKDF2 algorithm: {pbkdf2.HashAlgorithm
          }, Iteration count: {pbkdf2.IterationCount:N0}");

        aes.Key = pbkdf2.GetBytes(32); // Set a 256-bit key.
        aes.IV = pbkdf2.GetBytes(16); // Set a 128-bit IV.
      }

      timer.Stop();

      WriteLine($"{timer.ElapsedMilliseconds:N0} milliseconds to generate Key and IV.");

      if (timer.ElapsedMilliseconds < 100)
      {
        ConsoleColor previousColor = ForegroundColor;
        ForegroundColor = ConsoleColor.Red;
        WriteLine("WARNING: The elapsed time to generate the Key and IV " +
                  "may be too short to provide a secure encryption key.");
        ForegroundColor = previousColor;
      }

      WriteLine($"Encryption algorithm: {nameof(Aes)}-{aes.KeySize
        }, {aes.Mode} mode with {aes.Padding} padding.");

      using (MemoryStream ms = new())
      {
        using (ICryptoTransform transformer = aes.CreateEncryptor())
        {
          using (CryptoStream cs = new(
            ms, transformer, CryptoStreamMode.Write))
          {
            cs.Write(plainBytes, 0, plainBytes.Length);

            if (!cs.HasFlushedFinalBlock)
            {
              cs.FlushFinalBlock();
            }
          }
        }
        encryptedBytes = ms.ToArray();
      }
    }

    return ToBase64String(encryptedBytes);
  }

  public static string Decrypt(
    string cipherText, string password)
  {
    byte[] plainBytes;
    byte[] cryptoBytes = FromBase64String(cipherText);

    using (Aes aes = Aes.Create())
    {
      using (Rfc2898DeriveBytes pbkdf2 = new(
        password, salt, iterations, HashAlgorithmName.SHA256))
      {
        aes.Key = pbkdf2.GetBytes(32);
        aes.IV = pbkdf2.GetBytes(16);
      }

      using (MemoryStream ms = new())
      {
        using (ICryptoTransform transformer = aes.CreateDecryptor())
        {
          using (CryptoStream cs = new(
            ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
          {
            cs.Write(cryptoBytes, 0, cryptoBytes.Length);

            if (!cs.HasFlushedFinalBlock)
            {
              cs.FlushFinalBlock();
            }
          }
        }
        plainBytes = ms.ToArray();
      }
    }

    return Encoding.Unicode.GetString(plainBytes);
  }

  private static Dictionary<string, User> Users = new();

  public static User Register(string username, 
    string password, string[]? roles = null)
  {
    // Generate a random salt.
    RandomNumberGenerator rng = RandomNumberGenerator.Create();
    byte[] saltBytes = new byte[16];
    rng.GetBytes(saltBytes);
    string saltText = ToBase64String(saltBytes);

    // Generate the salted and hashed password.
    string saltedhashedPassword = SaltAndHashPassword(password, saltText);

    User user = new(username, saltText,
      saltedhashedPassword, roles);

    Users.Add(user.Name, user);

    return user;
  }

  // Check a user's password that is stored in the Users dictionary.
  public static bool CheckPassword(string username, string password)
  {
    if (!Users.ContainsKey(username))
    {
      return false;
    }

    User u = Users[username];

    return CheckPassword(password,
      u.Salt, u.SaltedHashedPassword);
  }

  // Check a password using salt and hashed password.
  public static bool CheckPassword(string password,
    string salt, string hashedPassword)
  {
    // re-generate the salted and hashed password 
    string saltedHashedPassword = SaltAndHashPassword(
      password, salt);

    return (saltedHashedPassword == hashedPassword);
  }

  private static string SaltAndHashPassword(string password, string salt)
  {
    using (SHA256 sha = SHA256.Create())
    {
      string saltedPassword = password + salt;
      return ToBase64String(sha.ComputeHash(
        Encoding.Unicode.GetBytes(saltedPassword)));
    }
  }

  public static string? PublicKey;

  public static string GenerateSignature(string data)
  {
    byte[] dataBytes = Encoding.Unicode.GetBytes(data);
    SHA256 sha = SHA256.Create();
    byte[] hashedData = sha.ComputeHash(dataBytes);
    RSA rsa = RSA.Create();

    PublicKey = rsa.ToXmlString(false); // exclude private key

    return ToBase64String(rsa.SignHash(hashedData,
      HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
  }

  public static bool ValidateSignature(string data, string signature)
  {
    if (PublicKey is null) return false;

    byte[] dataBytes = Encoding.Unicode.GetBytes(data);
    SHA256 sha = SHA256.Create();

    byte[] hashedData = sha.ComputeHash(dataBytes);
    byte[] signatureBytes = FromBase64String(signature);

    RSA rsa = RSA.Create();
    rsa.FromXmlString(PublicKey);

    return rsa.VerifyHash(hashedData, signatureBytes,
      HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
  }

  public static byte[] GetRandomKeyOrIV(int size)
  {
    RandomNumberGenerator r = RandomNumberGenerator.Create();
    byte[] data = new byte[size];

    // The array is filled with cryptographically random bytes.
    r.GetBytes(data);
    return data;
  }

  public static void LogIn(string username, string password)
  {
    if (CheckPassword(username, password))
    {
      GenericIdentity gi = new(
        name: username, type: "PacktAuth");

      GenericPrincipal gp = new(
        identity: gi, roles: Users[username].Roles);

      // Set the principal on the current thread so that
      // it will be used for authorization by default.
      Thread.CurrentPrincipal = gp;
    }
  }
}
```

*User.cs*

```C#
namespace Packt.Shared;

public record class User(string Name, string Salt, string SaltedHashedPassword, string[]? Roles);
```

=======================================
*EncryptionApp.csproj*

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
    <Using Include="System.Console" Static="true" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\CryptographyLib\CryptographyLib.csproj" />
  </ItemGroup>

</Project>
```

*Program.cs*

```C#
using System.Security.Cryptography; // To use CryptographicException.
using Packt.Shared; // To use Protector.

Write("Enter a message that you want to encrypt: ");
string? message = ReadLine();

Write("Enter a password: ");
string? password = ReadLine();

if ((password is null) || (message is null))
{
  WriteLine("Message or password cannot be null.");
  return; // Exit the app.
}

string cipherText = Protector.Encrypt(message, password);

WriteLine($"Encrypted text: {cipherText}");

Write("Enter the password: ");
string? password2Decrypt = ReadLine();

if (password2Decrypt is null)
{
  WriteLine("Password to decrypt cannot be null.");
  return;
}

try
{
  string clearText = Protector.Decrypt(cipherText, password2Decrypt);
  WriteLine($"Decrypted text: {clearText}");
}
catch (CryptographicException)
{
    WriteLine("You entered the wrong password!");
}
catch (Exception ex)
{
    WriteLine($"Non-cryptographic exception: {ex.GetType().Name}, {ex.Message}");
}
```

=======================================


### Hashing

*Program.cs*

```C#
using Packt.Shared; // To use Protector.

WriteLine("Registering Alice with Pa$$w0rd:");
User alice = Protector.Register("Alice", "Pa$$w0rd");

WriteLine($"  Name: {alice.Name}");
WriteLine($"  Salt: {alice.Salt}");
WriteLine($"  Password (salted and hashed): {alice.SaltedHashedPassword}");
WriteLine();

Write("Enter a new user to register: ");
string? username = ReadLine();
if (string.IsNullOrEmpty(username)) username = "Bob";

Write($"Enter a password for {username}: ");
string? password = ReadLine();
if (string.IsNullOrEmpty(password)) password = "Pa$$w0rd";

WriteLine("Registering a new user:");
User newUser = Protector.Register(username, password);
WriteLine($"  Name: {newUser.Name}");
WriteLine($"  Salt: {newUser.Salt}");
WriteLine($"  Password (salted and hashed): {newUser.SaltedHashedPassword}");
WriteLine();

bool correctPassword = false;

while (!correctPassword)
{
  Write("Enter a username to log in: ");
  string? loginUsername = ReadLine();
  if (string.IsNullOrEmpty(loginUsername))
  {
    WriteLine("Login username cannot be empty.");
    Write("Press Ctrl+C to end or press ENTER to retry.");
    ReadLine();
    continue; // Return to the while statement.
  }

  Write("Enter a password to log in: ");
  string? loginPassword = ReadLine();
  if (string.IsNullOrEmpty(loginPassword))
  {
    WriteLine("Login password cannot be empty.");
    Write("Press Ctrl+C to end or press ENTER to retry.");
    ReadLine();
    continue;
  }

  correctPassword = Protector.CheckPassword(loginUsername, loginPassword);

  if (correctPassword)
  {
    WriteLine($"Correct! {loginUsername} has been logged in.");
  }
  else
  {
    WriteLine("Invalid username or password. Try again.");
  }
}
```


### Signing with SHA256 and RSA

*Program.cs*

```C#
using Packt.Shared; // To use Protector.

Write("Enter some text to sign: ");
string? data = ReadLine();

if (string.IsNullOrEmpty(data))
{
    WriteLine("You must enter some text.");
    return; // Exit the app.
}

string signature = Protector.GenerateSignature(data);

WriteLine($"Signature: {signature}");
WriteLine("Public key used to check signature:");
WriteLine(Protector.PublicKey);

if (Protector.ValidateSignature(data, signature))
{
    WriteLine("Correct! Signature is valid. Data has not been manipulated.");
}
else
{
    WriteLine("Invalid signature or the data has been manipulated.");
}

// Simulate manipulated data by replacing the first 
// character with an X (or if already an X then Y).
char newFirstChar = 'X';

if (data[0] == newFirstChar)
{
    newFirstChar = 'Y';
}

string manipulatedData = $"{newFirstChar}{data.Substring(1)}";

if (Protector.ValidateSignature(manipulatedData, signature))
{
    WriteLine("Correct! Signature is valid. Data has not been manipulated. ");
}
else
{
    WriteLine($"Invalid signature or manipulated data: {manipulatedData}");
}
```


### Generating random numbers

*Program.cs*

```C#
using Packt.Shared; // To use Protector.

Write("How big do you want the key (in bytes): ");
string? size = ReadLine();

if (string.IsNullOrEmpty(size))
{
    WriteLine("You must enter a size for the key.");
    return; // Exit the app.
}

byte[] key = Protector.GetRandomKeyOrIV(int.Parse(size));

WriteLine($"Key as byte array:");
for (int b = 0; b < key.Length; b++)
{
    Write($"{key[b]:x2} ");
    if (((b + 1) % 16) == 0) WriteLine();
}

WriteLine();
```


### Authentication and authorization mechanisms

*Program.cs*

```C#
using Packt.Shared; // To use Protector.
using System.Security.Principal; // To use IPrincipal.
using System.Security.Claims; // To use ClaimsPrincipal, Claim.
using System.Security; // To use SecurityException.

WriteLine("Registering Alice, Bob, and Eve with passwords Pa$$w0rd.");

Protector.Register("Alice", "Pa$$w0rd", roles: new[] { "Admins" });

Protector.Register("Bob", "Pa$$w0rd",
  roles: new[] { "Sales", "TeamLeads" });

// Register Eve who is not a member of any roles.
Protector.Register("Eve", "Pa$$w0rd");

WriteLine();

// Prompt the user to enter a username and password to login
// as one of these three users.

Write("Enter your username: ");
string? username = ReadLine()!;

Write("Enter your password: ");
string? password = ReadLine()!;

Protector.LogIn(username, password);

if (Thread.CurrentPrincipal == null)
{
  WriteLine("Log in failed.");
  return; // Exit the app.
}

IPrincipal p = Thread.CurrentPrincipal;

WriteLine($"IsAuthenticated: {p.Identity?.IsAuthenticated}");
WriteLine($"AuthenticationType: {p.Identity?.AuthenticationType}");
WriteLine($"Name: {p.Identity?.Name}");
WriteLine($"IsInRole(\"Admins\"): {p.IsInRole("Admins")}");
WriteLine($"IsInRole(\"Sales\"): {p.IsInRole("Sales")}");

if (p is ClaimsPrincipal principal)
{
    WriteLine($"{principal.Identity?.Name} has the following claims:");

    foreach (Claim claim in principal.Claims)
    {
        WriteLine($"{claim.Type}: {claim.Value}");
    }
}

try
{
    SecureFeature();
}
catch (Exception ex)
{
    WriteLine($"{ex.GetType()}: {ex.Message}");
}

static void SecureFeature()
{
    if (Thread.CurrentPrincipal is null)
    {
        throw new SecurityException("A user must be logged in to access this feature.");
    }

    if (!Thread.CurrentPrincipal.IsInRole("Admins"))
    {
        throw new SecurityException("User must be a member of Admins to access this feature.");
    }
    
    WriteLine("You have access to this secure feature.");
}
```

============================
### Encrypt Data

*EncryptData.csproj*

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
    <Using Include="System.Console" Static="true" />
    <Using Include="System.IO.Path" Static="true" />
    <Using Include="System.Environment" Static="true" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\CryptographyLib\CryptographyLib.csproj" />
  </ItemGroup>

</Project>
```

*Customer.cs*

```C#
namespace Packt.Shared;

public record Customer(string Name, string CreditCard, string Password);
```

*Program.cs*

```C#
using Packt.Shared; // To use Protector.
using System.Xml; // To use XmlWriter.

WriteLine("You must enter a password to encrypt the sensitive data in the document.");
WriteLine("You must enter the same passord to decrypt the document later.");
Write("Password: ");
string? password = ReadLine();

if (string.IsNullOrEmpty(password))
{
  WriteLine("A password is required to continue.");
  return; // Exit the app.
}

// Define two example customers and note they have the same password.

Customer c1 = new Customer("Bob Smith", "1234-5678-9012-3456", "Pa$$w0rd");
Customer c2 = new Customer("Leslie Knope", "8002-5265-3400-2511", "Pa$$w0rd");

List<Customer> customers = [c1, c2];

// Define an XML file to write to.
string xmlFile = Combine(CurrentDirectory, "..", "protected-customers.xml");

XmlWriter xmlWriter = XmlWriter.Create(xmlFile, new XmlWriterSettings { Indent = true });

xmlWriter.WriteStartDocument();

xmlWriter.WriteStartElement("customers");

foreach (var customer in customers)
{
    xmlWriter.WriteStartElement("customer");
    xmlWriter.WriteElementString("name", customer.Name);
    
    // To protect the credit card number we must encrypt it
    // using the app-level password.
    xmlWriter.WriteElementString("creditcard", Protector.Encrypt(customer.CreditCard, password));

    // To protect the password we must salt and hash it
    // and we must store the random salt used.
    User user = Protector.Register(customer.Name, customer.Password);
    xmlWriter.WriteElementString("password", user.SaltedHashedPassword);
    xmlWriter.WriteElementString("salt", user.Salt);

    xmlWriter.WriteEndElement();
}
xmlWriter.WriteEndElement();
xmlWriter.WriteEndDocument();
xmlWriter.Close();

WriteLine();
WriteLine("Contents of the protected file:");
WriteLine();
WriteLine(File.ReadAllText(xmlFile));
```


### Decrypt Data

*Customer.cs*

```C#
namespace Packt.Shared;

public record Customer(string Name, string CreditCard, string Password);
```

*Program.cs*

```C#
using Packt.Shared; // To use Protector.
using System.Xml; // To use XmlReader.
using System.Security.Cryptography; // To use CryptographicException.

WriteLine("You must enter the correct password to decrypt the document.");
Write("Password: ");
string? password = ReadLine();

if (string.IsNullOrEmpty(password))
{
  WriteLine("A password is required to continue.");
  return; // Exit the app.
}

List<Customer> customers = new();

// Define an XML file to read from.
string xmlFile = Combine(CurrentDirectory,
  "..", "protected-customers.xml");

if (!File.Exists(xmlFile))
{
  WriteLine($"{xmlFile} does not exist!");
  return;
}

XmlReader xmlReader = XmlReader.Create(xmlFile,
  new XmlReaderSettings { IgnoreWhitespace = true });

while (xmlReader.Read())
{
  if (xmlReader.NodeType == XmlNodeType.Element 
    && xmlReader.Name == "customer")
  {
    xmlReader.Read(); // Move to <name> element.

    string name = xmlReader.ReadElementContentAsString();
    string creditcardEncrypted = xmlReader.ReadElementContentAsString();
    string? creditcard = null;
    string errorMessage = "No credit card";

    try
    {
      creditcard = Protector.Decrypt(creditcardEncrypted, password);
    }
    catch (CryptographicException)
    {
      errorMessage = $"Failed to decrypt {name}'s credit card.";
    }

    string passwordHashed = xmlReader.ReadElementContentAsString();
    string salt = xmlReader.ReadElementContentAsString();

    customers.Add(new Customer(name,
      creditcard ?? errorMessage, passwordHashed));
  }
}

xmlReader.Close();

WriteLine();
int number = 0;
WriteLine($"    {"Name",-20} {"Credit Card",-20}");

foreach (var customer in customers)
{
  WriteLine($"[{number}] {customer.Name,-20} {
    customer.CreditCard,-20}");

  number++;
}
WriteLine();

Write("Press the number of a customer to log in as: ");

string? customerName = null;

try
{
  number = int.Parse(ReadKey().KeyChar.ToString());
  customerName = customers[number].Name;
}
catch
{
  WriteLine();
  WriteLine("Not a valid customer selection.");
  return;
}

WriteLine();
Write($"Enter {customerName}'s password: ");

string? attemptPassword = ReadLine();

if (string.IsNullOrEmpty(attemptPassword))
{
  WriteLine("A password is required to continue.");
  return; // Exit the app.
}

if (Protector.CheckPassword(
  username: customers[number].Name,
  password: attemptPassword))
{
  WriteLine("Correct!");
}
else
{
  WriteLine("Wrong!");
}
```