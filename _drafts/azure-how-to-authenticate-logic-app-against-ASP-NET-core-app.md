---
layout: article
title: Azure: How to authenticate Logic App against ASP.NET core app
categories: [azure]
tags: [logic apps, asp.net, .net core, azure]
---

## The use case

We had an asp.net core web application, hosted in Azure. That application had some basic UI and REST API for some business logic. So far nothing special or extraordinary. We use Azure Active Directory to authenticate all requests in that application.

Besides using the UI, we needed to create some recurring thing, that calls our API and does some stuff with the data, that the API provides.
What better solution than [Logic Apps](https://azure.microsoft.com/en-us/services/logic-apps/).

## The idea

What we wanted to do, is to create a `logic app`, which will have daily recurrence. It will call our API and do its thing.
As mentioned above, our API uses Azure AD for authentication. Which is pretty straightforward, but we wanted to use [Managed Identity](https://docs.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/overview) for authenticating the `logic app` as a client to the API.

## The problem

Turns out that using both authentications in a single web application is not so out-of-the-box approach, as one might think. Also the way of achieving it was hidden deeply in a Microsoft article. But let me guide you towards the solution.

## The solution

As I said, we needed to have two types of authentication here. First is Azure AD, so you can login via the web UI and interact with the service and its website. The second one is a managed identity, which will be used for authentication from other cloud services in our tenant.

In the begning we had just the normal boilerplate code for Azure AD authentication in `Startup.cs`:

```c#
services.AddAuthentication(AzureADDefaults.AuthenticationScheme)
        .AddAzureAD(options => Configuration.Bind("AzureAd", options));
```

This worked fine for the traditional login. But when I tried logging in with the
