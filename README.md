# NOTE - This branch is used as a demo project for [Geta.EPi.Commerce.UI.Facets](https://github.com/Geta/Epi.Commerce.UI)

<div align="center">
  <a href="https://github.com/episerver/Foundation">
    <img src="https://www.optimizely.com/globalassets/02.-global-images/navigation/optimizely_logo_navigation.svg" alt="Optimizely Foundation" width="400">
  </a>
  <h1>Geta Foundation Core</h1>
  <p>A modular development environment for Optimizely CMS projects</p>

[![.NET 9.0](https://img.shields.io/badge/.NET-9.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/9.0)
[![Docker](https://img.shields.io/badge/Docker-✓-2496ED)](https://www.docker.com)
[![Aspire Supported](https://img.shields.io/badge/.NET_Aspire-✓-512BD4)](https://learn.microsoft.com/en-us/dotnet/aspire/)

</div>

---

## 🚀 Overview

This sandbox is a fork of [Optimizely Foundation](https://github.com/episerver/Foundation) designed to serve as a submodule for Geta's open-source packages.

Featuring integrated [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/) support for streamlined orchestration and enhanced developer productivity.

---

## 🌟 Key Features/Concept

- **Foundation Project**
    - 📦 Modular architecture covering CMS, Commerce, Personalization, Search, and more
- **Aspire AppHost**
    - 🐳 Docker-based environment
    - 📊 Centralized dashboard monitoring

### Concept

Instead of merging all existing open-source packages into the Optimizely Foundation project, 
it is possible to use this project’s codebase as a submodule for an open-source package and run a web project with a specific configuration.

This approach allows us to:
- Follow the single-responsibility principle.
- Reuse foundation code in open-source repositories by using a submodule.
- Keep package-specific functionality within its respective project.

---

## 🛠️ Prerequisites
| Aspire project                                                         | .Net project (Windows)                                                        | .Net project (Linux)                                                   | .Net project (MacOs)                                                   |
|------------------------------------------------------------------------|-------------------------------------------------------------------------------|------------------------------------------------------------------------|------------------------------------------------------------------------|
| [.NET 9.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) | [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) | [Docker Desktop](https://docs.docker.com/desktop/setup/install/linux/) | [Docker Desktop](https://docs.docker.com/desktop/setup/install/linux/) |
| [Docker Desktop](https://www.docker.com/products/docker-desktop)       | [Docker Desktop](https://www.docker.com/products/docker-desktop)              | [.NET 6.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) | [.NET 6.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) |
| [Node.js](https://nodejs.org/en/download/)                             | [.NET 6.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)        | [.NET 9.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) | [.NET 9.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) |
|                                                                        | [.NET 9.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)        | [Node.js](https://nodejs.org/en/download/)                             | [Node.js](https://nodejs.org/en/download/)                             |
|                                                                        | [Node.js](https://nodejs.org/en/download/)                                    |                                                                        |                                                                        |

---

## 🔑 Default Credentials
### Admin Access
<code>admin@example.com</code> / <code>Episerver123!</code>

## ❓ FAQ

- [How to Use as a submodule?](https://github.com/Geta/geta-packages-foundation-sample#-how-to-use-as-a-submodule)
