# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and csproj files for restoring dependencies
COPY Inventory-Management-System.slnx ./
COPY Inventory-Management-System.csproj ./
RUN dotnet restore Inventory-Management-System.csproj


# Copy the rest of the code and build
COPY . .
WORKDIR /src
RUN dotnet publish Inventory-Management-System.csproj -c Release -o /app/out


# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/out .

EXPOSE 8080
ENTRYPOINT ["dotnet", "Inventory-Management-System.dll"]