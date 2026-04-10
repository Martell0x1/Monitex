export interface SensorSummaryDto {
  sensorId: number;
  deviceId: number;
  name: string;
  type: string;
  location: string;
  ipAddress?: string | null;
  description?: string | null;
  createdAt: string;
}
