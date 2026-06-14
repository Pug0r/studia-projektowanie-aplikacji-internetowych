FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["ScentMarket.Client/ScentMarket.Client.csproj", "ScentMarket.Client/"]
COPY ["ScentMarket.Shared/ScentMarket.Shared.csproj", "ScentMarket.Shared/"]
RUN dotnet restore "ScentMarket.Client/ScentMarket.Client.csproj"

COPY . .
RUN dotnet publish "ScentMarket.Client/ScentMarket.Client.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM nginx:1.27-alpine AS runtime
COPY ScentMarket.Client/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]

