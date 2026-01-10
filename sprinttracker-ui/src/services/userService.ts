import api from '@/lib/api';
import { ApiResponse, User, UserRole } from '@/types';

export interface UpdateUserRequest {
  firstName?: string;
  lastName?: string;
  avatar?: string;
  role?: UserRole;
}

export const userService = {
  async getUsers(search?: string, role?: UserRole): Promise<ApiResponse<User[]>> {
    const response = await api.get<ApiResponse<User[]>>('/users', {
      params: { search, role },
    });
    return response.data;
  },

  async getUser(id: string): Promise<ApiResponse<User>> {
    const response = await api.get<ApiResponse<User>>(`/users/${id}`);
    return response.data;
  },

  async updateUser(id: string, data: UpdateUserRequest): Promise<ApiResponse<User>> {
    const response = await api.put<ApiResponse<User>>(`/users/${id}`, data);
    return response.data;
  },

  async getProjectTeam(projectId: string): Promise<ApiResponse<User[]>> {
    const response = await api.get<ApiResponse<User[]>>(`/users/team/${projectId}`);
    return response.data;
  },

  async deactivateUser(id: string): Promise<ApiResponse<boolean>> {
    const response = await api.delete<ApiResponse<boolean>>(`/users/${id}`);
    return response.data;
  },

  async reactivateUser(id: string): Promise<ApiResponse<boolean>> {
    const response = await api.post<ApiResponse<boolean>>(`/users/${id}/reactivate`);
    return response.data;
  },
};
