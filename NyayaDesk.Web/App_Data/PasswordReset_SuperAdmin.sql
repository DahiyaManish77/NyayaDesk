-- ============================================================================
-- NyayaDesk Password Reset & User Account Fix Script
-- ============================================================================
-- IMPORTANT: This script resets the superadmin password for testing
-- Update with your desired password and salt before running
-- ============================================================================

-- Password Setup Example:
-- Password: Welcome@123
-- Salt: 77AAE9B9-13DE-43B7-A2E3-1FBA0C390BD6
-- Hash: Generated from PasswordHashHelper.GeneratePasswordHash()

-- ============================================================================
-- STEP 1: Check current user status
-- ============================================================================
SELECT 
    UserId,
    Email,
    UserName,
    IsActive,
    IsDeleted,
    IsLocked,
    FailedLoginAttempts,
    LockoutEndDate,
    LastLoginDate,
    PasswordHash,
    PasswordSalt
FROM [User]
WHERE Email = 'admin@nyayadesk.in'

-- ============================================================================
-- STEP 2: Generate new password hash
-- ============================================================================
-- Option A: Use PasswordHashHelper.GeneratePasswordHash() in C#
-- Option B: Use this SQL to reset with a placeholder hash
-- You MUST run the C# code to generate the actual hash

-- Example output from C#:
-- Password: Welcome@123
-- Salt: 77AAE9B9-13DE-43B7-A2E3-1FBA0C390BD6
-- Hash: [Output from GeneratePasswordHash()]

-- ============================================================================
-- STEP 3: Update user with new password hash
-- ============================================================================
-- IMPORTANT: Replace the HASH value below with actual hash from C#
UPDATE [User]
SET 
    PasswordHash = 'REPLACE_WITH_ACTUAL_HASH_FROM_PASSWORD_RESET_TOOL',
    PasswordSalt = '77AAE9B9-13DE-43B7-A2E3-1FBA0C390BD6',
    IsActive = 1,
    IsLocked = 0,
    FailedLoginAttempts = 0,
    LockoutEndDate = NULL,
    IsDeleted = 0
WHERE Email = 'admin@nyayadesk.in'

-- ============================================================================
-- STEP 4: Verify update
-- ============================================================================
SELECT 
    UserId,
    Email,
    UserName,
    IsActive,
    IsLocked,
    FailedLoginAttempts,
    PasswordHash,
    PasswordSalt
FROM [User]
WHERE Email = 'admin@nyayadesk.in'

-- ============================================================================
-- ALTERNATIVE: Quick reset with known values
-- ============================================================================
-- If you know the exact hash for a password, use this directly:
-- 
-- UPDATE [User]
-- SET PasswordHash = 'B9E838FC0A7EEED6A961D03D3C6AC35E794AE921E55C255D966299A4604466BD',
--     PasswordSalt = '77AAE9B9-13DE-43B7-A2E3-1FBA0C390BD6',
--     IsActive = 1,
--     IsLocked = 0,
--     FailedLoginAttempts = 0,
--     LockoutEndDate = NULL
-- WHERE Email = 'admin@nyayadesk.in'
--
-- This hash corresponds to password: [Unknown - you need to recalculate]

-- ============================================================================
-- OPTIONAL: Create a test user if needed
-- ============================================================================
-- Uncomment and run if you want to create a test account
/*
INSERT INTO [User] (
    UserGuid,
    UserName,
    Email,
    MobileNumber,
    PasswordHash,
    PasswordSalt,
    SecurityStamp,
    FirstName,
    LastName,
    DisplayName,
    IsEmailVerified,
    IsMobileVerified,
    IsActive,
    IsDeleted,
    CreatedOn
)
VALUES (
    NEWID(),
    'testuser',
    'test@nyayadesk.in',
    '9999999999',
    'REPLACE_WITH_HASH',
    '77AAE9B9-13DE-43B7-A2E3-1FBA0C390BD6',
    NEWID(),
    'Test',
    'User',
    'Test User',
    1,
    1,
    1,
    0,
    GETDATE()
)
*/

-- ============================================================================
-- HELPFUL QUERIES FOR TROUBLESHOOTING
-- ============================================================================

-- View all users
-- SELECT * FROM [User] WHERE IsDeleted = 0

-- Reset all failed login attempts
-- UPDATE [User] SET FailedLoginAttempts = 0, IsLocked = 0, LockoutEndDate = NULL

-- Unlock specific user
-- UPDATE [User] SET IsLocked = 0, FailedLoginAttempts = 0, LockoutEndDate = NULL WHERE Email = 'admin@nyayadesk.in'

-- Activate user
-- UPDATE [User] SET IsActive = 1 WHERE Email = 'admin@nyayadesk.in'

-- View user roles
-- SELECT u.Email, r.RoleCode, r.RoleName, ur.IsPrimaryRole FROM [User] u 
-- JOIN UserRole ur ON u.UserId = ur.UserId 
-- JOIN [Role] r ON ur.RoleId = r.RoleId 
-- WHERE u.Email = 'admin@nyayadesk.in'
