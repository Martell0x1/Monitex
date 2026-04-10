#!/bin/bash

BROKER="localhost"
PORT=1883
TOPIC="topic/test"
DEVICE="esp32-1"
SENSOR="ldr"
IP_ADDRESS="${ESP32_SIM_IP:-$(hostname -I 2>/dev/null | awk '{print $1}')}"

if [ -z "$IP_ADDRESS" ]; then
    IP_ADDRESS="127.0.0.1"
fi

while true
do
    VALUE=$((RANDOM % 100))   # random value 0–99
    TIMESTAMP=$(date -u +"%Y-%m-%dT%H:%M:%SZ")

    PAYLOAD=$(printf '{"deviceName":"%s","sensorType":"%s","value":%d,"timestamp":"%s","ipAddress":"%s"}' \
        "$DEVICE" "$SENSOR" "$VALUE" "$TIMESTAMP" "$IP_ADDRESS")

    echo "Publishing: $PAYLOAD"

    mosquitto_pub \
        -h "$BROKER" \
        -p "$PORT" \
        -t "$TOPIC" \
        -m "$PAYLOAD"

    sleep 2
done
