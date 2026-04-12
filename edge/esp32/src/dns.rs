use esp_idf_sys as sys;
use std::net::IpAddr;
use std::str::FromStr;
use std::thread;
use std::time::Duration;

/// Resolve a hostname using mDNS (e.g. "monitex.local")
pub fn resolve_mdns(hostname: &str) -> Result<IpAddr, String> {
    unsafe {
        // Initialize mDNS (safe to call multiple times)
        let ret = sys::esp_err_to_name(sys::mdns_init());
        if !ret.is_null() {
            println!("mDNS init status: {:?}", ret);
        }
    }

    let mut attempts = 0;

    loop {
        attempts += 1;

        match query_host(hostname) {
            Some(ip) => return Ok(ip),
            None => {
                if attempts > 10 {
                    return Err(format!(
                        "Failed to resolve {} via mDNS after {} attempts",
                        hostname, attempts
                    ));
                }

                println!(
                    "[DNS] mDNS resolve failed for {}, retrying... ({})",
                    hostname, attempts
                );

                thread::sleep(Duration::from_millis(500));
            }
        }
    }
}

/// Internal resolver using ESP-IDF mDNS query
fn query_host(hostname: &str) -> Option<IpAddr> {
    unsafe {
        let mut addr: sys::esp_ip4_addr_t = std::mem::zeroed();

        let c_hostname = std::ffi::CString::new(hostname).ok()?;

        // NOTE: ESP-IDF mDNS API (C binding)
        let res = sys::mdns_query_a(
            c_hostname.as_ptr(),
            2000, // timeout ms
            &mut addr,
        );

        if res == sys::ESP_OK {
            let ip = IpAddr::from_str(&format!(
                "{}.{}.{}.{}",
                addr.addr & 0xff,
                (addr.addr >> 8) & 0xff,
                (addr.addr >> 16) & 0xff,
                (addr.addr >> 24) & 0xff,
            ))
            .ok();

            return ip;
        }
    }

    None
}
