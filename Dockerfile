# Giai đoạn 1: Build ứng dụng
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy file project và restore các gói NuGet
COPY ["KhoaNVCB_API/KhoaNVCB_API.csproj", "KhoaNVCB_API/"]
RUN dotnet restore "KhoaNVCB_API/KhoaNVCB_API.csproj"

# Copy toàn bộ mã nguồn và build
COPY . .
WORKDIR "/src/KhoaNVCB_API"
RUN dotnet build "KhoaNVCB_API.csproj" -c Release -o /app/build

# Giai đoạn 2: Publish (Đóng gói file thực thi)
FROM build AS publish
RUN dotnet publish "KhoaNVCB_API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Giai đoạn 3: Chạy ứng dụng (Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Render yêu cầu lắng nghe ở port 80 hoặc 10000
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

ENTRYPOINT ["dotnet", "KhoaNVCB_API.dll"]