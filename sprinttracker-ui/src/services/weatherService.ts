import api from '@/lib/api';
import { LocationSuggestion, WeatherReport } from '@/types';

export async function geocode(place: string, limit = 5): Promise<LocationSuggestion[]> {
  if (!place || place.trim().length === 0) return [];
  try {
    const res = await api.get(`/weather/geocode`, { params: { place, limit } });
    if (!res.data?.success) throw new Error(res.data?.message || 'Failed to fetch location suggestions');
    return res.data.data as LocationSuggestion[];
  } catch (err: any) {
    // Try extract server-provided message
    const msg = err?.response?.data?.message || err?.message || 'Failed to fetch location suggestions';
    throw new Error(msg);
  }
}

export async function getWeatherByCoords(lat: number, lon: number, units: 'metric' | 'imperial' = 'metric'): Promise<WeatherReport> {
  try {
    const res = await api.get(`/weather/report`, { params: { lat, lon, units } });
    if (!res.data?.success) throw new Error(res.data?.message || 'Failed to fetch weather report');
    return res.data.data as WeatherReport;
  } catch (err: any) {
    const msg = err?.response?.data?.message || err?.message || 'Failed to fetch weather report from server';
    throw new Error(msg);
  }
}
