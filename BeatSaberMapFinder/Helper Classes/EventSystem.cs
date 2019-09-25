using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Events;

namespace BeatSaberMapFinder
{
    public static class EventSystem
    {
        private static IEventAggregator _current;
        public static IEventAggregator Current
        {
            get
            {
                return _current ?? (_current = new EventAggregator());
            }
        }

        private static PubSubEvent<T> GetEvent<T>()
        {
            return Current.GetEvent<PubSubEvent<T>>();
        }

        public static void Publish<T>()
        {
            Publish<T>(default(T));
        }

        public static void Publish<T>(T @event)
        {
            GetEvent<T>().Publish(@event);
        }

        public static SubscriptionToken Subscribe<T>(Action action, ThreadOption threadOption = ThreadOption.PublisherThread, bool keepSubscriberReferenceAlive = false)
        {
            return Subscribe<T>(e => action(), threadOption, keepSubscriberReferenceAlive);
        }

        public static SubscriptionToken Subscribe<T>(Action<T> action, ThreadOption threadOption = ThreadOption.PublisherThread, bool keepSubscriberReferenceAlive = false, Predicate<T> filter = null)
        {
            return GetEvent<T>().Subscribe(action, threadOption, keepSubscriberReferenceAlive, filter);
        }

        public static void Unsubscribe<T>(SubscriptionToken token)
        {
            GetEvent<T>().Unsubscribe(token);
        }

        public static void Unsubscribe<T>(Action<T> subscriber)
        {
            GetEvent<T>().Unsubscribe(subscriber);
        }
    }
}
