using DealTrack.Domain.Enums;

namespace DealTrack.Domain.Constants
{
    public static class PlanLimits
    {
        public static int GetMaxClients(SubscriptionPlan plan) => plan switch
        {
            SubscriptionPlan.Free => 10,
            _ => int.MaxValue
        };

        public static int GetMaxSalesUnderTeamLead(SubscriptionPlan plan) => plan switch
        {
            SubscriptionPlan.Free => 2,
            SubscriptionPlan.Pro => 5,
            _ => int.MaxValue
        };

        public static int GetMaxTeamLeads(SubscriptionPlan plan) => plan switch
        {
            SubscriptionPlan.Free => 2,
            SubscriptionPlan.Pro => 10,
            _ => int.MaxValue
        };

        public static int GetMaxDirectSales(SubscriptionPlan plan) => plan switch
        {
            SubscriptionPlan.Free => 3,
            SubscriptionPlan.Pro => 20,
            _ => int.MaxValue
        };
    }
}
