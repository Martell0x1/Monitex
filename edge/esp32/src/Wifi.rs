use esp_idf_svc::wifi::{
    AuthMethod, BlockingWifi, ClientConfiguration, Configuration, EspWifi,
};
use esp_idf_sys::EspError;

/// Initialize WiFi
pub fn init_wifi(
    wifi: &mut BlockingWifi<EspWifi<'_>>,
) -> Result<(), EspError> {

    let wifi_conf = Configuration::Client(ClientConfiguration {
        ssid: "Martell".try_into().unwrap(),
        password: "MarwanMartell@04".try_into().unwrap(),
        bssid: None,
        auth_method: AuthMethod::WPA2Personal,
        channel: None,
        ..Default::default()
    });

    wifi.set_configuration(&wifi_conf)?;
    wifi.start()?;

    println!("WiFi Started...");

    wifi.connect()?;

    println!("WiFi Connecting...");

    wifi.wait_netif_up()?;

    println!("WiFi Connected!");

    Ok(())
}
