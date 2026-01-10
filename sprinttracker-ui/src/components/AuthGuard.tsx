'use client';

import { useEffect, useSyncExternalStore, useRef } from 'react';
import { useRouter, usePathname } from 'next/navigation';
import { useAuthStore } from '@/store/authStore';
import { Loader } from '@/components/ui';

const publicPaths = ['/login', '/register'];

const emptySubscribe = () => () => {};

export default function AuthGuard({ children }: { children: React.ReactNode }) {
  const router = useRouter();
  const pathname = usePathname();
  const { isAuthenticated } = useAuthStore();
  const hasCheckedAuth = useRef(false);
  
  const isMounted = useSyncExternalStore(
    emptySubscribe,
    () => true,
    () => false
  );

  useEffect(() => {
    if (!isMounted || hasCheckedAuth.current) return;
    
    const isPublicPath = publicPaths.includes(pathname);

    if (!isAuthenticated && !isPublicPath) {
      router.push('/login');
    } else if (isAuthenticated && isPublicPath) {
      router.push('/dashboard');
    }
    hasCheckedAuth.current = true;
  }, [isAuthenticated, pathname, router, isMounted]);

  if (!isMounted) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <Loader size="lg" />
      </div>
    );
  }

  return <>{children}</>;
}
