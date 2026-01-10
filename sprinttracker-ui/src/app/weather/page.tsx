'use client';

import WeatherBoard from '@/components/WeatherBoard';
import { Card } from '@/components/ui';

export default function WeatherPage() {
  return (
    <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      <div className="mb-4">
        <h1 className="text-2xl font-bold">Weather Report</h1>
        <p className="text-gray-600 mt-1">Search for a place to see a full weather report.</p>
      </div>

      <Card>
        <WeatherBoard />
      </Card>
    </div>
  );
}
