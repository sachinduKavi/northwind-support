import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuth, type RedirectState } from './authContext';

export function RequireAuth() {
  const { agent } = useAuth();
  const location = useLocation();

  if (!agent) {
    const state: RedirectState = { from: `${location.pathname}${location.search}` };

    return <Navigate to="/login" replace state={state} />;
  }

  return <Outlet />;
}
