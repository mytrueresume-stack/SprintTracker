'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { projectService } from '@/services/projectService';
import { Project, ProjectStatus, UserRole } from '@/types';
import { useAuthStore } from '@/store/authStore';
import { Card, Button, Loader, Badge, EmptyState, Modal, Input, Textarea } from '@/components/ui';
import { Plus, FolderKanban, Users, Calendar, ArrowRight } from 'lucide-react';
import { format } from 'date-fns';

export default function ProjectsPage() {
  const [projects, setProjects] = useState<Project[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [isCreating, setIsCreating] = useState(false);
  const [formData, setFormData] = useState({
    name: '',
    key: '',
    description: '',
    startDate: '',
    targetEndDate: '',
  });

  const { user } = useAuthStore();
  const canManageProjects = user?.role === UserRole.Manager || user?.role === UserRole.Admin;

  useEffect(() => {
    loadProjects();
  }, []);

  const loadProjects = async () => {
    try {
      const response = await projectService.getProjects(1, 50);
      if (response.success && response.data) {
        setProjects(response.data.items);
      } else {
        setError(response.message || 'Failed to load projects');
      }
    } catch (err) {
      setError('Failed to load projects');
      console.error(err);
    } finally {
      setIsLoading(false);
    }
  };

  const handleCreateProject = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    // Client-side validation matching server rules
    if (!formData.name || formData.name.trim().length < 2) {
      setError('Project name must be at least 2 characters');
      return;
    }

    const key = formData.key.toUpperCase().trim();
    const keyRegex = /^[A-Z0-9]{2,10}$/;
    if (!keyRegex.test(key)) {
      setError('Project key must be 2-10 characters, uppercase letters and numbers only');
      return;
    }

    if (formData.description && formData.description.length > 2000) {
      setError('Description cannot exceed 2000 characters');
      return;
    }

    if (formData.startDate && formData.targetEndDate) {
      const sd = new Date(formData.startDate);
      const te = new Date(formData.targetEndDate);
      if (isNaN(sd.getTime()) || isNaN(te.getTime())) {
        setError('Invalid dates provided');
        return;
      }
      if (te < sd) {
        setError('Target end date must be after start date');
        return;
      }
    }

    setIsCreating(true);

    try {
      const response = await projectService.createProject({
        name: formData.name.trim(),
        key,
        description: formData.description || undefined,
        startDate: formData.startDate || undefined,
        targetEndDate: formData.targetEndDate || undefined,
      });

      if (response.success && response.data) {
        setProjects([response.data, ...projects]);
        setIsModalOpen(false);
        setFormData({ name: '', key: '', description: '', startDate: '', targetEndDate: '' });
      } else {
        // show server-side validation if present
        if (response.errors && response.errors.length > 0) {
          setError(response.errors.join('; '));
        } else {
          setError(response.message || 'Failed to create project');
        }
      }
    } catch (err: any) {
      // Handle axios/server errors
      if (err?.isAxiosError && err.response?.data) {
        const serverData = err.response.data as any;
        if (serverData.errors && serverData.errors.length > 0) {
          setError(serverData.errors.join('; '));
        } else if (serverData.message) {
          setError(serverData.message);
        } else {
          setError('Failed to create project');
        }
      } else {
        setError('Failed to create project');
      }
      console.error(err);
    } finally {
      setIsCreating(false);
    }
  };

  const getStatusBadge = (status: ProjectStatus) => {
    const statusConfig = {
      [ProjectStatus.Active]: { variant: 'success' as const, label: 'Active' },
      [ProjectStatus.OnHold]: { variant: 'warning' as const, label: 'On Hold' },
      [ProjectStatus.Completed]: { variant: 'info' as const, label: 'Completed' },
      [ProjectStatus.Archived]: { variant: 'default' as const, label: 'Archived' },
    };
    const config = statusConfig[status] || statusConfig[ProjectStatus.Active];
    return <Badge variant={config.variant}>{config.label}</Badge>;
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-[60vh]">
        <Loader size="lg" />
      </div>
    );
  }

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      <div className="flex items-center justify-between mb-8">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Projects</h1>
          <p className="text-gray-600 mt-1">Manage your development projects</p>
        </div>
        {canManageProjects && (
          <Button onClick={() => setIsModalOpen(true)}>
            <Plus className="h-4 w-4 mr-2" />
            New Project
          </Button>
        )}
      </div>

      {error && (
        <div className="mb-6 p-4 bg-red-50 border border-red-200 rounded-lg text-red-700">
          {error}
        </div>
      )}

      {projects.length > 0 ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {projects.map((project) => (
            <Link key={project.id} href={`/projects/${project.id}`}>
              <Card className="h-full hover:shadow-md transition-shadow cursor-pointer">
                <div className="flex items-start justify-between mb-4">
                  <div className="flex items-center">
                    <div className="p-2 bg-blue-50 rounded-lg mr-3">
                      <FolderKanban className="h-5 w-5 text-blue-600" />
                    </div>
                    <div>
                      <h3 className="font-semibold text-gray-900">{project.name}</h3>
                      <span className="text-sm text-gray-500">{project.key}</span>
                    </div>
                  </div>
                  {getStatusBadge(project.status)}
                </div>

                {project.description && (
                  <p className="text-sm text-gray-600 mb-4 line-clamp-2">{project.description}</p>
                )}

                <div className="flex items-center justify-between text-sm text-gray-500 pt-4 border-t border-gray-100">
                  <div className="flex items-center">
                    <Users className="h-4 w-4 mr-1" />
                    {project.teamMembers?.length || 0} members
                  </div>
                  {project.startDate && (
                    <div className="flex items-center">
                      <Calendar className="h-4 w-4 mr-1" />
                      {format(new Date(project.startDate), 'MMM d, yyyy')}
                    </div>
                  )}
                </div>

                <div className="mt-4 flex items-center text-blue-600 text-sm font-medium">
                  View details <ArrowRight className="h-4 w-4 ml-1" />
                </div>
              </Card>
            </Link>
          ))}
        </div>
      ) : (
        <Card>
          <EmptyState
            title="No projects yet"
            description="Create your first project to start tracking sprints"
            icon={<FolderKanban className="h-8 w-8 text-gray-400" />}
            action={
              canManageProjects ? (
                <Button onClick={() => setIsModalOpen(true)}>
                  <Plus className="h-4 w-4 mr-2" />
                  Create Project
                </Button>
              ) : undefined
            }
          />
        </Card>
      )}

      <Modal isOpen={isModalOpen} onClose={() => setIsModalOpen(false)} title="Create New Project" size="md">
        <form onSubmit={handleCreateProject} className="space-y-4">
          <Input
            id="name"
            label="Project Name"
            value={formData.name}
            onChange={(e) => setFormData({ ...formData, name: e.target.value })}
            required
            placeholder="My Awesome Project"
          />

          <Input
            id="key"
            label="Project Key"
            value={formData.key}
            onChange={(e) => setFormData({ ...formData, key: e.target.value.toUpperCase() })}
            required
            placeholder="PROJ"
            maxLength={10}
          />

          <Textarea
            id="description"
            label="Description"
            value={formData.description}
            onChange={(e) => setFormData({ ...formData, description: e.target.value })}
            placeholder="Brief description of the project"
            rows={3}
          />

          <div className="grid grid-cols-2 gap-4">
            <Input
              id="startDate"
              type="date"
              label="Start Date"
              value={formData.startDate}
              onChange={(e) => setFormData({ ...formData, startDate: e.target.value })}
            />

            <Input
              id="targetEndDate"
              type="date"
              label="Target End Date"
              value={formData.targetEndDate}
              onChange={(e) => setFormData({ ...formData, targetEndDate: e.target.value })}
            />
          </div>

          <div className="flex justify-end space-x-3 pt-4">
            <Button type="button" variant="outline" onClick={() => setIsModalOpen(false)}>
              Cancel
            </Button>
            <Button type="submit" isLoading={isCreating}>
              Create Project
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
}
