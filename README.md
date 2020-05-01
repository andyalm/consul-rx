# ConsulRx
![License](https://img.shields.io/badge/license-MIT-green)
[![Nuget](https://img.shields.io/nuget/v/ConsulRx.Configuration)](https://www.nuget.org/packages/ConsulRx.Configuration)
[![CI/CD](https://gitlab.com/andyalm/consul-rx/badges/master/pipeline.svg)](https://gitlab.com/andyalm/consul-rx/pipelines)

A set of libraries for interacting with Hashicorp's Consul from .NET. These libraries leverage the [Reactive Extensions](https://github.com/Reactive-Extensions/Rx.NET) to allow you to consume Consul values as a stream so your application can react to changes very quickly.

## ConsulRx.Configuration
Provides the ability to add config values into the `Microsoft.Extensions.Configuration` framework from Consul's KV Store and Service Catalog.

### Getting started

In your `Startup.cs` (or equivilent bootstrapping file where you build your configuration):

```c#
Configuration = new ConfigurationBuilder()
    .AddConsul(c =>
        c.Endpoint("http://myconsulserver:8500") //defaults to localhost if you omit
         .MapHttpService("mywidgetservice", "serviceEndpoints:widget") //maps the address of the consul service mywidgetservice to the serviceEndpoints:widget config key in IConfiguration
         .MapKeyPrefix("apps/myapp", "consul") //recursively maps all keys underneath apps/myapp to live in equivilent structure under the consul section in IConfiguration
         .MapKey("shared/key1", "key1")
    );
```

## ConsulRx.Templating
Similar to Hashicorp's [consul-template](https://github.com/hashicorp/consul-template) but in .NET using Razor templates. Why? consul-template is a popular, capable and reliable solution for generating templates from consul values.
Why write an alternative in .NET? After using consul-template in several production projects, I have been frustrated by the lack of flexibility of the go templates.
In my experience, when trying to do anything somewhat complex, the templates become very hard to read and understand. This project is an experiment to see if I can get similar
functionality as consul-template in .NET with a much more flexible templating language of Razor that is easier to write and understand.