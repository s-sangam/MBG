#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/runtime:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
# install System.Drawing native dependencies
RUN apt-get update \
    && apt-get install -y --allow-unauthenticated \
        libc6-dev \
        libgdiplus \
        libx11-dev \
     && rm -rf /var/lib/apt/lists/*


FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["MBImageBuilder/MBImageBuilder.csproj", "MBImageBuilder/"]
RUN dotnet restore "MBImageBuilder/MBImageBuilder.csproj"
COPY . .
WORKDIR "/src/MBImageBuilder"
RUN dotnet build "MBImageBuilder.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MBImageBuilder.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MBImageBuilder.dll"]
