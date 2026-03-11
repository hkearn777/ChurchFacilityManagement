# Use the official .NET SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy the project file and restore dependencies
COPY ChurchFacilityManagement.csproj .
RUN dotnet restore

# Copy the rest of the files and build
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Use the runtime image for the final container
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .

# Cloud Run will set the PORT environment variable
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ChurchFacilityManagement.dll"]
