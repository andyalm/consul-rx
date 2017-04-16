# consul-template.net
A port of consul-template to .NET Core (uses Razor templates). Note that this is still a work in progress and not yet fully functional.

## Why?
[consul-template](https://github.com/hashicorp/consul-template) is a popular, capable and reliable solution for generating templates from consul values. Why write an alternative in .NET? After using consul-template in several production projects, I have been frustrated by the lack of flexibility of the go templates. When trying to do anything somewhat complex, the templates become very hard to read and understand. This project is an experiment to see if I can get similar functionality as consul-template in .NET with a much more flexible templating language of Razor that is easier to write and understand.