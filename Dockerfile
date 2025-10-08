# Use SDK to build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files first to leverage docker cache for restore
COPY ["GoElectrify.Api/GoElectrify.Api.csproj", "GoElectrify.Api/"]
COPY ["GoElectrify.BLL/GoElectrify.BLL.csproj", "GoElectrify.BLL/"]
COPY ["GoElectrify.DAL/GoElectrify.DAL.csproj", "GoElectrify.DAL/"]

# restore only the API project (will pull transitive refs)
RUN dotnet restore "GoElectrify.Api/GoElectrify.Api.csproj"

# copy rest and build
COPY . .
WORKDIR /src/GoElectrify.Api

# build & publish (no apphost to keep it smaller)
RUN dotnet publish "GoElectrify.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Recommended envs for containerized ASP.NET Core
ENV DOTNET_RUNNING_IN_CONTAINER=true \
    ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production

# copy publish output
COPY --from=build /app/publish .

EXPOSE 8080

# entrypoint uses your assembly name
ENTRYPOINT ["dotnet", "GoElectrify.Api.dll"]