using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace GlowSequencer
{
    public abstract class Observable : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private IList<object> funcReferences = new List<object>();

        protected void SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (!object.Equals(storage, value))
            {
                storage = value;
                Notify(propertyName);
            }
        }

        protected void Notify([CallerMemberName] string propertyName = null)
        {
            if (propertyName == null)
                throw new ArgumentNullException("missing property to notify");

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected void ForwardPropertyEvents(string sourcePropertyName, INotifyPropertyChanged source, params string[] notifyPropertyNames)
        {
            RegisterHandler(source, (sender, e) =>
            {
                foreach (string p in notifyPropertyNames)
                    Notify(p);
            }, sourcePropertyName);
        }

        protected void ForwardPropertyEvents(string sourcePropertyName, INotifyPropertyChanged source, Action notifyHandler, bool callInitially = false)
        {
            RegisterHandler(source, (sender, e) => notifyHandler(), sourcePropertyName);
            if (callInitially)
                notifyHandler();
        }

        private void RegisterHandler(INotifyPropertyChanged source, EventHandler<PropertyChangedEventArgs> func, string propertyName)
        {
            if (source == this)
            {
                // save the overhead of weak event subscription and subscribe strongly; all references stay local to this object anyway
                PropertyChanged += (sender, e) => { if (e.PropertyName == propertyName) func(sender, e); };
            }
            else
            {
                // because we use weak event subscription, above lambda expressions would instantly be garbage collected;
                // to prevent that, we keep a list of strong references in !!this!! class;
                // the weak reference will still be collected after the listener (this class) has been collected
                funcReferences.Add(func);
                PropertyChangedEventManager.AddHandler(source, func, propertyName);
            }
        }

        /// <summary>Listens to CollectionChanged events and notifies a list of other properties.</summary>
        protected void ForwardCollectionEvents(INotifyCollectionChanged source, params string[] notifyPropertyNames)
        {
            EventHandler<NotifyCollectionChangedEventArgs> func = (sender, e) =>
            {
                foreach (string p in notifyPropertyNames)
                    Notify(p);
            };
            funcReferences.Add(func);
            CollectionChangedEventManager.AddHandler(source, func);
        }

        /// <summary>Listens to CollectionChanged events and and invokes a callback.
        /// The benefit is a weak subscription that keeps a strong reference while the subscriber is alive.</summary>
        protected void ForwardCollectionEvents(INotifyCollectionChanged source, Action notifyHandler)
        {
            EventHandler<NotifyCollectionChangedEventArgs> func = (sender, e) => notifyHandler();
            funcReferences.Add(func);
            CollectionChangedEventManager.AddHandler(source, func);
        }

        /// <summary>Listens to CollectionChanged events and and invokes a callback.
        /// The benefit is a weak subscription that keeps a strong reference while the subscriber is alive.</summary>
        protected void ForwardCollectionEvents(INotifyCollectionChanged source, EventHandler<NotifyCollectionChangedEventArgs> notifyHandler)
        {
            funcReferences.Add(notifyHandler);
            CollectionChangedEventManager.AddHandler(source, notifyHandler);
        }
    }
}
