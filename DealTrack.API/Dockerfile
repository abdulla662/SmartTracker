FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY DealTrack.sln .
COPY DealTrack.API/DealTrack.API.csproj DealTrack.API/
COPY DealTrack.Application/DealTrack.Application.csproj DealTrack.Application/
COPY DealTrack.Domain/DealTrack.Domain.csproj DealTrack.Domain/
COPY DealTrack.Infrastructure/DealTrack.Infrastructure.csproj DealTrack.Infrastructure/

RUN dotnet restore

COPY . .

RUN dotnet publish DealTrack.API/DealTrack.API.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "DealTrack.API.dll"]