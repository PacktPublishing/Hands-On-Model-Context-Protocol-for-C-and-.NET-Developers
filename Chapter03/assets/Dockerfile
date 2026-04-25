FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY TravelBooking.sln .
COPY src/FlightsServer/*.csproj src/FlightsServer/
COPY src/TravelBooking.Contracts/*.csproj src/TravelBooking.Contracts/
RUN dotnet restore src/FlightsServer/FlightsServer.csproj
COPY src/ src/
RUN dotnet publish src/FlightsServer/FlightsServer.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FlightsServer.dll"]
