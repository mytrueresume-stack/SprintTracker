import api from '@/lib/api';
import { ApiResponse, PaginatedResponse, Sprint, SprintCapacity, SprintRetrospective, SprintStatus } from '@/types';

export interface CreateSprintRequest {
  projectId: string;
  name: string;
  goal?: string;
  startDate: string;
  endDate: string;
}

export interface UpdateSprintRequest {
  name?: string;
  goal?: string;
  status?: SprintStatus;
  startDate?: string;
  endDate?: string;
  capacity?: SprintCapacity;
}

export interface BurndownData {
  sprintId: string;
  idealBurndown: BurndownPoint[];
  actualBurndown: BurndownPoint[];
}

export interface BurndownPoint {
  date: string;
  points: number;
}

export interface VelocityData {
  projectId: string;
  velocities: SprintVelocity[];
  averageVelocity: number;
}

export interface SprintVelocity {
  sprintName: string;
  committedPoints: number;
  completedPoints: number;
}

export const sprintService = {
  async getSprintsByProject(projectId: string, page = 1, pageSize = 10): Promise<ApiResponse<PaginatedResponse<Sprint>>> {
    const response = await api.get<ApiResponse<PaginatedResponse<Sprint>>>(`/sprints/project/${projectId}`, {
      params: { page, pageSize },
    });
    return response.data;
  },

  async getSprint(id: string): Promise<ApiResponse<Sprint>> {
    const response = await api.get<ApiResponse<Sprint>>(`/sprints/${id}`);
    return response.data;
  },

  async getActiveSprint(projectId: string): Promise<ApiResponse<Sprint>> {
    const response = await api.get<ApiResponse<Sprint>>(`/sprints/project/${projectId}/active`);
    return response.data;
  },

  async createSprint(data: CreateSprintRequest): Promise<ApiResponse<Sprint>> {
    const response = await api.post<ApiResponse<Sprint>>('/sprints', data);
    return response.data;
  },

  async updateSprint(id: string, data: UpdateSprintRequest): Promise<ApiResponse<Sprint>> {
    const response = await api.put<ApiResponse<Sprint>>(`/sprints/${id}`, data);
    return response.data;
  },

  async startSprint(id: string): Promise<ApiResponse<Sprint>> {
    const response = await api.post<ApiResponse<Sprint>>(`/sprints/${id}/start`);
    return response.data;
  },

  async completeSprint(id: string, retrospective?: SprintRetrospective): Promise<ApiResponse<Sprint>> {
    const response = await api.post<ApiResponse<Sprint>>(`/sprints/${id}/complete`, retrospective);
    return response.data;
  },

  async deleteSprint(id: string): Promise<ApiResponse<boolean>> {
    const response = await api.delete<ApiResponse<boolean>>(`/sprints/${id}`);
    return response.data;
  },

  async getBurndownData(id: string): Promise<ApiResponse<BurndownData>> {
    const response = await api.get<ApiResponse<BurndownData>>(`/sprints/${id}/burndown`);
    return response.data;
  },

  async getVelocityData(projectId: string): Promise<ApiResponse<VelocityData>> {
    const response = await api.get<ApiResponse<VelocityData>>(`/sprints/project/${projectId}/velocity`);
    return response.data;
  },
};
