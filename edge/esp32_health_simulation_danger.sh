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
    VALUE=$((80 + RANDOM % 20))
    UPTIME=$((15 + RANDOM % 60))
    WIFI_RSSI=$((-92 + RANDOM % 4))
    FREE_HEAP=$((12000 + RANDOM % 4000))
    MIN_FREE_HEAP=$((7000 + RANDOM % 2000))

    MQTT_CONNECTED=$([ $((RANDOM % 2)) -eq 0 ] && echo "false" || echo "true")
    SENSOR_OK=$([ $((RANDOM % 3)) -eq 0 ] && echo "true" || echo "false")

    SENSOR_PAYLOAD=$(printf '{"deviceName":"%s","sensorType":"%s","value":%d,"timestamp":"%s","ipAddress":"%s"}' \
        "$DEVICE" "$SENSOR" "$VALUE" "$TIMESTAMP" "$IP_ADDRESS")

    HEALTH_PAYLOAD=$(printf '{"messageType":"health","deviceName":"%s","timestamp":"%s","ipAddress":"%s","uptimeSeconds":%d,"wifiRssi":%d,"freeHeapBytes":%d,"minFreeHeapBytes":%d,"restartReason":"panic","mqttConnected":%s,"lastSensorReadOk":%s}' \
        "$DEVICE" "$TIMESTAMP" "$IP_ADDRESS" "$UPTIME" "$WIFI_RSSI" "$FREE_HEAP" "$MIN_FREE_HEAP" "$MQTT_CONNECTED" "$SENSOR_OK")

    echo "Publishing DANGER sensor: $SENSOR_PAYLOAD"
    echo "Publishing DANGER health: $HEALTH_PAYLOAD"

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
