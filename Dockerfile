# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY M.BE.sln ./
COPY M.API/ M.API/
COPY M.Core/ M.Core/
COPY M.Contract.Repositories/ M.Contract.Repositories/
COPY M.Contract.Serivces/ M.Contract.Serivces/
COPY M.ModelViews/ M.ModelViews/
COPY M.Repositories/ M.Repositories/
COPY M.Services/ M.Services/

RUN dotnet publish M.API/M.API.csproj -c Release -o /app

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .

# Ảnh chấm công được PhotoStore tạo tại /app/wwwroot/uploads
# (PhotoStore "leo lên" tìm .csproj; trong publish flat -> dừng tại /app)
RUN mkdir -p wwwroot/uploads/attendance/checkin wwwroot/uploads/attendance/checkout

# Render inject biến môi trường PORT (default 10000). Lắng nghe đúng port đó.
CMD ["sh", "-c", "exec dotnet M.API.dll --urls http://0.0.0.0:${PORT:-10000}"]
