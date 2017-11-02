---
title: "Quick-Start Guide"
permalink: /docs/quick-start-guide/
excerpt: "How to install and setup QLNet library."
modified: 2017-11-02T10:01:43-04:00
redirect_from:
  - /theme-setup/
---

QLNet is a financial library developed in c# , easy to setup and use. It is build for .NET Framework 4.0 and upper , .NET Standard 2.0 and .NET Core 1.1 .

## Installing QLNet

You can install QLNet  in several ways : from source code , from latest .NET Framework build , from Nuget package manager.

**ProTip:** The fastest way to install the library from Visual Studio is from Package manager shell , use : Install-Package QLNet.
{: .notice--info}

### Package Manager Method

To get the latest stable version of QLNet with `Package managers`:

Nuget Console :
```ruby
PM> Install-Package QLNet 
```
.NET CLI :
```ruby
> dotnet add package QLNet
```

Paket CLI :
```ruby
> paket add QLNet
```


### GitHub Latest Release Method

Go to the <small><a href="https://github.com/amaggiulli/QLNet/releases/latest">Latest release</a></small> page on GitHub,
there you can find a build dll ready to use or source code ( zip or tar.gz ).

**Note:** The lastest release dll is  build on .NET Framework , if you need another framework read below for more details on how build from source.
{: .notice--warning}

<figure>
  <img src="{{ '/assets/images/latest_release.jpg' | absolute_url }}" alt="QLNet latest release">
</figure>

### Source code Method

Fork the [QLNet](https://github.com/amaggiulli/QLNet/fork), clone locally and open QLNet.sln solution file.

**Solution versions:** QLNet.sln is always updated at the latest version of Visual Studio , actually VS 2017.
If you are on older version use QLNetOld.sln. 
{: .notice--danger}

---

