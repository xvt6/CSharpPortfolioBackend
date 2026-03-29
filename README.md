# SharpPortfolio

SharpPortfolio is a portfolio application ecosystem built with .NET 10. This repository contains the backend API and supporting utilities.

## Projects in this Solution

The [SharpPortfolioBackend.sln](./SharpPortfolioBackend.sln) solution file includes the following projects:

-   **[SharpPortfolioBackend](./SharpPortfolioBackend)**: A .NET 10 Web API for managing audio assets and blog-style posts. It uses Oracle Database, Dapper, and DbUp for database management and Scalar for API documentation.
-   **[PwdHasher](./PwdHasher)**: A simple console utility to generate BCrypt password hashes for administrative credentials.

## Prerequisites

To run the projects in this solution, you will need:
-   [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
-   Oracle XE Database instance (for the Backend)
-   FFMpeg (for audio processing in the Backend)
-   Docker (optional, for containerized execution)

## Getting Started

Each project has its own detailed setup instructions:

1.  **Generate Admin Password**: Use [PwdHasher](./PwdHasher/README.md) to generate a secure hash for your administrative account.
2.  **Configure the Backend**: Follow the instructions in the [SharpPortfolioBackend README](./SharpPortfolioBackend/README.md) to set up your environment variables and database connection.
3.  **Run the API**: Start the [backend server](./SharpPortfolioBackend/README.md#running-locally) and access the Scalar API documentation.

## Tech Stack

The ecosystem leverages the following technologies:
-   **Languages**: C# 14.0
-   **Frameworks**: ASP.NET Core (.NET 10)
-   **Database**: Oracle Database, Dapper (ORM), DbUp (Migrations)
-   **Tools**: FFMpeg (Audio Processing), Scalar (API Reference), BCrypt.Net (Security)
