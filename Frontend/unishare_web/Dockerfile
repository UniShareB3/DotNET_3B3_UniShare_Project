# Stage 1: Build the Flutter web app
FROM ghcr.io/cirruslabs/flutter:stable AS build

# Set working directory
WORKDIR /app

# Define build argument - will be passed from deployment platform
ARG API_BASE_URL

# Copy pubspec files
COPY pubspec.yaml pubspec.lock ./

# Get Flutter dependencies
RUN flutter pub get

# Copy the rest of the application
COPY . .

# Create .env file with API_BASE_URL
# If API_BASE_URL is not provided, use the local .env file
RUN if [ -z "$API_BASE_URL" ]; then \
      echo "Using .env file from source"; \
    else \
      echo "API_BASE_URL=$API_BASE_URL" > .env; \
    fi

# Build the Flutter web app
RUN flutter build web --release

# Stage 2: Serve the app with nginx
FROM nginx:alpine

# Copy the built web app from the previous stage
COPY --from=build /app/build/web /usr/share/nginx/html

# Copy custom nginx configuration
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Expose port 80
EXPOSE 80

# Start nginx
CMD ["nginx", "-g", "daemon off;"]
