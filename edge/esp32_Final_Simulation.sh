#!/usr/bin/env bash

########################################
# Neon-Pastel Color Palette
########################################
PINK='\033[38;5;213m'
PURPLE='\033[38;5;147m'
CYAN='\033[38;5;123m'
TEAL='\033[38;5;86m'
LAVENDER='\033[38;5;183m'
RED='\033[38;5;203m'
BOLD='\033[1m'
DIM='\033[2m'
NC='\033[0m'

########################################
# Configuration & State
########################################
BROKER="${BROKER:-monitex.local}"
PORT="${PORT:-1883}"
TOPIC="${TOPIC:-topic/test}"
DEVICE="${DEVICE:-esp32-1}"
IP_ADDRESS="$(hostname -I 2>/dev/null | awk '{print $1}')"
[ -z "$IP_ADDRESS" ] && IP_ADDRESS="127.0.0.1"

# Initial State
STATE="NORMAL" # Options: NORMAL, WARNING, DANGER
UPTIME=0

########################################
# UI Components
########################################
log_status() {
    local color=$1
    local mode=$2
    local msg=$3
    echo -e "${LAVENDER}┃${NC} ${color}${BOLD}[${mode}]${NC} ${DIM}${msg}${NC}"
}


banner() {
    clear
    echo -e "${PINK}"
    # Using a 'Here-Doc' with single quotes around EOF to treat everything literally
    cat << 'EOF'
           .==-.                   .-==.
            \()8`-._  `.   .`  _.-8()/
            (88"   ::.  \./  .::   "88)
             \_.'`-::::.(#).::::-'\_/
               `._... .q(_)p. ..._.
                 ""-..-'|=|`-..-""

            S M A R T  S I M U L A T O R
EOF
    echo -e "${NC}"

    echo -e "  ${BOLD}${PURPLE}M O N I T E X${NC} ${DIM}v1.0.0${NC}"
    echo -e "  ${DIM}IoT Monitoring Ecosystem & Edge Setup${NC}"
    echo -e "${DIM}───────────────────────────────────────────────${NC}"
    echo ""
}


########################################
# Simulation Engine
########################################
update_state() {
    # 10% chance to change state every cycle
    local change_roll=$((RANDOM % 10))
    if [ $change_roll -eq 0 ]; then
        local state_roll=$((RANDOM % 100))
        if [ $state_roll -lt 70 ]; then
            STATE="NORMAL"
        elif [ $state_roll -lt 90 ]; then
            STATE="WARNING"
        else
            STATE="DANGER"
        fi
    fi
}

generate_data() {
    TIMESTAMP=$(date -u +"%Y-%m-%dT%H:%M:%SZ")
    UPTIME=$((UPTIME + 5))

    case "$STATE" in
        "NORMAL")
            VALUE=$((35 + RANDOM % 15))
            WIFI=$((-60 + RANDOM % 5))
            HEAP=$((140000 + RANDOM % 5000))
            MIN_HEAP=$((120000 + RANDOM % 2000))
            REASON="power_on"
            MQTT="true"
            SENSOR_OK="true"
            COLOR=$TEAL
            ;;
        "WARNING")
            VALUE=$((60 + RANDOM % 20))
            WIFI=$((-82 + RANDOM % 8))
            HEAP=$((40000 + RANDOM % 10000))
            MIN_HEAP=$((20000 + RANDOM % 5000))
            REASON="software_watchdog"
            MQTT="true"
            SENSOR_OK="true"
            COLOR=$PURPLE
            ;;
        "DANGER")
            VALUE=$((85 + RANDOM % 15))
            WIFI=$((-94 + RANDOM % 4))
            HEAP=$((8000 + RANDOM % 4000))
            MIN_HEAP=$((4000 + RANDOM % 2000))
            REASON="panic"
            # Randomly simulate disconnects in Danger mode
            [ $((RANDOM % 2)) -eq 0 ] && MQTT="false" || MQTT="true"
            [ $((RANDOM % 3)) -eq 0 ] && SENSOR_OK="false" || SENSOR_OK="true"
            COLOR=$RED
            ;;
    esac

    # Build Payloads
    SENSOR_PAYLOAD=$(printf '{"deviceName":"%s","sensorType":"ldr","value":%d,"timestamp":"%s","ipAddress":"%s"}' \
        "$DEVICE" "$VALUE" "$TIMESTAMP" "$IP_ADDRESS")

    HEALTH_PAYLOAD=$(printf '{"messageType":"health","deviceName":"%s","timestamp":"%s","ipAddress":"%s","uptimeSeconds":%d,"wifiRssi":%d,"freeHeapBytes":%d,"minFreeHeapBytes":%d,"restartReason":"%s","mqttConnected":%s,"lastSensorReadOk":%s}' \
        "$DEVICE" "$TIMESTAMP" "$IP_ADDRESS" "$UPTIME" "$WIFI" "$HEAP" "$MIN_HEAP" "$REASON" "$MQTT" "$SENSOR_OK")
}

########################################
# Execution Loop
########################################
banner
echo -e "${BOLD}${CYAN}◆ Initializing Connection${NC}"
log_status "$CYAN" "INFO" "Broker: $BROKER | Topic: $TOPIC"
echo -e "${DIM}───────────────────────────────────────────────${NC}"

while true
do
    update_state
    generate_data

    log_status "$COLOR" "$STATE" "Sensor: $VALUE | WiFi: $WIFI dBm | Heap: $HEAP bytes"

    # Execute MQTT Pubs (suppressing output)
    mosquitto_pub -h "$BROKER" -p "$PORT" -t "$TOPIC" -m "$SENSOR_PAYLOAD" 2>/dev/null
    mosquitto_pub -h "$BROKER" -p "$PORT" -t "$TOPIC" -m "$HEALTH_PAYLOAD" 2>/dev/null

    if [ $? -ne 0 ]; then
        echo -e "${RED}┗━━ ✖ Connection to $BROKER failed!${NC}"
    fi

    sleep 5
done
