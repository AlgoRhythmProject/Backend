FROM mcr.microsoft.com/dotnet/sdk:8.0

WORKDIR /app

# Create template with .csproj
RUN mkdir -p /app/template && \
    echo '<Project Sdk="Microsoft.NET.Sdk">\n\
  <PropertyGroup>\n\
    <OutputType>Exe</OutputType>\n\
    <TargetFramework>net8.0</TargetFramework>\n\
    <ImplicitUsings>enable</ImplicitUsings>\n\
  </PropertyGroup>\n\
  <ItemGroup>\n\
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />\n\
  </ItemGroup>\n\
</Project>' > /app/template/Project.csproj

# Pre-restore NuGet packages - HUGE time saver!
RUN cd /app/template && dotnet restore

# Pre-create obj directory structure
RUN cd /app/template && dotnet build-server shutdown

ENV DOTNET_CLI_HOME=/app
ENV HOME=/app
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1

CMD ["sleep", "infinity"]