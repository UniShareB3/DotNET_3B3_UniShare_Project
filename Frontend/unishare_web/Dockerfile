# Stage 1: Build the Flutter Web App
FROM ubuntu:20.04 AS build-env

# Install dependencies for Flutter
RUN apt-get update && \
    apt-get install -y curl git unzip

# Install Flutter SDK
RUN git clone https://github.com/flutter/flutter.git /usr/local/flutter
ENV PATH="/usr/local/flutter/bin:/usr/local/flutter/bin/cache/dart-sdk/bin:${PATH}"

# Run flutter doctor
RUN flutter doctor

# Copy your app source code (Flutter files)
COPY . /app
WORKDIR /app

# Build the web application
RUN flutter build web --release

# Stage 2: Create the Node.js Run Image
FROM node:18-alpine

WORKDIR /app

# 1. Copy package files from the ROOT (not server folder)
COPY package*.json ./
RUN npm install --production

# 2. Copy the server file from the ROOT
COPY server.js ./

# 3. Copy the compiled Flutter web assets from Stage 1
# We put them in a folder named 'public' inside the container
COPY --from=build-env /app/build/web ./public

EXPOSE 3000
CMD ["node", "server.js"]