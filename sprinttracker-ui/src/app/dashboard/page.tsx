'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { dashboardService } from '@/services/dashboardService';
import { DashboardStats, SprintStatus } from '@/types';
import { Card, StatCard, Loader, Badge, EmptyState } from '@/components/ui';
import { FolderKanban, Zap, CheckCircle, Clock, ArrowRight } from 'lucide-react';
import { format } from 'date-fns';

export default function DashboardPage() {
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    loadDashboard();
  }, []);

  const loadDashboard = async () => {
    try {
      const response = await dashboardService.getDashboardStats();
      if (response.success && response.data) {
        setStats(response.data);
      } else {
        setError(response.message || 'Failed to load dashboard');
      }
    } catch (err) {
      setError('Failed to load dashboard data');
      console.error(err);
    } finally {
      setIsLoading(false);
    }
  };

  const getSprintStatusBadge = (status: SprintStatus) => {
    const statusConfig = {
      [SprintStatus.Planning]: { variant: 'default' as const, label: 'Planning' },
      [SprintStatus.Active]: { variant: 'info' as const, label: 'Active' },
      [SprintStatus.Completed]: { variant: 'success' as const, label: 'Completed' },
      [SprintStatus.Cancelled]: { variant: 'danger' as const, label: 'Cancelled' },
    };
    const config = statusConfig[status] || statusConfig[SprintStatus.Planning];
    return <Badge variant={config.variant}>{config.label}</Badge>;
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-[60vh]">
        <Loader size="lg" />
      </div>
    );
  }

  if (error) {
    return (
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <div className="p-4 bg-red-50 border border-red-200 rounded-lg text-red-700">
          {error}
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      <div className="mb-8">
        <h1 className="text-2xl font-bold text-gray-900">Dashboard</h1>
        <p className="text-gray-600 mt-1">Overview of your sprint activities</p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
        <StatCard
          title="Total Projects"
          value={stats?.totalProjects || 0}
          icon={<FolderKanban className="h-6 w-6 text-blue-600" />}
        />
        <StatCard
          title="Active Sprints"
          value={stats?.activeSprints || 0}
          icon={<Zap className="h-6 w-6 text-yellow-600" />}
        />
        <StatCard
          title="Tasks Completed"
          value={stats?.tasksCompleted || 0}
          icon={<CheckCircle className="h-6 w-6 text-green-600" />}
        />
        <StatCard
          title="Tasks In Progress"
          value={stats?.tasksInProgress || 0}
          icon={<Clock className="h-6 w-6 text-purple-600" />}
        />
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <Card>
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-lg font-semibold text-gray-900">Recent Sprints</h2>
            <Link href="/projects" className="text-sm text-blue-600 hover:text-blue-700 flex items-center">
              View all <ArrowRight className="h-4 w-4 ml-1" />
            </Link>
          </div>

          {stats?.recentSprints && stats.recentSprints.length > 0 ? (
            <div className="space-y-4">
              {stats.recentSprints.map((sprint) => (
                <div
                  key={sprint.id}
                  className="p-4 bg-gray-50 rounded-lg border border-gray-100"
                >
                  <div className="flex items-start justify-between">
                    <div className="flex-1">
                      <h3 className="font-medium text-gray-900">{sprint.name}</h3>
                      <p className="text-sm text-gray-500 mt-1">{sprint.projectName}</p>
                    </div>
                    {getSprintStatusBadge(sprint.status)}
                  </div>
                  <div className="mt-3 flex items-center justify-between text-sm">
                    <span className="text-gray-500">
                      Ends: {format(new Date(sprint.endDate), 'MMM d, yyyy')}
                    </span>
                    {sprint.status === SprintStatus.Active && (
                      <span className={`font-medium ${sprint.daysRemaining <= 2 ? 'text-red-600' : 'text-gray-700'}`}>
                        {sprint.daysRemaining} days left
                      </span>
                    )}
                  </div>
                  <div className="mt-2">
                    <div className="flex items-center justify-between text-sm mb-1">
                      <span className="text-gray-500">Progress</span>
                      <span className="font-medium text-gray-700">{sprint.completionPercentage}%</span>
                    </div>
                    <div className="w-full bg-gray-200 rounded-full h-2">
                      <div
                        className="bg-blue-600 h-2 rounded-full transition-all"
                        style={{ width: `${sprint.completionPercentage}%` }}
                      />
                    </div>
                  </div>
                </div>
              ))}
            </div>
          ) : (
            <EmptyState
              title="No sprints yet"
              description="Create a project and start your first sprint"
              icon={<Zap className="h-8 w-8 text-gray-400" />}
            />
          )}
        </Card>

        <Card>
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-lg font-semibold text-gray-900">Quick Stats</h2>
          </div>

          <div className="space-y-4">
            <div className="p-4 bg-blue-50 rounded-lg">
              <div className="flex items-center justify-between">
                <span className="text-blue-700 font-medium">Total Tasks</span>
                <span className="text-2xl font-bold text-blue-700">{stats?.totalTasks || 0}</span>
              </div>
            </div>

            <div className="p-4 bg-yellow-50 rounded-lg">
              <div className="flex items-center justify-between">
                <span className="text-yellow-700 font-medium">Tasks Blocked</span>
                <span className="text-2xl font-bold text-yellow-700">{stats?.tasksBlocked || 0}</span>
              </div>
            </div>

            <div className="p-4 bg-green-50 rounded-lg">
              <div className="flex items-center justify-between">
                <span className="text-green-700 font-medium">Completion Rate</span>
                <span className="text-2xl font-bold text-green-700">
                  {stats?.totalTasks ? Math.round((stats.tasksCompleted / stats.totalTasks) * 100) : 0}%
                </span>
              </div>
            </div>

            <div className="p-4 bg-purple-50 rounded-lg">
              <div className="flex items-center justify-between">
                <span className="text-purple-700 font-medium">Active Projects</span>
                <span className="text-2xl font-bold text-purple-700">{stats?.totalProjects || 0}</span>
              </div>
            </div>
          </div>
        </Card>
      </div>
    </div>
  );
}
