# SharpPortfolioBackend

SharpPortfolioBackend is a .NET 10 Web API that serves as a backend for a portfolio application. It handles audio asset management and blog-style posts with full CRUD support, search, and audio conversion functionality.

## Features

-   **Audio Management**:
    -   Upload audio files with metadata.
    -   Search and filter audio by query, vibes, musical key, and BPM.
    -   Convert audio files to MP3 and WAV on the fly (via FFMpeg).
    -   Bulk deletion and zipped downloads for multiple audio files.
-   **Post Management**:
    -   Full CRUD (Create, Read, Update, Delete) functionality for blog posts.
    -   Search and filter posts by query and tags/vibes.
-   **Database**:
    -   Uses Oracle Database for storage.
    -   Automated migrations using DbUp.
    -   Performant data access with Dapper.
-   **Documentation**:
    -   Integrated Scalar API Reference for easy endpoint exploration and testing.
-   **Containerization**:
    -   Full Docker support with FFMpeg integration.

## Tech Stack

-   [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
-   [Oracle Database](https://www.oracle.com/database/technologies/xe-downloads.html)
-   [Dapper](https://github.com/DapperLib/Dapper)
-   [DbUp](https://dbup.github.io/)
-   [FFMpegCore](https://github.com/rosenbjerg/FFMpegCore)
-   [Scalar API Reference](https://scalar.com/docs/scalar/integrations/dotnet)
-   [DotNetEnv](https://github.com/tonerdo/dotnet-env)

## Prerequisites

-   .NET 10 SDK
-   Oracle XE Database instance
-   FFMpeg (installed and available in the system path or container)

## Setup and Configuration

1.  **Environment Variables**:
    Create a `.env` file in the project root and provide the following configuration:
    ```env
    ORACLE_USER=your_db_user
    ORACLE_PASSWORD=your_db_password
    ORACLE_HOST=your_db_host
    ORACLE_PORT=1521
    ORACLE_SERVICE=XEPDB1
    ```

2.  **Database Migrations**:
    The application uses DbUp to automatically apply migrations found in the `Database/Migrations` folder upon startup. Ensure the database connection details are correct in your `.env` file.

3.  **Running Locally**:
    ```bash
    dotnet run
    ```
    Once the application is running, the API reference will be available at `/scalar/v1` (in Development mode).

4.  **Running with Docker**:
    ```bash
    docker build -t sharpportfolio-backend .
    docker run -p 8080:8080 sharpportfolio-backend
    ```

## API Documentation

-   **Development**: Access the interactive Scalar API Reference at `/scalar/v1` to explore and test available endpoints for `Audio`, `Posts`, and `Auth`.
-   **OpenAPI**: The OpenAPI specification is available at `/openapi/v1.json`.

## Static Assets

Audio files are stored and managed in the `wwwroot/audio` directory. Ensure the application has the necessary write permissions for this folder.
