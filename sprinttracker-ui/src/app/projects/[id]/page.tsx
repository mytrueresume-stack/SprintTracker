'use client';

import { useEffect, useState } from 'react';
import { useParams } from 'next/navigation';
import Link from 'next/link';
import { projectService } from '@/services/projectService';
import { sprintService } from '@/services/sprintService';
import { Project, Sprint, SprintStatus } from '@/types';
import { Card, Button, Loader, Badge, EmptyState, Modal, Input, Textarea } from '@/components/ui';
import { ArrowLeft, Plus, Zap, Calendar, Play, CheckCircle, FileText, BarChart3 } from 'lucide-react';
import { format } from 'date-fns';

export default function ProjectDetailPage() {
  const params = useParams();
  const projectId = params.id as string;

  const [project, setProject] = useState<Project | null>(null);
  const [sprints, setSprints] = useState<Sprint[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [isCreating, setIsCreating] = useState(false);
  const [formData, setFormData] = useState({
    name: '',
    goal: '',
    startDate: '',
    endDate: '',
  });

  useEffect(() => {
    if (projectId) {
      loadProjectData();
    }
  }, [projectId]);

  const loadProjectData = async () => {
    try {
      const [projectRes, sprintsRes] = await Promise.all([
        projectService.getProject(projectId),
        sprintService.getSprintsByProject(projectId, 1, 50),
      ]);

      if (projectRes.success && projectRes.data) {
        setProject(projectRes.data);
      } else {
        setError('Failed to load project');
      }

      if (sprintsRes.success && sprintsRes.data) {
        setSprints(sprintsRes.data.items);
      }
    } catch (err) {
      setError('Failed to load project data');
      console.error(err);
    } finally {
      setIsLoading(false);
    }
  };

  const handleCreateSprint = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsCreating(true);

    try {
      const response = await sprintService.createSprint({
        projectId,
        name: formData.name,
        goal: formData.goal || undefined,
        startDate: formData.startDate,
        endDate: formData.endDate,
      });

      if (response.success && response.data) {
        setSprints([response.data, ...sprints]);
        setIsModalOpen(false);
        setFormData({ name: '', goal: '', startDate: '', endDate: '' });
      } else {
        setError(response.message || 'Failed to create sprint');
      }
    } catch (err) {
      setError('Failed to create sprint');
      console.error(err);
    } finally {
      setIsCreating(false);
    }
  };

  const handleStartSprint = async (sprintId: string) => {
    try {
      const response = await sprintService.startSprint(sprintId);
      if (response.success && response.data) {
        setSprints(sprints.map(s => s.id === sprintId ? response.data! : s));
      }
    } catch (err) {
      console.error('Failed to start sprint:', err);
    }
  };

  const handleCompleteSprint = async (sprintId: string) => {
    try {
      const response = await sprintService.completeSprint(sprintId);
      if (response.success && response.data) {
        setSprints(sprints.map(s => s.id === sprintId ? response.data! : s));
      }
    } catch (err) {
      console.error('Failed to complete sprint:', err);
    }
  };

  const getStatusBadge = (status: SprintStatus) => {
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

  if (!project) {
    return (
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <div className="p-4 bg-red-50 border border-red-200 rounded-lg text-red-700">
          Project not found
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      <div className="mb-6">
        <Link href="/projects" className="inline-flex items-center text-sm text-gray-500 hover:text-gray-700 mb-4">
          <ArrowLeft className="h-4 w-4 mr-1" />
          Back to Projects
        </Link>

        <div className="flex items-start justify-between">
          <div>
            <div className="flex items-center">
              <h1 className="text-2xl font-bold text-gray-900">{project.name}</h1>
              <span className="ml-3 px-2 py-1 bg-gray-100 text-gray-600 text-sm font-medium rounded">
                {project.key}
              </span>
            </div>
            {project.description && (
              <p className="text-gray-600 mt-2">{project.description}</p>
            )}
          </div>
          <Button onClick={() => setIsModalOpen(true)}>
            <Plus className="h-4 w-4 mr-2" />
            New Sprint
          </Button>
        </div>
      </div>

      {error && (
        <div className="mb-6 p-4 bg-red-50 border border-red-200 rounded-lg text-red-700">
          {error}
        </div>
      )}

      <div className="mb-8">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Sprints</h2>

        {sprints.length > 0 ? (
          <div className="space-y-4">
            {sprints.map((sprint) => (
              <Card key={sprint.id} className="hover:shadow-md transition-shadow">
                <div className="flex items-start justify-between">
                  <div className="flex items-start">
                    <div className="p-2 bg-blue-50 rounded-lg mr-4">
                      <Zap className="h-5 w-5 text-blue-600" />
                    </div>
                    <div>
                      <div className="flex items-center">
                        <h3 className="font-semibold text-gray-900">{sprint.name}</h3>
                        <span className="ml-2 text-sm text-gray-500">Sprint #{sprint.sprintNumber}</span>
                      </div>
                      {sprint.goal && (
                        <p className="text-sm text-gray-600 mt-1">{sprint.goal}</p>
                      )}
                      <div className="flex items-center mt-2 text-sm text-gray-500">
                        <Calendar className="h-4 w-4 mr-1" />
                        {format(new Date(sprint.startDate), 'MMM d')} - {format(new Date(sprint.endDate), 'MMM d, yyyy')}
                      </div>
                    </div>
                  </div>

                  <div className="flex items-center space-x-3">
                    {getStatusBadge(sprint.status)}
                  </div>
                </div>

                <div className="mt-4 pt-4 border-t border-gray-100 flex items-center justify-between">
                  <div className="flex items-center space-x-4 text-sm">
                    <span className="text-gray-500">
                      {sprint.stats?.completedStoryPoints || 0} / {sprint.stats?.totalStoryPoints || 0} story points
                    </span>
                    <span className="text-gray-500">
                      {sprint.stats?.completionPercentage || 0}% complete
                    </span>
                  </div>

                  <div className="flex items-center space-x-2">
                    {sprint.status === SprintStatus.Planning && (
                      <Button size="sm" variant="outline" onClick={() => handleStartSprint(sprint.id)}>
                        <Play className="h-4 w-4 mr-1" />
                        Start
                      </Button>
                    )}
                    {sprint.status === SprintStatus.Active && (
                      <>
                        <Link href={`/sprints/${sprint.id}/submit`}>
                          <Button size="sm" variant="outline">
                            <FileText className="h-4 w-4 mr-1" />
                            Submit Work
                          </Button>
                        </Link>
                        <Button size="sm" variant="outline" onClick={() => handleCompleteSprint(sprint.id)}>
                          <CheckCircle className="h-4 w-4 mr-1" />
                          Complete
                        </Button>
                      </>
                    )}
                    <Link href={`/sprints/${sprint.id}/report`}>
                      <Button size="sm" variant="ghost">
                        <BarChart3 className="h-4 w-4 mr-1" />
                        Report
                      </Button>
                    </Link>
                  </div>
                </div>
              </Card>
            ))}
          </div>
        ) : (
          <Card>
            <EmptyState
              title="No sprints yet"
              description="Create your first sprint to start tracking work"
              icon={<Zap className="h-8 w-8 text-gray-400" />}
              action={
                <Button onClick={() => setIsModalOpen(true)}>
                  <Plus className="h-4 w-4 mr-2" />
                  Create Sprint
                </Button>
              }
            />
          </Card>
        )}
      </div>

      <Modal isOpen={isModalOpen} onClose={() => setIsModalOpen(false)} title="Create New Sprint" size="md">
        <form onSubmit={handleCreateSprint} className="space-y-4">
          <Input
            id="name"
            label="Sprint Name"
            value={formData.name}
            onChange={(e) => setFormData({ ...formData, name: e.target.value })}
            required
            placeholder="Sprint 1"
          />

          <Textarea
            id="goal"
            label="Sprint Goal"
            value={formData.goal}
            onChange={(e) => setFormData({ ...formData, goal: e.target.value })}
            placeholder="What do you want to achieve in this sprint?"
            rows={3}
          />

          <div className="grid grid-cols-2 gap-4">
            <Input
              id="startDate"
              type="date"
              label="Start Date"
              value={formData.startDate}
              onChange={(e) => setFormData({ ...formData, startDate: e.target.value })}
              required
            />

            <Input
              id="endDate"
              type="date"
              label="End Date"
              value={formData.endDate}
              onChange={(e) => setFormData({ ...formData, endDate: e.target.value })}
              required
            />
          </div>

          <div className="flex justify-end space-x-3 pt-4">
            <Button type="button" variant="outline" onClick={() => setIsModalOpen(false)}>
              Cancel
            </Button>
            <Button type="submit" isLoading={isCreating}>
              Create Sprint
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
}
