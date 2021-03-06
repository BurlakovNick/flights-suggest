﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FlightsSuggest.Core.Infrastructure;
using FlightsSuggest.Core.Timelines;

namespace FlightsSuggest.Core.Notifications
{
    public class Notifier : INotifier
    {
        private readonly INotificationSender[] senders;
        private readonly ITimeline[] timelines;
        private readonly IOffsetStorage offsetStorage;

        public Notifier(
            INotificationSender[] senders,
            ITimeline[] timelines,
            IOffsetStorage offsetStorage
        )
        {
            this.senders = senders;
            this.timelines = timelines;
            this.offsetStorage = offsetStorage;
        }

        public async Task NotifyAsync(Subscriber[] subscribers)
        {
            foreach (var subscriber in subscribers)
            {
                await NotifyAsync(subscriber);
            }
        }

        public async Task NotifyAsync(Subscriber subscriber)
        {
            foreach (var notificationSender in senders.Where(s => s.CanSend(subscriber)))
            {
                foreach (var timeline in timelines)
                {
                    var offsetId = GetOffsetId(subscriber.Id, timeline.Name);
                    var offset = await offsetStorage.FindAsync(offsetId);
                    if (offset == null)
                    {
                        var latestOffset = await timeline.GetLatestOffsetAsync();
                        if (latestOffset.HasValue)
                        {
                            await offsetStorage.WriteAsync(offsetId, latestOffset.Value);
                        }
                        continue;
                    }

                    var flightEnumerator = timeline.GetNewsEnumerator(offset.Value);
                    while (true)
                    {
                        var (hasNext, flightNews) = await flightEnumerator.MoveNextAsync();
                        if (!hasNext)
                        {
                            break;
                        }

                        if (subscriber.ShouldNotify(flightNews))
                        {
                            notificationSender.SendTo(subscriber, flightNews);
                        }

                        await offsetStorage.WriteAsync(offsetId, flightNews.Offset);
                    }
                }
            }
        }

        public Task RewindOffsetAsync(string subscriberId, string timelineName, long offset)
        {
            var offsetId = GetOffsetId(subscriberId, timelineName);
            return offsetStorage.WriteAsync(offsetId, offset);
        }

        public async Task<(string timelineName, DateTime? offset)[]> SelectOffsetsAsync(string subscriberId)
        {
            var result = new List<(string, DateTime?)>();
            foreach (var timeline in timelines)
            {
                var offsetId = GetOffsetId(subscriberId, timeline.Name);
                var offset = await offsetStorage.FindAsync(offsetId);
                if (offset.HasValue)
                {
                    result.Add((timeline.Name, new DateTime(offset.Value)));
                }
                else
                {
                    result.Add((timeline.Name, null));
                }
            }

            return result.ToArray();
        }

        private static string GetOffsetId(string subscriberId, string timelineName) => $"{subscriberId}_{timelineName}";
    }
}