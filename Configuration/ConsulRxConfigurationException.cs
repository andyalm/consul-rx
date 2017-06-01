using System;

namespace ConsulRx.Configuration
{
    public class ConsulRxConfigurationException : Exception
    {
        public ConsulRxConfigurationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}