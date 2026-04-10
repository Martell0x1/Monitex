import asyncio

from anomaly_service import AnomalyReaderService


async def consume_anomaly_queue() -> None:
    service = AnomalyReaderService()
    await service.consume()


if __name__ == "__main__":
    try:
        asyncio.run(consume_anomaly_queue())
    except KeyboardInterrupt:
        print("\n[RabbitMQ Reader] Stopped by user.")
