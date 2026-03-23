use esp_idf_svc::ping::EspPing;
use std::net::Ipv4Addr;
use std::str::FromStr;

/// Ping your PC
pub fn check_pc(pc_ip: &str) {
    let mut ping = EspPing::new(0_u32);

    let ping_res = ping.ping(
        Ipv4Addr::from_str(pc_ip).unwrap(),
        &esp_idf_svc::ping::Configuration::default(),
    );

    match ping_res {
        Ok(summary) => println!(
            "[PING] Transmitted: {}, Received: {}, Time: {:?}",
            summary.transmitted, summary.received, summary.time
        ),
        Err(e) => println!("[PING ERROR] {:?}", e),
    }
}
