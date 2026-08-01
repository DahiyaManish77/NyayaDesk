using NyayaDesk.Models.ViewModels;
using System;
using System.Configuration;
using System.Data;
using System.Data.Entity.Core.EntityClient;
using System.Data.SqlClient;

namespace NyayaDesk.Services
{
    public class WorkspaceOnboardingService
    {
        private readonly string _connectionString;
        public WorkspaceOnboardingService(){_connectionString=new EntityConnectionStringBuilder(ConfigurationManager.ConnectionStrings["NyayaDeskDBEntities1"].ConnectionString).ProviderConnectionString;}
        public void Register(WorkspaceRegistrationVM model)
        {
            string email=model.Email.Trim().ToLowerInvariant();string roleCode=RoleCode(model.ProfessionalRole);var account=new AccountService();string salt=account.GenerateSalt();string hash=account.HashPassword(model.Password,salt);
            using(var cn=new SqlConnection(_connectionString)){cn.Open();using(var tx=cn.BeginTransaction(IsolationLevel.Serializable)){
                try{
                    using(var exists=new SqlCommand("SELECT COUNT(1) FROM dbo.[User] WITH(UPDLOCK,HOLDLOCK) WHERE LOWER(Email)=@Email AND IsDeleted=0",cn,tx)){exists.Parameters.Add("@Email",SqlDbType.NVarChar,200).Value=email;if(Convert.ToInt32(exists.ExecuteScalar())>0)throw new InvalidOperationException("An account with this email already exists.");}
                    int userId;using(var cmd=new SqlCommand(@"INSERT dbo.[User](UserName,Email,MobileNumber,PasswordHash,PasswordSalt,FirstName,LastName,DisplayName,IsEmailVerified,IsMobileVerified,IsFirstLogin,IsSystemUser,IsActive,IsDeleted)
VALUES(@UserName,@Email,@Mobile,@Hash,@Salt,@First,@Last,@Display,0,0,1,0,1,0);SELECT CAST(SCOPE_IDENTITY() AS INT);",cn,tx)){cmd.Parameters.Add("@UserName",SqlDbType.NVarChar,100).Value=email;cmd.Parameters.Add("@Email",SqlDbType.NVarChar,200).Value=email;cmd.Parameters.Add("@Mobile",SqlDbType.NVarChar,20).Value=model.MobileNumber.Trim();cmd.Parameters.Add("@Hash",SqlDbType.NVarChar,500).Value=hash;cmd.Parameters.Add("@Salt",SqlDbType.NVarChar,500).Value=salt;cmd.Parameters.Add("@First",SqlDbType.NVarChar,100).Value=model.FirstName.Trim();cmd.Parameters.Add("@Last",SqlDbType.NVarChar,100).Value=(object)(model.LastName??"").Trim()??DBNull.Value;cmd.Parameters.Add("@Display",SqlDbType.NVarChar,250).Value=(model.FirstName+" "+model.LastName).Trim();userId=Convert.ToInt32(cmd.ExecuteScalar());}
                    int roleId;using(var cmd=new SqlCommand("SELECT RoleId FROM dbo.Role WHERE RoleCode=@Code AND IsActive=1",cn,tx)){cmd.Parameters.Add("@Code",SqlDbType.NVarChar,30).Value=roleCode;object value=cmd.ExecuteScalar();if(value==null)throw new InvalidOperationException("Selected professional role is unavailable.");roleId=Convert.ToInt32(value);}
                    using(var cmd=new SqlCommand("INSERT dbo.UserRole(UserId,RoleId,IsPrimaryRole,IsActive) VALUES(@User,@Role,1,1)",cn,tx)){cmd.Parameters.Add("@User",SqlDbType.Int).Value=userId;cmd.Parameters.Add("@Role",SqlDbType.Int).Value=roleId;cmd.ExecuteNonQuery();}
                    string tenantCode="NYD-"+Guid.NewGuid().ToString("N").Substring(0,8).ToUpperInvariant();int tenantId;using(var cmd=new SqlCommand("INSERT dbo.Tenant(TenantCode,Name,LegalName,Email,Phone) VALUES(@Code,@Name,@Name,@Email,@Phone);SELECT CAST(SCOPE_IDENTITY() AS INT);",cn,tx)){cmd.Parameters.Add("@Code",SqlDbType.NVarChar,30).Value=tenantCode;cmd.Parameters.Add("@Name",SqlDbType.NVarChar,180).Value=model.WorkspaceName.Trim();cmd.Parameters.Add("@Email",SqlDbType.NVarChar,200).Value=email;cmd.Parameters.Add("@Phone",SqlDbType.NVarChar,20).Value=model.MobileNumber.Trim();tenantId=Convert.ToInt32(cmd.ExecuteScalar());}
                    using(var cmd=new SqlCommand("INSERT dbo.TenantUser(TenantId,UserId,IsOwner) VALUES(@Tenant,@User,1)",cn,tx)){cmd.Parameters.Add("@Tenant",SqlDbType.Int).Value=tenantId;cmd.Parameters.Add("@User",SqlDbType.Int).Value=userId;cmd.ExecuteNonQuery();}
                    using(var cmd=new SqlCommand(@"DECLARE @Plan INT=(SELECT SubscriptionPlanId FROM dbo.SubscriptionPlan WHERE PlanCode=@PlanCode AND IsActive=1);IF @Plan IS NULL THROW 50001,'Selected plan is unavailable.',1;DECLARE @Days INT=(SELECT TrialDays FROM dbo.SubscriptionPlan WHERE SubscriptionPlanId=@Plan);INSERT dbo.TenantSubscription(TenantId,SubscriptionPlanId,Status,StartDate,TrialEndDate,CurrentPeriodStart,CurrentPeriodEnd,BillingCycle) VALUES(@Tenant,@Plan,'Trialing',SYSUTCDATETIME(),DATEADD(DAY,@Days,SYSUTCDATETIME()),SYSUTCDATETIME(),DATEADD(DAY,@Days,SYSUTCDATETIME()),'Trial');",cn,tx)){cmd.Parameters.Add("@Tenant",SqlDbType.Int).Value=tenantId;cmd.Parameters.Add("@PlanCode",SqlDbType.NVarChar,40).Value=model.PlanCode;cmd.ExecuteNonQuery();}
                    tx.Commit();
                }catch{tx.Rollback();throw;}
            }}
        }
        private static string RoleCode(string role){switch((role??"").ToUpperInvariant()){case "SENIORADV":return "SENIORADV";case "FIRMADMIN":return "ADMIN";default:return "ADVOCATE";}}
    }
}
