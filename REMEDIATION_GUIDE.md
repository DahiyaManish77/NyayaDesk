# NyayaDesk - Quick Start Remediation Guide

## Priority 1: Fix CRITICAL Issues BEFORE Production Deployment

### Issue #1: Hardcoded Database Credentials ?? CRITICAL

**Current (INSECURE):**
```xml
<!-- Web.config -->
<connectionStrings>
    <add name="NyayaDeskDBEntities" 
        connectionString="...user id=sa;password=sa@123456..." />
</connectionStrings>
```

**Step 1: Create appsettings.json** (for secure configuration)
```json
{
  "ConnectionStrings": {
    "NyayaDeskDB": "Server=YOUR_SERVER;Database=NyayaDeskDB;Integrated Security=true;"
  }
}
```

**Step 2: Update Web.config** (reference environment variable)
```xml
<connectionStrings>
    <add name="NyayaDeskDBEntities" 
        connectionString="data source=.;initial catalog=NyayaDeskDB;Integrated Security=true;" />
</connectionStrings>
```

**Step 3: Set Environment Variable** (on server)
```powershell
# Run this on production server
[System.Environment]::SetEnvironmentVariable("ConnectionString_NyayaDeskDB", "YOUR_SECURE_CONNECTION_STRING", "Machine")
```

**Alternative: Use Azure Key Vault** (Recommended for Cloud)
```csharp
// Startup code
var keyVaultUrl = "https://yourkeyvault.vault.azure.net/";
var credential = new DefaultAzureCredential();
var client = new SecretClient(new Uri(keyVaultUrl), credential);
var connectionString = client.GetSecret("NyayaDeskDB-ConnectionString").Value.Value;
```

---

### Issue #2: Session State Not Production-Ready ?? CRITICAL

**Current (INSECURE):**
```xml
<sessionState mode="InProc" ... />
```

**Fix: Use SQL Server Session State**

**Step 1: Create ASP.NET State Database**
```sql
-- Run this SQL script (provided by ASP.NET)
-- Download from: C:\Windows\Microsoft.NET\Framework\v4.0.30319\InstallSqlState.sql

sqlcmd -S YOUR_SERVER -i InstallSqlState.sql
```

**Step 2: Update Web.config**
```xml
<sessionState mode="SqlServer" 
    sqlConnectionString="server=YOUR_SERVER;database=ASPState;user id=sa;password=YOUR_PASSWORD;"
    allowCustomSqlDatabase="true"
    timeout="20" />
```

**Alternative: Use Distributed Cache (Redis)** (Better for cloud)
```powershell
# Install Redis NuGet package
Install-Package StackExchange.Redis
```

```csharp
// Startup
var connection = ConnectionMultiplexer.Connect("YOUR_REDIS_SERVER:6379");
// Configure session to use Redis
```

---

### Issue #3: HTTPS/SSL Not Enforced ?? CRITICAL

**Current (INSECURE):**
```xml
<forms ... requireSSL="false" />
<httpCookies httpOnlyCookies="true" requireSSL="false" />
```

**Fix: Enforce HTTPS**

**Step 1: Update Web.config**
```xml
<!-- Enable SSL requirement -->
<forms loginUrl="~/Account/Login"
    timeout="2880"
    slidingExpiration="true"
    cookieless="UseCookies"
    requireSSL="true"
    protection="All"
    name="NyayaDesk_Auth"
    path="/" />

<!-- Secure cookies -->
<httpCookies httpOnlyCookies="true" requireSSL="true" />
```

**Step 2: Install SSL Certificate**
```powershell
# For IIS
# 1. Request certificate from CA (or use Let's Encrypt)
# 2. Install in IIS > Server Certificates
# 3. Bind to HTTPS in Site Bindings
```

**Step 3: Redirect HTTP to HTTPS** (in Web.config)
```xml
<system.webServer>
    <rewrite>
        <rules>
            <rule name="RedirectToHTTPS" stopProcessing="true">
                <match url="(.*)" />
                <conditions>
                    <add input="{HTTPS}" pattern="^OFF$" />
                </conditions>
                <action type="Redirect" url="https://{HTTP_HOST}/{R:1}" redirectType="Permanent" />
            </rule>
        </rules>
    </rewrite>
</system.webServer>
```

---

## Priority 2: Remove Debug Endpoints

### Issue: Exposed Debug Controller

**File to Remove or Disable:**
- `NyayaDesk.Web/Controllers/LoginDebugController.cs` - **DELETE BEFORE PRODUCTION**

**If you need it for testing only:**
```csharp
#if DEBUG
[Authorize(Roles = "ADMIN")]
public class LoginDebugController : Controller
{
    // Debug code only in DEBUG build
}
#endif
```

---

## Priority 3: Add Logging Framework

### Step 1: Install NLog
```powershell
Install-Package NLog
Install-Package NLog.Web.AspNet
```

### Step 2: Add NLog.config
```xml
<?xml version="1.0" encoding="utf-8" ?>
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">

    <targets>
        <target name="file" xsi:type="File" 
            fileName="${basedir}/logs/${shortdate}.log"
            layout="${longdate} ${level:uppercase=true} ${message}" />

        <target name="console" xsi:type="Console" 
            layout="${longdate} ${level:uppercase=true} ${message}" />
    </targets>

    <rules>
        <logger name="*" minlevel="Debug" writeTo="file,console" />
    </rules>
</nlog>
```

### Step 3: Update AccountService to use logging
```csharp
using NLog;

public class AccountService : IAccountService
{
    private static Logger logger = LogManager.GetCurrentClassLogger();

    public LoginResult ValidateLogin(LoginViewModel model)
    {
        logger.Debug("Login attempt for email: {0}", model.Email);

        try {
            // ... login logic ...
            logger.Info("User {0} logged in successfully", user.UserId);
        }
        catch (Exception ex) {
            logger.Error(ex, "Login failed for email: {0}", model.Email);
            throw;
        }
    }
}
```

---

## Priority 4: Add Dependency Injection

### Step 1: Install Ninject
```powershell
Install-Package Ninject
Install-Package Ninject.Web.Mvc
```

### Step 2: Configure in App_Start
```csharp
using Ninject;

public class NinjectWebCommon 
{
    private static readonly Bootstrapper bootstrapper = new Bootstrapper();

    public static void Start() 
    {
        DynamicModuleUtility.RegisterModule(typeof(OnePerRequestHttpModule));
        DynamicModuleUtility.RegisterModule(typeof(NinjectHttpModule));
        bootstrapper.Initialize(CreateKernel);
    }

    private static IKernel CreateKernel()
    {
        var kernel = new StandardKernel();
        try
        {
            kernel.Bind<Func<IKernel>>().ToMethod(ctx => () => new StandardKernel());
            kernel.Bind<ILoggerFactory>().To<LoggerFactory>();
            kernel.Bind<IAccountService>().To<AccountService>();
            kernel.Bind<ISubscriptionService>().To<SubscriptionService>();

            RegisterServices(kernel);
            return kernel;
        }
        catch
        {
            kernel.Dispose();
            throw;
        }
    }

    private static void RegisterServices(IKernel kernel)
    {
        // Register all your services here
    }
}
```

### Step 3: Update AccountController
```csharp
public class AccountController : Controller
{
    private readonly IAccountService _accountService;

    // Inject dependency
    public AccountController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    // ... rest of controller
}
```

---

## Priority 5: Add Input Validation

### Step 1: Create validation attributes
```csharp
using System.ComponentModel.DataAnnotations;

public class StrongPasswordAttribute : ValidationAttribute
{
    public override bool IsValid(object value)
    {
        string password = value as string;
        if (string.IsNullOrEmpty(password)) return false;

        // At least 8 chars, 1 uppercase, 1 number, 1 special char
        var hasUpperCase = password.Any(char.IsUpper);
        var hasLowerCase = password.Any(char.IsLower);
        var hasDigits = password.Any(char.IsDigit);
        var hasSpecialChar = password.Any(ch => !char.IsLetterOrDigit(ch));

        return password.Length >= 8 && hasUpperCase && hasLowerCase && hasDigits && hasSpecialChar;
    }
}
```

### Step 2: Use in view models
```csharp
public class LoginViewModel
{
    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 6)]
    [StrongPassword(ErrorMessage = "Password must contain uppercase, lowercase, digits, and special characters")]
    public string Password { get; set; }
}
```

---

## Testing Checklist

After applying fixes, test the following:

- [ ] Login works over HTTPS
- [ ] Login credentials cannot be exposed in logs
- [ ] Session persists across server restarts
- [ ] Database credentials not in source code
- [ ] Debug endpoints disabled
- [ ] Errors logged to file
- [ ] Account lockout works after 5 attempts
- [ ] Password reset works
- [ ] CSRF token validation works

---

## Deployment Checklist

Before deploying to production:

- [ ] All critical issues fixed
- [ ] Debug controllers removed
- [ ] Secrets in environment variables
- [ ] HTTPS certificate installed
- [ ] Logging configured
- [ ] Exception handling in place
- [ ] Security headers configured
- [ ] Automated backups enabled
- [ ] Monitoring/alerting enabled
- [ ] Penetration testing completed

---

## Emergency Contacts

If issues occur in production:

1. **Database down:** Check SQL Server service, connection string
2. **Login failing:** Check user account status, event logs
3. **Session issues:** Verify SQL Server ASPState database
4. **SSL errors:** Check certificate expiration, IIS bindings
5. **Performance issues:** Check logs, enable query profiling

---

## Quick Reference Commands

```powershell
# View event log
Get-EventLog -LogName Application -Newest 50

# Restart IIS
iisreset

# Check SQL Server
sqlcmd -S YOUR_SERVER -Q "SELECT @@VERSION"

# View IIS logs
Get-Content "C:\inetpub\logs\LogFiles\W3SVC1\*.log"

# Clear application cache
Remove-Item "C:\Users\Admin\AppData\Local\Temp\*"
```

---

**Last Updated:** August 2, 2026  
**Next Review:** After critical fixes applied
