namespace DealTrack.Domain.Enums
{
    public enum NotificationType
    {
        FollowUpReminder = 1,
        PaymentReminder = 2,
        SystemNotification = 3,
        TransferRequestReceived = 4,
        TransferRequestAccepted = 5,
        TransferRequestRejected = 6,
        ClientReassigned = 7,
        NewMemberJoined = 8
    }
}
