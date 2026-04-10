use crate::payload;

pub struct DeviceHealthSnapshot {
    pub uptime_seconds: u64,
    pub wifi_rssi: i32,
    pub free_heap_bytes: u32,
    pub min_free_heap_bytes: u32,
    pub restart_reason: &'static str,
    pub mqtt_connected: bool,
    pub last_sensor_read_ok: bool,
    pub ip_address: String,
    pub timestamp: String,
}

pub fn collect(ip_address: &str, last_sensor_read_ok: bool) -> DeviceHealthSnapshot {
    DeviceHealthSnapshot {
        uptime_seconds: current_uptime_seconds(),
        wifi_rssi: current_wifi_rssi(),
        free_heap_bytes: current_free_heap_bytes(),
        min_free_heap_bytes: current_min_free_heap_bytes(),
        restart_reason: current_restart_reason(),
        mqtt_connected: true,
        last_sensor_read_ok,
        ip_address: ip_address.to_owned(),
        timestamp: payload::current_timestamp_iso8601(),
    }
}

pub fn build_payload(device_name: &str, snapshot: &DeviceHealthSnapshot) -> String {
    format!(
        concat!(
            r#"{{"messageType":"health","deviceName":"{}","timestamp":"{}","ipAddress":"{}","#,
            r#""uptimeSeconds":{},"wifiRssi":{},"freeHeapBytes":{},"minFreeHeapBytes":{},"#,
            r#""restartReason":"{}","mqttConnected":{},"lastSensorReadOk":{}}}"#
        ),
        device_name,
        snapshot.timestamp,
        snapshot.ip_address,
        snapshot.uptime_seconds,
        snapshot.wifi_rssi,
        snapshot.free_heap_bytes,
        snapshot.min_free_heap_bytes,
        snapshot.restart_reason,
        snapshot.mqtt_connected,
        snapshot.last_sensor_read_ok
    )
}

fn current_uptime_seconds() -> u64 {
    let micros = unsafe { esp_idf_sys::esp_timer_get_time() };
    if micros <= 0 {
        return 0;
    }

    (micros as u64) / 1_000_000
}

fn current_free_heap_bytes() -> u32 {
    unsafe { esp_idf_sys::esp_get_free_heap_size() as u32 }
}

fn current_min_free_heap_bytes() -> u32 {
    unsafe { esp_idf_sys::esp_get_minimum_free_heap_size() as u32 }
}

fn current_wifi_rssi() -> i32 {
    let mut ap_info: esp_idf_sys::wifi_ap_record_t = unsafe { core::mem::zeroed() };
    let err = unsafe { esp_idf_sys::esp_wifi_sta_get_ap_info(&mut ap_info as *mut _) };

    if err == 0 {
        ap_info.rssi as i32
    } else {
        -127
    }
}

fn current_restart_reason() -> &'static str {
    match unsafe { esp_idf_sys::esp_reset_reason() } {
        esp_idf_sys::esp_reset_reason_t_ESP_RST_UNKNOWN => "unknown",
        esp_idf_sys::esp_reset_reason_t_ESP_RST_POWERON => "power_on",
        esp_idf_sys::esp_reset_reason_t_ESP_RST_EXT => "external",
        esp_idf_sys::esp_reset_reason_t_ESP_RST_SW => "software",
        esp_idf_sys::esp_reset_reason_t_ESP_RST_PANIC => "panic",
        esp_idf_sys::esp_reset_reason_t_ESP_RST_INT_WDT => "interrupt_watchdog",
        esp_idf_sys::esp_reset_reason_t_ESP_RST_TASK_WDT => "task_watchdog",
        esp_idf_sys::esp_reset_reason_t_ESP_RST_WDT => "watchdog",
        esp_idf_sys::esp_reset_reason_t_ESP_RST_DEEPSLEEP => "deepsleep",
        esp_idf_sys::esp_reset_reason_t_ESP_RST_BROWNOUT => "brownout",
        esp_idf_sys::esp_reset_reason_t_ESP_RST_SDIO => "sdio",
        _ => "other",
    }
}
