use esp_idf_hal::adc::oneshot::{
    config::AdcChannelConfig, AdcChannelDriver, AdcDriver,
};
use esp_idf_hal::adc::{ADC1, ADCCH6, ADCU1, attenuation};
use esp_idf_hal::gpio::Gpio34;
use esp_idf_sys::EspError;

pub struct IrSensor<'a> {
    channel: AdcChannelDriver<'a, ADCCH6<ADCU1>, AdcDriver<'a, ADCU1>>,
}

impl<'a> IrSensor<'a> {
    pub fn new(adc1: ADC1<'a>, pin: Gpio34<'a>) -> Result<Self, EspError> {
        let adc = AdcDriver::new(adc1)?;
        let config = AdcChannelConfig{
          attenuation:attenuation::DB_12,
          ..Default::default()
        };

        let channel = AdcChannelDriver::new(adc, pin, &config)?;

        Ok(Self { channel })
    }

    pub fn read(&mut self) -> Result<u16, EspError> {
        self.channel.read()
    }
}
