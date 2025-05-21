# OpenTelemetry Sidecar Example
This project demonstrates how to implement observability in a .NET application using OpenTelemetry with a sidecar pattern. The application exports metrics and traces to an OpenTelemetry Collector, which then forwards them to Prometheus for metrics and Jaeger for distributed tracing.
## Architecture
The project consists of the following components:
- **.NET Web Application**: A simple web service with custom metrics and tracing- **OpenTelemetry Collector**: Receives telemetry data and exports it to backends
- **Prometheus**: Time-series database for storing and querying metrics- **Jaeger**: Distributed tracing system for monitoring and troubleshooting
- **Grafana**: Visualization and dashboarding platform
```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│                 │     │                 │     │                 │
│  .NET App       │────▶│  OTel Collector │────▶│  Prometheus     │
│                 │     │                 │     │                 │
└─────────────────┘     └────────┬────────┘     └─────────────────┘
                                 │                        ▲
                                 │                        │
                                 ▼                        │
                        ┌─────────────────┐      ┌────────┴────────┐
                        │                 │      │                 │
                        │  Jaeger         │      │  Grafana        │
                        │                 │      │                 │
                        └─────────────────┘      └─────────────────┘
```
## Features
- Custom metrics using OpenTelemetry Metrics API- Distributed tracing with OpenTelemetry Tracing API
- Nested HTTP calls with context propagation- Prometheus metrics collection
- Jaeger trace visualization- Grafana dashboards for metrics visualization
## Prerequisites
- Docker and Docker Compose
- .NET 9.0 SDK (for local development)
## Getting Started
### Running the Application
1. Clone the repository:
2. ```
   git clone https://github.com/yourusername/OpenTelemetry_SideCar.git
   cd OpenTelemetry_SideCar
   ```
3. Start the services using Docker Compose:   ```
   docker-compose up -d   ```
4. Access the application:
   - Web Application: http://localhost:8080   - Nested Greeting: http://localhost:8080/NestedGreeting?nestlevel=3
### Accessing Observability Tools
- **Prometheus**: http://localhost:9090
  - Query metrics with PromQL  - Example: `greetings_count_total`
- **Jaeger UI**: http://localhost:16686
  - View distributed traces  - Filter by service: `telemetry_example`
- **Grafana**: http://localhost:3000
  - Default credentials: admin/admin  - Pre-configured dashboards for application metrics
## Application Endpoints
- **/** - Returns a simple greeting and increments the greeting counter
- **/NestedGreeting?nestlevel=N** - Creates a chain of N nested HTTP calls, demonstrating trace context propagation
## Configuration Files
- **docker-compose.yml**: Defines all services and their connections- **otel-collector-config.yaml**: Configures the OpenTelemetry Collector
- **prometheus.yml**: Prometheus scraping configuration- **Dockerfile**: Builds the .NET application
## Troubleshooting
### Common Issues
1. **No metrics in Prometheus**:
   - Verify the OpenTelemetry Collector is running: `docker-compose ps`   - Check collector logs: `docker-compose logs otel-collector`
   - Ensure Prometheus is scraping the collector: http://localhost:9090/targets
2. **No traces in Jaeger**:   - Verify Jaeger is running: `docker-compose ps jaeger`
   - Check that OTLP is enabled in Jaeger   - Generate some traces by accessing the application endpoints
3. **Application errors**:
   - Check application logs: `docker-compose logs app`
## Development
### Local Development
To run the application locally:
1. Navigate to the src directory2. Run `dotnet run`
Note: When running locally, you'll need to update the OTLP endpoint in Program.cs to point to your local OpenTelemetry Collector.
### Adding Custom Metrics
1. Create a new meter:
   ```csharp
   var myMeter = new Meter("MyApp.Metrics", "1.0.0");
   ```
2. Create metrics instruments:
   ```csharp
   var myCounter = myMeter.CreateCounter<int>("my.counter", "Count of operations");
   ```
4. Record measurements:
   ```csharp
   myCounter.Add(1);
   ```
5. Register the meter in the OpenTelemetry configuration:
   ```csharp
   metricsProviderBuilder.AddMeter("MyApp.Metrics");
   ```
