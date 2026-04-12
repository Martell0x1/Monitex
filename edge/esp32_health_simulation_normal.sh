#!/bin/bash

BROKER="${BROKER:-monitex.local}"
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
    VALUE=$((35 + RANDOM % 25))
    UPTIME=$((3600 + RANDOM % 7200))
    WIFI_RSSI=$((-62 + RANDOM % 8))
    FREE_HEAP=$((145000 + RANDOM % 12000))
    MIN_FREE_HEAP=$((118000 + RANDOM % 10000))

    SENSOR_PAYLOAD=$(printf '{"deviceName":"%s","sensorType":"%s","value":%d,"timestamp":"%s","ipAddress":"%s"}' \
        "$DEVICE" "$SENSOR" "$VALUE" "$TIMESTAMP" "$IP_ADDRESS")

    HEALTH_PAYLOAD=$(printf '{"messageType":"health","deviceName":"%s","timestamp":"%s","ipAddress":"%s","uptimeSeconds":%d,"wifiRssi":%d,"freeHeapBytes":%d,"minFreeHeapBytes":%d,"restartReason":"power_on","mqttConnected":true,"lastSensorReadOk":true}' \
        "$DEVICE" "$TIMESTAMP" "$IP_ADDRESS" "$UPTIME" "$WIFI_RSSI" "$FREE_HEAP" "$MIN_FREE_HEAP")

    echo "Publishing NORMAL sensor: $SENSOR_PAYLOAD"
    echo "Publishing NORMAL health: $HEALTH_PAYLOAD"

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
