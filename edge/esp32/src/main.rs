use esp_idf_hal::delay::FreeRtos;
use esp_idf_hal::peripherals::Peripherals;

use esp_idf_svc::eventloop::EspSystemEventLoop;
use esp_idf_svc::netif::EspNetif;
use esp_idf_svc::nvs::EspDefaultNvsPartition;
use esp_idf_svc::wifi::{BlockingWifi, EspWifi};

use esp_idf_sys::{self as _, EspError};

mod Wifi;
mod mqtt;
mod ping;
mod adc;
mod payload;
mod health;

use Wifi::init_wifi;
use mqtt::init_mqtt;
use ping::check_pc;

const DEVICE_NAME: &str = "esp32-1";

fn main() -> Result<(), EspError> {

    esp_idf_sys::link_patches();

    let peripherals = Peripherals::take().unwrap();

    let Peripherals {
        modem,
        adc1,
        pins,
        ..
    } = peripherals;

    let gpio34 = pins.gpio34;
    let mut ir = adc::IrSensor::new(adc1, gpio34)?;

    let sys_loop = EspSystemEventLoop::take()?;

    let nvs = EspDefaultNvsPartition::take()?;

    let mut wifi = BlockingWifi::wrap(
        EspWifi::new(modem, sys_loop.clone(), Some(nvs))?,
        sys_loop,
    )?;

    init_wifi(&mut wifi)?;

    let ip_info = wifi.wifi().sta_netif().get_ip_info()?;
    let ip_address = ip_info.ip.to_string();

    println!("WiFi DHCP IP: {}", ip_address);

    let mut client = init_mqtt()?;
    let mut heartbeat_counter: u8 = 0;

    loop {
        let adc_value = ir.read()?;
        let sensor_payload = payload::build_ldr_payload(DEVICE_NAME, adc_value, Some(&ip_address));

        println!(
            "Published: {:?}",
            sensor_payload
        );

        let sensor_publish_result = client.publish(
            "topic/test",
            esp_idf_svc::mqtt::client::QoS::AtLeastOnce,
            true,
            &sensor_payload.as_bytes(),
        );

        match sensor_publish_result {
            Ok(_) => {}
            Err(e) => println!("Publish error: {:?}", e),
        }

        heartbeat_counter = heartbeat_counter.wrapping_add(1);

        if heartbeat_counter >= 10 {
            heartbeat_counter = 0;

            let health_snapshot = health::collect(&ip_address, sensor_publish_result.is_ok());
            let health_payload = health::build_payload(DEVICE_NAME, &health_snapshot);

            println!("Health heartbeat: {:?}", health_payload);

            match client.publish(
                "topic/test",
                esp_idf_svc::mqtt::client::QoS::AtLeastOnce,
                true,
                &health_payload.as_bytes(),
            ) {
                Ok(_) => {}
                Err(e) => println!("Health publish error: {:?}", e),
            }
        }

        FreeRtos::delay_ms(500);
    }
}
