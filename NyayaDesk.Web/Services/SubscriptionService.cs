using NyayaDesk.Models.ViewModels.Subscription;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity.Core.EntityClient;
using System.Data.SqlClient;

namespace NyayaDesk.Services
{
    public class SubscriptionService
    {
        private readonly string _connectionString;
        public SubscriptionService(){var entity=new EntityConnectionStringBuilder(ConfigurationManager.ConnectionStrings["NyayaDeskDBEntities1"].ConnectionString);_connectionString=entity.ProviderConnectionString;}
        public TenantSubscriptionVM GetForUser(int userId)
        {
            const string sql=@"SELECT TOP(1)t.TenantId,t.Name,t.TenantCode,p.PlanName,p.PlanCode,s.Status,s.CurrentPeriodEnd,s.TrialEndDate,p.SubscriptionPlanId
FROM dbo.TenantUser tu JOIN dbo.Tenant t ON t.TenantId=tu.TenantId JOIN dbo.TenantSubscription s ON s.TenantId=t.TenantId
JOIN dbo.SubscriptionPlan p ON p.SubscriptionPlanId=s.SubscriptionPlanId WHERE tu.UserId=@UserId AND tu.Status='Active' AND t.IsActive=1 ORDER BY s.TenantSubscriptionId DESC";
            using(var cn=new SqlConnection(_connectionString))using(var cmd=new SqlCommand(sql,cn)){cmd.Parameters.Add("@UserId",SqlDbType.Int).Value=userId;cn.Open();using(var r=cmd.ExecuteReader()){if(!r.Read())return null;var end=Convert.ToDateTime(r["CurrentPeriodEnd"]);var status=Convert.ToString(r["Status"]);var planId=Convert.ToInt32(r["SubscriptionPlanId"]);var vm=new TenantSubscriptionVM{TenantId=Convert.ToInt32(r["TenantId"]),TenantName=Convert.ToString(r["Name"]),TenantCode=Convert.ToString(r["TenantCode"]),PlanName=Convert.ToString(r["PlanName"]),PlanCode=Convert.ToString(r["PlanCode"]),Status=status,PeriodEnd=end,TrialEnd=r["TrialEndDate"]==DBNull.Value?(DateTime?)null:Convert.ToDateTime(r["TrialEndDate"]),HasAccess=(status=="Active"||status=="Trialing")&&end>=DateTime.UtcNow,DaysRemaining=Math.Max(0,(int)Math.Ceiling((end-DateTime.UtcNow).TotalDays))};r.Close();vm.Features=GetFeatures(cn,planId);return vm;}}
        }
        public IList<SubscriptionPlanVM> GetPlans(){var list=new List<SubscriptionPlanVM>();using(var cn=new SqlConnection(_connectionString))using(var cmd=new SqlCommand("SELECT SubscriptionPlanId,PlanCode,PlanName,Description,MonthlyPrice,AnnualPrice,TrialDays,IsPopular FROM dbo.SubscriptionPlan WHERE IsActive=1 ORDER BY DisplayOrder",cn)){cn.Open();using(var r=cmd.ExecuteReader())while(r.Read())list.Add(new SubscriptionPlanVM{Id=Convert.ToInt32(r[0]),Code=Convert.ToString(r[1]),Name=Convert.ToString(r[2]),Description=Convert.ToString(r[3]),MonthlyPrice=Convert.ToDecimal(r[4]),AnnualPrice=Convert.ToDecimal(r[5]),TrialDays=Convert.ToInt32(r[6]),IsPopular=Convert.ToBoolean(r[7])});foreach(var p in list)p.Features=GetFeatures(cn,p.Id);}return list;}
        public bool HasFeature(int userId,string code){var current=GetForUser(userId);if(current==null||!current.HasAccess)return false;foreach(var feature in current.Features)if(String.Equals(feature.Code,code,StringComparison.OrdinalIgnoreCase))return feature.Enabled;return false;}
        private static IList<PlanFeatureVM> GetFeatures(SqlConnection cn,int planId){var list=new List<PlanFeatureVM>();using(var cmd=new SqlCommand("SELECT FeatureCode,FeatureName,IsEnabled,LimitValue FROM dbo.PlanFeature WHERE SubscriptionPlanId=@Id ORDER BY PlanFeatureId",cn)){cmd.Parameters.Add("@Id",SqlDbType.Int).Value=planId;using(var r=cmd.ExecuteReader())while(r.Read())list.Add(new PlanFeatureVM{Code=Convert.ToString(r[0]),Name=Convert.ToString(r[1]),Enabled=Convert.ToBoolean(r[2]),Limit=r[3]==DBNull.Value?(int?)null:Convert.ToInt32(r[3])});}return list;}
    }
}
