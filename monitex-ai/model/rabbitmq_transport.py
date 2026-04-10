import json

import aio_pika


class RabbitMQAnomalyTransport:
    def __init__(
        self,
        rabbitmq_url: str,
        queue_name: str,
        exchange_name: str,
        results_routing_key: str,
    ) -> None:
        self.rabbitmq_url = rabbitmq_url
        self.queue_name = queue_name
        self.exchange_name = exchange_name
        self.results_routing_key = results_routing_key

    async def connect(self) -> tuple[aio_pika.abc.AbstractRobustConnection, aio_pika.abc.AbstractChannel]:
        connection = await aio_pika.connect_robust(self.rabbitmq_url)
        channel = await connection.channel()
        await channel.set_qos(prefetch_count=1)
        return connection, channel

    async def declare_input_queue(
        self,
        channel: aio_pika.abc.AbstractChannel,
    ) -> aio_pika.abc.AbstractQueue:
        return await channel.declare_queue(self.queue_name, durable=True)

    async def get_exchange(
        self,
        channel: aio_pika.abc.AbstractChannel,
    ) -> aio_pika.abc.AbstractExchange:
        return await channel.get_exchange(self.exchange_name, ensure=True)

    async def publish_anomaly(
        self,
        exchange: aio_pika.abc.AbstractExchange,
        anomaly_payload: dict,
    ) -> None:
        await exchange.publish(
            aio_pika.Message(
                body=json.dumps(anomaly_payload).encode("utf-8"),
                content_type="application/json",
                delivery_mode=aio_pika.DeliveryMode.PERSISTENT,
            ),
            routing_key=self.results_routing_key,
        )
