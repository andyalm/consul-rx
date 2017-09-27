using System;
using System.Collections.Generic;
using System.Linq;

namespace ConsulRx.Configuration
{
    public static class NodeSelectors
    {
        private static readonly Random _random = new Random();

        public static Func<ServiceNode[], ServiceNode> RandomOne { get; } = nodes =>
        {
            if (!nodes.Any())
                return null;

            var randomNodeIndex = _random.Next(nodes.Length - 1);
            return nodes[randomNodeIndex];
        };
        
        public static Func<ServiceNode[], ServiceNode> First { get; } = nodes =>
        {
            var firstNode = nodes.FirstOrDefault();
            if (firstNode == null)
                throw new NodeSelectionException("Could not find any registered nodes");

            return firstNode;
        };

        public static Func<ServiceNode[], IEnumerable<ServiceNode>> Tag(string value)
        {
             return nodes => nodes.Where(n => n.Tags.Contains(value, StringComparer.OrdinalIgnoreCase));   
        }

        public static Func<ServiceNode[], IEnumerable<ServiceNode>> All { get; } = nodes => nodes;
    }
}