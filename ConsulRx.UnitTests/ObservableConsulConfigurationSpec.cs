using AwesomeAssertions;
using Xunit;

namespace ConsulRx.UnitTests
{
    public class ObservableConsulConfigurationSpec
    {
        [Theory]
        [InlineData("myhost:8500", "http://myhost:8500")]
        [InlineData("consul-us-west-2.internal.example.cloud:8500", "http://consul-us-west-2.internal.example.cloud:8500")]
        [InlineData("localhost:8500", "http://localhost:8500")]
        [InlineData("http://myhost:8500", "http://myhost:8500")]
        [InlineData("https://myhost:8500", "https://myhost:8500")]
        public void EndpointNormalizesToHttpUri(string input, string expected)
        {
            var config = new ObservableConsulConfiguration { Endpoint = input };
            config.Endpoint.Should().Be(expected);
        }
    }
}
