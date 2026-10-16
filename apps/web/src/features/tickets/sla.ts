import type { BadgeTone } from '../../components/Badge';
import type { SlaStatus } from '../../types/api';

export const slaStates: Record<SlaStatus, { label: string; tone: BadgeTone; symbol: string }> = {
  WithinSla: { label: 'Within SLA', tone: 'info', symbol: '○' },
  AtRisk: { label: 'At risk', tone: 'warning', symbol: '!' },
  Breached: { label: 'Breached', tone: 'danger', symbol: '✕' },
  Met: { label: 'Met', tone: 'success', symbol: '✓' },
  NotApplicable: { label: 'No SLA', tone: 'neutral', symbol: '–' },
};

export const slaStatuses = Object.keys(slaStates) as SlaStatus[];
