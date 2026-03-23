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

use Wifi::init_wifi;
use mqtt::init_mqtt;
use ping::check_pc;

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

    let ip_info = wifi.wifi().sta_netif().get_ip_info();

    println!("WiFi DHCP IP: {:?}", ip_info);

    let mut client = init_mqtt()?;

    let mut counter = 0;

    loop {
        let adc_value = ir.read()?;
        let payload = adc_value.to_string();

        println!(
            "Published: {:?}",
            payload
        );

        match client.publish(
            "topic/test",
            esp_idf_svc::mqtt::client::QoS::AtLeastOnce,
            true,
            &payload.as_bytes(),
        ) {

            Ok(_) => {}

            Err(e) => println!("Publish error: {:?}", e),
        }

        FreeRtos::delay_ms(500);

        counter += 1;
    }
}
