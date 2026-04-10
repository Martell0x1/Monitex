#!/bin/bash

BROKER="${BROKER:-localhost}"
PORT="${PORT:-1883}"
TOPIC="${TOPIC:-topic/test}"
DEVICE="${DEVICE:-esp32-1}"
SENSOR="${SENSOR:-ldr}"
IP_ADDRESS="${ESP32_SIM_IP:-$(hostname -I 2>/dev/null | awk '{print $1}')}"

if [ -z "$IP_ADDRESS" ]; then
    IP_ADDRESS="127.0.0.1"
fi

while true
do
    TIMESTAMP=$(date -u +"%Y-%m-%dT%H:%M:%SZ")
    VALUE=$((55 + RANDOM % 30))
    UPTIME=$((180 + RANDOM % 600))
    WIFI_RSSI=$((-80 + RANDOM % 4))
    FREE_HEAP=$((38000 + RANDOM % 9000))
    MIN_FREE_HEAP=$((18000 + RANDOM % 6000))

    SENSOR_PAYLOAD=$(printf '{"deviceName":"%s","sensorType":"%s","value":%d,"timestamp":"%s","ipAddress":"%s"}' \
        "$DEVICE" "$SENSOR" "$VALUE" "$TIMESTAMP" "$IP_ADDRESS")

    HEALTH_PAYLOAD=$(printf '{"messageType":"health","deviceName":"%s","timestamp":"%s","ipAddress":"%s","uptimeSeconds":%d,"wifiRssi":%d,"freeHeapBytes":%d,"minFreeHeapBytes":%d,"restartReason":"software","mqttConnected":true,"lastSensorReadOk":true}' \
        "$DEVICE" "$TIMESTAMP" "$IP_ADDRESS" "$UPTIME" "$WIFI_RSSI" "$FREE_HEAP" "$MIN_FREE_HEAP")

    echo "Publishing WARNING sensor: $SENSOR_PAYLOAD"
    echo "Publishing WARNING health: $HEALTH_PAYLOAD"

    mosquitto_pub \
        -h "$BROKER" \
        -p "$PORT" \
        -t "$TOPIC" \
        -m "$SENSOR_PAYLOAD"

    mosquitto_pub \
        -h "$BROKER" \
        -p "$PORT" \
        -t "$TOPIC" \
        -m "$HEALTH_PAYLOAD"

    sleep 5
done
