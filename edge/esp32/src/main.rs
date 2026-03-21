use esp_idf_svc::eventloop::EspSystemEventLoop;
use esp_idf_svc::mqtt::client::{
    EspMqttClient, EspMqttConnection, EventPayload, MqttClientConfiguration, QoS,
};
use esp_idf_svc::netif::EspNetif;
use esp_idf_svc::nvs::EspDefaultNvsPartition;
use esp_idf_svc::ping::EspPing;
use esp_idf_svc::wifi::{BlockingWifi, ClientConfiguration, Configuration, EspWifi};
use esp_idf_sys::{self as _, EspError};
use esp_idf_hal::delay::FreeRtos;
use esp_idf_hal::peripherals::Peripherals;

use std::net::Ipv4Addr;
use std::str::FromStr;

/// Ping your PC
fn check_pc(pc_ip: &str) {
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

/// Initialize WiFi
fn init_wifi(wifi: &mut BlockingWifi<EspWifi<'_>>) -> Result<(), EspError> {
    let wifi_conf = Configuration::Client(ClientConfiguration {
        ssid: "Martell".try_into().unwrap(),
        password: "MarwanMartell@04".try_into().unwrap(),
        bssid: None,
        auth_method: esp_idf_svc::wifi::AuthMethod::WPA2Personal,
        channel: None,
        ..Default::default()
    });

    wifi.set_configuration(&wifi_conf);
    wifi.start()?;
    println!("WiFi Started...");
    wifi.connect()?;
    println!("WiFi Connecting...");
    wifi.wait_netif_up()?;
    println!("WiFi Connected!");
    Ok(())
}

/// Initialize MQTT
use std::sync::{Arc, Mutex};

fn init_mqtt() -> Result<EspMqttClient<'static>,EspError> {
    let config = MqttClientConfiguration{
        client_id:"esp_client".into(),
        ..Default::default()
    };

    let (mut client , mut conn) = EspMqttClient::new("mqtt://192.168.1.2:1883", &config)?;

    let connected = Arc::new(Mutex::new(false));
    let connceted_clone = connected.clone();

    std::thread::spawn(move || {
        println!("Mqtt Event Loop thread Started !");

        while let Ok(event) = conn.next() {
            match event.payload() {
                EventPayload::Connected(_) => {
                    println!("MQtt Connected !");
                    *(connceted_clone.lock().unwrap()) = true;
                }
                EventPayload::Disconnected => {
                    println!("Mqtt Disconnected !");
                }
                EventPayload::Subscribed(id) => {
                    println!("Subscribed {}",id);
                }
                _ => {}
            }
        }
    });

    while !*connected.lock().unwrap() {
        FreeRtos::delay_ms(500);
    }

    client.subscribe("topic/test", QoS::AtLeastOnce);
    return Ok(client);
}

fn main() -> Result<(), EspError> {
    esp_idf_sys::link_patches();
    let perf = Peripherals::take().unwrap();
    let sys_loop = EspSystemEventLoop::take()?;
    let nvs = EspDefaultNvsPartition::take()?;

    let mut wifi = BlockingWifi::wrap(
        EspWifi::new(perf.modem, sys_loop.clone(), Some(nvs))?,
        sys_loop,
    )?;

    init_wifi(&mut wifi)?;
    let ip_info = wifi.wifi().sta_netif().get_ip_info();
    println!("WiFi DHCP IP: {:?}", ip_info);

    // Initialize MQTT
    let mut client = init_mqtt()?;

    // --- Main loop: non-blocking events + periodic publish + ping ---
    let mut counter = 0;
    loop {
        let payload = b"Hello from esp";
        println!("Published: {:?}",&payload.to_ascii_uppercase());
        match client.publish("topic/test", QoS::AtLeastOnce, true, payload) {
            Ok(_) => {},
            Err(e) => println!("Publish error: {:?}", e),
        }
        FreeRtos::delay_ms(500);
    }
}
