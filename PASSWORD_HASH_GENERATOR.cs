// ============================================================================
// Quick Password Hash Generator for NyayaDesk
// ============================================================================
// Run this C# code in Visual Studio Immediate Window or as a console app
// to generate password hashes for user accounts
// ============================================================================

using System;
using System.Security.Cryptography;
using System.Text;

// Step 1: Run this to generate a password hash
var password = "Welcome@123";  // CHANGE THIS TO YOUR DESIRED PASSWORD
var salt = "77AAE9B9-13DE-43B7-A2E3-1FBA0C390BD6";  // Use existing salt or generate new

// Generate hash
using (SHA256 sha256 = SHA256.Create())
{
    byte[] bytes = Encoding.UTF8.GetBytes(password + salt);
    byte[] hash = sha256.ComputeHash(bytes);
    StringBuilder builder = new StringBuilder();

    for (int i = 0; i < hash.Length; i++)
    {
        builder.Append(hash[i].ToString("X2"));
    }

    var generatedHash = builder.ToString();

    Console.WriteLine("============================================");
    Console.WriteLine("PASSWORD HASH GENERATOR");
    Console.WriteLine("============================================");
    Console.WriteLine($"Password: {password}");
    Console.WriteLine($"Salt: {salt}");
    Console.WriteLine($"Hash: {generatedHash}");
    Console.WriteLine("============================================");
    Console.WriteLine("");
    Console.WriteLine("SQL UPDATE STATEMENT:");
    Console.WriteLine("============================================");
    Console.WriteLine($@"UPDATE [User] 
SET PasswordHash = '{generatedHash}', 
    PasswordSalt = '{salt}',
    IsActive = 1,
    IsLocked = 0,
    FailedLoginAttempts = 0,
    LockoutEndDate = NULL
WHERE Email = 'admin@nyayadesk.in'");
    Console.WriteLine("============================================");
}

// Alternative: Generate new salt if needed
var newSalt = Guid.NewGuid().ToString().ToUpper();
Console.WriteLine($"\nNew Salt (if needed): {newSalt}");

// ============================================================================
// INSTRUCTIONS:
// ============================================================================
// 1. Open Visual Studio
// 2. Go to Debug > Windows > Immediate Window
// 3. Copy and paste the code above
// 4. Change the password to what you want
// 5. Copy the generated Hash value
// 6. Run the SQL UPDATE statement in your database
// 7. Try logging in with the password you set
// ============================================================================
