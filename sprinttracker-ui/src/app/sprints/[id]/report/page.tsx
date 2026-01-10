'use client';

import { useEffect, useState } from 'react';
import { useParams } from 'next/navigation';
import Link from 'next/link';
import { sprintService } from '@/services/sprintService';
import { sprintSubmissionService } from '@/services/sprintSubmissionService';
import { Sprint, SprintReportData } from '@/types';
import { Card, Loader, Badge, StatCard, EmptyState } from '@/components/ui';
import { ArrowLeft, Users, Target, Clock, AlertTriangle, Award, TrendingUp, CheckCircle } from 'lucide-react';
import { format } from 'date-fns';
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, PieChart, Pie, Cell, Legend } from 'recharts';

const COLORS = ['#3B82F6', '#10B981', '#F59E0B', '#EF4444', '#8B5CF6', '#EC4899'];

export default function SprintReportPage() {
  const params = useParams();
  const sprintId = params.id as string;

  const [sprint, setSprint] = useState<Sprint | null>(null);
  const [report, setReport] = useState<SprintReportData | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    if (sprintId) {
      loadData();
    }
  }, [sprintId]);

  const loadData = async () => {
    try {
      const [sprintRes, reportRes] = await Promise.all([
        sprintService.getSprint(sprintId),
        sprintSubmissionService.getSprintReport(sprintId),
      ]);

      if (sprintRes.success && sprintRes.data) {
        setSprint(sprintRes.data);
      }

      if (reportRes.success && reportRes.data) {
        setReport(reportRes.data);
      }
    } catch (err) {
      setError('Failed to load report data');
      console.error(err);
    } finally {
      setIsLoading(false);
    }
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-[60vh]">
        <Loader size="lg" />
      </div>
    );
  }

  if (!sprint) {
    return (
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <div className="p-4 bg-red-50 border border-red-200 rounded-lg text-red-700">
          Sprint not found
        </div>
      </div>
    );
  }

  const storyPointsData = report?.userBreakdown?.map(user => ({
    name: user.userName.split(' ')[0],
    planned: user.storyPointsPlanned,
    completed: user.storyPointsCompleted,
  })) || [];

  const hoursData = report?.userBreakdown?.map(user => ({
    name: user.userName.split(' ')[0],
    hours: user.hoursWorked,
  })) || [];

  const statusData = [
    { name: 'Completed', value: report?.totalStoryPointsCompleted || 0 },
    { name: 'Remaining', value: (report?.totalStoryPointsPlanned || 0) - (report?.totalStoryPointsCompleted || 0) },
  ].filter(d => d.value > 0);

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      <div className="mb-6">
        <Link href={`/projects/${sprint.projectId}`} className="inline-flex items-center text-sm text-gray-500 hover:text-gray-700 mb-4">
          <ArrowLeft className="h-4 w-4 mr-1" />
          Back to Project
        </Link>

        <div className="flex items-start justify-between">
          <div>
            <h1 className="text-2xl font-bold text-gray-900">Sprint Report</h1>
            <p className="text-gray-600 mt-1">{sprint.name} - Sprint #{sprint.sprintNumber}</p>
            <p className="text-sm text-gray-500 mt-1">
              {format(new Date(sprint.startDate), 'MMM d')} - {format(new Date(sprint.endDate), 'MMM d, yyyy')}
            </p>
          </div>
        </div>
      </div>

      {error && (
        <div className="mb-6 p-4 bg-red-50 border border-red-200 rounded-lg text-red-700">
          {error}
        </div>
      )}

      {!report ? (
        <Card>
          <EmptyState
            title="No submissions yet"
            description="Team members haven't submitted their work reports for this sprint"
            icon={<Users className="h-8 w-8 text-gray-400" />}
          />
        </Card>
      ) : (
        <div className="space-y-6">
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
            <StatCard
              title="Team Members"
              value={report.totalTeamMembers}
              icon={<Users className="h-6 w-6 text-blue-600" />}
            />
            <StatCard
              title="Story Points Completed"
              value={`${report.totalStoryPointsCompleted} / ${report.totalStoryPointsPlanned}`}
              icon={<Target className="h-6 w-6 text-green-600" />}
            />
            <StatCard
              title="Total Hours Worked"
              value={report.totalHoursWorked}
              icon={<Clock className="h-6 w-6 text-purple-600" />}
            />
            <StatCard
              title="Completion Rate"
              value={`${report.completionPercentage}%`}
              icon={<TrendingUp className="h-6 w-6 text-yellow-600" />}
            />
          </div>

          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            <Card>
              <h2 className="text-lg font-semibold text-gray-900 mb-4">Story Points by Team Member</h2>
              {storyPointsData.length > 0 ? (
                <div className="h-64">
                  <ResponsiveContainer width="100%" height="100%">
                    <BarChart data={storyPointsData}>
                      <CartesianGrid strokeDasharray="3 3" />
                      <XAxis dataKey="name" />
                      <YAxis />
                      <Tooltip />
                      <Legend />
                      <Bar dataKey="planned" fill="#93C5FD" name="Planned" />
                      <Bar dataKey="completed" fill="#3B82F6" name="Completed" />
                    </BarChart>
                  </ResponsiveContainer>
                </div>
              ) : (
                <p className="text-gray-500 text-sm">No data available</p>
              )}
            </Card>

            <Card>
              <h2 className="text-lg font-semibold text-gray-900 mb-4">Sprint Completion</h2>
              {statusData.length > 0 ? (
                <div className="h-64">
                  <ResponsiveContainer width="100%" height="100%">
                    <PieChart>
                      <Pie
                        data={statusData}
                        cx="50%"
                        cy="50%"
                        innerRadius={60}
                        outerRadius={80}
                        paddingAngle={5}
                        dataKey="value"
                        label={({ name, percent }) => `${name} ${(percent * 100).toFixed(0)}%`}
                      >
                        {statusData.map((entry, index) => (
                          <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                        ))}
                      </Pie>
                      <Tooltip />
                    </PieChart>
                  </ResponsiveContainer>
                </div>
              ) : (
                <p className="text-gray-500 text-sm">No data available</p>
              )}
            </Card>
          </div>

          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            <Card>
              <h2 className="text-lg font-semibold text-gray-900 mb-4">Hours Worked by Team Member</h2>
              {hoursData.length > 0 ? (
                <div className="h-64">
                  <ResponsiveContainer width="100%" height="100%">
                    <BarChart data={hoursData} layout="vertical">
                      <CartesianGrid strokeDasharray="3 3" />
                      <XAxis type="number" />
                      <YAxis dataKey="name" type="category" width={80} />
                      <Tooltip />
                      <Bar dataKey="hours" fill="#8B5CF6" name="Hours" />
                    </BarChart>
                  </ResponsiveContainer>
                </div>
              ) : (
                <p className="text-gray-500 text-sm">No data available</p>
              )}
            </Card>

            <Card>
              <h2 className="text-lg font-semibold text-gray-900 mb-4">Quick Stats</h2>
              <div className="space-y-4">
                <div className="flex items-center justify-between p-3 bg-blue-50 rounded-lg">
                  <div className="flex items-center">
                    <CheckCircle className="h-5 w-5 text-blue-600 mr-2" />
                    <span className="text-blue-700">User Stories</span>
                  </div>
                  <span className="font-semibold text-blue-700">{report.totalUserStories}</span>
                </div>
                <div className="flex items-center justify-between p-3 bg-green-50 rounded-lg">
                  <div className="flex items-center">
                    <Target className="h-5 w-5 text-green-600 mr-2" />
                    <span className="text-green-700">Features Delivered</span>
                  </div>
                  <span className="font-semibold text-green-700">{report.totalFeatures}</span>
                </div>
                <div className="flex items-center justify-between p-3 bg-yellow-50 rounded-lg">
                  <div className="flex items-center">
                    <AlertTriangle className="h-5 w-5 text-yellow-600 mr-2" />
                    <span className="text-yellow-700">Impediments</span>
                  </div>
                  <span className="font-semibold text-yellow-700">{report.totalImpediments} ({report.openImpediments} open)</span>
                </div>
                <div className="flex items-center justify-between p-3 bg-purple-50 rounded-lg">
                  <div className="flex items-center">
                    <Award className="h-5 w-5 text-purple-600 mr-2" />
                    <span className="text-purple-700">Appreciations</span>
                  </div>
                  <span className="font-semibold text-purple-700">{report.totalAppreciations}</span>
                </div>
              </div>
            </Card>
          </div>

          <Card>
            <h2 className="text-lg font-semibold text-gray-900 mb-4">Team Member Breakdown</h2>
            <div className="overflow-x-auto">
              <table className="min-w-full divide-y divide-gray-200">
                <thead className="bg-gray-50">
                  <tr>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Name</th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Story Points</th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Hours</th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Stories</th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Features</th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Status</th>
                  </tr>
                </thead>
                <tbody className="bg-white divide-y divide-gray-200">
                  {report.userBreakdown?.map((user) => (
                    <tr key={user.userId}>
                      <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">{user.userName}</td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                        {user.storyPointsCompleted} / {user.storyPointsPlanned}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">{user.hoursWorked}h</td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">{user.userStoriesCount}</td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">{user.featuresCount}</td>
                      <td className="px-6 py-4 whitespace-nowrap">
                        <Badge variant={user.submissionStatus === 'Submitted' ? 'success' : 'warning'}>
                          {user.submissionStatus}
                        </Badge>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </Card>

          {report.impediments && report.impediments.length > 0 && (
            <Card>
              <h2 className="text-lg font-semibold text-gray-900 mb-4">Impediments</h2>
              <div className="space-y-3">
                {report.impediments.map((impediment, index) => (
                  <div key={index} className="p-4 bg-gray-50 rounded-lg">
                    <div className="flex items-start justify-between">
                      <div>
                        <p className="text-gray-900">{impediment.description}</p>
                        <p className="text-sm text-gray-500 mt-1">
                          Reported by {impediment.reportedBy} - {impediment.category}
                        </p>
                      </div>
                      <Badge variant={impediment.status === 'Resolved' ? 'success' : 'warning'}>
                        {impediment.status}
                      </Badge>
                    </div>
                    {impediment.resolution && (
                      <p className="text-sm text-green-600 mt-2">Resolution: {impediment.resolution}</p>
                    )}
                  </div>
                ))}
              </div>
            </Card>
          )}

          {report.appreciations && report.appreciations.length > 0 && (
            <Card>
              <h2 className="text-lg font-semibold text-gray-900 mb-4">Team Appreciations</h2>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {report.appreciations.map((appreciation, index) => (
                  <div key={index} className="p-4 bg-purple-50 rounded-lg">
                    <div className="flex items-center mb-2">
                      <Award className="h-5 w-5 text-purple-600 mr-2" />
                      <span className="font-medium text-purple-900">{appreciation.appreciatedUserName}</span>
                    </div>
                    <p className="text-purple-700 text-sm">{appreciation.reason}</p>
                    <p className="text-purple-500 text-xs mt-2">
                      From {appreciation.givenBy} - {appreciation.category}
                    </p>
                  </div>
                ))}
              </div>
            </Card>
          )}
        </div>
      )}
    </div>
  );
}
