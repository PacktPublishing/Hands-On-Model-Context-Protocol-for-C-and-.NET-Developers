#!/usr/bin/env bash
set -euo pipefail

echo "Creating TravelBooking solution structure..."

mkdir -p TravelBooking
cd TravelBooking

dotnet new sln -n TravelBooking

dotnet new web -n FlightsServer -o src/FlightsServer
dotnet new web -n HotelsServer -o src/HotelsServer
dotnet new web -n PaymentsServer -o src/PaymentsServer
dotnet new web -n ItineraryServer -o src/ItineraryServer
dotnet new console -n TravelBookingClient -o src/TravelBookingClient
dotnet new classlib -n TravelBooking.Contracts -o src/TravelBooking.Contracts
dotnet new xunit -n TravelBooking.Tests -o tests/TravelBooking.Tests

echo "Adding projects to solution..."

dotnet sln add src/FlightsServer/FlightsServer.csproj
dotnet sln add src/HotelsServer/HotelsServer.csproj
dotnet sln add src/PaymentsServer/PaymentsServer.csproj
dotnet sln add src/ItineraryServer/ItineraryServer.csproj
dotnet sln add src/TravelBookingClient/TravelBookingClient.csproj
dotnet sln add src/TravelBooking.Contracts/TravelBooking.Contracts.csproj
dotnet sln add tests/TravelBooking.Tests/TravelBooking.Tests.csproj

echo "Installing MCP SDK packages..."

for project in FlightsServer HotelsServer PaymentsServer ItineraryServer; do
  dotnet add "src/$project" package ModelContextProtocol
  dotnet add "src/$project" package ModelContextProtocol.AspNetCore
  dotnet add "src/$project" package Microsoft.Extensions.Logging.Console
done

dotnet add src/TravelBookingClient package ModelContextProtocol
dotnet add tests/TravelBooking.Tests package ModelContextProtocol

echo "Listing solution contents..."
dotnet sln list

echo "Building solution..."
dotnet build

echo "TravelBooking setup complete."