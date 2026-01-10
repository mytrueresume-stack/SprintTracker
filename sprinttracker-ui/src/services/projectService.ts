import api from '@/lib/api';
import { ApiResponse, PaginatedResponse, Project, ProjectStatus } from '@/types';

export interface CreateProjectRequest {
  name: string;
  key: string;
  description?: string;
  startDate?: string;
  targetEndDate?: string;
}

export interface UpdateProjectRequest {
  name?: string;
  description?: string;
  status?: ProjectStatus;
  targetEndDate?: string;
  teamMemberIds?: string[];
}

export const projectService = {
  async getProjects(page = 1, pageSize = 10): Promise<ApiResponse<PaginatedResponse<Project>>> {
    const response = await api.get<ApiResponse<PaginatedResponse<Project>>>('/projects', {
      params: { page, pageSize },
    });
    return response.data;
  },

  async getProject(id: string): Promise<ApiResponse<Project>> {
    const response = await api.get<ApiResponse<Project>>(`/projects/${id}`);
    return response.data;
  },

  async createProject(data: CreateProjectRequest): Promise<ApiResponse<Project>> {
    const response = await api.post<ApiResponse<Project>>('/projects', data);
    return response.data;
  },

  async updateProject(id: string, data: UpdateProjectRequest): Promise<ApiResponse<Project>> {
    const response = await api.put<ApiResponse<Project>>(`/projects/${id}`, data);
    return response.data;
  },

  async deleteProject(id: string): Promise<ApiResponse<boolean>> {
    const response = await api.delete<ApiResponse<boolean>>(`/projects/${id}`);
    return response.data;
  },

  async addTeamMember(projectId: string, memberId: string): Promise<ApiResponse<boolean>> {
    const response = await api.post<ApiResponse<boolean>>(`/projects/${projectId}/members/${memberId}`);
    return response.data;
  },

  async updateTeamMembers(projectId: string, memberIds: string[]): Promise<ApiResponse<Project>> {
    const response = await api.post<ApiResponse<Project>>(`/projects/${projectId}/members`, { memberIds });
    return response.data;
  },

  async removeTeamMember(projectId: string, memberId: string): Promise<ApiResponse<boolean>> {
    const response = await api.delete<ApiResponse<boolean>>(`/projects/${projectId}/members/${memberId}`);
    return response.data;
  },
};
