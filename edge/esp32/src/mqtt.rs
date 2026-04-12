use esp_idf_hal::delay::FreeRtos;
use esp_idf_svc::mqtt::client::{
    EspMqttClient, EventPayload, MqttClientConfiguration, QoS,
};
use esp_idf_sys::EspError;

use std::sync::{Arc, Mutex};

use crate::dns::resolve_mdns;


pub fn init_mqtt() -> Result<EspMqttClient<'static>, EspError> {

    let config = MqttClientConfiguration {
        client_id: "esp_client".into(),
        ..Default::default()
    };
    let ip = resolve_mdns("monitex")
    .unwrap();

    let broker = format!("mqtt://{}:1883", ip);

    let (mut client, mut conn) =
        EspMqttClient::new(&broker, &config)?;

    let connected = Arc::new(Mutex::new(false));
    let connected_clone = connected.clone();

    std::thread::spawn(move || {

        println!("MQTT Event Loop thread Started!");

        while let Ok(event) = conn.next() {

            match event.payload() {

                EventPayload::Connected(_) => {

                    println!("MQTT Connected!");

                    *connected_clone.lock().unwrap() = true;
                }

                EventPayload::Disconnected => {

                    println!("MQTT Disconnected!");
                }

                EventPayload::Subscribed(id) => {

                    println!("Subscribed {}", id);
                }

                _ => {}
            }
        }
    });

    while !*connected.lock().unwrap() {

        FreeRtos::delay_ms(500);
    }

    client.subscribe("topic/test", QoS::AtLeastOnce)?;

    Ok(client)
}
