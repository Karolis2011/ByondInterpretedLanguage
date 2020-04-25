# SDK for building.
FROM mcr.microsoft.com/dotnet/core/sdk:2.1 AS build-env

WORKDIR /app

COPY CSharp/CSharp.csproj ./
RUN dotnet restore

COPY ./CSharp ./
RUN dotnet publish -c Release -o out

# Runtime for running.
FROM mcr.microsoft.com/dotnet/core/runtime:2.1

WORKDIR /app

COPY --from=build-env /app/out .

EXPOSE 1945

ENTRYPOINT ["dotnet", "CSharp.dll"]