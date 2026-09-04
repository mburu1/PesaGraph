# PesaGraph

> **Kenya-first. Multi-Rail Payment Reconciliation & Liquidity Intelligence Platform.**
> M-Pesa · Airtel Money · Banks · WhatsApp · SMS — one operations cockpit.

[![Build](https://github.com/mburu1/PesaGraph/actions/workflows/ci.yml/badge.svg)](https://github.com/mburu1/PesaGraph/actions)

---

## Quick Start

```bash
# 1. Clone and copy env template
git clone https://github.com/mburu1/PesaGraph.git && cd PesaGraph
cp .env.example .env          # Fill in sandbox credentials

# 2. Start full stack (RabbitMQ profile)
docker compose -f docker/docker-compose.yml --profile rabbitmq up

# 3. Open the Angular dashboard
open http://localhost:4200

# 4. Open RabbitMQ management UI
open http://localhost:15672    # user: pesagraph / pass: pesagraph_dev

# 5. (Optional) Add Kafka / Redpanda layer
docker compose -f docker/docker-compose.yml --profile both up
open http://localhost:8082     # Redpanda Console
```

## Local Development (without Docker)

```bash
# Backend
dotnet run --project src/PesaGraph.Api

# Frontend
cd frontend && npm install && npm start
# → http://localhost:4200
```

## Architecture

See **[Instructions.PesaGraph.READMe.md](Instructions.PesaGraph.READMe.md)** for the full product vision,
architecture decisions, bounded contexts, messaging strategy, and WhatsApp integration principles.

See **[docs/](docs/)** for C4 diagrams, sequence diagrams, OOAD class diagrams, state machines and ERDs.

## Stack

| Layer | Technology |
|---|---|
| API | ASP.NET 9, Minimal APIs |
| Workers | .NET Generic Host, MassTransit |
| Frontend | Angular 19 (standalone, signals) |
| Relational DB | PostgreSQL 16 |
| Document Store | MongoDB 7 |
| Cache | Redis 7 |
| Messaging | RabbitMQ 3.13 + MassTransit |
| Streaming | Redpanda (Kafka-compatible) |
| Container | Docker Compose (profiles) |
