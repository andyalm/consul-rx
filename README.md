# ConsulRx
A set of libraries for interacting with Hashicorp's Consul from .NET.

## ConsulRx.Configuration
Provides the ability to add config values into the `Microsoft.Extensions.Configuration` framework from Consul's KV Store and Service Catalog.

## ConsulRx.Templating
Similar to Hashicorp's [consul-template](https://github.com/hashicorp/consul-template) but in .NET using Razor templates. Why? consul-template is a popular, capable and reliable solution for generating templates from consul values.
Why write an alternative in .NET? After using consul-template in several production projects, I have been frustrated by the lack of flexibility of the go templates.
When trying to do anything somewhat complex, the templates become very hard to read and understand. This project is an experiment to see if I can get similar
functionality as consul-template in .NET with a much more flexible templating language of Razor that is easier to write and understand.