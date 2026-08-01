using System;
using System.Collections.Generic;

namespace NyayaDesk.Models.ViewModels.Subscription
{
    public class PlanFeatureVM { public string Code { get; set; } public string Name { get; set; } public bool Enabled { get; set; } public int? Limit { get; set; } }
    public class SubscriptionPlanVM { public int Id { get; set; } public string Code { get; set; } public string Name { get; set; } public string Description { get; set; } public decimal MonthlyPrice { get; set; } public decimal AnnualPrice { get; set; } public int TrialDays { get; set; } public bool IsPopular { get; set; } public IList<PlanFeatureVM> Features { get; set; } = new List<PlanFeatureVM>(); }
    public class TenantSubscriptionVM { public int TenantId { get; set; } public string TenantName { get; set; } public string TenantCode { get; set; } public string PlanName { get; set; } public string PlanCode { get; set; } public string Status { get; set; } public DateTime PeriodEnd { get; set; } public DateTime? TrialEnd { get; set; } public bool HasAccess { get; set; } public int DaysRemaining { get; set; } public IList<PlanFeatureVM> Features { get; set; } = new List<PlanFeatureVM>(); }
    public class SubscriptionPageVM { public TenantSubscriptionVM Current { get; set; } public IList<SubscriptionPlanVM> Plans { get; set; } = new List<SubscriptionPlanVM>(); }
}
