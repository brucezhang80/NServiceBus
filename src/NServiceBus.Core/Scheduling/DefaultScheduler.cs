namespace NServiceBus.Scheduling
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Logging;

    class DefaultScheduler
    {
        public void Schedule(TaskDefinition taskDefinition)
        {
            scheduledTasks[taskDefinition.Id] = taskDefinition;
        }

        public async Task Start(Guid taskId, IBusContext busContext)
        {
            TaskDefinition taskDefinition;

            if (!scheduledTasks.TryGetValue(taskId, out taskDefinition))
            {
                logger.InfoFormat("Could not find any scheduled task with id {0}. The DefaultScheduler does not persist tasks between restarts.", taskId);
                return;
            }

            await DeferTask(taskDefinition, busContext).ConfigureAwait(false);
            await ExecuteTask(taskDefinition, busContext).ConfigureAwait(false);
        }

        static async Task ExecuteTask(TaskDefinition taskDefinition, IBusContext busContext)
        {
            logger.InfoFormat("Start executing scheduled task named '{0}'.", taskDefinition.Name);
            var sw = new Stopwatch();
            sw.Start();

            try
            {
                await taskDefinition.Task(busContext).ConfigureAwait(false);
                logger.InfoFormat("Scheduled task '{0}' run for {1}", taskDefinition.Name, sw.Elapsed);
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to execute scheduled task '{taskDefinition.Name}'.", ex);
            }
            finally
            {
                sw.Stop();
            }
        }

        static Task DeferTask(TaskDefinition taskDefinition, IBusContext bus)
        {
            var options = new SendOptions();

            options.DelayDeliveryWith(taskDefinition.Every);
            options.RouteToLocalEndpointInstance();

            return bus.Send(new Messages.ScheduledTask
            {
                TaskId = taskDefinition.Id,
                Name = taskDefinition.Name,
                Every = taskDefinition.Every
            }, options);
        }

        static ILog logger = LogManager.GetLogger<DefaultScheduler>();
        internal ConcurrentDictionary<Guid, TaskDefinition> scheduledTasks = new ConcurrentDictionary<Guid, TaskDefinition>();
    }
}