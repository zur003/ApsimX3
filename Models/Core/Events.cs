﻿namespace Models.Core
{
    using APSIM.Shared.Utilities;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// An event handling class
    /// </summary>
    public class Events : IEvent
    {
        private IModel relativeTo;
        private ScopingRules scope = new ScopingRules();

        /// <summary>Constructor</summary>
        /// <param name="relativeTo">The model this events instance is relative to</param>
        public Events(IModel relativeTo)
        {
            this.relativeTo = relativeTo;
        }

        /// <summary>Connect all events in the specified simulation.</summary>
        public void ConnectEvents()
        {
            // Get a complete list of all models in simulation (including the simulation itself).
            List<IModel> allModels = new List<IModel>();
            allModels.Add(relativeTo);
            allModels.AddRange(Apsim.ChildrenRecursively(relativeTo));

            var publishers = Publisher.FindAll(allModels);
            var subscribers = Subscriber.GetAll(allModels);

            foreach (Publisher publisher in publishers)
                if (subscribers.ContainsKey(publisher.Name))
                    foreach (var subscriber in subscribers[publisher.Name])
                        if (scope.InScopeOf(subscriber.Model, publisher.Model))
                            publisher.ConnectSubscriber(subscriber);
        }

        /// <summary>Connect all events in the specified simulation.</summary>
        public void DisconnectEvents()
        {
            List<IModel> allModels = new List<IModel>();
            allModels.Add(relativeTo);
            allModels.AddRange(Apsim.ChildrenRecursively(relativeTo));
            List<Events.Publisher> publishers = Events.Publisher.FindAll(allModels);
            foreach (Events.Publisher publisher in publishers)
                publisher.DisconnectAll();
        }

        /// <summary>
        /// Subscribe to an event. Will throw if namePath doesn't point to a event publisher.
        /// </summary>
        /// <param name="eventNameAndPath">The name of the event to subscribe to</param>
        /// <param name="handler">The event handler</param>
        public void Subscribe(string eventNameAndPath, EventHandler handler)
        {
            // Get the name of the component and event.
            string componentName = StringUtilities.ParentName(eventNameAndPath, '.');
            if (componentName == null)
                throw new Exception("Invalid syntax for event: " + eventNameAndPath);
            string eventName = StringUtilities.ChildName(eventNameAndPath, '.');

            // Get the component.
            object component = Apsim.Get(relativeTo, componentName);
            if (component == null)
                throw new Exception(Apsim.FullPath(relativeTo) + " can not find the component: " + componentName);

            // Get the EventInfo for the published event.
            EventInfo componentEvent = component.GetType().GetEvent(eventName);
            if (componentEvent == null)
                throw new Exception("Cannot find event: " + eventName + " in model: " + componentName);

            // Subscribe to the event.
            Delegate target = Delegate.CreateDelegate(componentEvent.EventHandlerType, handler.Target, handler.Method);
            componentEvent.AddEventHandler(component, target);
        }

        /// <summary>
        /// Unsubscribe an event. Throws if not found.
        /// </summary>
        /// <param name="eventNameAndPath">The name of the event to subscribe to</param>
        /// <param name="handler">The event handler</param>
        public void Unsubscribe(string eventNameAndPath, EventHandler handler)
        {
            // Get the name of the component and event.
            string componentName = StringUtilities.ParentName(eventNameAndPath, '.');
            if (componentName == null)
                throw new Exception("Invalid syntax for event: " + eventNameAndPath);
            string eventName = StringUtilities.ChildName(eventNameAndPath, '.');

            // Get the component.
            object component = Apsim.Get(relativeTo, componentName);
            if (component == null)
                throw new Exception(Apsim.FullPath(relativeTo) + " can not find the component: " + componentName);

            // Get the EventInfo for the published event.
            EventInfo componentEvent = component.GetType().GetEvent(eventName);
            if (componentEvent == null)
                throw new Exception("Cannot find event: " + eventName + " in model: " + componentName);

            // Unsubscribe to the event.
            Delegate target = Delegate.CreateDelegate(componentEvent.EventHandlerType, handler.Target, handler.Method);
            componentEvent.RemoveEventHandler(component, target);
        }

        /// <summary>
        /// Call the specified event on the specified model and all child models.
        /// </summary>
        /// <param name="eventName">The name of the event</param>
        /// <param name="args">The event arguments. Can be null</param>
        public void Publish(string eventName, object[] args)
        {
            List<Subscriber> subscribers = Subscriber.FindAll(eventName, relativeTo, scope);

            foreach (Subscriber subscriber in subscribers)
                subscriber.Invoke(args);
        }
        
        /// <summary>A wrapper around an event subscriber MethodInfo.</summary>
        internal class Subscriber
        {
            /// <summary>The model instance containing the event hander.</summary>
            public IModel Model { get; set; }

            /// <summary>The method info for the event handler.</summary>
            private MethodInfo methodInfo { get; set; }

            /// <summary>Gets or sets the name of the event.</summary>
            public string Name { get; private set; }

            public Subscriber(string name, IModel model, MethodInfo method)
            {
                Name = name;
                Model = model;
                methodInfo = method;
            }

            internal static Dictionary<string, List<Subscriber>> GetAll(List<IModel> allModels)
            {
                Dictionary<string, List<Subscriber>> subscribers = new Dictionary<string, List<Subscriber>>();

                foreach (IModel modelNode in allModels)
                {
                    foreach (MethodInfo method in modelNode.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy))
                    {
                        EventSubscribeAttribute attribute = (EventSubscribeAttribute)ReflectionUtilities.GetAttribute(method, typeof(EventSubscribeAttribute), false);
                        if (attribute != null)
                        {
                            string eventName = attribute.ToString();
                            Subscriber subscriber = new Subscriber(eventName, modelNode, method);

                            if (!subscribers.ContainsKey(eventName))
                                subscribers.Add(eventName, new List<Subscriber>());
                            subscribers[eventName].Add(subscriber);
                        }
                    }
                }

                return subscribers;
            }

            internal static Dictionary<string, List<Subscriber>> GetAll(string name, IModel relativeTo, ScopingRules scope)
            {
                IModel[] allModels = scope.FindAll(relativeTo);
                Dictionary<string, List<Subscriber>> subscribers = new Dictionary<string, List<Subscriber>>();

                foreach (IModel modelNode in allModels)
                {
                    foreach (MethodInfo method in modelNode.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy))
                    {
                        EventSubscribeAttribute attribute = (EventSubscribeAttribute)ReflectionUtilities.GetAttribute(method, typeof(EventSubscribeAttribute), false);
                        if (attribute != null)
                        {
                            string eventName = attribute.ToString();

                            if (!eventName.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                                continue;

                            Subscriber subscriber = new Subscriber(eventName, modelNode, method);

                            if (subscribers[eventName] == null)
                                subscribers[eventName] = new List<Subscriber>();
                            subscribers[eventName].Add(subscriber);
                        }
                    }
                }

                return subscribers;
            }

            /// <summary>Find all event subscribers in the specified models.</summary>
            /// <param name="allModels">A list of all models in simulation.</param>
            /// <returns>The list of event subscribers</returns>
            internal static List<Subscriber> FindAll(List<IModel> allModels)
            {
                List<Subscriber> subscribers = new List<Subscriber>();
                foreach (IModel modelNode in allModels)
                {
                    foreach (MethodInfo method in modelNode.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy))
                    {
                        EventSubscribeAttribute subscriberAttribute = (EventSubscribeAttribute)ReflectionUtilities.GetAttribute(method, typeof(EventSubscribeAttribute), false);
                        if (subscriberAttribute != null)
                            subscribers.Add(new Subscriber(subscriberAttribute.ToString(), modelNode, method));
                    }
                }

                return subscribers;
            }

            /// <summary>Find all event subscribers in the specified models.</summary>
            /// <param name="name">The name of the event to look for</param>
            /// <param name="relativeTo">The model to use in scoping lookup</param>
            /// <param name="scope">Scoping rules</param>
            /// <returns>The list of event subscribers</returns>
            internal static List<Subscriber> FindAll(string name, IModel relativeTo, ScopingRules scope)
            {
                List<Subscriber> subscribers = new List<Subscriber>();
                foreach (IModel modelNode in scope.FindAll(relativeTo))
                {
                    foreach (MethodInfo method in modelNode.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy))
                    {
                        EventSubscribeAttribute subscriberAttribute = (EventSubscribeAttribute)ReflectionUtilities.GetAttribute(method, typeof(EventSubscribeAttribute), false);
                        if (subscriberAttribute != null && subscriberAttribute.ToString() == name)
                            subscribers.Add(new Subscriber(subscriberAttribute.ToString(), modelNode, method));
                    }
                }

                return subscribers;
            }

            /// <summary>Creates and returns a delegate for the event handler.</summary>
            /// <param name="handlerType">The corresponding event publisher event handler type.</param>
            /// <returns>The delegate. Never returns null.</returns>
            internal virtual Delegate CreateDelegate(Type handlerType)
            {
                return Delegate.CreateDelegate(handlerType, Model, methodInfo);
            }

            /// <summary>
            /// Call the event handler.
            /// </summary>
            /// <param name="args"></param>
            internal void Invoke(object[] args)
            {
                methodInfo.Invoke(Model, args);
            }

        }

        /// <summary>
        /// A wrapper around an event publisher EventInfo.
        /// </summary>
        public class Publisher
        {
            /// <summary>The model instance containing the event hander.</summary>
            public IModel Model { get; private set; }

            /// <summary>The reflection event info instance.</summary>
            public EventInfo EventInfo { get; private set; }

            /// <summary>Return the event name.</summary>
            public string Name {  get { return EventInfo.Name; } }

            internal void ConnectSubscriber(Subscriber subscriber)
            {
                // connect subscriber to the event.
                Delegate eventDelegate = subscriber.CreateDelegate(EventInfo.EventHandlerType);
                EventInfo.AddEventHandler(Model, eventDelegate);
            }

            internal void DisconnectAll()
            {
                FieldInfo eventAsField = Model.GetType().GetField(Name, BindingFlags.Instance | BindingFlags.NonPublic);
                if (eventAsField == null)
                {
                    //GetField will not find the EventHandler on a DerivedClass as the delegate is private
                    Type searchType = Model.GetType().BaseType;
                    while(eventAsField == null)
                    {
                        eventAsField = searchType?.GetField(Name, BindingFlags.Instance | BindingFlags.NonPublic);
                        searchType = searchType.BaseType;
                        if(searchType == null)
                        {
                            //not sure it's even possible to get to here, but it will make it easier to find itf it does
                            throw new Exception("Could not find " + Name + " in " + Model.GetType().Name + " using GetField");
                        }

                    }
                }
                eventAsField.SetValue(Model, null);
            }

            /// <summary>Find all event publishers in the specified models.</summary>
            /// <param name="models">The models to scan for event publishers</param>
            /// <returns>The list of event publishers</returns>
            public static List<Publisher> FindAll(List<IModel> models)
            {
                List<Publisher> publishers = new List<Publisher>();
                foreach (IModel modelNode in models)
                {
                    foreach (EventInfo eventInfo in modelNode.GetType().GetEvents(BindingFlags.Instance | BindingFlags.Public))
                        publishers.Add(new Publisher() { EventInfo = eventInfo, Model = modelNode });
                }

                return publishers;
            }
        }
    }
}
