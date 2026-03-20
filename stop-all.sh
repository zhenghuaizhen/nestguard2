#!/bin/bash

echo "Stopping NestGuard services..."

# Kill frontend processes on port 3000
FRONTEND_PID=$(lsof -ti:3000)
if [ ! -z "$FRONTEND_PID" ]; then
    echo "Killing frontend (PID: $FRONTEND_PID)..."
    kill -9 $FRONTEND_PID
fi

# Kill backend processes on port 5000  
BACKEND_PID=$(lsof -ti:5000)
if [ ! -z "$BACKEND_PID" ]; then
    echo "Killing backend (PID: $BACKEND_PID)..."
    kill -9 $BACKEND_PID
fi

echo "All services stopped."