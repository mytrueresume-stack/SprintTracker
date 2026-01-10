'use client';

import { useEffect, useState } from 'react';
import { useParams } from 'next/navigation';
import Link from 'next/link';
import { AxiosError } from 'axios';
import { projectService } from '@/services/projectService';
import { sprintService } from '@/services/sprintService';
import { Project, Sprint, SprintStatus, UserRole, ApiResponse } from '@/types';
import { Card, Button, Loader, Badge, EmptyState, Modal, Input, Textarea } from '@/components/ui';
import { useAuthStore } from '@/store/authStore';
import { ArrowLeft, Plus, Zap, Calendar, Play, CheckCircle, FileText, BarChart3, Users } from 'lucide-react';
import { format } from 'date-fns';
import { userService } from '@/services/userService';
import { User } from '@/types';

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

  // Sprint start / feedback state
  const [startingSprintId, setStartingSprintId] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState('');

  // Edit / Delete states for sprints
  const [editingSprintId, setEditingSprintId] = useState<string | null>(null);
  const [isDeletingId, setIsDeletingId] = useState<string | null>(null);
  const [isUpdating, setIsUpdating] = useState(false);

  const extractErrorMessage = (err: unknown): string => {
    if (err instanceof AxiosError && err.response?.data) {
      const data = err.response.data as ApiResponse<unknown>;
      if (data.message) return data.message;
      if (data.errors && data.errors.length > 0) return data.errors.join(', ');
    }
    if (err instanceof Error) return err.message;
    return 'An unexpected error occurred';
  };

  const { user } = useAuthStore();
  const isManager = user?.role === UserRole.Manager || user?.role === UserRole.Admin;

  // Team management state
  const [isTeamModalOpen, setIsTeamModalOpen] = useState(false);
  const [candidates, setCandidates] = useState<User[]>([]);
  const [isCandidatesLoading, setIsCandidatesLoading] = useState(false);
  const [searchTerm, setSearchTerm] = useState('');
  const [addingMemberId, setAddingMemberId] = useState<string | null>(null);
  const [removingMemberId, setRemovingMemberId] = useState<string | null>(null);

  const loadCandidates = async (search = '') => {
    setIsCandidatesLoading(true);
    try {
      const res = await userService.getUsers(search);
      if (res.success && res.data) {
        // Filter out users already in team
        const existingIds = new Set(project?.teamMembers?.map(m => m.id));
        setCandidates(res.data.filter(u => !existingIds.has(u.id)));
      }
    } catch (err) {
      console.error('Failed to load candidates', err);
    } finally {
      setIsCandidatesLoading(false);
    }
  };

  const openTeamModal = async () => {
    setIsTeamModalOpen(true);
    await loadCandidates('');
  };

  useEffect(() => {
    if (!isTeamModalOpen) return;
    const timer = setTimeout(() => {
      loadCandidates(searchTerm);
    }, 300);
    return () => clearTimeout(timer);
  }, [searchTerm, isTeamModalOpen]);

  const handleAddMember = async (memberId: string) => {
    setAddingMemberId(memberId);
    try {
      const res = await projectService.addTeamMember(projectId, memberId);
      if (res.success) {
        await loadProjectData();
        await loadCandidates(searchTerm);
      } else {
        setError(res.message || 'Failed to add member');
      }
    } catch (err) {
      console.error('Error adding team member', err);
      setError('Failed to add member');
    } finally {
      setAddingMemberId(null);
    }
  };

  const handleRemoveMember = async (memberId: string) => {
    setRemovingMemberId(memberId);
    try {
      const res = await projectService.removeTeamMember(projectId, memberId);
      if (res.success) {
        await loadProjectData();
        await loadCandidates(searchTerm);
      } else {
        setError(res.message || 'Failed to remove member');
      }
    } catch (err) {
      console.error('Error removing team member', err);
      setError('Failed to remove member');
    } finally {
      setRemovingMemberId(null);
    }
  };

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
    setError('');
    setSuccessMessage('');

    try {
      if (editingSprintId) {
        setIsUpdating(true);
        const response = await sprintService.updateSprint(editingSprintId, {
          name: formData.name,
          goal: formData.goal || undefined,
          startDate: formData.startDate,
          endDate: formData.endDate,
        });

        if (response.success && response.data) {
          await loadProjectData();
          setIsModalOpen(false);
          setEditingSprintId(null);
          setFormData({ name: '', goal: '', startDate: '', endDate: '' });
          setSuccessMessage('Sprint updated successfully');
        } else {
          setError(response.message || 'Failed to update sprint');
        }
      } else {
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
          setSuccessMessage('Sprint created successfully');
        } else {
          setError(response.message || 'Failed to create sprint');
        }
      }
    } catch (err) {
      setError(extractErrorMessage(err));
      console.error(err);
    } finally {
      setIsCreating(false);
      setIsUpdating(false);
    }
  };

  const handleStartSprint = async (sprintId: string) => {
    setStartingSprintId(sprintId);
    setError('');
    setSuccessMessage('');
    try {
      const response = await sprintService.startSprint(sprintId);
      if (response.success && response.data) {
        // Refresh project data to ensure everything (sprints, stats) is up-to-date
        await loadProjectData();
        setSuccessMessage('Sprint started successfully');
      } else {
        setError(response.message || 'Failed to start sprint');
      }
    } catch (err) {
      const msg = extractErrorMessage(err);
      setError(msg);
      console.error('Failed to start sprint:', err);
    } finally {
      setStartingSprintId(null);
    }
  };

  const handleEditSprint = (sprint: Sprint) => {
    setEditingSprintId(sprint.id);
    setFormData({
      name: sprint.name,
      goal: sprint.goal || '',
      startDate: sprint.startDate.slice(0, 10),
      endDate: sprint.endDate.slice(0, 10),
    });
    setIsModalOpen(true);
  };

  const handleDeleteSprint = async (sprintId: string) => {
    if (!confirm('Are you sure you want to delete this sprint? This cannot be undone.')) return;
    setIsDeletingId(sprintId);
    setError('');
    setSuccessMessage('');
    try {
      const res = await sprintService.deleteSprint(sprintId);
      if (res.success) {
        await loadProjectData();
        setSuccessMessage('Sprint deleted successfully');
      } else {
        setError(res.message || 'Failed to delete sprint');
      }
    } catch (err) {
      setError(extractErrorMessage(err));
      console.error('Delete error:', err);
    } finally {
      setIsDeletingId(null);
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
          {isManager && (
            <Button onClick={() => setIsModalOpen(true)}>
              <Plus className="h-4 w-4 mr-2" />
              New Sprint
            </Button>
          )}
        </div>
      </div>

      {error && (
        <div className="mb-6 p-4 bg-red-50 border border-red-200 rounded-lg text-red-700">
          {error}
        </div>
      )}

      {successMessage && (
        <div className="mb-6 p-4 bg-green-50 border border-green-200 rounded-lg text-green-700">
          {successMessage}
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
                    {sprint.status === SprintStatus.Planning && isManager && (
                      <>
                        <Button size="sm" variant="outline" onClick={() => handleStartSprint(sprint.id)} isLoading={startingSprintId === sprint.id}>
                          <Play className="h-4 w-4 mr-1" />
                          Start
                        </Button>

                        <Button size="sm" variant="outline" onClick={() => handleEditSprint(sprint)} isLoading={isUpdating && editingSprintId === sprint.id}>
                          Edit
                        </Button>

                        <Button size="sm" variant="outline" className="text-red-600" onClick={() => handleDeleteSprint(sprint.id)} isLoading={isDeletingId === sprint.id}>
                          Delete
                        </Button>
                      </>
                    )}

                    {sprint.status === SprintStatus.Active && (
                      <>
                        {user?.role === UserRole.Developer && (
                          <Link href={`/sprints/${sprint.id}/submit`}>
                            <Button size="sm" variant="outline">
                              <FileText className="h-4 w-4 mr-1" />
                              Submit Work
                            </Button>
                          </Link>
                        )}

                        {isManager && (
                          <Button size="sm" variant="outline" onClick={() => handleCompleteSprint(sprint.id)}>
                            <CheckCircle className="h-4 w-4 mr-1" />
                            Complete
                          </Button>
                        )}
                      </>
                    )}

                    {isManager && (
                      <Link href={`/sprints/${sprint.id}/report`}>
                        <Button size="sm" variant="ghost">
                          <BarChart3 className="h-4 w-4 mr-1" />
                          Report
                        </Button>
                      </Link>
                    )}
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
                isManager ? (
                  <Button onClick={() => setIsModalOpen(true)}>
                    <Plus className="h-4 w-4 mr-2" />
                    Create Sprint
                  </Button>
                ) : undefined
              }
            />
          </Card>
        )}
      </div>

      <div className="mb-8">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Team Members</h2>

        <Card>
          <div className="flex items-center justify-between mb-4">
            <p className="text-sm text-gray-600">Manage who is part of this project.</p>
            {isManager && (
              <Button onClick={openTeamModal}>
                <Plus className="h-4 w-4 mr-2" />
                Add Member
              </Button>
            )}
          </div>

          {project.teamMembers && project.teamMembers.length > 0 ? (
            <div className="space-y-3">
              {project.teamMembers.map((member) => (
                <div key={member.id} className="flex items-center justify-between">
                  <div className="flex items-center space-x-3">
                    <div className="w-9 h-9 bg-gray-100 rounded-full flex items-center justify-center text-sm font-medium text-gray-700">
                      {member.firstName?.[0] || member.fullName?.[0] || 'U'}
                    </div>
                    <div>
                      <div className="font-medium text-gray-900">{member.fullName}</div>
                      <div className="text-sm text-gray-500">{member.email} • {member.role}</div>
                    </div>
                  </div>

                  <div>
                    {isManager && member.id !== project.ownerId && (
                      <Button size="sm" variant="outline" className="text-red-600" onClick={() => handleRemoveMember(member.id)} isLoading={removingMemberId === member.id}>
                        Remove
                      </Button>
                    )}
                  </div>
                </div>
              ))}
            </div>
          ) : (
            <EmptyState
              title="No team members"
              description="Add team members so they can participate in sprints and tasks"
              icon={<Users className="h-8 w-8 text-gray-400" />}
              action={isManager ? (
                <Button onClick={openTeamModal}>
                  <Plus className="h-4 w-4 mr-2" />
                  Add Member
                </Button>
              ) : undefined}
            />
          )}
        </Card>
      </div>

      <Modal isOpen={isTeamModalOpen} onClose={() => setIsTeamModalOpen(false)} title="Manage Team" size="md">
        <div className="space-y-4">
          <Input
            id="search"
            label="Search users"
            value={searchTerm}
            onChange={(e) => { setSearchTerm(e.target.value); }}
            placeholder="Search by name or email"
          />

          <div className="max-h-60 overflow-auto space-y-2">
            {isCandidatesLoading ? (
              <div className="text-center py-4">
                <Loader />
              </div>
            ) : candidates.length === 0 ? (
              <div className="text-sm text-gray-500">No users found</div>
            ) : (
              candidates.map((u) => (
                <div key={u.id} className="flex items-center justify-between">
                  <div className="flex items-center space-x-3">
                    <div className="w-9 h-9 bg-gray-100 rounded-full flex items-center justify-center text-sm font-medium text-gray-700">
                      {u.firstName?.[0] || u.fullName?.[0] || 'U'}
                    </div>
                    <div>
                      <div className="font-medium text-gray-900">{u.fullName}</div>
                      <div className="text-sm text-gray-500">{u.email} • {u.role}</div>
                    </div>
                  </div>

                  <div>
                    <Button size="sm" onClick={() => handleAddMember(u.id)} isLoading={addingMemberId === u.id}>
                      Add
                    </Button>
                  </div>
                </div>
              ))
            )}
          </div>

          <div className="flex justify-end space-x-3">
            <Button variant="outline" onClick={() => setIsTeamModalOpen(false)}>
              Close
            </Button>
          </div>
        </div>
      </Modal>

      <Modal isOpen={isModalOpen} onClose={() => { setIsModalOpen(false); setEditingSprintId(null); setFormData({ name: '', goal: '', startDate: '', endDate: '' }); }} title={editingSprintId ? 'Edit Sprint' : 'Create New Sprint'} size="md">
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
            <Button type="button" variant="outline" onClick={() => { setIsModalOpen(false); setEditingSprintId(null); setFormData({ name: '', goal: '', startDate: '', endDate: '' }); }}>
              Cancel
            </Button>
            <Button type="submit" isLoading={isCreating || isUpdating}>
              {editingSprintId ? 'Update Sprint' : 'Create Sprint'}
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
}
