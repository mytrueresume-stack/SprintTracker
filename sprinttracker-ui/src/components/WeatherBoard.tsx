'use client';

import { useEffect, useState } from 'react';
// simple debounce implementation to avoid adding dependency
function debounce<T extends (...args: any[]) => void>(fn: T, wait = 300) {
  let timer: ReturnType<typeof setTimeout> | null = null;
  const debounced = (...args: Parameters<T>) => {
    if (timer) clearTimeout(timer);
    timer = setTimeout(() => fn(...args), wait);
  };
  debounced.cancel = () => { if (timer) { clearTimeout(timer); timer = null; } };
  return debounced as T & { cancel: () => void };
}
import { geocode, getWeatherByCoords } from '@/services/weatherService';
import { LocationSuggestion, WeatherReport } from '@/types';
import { Card, Loader, Button, Badge } from '@/components/ui';
import { Search, Sun, Cloud, Wind, Droplet, MapPin, X } from 'lucide-react';
import { ResponsiveContainer, LineChart, Line, XAxis, Tooltip } from 'recharts';

function weatherCodeToEmoji(code?: number) {
  switch (code) {
    case 0: return '☀️';
    case 1: case 2: return '⛅';
    case 3: return '☁️';
    case 45: case 48: return '🌫️';
    case 51: case 53: case 55: return '🌦️';
    case 61: case 63: case 65: return '🌧️';
    case 71: case 73: case 75: return '❄️';
    case 80: case 81: case 82: return '🌧️';
    case 95: case 96: case 99: return '⛈️';
    default: return '🌈';
  }
}

function uviVariant(uvi?: number | null) {
  if (uvi == null) return 'default';
  if (uvi <= 2) return 'success';
  if (uvi <= 5) return 'warning';
  if (uvi <= 7) return 'danger';
  return 'danger';
}

export default function WeatherBoard() {
  const [query, setQuery] = useState('');
  const [suggestions, setSuggestions] = useState<LocationSuggestion[]>([]);
  const [isSearching, setIsSearching] = useState(false);
  const [selected, setSelected] = useState<LocationSuggestion | null>(null);
  const [report, setReport] = useState<WeatherReport | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [highlightIndex, setHighlightIndex] = useState<number>(-1);

  // Debounced search
  const search = debounce(async (value: string) => {
    setError(null);
    if (!value || value.trim().length === 0) {
      setSuggestions([]);
      setIsSearching(false);
      return;
    }
    setIsSearching(true);
    try {
      const res = await geocode(value);
      setSuggestions(res || []);
    } catch (e: any) {
      setSuggestions([]);
      setError(e?.message || 'Unable to fetch locations');
    } finally {
      setIsSearching(false);
    }
  }, 400);

  useEffect(() => {
    search(query);
    setHighlightIndex(-1);
    return () => search.cancel();
  }, [query]);

  // keyboard navigation for suggestion list
  const onInputKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    const listLength = suggestions.length;
    if (e.key === 'ArrowDown') {
      e.preventDefault();
      setHighlightIndex((prev) => (prev < listLength - 1 ? prev + 1 : 0));
    } else if (e.key === 'ArrowUp') {
      e.preventDefault();
      setHighlightIndex((prev) => (prev > 0 ? prev - 1 : listLength - 1));
    } else if (e.key === 'Enter') {
      if (highlightIndex >= 0 && highlightIndex < suggestions.length) {
        const s = suggestions[highlightIndex];
        setSelected(s);
        setSuggestions([]);
        setQuery(s.name);
      }
    } else if (e.key === 'Escape') {
      setSuggestions([]);
      setHighlightIndex(-1);
    }
  };

  useEffect(() => {
    // load last selected from localStorage
    const last = typeof window !== 'undefined' ? localStorage.getItem('weather_last_location') : null;
    if (last && !selected) {
      try {
        const parsed = JSON.parse(last) as LocationSuggestion;
        setSelected(parsed);
      } catch (e) { /* ignore */ }
    }
  }, []);

  useEffect(() => {
    if (!selected) return;
    // persist last selection
    try { localStorage.setItem('weather_last_location', JSON.stringify(selected)); } catch (e) {}

    const load = async () => {
      setIsLoading(true);
      setError(null);
      try {
        const r = await getWeatherByCoords(selected.lat, selected.lon, 'metric');
        setReport(r);
      } catch (e: any) {
        setError(e?.message || 'Failed to load weather');
        setReport(null);
      } finally {
        setIsLoading(false);
      }
    };
    load();
  }, [selected]);

  return (
    <Card>
      <div className="flex items-center space-x-3 mb-4">
        <div className="relative flex-1">
          <div className="flex items-center border rounded-md px-3 py-2">
            <Search className="h-4 w-4 text-gray-400 mr-2" />
            <input
              value={query}
              onChange={(e) => { setQuery(e.target.value); setError(null); }}
              onKeyDown={onInputKeyDown}
              aria-label="Search for place"
              placeholder="Search for a place (e.g., London, Seattle)"
              className="w-full bg-transparent outline-none text-sm"
            />

            <div className="flex items-center space-x-2">
              <Button size="sm" variant="outline" onClick={() => {
                if (navigator?.geolocation) {
                  setIsLoading(true);
                  navigator.geolocation.getCurrentPosition(async (pos) => {
                    setIsLoading(false);
                    setSelected({ name: 'My location', lat: pos.coords.latitude, lon: pos.coords.longitude });
                  }, (err) => {
                    setIsLoading(false);
                    setError('Failed to get location from browser');
                  }, { timeout: 8000 });
                } else {
                  setError('Geolocation is not supported in your browser');
                }
              }} aria-label="Use my location">
                <MapPin className="h-4 w-4 mr-2" />
                Use my location
              </Button>

              {query && (
                <Button size="sm" variant="ghost" onClick={() => { setQuery(''); setSuggestions([]); setError(null); }} aria-label="Clear search">
                  <X className="h-4 w-4" />
                </Button>
              )}

              {isSearching && <Loader size="sm" />}
            </div>

          {suggestions.length > 0 && (
            <div id="weather-suggestions" role="listbox" aria-label="Location suggestions" className="absolute mt-1 bg-white border rounded-md w-full z-40">
              {suggestions.map((s, i) => (
                <div
                  key={i}
                  role="option"
                  aria-selected={highlightIndex === i}
                  tabIndex={-1}
                  onMouseEnter={() => setHighlightIndex(i)}
                  onClick={() => { setSelected(s); setSuggestions([]); setQuery(s.name); setHighlightIndex(-1); }}
                  className={`p-2 cursor-pointer ${highlightIndex === i ? 'bg-blue-50 text-blue-700' : 'hover:bg-gray-50'}`}
                >
                  {s.name}
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>

      {isLoading ? (
        <div className="flex items-center justify-center py-8">
          <Loader />
        </div>
      ) : error ? (
        <div className="p-4 rounded-md bg-red-50 border border-red-100">
          <div className="flex items-start justify-between">
            <div className="flex-1">
              <p className="text-sm text-red-700 font-medium">{error}</p>
              <p className="text-xs text-red-600 mt-1">Please check your network or try a different location.</p>
            </div>
            <div className="ml-4">
              <Button size="sm" variant="primary" onClick={() => { setError(null); if (selected) setSelected({ ...selected }); else if (query) search(query); }}>
                Retry
              </Button>
            </div>
          </div>
        </div>
      ) : report ? (
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div className="col-span-1 p-4 bg-gradient-to-br from-blue-50 to-white rounded-md">
            <div className="flex items-center justify-between">
              <div>
                <h3 className="text-lg font-semibold">{selected?.name}</h3>
                <p className="text-sm text-gray-600">{report.timezone} • Updated {new Date(report.current.dt).toLocaleTimeString()}</p>
              </div>
              <div className="text-right flex items-center space-x-3">
                <div className="text-3xl">{report.current.weatherIcon}</div>
                <div>
                  <div className="text-3xl font-bold">{Math.round(report.current.temp)}°C</div>
                  <div className="text-sm text-gray-600">Feels like {report.current.feelsLike ? Math.round(report.current.feelsLike) + '°C' : '—'}</div>
                </div>
              </div>
            </div>

            <div className="mt-4 space-y-2 text-sm">
              <div className="flex items-center"><Cloud className="h-4 w-4 mr-2" />{report.current.weatherDescription}</div>
              <div className="flex items-center"><Droplet className="h-4 w-4 mr-2" />{report.current.humidity ?? '—'}% humidity</div>
              <div className="flex items-center"><Wind className="h-4 w-4 mr-2" />{report.current.windSpeed ?? '—'} m/s - {report.current.windDeg ?? '—'}°</div>
              <div className="flex items-center"><Sun className="h-4 w-4 mr-2" />Sunrise: {report.current.sunrise ? new Date(report.current.sunrise).toLocaleTimeString() : '—'} • Sunset: {report.current.sunset ? new Date(report.current.sunset).toLocaleTimeString() : '—'}</div>
              <div className="flex items-center text-sm text-gray-600 mt-2 space-x-4">
                <div className="mr-4">Pressure: <strong>{report.current.pressure ?? '—'} hPa</strong></div>
                <div className="flex items-center space-x-2">
                  <span className="text-sm text-gray-600">UV:</span>
                  <Badge variant={uviVariant(report.current.uvi)}>{report.current.uvi ?? '—'}</Badge>
                </div>
              </div>
            </div>
          </div>

          <div className="col-span-2 p-4 bg-white rounded-md">
            <h4 className="text-sm font-medium mb-2">Next 24 hours</h4>
            <div className="h-40">
              <ResponsiveContainer width="100%" height="100%">
                <LineChart data={report.hourly.map(h => ({ time: new Date(h.dt).toLocaleTimeString([], {hour: '2-digit', minute:'2-digit'}), temp: Math.round(h.temp), pop: h.pop ? Math.round((h.pop||0)*100) : 0 }))}>
                  <XAxis dataKey="time" tick={{fontSize: 10}} />
                  <Tooltip content={({ active, payload }) => {
                    if (!active || !payload || !payload.length) return null;
                    const p = payload[0].payload;
                    return (
                      <div className="bg-white p-2 border rounded shadow text-sm">
                        <div><strong>{p.time}</strong></div>
                        <div>Temp: {p.temp}°C</div>
                        <div>Precip: {p.pop}%</div>
                      </div>
                    );
                  }} />
                  <Line type="monotone" dataKey="temp" stroke="#3B82F6" strokeWidth={2} dot={false} />
                </LineChart>
              </ResponsiveContainer>
            </div>

            <div className="mt-4 grid grid-cols-2 md:grid-cols-4 gap-2">
              {report.daily.map((d, idx) => (
                <div key={idx} className="p-2 bg-gray-50 rounded text-center">
                  <div className="text-sm font-medium">{new Date(d.dt).toLocaleDateString(undefined, { weekday: 'short' })}</div>
                  <div className="mx-auto h-8 w-8 text-xl">{weatherCodeToEmoji(d.weatherCode)}</div>
                  <div className="text-sm">{Math.round(d.tempMax)}° / {Math.round(d.tempMin)}°</div>
                  <div className="text-xs text-gray-500">{d.pop ? Math.round((d.pop||0)*100) + '%' : '—'}</div>
                </div>
              ))}
            </div>
          </div>
        </div>
      ) : (
        <div className="text-sm text-gray-500">Enter a location to view the weather report</div>
      )}
    </Card>
  );
}
