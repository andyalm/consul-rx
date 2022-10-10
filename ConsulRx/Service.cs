﻿using System.Collections.Generic;
using System.Linq;

namespace ConsulRx
{
    public class Service
    {
        public string Id { get; set; }
        public string Name { get; set; }

        public ServiceNode[] Nodes { get; set; }
        
        #region Autogenerated Equality by Jetbrains Rider

        protected bool Equals(Service other)
        {
            return Id == other.Id && Name == other.Name && Nodes.SequenceEqual(other.Nodes);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Service)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Id != null ? Id.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Nodes != null ? Nodes.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static bool operator ==(Service left, Service right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Service left, Service right)
        {
            return !Equals(left, right);
        }

        #endregion
    }

    public class ServiceNode
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public int Port { get; set; }
        public string[] Tags { get; set; }
        public IDictionary<string,string> Metadata { get; set; }

        #region Autegenerated Equality by Jetbrains Rider

        protected bool Equals(ServiceNode other)
        {
            return Name == other.Name
             && Address == other.Address
             && Port == other.Port
             && (Tags?.SequenceEqual(other.Tags) ?? other.Tags == null)
             && Equals(Metadata, other.Metadata);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ServiceNode)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Address != null ? Address.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Port;
                hashCode = (hashCode * 397) ^ (Tags != null ? Tags.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Metadata != null ? Metadata.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static bool operator ==(ServiceNode left, ServiceNode right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ServiceNode left, ServiceNode right)
        {
            return !Equals(left, right);
        }

        #endregion
    }
}