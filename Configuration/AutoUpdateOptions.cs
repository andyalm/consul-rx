using System;

namespace ConsulRx.Configuration
{
    /// <summary>
    /// Options for controlling auto-update behavior.
    /// </summary>
    public class AutoUpdateOptions
    {
        /// <summary>
        /// The amount of time to wait before retrying after receiving an error.
        /// </summary>
        /// <returns><see cref="Defaults.ErrorRetryInterval" /></returns>
        public TimeSpan ErrorRetryInterval { get; set; } = Defaults.ErrorRetryInterval;

        /// <summary>
        /// The maximum amount of time between updates.
        /// </summary>
        /// <returns><see cref="Defaults.UpdateMaxInterval" /></returns>
        public TimeSpan UpdateMaxInterval { get; set; } = Defaults.UpdateMaxInterval;
    }
}