import api from '@/lib/api';
import { ApiResponse, DashboardStats } from '@/types';

export interface ActivityLog {
  id: string;
  userId: string;
  action: string;
  entityType: string;
  entityId: string;
  details?: string;
  timestamp: string;
}

export const dashboardService = {
  async getDashboardStats(): Promise<ApiResponse<DashboardStats>> {
    const response = await api.get<ApiResponse<DashboardStats>>('/dashboard');
    return response.data;
  },

  async getRecentActivity(count = 20): Promise<ApiResponse<ActivityLog[]>> {
    const response = await api.get<ApiResponse<ActivityLog[]>>('/dashboard/activity', {
      params: { count },
    });
    return response.data;
  },
};
