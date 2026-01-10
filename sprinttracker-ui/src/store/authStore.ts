import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import { User, UserRole } from '@/types';

interface AuthState {
  token: string | null;
  user: User | null;
  isAuthenticated: boolean;
  setAuth: (token: string, user: User) => void;
  logout: () => void;
  updateUser: (user: User) => void;
}

function normalizeUserRole(user: User): User {
  // Convert role strings like "Manager" or numeric strings like "1" to the numeric enum value
  const roleVal = (user as any).role;
  let normalizedRole: UserRole;
  if (typeof roleVal === 'string') {
    // If it's a numeric string like "1", parse it first
    const num = Number(roleVal);
    if (!Number.isNaN(num)) {
      normalizedRole = num as UserRole;
    } else {
      // Attempt to map by name (e.g., "Manager")
      const mapped = (UserRole as any)[roleVal as string];
      normalizedRole = typeof mapped === 'number' ? mapped : UserRole.Developer;
    }
  } else if (typeof roleVal === 'number') {
    normalizedRole = roleVal;
  } else {
    normalizedRole = UserRole.Developer;
  }

  return { ...user, role: normalizedRole };
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      token: null,
      user: null,
      isAuthenticated: false,
      setAuth: (token: string, user: User) => {
        const normalized = normalizeUserRole(user);
        if (typeof window !== 'undefined') {
          localStorage.setItem('token', token);
        }
        set({ token, user: normalized, isAuthenticated: true });
      },
      logout: () => {
        if (typeof window !== 'undefined') {
          localStorage.removeItem('token');
        }
        set({ token: null, user: null, isAuthenticated: false });
      },
      updateUser: (user: User) => set({ user: normalizeUserRole(user) }),
    }),
    {
      name: 'auth-storage',
      partialize: (state) => ({ token: state.token, user: state.user, isAuthenticated: state.isAuthenticated }),
    }
  )
);
