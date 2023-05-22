﻿using System;
using System.Threading;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Core;

namespace Catalina.Common
{
    public struct Event
    {
        public TimeSpan Interval;
        public DateTime LastExecuted;
        public Action Action;

        public Event(TimeSpan delay, TimeSpan interval, Action action)
        {
            Interval = interval; Action = action;
            LastExecuted = DateTime.UtcNow + delay;
        }
        public Event(TimeSpan interval, Action action)
        {
            Interval = interval; Action = action;
            LastExecuted = DateTime.UtcNow + TimeSpan.FromMinutes(5);
        }
    }
    public static class EventScheduler
    {
        private static List<Event> _events = new List<Event>();

        public static void Start(ServiceProvider services)
        {
            var methods = Assembly.GetExecutingAssembly().DefinedTypes
                .SelectMany(cl => cl.GetMethods(BindingFlags.Public | (BindingFlags.Public & BindingFlags.Static)))
                .Where(m => m.GetCustomAttribute<ScheduledInvoke>() is not null);

            foreach (var method in methods)
            {
                AddEvent( new Event(
                    action: method.CreateDelegate<Action>(), 
                    interval: method.GetCustomAttribute<ScheduledInvoke>().Interval,
                    delay: method.GetCustomAttribute<ScheduledInvoke>().Delay));
            }

            new Thread(() =>
            {
                while (true)
                {
                    Tick(services);
                    Thread.Sleep(_events.Min(e => e.Interval));
                }
            });
        }

        public static void AddEvent(Event @event) 
        {
            if (!_events.Any(e => e.Action == @event.Action))
            {
                _events.Add(@event);
            }
            else throw new Exceptions.DuplicateEntryException("the action provided is already scheduled.");
        }

        public static void RemoveEvent(Event @event)
        {
            if (_events.Any(e => e.Action == @event.Action))
            {
                _events.Remove(_events.First(e => e.Action == @event.Action));
            }
            else throw new Exceptions.InvalidArgumentException("the action provided does not exist.");
        }

        private static void Tick(ServiceProvider services)
        {
            _events.ForEach(e =>
            {
                if (DateTime.UtcNow - e.LastExecuted > e.Interval)
                {
                    try
                    {
                        e.Action.BeginInvoke(null, null);
                    }
                    catch (Exception ex)
                    {
                        services.GetRequiredService<Logger>().Error(ex, ex.Message);
                    }
                    finally
                    {
                        e.LastExecuted = DateTime.UtcNow;
                    }
                }
            });
        }

    }
    public class ScheduledInvoke : Attribute
    {
        public TimeSpan Interval, Delay;
    }
}