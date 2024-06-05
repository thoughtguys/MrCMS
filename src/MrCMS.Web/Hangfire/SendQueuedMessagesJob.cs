using Hangfire;
using MrCMS.Tasks;

namespace MrCMS.Web.Hangfire;

public class SendQueuedMessagesJob : MrCMSRecurringJob
{
    public override void OnAddOrUpdate(string cron)
    {
        RecurringJob.AddOrUpdate<ISendQueuedMessagesTask>(Id, service => service.Execute(), cron);
    }
}