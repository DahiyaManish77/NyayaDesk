# Fix Login Issue - Step-by-Step Guide

## The Problem

The superadmin user account (`admin@nyayadesk.in`) has a password hash, but:
- ? We don't know what the original password was
- ? Login fails because password verification can't match

**Solution:** Reset the password with a new one you know.

---

## Quick Fix (2 minutes)

### Option 1: Use the Password Reset Tool (Easiest)

**Step 1:** Open Visual Studio  
**Step 2:** Copy and paste this code in **Debug > Immediate Window**:

```csharp
// Generate password hash
var password = "Welcome@123";  // <-- CHANGE THIS PASSWORD
var salt = "77AAE9B9-13DE-43B7-A2E3-1FBA0C390BD6";

using (var sha256 = System.Security.Cryptography.SHA256.Create())
{
    var bytes = System.Text.Encoding.UTF8.GetBytes(password + salt);
    var hash = sha256.ComputeHash(bytes);
    var result = string.Concat(System.Array.ConvertAll(hash, x => x.ToString("X2")));
    System.Diagnostics.Debug.WriteLine($"Hash: {result}");
    System.Diagnostics.Debug.WriteLine($"SQL: UPDATE [User] SET PasswordHash = '{result}', PasswordSalt = '{salt}', IsActive = 1, IsLocked = 0, FailedLoginAttempts = 0, LockoutEndDate = NULL WHERE Email = 'admin@nyayadesk.in'");
}
```

**Step 3:** Copy the generated **Hash** value from the Output window  
**Step 4:** Run this SQL in your database:

```sql
UPDATE [User] 
SET PasswordHash = '[PASTE HASH HERE]', 
    PasswordSalt = '77AAE9B9-13DE-43B7-A2E3-1FBA0C390BD6',
    IsActive = 1,
    IsLocked = 0,
    FailedLoginAttempts = 0,
    LockoutEndDate = NULL
WHERE Email = 'admin@nyayadesk.in'
```

**Step 5:** Login with:
- Email: `admin@nyayadesk.in`
- Password: `Welcome@123` (or whatever you set in step 2)

---

### Option 2: Use REST API Endpoint

1. **Make sure app is running**

2. **Call the password reset endpoint:**

```bash
POST http://localhost:YOUR_PORT/PasswordReset/GenerateHash
Body: password=Welcome@123

Response:
{
  "success": true,
  "password": "Welcome@123",
  "salt": "77AAE9B9-13DE-43B7-A2E3-1FBA0C390BD6",
  "hash": "GENERATED_HASH_HERE",
  "sqlUpdate": "UPDATE [User] SET ... WHERE Email = 'admin@nyayadesk.in'"
}
```

3. **Copy the `sqlUpdate` SQL statement and run it in your database**

4. **Login with password: `Welcome@123`**

---

### Option 3: Use SQL Script Directly

1. **Open `NyayaDesk.Web/App_Data/PasswordReset_SuperAdmin.sql`**

2. **Follow the steps in the SQL file:**
   - Step 1: Check current user status
   - Step 2: Generate hash (use Option 1 or 2)
   - Step 3: Update user with the hash
   - Step 4: Verify the update

---

## Verify the Fix

After updating, check that the user is properly configured:

```sql
SELECT 
    UserId,
    Email,
    UserName,
    IsActive,
    IsLocked,
    FailedLoginAttempts,
    LastLoginDate
FROM [User]
WHERE Email = 'admin@nyayadesk.in'
```

Should show:
- `IsActive` = 1
- `IsLocked` = 0
- `FailedLoginAttempts` = 0

---

## Test Login

1. Open the application
2. Go to login page
3. Enter:
   - **Email:** `admin@nyayadesk.in`
   - **Password:** `Welcome@123` (or whatever you set)
4. Click **Login**

? Should now work!

---

## What If It Still Doesn't Work?

### Check 1: User Roles
```sql
SELECT ur.*, r.RoleName, r.RoleCode
FROM UserRole ur
JOIN [Role] r ON ur.RoleId = r.RoleId
WHERE ur.UserId = 1  -- superadmin user
```

Must have at least one active role.

### Check 2: Account Status
```sql
SELECT * FROM [User] WHERE Email = 'admin@nyayadesk.in'
```

Verify:
- ? `IsActive = 1`
- ? `IsDeleted = 0`
- ? `IsLocked = 0`

### Check 3: Password Hash
```sql
-- Test password verification
DECLARE @password NVARCHAR(MAX) = 'Welcome@123'
DECLARE @salt NVARCHAR(MAX) = '77AAE9B9-13DE-43B7-A2E3-1FBA0C390BD6'

-- Check if hash matches in database
SELECT PasswordHash, PasswordSalt FROM [User] WHERE Email = 'admin@nyayadesk.in'
-- Regenerate hash and compare
```

### Check 4: Application Logs
Look for error messages in:
- Event Viewer > Application
- Application logs directory
- Visual Studio Output window

---

## New Files Created

| File | Purpose |
|------|---------|
| `PasswordHashHelper.cs` | Generates password hashes |
| `PasswordResetController.cs` | REST endpoints for password reset |
| `PasswordReset_SuperAdmin.sql` | SQL script for manual reset |
| `PASSWORD_HASH_GENERATOR.cs` | Console helper for hash generation |

---

## Important Notes

?? **Before Production:**
- Remove `PasswordResetController.cs` or protect it with authentication
- Change the default passwords used in examples
- Use proper secret management (Azure Key Vault, AWS Secrets)
- Never expose password reset endpoints to public

? **Password Requirements:**
- Minimum 6 characters
- Can contain any characters
- Case-sensitive
- Should use strong passwords in production

---

## Password Hashing Algorithm

The system uses:
- **Algorithm:** SHA256
- **Salt:** Unique per user (GUID format)
- **Format:** SHA256(password + salt)
- **Output:** Hexadecimal string (64 characters)

Example:
```
Password: Welcome@123
Salt: 77AAE9B9-13DE-43B7-A2E3-1FBA0C390BD6
Hash: [64-character hex string]
```

---

## Quick Reference

### Generate New Salt
```csharp
var newSalt = Guid.NewGuid().ToString().ToUpper();
// Output: 77AAE9B9-13DE-43B7-A2E3-1FBA0C390BD6
```

### Test Password Verification
Use the `LoginDiagnosticsHelper` endpoint:
```
POST /LoginDebug/DiagnoseLogin
Body: { "email": "admin@nyayadesk.in", "password": "Welcome@123" }
```

Response shows:
- If password is correct
- Password hash debug info
- Account status

---

## Additional Help

For more details, see:
- `LOGIN_TROUBLESHOOTING.md` - Debug login issues
- `REMEDIATION_GUIDE.md` - Security improvements
- `AccountService.cs` - Password hashing implementation

---

**Status:** ? Ready to fix  
**Time to fix:** 2-5 minutes  
**Difficulty:** Easy
