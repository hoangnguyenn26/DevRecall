import { get } from './httpClient'

export interface SystemInfo {
  applicationName: string
  version: string
  environment: string
  currentTimeUtc: string
}

export function getSystemInfo() {
  return get<SystemInfo>('/system/info')
}
