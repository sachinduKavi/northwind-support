import type { CurrentAgent } from '../../types/api';

export interface Session {
  token: string;
  expiresAtUtc: string;
  agent: CurrentAgent;
}

const storageKey = 'northwind-support.session';

let current: Session | undefined = readStoredSession();

export function isExpired(session: Session): boolean {
  return Date.parse(session.expiresAtUtc) <= Date.now();
}

export function currentSession(): Session | undefined {
  if (current && isExpired(current)) {
    clearSession();
  }

  return current;
}

export function saveSession(session: Session): void {
  current = session;

  try {
    sessionStorage.setItem(storageKey, JSON.stringify(session));
  } catch {
    return;
  }
}

export function clearSession(): void {
  current = undefined;

  try {
    sessionStorage.removeItem(storageKey);
  } catch {
    return;
  }
}

function readStoredSession(): Session | undefined {
  try {
    const raw = sessionStorage.getItem(storageKey);
    const session = raw ? (JSON.parse(raw) as Session) : undefined;

    return session && !isExpired(session) ? session : undefined;
  } catch {
    return undefined;
  }
}
