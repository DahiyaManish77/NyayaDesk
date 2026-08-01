SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID('dbo.Tenant','U') IS NULL
CREATE TABLE dbo.Tenant(
 TenantId INT IDENTITY(1,1) PRIMARY KEY,TenantGuid UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
 TenantCode NVARCHAR(30) NOT NULL,Name NVARCHAR(180) NOT NULL,LegalName NVARCHAR(220) NULL,
 Email NVARCHAR(255) NULL,Phone NVARCHAR(20) NULL,Gstin NVARCHAR(20) NULL,TimeZoneId NVARCHAR(80) NOT NULL DEFAULT 'India Standard Time',
 Status NVARCHAR(30) NOT NULL DEFAULT 'Active',IsActive BIT NOT NULL DEFAULT 1,CreatedOn DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
 CONSTRAINT UX_Tenant_Code UNIQUE(TenantCode),CONSTRAINT UX_Tenant_Guid UNIQUE(TenantGuid));

IF OBJECT_ID('dbo.TenantUser','U') IS NULL
CREATE TABLE dbo.TenantUser(TenantUserId INT IDENTITY PRIMARY KEY,TenantId INT NOT NULL,UserId INT NOT NULL,
 IsOwner BIT NOT NULL DEFAULT 0,Status NVARCHAR(30) NOT NULL DEFAULT 'Active',JoinedOn DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
 CONSTRAINT FK_TenantUser_Tenant FOREIGN KEY(TenantId) REFERENCES dbo.Tenant(TenantId),
 CONSTRAINT FK_TenantUser_User FOREIGN KEY(UserId) REFERENCES dbo.[User](UserId),CONSTRAINT UX_TenantUser UNIQUE(TenantId,UserId));

IF OBJECT_ID('dbo.SubscriptionPlan','U') IS NULL
CREATE TABLE dbo.SubscriptionPlan(SubscriptionPlanId INT IDENTITY PRIMARY KEY,PlanCode NVARCHAR(40) NOT NULL UNIQUE,
 PlanName NVARCHAR(100) NOT NULL,Description NVARCHAR(500) NULL,MonthlyPrice DECIMAL(18,2) NOT NULL,AnnualPrice DECIMAL(18,2) NOT NULL,
 TrialDays INT NOT NULL DEFAULT 14,DisplayOrder INT NOT NULL DEFAULT 0,IsPopular BIT NOT NULL DEFAULT 0,IsActive BIT NOT NULL DEFAULT 1);

IF OBJECT_ID('dbo.PlanFeature','U') IS NULL
CREATE TABLE dbo.PlanFeature(PlanFeatureId INT IDENTITY PRIMARY KEY,SubscriptionPlanId INT NOT NULL,FeatureCode NVARCHAR(80) NOT NULL,
 FeatureName NVARCHAR(150) NOT NULL,IsEnabled BIT NOT NULL DEFAULT 1,LimitValue INT NULL,
 CONSTRAINT FK_PlanFeature_Plan FOREIGN KEY(SubscriptionPlanId) REFERENCES dbo.SubscriptionPlan(SubscriptionPlanId),
 CONSTRAINT UX_PlanFeature UNIQUE(SubscriptionPlanId,FeatureCode));

IF OBJECT_ID('dbo.TenantSubscription','U') IS NULL
CREATE TABLE dbo.TenantSubscription(TenantSubscriptionId INT IDENTITY PRIMARY KEY,TenantId INT NOT NULL,SubscriptionPlanId INT NOT NULL,
 Status NVARCHAR(30) NOT NULL,StartDate DATETIME2 NOT NULL,TrialEndDate DATETIME2 NULL,CurrentPeriodStart DATETIME2 NOT NULL,
 CurrentPeriodEnd DATETIME2 NOT NULL,GracePeriodEnd DATETIME2 NULL,BillingCycle NVARCHAR(20) NOT NULL DEFAULT 'Monthly',
 CancelAtPeriodEnd BIT NOT NULL DEFAULT 0,ExternalCustomerId NVARCHAR(120) NULL,ExternalSubscriptionId NVARCHAR(120) NULL,
 CreatedOn DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),UpdatedOn DATETIME2 NULL,
 CONSTRAINT FK_TenantSubscription_Tenant FOREIGN KEY(TenantId) REFERENCES dbo.Tenant(TenantId),
 CONSTRAINT FK_TenantSubscription_Plan FOREIGN KEY(SubscriptionPlanId) REFERENCES dbo.SubscriptionPlan(SubscriptionPlanId));

IF OBJECT_ID('dbo.SubscriptionPayment','U') IS NULL
CREATE TABLE dbo.SubscriptionPayment(SubscriptionPaymentId INT IDENTITY PRIMARY KEY,TenantSubscriptionId INT NOT NULL,
 Amount DECIMAL(18,2) NOT NULL,Currency CHAR(3) NOT NULL DEFAULT 'INR',Status NVARCHAR(30) NOT NULL,Gateway NVARCHAR(30) NULL,
 GatewayPaymentId NVARCHAR(150) NULL,PaidOn DATETIME2 NULL,CreatedOn DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
 CONSTRAINT FK_SubscriptionPayment_Subscription FOREIGN KEY(TenantSubscriptionId) REFERENCES dbo.TenantSubscription(TenantSubscriptionId));

IF OBJECT_ID('dbo.TenantUsage','U') IS NULL
CREATE TABLE dbo.TenantUsage(TenantUsageId INT IDENTITY PRIMARY KEY,TenantId INT NOT NULL,FeatureCode NVARCHAR(80) NOT NULL,
 PeriodKey CHAR(7) NOT NULL,UsedValue INT NOT NULL DEFAULT 0,UpdatedOn DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
 CONSTRAINT FK_TenantUsage_Tenant FOREIGN KEY(TenantId) REFERENCES dbo.Tenant(TenantId),CONSTRAINT UX_TenantUsage UNIQUE(TenantId,FeatureCode,PeriodKey));

MERGE dbo.SubscriptionPlan AS t USING(VALUES
('SOLO','Solo Advocate','Essential practice management for an independent advocate',799,7990,14,1,0),
('PRO','Professional','Complete workspace for growing legal teams',1999,19990,14,2,1),
('FIRM','Law Firm','Advanced controls, analytics and larger teams',4999,49990,21,3,0))
AS s(Code,Name,Description,Monthly,Annual,TrialDays,Sort,Popular) ON t.PlanCode=s.Code
WHEN NOT MATCHED THEN INSERT(PlanCode,PlanName,Description,MonthlyPrice,AnnualPrice,TrialDays,DisplayOrder,IsPopular) VALUES(s.Code,s.Name,s.Description,s.Monthly,s.Annual,s.TrialDays,s.Sort,s.Popular);

DECLARE @Features TABLE(PlanCode NVARCHAR(40),Code NVARCHAR(80),Name NVARCHAR(150),Enabled BIT,LimitValue INT);
INSERT @Features VALUES
('SOLO','USERS','Team users',1,1),('SOLO','ACTIVE_CASES','Active cases',1,75),('SOLO','STORAGE_GB','Document storage (GB)',1,5),('SOLO','BILLING','Billing and invoices',1,NULL),('SOLO','ADVANCED_REPORTS','Advanced reports',0,NULL),
('PRO','USERS','Team users',1,10),('PRO','ACTIVE_CASES','Active cases',1,500),('PRO','STORAGE_GB','Document storage (GB)',1,50),('PRO','BILLING','Billing and invoices',1,NULL),('PRO','ADVANCED_REPORTS','Advanced reports',1,NULL),
('FIRM','USERS','Team users',1,50),('FIRM','ACTIVE_CASES','Active cases',1,5000),('FIRM','STORAGE_GB','Document storage (GB)',1,250),('FIRM','BILLING','Billing and invoices',1,NULL),('FIRM','ADVANCED_REPORTS','Advanced reports',1,NULL);
INSERT dbo.PlanFeature(SubscriptionPlanId,FeatureCode,FeatureName,IsEnabled,LimitValue)
SELECT p.SubscriptionPlanId,f.Code,f.Name,f.Enabled,f.LimitValue FROM @Features f JOIN dbo.SubscriptionPlan p ON p.PlanCode=f.PlanCode
WHERE NOT EXISTS(SELECT 1 FROM dbo.PlanFeature x WHERE x.SubscriptionPlanId=p.SubscriptionPlanId AND x.FeatureCode=f.Code);

IF NOT EXISTS(SELECT 1 FROM dbo.Tenant)
BEGIN
 INSERT dbo.Tenant(TenantCode,Name,LegalName,Email) VALUES('NYD-0001','NyayaDesk Legal Workspace','NyayaDesk Legal Workspace','admin@nyayadesk.in');
 DECLARE @TenantId INT=SCOPE_IDENTITY();
 INSERT dbo.TenantUser(TenantId,UserId,IsOwner) SELECT @TenantId,UserId,1 FROM dbo.[User] WHERE IsDeleted=0;
 DECLARE @PlanId INT=(SELECT SubscriptionPlanId FROM dbo.SubscriptionPlan WHERE PlanCode='PRO');
 INSERT dbo.TenantSubscription(TenantId,SubscriptionPlanId,Status,StartDate,TrialEndDate,CurrentPeriodStart,CurrentPeriodEnd,BillingCycle)
 VALUES(@TenantId,@PlanId,'Trialing',SYSUTCDATETIME(),DATEADD(DAY,14,SYSUTCDATETIME()),SYSUTCDATETIME(),DATEADD(DAY,14,SYSUTCDATETIME()),'Trial');
END

COMMIT;
