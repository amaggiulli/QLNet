QLNet
=====

QLNet C# library official repository.
QLNet is a financial library written in C# derived primarily from its C++ counterpart, Quantlib, 
which has been used as a base reference for modelling various financial instruments.
QLNet also contains new developments on the bond market like MBS, Amortized Cost, PSA Curve and others.

## Library Overview Presentation

📊 **[QLNet_Library_Overview.pptx](QLNet_Library_Overview.pptx)** - A comprehensive PowerPoint presentation providing a high-level overview of QLNet's capabilities and utility code available to users.

The presentation covers:
- Financial instruments (150+ instruments: Options, Bonds, Swaps, Derivatives)
- Pricing engines (70+ engines: Analytical, Monte Carlo, Tree-based, Finite Differences)
- Mathematical infrastructure (100+ utilities: Interpolation, Optimization, Solvers, Distributions)
- Interest rate & volatility models (Hull-White, Heston, LIBOR Market Model)
- Term structures and curve construction
- Utility code and helper functions
- Special features (MBS, Callable Bonds, Credit Products)

See [PRESENTATION_README.md](PRESENTATION_README.md) for detailed slide-by-slide description.

[![Build status](https://ci.appveyor.com/api/projects/status/nn0a2mw6qu8mg481?svg=true)](https://ci.appveyor.com/project/amaggiulli/qlnet-p0t4r)
[![NuGet](https://img.shields.io/nuget/vpre/QLNet?style=flat-square)](https://www.nuget.org/packages/QLNet)
[![Donate](https://img.shields.io/badge/Donate-PayPal-green.svg)](https://www.paypal.com/cgi-bin/webscr?item_name=Donation+to+QLNet&cmd=_donations&business=a.maggiulli%40gmail.com)

[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=QLNet&metric=alert_status)](https://sonarcloud.io/dashboard?id=QLNet)
[![Bugs](https://sonarcloud.io/api/project_badges/measure?project=QLNet&metric=bugs)](https://sonarcloud.io/dashboard?id=QLNet)
[![Vulnerabilities](https://sonarcloud.io/api/project_badges/measure?project=QLNet&metric=vulnerabilities)](https://sonarcloud.io/dashboard?id=QLNet)
[![Code Smells](https://sonarcloud.io/api/project_badges/measure?project=QLNet&metric=code_smells)](https://sonarcloud.io/dashboard?id=QLNet)
[![Duplicated Lines (%)](https://sonarcloud.io/api/project_badges/measure?project=QLNet&metric=duplicated_lines_density)](https://sonarcloud.io/dashboard?id=QLNet)
[![Lines of Code](https://sonarcloud.io/api/project_badges/measure?project=QLNet&metric=ncloc)](https://sonarcloud.io/dashboard?id=QLNet)
[![Maintainability Rating](https://sonarcloud.io/api/project_badges/measure?project=QLNet&metric=sqale_rating)](https://sonarcloud.io/dashboard?id=QLNet)
[![Reliability Rating](https://sonarcloud.io/api/project_badges/measure?project=QLNet&metric=reliability_rating)](https://sonarcloud.io/dashboard?id=QLNet)
[![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=QLNet&metric=security_rating)](https://sonarcloud.io/dashboard?id=QLNet)
[![Technical Debt](https://sonarcloud.io/api/project_badges/measure?project=QLNet&metric=sqale_index)](https://sonarcloud.io/dashboard?id=QLNet)


## Development workflow 

###### QLNet use git flow workflow.

Instead of a single master branch, this workflow uses two branches to record the history of the project. 
The *master* branch stores the official release history, and the *develop* branch serves as an integration branch for features.
The *develop* branch will also contain the complete history of the project.

###### Features 

To contribute features, you should clone the repository, create a tracking branch for develop and create the feature:

```
git clone https://github.com/amaggiulli/qlnet.git
git checkout -b develop origin/develop
git checkout -b some-feature develop
```

When the feature is ready, you can make a pull request to merge that feature into *develop*. 
Note that features will never be merged directly into *master*.

###### Releases

When a release is ready, we fork a release branch from *develop*. Creating this branch starts the next release cycle, 
so no new features can be added after this point; only bug fixes, documentation generation, and other release-oriented tasks go in this branch. 
Once it's ready to ship, the release gets merged into *master* and tagged with a version number. 

###### HotFix

Maintenance or “hotfix” branches are used to quickly patch production releases. This is the only branch that fork directly off of *master*. 
As soon as the fix is complete, it will be merged into both *master* and *develop*, and *master* will be tagged with an updated version number.

## Acknowledgements

Thanks to all Quantlib creators and contributors.
Thanks to all QLNet contributors.
 
