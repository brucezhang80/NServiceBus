namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Collections.Generic;
    using Customization;

    /// <summary>
    /// Metadata on events and their publishers.
    /// </summary>
    public class PublisherMetadata
    {
        public IEnumerable<PublisherDetails> Publishers => publisherDetails.Values;

        public void RegisterPublisherFor<T>(string endpointName)
        {
            PublisherDetails publisher;
            if (!publisherDetails.TryGetValue(endpointName, out publisher))
            {
                publisher = new PublisherDetails(endpointName);

                publisherDetails[endpointName] = publisher;
            }

            publisher.RegisterOwnedEvent<T>();
        }

        public void RegisterPublisherFor<T>(Type endpointType)
        {
            RegisterPublisherFor<T>(Conventions.EndpointNamingConvention(endpointType));
        }

        Dictionary<string, PublisherDetails> publisherDetails = new Dictionary<string, PublisherDetails>();

        public class PublisherDetails
        {
            public PublisherDetails(string publisherName)
            {
                PublisherName = publisherName;
            }

            public List<Type> Events { get; } = new List<Type>();

            public string PublisherName { get; private set; }

            public void RegisterOwnedEvent<T>()
            {
                var eventType = typeof(T);

                if (Events.Contains(eventType))
                {
                    return;
                }

                Events.Add(eventType);
            }
        }
    }
}