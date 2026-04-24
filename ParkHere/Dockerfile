FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5130 
ENV ASPNETCORE_URLS=http://+:5130

# Install tzdata to ensure correct timezone behavior for DateTime.Now and Npgsql
RUN apt-get update && apt-get install -y tzdata && rm -rf /var/lib/apt/lists/*
ENV TZ=Europe/Sarajevo

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .

FROM build AS publish
RUN dotnet publish "ParkHere.WebAPI/ParkHere.WebAPI.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
COPY ParkHere.WebAPI/Assets ./Assets

ENTRYPOINT ["dotnet", "ParkHere.WebAPI.dll"]