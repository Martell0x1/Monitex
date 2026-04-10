use std::ffi::CStr;

pub fn current_timestamp_iso8601() -> String {
    let mut now: esp_idf_sys::time_t = 0;

    unsafe {
        esp_idf_sys::time(&mut now as *mut _);
    }

    let mut utc_tm: esp_idf_sys::tm = unsafe { core::mem::zeroed() };

    unsafe {
        esp_idf_sys::gmtime_r(&now as *const _, &mut utc_tm as *mut _);
    }

    let mut buf = [0_u8; 21];
    let fmt = b"%Y-%m-%dT%H:%M:%SZ\0";

    unsafe {
        esp_idf_sys::strftime(
            buf.as_mut_ptr(),
            buf.len(),
            fmt.as_ptr(),
            &utc_tm as *const _,
        );
    }

    unsafe { CStr::from_ptr(buf.as_ptr()) }
        .to_string_lossy()
        .into_owned()
}

pub fn build_payload(
    device_name: &str,
    sensor_type: &str,
    value: f32,
    timestamp: &str,
    ip_address: Option<&str>,
) -> String {
    let ip_field = ip_address.unwrap_or("");

    format!(
        r#"{{"deviceName":"{}","sensorType":"{}","value":{},"timestamp":"{}","ipAddress":"{}"}}"#,
        device_name, sensor_type, value, timestamp, ip_field
    )
}

pub fn build_ldr_payload(device_name: &str, adc_value: u16, ip_address: Option<&str>) -> String {
    let timestamp = current_timestamp_iso8601();
    build_payload(device_name, "ldr", adc_value as f32, &timestamp, ip_address)
}
