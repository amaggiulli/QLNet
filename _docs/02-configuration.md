---
title: "Configuration"
permalink: /docs/configuration/
excerpt: "Configuring the QLNet build"
modified: 2017-11-02T10:44:22-05:00
---

There are several pre-processor directive that can be used for customizing the library :

| Name                           | Description       | Default|
| ----                           | ----------------  ||
| **QL_NEGATIVE_RATES**          | Define this if negative rates should be allowed.|enabled|
| **QL_USE_INDEXED_COUPON**      | Define this to use indexed coupons instead of par coupons in floating legs.|disabled|
| **QL_EXTRA_SAFETY_CHECKS**     | Define this if extra safety checks should be performed. This can degrade performance.|disabled|


