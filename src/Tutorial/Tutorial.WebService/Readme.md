---
id: tutorial-web-service
title: "Tutorial: Web Service"
subject: "Exposing HumanOS gateway data via a custom REST endpoint with a C# payload script"
keywords: [HumanOS, WebService, REST, HTTP, payload script, GET, POST, PATCH, DELETE, API]
---

# Tutorial: Web Service

Shows how to expose the HumanOS gateway as a **custom REST API** using the `HumanOS.PeSeL.WebService` plugin and a C# payload script. The `PayLoadScript.cs` file implements handler methods for each HTTP verb — GET, POST, PATCH, and DELETE — giving full control over request parsing and response generation.

No physical hardware or device connector is required; the tutorial focuses entirely on the web service scripting pattern.

## Step by Step Guide
In [Web Service Step by Step Guide](https://doc.cybertech.swiss/runtime/Tutorials/Tutorial8/01_Example.md) you can find a step-by-step instruction of this tutorial.

## Architecture

```text
HTTP Client  (browser, curl, Postman, etc.)
        │  HTTP request  (GET / POST / PATCH / DELETE)
        ▼
HumanOS.PeSeL.WebService  (configured port)
        │  dispatches by HTTP method
        ▼
PayLoadScript.cs
        │  handleGet    — read data from node space, return JSON/text
        │  handlePost   — receive and process incoming payload
        │  handlePatch  — partial update
        │  handleDelete — remove resource
        │  return HTTP status code + response body
        ▼
HTTP Client  receives response
```

## Prerequisites

- HumanOS IoT Designer ≥ 2.10
- No additional hardware required
- A REST client ([Postman](https://www.postman.com)) to test endpoints

## See Also

- [WebService Plugin Reference](https://doc.cybertech.swiss/runtime/Manuals/HumanOS.PeSeL.WebService/)
- [C# Scripting Guide](https://doc.cybertech.swiss/runtime/Development/)
- [HumanOS Tutorials](https://doc.cybertech.swiss/runtime/Tutorials/)
