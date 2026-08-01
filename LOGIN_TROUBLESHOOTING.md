# NyayaDesk Login Troubleshooting Guide

## Why You Can't Login with Correct Credentials

This guide helps diagnose and fix login issues in the NyayaDesk application.

## Common Issues and Solutions

### 1. ? Account Locked (After 5 Failed Attempts)
**Symptom:** Error message "Your account is locked"

**Cause:** Account locks for 15 minutes after 5 failed login attempts

**Solution:**
- Wait 15 minutes and try again, OR
- Database Admin: Execute this SQL to unlock immediately:
```sql
UPDATE [User] SET IsLocked = 0, FailedLoginAttempts = 0, LockoutEndDate = NULL 
WHERE Email = 'your-email@example.com'
```

**Using Debug Controller (for testing only):**
```
POST /LoginDebug/UnlockUser
Body: { userId: 123 }
```

---

### 2. ? Account is Inactive
**Symptom:** Error message "Your account is inactive"

**Cause:** User account's `IsActive` flag is set to false

**Solution:**
Database Admin: Activate the account:
```sql
UPDATE [User] SET IsActive = 1 
WHERE Email = 'your-email@example.com'
```

**Using Debug Controller:**
```
POST /LoginDebug/ActivateUser
Body: { userId: 123 }
```

---

### 3. ? Account Deleted
**Symptom:** Error message "Your account does not exist"

**Cause:** User account's `IsDeleted` flag is set to true

**Solution:**
Database Admin: Restore the account:
```sql
UPDATE [User] SET IsDeleted = 0 
WHERE Email = 'your-email@example.com'
```

---

### 4. ? Email Not Found
**Symptom:** Error message "Invalid email or password"

**Cause:** 
- Email doesn't exist in database, OR
- Typo in email address, OR
- Email case mismatch (though system uses case-insensitive search)

**Solution:**
- Double-check email spelling
- Verify account was created during registration
- Database Admin: Check if user exists:
```sql
SELECT UserId, Email, IsActive, IsDeleted, IsLocked 
FROM [User] 
WHERE Email LIKE '%your-email@%'
```

**Using Debug Controller to diagnose:**
```
POST /LoginDebug/DiagnoseLogin
Body: { email: "your-email@example.com", password: "your-password" }
```
This will return all available emails in the system (for testing only).

---

### 5. ? Password Incorrect
**Symptom:** Error message "Invalid email or password"

**Cause:** 
- Password is wrong
- Password was reset and user doesn't know new password
- Password stored with different hashing algorithm

**Solution:**
- Verify caps-lock is off
- Check if password contains special characters
- Database Admin: Reset password using registration system

**Debug Info:**
```
POST /LoginDebug/DiagnoseLogin
Body: { email: "your-email@example.com", password: "your-password" }
```
This will show password hash debug info (only for testing).

---

## How to Use the Diagnostic Tools

### Step 1: Diagnose the Issue
**Endpoint:** `POST /LoginDebug/DiagnoseLogin`

**Request Body:**
```json
{
  "email": "user@example.com",
  "password": "userPassword123"
}
```

**Response Example:**
```json
{
  "userFound": true,
  "userId": 5,
  "userEmail": "user@example.com",
  "userName": "john_doe",
  "passwordIsCorrect": false,
  "failedLoginAttempts": 3,
  "status": null,
  "issues": [
    "PASSWORD IS INCORRECT"
  ],
  "availableEmails": [],
  "debugInfo": {
    "enteredPassword": "userPassword123",
    "storedSalt": "ABC123DEF456...",
    "storedHash": "HASH1...",
    "computedHash": "HASH2..."
  }
}
```

### Step 2: Fix Based on Diagnosis

- **If "Account is LOCKED"** ? Use `UnlockUser` endpoint
- **If "PASSWORD IS INCORRECT"** ? User needs to reset password or verify credentials
- **If "Account is INACTIVE"** ? Use `ActivateUser` endpoint or `UPDATE` in SQL
- **If "No user found"** ? Check email spelling, create new account via registration

### Step 3: Test Login Again
Try logging in with the same credentials.

---

## Database Queries for Admin

### View User Account Status
```sql
SELECT 
    UserId,
    Email,
    UserName,
    IsActive,
    IsDeleted,
    IsLocked,
    FailedLoginAttempts,
    LockoutEndDate,
    LastLoginDate
FROM [User]
WHERE Email = 'your-email@example.com'
```

### Unlock All Locked Accounts
```sql
UPDATE [User] 
SET IsLocked = 0, FailedLoginAttempts = 0, LockoutEndDate = NULL 
WHERE IsLocked = 1
```

### Activate All Inactive Accounts
```sql
UPDATE [User] 
SET IsActive = 1 
WHERE IsActive = 0
```

### Check Last Login Activity
```sql
SELECT TOP 20
    UserId,
    Email,
    UserName,
    LastLoginDate,
    LastLoginOn,
    FailedLoginAttempts
FROM [User]
ORDER BY LastLoginDate DESC
```

---

## Important Security Notes

?? **IMPORTANT:** 
- The `LoginDebugController` is included for development/testing only
- **REMOVE THIS CONTROLLER BEFORE DEPLOYING TO PRODUCTION**
- Do not expose password debugging information in production
- Use proper authentication and authorization for debug endpoints

---

## Account Lockout Policy

Current Settings:
- **Failed Attempts Threshold:** 5 attempts
- **Lockout Duration:** 15 minutes
- **Auto-Unlock:** After 15 minutes have passed

After lockout expires:
- `IsLocked` is set to `false`
- `FailedLoginAttempts` is reset to `0`
- `LockoutEndDate` is cleared

---

## Password Requirements

From `LoginViewModel`:
- **Required:** Yes
- **Minimum Length:** 6 characters
- **Maximum Length:** 100 characters
- **Format:** Case-sensitive

Password is hashed using:
- **Algorithm:** SHA256
- **Salt:** Unique per user
- **Method:** SHA256(password + salt)

---

## Contact Support

If issues persist:
1. Collect the diagnostic output from `DiagnoseLogin`
2. Check database logs for any errors
3. Verify email configuration for any verification blocks
4. Contact system administrator

---

Last Updated: 2024
