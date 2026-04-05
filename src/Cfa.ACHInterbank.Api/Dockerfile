# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# Depending on the operating system of the host machines(s) that will build or run the containers, the image specified in the FROM statement may need to be changed.
# For more information, please see https://aka.ms/containercompat

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0.5 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0.201 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Cfa.ACHInterbank.Api/Cfa.ACHInterbank.Api.csproj", "src/Cfa.ACHInterbank.Api/"]
COPY ["src/Cfa.ACHInterbank.Application/Cfa.ACHInterbank.Application.csproj", "src/Cfa.ACHInterbank.Application/"]
COPY ["src/Cfa.ACHInterbank.Domain/Cfa.ACHInterbank.Domain.csproj", "src/Cfa.ACHInterbank.Domain/"]
COPY ["src/Cfa.ACHInterbank.External/Cfa.ACHInterbank.External.csproj", "src/Cfa.ACHInterbank.External/"]
COPY ["src/Cfa.ACHInterbank.Persistence/Cfa.ACHInterbank.Persistence.csproj", "src/Cfa.ACHInterbank.Persistence/"]
RUN dotnet restore "./src/Cfa.ACHInterbank.Api/Cfa.ACHInterbank.Api.csproj"
COPY . .
WORKDIR "/src/src/Cfa.ACHInterbank.Api"
RUN dotnet build "./Cfa.ACHInterbank.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Cfa.ACHInterbank.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY ["src/Cfa.ACHInterbank.Api/entrypoint.sh", "/app/entrypoint.sh"]
RUN sed -i 's/\r$//' /app/entrypoint.sh && chmod +x /app/entrypoint.sh
ENTRYPOINT ["/app/entrypoint.sh"]
