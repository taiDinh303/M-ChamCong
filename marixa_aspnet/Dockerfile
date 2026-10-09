# ============================================================
# M.API Dockerfile - Build & publish .NET 8 API
#
# Render Web Service (Runtime: Docker) sẽ dùng file này.
# Render sẽ:
#   1. Build:   docker build -f Dockerfile .
#   2. Run:     docker run (auto-detect port via EXPOSE/PORT env)
#   3. HTTPS:   Load balancer Render xử lý; container chạy HTTP trên $PORT
# ============================================================

# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# Copy csproj trước để tận dụng Docker layer cache (deps không đổi -> không restore lại)
COPY M.API/*.csproj M.API/
COPY M.Contract.Repositories/*.csproj M.Contract.Repositories/
COPY M.Contract.Serivces/*.csproj M.Contract.Serivces/
COPY M.Core/*.csproj M.Core/
COPY M.ModelViews/*.csproj M.ModelViews/
COPY M.Repositories/*.csproj M.Repositories/
COPY M.Services/*.csproj M.Services/

# Restore tất cả packages (chạy 1 lần, cache cho build tiếp theo)
RUN dotnet restore M.API/M.API.csproj

# Copy source + build publish (framework-dependent, nhỏ hơn self-contained)
COPY M.API/ M.API/
COPY M.Contract.Repositories/ M.Contract.Repositories/
COPY M.Contract.Serivces/ M.Contract.Serivces/
COPY M.Core/ M.Core/
COPY M.ModelViews/ M.ModelViews/
COPY M.Repositories/ M.Repositories/
COPY M.Services/ M.Services/

# Publish: framework-dependent (render runtime có .NET 8) + ASPNETCORE_ENVIRONMENT=Production
RUN dotnet publish M.API/M.API.csproj \
    -c Release \
    -o /publish \
    /p:UseAppHost=false \
    -p:PublishTrimmed=false

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copy artifacts build
COPY --from=build /publish ./

# Render inject env var PORT lúc runtime (thường 10000, khớp EXPOSE).
# Lệnh ENV không mở rộng shell var khi build, nên dùng shell-form CMD để
# $PORT được resolve THỰC SỰ khi container khởi động (Kestrel listen all-interfaces).
ENV ASPNETCORE_ENVIRONMENT=Production

# Render Web Service: port phải là $PORT (Render đặt mặc định 10000)
EXPOSE 10000

# Tạo wwwroot/uploads nếu chưa có (PhotoStore sẽ tạo tự động, đây chỉ là fallback)
RUN mkdir -p /app/wwwroot/uploads

# Chạy .NET (framework-dependent). + = listen all interfaces (0.0.0.0) để
# Render load balancer gọi vào được. Default 10000 nếu PORT chưa set.
CMD /bin/sh -c "export ASPNETCORE_URLS=http://+:${PORT:-10000}; exec dotnet M.API.dll"
