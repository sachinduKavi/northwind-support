import { createContext, useContext } from 'react';
import type { CurrentAgent } from '../../types/api';

export interface AuthContextValue {
  agent?: CurrentAgent;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
}

export const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function useAuth(): AuthContextValue {
  const value = useContext(AuthContext);

  if (!value) {
    throw new Error('useAuth must be used inside AuthProvider.');
  }

  return value;
}

export interface RedirectState {
  from?: string;
}
