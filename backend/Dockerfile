# build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only the project files first, so the restore layer is cached
# until a package reference actually changes.
COPY src/SchoolAdmission.Domain/*.csproj         src/SchoolAdmission.Domain/
COPY src/SchoolAdmission.Application/*.csproj    src/SchoolAdmission.Application/
COPY src/SchoolAdmission.Infrastructure/*.csproj src/SchoolAdmission.Infrastructure/
COPY src/SchoolAdmission.Api/*.csproj            src/SchoolAdmission.Api/
RUN dotnet restore src/SchoolAdmission.Api/SchoolAdmission.Api.csproj

COPY . .
RUN dotnet publish src/SchoolAdmission.Api/SchoolAdmission.Api.csproj \
    -c Release -o /app/publish --no-restore

# runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SchoolAdmission.Api.dll"]