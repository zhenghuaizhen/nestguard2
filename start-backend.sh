#!/bin/bash
# 固定后端端口为 5000
cd /home/admin/.openclaw/workspace/nestguard2/AluminumApi
echo "Starting NestGuard backend on port 5000..."
dotnet run --urls="http://0.0.0.0:5000"