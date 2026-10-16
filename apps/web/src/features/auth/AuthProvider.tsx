import { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { authApi } from '../../api/auth';
import { setUnauthorizedHandler } from '../../api/client';
import { AuthContext, type AuthContextValue, type RedirectState } from './authContext';
import { clearSession, currentSession, isExpired, saveSession, type Session } from './session';

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const navigate = useNavigate();
  const [session, setSession] = useState<Session | undefined>(currentSession);

  const endSession = useCallback(
    (state?: RedirectState) => {
      clearSession();
      setSession(undefined);
      void navigate('/login', { replace: true, state });
    },
    [navigate],
  );

  useEffect(() => {
    setUnauthorizedHandler(() =>
      endSession({ from: `${window.location.pathname}${window.location.search}` }),
    );

    return () => setUnauthorizedHandler(() => undefined);
  }, [endSession]);

  const value = useMemo<AuthContextValue>(
    () => ({
      agent: session && !isExpired(session) ? session.agent : undefined,
      login: async (email, password) => {
        const response = await authApi.login({ email, password });
        const next: Session = {
          token: response.token,
          expiresAtUtc: response.expiresAtUtc,
          agent: response.agent,
        };

        saveSession(next);
        setSession(next);
      },
      logout: () => endSession(),
    }),
    [session, endSession],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
