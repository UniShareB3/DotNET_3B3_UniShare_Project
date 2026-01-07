# Flutter Frontend Deployment with Docker

This directory contains the necessary files to deploy the Flutter web application using Docker.

## Files Overview

- **Dockerfile**: Multi-stage Docker build configuration
- **nginx.conf**: Custom nginx configuration for Flutter web routing
- **.dockerignore**: Files to exclude from Docker build context
- **.env**: Local development environment variables (not committed to git)

## Environment Variables

The application requires the `API_BASE_URL` environment variable to connect to the backend API.

### Local Development

The `.env` file in this directory contains:
```env
API_BASE_URL=https://dotnet-3b3-unishare-project.onrender.com
```

### Docker Build with Custom API URL

To build with a custom API URL:

```bash
docker build --build-arg API_BASE_URL=https://your-backend-url.com -t unishare-frontend:latest .
```

## Building the Docker Image

### Using local .env file:
```bash
docker build -t unishare-frontend:latest .
```

### With custom API URL:
```bash
docker build --build-arg API_BASE_URL=https://your-api.com -t unishare-frontend:latest .
```

## Running the Container

To run the container locally:

```bash
docker run -d -p 8080:80 --name unishare-frontend unishare-frontend:latest
```

Access the application at: http://localhost:8080

## Deployment Platform Configuration

### Azure Container Apps / Web Apps

In your Azure pipeline or portal, set the build argument:

```yaml
# azure-pipelines.yml
- task: Docker@2
  inputs:
    command: 'build'
    Dockerfile: '**/Dockerfile'
    arguments: '--build-arg API_BASE_URL=$(API_BASE_URL)'
```

In Azure Portal:
1. Go to Configuration > Application Settings
2. Add: `API_BASE_URL` = `https://your-backend.azurewebsites.net`
3. In Docker settings, ensure build args are passed

### Render

In your Render Dashboard:
1. Go to your service settings
2. Add Environment Variable: `API_BASE_URL` with your backend URL
3. Render automatically passes environment variables as build args if you configure:

Add to your service settings or `render.yaml`:

```yaml
services:
  - type: web
    name: unishare-frontend
    env: docker
    dockerfilePath: ./Frontend/unishare_web/Dockerfile
    dockerContext: ./Frontend/unishare_web
    envVars:
      - key: API_BASE_URL
        value: https://your-backend.onrender.com
```

**Important for Render**: In the service settings, under "Docker Command", you may need to specify:
```bash
--build-arg API_BASE_URL=$API_BASE_URL
```

### Railway

Set the environment variable in Railway dashboard:
1. Go to Variables tab
2. Add: `API_BASE_URL` = `https://your-backend.up.railway.app`

Railway automatically passes environment variables as build args.

### Heroku

```bash
heroku config:set API_BASE_URL=https://your-backend.herokuapp.com
```

In your `heroku.yml`:
```yaml
build:
  docker:
    web:
      dockerfile: Dockerfile
      buildArgs:
        API_BASE_URL: $API_BASE_URL
```

### Google Cloud Run

```bash
gcloud run deploy unishare-frontend \
  --source . \
  --build-arg API_BASE_URL=https://your-backend.run.app
```

## Docker Compose

Update the `docker-compose.yml` to pass the build arg:

```yaml
version: '3.8'
services:
  frontend:
    build:
      context: .
      dockerfile: Dockerfile
      args:
        API_BASE_URL: ${API_BASE_URL:-https://dotnet-3b3-unishare-project.onrender.com}
    ports:
      - "8080:80"
```

## Stopping and Removing the Container

```bash
docker stop unishare-frontend
docker rm unishare-frontend
```

## Production Deployment Checklist

- [ ] Set `API_BASE_URL` environment variable in your deployment platform
- [ ] Configure the platform to pass `API_BASE_URL` as a Docker build argument
- [ ] Ensure the backend URL is accessible from the internet
- [ ] Configure CORS on your backend to allow requests from the frontend domain
- [ ] Test the deployment with a test API call
- [ ] Monitor nginx logs for any connection issues

## Troubleshooting

### Frontend is calling localhost or wrong URL instead of the backend URL

**Cause**: The `API_BASE_URL` wasn't passed during the Docker build.

**Solution**: 
1. Verify the environment variable is set in your deployment platform
2. Ensure the platform is configured to pass it as a build argument
3. Rebuild the Docker image with the correct build arg:
   ```bash
   docker build --build-arg API_BASE_URL=https://your-backend.com -t unishare-frontend .
   ```

### CORS errors

**Cause**: Backend isn't configured to accept requests from your frontend domain.

**Solution**: Update your backend CORS policy to include your frontend URL.

### Verify what API URL was built into the image

Run this after building:
```bash
docker run --rm -it unishare-frontend:latest sh -c "cat /usr/share/nginx/html/flutter.js | head -100"
```

Or check the .env during build by adding this to Dockerfile temporarily:
```dockerfile
RUN cat .env
```

## Notes

- Flutter web apps compile environment variables at **build time**, not runtime
- The `.env` file must exist before `flutter build web` runs
- Nginx serves the pre-built static files
- Static assets are cached for optimal performance
- The container runs on port 80 internally, mapped to your desired external port

