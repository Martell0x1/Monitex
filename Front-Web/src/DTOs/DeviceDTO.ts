export interface DeviceDto {
  id: number | string;
  name: string;
  type: string;
  location: string;
  ipAddress: string;
  description: string;
  status?: string;
}
