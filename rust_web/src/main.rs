use axum::{
    extract::State,
    routing::get,
    Router,
};
use opentelemetry::{global::{self, BoxedTracer}, trace::{Span, TraceContextExt, Tracer}, KeyValue};
use opentelemetry_sdk::{metrics::SdkMeterProvider, trace::{ BatchSpanProcessor, SdkTracerProvider}, Resource};
use opentelemetry_otlp::{ ExporterBuildError, MetricExporter, SpanExporter, WithExportConfig};
use std::{error::Error, net::SocketAddr, sync::{Arc, OnceLock}};
struct AppState{
    counter: opentelemetry::metrics::Counter<u64>,
    tracer : BoxedTracer
}

fn get_resource() -> Resource {
    static RESOURCE: OnceLock<Resource> = OnceLock::new();
    RESOURCE
        .get_or_init(|| {
            Resource::builder()
                .with_service_name("basic-otlp-example-grpc")
                .build()
        })
        .clone()
}

fn init_traces() -> Result<SdkTracerProvider, ExporterBuildError>  {
    let exporter = SpanExporter::builder()
        .with_tonic()
        .build()?;

    let processor = BatchSpanProcessor::builder(exporter).build();
    Ok(SdkTracerProvider::builder()
        .with_span_processor(processor)
        .with_resource(get_resource())
        .build())
}


fn init_metrics() -> Result<SdkMeterProvider, ExporterBuildError> {
    let exporter = MetricExporter::builder()
        .with_tonic()
        .build()?;

    Ok(SdkMeterProvider::builder()
    .with_periodic_exporter(exporter)
    .with_resource(get_resource())
    .build())
}
async fn root(State(state): State<Arc<AppState>>) -> &'static str {
    state.counter.add(1, &[KeyValue::new("route", "/")]);
    state.tracer.in_span("doing_work", |cx| {
        let span = cx.span();
        span.add_event("root", vec![KeyValue::new("route", "/")]);
    });

    "Hello, World! Rust."
}

#[tokio::main]
async fn main() -> Result<(), Box<dyn Error>> {
    // Initialize tracer
    let tracer_provider = init_traces()?;
    global::set_tracer_provider(tracer_provider.clone());

    // Initialize meter provider
    let meter_provider = init_metrics()?;
    global::set_meter_provider(meter_provider.clone());

    let tracer = global::tracer("rustweb_tracer");
    let meter = global::meter("rustweb_meter");


    let counter = meter
            .u64_counter("http_requests_total")
            .with_description("a simple counter for demo purposes.")
            .with_unit("my_unit")
            .build();

    let app_state = Arc::new(AppState { counter : counter, tracer : tracer });

    // Build Axum app
    let app = Router::new()
        .route("/", get(root))
        .with_state(app_state);

    // Start server
    let addr = SocketAddr::from(([0, 0, 0, 0], 3333));
    println!("Listening on http://{}", addr);
    let listener = tokio::net::TcpListener::bind(addr).await?;
    axum::serve(listener, app).await?;

    // Shut down providers
    let mut shutdown_errors = Vec::new();
    // if let Err(e) = tracer_provider.shutdown() {
    //     shutdown_errors.push(format!("tracer provider: {}", e));
    // }

    if let Err(e) = meter_provider.shutdown() {
        shutdown_errors.push(format!("meter provider: {}", e));
    }

    if !shutdown_errors.is_empty() {
        return Err(format!(
            "Failed to shutdown providers:{}",
            shutdown_errors.join("\n")
        )
        .into());
    }

    Ok(())
}




